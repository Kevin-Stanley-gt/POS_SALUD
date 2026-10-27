using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;

namespace POS_SALUD.Pages.Account;

public class LoginModel(AppDbContext db, IPasswordHasher<Usuario> hasher) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? MensajeError { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == Input.Email && u.Activo);

        if (usuario is null || hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, Input.Password) == PasswordVerificationResult.Failed)
        {
            MensajeError = "Credenciales no válidas.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol?.Nombre ?? string.Empty),
            new("IdSucursal", (usuario.IdSucursalPredeterminada ?? 0).ToString())
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        return RedirectToPage("/Index");
    }
}
