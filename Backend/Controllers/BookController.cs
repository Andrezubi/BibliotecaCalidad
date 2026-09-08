using Backend.Application.DTOs;
using Backend.Application.Interfaces;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
    {
        var books = await _bookService.GetAllAsync();

        return Ok(books);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetById(int id)
    {
        var book = await _bookService.GetByIdAsync(id);

        if (book == null)
            return NotFound();

        return Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(CreateBookDto dto)
    {
        var book = await _bookService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = book.Id },
            book
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BookDto>> Update(
        int id,
        UpdateBookDto dto)
    {
        var book = await _bookService.UpdateAsync(id, dto);

        if (book == null)
            return NotFound();

        return Ok(book);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _bookService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }


    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDto>>> Search(
    [FromQuery] string? title = null,
    [FromQuery] string? author = null,
    [FromQuery] string? category = null,
    [FromQuery] string? isbn = null,
    [FromQuery] string? publisher = null)
    {
        var books = await _bookService.SearchAsync(
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