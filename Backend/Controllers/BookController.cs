using Microsoft.AspNetCore.Http;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;

    public BookController(IBookService bookService)
    {
        _bookService = bookService;
    }

    // GET: api/Book?onlyAvailable=true
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyAvailable = false)
    {
        try
        {
            var books = await _bookService.GetAllBooksAsync(onlyAvailable);
            return Ok(books);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener los libros", error = ex.Message });
        }
    }

    // GET: api/Book/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            return Ok(book);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Libro no encontrado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener el libro", error = ex.Message });
        }
    }
}