//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyConfiguratorTests (minimal)
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Timesheet;

public sealed class TimesheetPolicyConfiguratorTests : BunitContext
{
    private const string NB = "НБ";

    // -----------------------
    // Minimal service injection
    // -----------------------
    private static void AddServices(BunitContext ctx, ITimesheetPolicyRepository repo)
    {
        // 1) Repo used by component
        ctx.Services.AddSingleton(repo);

        // 2) Toast service used by component (even if test doesn't click Save)
        ctx.Services.AddSingleton(new ToastService());
    }

    private static TimesheetCodeDefinition Code(TimesheetLane lane, string code, string title, int sort)
        => new()
        {
            Id = Guid.NewGuid(),
            Lane = lane,
            Code = code,
            Title = title,
            SortOrder = sort,
            IsActive = true,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

    [Fact]
    public void Renders_Page_And_AutoSelects_First_Main_NonNB()
    {
        // arrange
        var mainNb = Code(TimesheetLane.Main, NB, "Поза табелем", 1);
        var main30 = Code(TimesheetLane.Main, "30", "В районі", 10);
        var taskX = Code(TimesheetLane.Task, "ПЛАН", "План", 10);

        var repo = new FakePolicyRepo(
            mainCodes: [mainNb, main30],
            taskCodes: [taskX]
        );

        AddServices(this, repo);

        // act
        var cut = Render<TimesheetPolicyConfigurator>();

        // assert (minimal: page rendered)
        cut.Markup.Contains("Політика переходів");

        // assert: selected code info shows 30 (not NB)
        Assert.Contains("30", cut.Markup);
        Assert.DoesNotContain("Lane: Main, Код: НБ", cut.Markup); // defensive check
    }

    [Fact]
    public void DoesNotShow_NB_In_LeftLists_And_RightTargets()
    {
        // arrange
        var mainNb = Code(TimesheetLane.Main, NB, "Поза табелем", 1);
        var main30 = Code(TimesheetLane.Main, "30", "В районі", 10);
        var mainVp = Code(TimesheetLane.Main, "ВП", "Відпустка", 20);

        var repo = new FakePolicyRepo(
            mainCodes: [mainNb, main30, mainVp],
            taskCodes: []
        );

        AddServices(this, repo);

        // act
        var cut = Render<TimesheetPolicyConfigurator>();

        // assert: NB is not shown in left buttons
        var leftButtons = cut.FindAll("div.col-4 button.list-group-item");
        Assert.DoesNotContain(leftButtons, b => b.TextContent.Contains(NB));

        // assert: NB is not shown in right checkbox labels
        // (right labels exist because there are targets besides selected)
        var rightLabels = cut.FindAll("label.form-check");
        Assert.DoesNotContain(rightLabels, l => l.TextContent.Contains(NB));
    }

    // -----------------------
    // Minimal fake repo
    // -----------------------
    private sealed class FakePolicyRepo(IEnumerable<TimesheetCodeDefinition> mainCodes, IEnumerable<TimesheetCodeDefinition> taskCodes) : ITimesheetPolicyRepository
    {
        private readonly List<TimesheetCodeDefinition> _main = [.. mainCodes];
        private readonly List<TimesheetCodeDefinition> _task = [.. taskCodes];

        public Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(TimesheetLane lane, CancellationToken ct = default)
        {
            IReadOnlyList<TimesheetCodeDefinition> res = lane == TimesheetLane.Main ? _main : _task;
            return Task.FromResult(res);
        }

        public Task<IReadOnlySet<Guid>> GetAllowedNextAsync(Guid fromCodeId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task SavePolicyAsync(
            TimesheetLane lane,
            Guid fromCodeId,
            TimesheetEndDateMeaning endDateMeaning,
            string? nextCodeOnEnd,
            IReadOnlyCollection<Guid> allowedToCodeIds,
            string author,
            DateTime nowUtc,
            CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
