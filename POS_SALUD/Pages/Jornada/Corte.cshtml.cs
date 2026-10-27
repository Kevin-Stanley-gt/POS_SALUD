using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Jornada;

[Authorize]
public class CorteModel(AppDbContext db) : PageModel
{
    public Data.Entities.Jornada? Jornada { get; set; }
    public string NombreSucursal { get; set; } = "POS Farma";
    public int NumeroVentas { get; set; }
    public int NumeroDevoluciones { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalDevoluciones { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Jornada = await db.Jornadas
            .Include(j => j.Sucursal)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.IdJornada == id);

        if (Jornada is null || Jornada.Estado != "CERRADA")
        {
            return NotFound();
        }

        NombreSucursal = Jornada.Sucursal?.Nombre ?? NombreSucursal;

        var movimientos = await db.Ventas
            .AsNoTracking()
            .Where(v => v.IdJornada == id)
            .ToListAsync();

        var ventas = movimientos.Where(v => v.TipoVenta == "VENTA").ToList();
        var devoluciones = movimientos.Where(v => v.TipoVenta == "DEVOLUCION").ToList();

        NumeroVentas = ventas.Count;
        NumeroDevoluciones = devoluciones.Count;
        TotalVentas = ventas.Sum(v => v.Total);
        TotalDevoluciones = devoluciones.Sum(v => v.Total);

        ViewData["Title"] = $"Corte de caja {Jornada.IdJornada}";
        return Page();
    }
}
