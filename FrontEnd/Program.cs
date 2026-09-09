using FrontEnd.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// SERVICES
// ======================================================

builder.Services.AddRazorPages();

// Cache necesaria para Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor(); 
// ======================================================
// HTTP CLIENTS
// ======================================================

builder.Services.AddHttpClient<BookService>(client =>
{
    var backendUrl = builder.Configuration["BackendUrl"];

    client.BaseAddress = new Uri(backendUrl!);
});

builder.Services.AddHttpClient<LoanService>();

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

app.UseSession();

// ======================================================
// PROTECCIÓN DE PÁGINAS
// ======================================================

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    var isPublicPage =
        path.StartsWithSegments("/Auth/Login") ||
        path.StartsWithSegments("/Auth/Register") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/favicon.ico") ||
        path.StartsWithSegments("/Error");

    var token = context.Session.GetString("AuthToken");

    if (string.IsNullOrEmpty(token) && !isPublicPage)
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapRazorPages();

app.Run();