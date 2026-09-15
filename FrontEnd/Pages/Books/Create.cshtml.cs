using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FrontEnd.Pages.Shared;

namespace FrontEnd.Pages.Books;

public class CreateModel : AuthorizedPageModel
{
    private readonly BookService _bookService;

    public CreateModel(BookService bookService)
    {
        _bookService = bookService;
    }

    // =====================================================
    // LIBRO
    // =====================================================

    [BindProperty]
    public CreateBookDto Book { get; set; } = new();

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
        if (!IsInAnyRole("Admin", "Librarian"))
        {
            return RedirectToPage("/AccessDenied");
        }

        await LoadAuthorsAndCategoriesAsync();

        return Page();
    }

    // =====================================================
    // POST
    // =====================================================

    public async Task<IActionResult> OnPostAsync()
    {
        // =================================================
        // VALIDAR AUTORIZACIÓN
        // =================================================

        if (!IsInAnyRole("Admin", "Librarian"))
        {
            return RedirectToPage("/AccessDenied");
        }

        // =================================================
        // LIMPIAR IDs
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
        // VALIDAR MODELO
        // =================================================

        if (!ModelState.IsValid)
        {
            await LoadAuthorsAndCategoriesAsync();
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
                if (string.IsNullOrWhiteSpace(newAuthor.FirstName) &&
                    string.IsNullOrWhiteSpace(newAuthor.LastName))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(newAuthor.FirstName))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "El nombre del nuevo autor es obligatorio.");

                    await LoadAuthorsAndCategoriesAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(newAuthor.LastName))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "El apellido del nuevo autor es obligatorio.");

                    await LoadAuthorsAndCategoriesAsync();
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

                    await LoadAuthorsAndCategoriesAsync();
                    return Page();
                }

                // Agregar el ID real generado por la BD
                Book.AuthorIds.Add(result.Id.Value);
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
                if (string.IsNullOrWhiteSpace(newCategory.Name))
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

                    await LoadAuthorsAndCategoriesAsync();
                    return Page();
                }

                // Agregar el ID real generado por la BD
                Book.CategoryIds.Add(result.Id.Value);
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
        // CREAR LIBRO
        // =================================================

        var resultBook =
            await _bookService.CreateAsync(Book);

        if (!resultBook.Success)
        {
            AddErrorsToModelState(
                resultBook.Errors);

            await LoadAuthorsAndCategoriesAsync();
            return Page();
        }

        // =================================================
        // ÉXITO
        // =================================================

        TempData["Success"] =
            "Libro registrado correctamente.";

        return RedirectToPage("Index");
    }

    // =====================================================
    // CARGAR AUTORES Y CATEGORÍAS
    // =====================================================

    private async Task LoadAuthorsAndCategoriesAsync()
    {
        Authors =
            (await _bookService.GetAuthorsAsync()).ToList();

        Categories =
            (await _bookService.GetCategoriesAsync()).ToList();
    }

    // =====================================================
    // ERRORES
    // =====================================================

    private void AddErrorsToModelState(
        Dictionary<string, string[]> errors)
    {
        foreach (var error in errors)
        {
            var key = error.Key;

            if (string.IsNullOrWhiteSpace(key))
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