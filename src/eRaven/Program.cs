//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Handlers;
using eRaven.Components;
using eRaven.Domain.Entities;
using eRaven.Domain.Validation;
using eRaven.Extensions;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Excel;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Infrastructure.Repositories.RankRepository;
using eRaven.Presentation.Errors;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor + Blazor Server (Interactive)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt => { opt.DetailedErrors = true; });

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); // або ваш провайдер
});

// Validation
builder.Services.AddScoped<IValidator<PositionUnit>, PositionUnitValidator>();
builder.Services.AddScoped<IValidator<Rank>, RankValidator>();

// Add services to the container.
// Repository
builder.Services.AddScoped<IPositionUnitRepository, PositionUnitRepository>();
builder.Services.AddScoped<IRankRepository, RankRepository>();

// services
builder.Services.AddScoped<IPositionUnitExcelService, PositionUnitExcelService>();

builder.Services.AddScoped<CreateCandidateCommandHandler>();

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
