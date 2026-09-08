using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class UpdateModel : PageModel
{
    private readonly BookService _bookService;

    public UpdateModel(BookService bookService)
    {
        _bookService = bookService;
    }

    [BindProperty]
    public UpdateBookDto Book { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public string? CurrentCoverImage { get; set; }

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
            UserId = null
        };

        CurrentCoverImage =
            book.CoverImage;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCurrentCoverImageAsync();

            return Page();
        }

        var result =
            await _bookService.UpdateAsync(
                Id,
                Book);

        if (!result.Success)
        {
            AddErrorsToModelState(result.Errors);

            await LoadCurrentCoverImageAsync();

            return Page();
        }

        TempData["Success"] =
            "Libro actualizado correctamente.";

        return RedirectToPage("Index");
    }

    private async Task LoadCurrentCoverImageAsync()
    {
        var book =
            await _bookService.GetByIdAsync(Id);

        CurrentCoverImage =
            book?.CoverImage;
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