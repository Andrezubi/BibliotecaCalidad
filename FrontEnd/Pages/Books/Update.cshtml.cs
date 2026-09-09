using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class UpdateModel : PageModel
{
    private readonly BookService _bookService;
    private readonly IConfiguration _configuration;

    public UpdateModel(
        BookService bookService,
        IConfiguration configuration)
    {
        _bookService = bookService;
        _configuration = configuration;
    }

    // =====================================================
    // LIBRO
    // =====================================================

    [BindProperty]
    public UpdateBookDto Book { get; set; } = new();

    // =====================================================
    // ID
    // =====================================================

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    // =====================================================
    // PORTADA
    // =====================================================

    public string? CurrentCoverImage { get; set; }

    // =====================================================
    // AUTORES
    // =====================================================

    public List<AuthorDto> Authors { get; set; } = new();

    // =====================================================
    // CATEGORÍAS
    // =====================================================

    public List<CategoryDto> Categories { get; set; } = new();

    // =====================================================
    // GET
    // =====================================================

    public async Task<IActionResult> OnGetAsync()
    {
        var book =
            await _bookService.GetByIdAsync(Id);

        if (book == null)
        {
            return NotFound();
        }

        Book = new UpdateBookDto
        {
            Title = book.Title,
            EditionNumber = book.EditionNumber,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Description = book.Description,
            UserId = book.UserId,

            AuthorIds =
                book.AuthorIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList(),

            CategoryIds =
                book.CategoryIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList()
        };

        CurrentCoverImage =
            book.CoverImage;

        await LoadAuthorsAndCategoriesAsync();

        return Page();
    }

    // =====================================================
    // POST
    // =====================================================

    public async Task<IActionResult> OnPostAsync()
    {
        // =================================================
        // LIMPIAR IDS
        // =================================================

        Book.AuthorIds =
            Book.AuthorIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList()
            ?? new List<int>();

        Book.CategoryIds =
            Book.CategoryIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList()
            ?? new List<int>();

        // =================================================
        // VALIDAR PORTADA
        // =================================================

        ValidateCover();

        // =================================================
        // VALIDACIÓN
        // =================================================

        if (!ModelState.IsValid)
        {
            await LoadCurrentDataAsync();
            return Page();
        }

        // =================================================
        // CREAR AUTORES NUEVOS
        // =================================================

        if (Book.NewAuthors != null &&
            Book.NewAuthors.Count > 0)
        {
            foreach (var newAuthor in Book.NewAuthors)
            {
                if (string.IsNullOrWhiteSpace(
                        newAuthor.FirstName) &&
                    string.IsNullOrWhiteSpace(
                        newAuthor.LastName))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        newAuthor.FirstName))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "El nombre del nuevo autor es obligatorio.");

                    await LoadCurrentDataAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(
                        newAuthor.LastName))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "El apellido del nuevo autor es obligatorio.");

                    await LoadCurrentDataAsync();
                    return Page();
                }

                var result =
                    await _bookService.CreateAuthorAsync(
                        newAuthor);

                if (!result.Success ||
                    result.Id == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result.Error ??
                        "No se pudo crear el autor.");

                    await LoadCurrentDataAsync();
                    return Page();
                }

                Book.AuthorIds.Add(
                    result.Id.Value);
            }
        }

        // =================================================
        // CREAR CATEGORÍAS NUEVAS
        // =================================================

        if (Book.NewCategories != null &&
            Book.NewCategories.Count > 0)
        {
            foreach (var newCategory in Book.NewCategories)
            {
                if (string.IsNullOrWhiteSpace(
                        newCategory.Name))
                {
                    continue;
                }

                var result =
                    await _bookService.CreateCategoryAsync(
                        newCategory);

                if (!result.Success ||
                    result.Id == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result.Error ??
                        "No se pudo crear la categoría.");

                    await LoadCurrentDataAsync();
                    return Page();
                }

                Book.CategoryIds.Add(
                    result.Id.Value);
            }
        }

        // =================================================
        // LIMPIAR DUPLICADOS
        // =================================================

        Book.AuthorIds =
            Book.AuthorIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        Book.CategoryIds =
            Book.CategoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        // =================================================
        // ACTUALIZAR
        // =================================================

        var resultBook =
            await _bookService.UpdateAsync(
                Id,
                Book);

        if (!resultBook.Success)
        {
            AddErrorsToModelState(
                resultBook.Errors);

            await LoadCurrentDataAsync();

            return Page();
        }

        TempData["Success"] =
            "Libro actualizado correctamente.";

        return RedirectToPage("Index");
    }

    // =====================================================
    // VALIDAR PORTADA
    // =====================================================

    private void ValidateCover()
    {
        var file =
            Book.CoverImage;

        if (file == null ||
            file.Length == 0)
        {
            return;
        }

        const long maxFileSize =
            5 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            ModelState.AddModelError(
                "Book.CoverImage",
                "La portada no puede superar los 5 MB.");

            return;
        }

        var allowedExtensions =
            new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        var extension =
            Path.GetExtension(
                file.FileName)
                .ToLowerInvariant();

        if (!allowedExtensions.Contains(
                extension))
        {
            ModelState.AddModelError(
                "Book.CoverImage",
                "La portada debe tener formato JPG, JPEG, PNG o WEBP.");

            return;
        }

        var allowedContentTypes =
            new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        if (!allowedContentTypes.Contains(
                file.ContentType.ToLowerInvariant()))
        {
            ModelState.AddModelError(
                "Book.CoverImage",
                "El archivo seleccionado no es una imagen válida.");
        }
    }

    // =====================================================
    // CARGAR AUTORES Y CATEGORÍAS
    // =====================================================

    private async Task LoadAuthorsAndCategoriesAsync()
    {
        Authors =
            await _bookService.GetAuthorsAsync();

        Categories =
            await _bookService.GetCategoriesAsync();
    }

    // =====================================================
    // CARGAR DATOS ACTUALES
    // =====================================================

    private async Task LoadCurrentDataAsync()
    {
        var book =
            await _bookService.GetByIdAsync(Id);

        if (book != null)
        {
            CurrentCoverImage =
                book.CoverImage;
        }

        await LoadAuthorsAndCategoriesAsync();
    }

    // =====================================================
    // URL PORTADA
    // =====================================================

    public string GetCoverUrl(
        string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(
                coverImage))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(
                coverImage,
                UriKind.Absolute,
                out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var backendUrl =
            _configuration["BackendUrl"]?
                .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(
                backendUrl))
        {
            return coverImage;
        }

        return
            $"{backendUrl}/{coverImage.TrimStart('/')}";
    }

    // =====================================================
    // ERRORES
    // =====================================================

    private void AddErrorsToModelState(
        Dictionary<string, string[]> errors)
    {
        foreach (var error in errors)
        {
            var key =
                error.Key;

            if (string.IsNullOrWhiteSpace(
                    key))
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        message);
                }

                continue;
            }

            foreach (var message in error.Value)
            {
                ModelState.AddModelError(
                    $"Book.{key}",
                    message);
            }
        }
    }
}