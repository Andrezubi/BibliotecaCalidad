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
        if (!IsInAnyRole("Admin", "Librarian"))
        {
            return RedirectToPage("/AccessDenied");
        }

        NormalizeSelectedIds();

        if (!ModelState.IsValid)
        {
            return await ReloadPageAsync();
        }

        if (!await CreateNewAuthorsAsync())
        {
            return await ReloadPageAsync();
        }

        if (!await CreateNewCategoriesAsync())
        {
            return await ReloadPageAsync();
        }

        NormalizeSelectedIds();

        var resultBook =
            await _bookService.CreateAsync(Book);

        if (!resultBook.Success)
        {
            AddErrorsToModelState(
                resultBook.Errors);

            return await ReloadPageAsync();
        }

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

    // =====================================================
    // HELPERS
    // =====================================================
    private void NormalizeSelectedIds()
    {
        Book.AuthorIds =
            NormalizeIds(Book.AuthorIds);

        Book.CategoryIds =
            NormalizeIds(Book.CategoryIds);
    }

    private static List<int> NormalizeIds(
        IEnumerable<int>? ids)
    {
        return ids?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();
    }


    private async Task<bool> CreateNewAuthorsAsync()
    {
        if (Book.NewAuthors == null ||
            Book.NewAuthors.Count == 0)
        {
            return true;
        }

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

                return false;
            }

            if (string.IsNullOrWhiteSpace(newAuthor.LastName))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El apellido del nuevo autor es obligatorio.");

                return false;
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

                return false;
            }

            Book.AuthorIds.Add(result.Id.Value);
        }

        return true;
    }


    private async Task<bool> CreateNewCategoriesAsync()
    {
        if (Book.NewCategories == null ||
            Book.NewCategories.Count == 0)
        {
            return true;
        }

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

                return false;
            }

            Book.CategoryIds.Add(result.Id.Value);
        }

        return true;
    }

    private async Task<IActionResult> ReloadPageAsync()
    {
        await LoadAuthorsAndCategoriesAsync();

        return Page();
    }

}