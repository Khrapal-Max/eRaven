//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Handlers;
using eRaven.Application.Queries;
using eRaven.Application.Validations;
using eRaven.Components;
using eRaven.Extensions;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Projectors;
using eRaven.Infrastructure.Repositories.PersonRepository;
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

// Catalogs
builder.Services.AddScoped<IRankCatalog, DefaultRankCatalog>();

// Validators
builder.Services.AddScoped<IValidator<CreateReservedDto>, CreateReservedDtoValidator>();
builder.Services.AddScoped<IValidator<EnrollDto>, EnrollDtoValidator>();
builder.Services.AddScoped<IValidator<ExcludeDto>, ExcludeDtoValidator>();

// Projector (stateless)
builder.Services.AddSingleton<IPersonReadModelProjector, PersonReadModelProjector>();

// Repository
builder.Services.AddScoped<IPersonRepository, PersonRepository>();

// Command handlers
builder.Services.AddScoped<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>, BootstrapPersonsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateReservedCommand, Guid>, CreateReservedCommandHandler>();
builder.Services.AddScoped<ICommandHandler<EnrollCommand>, EnrollCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ExcludeCommand>, ExcludeCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ChangeRankCommand>, ChangeRankCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ChangePositionCommand>, ChangePositionCommandHandler>();


// Query handlers
builder.Services.AddScoped<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>, GetPersonsPageQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>, GetPersonDetailsQueryHandler>();

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
