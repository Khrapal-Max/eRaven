//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupEndDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer закриття групи участей (ставить To та EndSourceDocNo).
/// </summary>
public partial class CombatGroupEndDrawer
{
    [Inject] public ToastService Toasts { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public CombatEntryDetailsDto? Group { get; set; }

    [Parameter] public EventCallback<EndCombatTaskGroupModel> OnEnded { get; set; }

    private DateOnly _to = DateOnly.FromDateTime(DateTime.Now);
    private string _endSourceDocNo = string.Empty;

    protected override void OnParametersSet()
    {
        if (!IsOpen || Group is null) return;

        _to = DateOnly.FromDateTime(DateTime.Now);
        _endSourceDocNo = string.Empty;
    }

    private bool CanSave
        => Group is not null
           && !string.IsNullOrWhiteSpace(_endSourceDocNo)
           && _to >= Group.From;

    private async Task Save()
    {
        if (Group is null) return;

        try
        {
            if (_to < Group.From)
                throw new InvalidOperationException("Дата завершення не може бути раніше дати початку.");

            await OnEnded.InvokeAsync(new EndCombatTaskGroupModel(
                GroupId: Group.GroupId,
                To: _to,
                EndSourceDocNo: _endSourceDocNo.Trim()));

            await Close();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
    }

    private Task Close()
        => IsOpenChanged.InvokeAsync(false);
}
