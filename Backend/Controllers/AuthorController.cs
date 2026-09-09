using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorController : ControllerBase
    {
        private readonly IAuthorService _authorService;

        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Author>>> GetAll()
        {
            var authors = await _authorService.GetAllAsync();

            return Ok(authors);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Author>> GetById(int id)
        {
            var author = await _authorService.GetByIdAsync(id);

            if (author == null)
                return NotFound();

            return Ok(author);
        }

        [HttpPost]
        public async Task<ActionResult<Author>> Create(
            CreateAuthorDto dto)
        {
            try
            {
                var author = await _authorService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = author.Id },
                    author);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Author>> Update(
            int id,
            CreateAuthorDto dto)
        {
            try
            {
                var author =
                    await _authorService.UpdateAsync(id, dto);

                if (author == null)
                    return NotFound();

                return Ok(author);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _authorService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
