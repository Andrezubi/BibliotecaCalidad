using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Application.Validators;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Repositories;
using Backend.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<RegisterUserValidator>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();