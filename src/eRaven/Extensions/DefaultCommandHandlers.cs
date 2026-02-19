//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultCommandHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Application.Commands.Excel;
using eRaven.Application.Commands.Missions;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Application.Handlers.Missions;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Handlers.Timesheets;

namespace eRaven.Extensions;

public static class DefaultCommandHandlers
{
    public static IServiceCollection AddRegistredCommandHandlers(this IServiceCollection services)
    {
        // Persons
        services.AddScoped<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>, BootstrapPersonsCommandHandler>();
        services.AddScoped<ICommandHandler<CreateReservedCommand, Guid>, CreateReservedCommandHandler>();
        services.AddScoped<ICommandHandler<EnrollCommand>, EnrollCommandHandler>();
        services.AddScoped<ICommandHandler<ExcludeCommand>, ExcludeCommandHandler>();

        services.AddScoped<ICommandHandler<UpdatePersonalInfoCommand>, UpdatePersonalInfoCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeRankCommand>, ChangeRankCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePositionCommand>, ChangePositionCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeBzvpCommand>, ChangeBzvpCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeWeaponCommand>, ChangeWeaponCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeCallsignCommand>, ChangeCallsignCommandHandler>();
        services.AddScoped<ICommandHandler<VoidPersonEventCommand>, VoidPersonEventCommandHandler>();

        // Timesheet
        services.AddScoped<ICommandHandler<TransitionTimesheetStateCommand, Guid>, TransitionTimesheetStateCommandHandler>();

        services.AddScoped<ICommandHandler<AddTimesheetCodeCommand, Guid>, AddTimesheetCodeCommandHandler>();
        services.AddScoped<ICommandHandler<CloseTimesheetCodeCommand>, CloseTimesheetCodeCommandHandler>();
        services.AddScoped<ICommandHandler<SaveTimesheetPolicyCommand>, SaveTimesheetPolicyCommandHandler>();

        // Combat tasks (documents)
        services.AddScoped<ICommandHandler<CreateCombatTaskDocumentCommand, Guid>, CreateCombatTaskDocumentCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCombatTaskCommand, Guid>, CreateCombatTaskCommandHandler>();

        // Missions
        services.AddScoped<ICommandHandler<CreateMissionCommand, Guid>, CreateMissionCommandHandler>();
        services.AddScoped<ICommandHandler<CloseMissionCommand>, CloseMissionCommandHandler>();


        services.AddScoped<ICommandHandler<AddTimesheetCodeCommand, Guid>, AddTimesheetCodeCommandHandler>();
        services.AddScoped<ICommandHandler<CloseTimesheetCodeCommand>, CloseTimesheetCodeCommandHandler>();
        services.AddScoped<ICommandHandler<SaveTimesheetPolicyCommand>, SaveTimesheetPolicyCommandHandler>();

        return services;
    }
}
