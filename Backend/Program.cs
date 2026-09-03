using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;

using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Persistence;
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


// ======================================================
// USERS - REGISTER
// ======================================================

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<IUserService, UserService>();


// ======================================================
// AUTHENTICATION - JWT
// ======================================================

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IAuthService, AuthService>();


// ======================================================
// JWT CONFIGURATION
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "La configuración 'Jwt:Key' no fue encontrada."
    );
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];

var jwtAudience = builder.Configuration["Jwt:Audience"];


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    )
            };
    });


builder.Services.AddAuthorization();


// ======================================================
// BUILD APP
// ======================================================

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