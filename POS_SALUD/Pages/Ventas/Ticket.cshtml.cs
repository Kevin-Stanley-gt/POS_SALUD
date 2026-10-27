using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Ventas;

[Authorize]
public class TicketModel(AppDbContext db) : PageModel
{
    public Venta? Venta { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Venta = await db.Ventas
            .Include(v => v.Sucursal)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Articulo)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.IdVenta == id);

        if (Venta is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Ticket {Venta.NumeroDocumento}";
        return Page();
    }
}
