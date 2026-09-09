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

    [BindProperty]
    public CreateBookDto Book { get; set; } = new();

    public IActionResult OnGet()
    {
        if (!IsInAnyRole("Admin", "Librarian"))
        {
            return RedirectToPage("/AccessDenied");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsInAnyRole("Admin", "Librarian"))
        {
            return RedirectToPage("/AccessDenied");
        }


        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result =
            await _bookService.CreateAsync(Book);

        if (!result.Success)
        {
            AddErrorsToModelState(result.Errors);

            return Page();
        }

        TempData["Success"] =
            "Libro registrado correctamente.";

        return RedirectToPage("Index");
    }

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