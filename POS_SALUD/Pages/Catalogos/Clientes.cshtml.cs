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
public class ClientesModel(AppDbContext db) : PageModel
{
    public List<Cliente> Items { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [StringLength(25)]
        public string? Nit { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Direccion { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }
    }

    public async Task OnGetAsync() => Items = await db.Clientes.OrderBy(c => c.Nombre).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        db.Clientes.Add(new Cliente
        {
            Nit = string.IsNullOrWhiteSpace(Input.Nit) ? null : Input.Nit.Trim(),
            Nombre = Input.Nombre.Trim(),
            Direccion = string.IsNullOrWhiteSpace(Input.Direccion) ? null : Input.Direccion.Trim(),
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
        var item = await db.Clientes.FindAsync(id);
        if (item is not null)
        {
            item.Activo = !item.Activo;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
