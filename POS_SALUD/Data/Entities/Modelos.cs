namespace POS_SALUD.Data.Entities;

public class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class Sucursal
{
    public int IdSucursal { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int? IdUsuarioCreacion { get; set; }
}

public class Usuario
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public int? IdSucursalPredeterminada { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Rol? Rol { get; set; }
    public Sucursal? SucursalPredeterminada { get; set; }
}

public class Generico
{
    public int IdGenerico { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }
}

public class Laboratorio
{
    public int IdLaboratorio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }
}

public class Articulo
{
    public int IdArticulo { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdGenerico { get; set; }
    public int IdLaboratorio { get; set; }
    public bool RequiereReceta { get; set; }
    public decimal PrecioBase { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }

    public Generico? Generico { get; set; }
    public Laboratorio? Laboratorio { get; set; }
}

public class ArticuloSucursal
{
    public int IdArticuloSucursal { get; set; }
    public int IdSucursal { get; set; }
    public int IdArticulo { get; set; }
    public int ExistenciaTotal { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    public Sucursal? Sucursal { get; set; }
    public Articulo? Articulo { get; set; }
    public ICollection<ArticuloSucursalLote> Lotes { get; set; } = new List<ArticuloSucursalLote>();
}

public class ArticuloSucursalLote
{
    public int IdArticuloSucursalLote { get; set; }
    public int IdArticuloSucursal { get; set; }
    public string Lote { get; set; } = string.Empty;
    public DateOnly FechaVencimiento { get; set; }
    public decimal CostoUnitario { get; set; }
    public int Existencia { get; set; }

    public ArticuloSucursal? ArticuloSucursal { get; set; }
}

public class TipoDocumento
{
    public int IdTipoDocumento { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool AfectaInventario { get; set; } = true;
}

public class Proveedor
{
    public int IdProveedor { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }
}

public class Cliente
{
    public int IdCliente { get; set; }
    public string? Nit { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }
}

public class Compra
{
    public int IdCompra { get; set; }
    public int IdTipoDocumento { get; set; }
    public int IdProveedor { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaDocumento { get; set; }
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }

    public TipoDocumento? TipoDocumento { get; set; }
    public Proveedor? Proveedor { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
}

public class DetalleCompra
{
    public int IdDetalleCompra { get; set; }
    public int IdCompra { get; set; }
    public int IdArticulo { get; set; }
    public int IdArticuloSucursalLote { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public Compra? Compra { get; set; }
    public Articulo? Articulo { get; set; }
    public ArticuloSucursalLote? Lote { get; set; }
}

public class Venta
{
    public int IdVenta { get; set; }
    public int IdTipoDocumento { get; set; }
    public int? IdCliente { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    public int? IdJornada { get; set; }
    public string TipoVenta { get; set; } = "VENTA";
    public int? IdVentaOrigen { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaDocumento { get; set; }
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }
    public decimal MontoEfectivo { get; set; }
    public decimal MontoTarjeta { get; set; }

    public TipoDocumento? TipoDocumento { get; set; }
    public Cliente? Cliente { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Usuario? Usuario { get; set; }
    public Jornada? Jornada { get; set; }
    public Venta? VentaOrigen { get; set; }
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}

public class Jornada
{
    public int IdJornada { get; set; }
    public int IdUsuario { get; set; }
    public int IdSucursal { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal FondoInicial { get; set; }
    public decimal? EfectivoEsperado { get; set; }
    public decimal? TarjetaEsperada { get; set; }
    public decimal? EfectivoDeclarado { get; set; }
    public decimal? TarjetaDeclarada { get; set; }
    public decimal? DiferenciaEfectivo { get; set; }
    public decimal? DiferenciaTarjeta { get; set; }
    public string Estado { get; set; } = "ABIERTA";

    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}

public class DetalleVenta
{
    public int IdDetalleVenta { get; set; }
    public int IdVenta { get; set; }
    public int IdArticulo { get; set; }
    public int IdArticuloSucursalLote { get; set; }
    public int? IdDetalleVentaOrigen { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public Venta? Venta { get; set; }
    public Articulo? Articulo { get; set; }
    public ArticuloSucursalLote? Lote { get; set; }
    public DetalleVenta? DetalleVentaOrigen { get; set; }
}

public class Traslado
{
    public int IdTraslado { get; set; }
    public int IdSucursalOrigen { get; set; }
    public int IdSucursalDestino { get; set; }
    public int IdUsuarioEnvia { get; set; }
    public int? IdUsuarioRecibe { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public string Estado { get; set; } = "EN_TRANSITO";
    public string? Observaciones { get; set; }

    public Sucursal? SucursalOrigen { get; set; }
    public Sucursal? SucursalDestino { get; set; }
    public ICollection<DetalleTraslado> Detalles { get; set; } = new List<DetalleTraslado>();
}

public class DetalleTraslado
{
    public int IdDetalleTraslado { get; set; }
    public int IdTraslado { get; set; }
    public int IdArticuloSucursalLoteOrigen { get; set; }
    public int CantidadEnviada { get; set; }
    public int? CantidadRecibida { get; set; }

    public Traslado? Traslado { get; set; }
    public ArticuloSucursalLote? LoteOrigen { get; set; }
}

public class MovimientoInventario
{
    public int IdMovimiento { get; set; }
    public int IdSucursal { get; set; }
    public int IdArticuloSucursalLote { get; set; }
    public DateTime? FechaMovimiento { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Documento { get; set; }
    public string? Observacion { get; set; }
    public int IdUsuario { get; set; }

    public Sucursal? Sucursal { get; set; }
    public ArticuloSucursalLote? Lote { get; set; }
    public Usuario? Usuario { get; set; }
}
