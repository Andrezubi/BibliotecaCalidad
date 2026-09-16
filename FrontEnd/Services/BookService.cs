using FrontEnd.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FrontEnd.Services;

public class BookService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public class ApiMessage
    {
        public string? Message { get; set; }
    }

    // ============================================================
    // AUTORIZACIÓN
    // ============================================================

    private void AddAuthorizationHeader()
    {
        var token =
            _httpContextAccessor.HttpContext?
                .Session
                .GetString("AuthToken");

        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    // ============================================================
    // COPIAS
    // ============================================================

    public async Task<(bool Success, string Message)> AddCopyAsync(
        int bookId,
        string internalCode)
    {
        AddAuthorizationHeader();

        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/Book/{bookId}/copies",
                new
                {
                    InternalCode = internalCode
                });

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiMessage>();

        var message =
            result?.Message ??
            GetCopyOperationMessage(
                response.IsSuccessStatusCode);

        return (
            response.IsSuccessStatusCode,
            message);
    }

    private static string GetCopyOperationMessage(
        bool success)
    {
        return success
            ? "Copia agregada correctamente."
            : "No se pudo agregar la copia.";
    }

    // ============================================================
    // LIBROS - GET ALL
    // ============================================================

    public async Task<List<BookDto>> GetAllAsync()
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.GetAsync(
                    "api/Book");

            if (!response.IsSuccessStatusCode)
            {
                return new List<BookDto>();
            }

            return await response.Content
                       .ReadFromJsonAsync<List<BookDto>>()
                   ?? new List<BookDto>();
        }
        catch
        {
            return new List<BookDto>();
        }
    }

    // ============================================================
    // LIBROS - GET BY ID
    // ============================================================

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.GetAsync(
                    $"api/Book/{id}");

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<BookDto>();
        }
        catch
        {
            return null;
        }
    }

    // ============================================================
    // LIBROS - SEARCH
    // ============================================================

    public async Task<List<BookDto>> SearchAsync(
        string? phrase)
    {
        AddAuthorizationHeader();

        try
        {
            var url =
                BuildSearchUrl(phrase);

            var response =
                await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<BookDto>();
            }

            return await response.Content
                       .ReadFromJsonAsync<List<BookDto>>()
                   ?? new List<BookDto>();
        }
        catch
        {
            return new List<BookDto>();
        }
    }

    private static string BuildSearchUrl(
        string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return "api/Book/search";
        }

        return
            $"api/Book/search?phrase={Uri.EscapeDataString(phrase)}";
    }

    // ============================================================
    // LIBROS - CREATE
    // ============================================================

    public async Task<ServiceResult> CreateAsync(
        CreateBookDto book)
    {
        AddAuthorizationHeader();

        using var content =
            BuildCreateBookContent(book);

        try
        {
            var response =
                await _httpClient.PostAsync(
                    "api/Book",
                    content);

            return await ProcessResponseAsync(
                response);
        }
        catch (HttpRequestException)
        {
            return CreateConnectionError();
        }
    }

    // ============================================================
    // LIBROS - UPDATE
    // ============================================================

    public async Task<ServiceResult> UpdateAsync(
        int id,
        UpdateBookDto book)
    {
        AddAuthorizationHeader();

        using var content =
            BuildUpdateBookContent(book);

        try
        {
            var response =
                await _httpClient.PutAsync(
                    $"api/Book/{id}",
                    content);

            return await ProcessResponseAsync(
                response);
        }
        catch (HttpRequestException)
        {
            return CreateConnectionError();
        }
    }

    private static ServiceResult CreateConnectionError()
    {
        return ServiceResult.Fail(
            new Dictionary<string, string[]>
            {
                {
                    string.Empty,
                    new[]
                    {
                        "No se pudo conectar con el servidor."
                    }
                }
            });
    }

    // ============================================================
    // LIBROS - DELETE
    // ============================================================

    public async Task<bool> DeleteAsync(int id)
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.DeleteAsync(
                    $"api/Book/{id}");

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // MULTIPART - CREATE BOOK
    // ============================================================

    private MultipartFormDataContent BuildCreateBookContent(
        CreateBookDto book)
    {
        var content =
            new MultipartFormDataContent();

        AddBookFields(
            content,
            book.Title,
            book.EditionNumber,
            book.ISBN,
            book.PublicationYear,
            book.Publisher,
            book.PageCount,
            book.Description,
            book.UserId);

        AddAuthors(
            content,
            book.AuthorIds);

        AddCategories(
            content,
            book.CategoryIds);

        AddCover(
            content,
            book.CoverImage);

        return content;
    }

    // ============================================================
    // MULTIPART - UPDATE BOOK
    // ============================================================

    private MultipartFormDataContent BuildUpdateBookContent(
        UpdateBookDto book)
    {
        var content =
            new MultipartFormDataContent();

        AddBookFields(
            content,
            book.Title,
            book.EditionNumber,
            book.ISBN,
            book.PublicationYear,
            book.Publisher,
            book.PageCount,
            book.Description,
            book.UserId);

        AddAuthors(
            content,
            book.AuthorIds);

        AddCategories(
            content,
            book.CategoryIds);

        AddCover(
            content,
            book.CoverImage);

        return content;
    }

    // ============================================================
    // CAMPOS DEL LIBRO
    // ============================================================

    private static void AddBookFields(
        MultipartFormDataContent content,
        string? title,
        object? editionNumber,
        object? isbn,
        object? publicationYear,
        object? publisher,
        object? pageCount,
        object? description,
        object? userId)
    {
        AddString(
            content,
            "Title",
            title);

        AddNullableString(
            content,
            "EditionNumber",
            editionNumber);

        AddNullableString(
            content,
            "ISBN",
            isbn);

        AddNullableString(
            content,
            "PublicationYear",
            publicationYear);

        AddNullableString(
            content,
            "Publisher",
            publisher);

        AddNullableString(
            content,
            "PageCount",
            pageCount);

        AddNullableString(
            content,
            "Description",
            description);

        AddNullableString(
            content,
            "UserId",
            userId);
    }

    // ============================================================
    // AUTORES MULTIPART
    // ============================================================

    private static void AddAuthors(
        MultipartFormDataContent content,
        IEnumerable<int> authorIds)
    {
        var ids =
            authorIds
                .Where(id => id > 0)
                .Distinct();

        foreach (var authorId in ids)
        {
            content.Add(
                new StringContent(
                    authorId.ToString()),
                "AuthorIds");
        }
    }

    // ============================================================
    // CATEGORÍAS MULTIPART
    // ============================================================

    private static void AddCategories(
        MultipartFormDataContent content,
        IEnumerable<int> categoryIds)
    {
        var ids =
            categoryIds
                .Where(id => id > 0)
                .Distinct();

        foreach (var categoryId in ids)
        {
            content.Add(
                new StringContent(
                    categoryId.ToString()),
                "CategoryIds");
        }
    }

    // ============================================================
    // AGREGAR PORTADA
    // ============================================================

    private static void AddCover(
        MultipartFormDataContent content,
        IFormFile? file)
    {
        if (file == null || file.Length <= 0)
        {
            return;
        }

        var stream =
            file.OpenReadStream();

        var streamContent =
            new StreamContent(stream);

        var contentType =
            string.IsNullOrWhiteSpace(
                file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        content.Add(
            streamContent,
            "CoverImage",
            file.FileName);
    }

    // ============================================================
    // STRING HELPERS
    // ============================================================

    private static void AddString(
        MultipartFormDataContent content,
        string name,
        string? value)
    {
        content.Add(
            new StringContent(
                value ?? string.Empty),
            name);
    }

    private static void AddNullableString(
        MultipartFormDataContent content,
        string name,
        object? value)
    {
        if (value == null)
        {
            return;
        }

        content.Add(
            new StringContent(
                value.ToString() ?? string.Empty),
            name);
    }

    // ============================================================
    // AUTORES - GET
    // ============================================================

    public async Task<List<AuthorDto>> GetAuthorsAsync()
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.GetAsync(
                    "api/Author");

            if (!response.IsSuccessStatusCode)
            {
                return new List<AuthorDto>();
            }

            var authors =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BackendAuthorResponse>>();

            return MapAuthors(authors);
        }
        catch
        {
            return new List<AuthorDto>();
        }
    }

    private static List<AuthorDto> MapAuthors(
        List<BackendAuthorResponse>? authors)
    {
        if (authors == null)
        {
            return new List<AuthorDto>();
        }

        return authors
            .Select(author => new AuthorDto
            {
                Id = author.Id,
                FirstName = author.FirstName,
                LastName = author.LastName
            })
            .ToList();
    }

    // ============================================================
    // AUTORES - CREATE
    // ============================================================

    public async Task<(
        bool Success,
        int? Id,
        string? Error)>
        CreateAuthorAsync(
            CreateAuthorDto author)
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/Author",
                    author);

            if (response.IsSuccessStatusCode)
            {
                return await ProcessCreatedAuthorAsync(
                    response);
            }

            var error =
                await ReadSimpleErrorAsync(
                    response);

            return (
                false,
                null,
                error);
        }
        catch (HttpRequestException)
        {
            return (
                false,
                null,
                "No se pudo conectar con el servidor.");
        }
    }

    private static async Task<(
        bool Success,
        int? Id,
        string? Error)>
        ProcessCreatedAuthorAsync(
            HttpResponseMessage response)
    {
        var created =
            await response.Content
                .ReadFromJsonAsync<
                    BackendAuthorResponse>();

        if (created != null)
        {
            return (
                true,
                created.Id,
                null);
        }

        return (
            false,
            null,
            "El autor fue creado pero no se pudo obtener su Id.");
    }

    // ============================================================
    // CATEGORÍAS - GET
    // ============================================================

    public async Task<List<CategoryDto>>
        GetCategoriesAsync()
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.GetAsync(
                    "api/Category");

            if (!response.IsSuccessStatusCode)
            {
                return new List<CategoryDto>();
            }

            var categories =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BackendCategoryResponse>>();

            return MapCategories(categories);
        }
        catch
        {
            return new List<CategoryDto>();
        }
    }

    private static List<CategoryDto> MapCategories(
        List<BackendCategoryResponse>? categories)
    {
        if (categories == null)
        {
            return new List<CategoryDto>();
        }

        return categories
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToList();
    }

    // ============================================================
    // CATEGORÍAS - CREATE
    // ============================================================

    public async Task<(
        bool Success,
        int? Id,
        string? Error)>
        CreateCategoryAsync(
            CreateCategoryDto category)
    {
        AddAuthorizationHeader();

        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/Category",
                    category);

            if (response.IsSuccessStatusCode)
            {
                return await ProcessCreatedCategoryAsync(
                    response);
            }

            var error =
                await ReadSimpleErrorAsync(
                    response);

            return (
                false,
                null,
                error);
        }
        catch (HttpRequestException)
        {
            return (
                false,
                null,
                "No se pudo conectar con el servidor.");
        }
    }

    private static async Task<(
        bool Success,
        int? Id,
        string? Error)>
        ProcessCreatedCategoryAsync(
            HttpResponseMessage response)
    {
        var created =
            await response.Content
                .ReadFromJsonAsync<
                    BackendCategoryResponse>();

        if (created != null)
        {
            return (
                true,
                created.Id,
                null);
        }

        return (
            false,
            null,
            "La categoría fue creada pero no se pudo obtener su Id.");
    }

    // ============================================================
    // PROCESAR RESPUESTA DEL BACKEND
    // ============================================================

    private async Task<ServiceResult>
        ProcessResponseAsync(
            HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return ServiceResult.Ok();
        }

        var body =
            await response.Content
                .ReadAsStringAsync();

        var errors =
            ParseResponseErrors(body);

        if (errors.Count == 0)
        {
            AddGenericError(
                errors,
                response.StatusCode);
        }

        return ServiceResult.Fail(errors);
    }

    private static Dictionary<string, string[]>
        ParseResponseErrors(
            string body)
    {
        var errors =
            new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(body))
        {
            return errors;
        }

        try
        {
            using var document =
                JsonDocument.Parse(body);

            var root =
                document.RootElement;

            AddValidationErrors(
                errors,
                root);

            AddMessageError(
                errors,
                root);

            AddDetailError(
                errors,
                root);
        }
        catch (JsonException)
        {
            // Se utilizará el mensaje genérico.
        }

        return errors;
    }

    // ============================================================
    // ERRORES DE VALIDACIÓN
    // ============================================================

    private static void AddValidationErrors(
        Dictionary<string, string[]> errors,
        JsonElement root)
    {
        if (!root.TryGetProperty(
                "errors",
                out var errorsElement))
        {
            return;
        }

        foreach (var property in
                 errorsElement.EnumerateObject())
        {
            var messages =
                GetValidationMessages(
                    property.Value);

            if (messages.Length > 0)
            {
                errors[property.Name] =
                    messages;
            }
        }
    }

    private static string[] GetValidationMessages(
        JsonElement property)
    {
        return property
            .EnumerateArray()
            .Select(element =>
                element.GetString() ?? string.Empty)
            .Where(text =>
                !string.IsNullOrWhiteSpace(text))
            .ToArray();
    }

    // ============================================================
    // MESSAGE
    // ============================================================

    private static void AddMessageError(
        Dictionary<string, string[]> errors,
        JsonElement root)
    {
        if (!TryGetJsonString(
                root,
                "message",
                out var message))
        {
            return;
        }

        errors[string.Empty] =
            new[]
            {
                message
            };
    }

    // ============================================================
    // DETAIL
    // ============================================================

    private static void AddDetailError(
        Dictionary<string, string[]> errors,
        JsonElement root)
    {
        if (errors.Count > 0)
        {
            return;
        }

        if (!TryGetJsonString(
                root,
                "detail",
                out var detail))
        {
            return;
        }

        errors[string.Empty] =
            new[]
            {
                detail
            };
    }

    // ============================================================
    // LEER STRING JSON
    // ============================================================

    private static bool TryGetJsonString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(
                propertyName,
                out var property))
        {
            return false;
        }

        var text =
            property.GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    // ============================================================
    // ERROR GENÉRICO
    // ============================================================

    private static void AddGenericError(
        Dictionary<string, string[]> errors,
        HttpStatusCode statusCode)
    {
        errors[string.Empty] =
            new[]
            {
                GetStatusErrorMessage(
                    statusCode)
            };
    }

    // ============================================================
    // LEER ERROR SIMPLE
    // ============================================================

    private async Task<string>
        ReadSimpleErrorAsync(
            HttpResponseMessage response)
    {
        var body =
            await response.Content
                .ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return GetStatusErrorMessage(
                response.StatusCode);
        }

        try
        {
            using var document =
                JsonDocument.Parse(body);

            return ExtractSimpleError(
                       document.RootElement)
                   ?? GetStatusErrorMessage(
                       response.StatusCode);
        }
        catch (JsonException)
        {
            return GetStatusErrorMessage(
                response.StatusCode);
        }
    }

    // ============================================================
    // EXTRAER ERROR SIMPLE
    // ============================================================

    private static string?
        ExtractSimpleError(
            JsonElement root)
    {
        return
            GetMessage(root)
            ?? GetDetail(root)
            ?? GetValidationError(root);
    }

    // ============================================================
    // EXTRAER MESSAGE
    // ============================================================

    private static string? GetMessage(
        JsonElement root)
    {
        return TryGetJsonString(
            root,
            "message",
            out var message)
            ? message
            : null;
    }

    // ============================================================
    // EXTRAER DETAIL
    // ============================================================

    private static string? GetDetail(
        JsonElement root)
    {
        return TryGetJsonString(
            root,
            "detail",
            out var detail)
            ? detail
            : null;
    }

    // ============================================================
    // EXTRAER ERRORES DE VALIDACIÓN
    // ============================================================

    private static string? GetValidationError(
        JsonElement root)
    {
        if (!root.TryGetProperty(
                "errors",
                out var errorsElement))
        {
            return null;
        }

        var messages =
            errorsElement
                .EnumerateObject()
                .SelectMany(property =>
                    GetValidationMessages(
                        property.Value))
                .ToList();

        return messages.Count > 0
            ? string.Join(" ", messages)
            : null;
    }

    // ============================================================
    // MENSAJES SEGÚN STATUS HTTP
    // ============================================================

    private static string
        GetStatusErrorMessage(
            HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest =>
                "Los datos enviados no son válidos.",

            HttpStatusCode.Unauthorized =>
                "La sesión ha expirado. Inicie sesión nuevamente.",

            HttpStatusCode.Forbidden =>
                "No tiene permisos para realizar esta operación.",

            HttpStatusCode.NotFound =>
                "El recurso solicitado no fue encontrado.",

            HttpStatusCode.Conflict =>
                "La operación no se pudo realizar porque existe un conflicto.",

            HttpStatusCode.InternalServerError =>
                "Ocurrió un error interno en el servidor.",

            _ =>
                "No se pudo completar la operación."
        };
    }

    // ============================================================
    // RESPUESTAS DEL BACKEND
    // ============================================================

    private class BackendAuthorResponse
    {
        public int Id { get; set; }

        public string FirstName { get; set; }
            = string.Empty;

        public string LastName { get; set; }
            = string.Empty;
    }

    private class BackendCategoryResponse
    {
        public int Id { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string? Description { get; set; }
    }
}

// ================================================================
// RESULTADO DE OPERACIONES
// ================================================================

public class ServiceResult
{
    public bool Success { get; private set; }

    public Dictionary<string, string[]> Errors { get; private set; }
        = new();

    public static ServiceResult Ok()
    {
        return new ServiceResult
        {
            Success = true
        };
    }

    public static ServiceResult Fail(
        Dictionary<string, string[]> errors)
    {
        return new ServiceResult
        {
            Success = false,
            Errors = errors
        };
    }
}