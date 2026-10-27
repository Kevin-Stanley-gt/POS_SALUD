using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Traslados;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<Traslado> Traslados { get; set; } = new();
    public List<Sucursal> Sucursales { get; set; } = new();
    public List<ArticuloSucursalLote> LotesDisponibles { get; set; } = new();
    public string? MensajeError { get; set; }
    public string? MensajeOk { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Range(1, int.MaxValue)]
        public int IdSucursalDestino { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public List<LineaInput> Lineas { get; set; } = new();
    }

    public class LineaInput
    {
        public int? IdArticuloSucursalLote { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        await CargarAsync();
        Input.Lineas = Enumerable.Range(0, 5).Select(_ => new LineaInput()).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarAsync();

        var idSucursal = SucursalActual();
        var idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var lineas = Input.Lineas.Where(l => l.IdArticuloSucursalLote.HasValue && l.Cantidad > 0).ToList();

        if (Input.IdSucursalDestino == idSucursal)
        {
            MensajeError = "La sucursal destino debe ser diferente a la sucursal de origen.";
            return Page();
        }
        if (lineas.Count == 0)
        {
            MensajeError = "Agregue al menos una línea de traslado.";
            return Page();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync();
        try
        {
            var traslado = new Traslado
            {
                IdSucursalOrigen = idSucursal,
                IdSucursalDestino = Input.IdSucursalDestino,
                IdUsuarioEnvia = idUsuario,
                FechaEnvio = DateTime.Now,
                Estado = "EN_TRANSITO",
                Observaciones = Input.Observaciones
            };
            db.Traslados.Add(traslado);

            foreach (var linea in lineas)
            {
                var lote = await db.ArticuloSucursalLotes
                    .Include(l => l.ArticuloSucursal)
                    .FirstOrDefaultAsync(l => l.IdArticuloSucursalLote == linea.IdArticuloSucursalLote!.Value)
                    ?? throw new InvalidOperationException("Lote no encontrado.");

                if (lote.ArticuloSucursal!.IdSucursal != idSucursal)
                {
                    throw new InvalidOperationException("El lote seleccionado no pertenece a su sucursal.");
                }
                if (lote.Existencia < linea.Cantidad)
                {
                    throw new InvalidOperationException($"Existencia insuficiente en lote {lote.Lote}.");
                }

                lote.Existencia -= linea.Cantidad;

                traslado.Detalles.Add(new DetalleTraslado
                {
                    IdArticuloSucursalLoteOrigen = lote.IdArticuloSucursalLote,
                    CantidadEnviada = linea.Cantidad
                });

                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdSucursal = idSucursal,
                    IdArticuloSucursalLote = lote.IdArticuloSucursalLote,
                    FechaMovimiento = DateTime.Now,
                    TipoMovimiento = "SALIDA",
                    Cantidad = linea.Cantidad,
                    Documento = $"TR-{traslado.IdTraslado}",
                    Observacion = "Traslado enviado",
                    IdUsuario = idUsuario
                });
            }

            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            MensajeOk = "Traslado enviado correctamente.";
            Input = new InputModel { Lineas = Enumerable.Range(0, 5).Select(_ => new LineaInput()).ToList() };
        }
        catch (InvalidOperationException ex)
        {
            await transaccion.RollbackAsync();
            MensajeError = ex.Message;
        }

        await CargarAsync();
        return Page();
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
            await CargarAsync();
            return Page();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync();

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
                    IdArticulo = loteOrigen.ArticuloSucursal!.IdArticulo
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
        MensajeOk = "Traslado recibido correctamente.";
        await CargarAsync();
        return Page();
    }

    private int SucursalActual() => int.Parse(User.FindFirst("IdSucursal")?.Value ?? "0");

    private async Task CargarAsync()
    {
        var idSucursal = SucursalActual();
        Sucursales = await db.Sucursales.Where(s => s.Activo && s.IdSucursal != idSucursal).OrderBy(s => s.Nombre).ToListAsync();
        LotesDisponibles = await db.ArticuloSucursalLotes
            .Include(l => l.ArticuloSucursal!)
                .ThenInclude(x => x.Articulo)
            .Where(l => l.ArticuloSucursal!.IdSucursal == idSucursal && l.Existencia > 0)
            .OrderBy(l => l.FechaVencimiento)
            .ToListAsync();
        Traslados = await db.Traslados
            .Include(t => t.SucursalOrigen)
            .Include(t => t.SucursalDestino)
            .Where(t => t.IdSucursalOrigen == idSucursal || t.IdSucursalDestino == idSucursal)
            .OrderByDescending(t => t.FechaEnvio)
            .Take(50)
            .ToListAsync();
    }
}
