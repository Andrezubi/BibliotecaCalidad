using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 1. Get connection string
// Use "LibraryDatabase" to match your appsettings.json key exactly
var connectionString = builder.Configuration.GetConnectionString("LibraryDatabase");

// 2. Register Pomelo MySQL Context
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. Register the Generic Repository
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));



builder.Services.AddScoped<LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();