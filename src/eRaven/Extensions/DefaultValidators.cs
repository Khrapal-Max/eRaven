//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultValidators
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using FluentValidation;

namespace eRaven.Extensions;

public static class DefaultValidators
{
    public static IServiceCollection AddRegistredValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateReservedDto>, CreateReservedDtoValidator>();
        services.AddScoped<IValidator<EnrollDto>, EnrollDtoValidator>();
        services.AddScoped<IValidator<ExcludeDto>, ExcludeDtoValidator>();

        services.AddScoped<IValidator<UpdatePersonalInfoDto>, UpdatePersonalInfoDtoValidator>();
        services.AddScoped<IValidator<ChangeRankDto>, ChangeRankDtoValidator>();
        services.AddScoped<IValidator<ChangePositionDto>, ChangePositionDtoValidator>();
        services.AddScoped<IValidator<ChangeBzvpDto>, ChangeBzvpDtoValidator>();
        services.AddScoped<IValidator<ChangeWeaponDto>, ChangeWeaponDtoValidator>();
        services.AddScoped<IValidator<ChangeCallsingDto>, ChangeCallsingDtoValidator>();

        services.AddSingleton<IValidator<VoidPersonEventDto>, VoidPersonEventDtoValidator>();

        return services;
    }
}
