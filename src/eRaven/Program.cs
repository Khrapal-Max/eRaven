//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using eRaven.Components;
using eRaven.Extensions;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor + Blazor Server (Interactive)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt => { opt.DetailedErrors = true; });

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); // або ваш провайдер
});

// Add services to the container.
// Repository
builder.Services.AddTransient<IPositionUnitRepository, PositionUnitRepository>();

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
