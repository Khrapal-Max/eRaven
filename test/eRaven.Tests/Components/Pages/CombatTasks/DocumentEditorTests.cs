//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Components.Pages.CombatTasks;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.CombatTasks;

public sealed class DocumentEditorTests : BunitContext
{
    private readonly ToastService _toastService;
    private readonly Mock<IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?>> _query;

    public DocumentEditorTests()
    {
        _toastService = new ToastService();
        _query = new(MockBehavior.Strict);

        Services.AddSingleton(_query.Object);
        Services.AddSingleton(_toastService);
    }

    [Fact]
    public void Render_ShowsLoading_ThenRendersDocumentAndMissions_WhenQueryCompletes()
    {
        var docId = Guid.NewGuid();
        var dto = BuildDto(docId);

        var tcs = new TaskCompletionSource<CombatTaskEditorDto?>();
        _query
            .Setup(x => x.HandleAsync(
                It.Is<GetCombatTaskDetailsByDocumentIdQuery>(q => q.DocumentId == docId),
                It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var ctx = Render<DocumentEditor>(ps => ps.Add(p => p.DocumentId, docId));

        // Initial render (async work in progress)
        Assert.Contains("Завантаження", ctx.Markup);

        // Complete async query
        tcs.SetResult(dto);

        ctx.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Завантаження", ctx.Markup);
            Assert.Contains(dto.DocumentName, ctx.Markup);
            Assert.Contains("Місія A", ctx.Markup);
            Assert.Contains("Місія B", ctx.Markup);
        });

        _query.VerifyAll();

        Assert.NotNull(ctx.Instance.GetCombatTaskDetailsByDocumentIdQueryHandler);
        Assert.NotNull(ctx.Instance.NavigationManager);
        Assert.NotNull(ctx.Instance.ToastService);
    }

    [Fact]
    public void Render_UsesDocumentIdParameter_ForQuery_AndButtonsNavigateToStartAndClose()
    {
        var docId = Guid.NewGuid();

        _query
            .Setup(x => x.HandleAsync(
                It.Is<GetCombatTaskDetailsByDocumentIdQuery>(q => q.DocumentId == docId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildDto(docId));

        var cut = Render<DocumentEditor>(ps => ps.Add(p => p.DocumentId, docId));
        cut.WaitForAssertion(() => Assert.Contains("Розпорядження", cut.Markup));

        var nav = Services.GetRequiredService<NavigationManager>();

        // Start form button (green)
        cut.Find("button.btn-success").Click();
        Assert.Contains($"/task-document/{docId}/start", nav.Uri, StringComparison.Ordinal);

        // Close form button (warning)
        cut.Find("button.btn-warning").Click();
        Assert.Contains($"/task-document/{docId}/close", nav.Uri, StringComparison.Ordinal);

        _query.VerifyAll();
    }

    [Fact]
    public void Render_WhenQueryReturnsNull_ShowsNotFoundBlock_AndRaisesWarningToast()
    {
        var toast = new ToastService();
        ToastMessage? last = null;
        toast.OnShow += m => last = m;

        // Override toast service registered in ctor.
        Services.AddSingleton(toast);

        var docId = Guid.NewGuid();
        _query
            .Setup(x => x.HandleAsync(
                It.Is<GetCombatTaskDetailsByDocumentIdQuery>(q => q.DocumentId == docId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CombatTaskEditorDto?)null);

        var cut = Render<DocumentEditor>(ps => ps.Add(p => p.DocumentId, docId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Документ не знайдено", cut.Markup);
            Assert.NotNull(last);
            Assert.Equal(ToastKind.Warning, last!.Kind);
            Assert.Contains("Документ не знайдено", last!.Title);
        });

        _query.VerifyAll();
    }

    [Fact]
    public void Render_SortsDetailsInsideMission_EndBeforeStart_OnSameDate()
    {
        var docId = Guid.NewGuid();
        var dto = BuildDto(docId);

        _query
            .Setup(x => x.HandleAsync(
                It.Is<GetCombatTaskDetailsByDocumentIdQuery>(q => q.DocumentId == docId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Ensure input is Start then End for the same date (the UI sorting should render End row first).
        var missionA = dto.Missions.Single(m => m.MissionName == "Місія A");
        var sameDay = missionA.CombatTaskDetails.Where(x => x.EffectiveAt == new DateOnly(2026, 1, 2)).ToList();
        Assert.Equal(2, sameDay.Count);
        Assert.Equal(CombatTaskDetailsKindDto.Start, sameDay[0].Kind);
        Assert.Equal(CombatTaskDetailsKindDto.End, sameDay[1].Kind);

        var cut = Render<DocumentEditor>(ps => ps.Add(p => p.DocumentId, docId));
        cut.WaitForAssertion(() => Assert.Contains("Місія A", cut.Markup));

        // Assert within rows for 02.01.2026: End row comes before Start row.
        var date = "02.01.2026";
        var rows = cut.FindAll("tr")
            .Where(r => r.TextContent.Contains(date, StringComparison.Ordinal))
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Contains("🟨", rows[0].TextContent);
        Assert.Contains("🟩", rows[1].TextContent);

        _query.VerifyAll();
    }

    private static CombatTaskEditorDto BuildDto(Guid documentId)
    {
        var missionAId = Guid.NewGuid();
        var missionBId = Guid.NewGuid();

        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();

        // Mission A: intentionally unsorted (Start then End on same date) - UI will sort.
        var missionA = new CombatTaskMissionBlockDto(
            CombatTaskId: Guid.NewGuid(),
            MissionId: missionAId,
            MissionName: "Місія A",
            SourceDocument: "Наказ-1",
            CombatTaskDetails:
            [
                new(
                    CombatTaskDetailsId: Guid.NewGuid(),
                    Kind: CombatTaskDetailsKindDto.Start,
                    EffectiveAt: new DateOnly(2026, 1, 2),
                    PersonId: personAId,
                    Rnokpp: "0000000001",
                    FullName: "Людина A",
                    Rank: null,
                    Position: null,
                    Weapon: null,
                    Callsign: "A"),
                new(
                    CombatTaskDetailsId: Guid.NewGuid(),
                    Kind: CombatTaskDetailsKindDto.End,
                    EffectiveAt: new DateOnly(2026, 1, 2),
                    PersonId: personAId,
                    Rnokpp: "0000000001",
                    FullName: "Людина A",
                    Rank: null,
                    Position: null,
                    Weapon: null,
                    Callsign: "A"),
                new(
                    CombatTaskDetailsId: Guid.NewGuid(),
                    Kind: CombatTaskDetailsKindDto.Start,
                    EffectiveAt: new DateOnly(2026, 1, 1),
                    PersonId: personBId,
                    Rnokpp: "0000000002",
                    FullName: "Людина B",
                    Rank: null,
                    Position: null,
                    Weapon: null,
                    Callsign: "B"),
            ]);

        var missionB = new CombatTaskMissionBlockDto(
            CombatTaskId: Guid.NewGuid(),
            MissionId: missionBId,
            MissionName: "Місія B",
            SourceDocument: "Наказ-2",
            CombatTaskDetails:
            [
                new(
                    CombatTaskDetailsId: Guid.NewGuid(),
                    Kind: CombatTaskDetailsKindDto.Start,
                    EffectiveAt: new DateOnly(2026, 1, 3),
                    PersonId: Guid.NewGuid(),
                    Rnokpp: "0000000003",
                    FullName: "Людина C",
                    Rank: null,
                    Position: null,
                    Weapon: null,
                    Callsign: "C"),
            ]);

        return new CombatTaskEditorDto(
            DocumentId: documentId,
            DocumentName: "Розпорядження №123",
            Description: "Опис",
            Status: DocumentStatusDto.Active,
            RecordedAt: new DateOnly(2026, 1, 2),
            Missions: [missionA, missionB]);
    }
}
