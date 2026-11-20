using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BibliotecaCalidad.E2E;

// EC3 - Caja negra: formulario Razor Pages /Books/Create.
// Los casos de aceptacion crean libros reales: ejecutar en una base de datos de PRUEBAS.
// Los casos invalidos verifican la respuesta visible de la interfaz.
[TestFixture]
[Category("EC3-Libros")]
[NonParallelizable]
public class LibrosTests : PageTest
{
    private static string BaseUrl =>
        (Environment.GetEnvironmentVariable("BC_BASE_URL") ?? "https://localhost:7001")
        .TrimEnd('/');

    private static string TituloUnico() =>
        "Libro EC3 " + Guid.NewGuid().ToString("N")[..10];

    [SetUp]
    public async Task IniciarSesionYAbrirRegistro()
    {
        string usuario = Environment.GetEnvironmentVariable("BC_TEST_USER")
            ?? throw new InvalidOperationException("Configura BC_TEST_USER en PowerShell.");
        string clave = Environment.GetEnvironmentVariable("BC_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Configura BC_TEST_PASSWORD en PowerShell.");

        await Page.GotoAsync($"{BaseUrl}/Auth/Login");
        await Page.Locator("#Login_Username").FillAsync(usuario);
        await Page.Locator("#Login_Password").FillAsync(clave);
        await Page.Locator("button.auth-submit").ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/Books/Create");
        await Expect(Page.Locator("#createBookForm")).ToBeVisibleAsync();
        await Expect(Page.Locator("#Book_Title")).ToBeVisibleAsync();
    }

    [TearDown]
    public async Task GuardarEvidencia()
    {
        try
        {
            string carpeta = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "EvidenciasEC3", "Libros");
            Directory.CreateDirectory(carpeta);

            string nombre = TestContext.CurrentContext.Test.Name;
            string archivo = Path.Combine(carpeta, nombre + ".png");
            await Page.ScreenshotAsync(new() { Path = archivo, FullPage = true });
            TestContext.AddTestAttachment(archivo, "EC3 - Libros - " + nombre);
            TestContext.Progress.WriteLine("Evidencia: " + archivo);
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine("No se pudo capturar evidencia: " + ex.Message);
        }
    }

    private async Task PrepararFormularioValidoAsync(
        bool seleccionarAutor = true,
        bool seleccionarCategoria = true)
    {
        await Page.Locator("#Book_Title").FillAsync(TituloUnico());
        await Page.Locator("#Book_EditionNumber").FillAsync("1");

        // Autor y categoria son obligatorios para el Backend.
        // El dropdown es personalizado: hay que abrirlo antes de marcar.
        if (seleccionarAutor)
        {
            await Page.Locator("#authorDropdownButton").ClickAsync();
            var autores = Page.Locator("#authorDropdown .author-checkbox");
            if (await autores.CountAsync() == 0)
                Assert.Ignore("No hay autores activos. Registra al menos uno antes de probar libros.");
            await autores.First.CheckAsync();

            // El menu de autores queda abierto despues de seleccionar.
            // Clic fuera del desplegable: el JavaScript del formulario lo cierra.
            await Page.Locator("h1.page-title").ClickAsync();
            await Expect(Page.Locator("#authorDropdown")).ToBeHiddenAsync();
        }

        if (seleccionarCategoria)
        {
            await Page.Locator("#categoryDropdownButton").ClickAsync();
            var categorias = Page.Locator("#categoryDropdown .category-checkbox");
            if (await categorias.CountAsync() == 0)
                Assert.Ignore("No hay categorias activas. Registra al menos una antes de probar libros.");
            await categorias.First.CheckAsync();

            // Cerrar categorias para no tapar el boton Registrar libro.
            await Page.Locator("h1.page-title").ClickAsync();
            await Expect(Page.Locator("#categoryDropdown")).ToBeHiddenAsync();
        }
    }

    private async Task RegistrarYComprobarExitoAsync()
    {
        await Page.Locator("#saveBookButton").ClickAsync();
        await Expect(Page.Locator(".alert-success"))
            .ToContainTextAsync("Libro registrado correctamente.");
        Assert.That(Page.Url, Does.Contain("/Books"),
            "Al registrar un libro, se esperaba volver al listado de libros.");
    }

    private async Task EnviarYComprobarErrorAsync(string mensajeEsperado)
    {
        await Page.Locator("#saveBookButton").ClickAsync();
        await Expect(Page.Locator("#createBookForm .alert-danger"))
            .ToContainTextAsync(mensajeEsperado);
        await Expect(Page.Locator("#Book_Title")).ToBeVisibleAsync();
    }

    private async Task ComprobarRestriccionHtmlAsync(string selector)
    {
        var campo = Page.Locator(selector);
        bool esInvalido = await campo.EvaluateAsync<bool>(
            "element => !element.checkValidity()");
        Assert.That(esInvalido, Is.True,
            $"Se esperaba que el navegador marcara como invalido el campo {selector}.");

        await Page.Locator("#saveBookButton").ClickAsync();
        await Expect(Page.Locator("#createBookForm")).ToBeVisibleAsync();
    }

    // Particion de equivalencia: titulo obligatorio.
    [Test]
    public async Task LIB01_EP_TituloVacio_Rechazado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Title").FillAsync("");
        await Page.Locator("#saveBookButton").ClickAsync();
        // La validacion puede ejecutarse en el cliente o en Razor Pages.
        await Expect(Page.Locator("[data-valmsg-for='Book.Title']"))
            .ToContainTextAsync(new Regex(@"\S"));
        await Expect(Page.Locator("#Book_Title")).ToBeVisibleAsync();
    }

    [Test]
    public async Task LIB02_EP_TituloConEmoji_Rechazado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Title").FillAsync("Libro EC3 😀");
        await EnviarYComprobarErrorAsync("El título contiene caracteres no válidos.");
    }

    // BVA del titulo (limite superior: 255): 0, 1, 255 y 256.
    [Test]
    public async Task LIB03_BVA_TituloLongitud1_Aceptado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Title").FillAsync("A");
        await RegistrarYComprobarExitoAsync();
    }

    [Test]
    public async Task LIB04_BVA_TituloLongitud255_Aceptado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Title").FillAsync(new string('L', 255));
        await RegistrarYComprobarExitoAsync();
    }

    [Test]
    public async Task LIB05_BVA_TituloLongitud256_Rechazado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Title").FillAsync(new string('L', 256));
        await EnviarYComprobarErrorAsync("El título no puede superar los 255 caracteres.");
    }

    // BVA del anio: 999, 1000, 2026, 2027 (segun BookValidator.cs).
    [Test]
    public async Task LIB06_BVA_Anio999_BloqueadoPorHTML()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_PublicationYear").FillAsync("999");
        await ComprobarRestriccionHtmlAsync("#Book_PublicationYear");
    }

    [Test]
    public async Task LIB07_BVA_Anio1000_Aceptado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_PublicationYear").FillAsync("1000");
        await RegistrarYComprobarExitoAsync();
    }

    [Test]
    public async Task LIB08_BVA_Anio2026_Aceptado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_PublicationYear").FillAsync("2026");
        await RegistrarYComprobarExitoAsync();
    }

    [Test]
    public async Task LIB09_BVA_Anio2027_RechazadoPorBackend()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_PublicationYear").FillAsync("2027");
        await EnviarYComprobarErrorAsync(
            "El año de publicación debe estar entre 1000 y 2026.");
    }

    // Particion de equivalencia: relaciones obligatorias.
    [Test]
    public async Task LIB10_EP_SinAutor_Rechazado()
    {
        await PrepararFormularioValidoAsync(seleccionarAutor: false);
        await EnviarYComprobarErrorAsync("Debe seleccionar al menos un autor.");
    }

    [Test]
    public async Task LIB11_EP_SinCategoria_Rechazado()
    {
        await PrepararFormularioValidoAsync(seleccionarCategoria: false);
        await EnviarYComprobarErrorAsync("Debe seleccionar al menos una categoría.");
    }

    // Particion de equivalencia: ISBN opcional, pero si se indica, 13 digitos.
    [Test]
    public async Task LIB12_EP_ISBN12Digitos_Rechazado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_ISBN").FillAsync("978123456789");
        await EnviarYComprobarErrorAsync("El ISBN debe contener 13 dígitos.");
    }

    [Test]
    public async Task LIB13_EP_ISBN13Digitos_Aceptado()
    {
        await PrepararFormularioValidoAsync();
        // Numero de 13 digitos diferente en cada ejecucion para evitar duplicados.
        string isbn = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        await Page.Locator("#Book_ISBN").FillAsync(isbn);
        await RegistrarYComprobarExitoAsync();
    }

    [Test]
    public async Task LIB14_EP_ISBN14Digitos_Rechazado()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_ISBN").FillAsync("97812345678901");
        await EnviarYComprobarErrorAsync("El ISBN debe contener 13 dígitos.");
    }

    // Validaciones nativas del navegador (HTML min=1).
    [Test]
    public async Task LIB15_EP_EdicionCero_BloqueadaPorHTML()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_EditionNumber").FillAsync("0");
        await ComprobarRestriccionHtmlAsync("#Book_EditionNumber");
    }

    [Test]
    public async Task LIB16_EP_PaginasCero_BloqueadasPorHTML()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_PageCount").FillAsync("0");
        await ComprobarRestriccionHtmlAsync("#Book_PageCount");
    }

    // La interfaz permite hasta 2000 caracteres, pero el Backend solo 400.
    [Test]
    public async Task LIB17_BVA_Descripcion401_RechazadaPorBackend()
    {
        await PrepararFormularioValidoAsync();
        await Page.Locator("#Book_Description").FillAsync(new string('D', 401));
        await EnviarYComprobarErrorAsync("La descripción no puede superar los 400 caracteres.");
    }
}
