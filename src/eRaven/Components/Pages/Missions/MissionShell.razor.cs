//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Missions;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Missions;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Missions;

public partial class MissionShell : ComponentBase
{
    //=======================
    // DI
    //=======================
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> MissionQueryHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //=======================
    // UI State
    //=======================
    private bool _loading;
    private bool _isActiveMission = true; // зазвичай стартуємо з активних
    private string? _search;
    private MissionModeDto? _mode;

    private bool _createOpen;
    private bool _closeOpen;

    private MissionDto? _selected;
    private MissionDto? _selectedToClose;

    //=======================
    // Data
    //=======================
    private IReadOnlyList<MissionDto> _missions = [];

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    /// <summary>
    /// Перезавантажує таблицю місій з урахуванням фільтрів (active/search/mode).
    /// </summary>
    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _missions = await MissionQueryHandler.HandleAsync(new GetMissionsQuery(
                OnlyOpen: _isActiveMission,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim(),
                Mode: _mode));

            // якщо після reload selected “випав” — знімаємо
            if (_selected is not null && _missions.All(x => x.MissionId != _selected.MissionId))
                _selected = null;
        }
        catch (Exception ex)
        {
            _missions = [];
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    //=======================
    // Actions
    //=======================
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private async Task OnModeChanged(ChangeEventArgs e)
    {
        var raw = Convert.ToString(e.Value) ?? "";
        if (Enum.TryParse<MissionModeDto>(raw, out var mode))
        {
            _mode = mode;
        }
        else
        {
            _mode = null;
        }

        await ReloadAsync();
    }

    private async Task ToggleActiveAsync()
    {
        _isActiveMission = !_isActiveMission;
        await ReloadAsync();
    }

    private Task OpenCreateDrawer()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenCloseDrawer(MissionDto m)
    {
        _selectedToClose = m;
        _closeOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreatedAsync(Guid _)
        => await ReloadAsync();

    private async Task HandleClosedAsync()
        => await ReloadAsync();

    //=======================
    // Helpers
    //=======================
    private static string GetMissionMode(MissionModeDto mode)
       => mode switch
       {
           MissionModeDto.Day => "День",
           MissionModeDto.Night => "Ніч",
           MissionModeDto.FullTime => "Цілодобово",
           _ => "Всі види"
       };
}
