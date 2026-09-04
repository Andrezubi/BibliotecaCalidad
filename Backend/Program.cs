using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// ============================================
// DATABASE
// ============================================

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

// ============================================
// BASE REPOSITORY
// ============================================

builder.Services.AddScoped(
    typeof(IBaseRepository<>),
    typeof(BaseRepository<>)
);

// ============================================
// BOOK
// ============================================

builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookService, BookService>();

// ============================================
// LOAN
// ============================================

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

// ============================================
// APPLICATION
// ============================================

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();