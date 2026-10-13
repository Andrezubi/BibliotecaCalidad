using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublisherController : ControllerBase
    {
        private readonly IPublisherService _publisherService;

        public PublisherController(IPublisherService publisherService)
        {
            _publisherService = publisherService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Publisher>>> GetAll()
        {
            var publishers = await _publisherService.GetAllAsync();
            return Ok(publishers);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Publisher>> GetById(int id)
        {
            var publisher = await _publisherService.GetByIdAsync((uint)id);

            if (publisher == null)
                return NotFound();

            return Ok(publisher);
        }

        [HttpPost]
        public async Task<ActionResult<Publisher>> Create(
            CreatePublisherDto dto)
        {
            try
            {
                var publisher = await _publisherService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = publisher.Id }, publisher);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Publisher>> Update(
            int id,
            CreatePublisherDto dto)
        {
            try
            {
                var publisher = await _publisherService.UpdateAsync((uint)id, dto);

                if (publisher == null)
                    return NotFound();

                return Ok(publisher);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _publisherService.DeleteAsync((uint)id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}