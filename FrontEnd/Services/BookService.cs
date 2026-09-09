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

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }
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
                await _httpClient.GetAsync("api/Book");

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
            var url = "api/Book/search";

            if (!string.IsNullOrWhiteSpace(phrase))
            {
                url +=
                    $"?phrase={Uri.EscapeDataString(phrase)}";
            }

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

    private MultipartFormDataContent
        BuildCreateBookContent(
            CreateBookDto book)
    {
        var content =
            new MultipartFormDataContent();

        AddString(
            content,
            "Title",
            book.Title);

        AddNullableString(
            content,
            "EditionNumber",
            book.EditionNumber);

        AddNullableString(
            content,
            "ISBN",
            book.ISBN);

        AddNullableString(
            content,
            "PublicationYear",
            book.PublicationYear);

        AddNullableString(
            content,
            "Publisher",
            book.Publisher);

        AddNullableString(
            content,
            "PageCount",
            book.PageCount);

        AddNullableString(
            content,
            "Description",
            book.Description);

        AddNullableString(
            content,
            "UserId",
            book.UserId);

        // ========================================================
        // AUTORES
        // ========================================================

        foreach (var authorId in
                 book.AuthorIds
                     .Where(id => id > 0)
                     .Distinct())
        {
            content.Add(
                new StringContent(
                    authorId.ToString()),
                "AuthorIds");
        }

        // ========================================================
        // CATEGORÍAS
        // ========================================================

        foreach (var categoryId in
                 book.CategoryIds
                     .Where(id => id > 0)
                     .Distinct())
        {
            content.Add(
                new StringContent(
                    categoryId.ToString()),
                "CategoryIds");
        }

        // ========================================================
        // PORTADA
        // ========================================================

        AddCover(
            content,
            book.CoverImage);

        return content;
    }

    // ============================================================
    // MULTIPART - UPDATE BOOK
    // ============================================================

    private MultipartFormDataContent
        BuildUpdateBookContent(
            UpdateBookDto book)
    {
        var content =
            new MultipartFormDataContent();

        AddString(
            content,
            "Title",
            book.Title);

        AddNullableString(
            content,
            "EditionNumber",
            book.EditionNumber);

        AddNullableString(
            content,
            "ISBN",
            book.ISBN);

        AddNullableString(
            content,
            "PublicationYear",
            book.PublicationYear);

        AddNullableString(
            content,
            "Publisher",
            book.Publisher);

        AddNullableString(
            content,
            "PageCount",
            book.PageCount);

        AddNullableString(
            content,
            "Description",
            book.Description);

        AddNullableString(
            content,
            "UserId",
            book.UserId);

        // ========================================================
        // AUTORES
        // ========================================================

        foreach (var authorId in
                 book.AuthorIds
                     .Where(id => id > 0)
                     .Distinct())
        {
            content.Add(
                new StringContent(
                    authorId.ToString()),
                "AuthorIds");
        }

        // ========================================================
        // CATEGORÍAS
        // ========================================================

        foreach (var categoryId in
                 book.CategoryIds
                     .Where(id => id > 0)
                     .Distinct())
        {
            content.Add(
                new StringContent(
                    categoryId.ToString()),
                "CategoryIds");
        }

        // ========================================================
        // PORTADA
        // ========================================================

        AddCover(
            content,
            book.CoverImage);

        return content;
    }

    // ============================================================
    // AGREGAR PORTADA
    // ============================================================

    private static void AddCover(
        MultipartFormDataContent content,
        IFormFile? file)
    {
        if (file == null ||
            file.Length <= 0)
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

    public async Task<List<AuthorDto>>
        GetAuthorsAsync()
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

            if (authors == null)
            {
                return new List<AuthorDto>();
            }

            return authors
                .Select(author => new AuthorDto
                {
                    Id =
                        author.Id,

                    FirstName =
                        author.FirstName,

                    LastName =
                        author.LastName
                })
                .ToList();
        }
        catch
        {
            return new List<AuthorDto>();
        }
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

            if (categories == null)
            {
                return new List<CategoryDto>();
            }

            return categories
                .Select(category => new CategoryDto
                {
                    Id =
                        category.Id,

                    Name =
                        category.Name
                })
                .ToList();
        }
        catch
        {
            return new List<CategoryDto>();
        }
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

        var errors =
            new Dictionary<string, string[]>();

        var body =
            await response.Content
                .ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document =
                    JsonDocument.Parse(body);

                var root =
                    document.RootElement;

                // =================================================
                // ERRORS
                // =================================================

                if (root.TryGetProperty(
                        "errors",
                        out var errorsElement))
                {
                    foreach (var property in
                             errorsElement.EnumerateObject())
                    {
                        var errorMessages =
                            property.Value
                                .EnumerateArray()
                                .Select(
                                    element =>
                                        element.GetString()
                                        ?? string.Empty)
                                .Where(
                                    text =>
                                        !string.IsNullOrWhiteSpace(
                                            text))
                                .ToArray();

                        if (errorMessages.Length > 0)
                        {
                            errors[property.Name] =
                                errorMessages;
                        }
                    }
                }

                // =================================================
                // MESSAGE
                // =================================================

                if (root.TryGetProperty(
                        "message",
                        out var messageElement))
                {
                    var messageText =
                        messageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            messageText))
                    {
                        errors[string.Empty] =
                            new[]
                            {
                                messageText
                            };
                    }
                }

                // =================================================
                // DETAIL
                // =================================================

                if (errors.Count == 0 &&
                    root.TryGetProperty(
                        "detail",
                        out var detailElement))
                {
                    var detailText =
                        detailElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            detailText))
                    {
                        errors[string.Empty] =
                            new[]
                            {
                                detailText
                            };
                    }
                }
            }
            catch (JsonException)
            {
                // Se utilizará el mensaje genérico.
            }
        }

        // =========================================================
        // ERROR GENÉRICO
        // =========================================================

        if (errors.Count == 0)
        {
            errors[string.Empty] =
                new[]
                {
                    GetStatusErrorMessage(
                        response.StatusCode)
                };
        }

        return ServiceResult.Fail(
            errors);
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

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document =
                    JsonDocument.Parse(body);

                var root =
                    document.RootElement;

                // =================================================
                // MESSAGE
                // =================================================

                if (root.TryGetProperty(
                        "message",
                        out var messageElement))
                {
                    var messageText =
                        messageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            messageText))
                    {
                        return messageText;
                    }
                }

                // =================================================
                // DETAIL
                // =================================================

                if (root.TryGetProperty(
                        "detail",
                        out var detailElement))
                {
                    var detailText =
                        detailElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            detailText))
                    {
                        return detailText;
                    }
                }

                // =================================================
                // VALIDATION ERRORS
                // =================================================

                if (root.TryGetProperty(
                        "errors",
                        out var errorsElement))
                {
                    var validationMessages =
                        new List<string>();

                    foreach (var property in
                             errorsElement.EnumerateObject())
                    {
                        foreach (var errorElement in
                                 property.Value.EnumerateArray())
                        {
                            var errorText =
                                errorElement.GetString();

                            if (!string.IsNullOrWhiteSpace(
                                    errorText))
                            {
                                validationMessages.Add(
                                    errorText);
                            }
                        }
                    }

                    if (validationMessages.Count > 0)
                    {
                        return string.Join(
                            " ",
                            validationMessages);
                    }
                }
            }
            catch (JsonException)
            {
                // Se utilizará el mensaje HTTP.
            }
        }

        return GetStatusErrorMessage(
            response.StatusCode);
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