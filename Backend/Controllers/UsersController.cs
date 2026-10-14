using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request)
    {
        try
        {
            var userId =
                await _userService.RegisterAsync(request);

            return Ok(new
            {
                message = "Usuario registrado correctamente.",
                userId
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

        [HttpGet("readers")]
        public async Task<IActionResult> GetReaders()
        {
            var readers = await _userService.GetReadersAsync();
            return Ok(readers);
        }
}