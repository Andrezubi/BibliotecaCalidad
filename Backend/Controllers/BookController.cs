using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Validators;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;

    public BookController(IBookService bookService)
    {
        _bookService = bookService;
    }

    // ============================================================
    // GET ALL
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
    {
        var books = await _bookService.GetAllAsync();

        return Ok(books);
    }

    // ============================================================
    // GET BY ID
    // ============================================================

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetById(int id)
    {
        var book = await _bookService.GetByIdAsync(id);

        if (book == null)
            return NotFound();

        return Ok(book);
    }

    // ============================================================
    // CREATE
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(
        [FromForm] CreateBookDto dto)
    {
        try
        {
            var book =
                await _bookService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = book.Id },
                book);
        }
        catch (BookValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(
                        error.Key,
                        message);
                }
            }

            return ValidationProblem(ModelState);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return ValidationProblem(ModelState);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

    [HttpPut("{id}")]
    public async Task<ActionResult<BookDto>> Update(
        int id,
        [FromForm] UpdateBookDto dto)
    {
        try
        {
            var book =
                await _bookService.UpdateAsync(id, dto);

            if (book == null)
                return NotFound();

            return Ok(book);
        }
        catch (BookValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(
                        error.Key,
                        message);
                }
            }

            return ValidationProblem(ModelState);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return ValidationProblem(ModelState);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // DELETE
    // ============================================================

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted =
            await _bookService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ============================================================
    // SEARCH
    // ============================================================

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDto>>> Search(
        [FromQuery] string? title = null,
        [FromQuery] string? author = null,
        [FromQuery] string? category = null,
        [FromQuery] string? isbn = null,
        [FromQuery] string? publisher = null)
    {
        var books =
            await _bookService.SearchAsync(
                title,
                author,
                category,
                isbn,
                publisher);

        return Ok(books);
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAvailable()
    {
        var books = await _bookService.GetAvailableAsync();
        return Ok(books);
    }
}