//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ValidationHelper
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Helpers;

internal static class ValidationHelper
{
    public static IList<ValidationResult> ValidateObject(object instance)
    {
        var ctx = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return results;
    }

    public static string Repeat(char c, int count) => new string(c, count);
}
