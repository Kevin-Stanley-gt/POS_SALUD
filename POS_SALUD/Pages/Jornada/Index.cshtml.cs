using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using POS_SALUD.Data.Entities;
using POS_SALUD.Services;

namespace POS_SALUD.Pages.Jornada;

[Authorize]
public class IndexModel(PosService pos) : PageModel
{
    public Data.Entities.Jornada? JornadaActiva { get; set; }
    public string? MensajeError { get; set; }
    public string? MensajeOk { get; set; }

    [BindProperty]
    public AbrirInput Abrir { get; set; } = new();

    [BindProperty]
    public CerrarInput Cerrar { get; set; } = new();

    public class AbrirInput
    {
        [Range(0, double.MaxValue)]
        public decimal FondoInicial { get; set; }
    }

    public class CerrarInput
    {
        [Range(0, double.MaxValue)]
        public decimal EfectivoDeclarado { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TarjetaDeclarada { get; set; }
    }

    public async Task OnGetAsync()
    {
        JornadaActiva = await pos.ObtenerJornadaAbiertaAsync(IdUsuario);
    }

    public async Task<IActionResult> OnPostAbrirAsync()
    {
        try
        {
            await pos.AbrirJornadaAsync(IdUsuario, IdSucursal, Abrir.FondoInicial);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCerrarAsync()
    {
        try
        {
            var cerrada = await pos.CerrarJornadaAsync(IdUsuario, Cerrar.EfectivoDeclarado, Cerrar.TarjetaDeclarada);
            TempData["Resumen"] = $"Jornada cerrada. Diferencia efectivo: {cerrada.DiferenciaEfectivo:0.00}; tarjeta: {cerrada.DiferenciaTarjeta:0.00}.";
            return RedirectToPage("Corte", new { id = cerrada.IdJornada });
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    private int IdUsuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private int IdSucursal => int.Parse(User.FindFirstValue("IdSucursal") ?? "0");
}
