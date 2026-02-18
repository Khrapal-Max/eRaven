//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using eRaven.Application.EventJson;
using eRaven.Application.Presenter;
using eRaven.Components;
using eRaven.Extensions;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Projections.Person;
using eRaven.Presentation.Errors;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor + Blazor Server (Interactive)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt => { opt.DetailedErrors = true; });

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); // або ваш провайдер
});

// DataProtection keys must survive restarts and be shared across instances
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/data-protection-keys"));

// Stateless
builder.Services.AddSingleton<IPersonReadModelProjector, PersonReadModelProjector>();
builder.Services.AddSingleton<IEventJson, EventJson>();
builder.Services.AddScoped<IPersonEventPresenter, PersonEventPresenter>();

// Catalogs
builder.Services.AddRegistredCatalogs();
// Validators
builder.Services.AddRegistredValidators();
// Repository
builder.Services.AddRegistredRepositories();
// Command handlers
builder.Services.AddRegistredCommandHandlers();
// Query handlers
builder.Services.AddRegistredQueryHandlers();

// services
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ErrorBoundaryHub>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

await app.AddMigrationDb();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.ContentSecurityPolicy =
        "frame-ancestors 'none'";
    await next();
});

app.Run();
