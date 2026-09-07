using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet("loaned-books")]
        public async Task<IActionResult> GetLoanedBooks()
        {
            var books = await _loanService.GetLoanedBooksAsync();
            return Ok(books);
        }

        [HttpPost("{copyId}/return")]
        public async Task<IActionResult> ReturnBook(int copyId)
        {
            var result = await _loanService.ReturnLoanAsync(copyId);
            if (!result)
            {
                return BadRequest(new { message = "No se pudo registrar la devolución del libro." });
            }

            return Ok(new { message = "Libro devuelto exitosamente." });
        }
    
    [HttpPost("book/{bookId}/loan")]
        public async Task<IActionResult> LoanBook(int bookId)
        {
            var result = await _loanService.LoanBookAsync(bookId);
            if (!result)
            {
                return BadRequest(new { message = "No hay copias disponibles para prestar." });
            }

            return Ok(new { message = "Libro marcado como prestado exitosamente." });
        }
    }
}