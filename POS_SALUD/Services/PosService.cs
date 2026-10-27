using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Services;

public record LineaVenta(int IdArticulo, int Cantidad);

public record LineaCompra(int IdArticulo, string Lote, DateOnly FechaVencimiento, decimal CostoUnitario, int Cantidad);

public class PosService(AppDbContext db)
{
    public async Task<Jornada?> ObtenerJornadaAbiertaAsync(int idUsuario, CancellationToken ct = default)
    {
        return await db.Jornadas.FirstOrDefaultAsync(j => j.IdUsuario == idUsuario && j.Estado == "ABIERTA", ct);
    }

    public async Task<Jornada> AbrirJornadaAsync(int idUsuario, int idSucursal, decimal fondoInicial, CancellationToken ct = default)
    {
        if (fondoInicial < 0)
        {
            throw new InvalidOperationException("El fondo inicial no puede ser negativo.");
        }

        if (await ObtenerJornadaAbiertaAsync(idUsuario, ct) is not null)
        {
            throw new InvalidOperationException("El usuario ya tiene una jornada abierta.");
        }

        var jornada = new Jornada
        {
            IdUsuario = idUsuario,
            IdSucursal = idSucursal,
            FechaApertura = DateTime.Now,
            FondoInicial = fondoInicial,
            Estado = "ABIERTA"
        };
        db.Jornadas.Add(jornada);
        await db.SaveChangesAsync(ct);
        return jornada;
    }

    public async Task<Jornada> CerrarJornadaAsync(int idUsuario, decimal efectivoDeclarado, decimal tarjetaDeclarada, CancellationToken ct = default)
    {
        var jornada = await ObtenerJornadaAbiertaAsync(idUsuario, ct)
            ?? throw new InvalidOperationException("El usuario no tiene una jornada abierta.");

        if (efectivoDeclarado < 0 || tarjetaDeclarada < 0)
        {
            throw new InvalidOperationException("Los montos declarados no pueden ser negativos.");
        }

        var movimientos = await db.Ventas.Where(v => v.IdJornada == jornada.IdJornada).ToListAsync(ct);
        var ventas = movimientos.Where(v => v.TipoVenta == "VENTA").ToList();
        var devoluciones = movimientos.Where(v => v.TipoVenta == "DEVOLUCION").ToList();

        jornada.EfectivoEsperado = jornada.FondoInicial + ventas.Sum(v => v.MontoEfectivo) - devoluciones.Sum(d => d.MontoEfectivo);
        jornada.TarjetaEsperada = ventas.Sum(v => v.MontoTarjeta) - devoluciones.Sum(d => d.MontoTarjeta);
        jornada.EfectivoDeclarado = efectivoDeclarado;
        jornada.TarjetaDeclarada = tarjetaDeclarada;
        jornada.DiferenciaEfectivo = efectivoDeclarado - jornada.EfectivoEsperado;
        jornada.DiferenciaTarjeta = tarjetaDeclarada - jornada.TarjetaEsperada;
        jornada.FechaCierre = DateTime.Now;
        jornada.Estado = "CERRADA";

        await db.SaveChangesAsync(ct);
        return jornada;
    }

    public async Task<Venta> RegistrarVentaAsync(int idSucursal, int idUsuario, int idTipoDocumento, int? idCliente, IReadOnlyList<LineaVenta> lineas, decimal montoEfectivo, decimal montoTarjeta, CancellationToken ct = default)
    {
        if (lineas.Count == 0)
        {
            throw new InvalidOperationException("La venta no tiene artículos.");
        }

        var jornada = await ObtenerJornadaAbiertaAsync(idUsuario, ct)
            ?? throw new InvalidOperationException("Debe abrir una jornada antes de registrar ventas.");

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var venta = new Venta
        {
            IdTipoDocumento = idTipoDocumento,
            IdCliente = idCliente,
            IdSucursal = idSucursal,
            IdUsuario = idUsuario,
            IdJornada = jornada.IdJornada,
            NumeroDocumento = GenerarNumero("VT"),
            FechaDocumento = DateTime.Now
        };
        db.Ventas.Add(venta);

        foreach (var linea in lineas)
        {
            var articulo = await db.Articulos.AsNoTracking()
                .FirstOrDefaultAsync(a => a.IdArticulo == linea.IdArticulo, ct)
                ?? throw new InvalidOperationException($"Artículo {linea.IdArticulo} no existe.");

            var lotes = await db.ArticuloSucursalLotes
                .Where(l => l.ArticuloSucursal!.IdSucursal == idSucursal
                         && l.ArticuloSucursal.IdArticulo == linea.IdArticulo
                         && l.Existencia > 0)
                .OrderBy(l => l.FechaVencimiento)
                .ToListAsync(ct);

            if (lotes.Sum(l => l.Existencia) < linea.Cantidad)
            {
                throw new InvalidOperationException($"Stock insuficiente para {articulo.Nombre}.");
            }

            var pendiente = linea.Cantidad;
            foreach (var lote in lotes.TakeWhile(_ => pendiente > 0))
            {
                var cantidad = Math.Min(pendiente, lote.Existencia);
                lote.Existencia -= cantidad;
                pendiente -= cantidad;

                venta.Detalles.Add(new DetalleVenta
                {
                    IdArticulo = linea.IdArticulo,
                    IdArticuloSucursalLote = lote.IdArticuloSucursalLote,
                    Cantidad = cantidad,
                    PrecioUnitario = articulo.PrecioBase,
                    Subtotal = cantidad * articulo.PrecioBase
                });

                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdSucursal = idSucursal,
                    IdArticuloSucursalLote = lote.IdArticuloSucursalLote,
                    FechaMovimiento = DateTime.Now,
                    TipoMovimiento = "SALIDA",
                    Cantidad = cantidad,
                    Documento = venta.NumeroDocumento,
                    IdUsuario = idUsuario
                });
            }
        }

        venta.Total = venta.Detalles.Sum(d => d.Subtotal);

        if (montoEfectivo < 0 || montoTarjeta < 0 || montoEfectivo + montoTarjeta != venta.Total)
        {
            throw new InvalidOperationException($"El pago ({montoEfectivo + montoTarjeta:0.00}) no coincide con el total de la venta ({venta.Total:0.00}).");
        }

        venta.MontoEfectivo = montoEfectivo;
        venta.MontoTarjeta = montoTarjeta;
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return venta;
    }

    public async Task<Compra> RegistrarCompraAsync(int idTipoDocumento, int idProveedor, int idSucursal, int idUsuario, string numeroDocumento, string? observaciones, IReadOnlyList<LineaCompra> lineas, CancellationToken ct = default)
    {
        if (lineas.Count == 0)
        {
            throw new InvalidOperationException("La compra no tiene artículos.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var compra = new Compra
        {
            IdTipoDocumento = idTipoDocumento,
            IdProveedor = idProveedor,
            IdSucursal = idSucursal,
            IdUsuario = idUsuario,
            NumeroDocumento = numeroDocumento,
            FechaDocumento = DateTime.Now,
            Observaciones = observaciones
        };
        db.Compras.Add(compra);

        foreach (var linea in lineas)
        {
            var articuloSucursal = await db.ArticuloSucursal
                .FirstOrDefaultAsync(x => x.IdSucursal == idSucursal && x.IdArticulo == linea.IdArticulo, ct);

            if (articuloSucursal is null)
            {
                articuloSucursal = new ArticuloSucursal { IdSucursal = idSucursal, IdArticulo = linea.IdArticulo };
                db.ArticuloSucursal.Add(articuloSucursal);
                await db.SaveChangesAsync(ct);
            }

            var lote = await db.ArticuloSucursalLotes
                .FirstOrDefaultAsync(l => l.IdArticuloSucursal == articuloSucursal.IdArticuloSucursal && l.Lote == linea.Lote, ct);

            if (lote is null)
            {
                lote = new ArticuloSucursalLote
                {
                    IdArticuloSucursal = articuloSucursal.IdArticuloSucursal,
                    Lote = linea.Lote,
                    FechaVencimiento = linea.FechaVencimiento,
                    CostoUnitario = linea.CostoUnitario
                };
                db.ArticuloSucursalLotes.Add(lote);
                await db.SaveChangesAsync(ct);
            }

            lote.Existencia += linea.Cantidad;
            lote.CostoUnitario = linea.CostoUnitario;
            await db.SaveChangesAsync(ct);

            compra.Detalles.Add(new DetalleCompra
            {
                IdArticulo = linea.IdArticulo,
                IdArticuloSucursalLote = lote.IdArticuloSucursalLote,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Subtotal = linea.Cantidad * linea.CostoUnitario
            });

            db.MovimientosInventario.Add(new MovimientoInventario
            {
                IdSucursal = idSucursal,
                IdArticuloSucursalLote = lote.IdArticuloSucursalLote,
                FechaMovimiento = DateTime.Now,
                TipoMovimiento = "ENTRADA",
                Cantidad = linea.Cantidad,
                Documento = numeroDocumento,
                Observacion = observaciones,
                IdUsuario = idUsuario
            });
        }

        compra.Total = compra.Detalles.Sum(d => d.Subtotal);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return compra;
    }

    public async Task<Venta> RegistrarDevolucionAsync(int idVenta, int idUsuario, IReadOnlyList<int> idsDetalleVenta, IReadOnlyList<int> cantidades, CancellationToken ct = default)
    {
        if (idsDetalleVenta.Count == 0 || idsDetalleVenta.Count != cantidades.Count)
        {
            throw new InvalidOperationException("Seleccione al menos un artículo a devolver.");
        }

        var jornada = await ObtenerJornadaAbiertaAsync(idUsuario, ct)
            ?? throw new InvalidOperationException("Debe abrir una jornada antes de registrar devoluciones.");

        var original = await db.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.IdVenta == idVenta && v.TipoVenta == "VENTA", ct)
            ?? throw new InvalidOperationException("La venta no existe.");

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var tipoDocumentoDevolucion = await db.TiposDocumento
            .FirstOrDefaultAsync(t => t.Codigo == "DV", ct)
            ?? throw new InvalidOperationException("No existe el tipo de documento DV (Devolución Venta).");

        var devolucion = new Venta
        {
            TipoVenta = "DEVOLUCION",
            IdVentaOrigen = original.IdVenta,
            IdTipoDocumento = tipoDocumentoDevolucion.IdTipoDocumento,
            IdCliente = original.IdCliente,
            IdSucursal = original.IdSucursal,
            IdUsuario = idUsuario,
            IdJornada = jornada.IdJornada,
            NumeroDocumento = GenerarNumero("DV"),
            FechaDocumento = DateTime.Now
        };
        db.Ventas.Add(devolucion);

        for (var i = 0; i < idsDetalleVenta.Count; i++)
        {
            var detalle = original.Detalles.FirstOrDefault(d => d.IdDetalleVenta == idsDetalleVenta[i])
                ?? throw new InvalidOperationException("El detalle indicado no pertenece a la venta.");

            var yaDevuelto = await db.Ventas
                .Where(v => v.TipoVenta == "DEVOLUCION")
                .SelectMany(v => v.Detalles)
                .Where(d => d.IdDetalleVentaOrigen == detalle.IdDetalleVenta)
                .SumAsync(d => (int?)d.Cantidad, ct) ?? 0;

            var cantidad = cantidades[i];
            if (cantidad <= 0 || cantidad > detalle.Cantidad - yaDevuelto)
            {
                throw new InvalidOperationException($"Cantidad a devolver no válida para el artículo {detalle.IdArticulo}.");
            }

            var lote = await db.ArticuloSucursalLotes.FirstOrDefaultAsync(l => l.IdArticuloSucursalLote == detalle.IdArticuloSucursalLote, ct)
                ?? throw new InvalidOperationException("El lote de la venta ya no existe.");

            lote.Existencia += cantidad;

            devolucion.Detalles.Add(new DetalleVenta
            {
                IdArticulo = detalle.IdArticulo,
                IdArticuloSucursalLote = detalle.IdArticuloSucursalLote,
                IdDetalleVentaOrigen = detalle.IdDetalleVenta,
                Cantidad = cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Subtotal = cantidad * detalle.PrecioUnitario
            });

            db.MovimientosInventario.Add(new MovimientoInventario
            {
                IdSucursal = original.IdSucursal,
                IdArticuloSucursalLote = detalle.IdArticuloSucursalLote,
                FechaMovimiento = DateTime.Now,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidad,
                Documento = devolucion.NumeroDocumento,
                Observacion = $"Devolución de {original.NumeroDocumento}",
                IdUsuario = idUsuario
            });
        }

        devolucion.Total = devolucion.Detalles.Sum(d => d.Subtotal);

        var proporcionEfectivo = original.Total == 0 ? 0 : original.MontoEfectivo / original.Total;
        devolucion.MontoEfectivo = Math.Round(devolucion.Total * proporcionEfectivo, 2);
        devolucion.MontoTarjeta = devolucion.Total - devolucion.MontoEfectivo;

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return devolucion;
    }

    private static string GenerarNumero(string prefijo) => $"{prefijo}-{DateTime.Now:yyyyMMddHHmmssfff}";
}
