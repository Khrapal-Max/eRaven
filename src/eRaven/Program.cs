//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Program
//-----------------------------------------------------------------------------

using Blazored.Toast;
using eRaven.Application.CommandHandlers;
using eRaven.Application.CommandHandlers.Persons;
using eRaven.Application.CommandHandlers.PlanActions;
using eRaven.Application.CommandHandlers.PositionAssignments;
using eRaven.Application.CommandHandlers.Positions;
using eRaven.Application.CommandHandlers.StatusKinds;
using eRaven.Application.Commands.Persons;
using eRaven.Application.Commands.PlanActions;
using eRaven.Application.Commands.PositionAssignments;
using eRaven.Application.Commands.Positions;
using eRaven.Application.Commands.StatusKinds;
using eRaven.Application.DTOs;
using eRaven.Application.Queries.Persons;
using eRaven.Application.Queries.Positions;
using eRaven.Application.Queries.StatusKinds;
using eRaven.Application.QueryHandlers;
using eRaven.Application.QueryHandlers.Persons;
using eRaven.Application.QueryHandlers.Positions;
using eRaven.Application.QueryHandlers.StatusKinds;
using eRaven.Application.Repositories;
using eRaven.Components;
using eRaven.Domain.Services;
using eRaven.Extensions;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories;
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

// ========== REPOSITORIES ==========
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IPositionUnitRepository, PositionUnitRepository>();
builder.Services.AddScoped<IStatusKindRepository, StatusKindRepository>();
builder.Services.AddScoped<IStatusTransitionRepository, StatusTransitionRepository>();

// ========== DOMAIN SERVICES ==========
builder.Services.AddScoped<IStatusTransitionValidator, StatusTransitionValidator>();
builder.Services.AddScoped<IPositionAssignmentPolicy, PositionAssignmentPolicy>();

// ========== COMMAND HANDLERS - PERSONS ==========
builder.Services.AddScoped<ICommandHandler<CreatePersonCommand, Guid>,
    CreatePersonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdatePersonInfoCommand>,
    UpdatePersonInfoCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ChangePersonStatusCommand>,
    ChangePersonStatusCommandHandler>();

// ========== QUERY HANDLERS - PERSONS ==========
builder.Services.AddScoped<IQueryHandler<GetPersonByIdQuery, PersonDto?>,
    GetPersonByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<SearchPersonsQuery, IReadOnlyList<PersonDto>>,
    SearchPersonsQueryHandler>();

// ========== COMMAND HANDLERS - POSITIONS ==========
builder.Services.AddScoped<ICommandHandler<CreatePositionCommand, Guid>,
    CreatePositionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetPositionActiveStateCommand>,
    SetPositionActiveStateCommandHandler>();

// ========== QUERY HANDLERS - POSITIONS ==========
builder.Services.AddScoped<IQueryHandler<GetAllPositionsQuery, IReadOnlyList<PositionDto>>,
    GetAllPositionsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetPositionByIdQuery, PositionDto?>,
    GetPositionByIdQueryHandler>();

// ========== COMMAND HANDLERS - POSITION ASSIGNMENTS ==========
builder.Services.AddScoped<ICommandHandler<AssignToPositionCommand>,
    AssignToPositionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UnassignFromPositionCommand>,
    UnassignFromPositionCommandHandler>();

// ========== COMMAND HANDLERS - PLAN ACTIONS ==========
builder.Services.AddScoped<ICommandHandler<CreatePlanActionCommand, Guid>,
    CreatePlanActionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ApprovePlanActionCommand>,
    ApprovePlanActionCommandHandler>();

// ========== COMMAND HANDLERS - STATUS KINDS ==========
builder.Services.AddScoped<ICommandHandler<CreateStatusKindCommand, int>,
    CreateStatusKindCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateStatusKindOrderCommand>,
    UpdateStatusKindOrderCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetStatusKindActiveCommand>,
    SetStatusKindActiveCommandHandler>();

// ========== QUERY HANDLERS - STATUS KINDS ==========
builder.Services.AddScoped<IQueryHandler<GetAllStatusKindsQuery, IReadOnlyList<StatusKindDto>>,
    GetAllStatusKindsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetStatusKindByIdQuery, StatusKindDto?>,
    GetStatusKindByIdQueryHandler>();

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
