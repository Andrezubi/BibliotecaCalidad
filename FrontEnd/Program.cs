using FrontEnd.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// SERVICES
// ======================================================

builder.Services.AddRazorPages();

// ======================================================
// SESSION
// ======================================================

// Cache necesaria para Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Permite acceder a HttpContext desde los Services
builder.Services.AddHttpContextAccessor();


// ======================================================
// HTTP CLIENT - BOOKS
// ======================================================

builder.Services.AddHttpClient<BookService>(client =>
{
    var backendUrl = builder.Configuration["BackendUrl"];

    if (string.IsNullOrWhiteSpace(backendUrl))
    {
        throw new InvalidOperationException(
            "No se encontró 'BackendUrl' en appsettings.json.");
    }

    client.BaseAddress = new Uri(backendUrl);
});
builder.Services.AddHttpClient<AuthorService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["BackendUrl"]!
    );
});

builder.Services.AddHttpClient<CategoryService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["BackendUrl"]!
    );
});

// ======================================================
// HTTP CLIENT - LOANS
// ======================================================

builder.Services.AddHttpClient<LoanService>();


// ======================================================
// HTTP CLIENT - AUTH
// ======================================================

builder.Services.AddHttpClient<AuthApiService>();

builder.Services.AddScoped<AuthSessionService>();

var app = builder.Build();


// ======================================================
// HTTP PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


// ======================================================
// SESSION
// ======================================================

app.UseSession();


// ======================================================
// PROTECCIÓN DE PÁGINAS
// ======================================================

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // --------------------------------------------------
    // PÁGINAS Y RECURSOS PÚBLICOS
    // --------------------------------------------------

    var isPublicPage =
        path.StartsWithSegments("/Auth/Login") ||
        path.StartsWithSegments("/Auth/Register") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/favicon.ico") ||
        path.StartsWithSegments("/Error");

    // --------------------------------------------------
    // TOKEN DE AUTENTICACIÓN
    // --------------------------------------------------

    var token =
        context.Session.GetString("AuthToken");

    // --------------------------------------------------
    // SI NO ESTÁ AUTENTICADO
    // --------------------------------------------------

    if (string.IsNullOrEmpty(token) && !isPublicPage)
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});


// ======================================================
// AUTHORIZATION
// ======================================================

app.UseAuthorization();


// ======================================================
// RAZOR PAGES
// ======================================================

app.MapRazorPages();


// ======================================================
// RUN
// ======================================================

app.Run();