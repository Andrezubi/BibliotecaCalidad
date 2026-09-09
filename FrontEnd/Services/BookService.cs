using FrontEnd.DTOs;
using System.Net;
using System.Net.Http.Headers;

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
    // ======================================================
    // AUTORIZACIÓN
    // ======================================================

    private void AddAuthorizationHeader()
    {
        var token = _httpContextAccessor
            .HttpContext?
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


    // ======================================================
    // CONVERTIR RUTA DE PORTADA EN URL DEL BACKEND
    // ======================================================

    private string? BuildCoverImageUrl(string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(coverImage))
        {
            return null;
        }

        // Si ya es una URL completa, no la modificamos.
        if (Uri.TryCreate(
                coverImage,
                UriKind.Absolute,
                out _))
        {
            return coverImage;
        }

        var backendUrl =
            _httpClient.BaseAddress?
                .ToString()
                .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            return coverImage;
        }

        return $"{backendUrl}/{coverImage.TrimStart('/')}";
    }


    // ======================================================
    // GET ALL
    // ======================================================

    public async Task<List<BookDto>> GetAllAsync()
    {
        AddAuthorizationHeader();

        var response =
            await _httpClient.GetAsync("api/Book");

        if (!response.IsSuccessStatusCode)
        {
            return new List<BookDto>();
        }

        var books =
            await response.Content
                .ReadFromJsonAsync<List<BookDto>>()
            ?? new List<BookDto>();


        // Convertir las rutas de las portadas
        foreach (var book in books)
        {
            book.CoverImage =
                BuildCoverImageUrl(book.CoverImage);
        }

        return books;
    }


    // ======================================================
    // GET BY ID
    // ======================================================

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        AddAuthorizationHeader();

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

        var book =
            await response.Content
                .ReadFromJsonAsync<BookDto>();

        if (book == null)
        {
            return null;
        }

        book.CoverImage =
            BuildCoverImageUrl(book.CoverImage);

        return book;
    }


    // ======================================================
    // CREATE
    // ======================================================

    public async Task<(
        bool Success,
        Dictionary<string, string[]> Errors)>
        CreateAsync(CreateBookDto book)
    {
        AddAuthorizationHeader();

        using var content =
            new MultipartFormDataContent();


        content.Add(
            new StringContent(
                book.Title ?? string.Empty),
            "Title");


        content.Add(
            new StringContent(
                book.EditionNumber?.ToString()
                ?? string.Empty),
            "EditionNumber");


        content.Add(
            new StringContent(
                book.ISBN ?? string.Empty),
            "ISBN");


        content.Add(
            new StringContent(
                book.PublicationYear?.ToString()
                ?? string.Empty),
            "PublicationYear");


        content.Add(
            new StringContent(
                book.Publisher ?? string.Empty),
            "Publisher");


        content.Add(
            new StringContent(
                book.PageCount?.ToString()
                ?? string.Empty),
            "PageCount");


        content.Add(
            new StringContent(
                book.Description ?? string.Empty),
            "Description");


        // ==================================================
        // PORTADA
        // ==================================================

        if (book.CoverImage != null &&
            book.CoverImage.Length > 0)
        {
            var stream =
                book.CoverImage.OpenReadStream();

            var streamContent =
                new StreamContent(stream);

            streamContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    book.CoverImage.ContentType);

            content.Add(
                streamContent,
                "CoverImage",
                Path.GetFileName(
                    book.CoverImage.FileName));
        }


        var response =
            await _httpClient.PostAsync(
                "api/Book",
                content);


        if (response.IsSuccessStatusCode)
        {
            return (
                true,
                new Dictionary<string, string[]>()
            );
        }


        var errors =
            await ReadValidationErrorsAsync(
                response);

        return (
            false,
            errors
        );
    }


    // ======================================================
    // UPDATE
    // ======================================================

    public async Task<(
        bool Success,
        Dictionary<string, string[]> Errors)>
        UpdateAsync(
            int id,
            UpdateBookDto book)
    {
        AddAuthorizationHeader();

        using var content =
            new MultipartFormDataContent();


        content.Add(
            new StringContent(
                book.Title ?? string.Empty),
            "Title");


        content.Add(
            new StringContent(
                book.EditionNumber?.ToString()
                ?? string.Empty),
            "EditionNumber");


        content.Add(
            new StringContent(
                book.ISBN ?? string.Empty),
            "ISBN");


        content.Add(
            new StringContent(
                book.PublicationYear?.ToString()
                ?? string.Empty),
            "PublicationYear");


        content.Add(
            new StringContent(
                book.Publisher ?? string.Empty),
            "Publisher");


        content.Add(
            new StringContent(
                book.PageCount?.ToString()
                ?? string.Empty),
            "PageCount");


        content.Add(
            new StringContent(
                book.Description ?? string.Empty),
            "Description");


        // ==================================================
        // NUEVA PORTADA
        // ==================================================

        if (book.CoverImage != null &&
            book.CoverImage.Length > 0)
        {
            var stream =
                book.CoverImage.OpenReadStream();

            var streamContent =
                new StreamContent(stream);

            streamContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    book.CoverImage.ContentType);

            content.Add(
                streamContent,
                "CoverImage",
                Path.GetFileName(
                    book.CoverImage.FileName));
        }


        var response =
            await _httpClient.PutAsync(
                $"api/Book/{id}",
                content);


        if (response.IsSuccessStatusCode)
        {
            return (
                true,
                new Dictionary<string, string[]>()
            );
        }


        var errors =
            await ReadValidationErrorsAsync(
                response);

        return (
            false,
            errors
        );
    }


    // ======================================================
    // DELETE LÓGICO
    // ======================================================

    public async Task<bool> DeleteAsync(int id)
    {
        AddAuthorizationHeader();

        var response =
            await _httpClient.DeleteAsync(
                $"api/Book/{id}");

        return response.IsSuccessStatusCode;
    }


    // ======================================================
    // SEARCH
    // ======================================================

    public async Task<List<BookDto>> SearchAsync(
        string? title = null,
        string? author = null,
        string? category = null,
        string? isbn = null,
        string? publisher = null)
    {
        AddAuthorizationHeader();

        var queryParams =
            new List<string>();


        if (!string.IsNullOrWhiteSpace(title))
        {
            queryParams.Add(
                $"title={Uri.EscapeDataString(title)}");
        }


        if (!string.IsNullOrWhiteSpace(author))
        {
            queryParams.Add(
                $"author={Uri.EscapeDataString(author)}");
        }


        if (!string.IsNullOrWhiteSpace(category))
        {
            queryParams.Add(
                $"category={Uri.EscapeDataString(category)}");
        }


        if (!string.IsNullOrWhiteSpace(isbn))
        {
            queryParams.Add(
                $"isbn={Uri.EscapeDataString(isbn)}");
        }


        if (!string.IsNullOrWhiteSpace(publisher))
        {
            queryParams.Add(
                $"publisher={Uri.EscapeDataString(publisher)}");
        }


        var url =
            "api/Book/search";


        if (queryParams.Any())
        {
            url += "?" +
                   string.Join(
                       "&",
                       queryParams);
        }


        var books =
            await _httpClient
                .GetFromJsonAsync<List<BookDto>>(
                    url)
            ?? new List<BookDto>();


        // IMPORTANTE:
        // también convertir las portadas
        // cuando usamos búsqueda.

        foreach (var book in books)
        {
            book.CoverImage =
                BuildCoverImageUrl(
                    book.CoverImage);
        }


        return books;
    }
    public async Task<List<BookDto>> GetAvailableAsync()
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync("api/Book/available");

        if (!response.IsSuccessStatusCode)
        {
            return new List<BookDto>();
        }

        var books = await response.Content
            .ReadFromJsonAsync<List<BookDto>>();

        return books ?? new List<BookDto>();
    }


    // ======================================================
    // READ VALIDATION ERRORS
    // ======================================================

    private async Task<
        Dictionary<string, string[]>>
        ReadValidationErrorsAsync(
            HttpResponseMessage response)
    {
        try
        {
            var validation =
                await response.Content
                    .ReadFromJsonAsync<
                        ValidationResponse>();


            if (validation?.Errors != null)
            {
                return validation.Errors;
            }


            if (!string.IsNullOrWhiteSpace(
                    validation?.Message))
            {
                return new Dictionary<
                    string,
                    string[]>
                {
                    {
                        string.Empty,
                        new[]
                        {
                            validation.Message
                        }
                    }
                };
            }
        }
        catch
        {
            // La respuesta no tiene
            // el formato esperado.
        }


        return new Dictionary<
            string,
            string[]>
        {
            {
                string.Empty,
                new[]
                {
                    "No se pudo procesar la solicitud."
                }
            }
        };
    }


    // ======================================================
    // VALIDATION RESPONSE
    // ======================================================

    private class ValidationResponse
    {
        public string? Title { get; set; }

        public int? Status { get; set; }

        public string? Detail { get; set; }

        public string? Message { get; set; }

        public Dictionary<
            string,
            string[]>? Errors
        { get; set; }
    }
    public async Task<(bool Success, string Message)> AddCopyAsync(int bookId, string internalCode)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.PostAsJsonAsync(
            $"api/Book/{bookId}/copies",
            new { InternalCode = internalCode });

        var result = await response.Content
            .ReadFromJsonAsync<ApiMessage>();

        var message = result?.Message
            ?? (response.IsSuccessStatusCode
                ? "Copia agregada correctamente."
                : "No se pudo agregar la copia.");

        return (response.IsSuccessStatusCode, message);
    }
}