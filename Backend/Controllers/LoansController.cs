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
    }
}