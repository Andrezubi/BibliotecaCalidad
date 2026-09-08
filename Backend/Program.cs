using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
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
// DATABASE
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
// BASE REPOSITORY
// ======================================================

builder.Services.AddScoped(
    typeof(IBaseRepository<>),
    typeof(BaseRepository<>)
);

// ======================================================
// BOOK
// ======================================================

builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookService, BookService>();

// ======================================================
// LOAN
// ======================================================

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

// ======================================================
// USER
// ======================================================

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// ======================================================
// AUTHENTICATION
// ======================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// ======================================================
// JWT
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "La clave Jwt:Key no está configurada."
    );
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
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
            ),

        ClockSkew = TimeSpan.Zero
    };
});

// ======================================================
// BUILD
// ======================================================

var app = builder.Build();

// ======================================================
// HTTP PIPELINE
// ======================================================

app.UseHttpsRedirection();
// para imagenes 
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();