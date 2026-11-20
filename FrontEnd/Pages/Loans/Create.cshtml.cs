using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Loans;

public class CreateModel : AuthorizedPageModel
{
    private readonly LoanService _loanService;
    private readonly BookService _bookService;
    private readonly UserService _userService;

    public CreateModel(
        LoanService loanService,
        BookService bookService,
        UserService userService)
    {
        _loanService = loanService;
        _bookService = bookService;
        _userService = userService;
    }

    public List<BookDto> Books { get; set; } = new();
    public List<UserListDto> Borrowers { get; set; } = new();

    [BindProperty]
    public int SelectedBorrowerId { get; set; }

    [BindProperty]
    public List<int> SelectedBookIds { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsInAnyRole("Admin", "Librarian"))
            return RedirectToPage("/AccessDenied");

        Books = await _bookService.GetAllAsync();
        Borrowers = await _userService.GetReadersAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsInAnyRole("Admin", "Librarian"))
            return RedirectToPage("/AccessDenied");

        if (SelectedBorrowerId <= 0 || SelectedBookIds == null || !SelectedBookIds.Any())
        {
            ErrorMessage = "Seleccione un lector y al menos un libro.";
            Books = await _bookService.GetAllAsync();
            Borrowers = await _userService.GetReadersAsync();
            return Page();
        }

        var (success, message, loanId) = await _loanService.CreateLoanAsync(SelectedBorrowerId, SelectedBookIds);

        if (!success)
        {
            ErrorMessage = message;
            Books = await _bookService.GetAllAsync();
            Borrowers = await _userService.GetReadersAsync();
            return Page();
        }

        TempData["Success"] = "Préstamo creado correctamente.";
        return RedirectToPage("/Loans/Index");
    }
}
