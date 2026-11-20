using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BibliotecaCalidad.E2E;

[TestFixture]
[Category("EC3-Decision")]
[NonParallelizable]
public class PrestamosDecisionTests : PageTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static string FrontUrl => (Environment.GetEnvironmentVariable("BC_BASE_URL") ?? "https://localhost:7001").TrimEnd('/');
    private static string BackendUrl => (Environment.GetEnvironmentVariable("BC_BACKEND_URL") ?? "https://localhost:7118").TrimEnd('/');

    private HttpClient _api = null!;
    private HashSet<int> _prestamosAntes = new();
    private int _lectorSeleccionado;

    private sealed class ResumenPrestamo
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
    }
    private sealed class DetallePrestamo
    {
        public int BorrowerId { get; set; }
        public List<ItemPrestamo> Items { get; set; } = new();
    }
    private sealed class ItemPrestamo
    {
        public int CopyId { get; set; }
        public string Status { get; set; } = "";
    }

    [SetUp]
    public async Task Preparar()
    {
        var usuario = Environment.GetEnvironmentVariable("BC_TEST_USER")
            ?? throw new InvalidOperationException("Configura BC_TEST_USER.");
        var clave = Environment.GetEnvironmentVariable("BC_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Configura BC_TEST_PASSWORD.");

        // Se aceptan certificados de desarrollo solamente para el backend local.
        var uri = new Uri(BackendUrl);
        if (!uri.IsLoopback)
            throw new InvalidOperationException("BC_BACKEND_URL debe apuntar a localhost para estas pruebas.");
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _api = new HttpClient(handler) { BaseAddress = new Uri(BackendUrl + "/") };

        await Page.GotoAsync(FrontUrl + "/Auth/Login");
        await Page.Locator("#Login_Username").FillAsync(usuario);
        await Page.Locator("#Login_Password").FillAsync(clave);
        await Page.Locator("button.auth-submit").ClickAsync();
        await Page.GotoAsync(FrontUrl + "/Loans/Create");
        await Expect(Page.Locator("#readerSearch")).ToBeVisibleAsync();
        await Expect(Page.Locator("#bookSearch")).ToBeVisibleAsync();
        _prestamosAntes = (await ObtenerPrestamos()).Select(x => x.Id).ToHashSet();
    }

    [TearDown]
    public async Task EvidenciaYLimpieza()
    {
        try
        {
            var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "EvidenciasEC3", "Decision");
            Directory.CreateDirectory(dir);
            var ruta = Path.Combine(dir, TestContext.CurrentContext.Test.Name + ".png");
            await Page.ScreenshotAsync(new() { Path = ruta, FullPage = true });
            TestContext.AddTestAttachment(ruta, "Tabla de decision");
            TestContext.Progress.WriteLine("Evidencia: " + ruta);
        }
        catch (Exception ex) { TestContext.Progress.WriteLine("Captura: " + ex.Message); }

        // Si el sistema aceptó un prestamo que debia rechazar, devolver las copias
        // creadas por ESTA prueba para no bloquear los siguientes casos.
        try
        {
            if (_api != null && _lectorSeleccionado > 0)
            {
                foreach (var resumen in await ObtenerPrestamos())
                {
                    if (_prestamosAntes.Contains(resumen.Id)) continue;
                    var detalle = await ObtenerDetalle(resumen.Id);
                    if (detalle.BorrowerId != _lectorSeleccionado) continue;
                    foreach (var item in detalle.Items.Where(i => i.Status is "Active" or "Overdue"))
                        await _api.PostAsync($"api/loans/return/{item.CopyId}?confirm=true", null);
                }
            }
        }
        catch (Exception ex) { TestContext.Progress.WriteLine("Limpieza: " + ex.Message); }
        _api?.Dispose();
    }

    private async Task<List<ResumenPrestamo>> ObtenerPrestamos()
    {
        using var response = await _api.GetAsync("api/loans");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ResumenPrestamo>>(JsonOptions) ?? new();
    }

    private async Task<DetallePrestamo> ObtenerDetalle(int id)
    {
        using var response = await _api.GetAsync($"api/loans/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DetallePrestamo>(JsonOptions)
            ?? throw new InvalidOperationException("No se pudo leer el prestamo " + id);
    }

    private async Task<HashSet<int>> LectoresConPrestamoPendiente()
    {
        var pendientes = (await ObtenerPrestamos())
            .Where(p => p.Status is "Active" or "Overdue").ToList();
        var ids = new HashSet<int>();
        foreach (var p in pendientes)
            ids.Add((await ObtenerDetalle(p.Id)).BorrowerId);
        return ids;
    }

    private async Task SeleccionarLector(bool conPendiente)
    {
        var pendientes = await LectoresConPrestamoPendiente();
        var lectores = Page.Locator(".reader-radio");
        for (var i = 0; i < await lectores.CountAsync(); i++)
        {
            var radio = lectores.Nth(i);
            if (!int.TryParse(await radio.GetAttributeAsync("value"), out var id)) continue;
            if (pendientes.Contains(id) != conPendiente) continue;
            _lectorSeleccionado = id;
            await radio.CheckAsync();
            return;
        }
        Assert.Ignore(conPendiente
            ? "Falta un lector con prestamo Activo/Vencido para probar esta regla. Preparalo en la BD de pruebas."
            : "Falta un lector sin prestamos Activos/Vencidos para probar esta regla.");
    }

    private async Task SeleccionarLibros(bool disponibles, int cantidad)
    {
        var selector = disponibles
            ? ".book-option:not(:has-text('Sin copias disponibles')) .book-checkbox"
            : ".book-option:has-text('Sin copias disponibles') .book-checkbox";
        var libros = Page.Locator(selector);
        var total = await libros.CountAsync();
        if (total < cantidad)
            Assert.Ignore($"Se necesitan {cantidad} libros {(disponibles ? "con" : "sin")} copias disponibles; hay {total}.");
        for (var i = 0; i < cantidad; i++)
            await libros.Nth(i).CheckAsync();
    }

    private static void AutorizarEscrituras()
    {
        if (Environment.GetEnvironmentVariable("BC_EC3_ALLOW_WRITES") != "1")
            Assert.Ignore("Para pruebas que pueden registrar prestamos, usa una BD de pruebas y configura BC_EC3_ALLOW_WRITES=1.");
    }

    private async Task<string> EnviarYLeerError()
    {
        await Page.Locator("#submitLoanBtn").ClickAsync();
        var error = Page.Locator(".alert-danger");
        if (await error.CountAsync() == 0)
            Assert.Fail("El sistema acepto el prestamo. Se esperaba rechazo segun la tabla de decision del informe.");
        return await error.InnerTextAsync();
    }

    private static void DebeContener(string mensaje, string condicion, string patron)
    {
        Assert.That(Regex.IsMatch(mensaje, patron, RegexOptions.IgnoreCase), Is.True,
            $"No se reporto '{condicion}'. Respuesta real: {mensaje}");
    }

    // R1: copias disponibles + lector sin pendientes + 1 libro => registro.
    [Test]
    public async Task TD01_R1_PrestamoValido_Registrado()
    {
        AutorizarEscrituras();
        await SeleccionarLector(false);
        await SeleccionarLibros(true, 1);
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator("h2.page-title")).ToContainTextAsync("Préstamos");
        Assert.That((await ObtenerPrestamos()).Any(p => !_prestamosAntes.Contains(p.Id)), Is.True,
            "No se encontro un prestamo nuevo en el backend.");
    }

    // R2: copias disponibles + lector sin pendientes + 3 libros => cantidad invalida.
    [Test]
    public async Task TD02_R2_TresLibros_RechazaCantidad()
    {
        await SeleccionarLector(false);
        await SeleccionarLibros(true, 3);
        DebeContener(await EnviarYLeerError(), "cantidad", "más de 2|cantidad");
    }

    // R3: copias disponibles + lector CON pendientes + 1 libro => lector invalido.
    [Test]
    public async Task TD03_R3_LectorConPendiente_Rechazado()
    {
        AutorizarEscrituras(); // Actualmente el backend puede aceptar este caso.
        await SeleccionarLector(true);
        await SeleccionarLibros(true, 1);
        DebeContener(await EnviarYLeerError(), "lector con prestamo pendiente", "lector|pendiente|prestatario");
    }

    // R4: 3 disponibles + lector con pendientes => ambos errores segun informe.
    [Test]
    public async Task TD04_R4_PendienteYTresLibros_AmbosErrores()
    {
        await SeleccionarLector(true);
        await SeleccionarLibros(true, 3);
        var error = await EnviarYLeerError();
        DebeContener(error, "cantidad", "más de 2|cantidad");
        DebeContener(error, "lector", "lector|pendiente|prestatario");
    }

    // R5: libro sin copias + lector sin pendientes => libro invalido.
    [Test]
    public async Task TD05_R5_LibroNoDisponible_Rechazado()
    {
        await SeleccionarLector(false);
        await SeleccionarLibros(false, 1);
        DebeContener(await EnviarYLeerError(), "disponibilidad", "no hay copias|no disponible");
    }

    // R6: 3 libros sin copias => cantidad y disponibilidad invalidas.
    [Test]
    public async Task TD06_R6_TresNoDisponibles_AmbosErrores()
    {
        await SeleccionarLector(false);
        await SeleccionarLibros(false, 3);
        var error = await EnviarYLeerError();
        DebeContener(error, "cantidad", "más de 2|cantidad");
        DebeContener(error, "disponibilidad", "no hay copias|no disponible");
    }

    // R7: lector con pendiente y libro sin copias => dos errores.
    [Test]
    public async Task TD07_R7_PendienteYNoDisponible_AmbosErrores()
    {
        await SeleccionarLector(true);
        await SeleccionarLibros(false, 1);
        var error = await EnviarYLeerError();
        DebeContener(error, "lector", "lector|pendiente|prestatario");
        DebeContener(error, "disponibilidad", "no hay copias|no disponible");
    }

    // R8: lector con pendiente + 3 libros sin copias => tres errores.
    [Test]
    public async Task TD08_R8_TodasLasCondicionesInvalidas_TresErrores()
    {
        await SeleccionarLector(true);
        await SeleccionarLibros(false, 3);
        var error = await EnviarYLeerError();
        DebeContener(error, "lector", "lector|pendiente|prestatario");
        DebeContener(error, "disponibilidad", "no hay copias|no disponible");
        DebeContener(error, "cantidad", "más de 2|cantidad");
    }
}
