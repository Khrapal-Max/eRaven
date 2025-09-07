//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using Blazored.Toast;
using eRaven.Application.ExcelService;
using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Components;
using eRaven.Components.Pages.Positions.Modals;
using eRaven.Extensions;
using eRaven.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor + Blazor Server (Interactive)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt => { opt.DetailedErrors = true; });

// DbContext (рядок підключення можна пробросити через env: ConnectionStrings__DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddBlazoredToast();
builder.Services.AddTransient<IValidator<CreatePositionUnitViewModel>, CreatePositionUnitViewModelValidator>();

//Services
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IPositionService, PositionService>();

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

app.Run();
