//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.DTOs;
using eRaven.Domain.Aggregates;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;

namespace eRaven.Application.Handlers;

public sealed class CreateCandidateCommandHandler(
    IPersonRepository repo,
    IPositionUnitRepository positionUnits)
    : ICommandHandler<CreatePersonCandidateCommand, Guid>
{
    private readonly IPersonRepository _repo = repo;
    private readonly IPositionUnitRepository _positionUnits = positionUnits;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();

        var personal = new PersonalInfo(command.Rnokpp, command.LastName, command.FirstName, command.MiddleName);

        var plannedPosition = Normalize(command.PlannedPosition);

        if (plannedPosition is null && command.PlannedPositionUnitId is Guid plannedId)
        {
            var option = await _positionUnits.GetOptionByIdAsync(plannedId, ct)
                ?? throw new InvalidOperationException("Планова посада не знайдена.");

            plannedPosition = FormatPlannedPosition(option);
        }

        var agg = PersonAggregate.CreateCandidate(
            id: id,
            personal: personal,
            plannedPositionUnitId: command.PlannedPositionUnitId,
            plannedPosition: plannedPosition,
            author: "author",
            nowUtc: DateTime.UtcNow);

        await _repo.SaveAsync(
            agg,
            expectedVersion: 0,
            ct: ct);

        return id;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatPlannedPosition(PositionUnitOptionDto option)
        => !string.IsNullOrWhiteSpace(option.ShortName)
            ? option.ShortName.Trim()
            : !string.IsNullOrWhiteSpace(option.FullName)
                ? option.FullName.Trim()
                : option.Code.Trim();
}
