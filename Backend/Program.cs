using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;

using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Repositories;
using Backend.Infrastructure.Repositories;
using Backend.Infrastructure.Repositories;
using Backend.Infrastructure.Security;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;


var builder = WebApplication.CreateBuilder(args);


// ======================================================
// CONTROLLERS
// ======================================================

builder.Services.AddControllers();


// ======================================================
// DATABASE - MYSQL
// ======================================================

var connectionString =
    builder.Configuration.GetConnectionString("LibraryDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "La cadena de conexión 'LibraryDatabase' no está configurada."
    );
}

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);


// ======================================================
// GENERIC REPOSITORY
// ======================================================

builder.Services.AddScoped(
    typeof(IBaseRepository<>),
    typeof(BaseRepository<>)
);


// ======================================================
// LOANS
// ======================================================

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

var app = builder.Build();


// ======================================================
// HTTP PIPELINE
// ======================================================

app.UseHttpsRedirection();


// Primero autenticamos
app.UseAuthentication();

// Después comprobamos permisos
app.UseAuthorization();


app.MapControllers();


app.Run();