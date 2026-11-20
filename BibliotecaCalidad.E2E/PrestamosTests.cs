using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BibliotecaCalidad.E2E;

// Pruebas de caja negra para FrontEnd/Pages/Loans/Create.cshtml.
// Basadas en la version del formulario que permite seleccionar libros sin copias
// y mas de dos libros, dejando la validacion al backend.
[TestFixture]
[Category("EC3")]
public class PrestamosTests : PageTest
{
    private static string BaseUrl =>
        (Environment.GetEnvironmentVariable("BC_BASE_URL") ?? "https://localhost:7001")
        .TrimEnd('/');

    [SetUp]
    public async Task IniciarSesionYAbrirFormulario()
    {
        string usuario = Environment.GetEnvironmentVariable("BC_TEST_USER")
            ?? throw new InvalidOperationException("Configura BC_TEST_USER en PowerShell.");
        string clave = Environment.GetEnvironmentVariable("BC_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Configura BC_TEST_PASSWORD en PowerShell.");

        await Page.GotoAsync($"{BaseUrl}/Auth/Login");
        await Page.Locator("#Login_Username").FillAsync(usuario);
        await Page.Locator("#Login_Password").FillAsync(clave);
        await Page.Locator("button.auth-submit").ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/Loans/Create");
        await Expect(Page.Locator("#readerSearch")).ToBeVisibleAsync();
        await Expect(Page.Locator("#bookSearch")).ToBeVisibleAsync();
    }

    // Captura la pantalla tras cada prueba, incluso cuando una asercion falla.
    [TearDown]
    public async Task GuardarEvidencia()
    {
        try
        {
            string carpeta = Path.Combine(
                TestContext.CurrentContext.WorkDirectory, "EvidenciasEC3");
            Directory.CreateDirectory(carpeta);

            string nombre = TestContext.CurrentContext.Test.Name;
            string archivo = Path.Combine(carpeta, nombre + ".png");
            await Page.ScreenshotAsync(new() { Path = archivo, FullPage = true });
            TestContext.AddTestAttachment(archivo, "Evidencia EC3: " + nombre);
            TestContext.Progress.WriteLine("Evidencia: " + archivo);
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine("No se pudo tomar captura: " + ex.Message);
        }
    }

    private async Task SeleccionarPrimerLector()
    {
        var lectores = Page.Locator(".reader-radio");
        if (await lectores.CountAsync() == 0)
            Assert.Ignore("No hay lectores activos en la base de datos de pruebas.");
        await lectores.First.CheckAsync();
    }

    private async Task SeleccionarLibros(int cantidad)
    {
        var libros = Page.Locator(".book-checkbox");
        if (await libros.CountAsync() < cantidad)
            Assert.Ignore($"Se necesitan {cantidad} libros activos para este caso.");
        for (int i = 0; i < cantidad; i++)
            await libros.Nth(i).CheckAsync();
    }

    // EP: combinaciones invalidas del formulario.
    [Test]
    public async Task CP01_SinLectorNiLibros_Rechaza()
    {
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("Seleccione un lector y al menos un libro.");
    }

    [Test]
    public async Task CP02_LectorSinLibros_Rechaza()
    {
        await SeleccionarPrimerLector();
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("Seleccione un lector y al menos un libro.");
    }

    [Test]
    public async Task CP03_LibroSinLector_Rechaza()
    {
        await SeleccionarLibros(1);
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("Seleccione un lector y al menos un libro.");
    }

    // Pruebas funcionales complementarias de busqueda.
    [Test]
    public async Task CP04_BuscarLectorInexistente()
    {
        await Page.Locator("#readerSearch")
            .FillAsync("ZZZ_LECTOR_INEXISTENTE_999");
        await Expect(Page.Locator("#readerSearchEmpty")).ToBeVisibleAsync();
        await Expect(Page.Locator(".reader-option:visible")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task CP05_BuscarLibroInexistente()
    {
        await Page.Locator("#bookSearch")
            .FillAsync("ZZZ_LIBRO_INEXISTENTE_999");
        await Expect(Page.Locator("#bookSearchEmpty")).ToBeVisibleAsync();
        await Expect(Page.Locator(".book-option:visible")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task CP06_SoloUnLectorSeleccionado()
    {
        var lectores = Page.Locator(".reader-radio");
        if (await lectores.CountAsync() < 2)
            Assert.Ignore("Se necesitan al menos dos lectores activos.");

        await lectores.Nth(0).CheckAsync();
        await lectores.Nth(1).CheckAsync();
        await Expect(lectores.Nth(0)).Not.ToBeCheckedAsync();
        await Expect(lectores.Nth(1)).ToBeCheckedAsync();
        await Expect(Page.Locator(".reader-radio:checked")).ToHaveCountAsync(1);
    }

    // BVA 3 del rango de 1 a 2 libros: 0, 1, 2 y 3.
    // El valor 0 ya se valida en CP02; 1 y 2 aqui verifican la seleccion
    // en la interfaz (NO comprueban que el backend acepte el prestamo).
    [Test]
    public async Task CP07_BVA_UnLibro_SeleccionPermitida()
    {
        await SeleccionarLibros(1);
        await Expect(Page.Locator("#selectedBooksCount"))
            .ToHaveTextAsync("1 libro seleccionado");
        await Expect(Page.Locator("#bookLimitError")).ToBeHiddenAsync();
    }

    [Test]
    public async Task CP08_BVA_DosLibros_SeleccionPermitida()
    {
        await SeleccionarLibros(2);
        await Expect(Page.Locator("#selectedBooksCount"))
            .ToHaveTextAsync("2 libros seleccionados");
        await Expect(Page.Locator("#bookLimitError")).ToBeHiddenAsync();
    }

    [Test]
    public async Task CP09_BVA_TresLibros_BackendRechaza()
    {
        await SeleccionarPrimerLector();
        await SeleccionarLibros(3);

        await Expect(Page.Locator("#selectedBooksCount"))
            .ToHaveTextAsync("3 libros seleccionados");
        await Expect(Page.Locator("#bookLimitError")).ToBeVisibleAsync();

        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("No se pueden prestar más de 2 libros a la vez.");
    }

    // EP: el libro aparece y se puede seleccionar, pero no debe prestarse
    // si no tiene copias disponibles.
    [Test]
    public async Task CP10_LibroSinCopias_BackendRechaza()
    {
        var libroSinCopias = Page.Locator(
            ".book-option:has-text('Sin copias disponibles') .book-checkbox");
        if (await libroSinCopias.CountAsync() == 0)
            Assert.Ignore("No hay libros activos con cero copias disponibles.");

        await SeleccionarPrimerLector();
        await libroSinCopias.First.CheckAsync();
        await Page.Locator("#submitLoanBtn").ClickAsync();
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("No hay copias disponibles para:");
    }

    [Test]
    public async Task CP11_BusquedaConservaLibroSeleccionado()
    {
        await SeleccionarLibros(1);
        var primerLibro = Page.Locator(".book-checkbox").First;

        await Page.Locator("#bookSearch")
            .FillAsync("ZZZ_LIBRO_INEXISTENTE_999");
        await Expect(primerLibro).ToBeCheckedAsync();
        await Expect(Page.Locator("#selectedBooksCount"))
            .ToHaveTextAsync("1 libro seleccionado");

        await Page.Locator("#bookSearch").FillAsync("");
        await Expect(primerLibro).ToBeCheckedAsync();
    }
}
