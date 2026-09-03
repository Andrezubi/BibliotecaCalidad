using Backend.Domain.Models;

namespace Backend.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}