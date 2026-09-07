using FrontEnd.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// SERVICES
// ======================================================
builder.Services.AddRazorPages();
builder.Services.AddSession();

var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7118/";

builder.Services.AddHttpClient<LoanService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<AuthApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<BookService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

// ======================================================
// BUILD APP (siempre después de registrar todos los servicios)
// ======================================================
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
        path.StartsWithSegments("/favicon.ico");

    var token = context.Session.GetString("AuthToken");

    if (string.IsNullOrEmpty(token) && !isPublicPage)
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();