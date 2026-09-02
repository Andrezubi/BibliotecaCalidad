using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();