using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class IndexModel : PageModel
{
    private readonly BookService _bookService;

    public IndexModel(BookService bookService)
    {
        _bookService = bookService;
    }

    public List<BookDto> Books { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool OnlyAvailable { get; set; } = true;

    public async Task OnGetAsync()
    {
        Books = await _bookService.GetBooksAsync(OnlyAvailable);
    }
}