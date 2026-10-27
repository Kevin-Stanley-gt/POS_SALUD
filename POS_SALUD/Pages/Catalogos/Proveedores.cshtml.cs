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
public class ProveedoresModel(AppDbContext db) : PageModel
{
    public List<Proveedor> Items { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(25)]
        public string Nit { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }
    }

    public async Task OnGetAsync() => Items = await db.Proveedores.OrderBy(p => p.RazonSocial).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var nit = Input.Nit.Trim();
        if (await db.Proveedores.AnyAsync(p => p.Nit == nit))
        {
            ModelState.AddModelError("Input.Nit", "Ya existe un proveedor con ese NIT.");
            await OnGetAsync();
            return Page();
        }

        db.Proveedores.Add(new Proveedor
        {
            Nit = nit,
            RazonSocial = Input.RazonSocial.Trim(),
            Telefono = string.IsNullOrWhiteSpace(Input.Telefono) ? null : Input.Telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email.Trim(),
            Activo = true,
            FechaCreacion = DateTime.Now,
            IdUsuarioCreacion = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
        });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var item = await db.Proveedores.FindAsync(id);
        if (item is not null)
        {
            item.Activo = !item.Activo;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
