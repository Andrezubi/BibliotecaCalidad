using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
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
        [HttpPost("return/{copyId}")]
        public async Task<IActionResult> RegisterReturn(int copyId)
        {
            var result = await _loanService.RegisterReturnAsync((uint)copyId);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "No se pudo registrar la devolución. " +
                              "La copia no existe o no está prestada."
                });
            }

            return Ok(new
            {
                message = "Devolución registrada. El libro ahora está disponible."
            });
        }
        [HttpPost("loan-book/{bookId}")]
        public async Task<IActionResult> RegisterLoan(int bookId)
        {
            var result = await _loanService.RegisterLoanAsync((uint)bookId);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "No se pudo registrar el préstamo. " +
                              "El libro no tiene copias disponibles."
                });
            }

            return Ok(new
            {
                message = "Préstamo registrado. El libro ahora está prestado."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var loans = await _loanService.GetAllLoansAsync();
            return Ok(loans);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var loan = await _loanService.GetLoanByIdAsync((uint)id);

            if (loan == null)
                return NotFound();

            return Ok(loan);
        }
    }

}