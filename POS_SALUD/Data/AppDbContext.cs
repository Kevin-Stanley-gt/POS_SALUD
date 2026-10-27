using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Generico> Genericos => Set<Generico>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<ArticuloSucursal> ArticuloSucursal => Set<ArticuloSucursal>();
    public DbSet<ArticuloSucursalLote> ArticuloSucursalLotes => Set<ArticuloSucursalLote>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetalleCompra => Set<DetalleCompra>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetalleVenta => Set<DetalleVenta>();
    public DbSet<Traslado> Traslados => Set<Traslado>();
    public DbSet<DetalleTraslado> DetalleTraslado => Set<DetalleTraslado>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Jornada> Jornadas => Set<Jornada>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().AreUnicode(false);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
        });

        modelBuilder.Entity<Sucursal>(e =>
        {
            e.ToTable("sucursales");
            e.HasKey(x => x.IdSucursal);
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Direccion).HasColumnName("direccion");
            e.Property(x => x.Telefono).HasColumnName("telefono");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.IdSucursalPredeterminada).HasColumnName("id_sucursal_predeterminada");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.IdRol);
            e.HasOne(x => x.SucursalPredeterminada).WithMany().HasForeignKey(x => x.IdSucursalPredeterminada);
        });

        modelBuilder.Entity<Generico>(e =>
        {
            e.ToTable("genericos");
            e.HasKey(x => x.IdGenerico);
            e.Property(x => x.IdGenerico).HasColumnName("id_generico");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Laboratorio>(e =>
        {
            e.ToTable("Laboratorio");
            e.HasKey(x => x.IdLaboratorio);
            e.Property(x => x.IdLaboratorio).HasColumnName("id_laboratorio");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Articulo>(e =>
        {
            e.ToTable("articulos");
            e.HasKey(x => x.IdArticulo);
            e.Property(x => x.IdArticulo).HasColumnName("id_articulo");
            e.Property(x => x.CodigoBarras).HasColumnName("codigo_barras");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.IdGenerico).HasColumnName("id_generico");
            e.Property(x => x.IdLaboratorio).HasColumnName("id_laboratorio");
            e.Property(x => x.RequiereReceta).HasColumnName("requiere_receta");
            e.Property(x => x.PrecioBase).HasColumnName("precio_base");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
            e.HasIndex(x => x.CodigoBarras).IsUnique();
            e.HasOne(x => x.Generico).WithMany().HasForeignKey(x => x.IdGenerico);
            e.HasOne(x => x.Laboratorio).WithMany().HasForeignKey(x => x.IdLaboratorio);
        });

        modelBuilder.Entity<ArticuloSucursal>(e =>
        {
            e.ToTable("articulo_sucursal");
            e.HasKey(x => x.IdArticuloSucursal);
            e.Property(x => x.IdArticuloSucursal).HasColumnName("id_articulo_sucursal");
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.IdArticulo).HasColumnName("id_articulo");
            e.Property(x => x.ExistenciaTotal).HasColumnName("existencia_total").ValueGeneratedOnAddOrUpdate();
            e.Property(x => x.StockMinimo).HasColumnName("stock_minimo");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.HasIndex(x => new { x.IdSucursal, x.IdArticulo }).IsUnique();
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo);
            e.HasMany(x => x.Lotes).WithOne(x => x.ArticuloSucursal).HasForeignKey(x => x.IdArticuloSucursal);
        });

        modelBuilder.Entity<ArticuloSucursalLote>(e =>
        {
            e.ToTable("articulo_sucursal_lotes", t => t.HasTrigger("trg_actualizar_existencia_total"));
            e.HasKey(x => x.IdArticuloSucursalLote);
            e.Property(x => x.IdArticuloSucursalLote).HasColumnName("id_articulo_sucursal_lote");
            e.Property(x => x.IdArticuloSucursal).HasColumnName("id_articulo_sucursal");
            e.Property(x => x.Lote).HasColumnName("lote");
            e.Property(x => x.FechaVencimiento).HasColumnName("fecha_vencimiento");
            e.Property(x => x.CostoUnitario).HasColumnName("costo_unitario");
            e.Property(x => x.Existencia).HasColumnName("existencia");
            e.HasIndex(x => new { x.IdArticuloSucursal, x.Lote }).IsUnique();
            e.HasIndex(x => x.FechaVencimiento);
        });

        modelBuilder.Entity<TipoDocumento>(e =>
        {
            e.ToTable("tipos_documento");
            e.HasKey(x => x.IdTipoDocumento);
            e.Property(x => x.IdTipoDocumento).HasColumnName("id_tipo_documento");
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.AfectaInventario).HasColumnName("afecta_inventario");
        });

        modelBuilder.Entity<Proveedor>(e =>
        {
            e.ToTable("proveedores");
            e.HasKey(x => x.IdProveedor);
            e.Property(x => x.IdProveedor).HasColumnName("id_proveedor");
            e.Property(x => x.Nit).HasColumnName("nit");
            e.Property(x => x.RazonSocial).HasColumnName("razon_social");
            e.Property(x => x.Telefono).HasColumnName("telefono");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
            e.HasIndex(x => x.Nit).IsUnique();
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasKey(x => x.IdCliente);
            e.Property(x => x.IdCliente).HasColumnName("id_cliente");
            e.Property(x => x.Nit).HasColumnName("nit");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Direccion).HasColumnName("direccion");
            e.Property(x => x.Telefono).HasColumnName("telefono");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.IdUsuarioCreacion).HasColumnName("id_usuario_creacion");
        });

        modelBuilder.Entity<Compra>(e =>
        {
            e.ToTable("compras");
            e.HasKey(x => x.IdCompra);
            e.Property(x => x.IdCompra).HasColumnName("id_compra");
            e.Property(x => x.IdTipoDocumento).HasColumnName("id_tipo_documento");
            e.Property(x => x.IdProveedor).HasColumnName("id_proveedor");
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.NumeroDocumento).HasColumnName("numero_documento");
            e.Property(x => x.FechaDocumento).HasColumnName("fecha_documento");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
            e.Property(x => x.Total).HasColumnName("total");
            e.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.IdTipoDocumento);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor);
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            e.HasMany(x => x.Detalles).WithOne(x => x.Compra).HasForeignKey(x => x.IdCompra).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DetalleCompra>(e =>
        {
            e.ToTable("detalle_compra");
            e.HasKey(x => x.IdDetalleCompra);
            e.Property(x => x.IdDetalleCompra).HasColumnName("id_detalle_compra");
            e.Property(x => x.IdCompra).HasColumnName("id_compra");
            e.Property(x => x.IdArticulo).HasColumnName("id_articulo");
            e.Property(x => x.IdArticuloSucursalLote).HasColumnName("id_articulo_sucursal_lote");
            e.Property(x => x.Cantidad).HasColumnName("cantidad");
            e.Property(x => x.CostoUnitario).HasColumnName("costo_unitario");
            e.Property(x => x.Subtotal).HasColumnName("subtotal");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo);
            e.HasOne(x => x.Lote).WithMany().HasForeignKey(x => x.IdArticuloSucursalLote);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.ToTable("ventas");
            e.HasKey(x => x.IdVenta);
            e.Property(x => x.IdVenta).HasColumnName("id_venta");
            e.Property(x => x.IdTipoDocumento).HasColumnName("id_tipo_documento");
            e.Property(x => x.IdCliente).HasColumnName("id_cliente");
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.NumeroDocumento).HasColumnName("numero_documento");
            e.Property(x => x.FechaDocumento).HasColumnName("fecha_documento");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
            e.Property(x => x.Total).HasColumnName("total");
            e.Property(x => x.IdJornada).HasColumnName("id_jornada");
            e.Property(x => x.MontoEfectivo).HasColumnName("monto_efectivo");
            e.Property(x => x.MontoTarjeta).HasColumnName("monto_tarjeta");
            e.Property(x => x.TipoVenta).HasColumnName("tipo_venta");
            e.Property(x => x.IdVentaOrigen).HasColumnName("id_venta_origen");
            e.HasOne(x => x.VentaOrigen).WithMany().HasForeignKey(x => x.IdVentaOrigen);
            e.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.IdTipoDocumento);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente);
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            e.HasOne(x => x.Jornada).WithMany(x => x.Ventas).HasForeignKey(x => x.IdJornada);
            e.HasMany(x => x.Detalles).WithOne(x => x.Venta).HasForeignKey(x => x.IdVenta).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Jornada>(e =>
        {
            e.ToTable("jornadas");
            e.HasKey(x => x.IdJornada);
            e.Property(x => x.IdJornada).HasColumnName("id_jornada");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.FechaApertura).HasColumnName("fecha_apertura");
            e.Property(x => x.FechaCierre).HasColumnName("fecha_cierre");
            e.Property(x => x.FondoInicial).HasColumnName("fondo_inicial");
            e.Property(x => x.EfectivoEsperado).HasColumnName("efectivo_esperado");
            e.Property(x => x.TarjetaEsperada).HasColumnName("tarjeta_esperada");
            e.Property(x => x.EfectivoDeclarado).HasColumnName("efectivo_declarado");
            e.Property(x => x.TarjetaDeclarada).HasColumnName("tarjeta_declarada");
            e.Property(x => x.DiferenciaEfectivo).HasColumnName("diferencia_efectivo");
            e.Property(x => x.DiferenciaTarjeta).HasColumnName("diferencia_tarjeta");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal);
        });

        modelBuilder.Entity<DetalleVenta>(e =>
        {
            e.ToTable("detalle_venta");
            e.HasKey(x => x.IdDetalleVenta);
            e.Property(x => x.IdDetalleVenta).HasColumnName("id_detalle_venta");
            e.Property(x => x.IdVenta).HasColumnName("id_venta");
            e.Property(x => x.IdArticulo).HasColumnName("id_articulo");
            e.Property(x => x.IdArticuloSucursalLote).HasColumnName("id_articulo_sucursal_lote");
            e.Property(x => x.IdDetalleVentaOrigen).HasColumnName("id_detalle_venta_origen");
            e.Property(x => x.Cantidad).HasColumnName("cantidad");
            e.Property(x => x.PrecioUnitario).HasColumnName("precio_unitario");
            e.Property(x => x.Subtotal).HasColumnName("subtotal");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo);
            e.HasOne(x => x.DetalleVentaOrigen).WithMany().HasForeignKey(x => x.IdDetalleVentaOrigen);
            e.HasOne(x => x.Lote).WithMany().HasForeignKey(x => x.IdArticuloSucursalLote);
        });

        modelBuilder.Entity<Traslado>(e =>
        {
            e.ToTable("traslados");
            e.HasKey(x => x.IdTraslado);
            e.Property(x => x.IdTraslado).HasColumnName("id_traslado");
            e.Property(x => x.IdSucursalOrigen).HasColumnName("id_sucursal_origen");
            e.Property(x => x.IdSucursalDestino).HasColumnName("id_sucursal_destino");
            e.Property(x => x.IdUsuarioEnvia).HasColumnName("id_usuario_envia");
            e.Property(x => x.IdUsuarioRecibe).HasColumnName("id_usuario_recibe");
            e.Property(x => x.FechaEnvio).HasColumnName("fecha_envio");
            e.Property(x => x.FechaRecepcion).HasColumnName("fecha_recepcion");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
            e.HasOne(x => x.SucursalOrigen).WithMany().HasForeignKey(x => x.IdSucursalOrigen).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.SucursalDestino).WithMany().HasForeignKey(x => x.IdSucursalDestino).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Detalles).WithOne(x => x.Traslado).HasForeignKey(x => x.IdTraslado).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DetalleTraslado>(e =>
        {
            e.ToTable("detalle_traslado");
            e.HasKey(x => x.IdDetalleTraslado);
            e.Property(x => x.IdDetalleTraslado).HasColumnName("id_detalle_traslado");
            e.Property(x => x.IdTraslado).HasColumnName("id_traslado");
            e.Property(x => x.IdArticuloSucursalLoteOrigen).HasColumnName("id_articulo_sucursal_lote_origen");
            e.Property(x => x.CantidadEnviada).HasColumnName("cantidad_enviada");
            e.Property(x => x.CantidadRecibida).HasColumnName("cantidad_recibida");
            e.HasOne(x => x.LoteOrigen).WithMany().HasForeignKey(x => x.IdArticuloSucursalLoteOrigen);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("movimientos_inventario");
            e.HasKey(x => x.IdMovimiento);
            e.Property(x => x.IdMovimiento).HasColumnName("id_movimiento");
            e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");
            e.Property(x => x.IdArticuloSucursalLote).HasColumnName("id_articulo_sucursal_lote");
            e.Property(x => x.FechaMovimiento).HasColumnName("fecha_movimiento");
            e.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento");
            e.Property(x => x.Cantidad).HasColumnName("cantidad");
            e.Property(x => x.Documento).HasColumnName("documento");
            e.Property(x => x.Observacion).HasColumnName("observacion");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal);
            e.HasOne(x => x.Lote).WithMany().HasForeignKey(x => x.IdArticuloSucursalLote);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
        });

    }
}
