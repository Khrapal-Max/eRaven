//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using Blazored.Toast;
using eRaven.Application.Services.ConfirmService;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.Services.PositionService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Components;
using eRaven.Components.Pages.Persons;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Components.Pages.Positions.Modals;
using eRaven.Components.Pages.StatusTransitions.Modals;
using eRaven.Extensions;
using eRaven.Infrastructure;
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

builder.Services.AddBlazoredToast();

builder.Services.AddTransient<IValidator<EditPersonViewModel>, EditPersonViewModelValidator>();
builder.Services.AddTransient<IValidator<CreateKindViewModel>, CreateKindViewModelValidator>();
builder.Services.AddTransient<IValidator<CreatePersonViewModel>, CreatePersonViewModelValidator>();
builder.Services.AddTransient<IValidator<CreatePositionUnitViewModel>, CreatePositionUnitViewModelValidator>();

//Services
builder.Services.AddScoped<IConfirmService, ConfirmService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IPlanActionService, PlanActionService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IPersonStatusService, PersonStatusService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IPositionAssignmentService, PositionAssignmentService>();
builder.Services.AddScoped<IStatusKindService, StatusKindService>();
builder.Services.AddScoped<IStatusTransitionService, StatusTransitionService>();
builder.Services.AddScoped<IPersonStatusReadService, PersonStatusReadService>();

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
