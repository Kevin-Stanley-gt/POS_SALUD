IF OBJECT_ID(N'dbo.jornadas', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.jornadas (
		id_jornada INT IDENTITY(1,1) PRIMARY KEY,
		id_usuario INT NOT NULL,
		id_sucursal INT NOT NULL,
		fecha_apertura DATETIME2 NOT NULL,
		fecha_cierre DATETIME2 NULL,
		fondo_inicial DECIMAL(18,2) NOT NULL DEFAULT 0,
		efectivo_esperado DECIMAL(18,2) NULL,
		tarjeta_esperada DECIMAL(18,2) NULL,
		efectivo_declarado DECIMAL(18,2) NULL,
		tarjeta_declarada DECIMAL(18,2) NULL,
		diferencia_efectivo DECIMAL(18,2) NULL,
		diferencia_tarjeta DECIMAL(18,2) NULL,
		estado NVARCHAR(20) NOT NULL DEFAULT N'ABIERTA',
		CONSTRAINT FK_jornadas_usuarios FOREIGN KEY (id_usuario) REFERENCES dbo.usuarios (id_usuario),
		CONSTRAINT FK_jornadas_sucursales FOREIGN KEY (id_sucursal) REFERENCES dbo.sucursales (id_sucursal)
	);
END;

IF COL_LENGTH(N'dbo.ventas', N'id_jornada') IS NULL
	ALTER TABLE dbo.ventas ADD id_jornada INT NULL;

IF COL_LENGTH(N'dbo.ventas', N'monto_efectivo') IS NULL
	ALTER TABLE dbo.ventas ADD monto_efectivo DECIMAL(18,2) NOT NULL DEFAULT 0;

IF COL_LENGTH(N'dbo.ventas', N'monto_tarjeta') IS NULL
	ALTER TABLE dbo.ventas ADD monto_tarjeta DECIMAL(18,2) NOT NULL DEFAULT 0;

IF COL_LENGTH(N'dbo.ventas', N'tipo_venta') IS NULL
	ALTER TABLE dbo.ventas ADD tipo_venta NVARCHAR(20) NOT NULL CONSTRAINT DF_ventas_tipo_venta DEFAULT N'VENTA';

IF COL_LENGTH(N'dbo.ventas', N'id_venta_origen') IS NULL
	ALTER TABLE dbo.ventas ADD id_venta_origen INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ventas_ventas_origen')
	ALTER TABLE dbo.ventas ADD CONSTRAINT FK_ventas_ventas_origen FOREIGN KEY (id_venta_origen) REFERENCES dbo.ventas (id_venta);

IF COL_LENGTH(N'dbo.detalle_venta', N'id_detalle_venta_origen') IS NULL
	ALTER TABLE dbo.detalle_venta ADD id_detalle_venta_origen INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_detalle_venta_origen')
	ALTER TABLE dbo.detalle_venta ADD CONSTRAINT FK_detalle_venta_origen FOREIGN KEY (id_detalle_venta_origen) REFERENCES dbo.detalle_venta (id_detalle_venta);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ventas_jornadas')
	ALTER TABLE dbo.ventas ADD CONSTRAINT FK_ventas_jornadas FOREIGN KEY (id_jornada) REFERENCES dbo.jornadas (id_jornada);
