// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// SetPersonStatusViewModelTests
// -----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Application.Tests.ViewModels.Persons;

public sealed class SetPersonStatusViewModelTests
{
    [Fact(DisplayName = "SetPersonStatusVM: значення за замовчуванням коректні")]
    public void Defaults_AreExpected()
    {
        var vm = new SetPersonStatusViewModel();

        Assert.Equal(Guid.Empty, vm.PersonId);
        Assert.Equal(0, vm.StatusId);
        Assert.Equal(default, vm.Moment); // 0001-01-01
        Assert.Null(vm.Note);
        Assert.Null(vm.Author);
    }

    [Fact(DisplayName = "SetPersonStatusVM: MaxLength працює для Note(512) та Author(64)")]
    public void MaxLength_Validation_Works()
    {
        var vm = new SetPersonStatusViewModel
        {
            PersonId = Guid.NewGuid(),
            StatusId = 1,
            Moment = new DateTime(2025, 09, 01), // Kind=Unspecified — ок
            Note = new string('x', 513),         // > 512 → помилка
            Author = new string('y', 65)         // > 64  → помилка
        };

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SetPersonStatusViewModel.Note)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SetPersonStatusViewModel.Author)));

        // Граничні значення — валідні
        vm.Note = new string('x', 512);
        vm.Author = new string('y', 64);
        results = Validate(vm);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(SetPersonStatusViewModel.Note)));
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(SetPersonStatusViewModel.Author)));
    }

    [Fact(DisplayName = "SetPersonStatusVM: валідний екземпляр проходить валідацію")]
    public void Valid_Instance_Passes_Validation()
    {
        var vm = new SetPersonStatusViewModel
        {
            PersonId = Guid.NewGuid(),
            StatusId = 10,
            // UI задає локальну календарну дату з Kind=Unspecified — це очікувано
            Moment = new DateTime(2025, 09, 01),
            Note = "Опційна нотатка",
            Author = "tester"
        };

        var results = Validate(vm);
        Assert.Empty(results);
        Assert.Equal(DateTimeKind.Unspecified, vm.Moment.Kind); // явна перевірка очікуваної семантики
    }

    // ---- helpers ----
    private static List<ValidationResult> Validate(object instance)
    {
        var ctx = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return results;
    }
}
