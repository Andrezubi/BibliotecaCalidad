using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BibliotecaCalidad.E2E;

[TestFixture]
[Category("EC3-Estados")]
[NonParallelizable]
public class PrestamosEstadosTests : PageTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static string FrontUrl => (Environment.GetEnvironmentVariable("BC_BASE_URL") ?? "https://localhost:7001").TrimEnd('/');
    private static string BackendUrl => (Environment.GetEnvironmentVariable("BC_BACKEND_URL") ?? "https://localhost:7118").TrimEnd('/');

    private HttpClient _api = null!;
    private HashSet<int> _idsIniciales = new();
    private int _prestamoCreadoId;
    private int _copiaCreadaId;
    private string _lectorNombre = "";
    private bool _autenticadoEnApi;

    private sealed class ResumenPrestamo
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
    }
    private sealed class DetallePrestamo
    {
        public int Id { get; set; }
        public int BorrowerId { get; set; }
        public string Status { get; set; } = "";
        public List<ItemPrestamo> Items { get; set; } = new();
    }
    private sealed class ItemPrestamo
    {
        public int CopyId { get; set; }
        public int BookId { get; set; }
        public string Status { get; set; } = "";
        public DateTime? ReturnedAt { get; set; }
    }
    private sealed class RespuestaDevolucion
    {
        public bool HasFine { get; set; }
        public string Message { get; set; } = "";
        public decimal Amount { get; set; }
        public int OverdueDays { get; set; }
    }
    private sealed class RespuestaLogin
    {
        public string Token { get; set; } = "";
    }
    private sealed class DetalleMultas
    {
        public List<Multa> Fines { get; set; } = new();
    }
    private sealed class Multa
    {
        public int FineId { get; set; }
        public decimal Amount { get; set; }
    }

    [SetUp]
    public async Task Preparar()
    {
        if (Environment.GetEnvironmentVariable("BC_EC3_ALLOW_WRITES") != "1")
            Assert.Ignore("Estas pruebas crean prestamos/multas. Usa una BD de pruebas y configura BC_EC3_ALLOW_WRITES=1.");

        var usuario = Environment.GetEnvironmentVariable("BC_TEST_USER")
            ?? throw new InvalidOperationException("Configura BC_TEST_USER.");
        var clave = Environment.GetEnvironmentVariable("BC_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Configura BC_TEST_PASSWORD.");

        var uri = new Uri(BackendUrl);
        if (!uri.IsLoopback)
            throw new InvalidOperationException("BC_BACKEND_URL debe apuntar a localhost.");
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _api = new HttpClient(handler) { BaseAddress = new Uri(BackendUrl + "/") };

        // Token necesario para consultar el detalle de las multas (api/fines).
        using (var response = await _api.PostAsJsonAsync("api/auth/login", new { Username = usuario, Password = clave }))
        {
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<RespuestaLogin>(JsonOptions);
                if (!string.IsNullOrWhiteSpace(auth?.Token))
                {
                    _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    _autenticadoEnApi = true;
                }
            }
        }

        await Page.GotoAsync(FrontUrl + "/Auth/Login");
        await Page.Locator("#Login_Username").FillAsync(usuario);
        await Page.Locator("#Login_Password").FillAsync(clave);
        await Page.Locator("button.auth-submit").ClickAsync();
        await Page.GotoAsync(FrontUrl + "/Loans/Create");
        await Expect(Page.Locator("#readerSearch")).ToBeVisibleAsync();
        _idsIniciales = (await ObtenerPrestamos()).Select(p => p.Id).ToHashSet();
    }

    [TearDown]
    public async Task EvidenciaYLimpieza()
    {
        try
        {
            var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "EvidenciasEC3", "Estados");
            Directory.CreateDirectory(dir);
            var ruta = Path.Combine(dir, TestContext.CurrentContext.Test.Name + ".png");
            await Page.ScreenshotAsync(new() { Path = ruta, FullPage = true });
            TestContext.AddTestAttachment(ruta, "Transicion de estados");
            TestContext.Progress.WriteLine("Evidencia: " + ruta);
        }
        catch (Exception ex) { TestContext.Progress.WriteLine("Captura: " + ex.Message); }

        // Liberar copias si una prueba falla antes de completar la devolucion.
        // No elimina el historial ni las multas: por eso se exige una BD de pruebas.
        try
        {
            if (_api != null && _prestamoCreadoId > 0)
            {
                var detalle = await ObtenerDetalle(_prestamoCreadoId);
                foreach (var item in detalle.Items.Where(i => i.Status is "Active" or "Overdue"))
                    await _api.PostAsync($"api/loans/return/{item.CopyId}?confirm=true", null);
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
            ?? throw new InvalidOperationException("No se pudo consultar el prestamo " + id);
    }

    private async Task CrearPrestamoEnChromium()
    {
        // Preferir lector sin prestamos pendientes, segun la regla del informe.
        var idsPendientes = new HashSet<int>();
        foreach (var p in (await ObtenerPrestamos()).Where(p => p.Status is "Active" or "Overdue"))
            idsPendientes.Add((await ObtenerDetalle(p.Id)).BorrowerId);

        var lectores = Page.Locator(".reader-radio");
        int lectorId = 0;
        for (int i = 0; i < await lectores.CountAsync(); i++)
        {
            var radio = lectores.Nth(i);
            if (!int.TryParse(await radio.GetAttributeAsync("value"), out var id)) continue;
            if (idsPendientes.Contains(id)) continue;
            lectorId = id;
            _lectorNombre = await radio.GetAttributeAsync("data-name") ?? "";
            await radio.CheckAsync();
            break;
        }
        if (lectorId == 0)
            Assert.Ignore("Necesitas un lector sin prestamos pendientes para crear el prestamo de prueba.");

        var disponibles = Page.Locator(".book-option:not(:has-text('Sin copias disponibles')) .book-checkbox");
        if (await disponibles.CountAsync() == 0)
            Assert.Ignore("Necesitas al menos un libro con una copia disponible.");
        var libro = disponibles.First;
        int bookId = int.Parse((await libro.GetAttributeAsync("value"))!);
        await libro.CheckAsync();
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator("h2.page-title")).ToContainTextAsync("Préstamos");

        var nuevos = (await ObtenerPrestamos()).Where(p => !_idsIniciales.Contains(p.Id)).ToList();
        foreach (var nuevo in nuevos)
        {
            var detalle = await ObtenerDetalle(nuevo.Id);
            if (detalle.BorrowerId != lectorId) continue;
            var item = detalle.Items.FirstOrDefault(i => i.BookId == bookId);
            if (item == null) continue;
            _prestamoCreadoId = nuevo.Id;
            _copiaCreadaId = item.CopyId;
            return;
        }
        Assert.Fail("El FrontEnd indico exito, pero no se encontro el prestamo creado en la API.");
    }

    private async Task MarcarPrestamoVencido()
    {
        using var response = await _api.PutAsJsonAsync($"api/loans/{_prestamoCreadoId}",
            new { DueDate = DateTime.Today.AddDays(-8), Status = "Overdue" });
        response.EnsureSuccessStatusCode();
        await ObtenerPrestamos(); // Fuerza sincronizacion de estados en LoanService.
        var detalle = await ObtenerDetalle(_prestamoCreadoId);
        Assert.That(detalle.Status, Is.EqualTo("Overdue"), "AS -> VC: el prestamo debe estar vencido.");
        Assert.That(detalle.Items.Single().Status, Is.EqualTo("Overdue"));
    }

    private async Task<RespuestaDevolucion> Devolver(bool confirmar)
    {
        using var response = await _api.PostAsync(
            $"api/loans/return/{_copiaCreadaId}?confirm={confirmar.ToString().ToLowerInvariant()}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RespuestaDevolucion>(JsonOptions)
            ?? throw new InvalidOperationException("Respuesta de devolucion vacia.");
    }

    private async Task VerificarDevuelto()
    {
        var detalle = await ObtenerDetalle(_prestamoCreadoId);
        Assert.That(detalle.Status, Is.EqualTo("Completed"));
        Assert.That(detalle.Items.Single().Status, Is.EqualTo("Returned"));
        Assert.That(detalle.Items.Single().ReturnedAt, Is.Not.Null);
    }

    private async Task MostrarDetalleEnChromium()
    {
        await Page.GotoAsync(FrontUrl + "/Loans/Index");
        await Expect(Page.Locator("h2.page-title")).ToContainTextAsync("Préstamos");
        // El prestamo de prueba es el mas reciente de este lector.
        var fila = Page.Locator("table tbody tr").Filter(new() { HasText = _lectorNombre }).First;
        await Expect(fila).ToBeVisibleAsync();
        await fila.Locator("a[title='Ver detalles']").ClickAsync();
        await Expect(Page.Locator("h2.page-title")).ToContainTextAsync("Detalle de Préstamo");
    }

    private async Task<DetalleMultas> ObtenerMultas()
    {
        if (!_autenticadoEnApi)
            Assert.Ignore("No se pudo autenticar api/fines. Revisa BC_TEST_USER y BC_TEST_PASSWORD.");
        using var response = await _api.GetAsync($"api/fines/loans/{_prestamoCreadoId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DetalleMultas>(JsonOptions)
            ?? throw new InvalidOperationException("No se pudo leer el detalle de multas.");
    }

    // CP1: AS -> VC -> DC: vencimiento, devolucion y multa.
    [Test]
    public async Task TE01_CP1_VencidoDevuelto_GeneraMulta()
    {
        await CrearPrestamoEnChromium();
        await MarcarPrestamoVencido();
        var vistaPrevia = await Devolver(false);
        Assert.That(vistaPrevia.HasFine, Is.True, "La vista previa debe informar multa.");
        var confirmacion = await Devolver(true);
        Assert.That(confirmacion.HasFine, Is.True);
        Assert.That(confirmacion.Amount, Is.GreaterThan(0m));
        Assert.That(confirmacion.OverdueDays, Is.GreaterThan(0));
        await VerificarDevuelto();
        await MostrarDetalleEnChromium();
    }

    // CP2: AS -> DS: devolucion dentro de plazo sin multa.
    [Test]
    public async Task TE02_CP2_DevueltoEnPlazo_SinMulta()
    {
        await CrearPrestamoEnChromium();
        var confirmacion = await Devolver(true);
        Assert.That(confirmacion.HasFine, Is.False);
        Assert.That(confirmacion.Amount, Is.EqualTo(0m));
        await VerificarDevuelto();
        await MostrarDetalleEnChromium();
    }

    // CP3: DS -> DS: una segunda devolucion debe ser rechazada.
    [Test]
    public async Task TE03_CP3_DevueltoSinMulta_NoSeDevuelveDosVeces()
    {
        await CrearPrestamoEnChromium();
        await Devolver(true);
        var repetida = await Devolver(true);
        Assert.That(repetida.Message, Does.Contain("no está prestada").IgnoreCase);
        await VerificarDevuelto();
        await MostrarDetalleEnChromium();
    }

    // CP4: DS + fecha de vencimiento pasada => permanece DS (sin multa).
    [Test]
    public async Task TE04_CP4_DevueltoLuegoVence_NoCambiaEstado()
    {
        await CrearPrestamoEnChromium();
        await Devolver(true);
        using (var response = await _api.PutAsJsonAsync($"api/loans/{_prestamoCreadoId}",
            new { DueDate = DateTime.Today.AddDays(-8), Status = "Completed" }))
            response.EnsureSuccessStatusCode();
        await ObtenerPrestamos();
        await VerificarDevuelto();
        await MostrarDetalleEnChromium();
    }

    // CP5: DC -> DC: segunda devolucion no debe generar otra multa.
    // El documento tiene una inconsistencia en CP5 (dice DS), pero la tabla
    // de transicion y la regla de no duplicar multas indican conservar DC.
    [Test]
    public async Task TE05_CP5_DevueltoConMulta_NoDuplicaMulta()
    {
        if (!_autenticadoEnApi)
            Assert.Ignore("Para CP5 se necesita acceso autenticado a api/fines.");
        await CrearPrestamoEnChromium();
        await MarcarPrestamoVencido();
        var confirmacion = await Devolver(true);
        Assert.That(confirmacion.HasFine, Is.True);
        var multasAntes = (await ObtenerMultas()).Fines.Count(f => f.FineId > 0);
        Assert.That(multasAntes, Is.EqualTo(1), "Debe existir exactamente una multa registrada.");
        var repetida = await Devolver(true);
        Assert.That(repetida.Message, Does.Contain("no está prestada").IgnoreCase);
        var multasDespues = (await ObtenerMultas()).Fines.Count(f => f.FineId > 0);
        Assert.That(multasDespues, Is.EqualTo(multasAntes), "No debe duplicarse la multa.");
        await VerificarDevuelto();
        await MostrarDetalleEnChromium();
    }
}
