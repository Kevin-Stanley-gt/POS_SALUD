using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;
using POS_SALUD.Services;

namespace POS_SALUD.Pages.Compras;

[Authorize]
public class NuevaModel(AppDbContext db, PosService pos) : PageModel
{
    public List<Articulo> Articulos { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();
    public List<TipoDocumento> TiposDocumento { get; set; } = new();
    public string? MensajeError { get; set; }
    public Compra? CompraRegistrada { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Range(1, int.MaxValue)]
        public int IdTipoDocumento { get; set; }

        [Range(1, int.MaxValue)]
        public int IdProveedor { get; set; }

        [Required, StringLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public List<LineaInput> Lineas { get; set; } = new();
    }

    public class LineaInput
    {
        public int? IdArticulo { get; set; }

        [StringLength(50)]
        public string? Lote { get; set; }

        public DateOnly? FechaVencimiento { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CostoUnitario { get; set; }

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

        if (!ModelState.IsValid)
        {
            MensajeError = "Revise los datos ingresados. El número de documento es obligatorio y los demás campos deben ser válidos.";
            return Page();
        }

        var lineas = Input.Lineas
            .Where(l => l.IdArticulo.HasValue && l.FechaVencimiento.HasValue && !string.IsNullOrWhiteSpace(l.Lote) && l.Cantidad > 0)
            .Select(l => new LineaCompra(l.IdArticulo!.Value, l.Lote!.Trim(), l.FechaVencimiento!.Value, l.CostoUnitario, l.Cantidad))
            .ToList();

        if (lineas.Count == 0)
        {
            MensajeError = "Agregue al menos una línea completa (artículo, lote, vencimiento, cantidad).";
            return Page();
        }

        var idSucursal = int.Parse(User.FindFirst("IdSucursal")?.Value ?? "0");
        var idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        try
        {
            CompraRegistrada = await pos.RegistrarCompraAsync(Input.IdTipoDocumento, Input.IdProveedor, idSucursal, idUsuario, Input.NumeroDocumento.Trim(), Input.Observaciones, lineas);
            ModelState.Clear();
            Input = new InputModel { Lineas = Enumerable.Range(0, 5).Select(_ => new LineaInput()).ToList() };
        }
        catch (DbUpdateException)
        {
            MensajeError = "No se pudo registrar la compra. Verifique que el número de documento y los datos sean válidos.";
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
        }

        return Page();
    }

    private async Task CargarAsync()
    {
        Articulos = await db.Articulos.Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync();
        Proveedores = await db.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync();
        TiposDocumento = await db.TiposDocumento.Where(t => t.Codigo == "FC" || t.Codigo == "DC").ToListAsync();
    }
}
