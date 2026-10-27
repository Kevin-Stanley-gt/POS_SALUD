using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Catalogos;

[Authorize]
public class LaboratoriosModel(AppDbContext db) : PageModel
{
    public List<Laboratorio> Items { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;
    }

    public async Task OnGetAsync() => Items = await db.Laboratorios.OrderBy(l => l.Nombre).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var nombre = Input.Nombre.Trim();
        if (await db.Laboratorios.AnyAsync(l => l.Nombre == nombre))
        {
            ModelState.AddModelError("Input.Nombre", "El laboratorio ya existe.");
            await OnGetAsync();
            return Page();
        }

        db.Laboratorios.Add(new Laboratorio
        {
            Nombre = nombre,
            Activo = true,
            FechaCreacion = DateTime.Now,
            IdUsuarioCreacion = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
        });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var item = await db.Laboratorios.FindAsync(id);
        if (item is not null)
        {
            item.Activo = !item.Activo;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
