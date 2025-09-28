// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PersonStatusImportViewTests
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonStatusViewModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace eRaven.Tests.Application.Tests.ViewModels.Excel;

public class PersonStatusImportViewTests
{
    [Fact(DisplayName = "PersonStatusImportView: значення за замовчуванням коректні")]
    public void Defaults_AreExpected()
    {
        // Arrange
        var vm = new PersonStatusImportView();

        // Assert
        Assert.Null(vm.Rnokpp);
        Assert.Null(vm.StatusKindId);
        Assert.Equal(default, vm.FromDateLocal); // 0001-01-01
        Assert.Null(vm.Note);
    }

    [Fact(DisplayName = "PersonStatusImportView: атрибути Display(Name) встановлені очікувано")]
    public void DisplayAttributes_AreCorrect()
    {
        // Arrange
        var t = typeof(PersonStatusImportView);

        // Act
        string DisplayName(string prop) =>
            t.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)!
             .GetCustomAttribute<DisplayAttribute>()?.Name!;

        // Assert
        Assert.Equal("RNOKPP", DisplayName(nameof(PersonStatusImportView.Rnokpp)));
        Assert.Equal("StatusKindId", DisplayName(nameof(PersonStatusImportView.StatusKindId)));
        Assert.Equal("FromDateLocal", DisplayName(nameof(PersonStatusImportView.FromDateLocal)));
        Assert.Equal("Note", DisplayName(nameof(PersonStatusImportView.Note)));
    }

    [Fact(DisplayName = "PersonStatusImportView: сетери/гетери працюють; FromDateLocal має Kind=Unspecified")]
    public void SettersGetters_Work_AsExpected()
    {
        // Arrange
        var vm = new PersonStatusImportView
        {
            Rnokpp = "1234567890",
            StatusKindId = 7,
            FromDateLocal = new DateTime(2025, 08, 09), // без Kind → Unspecified
            Note = "Зарахування"
        };

        // Assert
        Assert.Equal("1234567890", vm.Rnokpp);
        Assert.Equal(7, vm.StatusKindId);
        Assert.Equal(new DateTime(2025, 08, 09), vm.FromDateLocal);
        Assert.Equal(DateTimeKind.Unspecified, vm.FromDateLocal.Kind);
        Assert.Equal("Зарахування", vm.Note);
    }
}
