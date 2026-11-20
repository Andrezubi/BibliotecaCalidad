using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BibliotecaCalidad.E2E;

// EC3: pruebas funcionales de caja negra contra la API REST de autores.
// Correspondencia con el informe: EP (N2, N5, N12, A5, DU1, DU2),
// BVA 3 (NB1, NB2, NB5, NB6, AB5, AB6) y estados (CP9, CP10, CP13).
// IMPORTANTE: ejecutar sobre una BASE DE DATOS DE PRUEBAS.
// Las eliminaciones son logicas (IsActive=false), por lo que dejan historial.
[TestFixture]
[NonParallelizable]
[Category("EC3")]
[Category("EC3-Autor")]
public class AutorTests : PlaywrightTest
{
    private IAPIRequestContext? _api;
    private readonly List<uint> _autoresCreados = new();
    private readonly StringBuilder _bitacora = new();

    private static string ApiUrl =>
        (Environment.GetEnvironmentVariable("BC_API_URL") ?? "https://localhost:7001")
        .TrimEnd('/');

    [SetUp]
    public async Task PrepararApi()
    {
        _autoresCreados.Clear();
        _bitacora.Clear();
        _bitacora.AppendLine($"Caso: {TestContext.CurrentContext.Test.Name}");
        _bitacora.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _bitacora.AppendLine($"API: {ApiUrl}");

        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            IgnoreHTTPSErrors = true,
            Timeout = 15000
        });

        // Comprobar que el backend esta disponible antes de probar sus reglas.
        var respuesta = await _api.GetAsync("/api/Author");
        await RegistrarRespuestaAsync("GET /api/Author (conexion)", respuesta);
        Assert.That(respuesta.Status, Is.EqualTo(200),
            $"No se pudo consultar la API de autores en {ApiUrl}. " +
            "Comprueba que Backend este iniciado y que BC_API_URL sea correcto.");
    }

    [TearDown]
    public async Task GuardarEvidenciaYLiberarRecursos()
    {
        try
        {
            if (_api != null)
            {
                // Limpiar SOLO los autores creados por esta prueba.
                // La API realiza baja logica; no se borran filas fisicamente.
                foreach (uint id in _autoresCreados)
                {
                    try
                    {
                        var respuesta = await _api.DeleteAsync($"/api/Author/{id}");
                        _bitacora.AppendLine($"Limpieza DELETE /api/Author/{id}: HTTP {respuesta.Status}");
                    }
                    catch (Exception ex)
                    {
                        _bitacora.AppendLine($"No se pudo limpiar autor {id}: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            if (_api != null)
            {
                await _api.DisposeAsync();
                _api = null;
            }

            string carpeta = Path.Combine(
                TestContext.CurrentContext.WorkDirectory, "EvidenciasEC3", "Autores");
            Directory.CreateDirectory(carpeta);

            string archivo = Path.Combine(
                carpeta, TestContext.CurrentContext.Test.Name + ".txt");
            _bitacora.AppendLine($"Resultado NUnit: {TestContext.CurrentContext.Result.Outcome.Status}");
            File.WriteAllText(archivo, _bitacora.ToString(), Encoding.UTF8);
            TestContext.AddTestAttachment(archivo, "Evidencia de API - EC3 Autor");
            TestContext.Progress.WriteLine("Evidencia: " + archivo);
        }
    }

    private static string Sufijo() => Guid.NewGuid().ToString("N")[..8];

    private async Task RegistrarRespuestaAsync(string accion, IAPIResponse respuesta)
    {
        _bitacora.AppendLine();
        _bitacora.AppendLine(accion);
        _bitacora.AppendLine($"HTTP {respuesta.Status}");
        _bitacora.AppendLine(await respuesta.TextAsync());
    }

    private async Task<IAPIResponse> CrearAsync(string? nombre, string? apellido)
    {
        if (_api == null)
            throw new InvalidOperationException("No se ha inicializado el cliente API.");

        _bitacora.AppendLine($"POST /api/Author | FirstName={JsonSerializer.Serialize(nombre)} " +
                            $"LastName={JsonSerializer.Serialize(apellido)}");

        var respuesta = await _api.PostAsync("/api/Author", new()
        {
            DataObject = new { firstName = nombre, lastName = apellido, userId = (int?)null }
        });
        await RegistrarRespuestaAsync("POST /api/Author (respuesta)", respuesta);

        if (respuesta.Status == 201)
        {
            uint id = await LeerIdAsync(respuesta);
            _autoresCreados.Add(id);
        }
        return respuesta;
    }

    private static async Task<uint> LeerIdAsync(IAPIResponse respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.TextAsync());
        JsonElement raiz = json.RootElement;
        if (raiz.TryGetProperty("id", out var id) ||
            raiz.TryGetProperty("Id", out id))
            return id.GetUInt32();

        throw new InvalidOperationException("La API devolvio HTTP 201 sin un ID de autor legible.");
    }

    private async Task<uint> CrearValidoAsync(string nombre, string apellido)
    {
        var respuesta = await CrearAsync(nombre, apellido);
        Assert.That(respuesta.Status, Is.EqualTo(201),
            $"Se esperaba HTTP 201 al crear el autor. Respuesta: {await respuesta.TextAsync()}");
        uint id = await LeerIdAsync(respuesta);

        var consulta = await _api!.GetAsync($"/api/Author/{id}");
        await RegistrarRespuestaAsync($"GET /api/Author/{id}", consulta);
        Assert.That(consulta.Status, Is.EqualTo(200),
            "El autor creado debe poder consultarse como activo.");
        return id;
    }

    private async Task DebeRechazarDatosAsync(string? nombre, string? apellido)
    {
        var respuesta = await CrearAsync(nombre, apellido);
        // 500 NO cuenta como rechazo correcto: indica error interno del servidor.
        Assert.That(respuesta.Status == 400 || respuesta.Status == 422, Is.True,
            $"Se esperaba rechazo de validacion HTTP 400/422, " +
            $"pero se obtuvo HTTP {respuesta.Status}. " +
            $"Detalle: {await respuesta.TextAsync()}");
    }

    private async Task EliminarAsync(uint id)
    {
        var respuesta = await _api!.DeleteAsync($"/api/Author/{id}");
        await RegistrarRespuestaAsync($"DELETE /api/Author/{id}", respuesta);
        Assert.That(respuesta.Status, Is.EqualTo(204),
            "Eliminar un autor activo debe devolver HTTP 204.");
    }

    // -------- PARTICION DE EQUIVALENCIAS (EP) --------

    [Test]
    public async Task AUT01_EP_N2_NombreConTildes_Aceptado()
    {
        await CrearValidoAsync("Begoña María", "Prueba" + Sufijo());
    }

    [Test]
    public async Task AUT02_EP_N5_NombreVacio_Rechazado()
    {
        await DebeRechazarDatosAsync("", "Prueba" + Sufijo());
    }

    [Test]
    public async Task AUT03_EP_N12_SimboloNoPermitido_Rechazado()
    {
        await DebeRechazarDatosAsync("Gabr$el", "Prueba" + Sufijo());
    }

    [Test]
    public async Task AUT04_EP_A5_ApellidoVacio_Rechazado()
    {
        await DebeRechazarDatosAsync("Prueba" + Sufijo(), "");
    }

    // -------- ANALISIS DE VALORES LIMITE (BVA 3) --------

    [Test]
    public async Task AUT05_BVA_NB1_NombreLongitud0_Rechazado()
    {
        await DebeRechazarDatosAsync("", "Limite" + Sufijo());
    }

    [Test]
    public async Task AUT06_BVA_NB2_NombreLongitud1_Aceptado()
    {
        await CrearValidoAsync("A", "Limite" + Sufijo());
    }

    [Test]
    public async Task AUT07_BVA_NB5_NombreLongitud50_Aceptado()
    {
        await CrearValidoAsync(new string('A', 50), "Limite" + Sufijo());
    }

    [Test]
    public async Task AUT08_BVA_NB6_NombreLongitud51_Rechazado()
    {
        await DebeRechazarDatosAsync(new string('A', 51), "Limite" + Sufijo());
    }

    [Test]
    public async Task AUT09_BVA_AB5_ApellidoLongitud50_Aceptado()
    {
        await CrearValidoAsync("Limite" + Sufijo(), new string('A', 50));
    }

    [Test]
    public async Task AUT10_BVA_AB6_ApellidoLongitud51_Rechazado()
    {
        await DebeRechazarDatosAsync("Limite" + Sufijo(), new string('A', 51));
    }

    // -------- PARTICIONES DE DUPLICADOS --------

    [Test]
    public async Task AUT11_EP_DU1_AutorNuevo_Aceptado()
    {
        string sufijo = Sufijo();
        await CrearValidoAsync("Gabriel" + sufijo, "Garcia" + sufijo);
    }

    [Test]
    public async Task AUT12_EP_DU2_AutorDuplicado_Rechazado409()
    {
        string sufijo = Sufijo();
        string nombre = "Gabriel" + sufijo;
        string apellido = "Garcia" + sufijo;

        await CrearValidoAsync(nombre, apellido);
        var duplicado = await CrearAsync(nombre, apellido);
        Assert.That(duplicado.Status, Is.EqualTo(409),
            $"El duplicado activo debe rechazarse con HTTP 409. " +
            $"Respuesta: {await duplicado.TextAsync()}");
    }

    // -------- TRANSICION DE ESTADOS (NE / EA / EI) --------

    [Test]
    public async Task AUT13_EST_CP9_EliminarAutorActivo_PasaAInactivo()
    {
        string sufijo = Sufijo();
        uint id = await CrearValidoAsync("Estado" + sufijo, "Activo" + sufijo);
        await EliminarAsync(id);

        var consulta = await _api!.GetAsync($"/api/Author/{id}");
        await RegistrarRespuestaAsync($"GET /api/Author/{id} (tras eliminar)", consulta);
        Assert.That(consulta.Status, Is.EqualTo(404),
            "Un autor eliminado logicamente ya no debe consultarse como activo.");
    }

    [Test]
    public async Task AUT14_EST_CP10_ConsultarAutorInactivo_Devuelve404()
    {
        string sufijo = Sufijo();
        uint id = await CrearValidoAsync("Estado" + sufijo, "Inactivo" + sufijo);
        await EliminarAsync(id);

        var consulta = await _api!.GetAsync($"/api/Author/{id}");
        await RegistrarRespuestaAsync($"GET /api/Author/{id} (estado EI)", consulta);
        Assert.That(consulta.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task AUT15_EST_CP13_RecrearAutorEliminado_GeneraNuevoRegistro()
    {
        string sufijo = Sufijo();
        string nombre = "Recrear" + sufijo;
        string apellido = "Autor" + sufijo;

        uint idOriginal = await CrearValidoAsync(nombre, apellido);
        await EliminarAsync(idOriginal);
        uint idNuevo = await CrearValidoAsync(nombre, apellido);

        Assert.That(idNuevo, Is.Not.EqualTo(idOriginal),
            "Recrear un autor inactivo debe generar un ID diferente.");

        var antiguo = await _api!.GetAsync($"/api/Author/{idOriginal}");
        await RegistrarRespuestaAsync($"GET /api/Author/{idOriginal} (antiguo)", antiguo);
        Assert.That(antiguo.Status, Is.EqualTo(404),
            "El registro anterior debe continuar inactivo.");

        var nuevo = await _api.GetAsync($"/api/Author/{idNuevo}");
        await RegistrarRespuestaAsync($"GET /api/Author/{idNuevo} (nuevo)", nuevo);
        Assert.That(nuevo.Status, Is.EqualTo(200),
            "El nuevo registro debe estar activo.");
    }
}
