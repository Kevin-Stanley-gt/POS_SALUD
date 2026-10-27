using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using POS_SALUD.Data;
using POS_SALUD.Data.Entities;
using POS_SALUD.Services;

namespace POS_SALUD.Pages.Ventas;

[Authorize]
public class NuevaModel(AppDbContext db, PosService pos) : PageModel
{
    public const string OperacionVenta = "VENTA";
    public const string OperacionDevolucion = "DEVOLUCION";

    public List<TipoDocumento> TiposDocumento { get; set; } = new();
    public List<Articulo> ArticulosEncontrados { get; set; } = new();
    public List<Cliente> ClientesEncontrados { get; set; } = new();
    public Cliente? ClienteSeleccionado { get; set; }
    public List<Venta> VentasCliente { get; set; } = new();
    public Venta? VentaOrigenSeleccionada { get; set; }
    public Dictionary<int, int> CantidadDisponible { get; set; } = new();
    public string? MensajeError { get; set; }
    public Venta? VentaRegistrada { get; set; }
    public POS_SALUD.Data.Entities.Jornada? JornadaActiva { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string TipoOperacion { get; set; } = OperacionVenta;
        public int IdTipoDocumento { get; set; }
        public string? BusquedaCliente { get; set; }
        public string? BusquedaArticulo { get; set; }
        public int? IdCliente { get; set; }
        public int? IdVentaOrigen { get; set; }
        public decimal MontoEfectivo { get; set; }
        public decimal MontoTarjeta { get; set; }
        public List<LineaInput> Lineas { get; set; } = new();
    }

    public class LineaInput
    {
        public int IdArticulo { get; set; }
        public int? IdDetalleVenta { get; set; }
        public string NombreArticulo { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        await CargarAsync();
    }

    public async Task<IActionResult> OnPostCambiarTipoAsync()
    {
        await CargarAsync();
        Input.Lineas = new();
        Input.IdCliente = null;
        Input.IdVentaOrigen = null;
        ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.IdCliente)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.IdVentaOrigen)}");
        Input.BusquedaCliente = null;
        Input.BusquedaArticulo = null;
        ClienteSeleccionado = null;
        VentasCliente = new();
        VentaOrigenSeleccionada = null;
        CantidadDisponible = new();
        return Page();
    }

    public async Task<IActionResult> OnPostBuscarClienteAsync()
    {
        await CargarAsync();
        var texto = (Input.BusquedaCliente ?? string.Empty).Trim();
        if (texto.Length == 0)
        {
            MensajeError = "Ingrese un nombre para buscar el cliente.";
            return Page();
        }

        ClientesEncontrados = await db.Clientes.AsNoTracking()
            .Where(c => c.Activo && c.Nombre.Contains(texto))
            .OrderBy(c => c.Nombre)
            .Take(20)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSeleccionarClienteAsync(int idCliente)
    {
        await CargarAsync();
        Input.IdCliente = idCliente;
        Input.IdVentaOrigen = null;
        Input.Lineas = new();
        ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.IdCliente)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.IdVentaOrigen)}");
        await CargarClienteYVentasAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSeleccionarVentaAsync(int idVenta)
    {
        Input.IdVentaOrigen = idVenta;
        await CargarAsync();
        ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.IdVentaOrigen)}");
        return Page();
    }

    public async Task<IActionResult> OnPostBuscarArticuloAsync()
    {
        await CargarAsync();
        if (Input.TipoOperacion == OperacionDevolucion)
        {
            return Page();
        }

        var texto = (Input.BusquedaArticulo ?? string.Empty).Trim();
        if (texto.Length == 0)
        {
            MensajeError = "Ingrese un nombre para buscar el artículo.";
            return Page();
        }

        ArticulosEncontrados = await db.Articulos.AsNoTracking()
            .Where(a => a.Activo && a.Nombre.Contains(texto))
            .OrderBy(a => a.Nombre)
            .Take(20)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAgregarArticuloAsync(int idArticulo)
    {
        await CargarAsync();
        if (Input.TipoOperacion == OperacionDevolucion)
        {
            return Page();
        }

        var articulo = await db.Articulos.AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdArticulo == idArticulo && a.Activo);
        if (articulo is null)
        {
            MensajeError = "El artículo no existe o está inactivo.";
            return Page();
        }

        var existente = Input.Lineas.FirstOrDefault(l => l.IdArticulo == idArticulo);
        if (existente is not null)
        {
            existente.Cantidad++;
        }
        else
        {
            Input.Lineas.Add(new LineaInput
            {
                IdArticulo = articulo.IdArticulo,
                NombreArticulo = articulo.Nombre,
                PrecioUnitario = articulo.PrecioBase,
                Cantidad = 1
            });
        }
        Input.BusquedaArticulo = null;
        ArticulosEncontrados = new();
        return Page();
    }

    public async Task<IActionResult> OnPostModificarCantidadAsync([FromForm(Name = "idArticuloModal")] int idArticulo, int cantidadNueva)
    {
        await CargarAsync();
        if (Input.TipoOperacion == OperacionDevolucion)
        {
            return Page();
        }

        if (cantidadNueva < 1)
        {
            MensajeError = "La cantidad debe ser mayor que cero.";
            return Page();
        }

        var linea = Input.Lineas.FirstOrDefault(l => l.IdArticulo == idArticulo);
        if (linea is not null)
        {
            linea.Cantidad = cantidadNueva;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostQuitarArticuloAsync(int idArticulo)
    {
        await CargarAsync();
        if (Input.TipoOperacion == OperacionDevolucion)
        {
            return Page();
        }

        Input.Lineas.RemoveAll(l => l.IdArticulo == idArticulo);
        return Page();
    }

    public async Task<IActionResult> OnPostRegistrarAsync()
    {
        await CargarAsync();
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var idSucursal = int.Parse(User.FindFirstValue("IdSucursal") ?? "0");

        try
        {
            if (Input.TipoOperacion == OperacionDevolucion)
            {
                await RegistrarDevolucionAsync(idUsuario);
            }
            else
            {
                await RegistrarVentaNormalAsync(idSucursal, idUsuario);
            }
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
        }

        return Page();
    }

    private async Task RegistrarVentaNormalAsync(int idSucursal, int idUsuario)
    {
        var lineas = Input.Lineas
            .Where(l => l.Cantidad > 0)
            .Select(l => new LineaVenta(l.IdArticulo, l.Cantidad))
            .ToList();
        if (lineas.Count == 0)
        {
            MensajeError = "Agregue al menos un artículo.";
            return;
        }

        VentaRegistrada = await pos.RegistrarVentaAsync(idSucursal, idUsuario, Input.IdTipoDocumento, Input.IdCliente, lineas, Input.MontoEfectivo, Input.MontoTarjeta);
        LimpiarFormulario();
    }

    private async Task RegistrarDevolucionAsync(int idUsuario)
    {
        if (Input.IdVentaOrigen is null || Input.IdCliente is null)
        {
            MensajeError = "Seleccione el cliente y la venta a devolver.";
            return;
        }

        await CargarVentaOrigenAsync(Input.IdVentaOrigen.Value);
        var disponibles = CantidadDisponible.Where(kv => kv.Value > 0).ToList();
        if (disponibles.Count == 0)
        {
            MensajeError = "La venta seleccionada ya no tiene artículos disponibles para devolver.";
            return;
        }

        VentaRegistrada = await pos.RegistrarDevolucionAsync(
            Input.IdVentaOrigen.Value,
            idUsuario,
            disponibles.Select(kv => kv.Key).ToList(),
            disponibles.Select(kv => kv.Value).ToList());
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        ModelState.Clear();
        Input = new InputModel { TipoOperacion = Input.TipoOperacion, IdTipoDocumento = Input.IdTipoDocumento };
        ClienteSeleccionado = null;
        VentasCliente = new();
        VentaOrigenSeleccionada = null;
        CantidadDisponible = new();
    }

    private async Task CargarAsync()
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        JornadaActiva = await pos.ObtenerJornadaAbiertaAsync(idUsuario);

        var codigo = Input.TipoOperacion == OperacionDevolucion ? "DV" : "FV";
        TiposDocumento = await db.TiposDocumento.Where(t => t.Codigo == "FV" || t.Codigo == "DV").ToListAsync();
        if (!TiposDocumento.Any(t => t.IdTipoDocumento == Input.IdTipoDocumento && t.Codigo == codigo))
        {
            Input.IdTipoDocumento = TiposDocumento.First(t => t.Codigo == codigo).IdTipoDocumento;
        }

        await CargarClienteYVentasAsync();
        if (Input.TipoOperacion == OperacionDevolucion && Input.IdVentaOrigen is int idVentaOrigen)
        {
            await CargarVentaOrigenAsync(idVentaOrigen);
        }
    }

    private async Task CargarClienteYVentasAsync()
    {
        if (Input.IdCliente is null)
        {
            return;
        }

        ClienteSeleccionado = await db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCliente == Input.IdCliente);

        if (Input.TipoOperacion != OperacionDevolucion)
        {
            return;
        }

        VentasCliente = await db.Ventas.AsNoTracking()
            .Where(v => v.IdCliente == Input.IdCliente && v.TipoVenta == OperacionVenta)
            .OrderByDescending(v => v.FechaDocumento)
            .Take(50)
            .ToListAsync();
    }

    private async Task CargarVentaOrigenAsync(int idVenta)
    {
        VentaOrigenSeleccionada = await db.Ventas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.IdVenta == idVenta && v.TipoVenta == OperacionVenta);
        if (VentaOrigenSeleccionada is null)
        {
            return;
        }

        Input.IdVentaOrigen = idVenta;
        var detalles = await db.DetalleVenta.AsNoTracking()
            .Include(d => d.Articulo)
            .Where(d => d.IdVenta == idVenta)
            .ToListAsync();

        Input.Lineas = detalles.Select(d => new LineaInput
        {
            IdArticulo = d.IdArticulo,
            IdDetalleVenta = d.IdDetalleVenta,
            NombreArticulo = d.Articulo?.Nombre ?? string.Empty,
            PrecioUnitario = d.PrecioUnitario,
            Cantidad = d.Cantidad
        }).ToList();

        var devueltos = await db.DetalleVenta.AsNoTracking()
            .Where(d => d.Venta!.IdVentaOrigen == idVenta && d.IdDetalleVentaOrigen != null)
            .GroupBy(d => d.IdDetalleVentaOrigen!.Value)
            .Select(g => new { Id = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToListAsync();

        CantidadDisponible = detalles.ToDictionary(
            d => d.IdDetalleVenta,
            d => d.Cantidad - (devueltos.FirstOrDefault(x => x.Id == d.IdDetalleVenta)?.Cantidad ?? 0));
    }
}
