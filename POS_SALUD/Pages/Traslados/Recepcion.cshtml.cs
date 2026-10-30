using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Traslados;

[Authorize]
public class RecepcionModel(AppDbContext db) : PageModel
{
    public List<TrasladoItemViewModel> TrasladosPendientes { get; set; } = new();

    [TempData]
    public string? MensajeOk { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public class DetalleItemViewModel
    {
        public string Articulo { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Vencimiento { get; set; } = string.Empty;
        public int CantidadEnviada { get; set; }
    }

    public class TrasladoItemViewModel
    {
        public int IdTraslado { get; set; }
        public string SucursalOrigen { get; set; } = string.Empty;
        public string UsuarioEnvia { get; set; } = string.Empty;
        public DateTime? FechaEnvio { get; set; }
        public string? Observaciones { get; set; }
        public List<DetalleItemViewModel> Detalles { get; set; } = new();
    }

    private int SucursalActual() => int.Parse(User.FindFirst("IdSucursal")?.Value ?? "0");

    public async Task OnGetAsync()
    {
        await CargarPendientesAsync();
    }

    private async Task CargarPendientesAsync()
    {
        var idSucursal = SucursalActual();

        // 1. Obtener traslados en tránsito hacia mi sucursal con sus detalles
        var traslados = await db.Traslados
            .Include(t => t.SucursalOrigen)
            .Include(t => t.Detalles)
            .Where(t => t.IdSucursalDestino == idSucursal && t.Estado == "EN_TRANSITO")
            .OrderByDescending(t => t.FechaEnvio)
            .ToListAsync();

        // 2. Extraer los IDs de lotes para consultar sus datos en bloque
        var idsLotesOrigen = traslados
            .SelectMany(t => t.Detalles)
            .Select(d => d.IdArticuloSucursalLoteOrigen)
            .Distinct()
            .ToList();

        var lotesDiccionario = await db.ArticuloSucursalLotes
            .Include(l => l.ArticuloSucursal!)
                .ThenInclude(asuc => asuc.Articulo)
            .Where(l => idsLotesOrigen.Contains(l.IdArticuloSucursalLote))
            .ToDictionaryAsync(l => l.IdArticuloSucursalLote);

        // 3. Mapear a la vista
        TrasladosPendientes = traslados.Select(t => new TrasladoItemViewModel
        {
            IdTraslado = t.IdTraslado,
            SucursalOrigen = t.SucursalOrigen?.Nombre ?? "Desconocida",
            UsuarioEnvia = $"Usuario #{t.IdUsuarioEnvia}",
            FechaEnvio = t.FechaEnvio,
            Observaciones = t.Observaciones,
            Detalles = t.Detalles.Select(d =>
            {
                lotesDiccionario.TryGetValue(d.IdArticuloSucursalLoteOrigen, out var lote);
                return new DetalleItemViewModel
                {
                    Articulo = lote?.ArticuloSucursal?.Articulo?.Nombre ?? "Artículo",
                    Lote = lote?.Lote ?? "S/L",
                    Vencimiento = lote?.FechaVencimiento.ToString("dd/MM/yyyy") ?? "—",
                    CantidadEnviada = d.CantidadEnviada
                };
            }).ToList()
        }).ToList();
    }

    public async Task<IActionResult> OnPostRecibirAsync(int id)
    {
        var idSucursal = SucursalActual();
        var idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var traslado = await db.Traslados
            .Include(t => t.Detalles)
            .FirstOrDefaultAsync(t => t.IdTraslado == id && t.IdSucursalDestino == idSucursal);

        if (traslado is null || traslado.Estado != "EN_TRANSITO")
        {
            MensajeError = "El traslado no existe o ya fue procesado.";
            return RedirectToPage();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var detalle in traslado.Detalles)
            {
                var loteOrigen = await db.ArticuloSucursalLotes
                    .Include(l => l.ArticuloSucursal)
                    .FirstAsync(l => l.IdArticuloSucursalLote == detalle.IdArticuloSucursalLoteOrigen);

                var articuloSucursalDestino = await db.ArticuloSucursal
                    .FirstOrDefaultAsync(x => x.IdSucursal == idSucursal && x.IdArticulo == loteOrigen.ArticuloSucursal!.IdArticulo);

                if (articuloSucursalDestino is null)
                {
                    articuloSucursalDestino = new ArticuloSucursal
                    {
                        IdSucursal = idSucursal,
                        IdArticulo = loteOrigen.ArticuloSucursal!.IdArticulo,
                        StockMinimo = 0,
                        Activo = true
                    };
                    db.ArticuloSucursal.Add(articuloSucursalDestino);
                    await db.SaveChangesAsync();
                }

                var loteDestino = await db.ArticuloSucursalLotes
                    .FirstOrDefaultAsync(l => l.IdArticuloSucursal == articuloSucursalDestino.IdArticuloSucursal && l.Lote == loteOrigen.Lote);

                if (loteDestino is null)
                {
                    loteDestino = new ArticuloSucursalLote
                    {
                        IdArticuloSucursal = articuloSucursalDestino.IdArticuloSucursal,
                        Lote = loteOrigen.Lote,
                        FechaVencimiento = loteOrigen.FechaVencimiento,
                        CostoUnitario = loteOrigen.CostoUnitario
                    };
                    db.ArticuloSucursalLotes.Add(loteDestino);
                    await db.SaveChangesAsync();
                }

                loteDestino.Existencia += detalle.CantidadEnviada;
                detalle.CantidadRecibida = detalle.CantidadEnviada;

                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdSucursal = idSucursal,
                    IdArticuloSucursalLote = loteDestino.IdArticuloSucursalLote,
                    FechaMovimiento = DateTime.Now,
                    TipoMovimiento = "ENTRADA",
                    Cantidad = detalle.CantidadEnviada,
                    Documento = $"TR-{traslado.IdTraslado}",
                    Observacion = "Traslado recibido",
                    IdUsuario = idUsuario
                });
            }

            traslado.Estado = "RECIBIDO";
            traslado.IdUsuarioRecibe = idUsuario;
            traslado.FechaRecepcion = DateTime.Now;

            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            MensajeOk = $"El traslado #{traslado.IdTraslado} ha sido recibido y el inventario fue actualizado.";
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync();
            MensajeError = $"Error al procesar la recepción: {ex.Message}";
        }

        return RedirectToPage();
    }
}