using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;

namespace POS_SALUD.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public int TotalArticulos { get; set; }
    public int LotesPorVencer { get; set; }
    public int ArticulosBajoMinimo { get; set; }

    public async Task OnGetAsync()
    {
        var limite = DateOnly.FromDateTime(DateTime.Today.AddDays(60));

        TotalArticulos = await db.Articulos.CountAsync(a => a.Activo);
        LotesPorVencer = await db.ArticuloSucursalLotes
            .CountAsync(l => l.Existencia > 0 && l.FechaVencimiento <= limite);
        ArticulosBajoMinimo = await db.ArticuloSucursal
            .CountAsync(x => x.Activo && x.ExistenciaTotal <= x.StockMinimo);
    }
}
