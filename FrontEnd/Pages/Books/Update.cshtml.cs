using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryBookCrud.Pages.Books;

public class UpdateModel : PageModel
{
    private readonly IBookService _bookService;

    public UpdateModel(IBookService bookService)
    {
        _bookService = bookService;
    }

    [BindProperty]
    public UpdateBookDto Book { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var book = await _bookService.GetByIdAsync(Id);

        if (book == null)
            return NotFound();

        Book = new UpdateBookDto
        {
            Title = book.Title,
            EditionNumber = book.EditionNumber,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Description = book.Description,
            UserId = book.UserId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await _bookService.UpdateAsync(Id, Book);

            if (result == null)
                return NotFound();

            TempData["Success"] = "Libro actualizado correctamente.";

            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            return Page();
        }
    }
}