using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class IndexModel : AuthorizedPageModel
{
    private readonly BookService _bookService;
    private readonly LoanService _loanService;
    private readonly IConfiguration _configuration;

    public IndexModel(
        BookService bookService,
        LoanService loanService,
        IConfiguration configuration)
    {
        _bookService = bookService;
        _loanService = loanService;
        _configuration = configuration;
    }

    public List<BookDto> Books { get; set; } = new();

    public List<AuthorDto> Authors { get; set; } = new();

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Phrase { get; set; }

    // =========================================================
    // GET
    // =========================================================

    public async Task OnGetAsync()
    {
        // Cargar autores y categorías
        Authors = (await _bookService.GetAuthorsAsync()).ToList();
        Categories = (await _bookService.GetCategoriesAsync()).ToList();

        // Cargar libros
        if (string.IsNullOrWhiteSpace(Phrase))
        {
            Books = (await _bookService.GetAllAsync())
                .ToList();

            return;
        }

        Books = (await _bookService.SearchAsync(Phrase))
            .ToList();
    }

    // =========================================================
    // ELIMINAR
    // =========================================================

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result =
            await _bookService.DeleteAsync(id);

        if (!result)
        {
            TempData["Error"] =
                "No se pudo eliminar el libro.";

            return RedirectToPage();
        }

        TempData["Success"] =
            "Libro eliminado correctamente.";

        return RedirectToPage();
    }

    // =========================================================
    // PRESTAR
    // =========================================================

    public async Task<IActionResult> OnPostLoanAsync(int bookId)
    {
        var ok =
            await _loanService.RegisterLoanAsync(bookId);

        if (ok)
        {
            TempData["Success"] =
                "Préstamo registrado. El libro ahora está prestado.";
        }
        else
        {
            TempData["Error"] =
                "No se pudo registrar el préstamo. El libro no tiene copias disponibles.";
        }

        return RedirectToPage();
    }

    // =========================================================
    // NOMBRES DE AUTORES
    // =========================================================

    public string GetAuthorNames(BookDto book)
    {
        if (book.AuthorIds == null ||
            !book.AuthorIds.Any())
        {
            return "Sin autor";
        }

        var names = book.AuthorIds
            .Select(authorId =>
                Authors.FirstOrDefault(
                    author => author.Id == authorId))
            .Where(author => author != null)
            .Select(author =>
                $"{author!.FirstName} {author.LastName}".Trim())
            .Where(name =>
                !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (!names.Any())
        {
            return "Sin autor";
        }

        return string.Join(", ", names);
    }

    // =========================================================
    // NOMBRES DE CATEGORÍAS
    // =========================================================

    public string GetCategoryNames(BookDto book)
    {
        if (book.CategoryIds == null ||
            !book.CategoryIds.Any())
        {
            return "Sin categoría";
        }

        var names = book.CategoryIds
            .Select(categoryId =>
                Categories.FirstOrDefault(
                    category => category.Id == categoryId))
            .Where(category => category != null)
            .Select(category => category!.Name)
            .Where(name =>
                !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (!names.Any())
        {
            return "Sin categoría";
        }

        return string.Join(", ", names);
    }

    // =========================================================
    // URL DE PORTADA
    // =========================================================

    public string GetCoverUrl(string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(coverImage))
        {
            return string.Empty;
        }

        // Si ya es una URL completa
        if (Uri.TryCreate(
                coverImage,
                UriKind.Absolute,
                out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var backendUrl =
            _configuration["BackendUrl"]?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            return coverImage;
        }

        return $"{backendUrl}/{coverImage.TrimStart('/')}";
    }
}