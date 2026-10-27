using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Inventario;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<ArticuloSucursalLote> Lotes { get; set; } = new();

    public async Task OnGetAsync()
    {
        var idSucursal = int.Parse(User.FindFirst("IdSucursal")?.Value ?? "0");

        Lotes = await db.ArticuloSucursalLotes
            .Include(l => l.ArticuloSucursal!)
                .ThenInclude(x => x.Articulo)
            .Where(l => l.ArticuloSucursal!.IdSucursal == idSucursal)
            .OrderBy(l => l.FechaVencimiento)
            .ToListAsync();
    }
}
