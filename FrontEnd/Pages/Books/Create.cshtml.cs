using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class CreateModel : PageModel
{
    private readonly BookService _bookService;

    public CreateModel(BookService bookService)
    {
        _bookService = bookService;
    }

    [BindProperty]
    public CreateBookDto Book { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _bookService.CreateAsync(Book);

        if (!result)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se pudo registrar el libro."
            );

            return Page();
        }

        TempData["Success"] = "Libro registrado correctamente.";

        return RedirectToPage("Index");
    }
}