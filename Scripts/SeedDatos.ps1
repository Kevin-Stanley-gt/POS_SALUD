$ErrorActionPreference = 'Stop'
$proyecto = 'C:\Users\DBA\source\repos\POS_SALUD\POS_SALUD\POS_SALUD.csproj'
$linea = dotnet user-secrets list --project $proyecto | Where-Object { $_ -like 'ConnectionStrings:PosFarma*' }
$cs = $linea.Substring($linea.IndexOf(' = ') + 3)

function Get-BigEndian([int]$n) {
	$b = [BitConverter]::GetBytes($n)
	[Array]::Reverse($b)
	return $b
}

function New-PasswordHash([string]$password) {
	$salt = New-Object byte[] 16
	$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
	$rng.GetBytes($salt)
	$kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($password, $salt, 10000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
	$subkey = $kdf.GetBytes(32)
	$buffer = [byte[]](@(1) + (Get-BigEndian 1) + (Get-BigEndian 10000) + (Get-BigEndian 16) + $salt + $subkey)
	return [Convert]::ToBase64String($buffer)
}

$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()

function Invoke-Scalar([string]$sql, [hashtable]$params) {
	$cmd = $conn.CreateCommand()
	$cmd.CommandText = $sql
	foreach ($k in $params.Keys) { [void]$cmd.Parameters.AddWithValue($k, $(if ($null -eq $params[$k]) { [DBNull]::Value } else { $params[$k] })) }
	return $cmd.ExecuteScalar()
}

$roles = @{
	'Admin'    = 'Administrador del sistema'
	'Jefe'     = 'Jefe de sucursal'
	'Vendedor' = 'Vendedor de mostrador'
}
foreach ($r in $roles.Keys) {
	[void](Invoke-Scalar "IF NOT EXISTS (SELECT 1 FROM roles WHERE nombre = @n) INSERT INTO roles (nombre, descripcion) VALUES (@n, @d); SELECT id_rol FROM roles WHERE nombre = @n;" @{ '@n' = $r; '@d' = $roles[$r] })
}
$idRol = @{}
foreach ($r in $roles.Keys) {
	$idRol[$r] = [int](Invoke-Scalar "SELECT id_rol FROM roles WHERE nombre = @n;" @{ '@n' = $r })
}

$idSucursal = [int](Invoke-Scalar "IF NOT EXISTS (SELECT 1 FROM sucursales WHERE nombre = @n) INSERT INTO sucursales (nombre, direccion, telefono, activo) VALUES (@n, @dir, @tel, 1); SELECT id_sucursal FROM sucursales WHERE nombre = @n;" @{ '@n' = 'Sucursal Central'; '@dir' = 'Direccion por definir'; '@tel' = $null })

$usuarios = @(
	@{ Email = 'admin@posfarma.local'; Nombre = 'Administrador'; Rol = 'Admin';    Pass = 'Admin123!' },
	@{ Email = 'jefe@posfarma.local';  Nombre = 'Jefe Sucursal'; Rol = 'Jefe';     Pass = 'Jefe123!' },
	@{ Email = 'vendedor@posfarma.local'; Nombre = 'Vendedor 1';  Rol = 'Vendedor'; Pass = 'Vendedor123!' }
)
$idAdmin = $null
foreach ($u in $usuarios) {
	$hash = New-PasswordHash $u.Pass
	$id = [int](Invoke-Scalar "IF NOT EXISTS (SELECT 1 FROM usuarios WHERE email = @e) INSERT INTO usuarios (id_rol, id_sucursal_predeterminada, nombre, email, password_hash, activo) VALUES (@r, @s, @n, @e, @h, 1); SELECT id_usuario FROM usuarios WHERE email = @e;" @{ '@r' = $idRol[$u.Rol]; '@s' = $idSucursal; '@n' = $u.Nombre; '@e' = $u.Email; '@h' = $hash })
	if ($u.Rol -eq 'Admin') { $idAdmin = $id }
}

[void](Invoke-Scalar "UPDATE sucursales SET id_usuario_creacion = @u WHERE id_sucursal = @s AND id_usuario_creacion IS NULL; SELECT 1;" @{ '@u' = $idAdmin; '@s' = $idSucursal })

$conn.Close()
Write-Output "OK: roles, sucursal y usuarios registrados."
