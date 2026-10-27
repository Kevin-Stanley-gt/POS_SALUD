using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Articulos;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<Articulo> Articulos { get; set; } = new();
    public List<Generico> Genericos { get; set; } = new();
    public List<Laboratorio> Laboratorios { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int IdGenerico { get; set; }

        [Range(1, int.MaxValue)]
        public int IdLaboratorio { get; set; }

        public bool RequiereReceta { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrecioBase { get; set; }
    }

    public async Task OnGetAsync()
    {
        await CargarAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarAsync();
            return Page();
        }

        var idUsuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        db.Articulos.Add(new Articulo
        {
            CodigoBarras = string.IsNullOrWhiteSpace(Input.CodigoBarras) ? null : Input.CodigoBarras.Trim(),
            Nombre = Input.Nombre.Trim(),
            IdGenerico = Input.IdGenerico,
            IdLaboratorio = Input.IdLaboratorio,
            RequiereReceta = Input.RequiereReceta,
            PrecioBase = Input.PrecioBase,
            Activo = true,
            FechaCreacion = DateTime.Now,
            IdUsuarioCreacion = idUsuario
        });

        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        Articulos = await db.Articulos
            .Include(a => a.Generico)
            .Include(a => a.Laboratorio)
            .OrderBy(a => a.Nombre)
            .ToListAsync();
        Genericos = await db.Genericos.Where(g => g.Activo).OrderBy(g => g.Nombre).ToListAsync();
        Laboratorios = await db.Laboratorios.Where(l => l.Activo).OrderBy(l => l.Nombre).ToListAsync();
    }
}
