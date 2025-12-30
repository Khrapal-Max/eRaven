//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditContextFluentValidationExtensions
//-----------------------------------------------------------------------------

using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Extensions;

public static class EditContextFluentValidationExtensions
{
    // зберігаємо store в EditContext.Properties, щоб очищати/перезаписувати саме його
    private static readonly object StoreKey = new();

    private static ValidationMessageStore GetOrCreateStore(EditContext editContext)
    {
        if (!editContext.Properties.TryGetValue(StoreKey, out var obj) || obj is not ValidationMessageStore store)
        {
            store = new ValidationMessageStore(editContext);
            editContext.Properties[StoreKey] = store;
        }

        return store;
    }

    /// <summary>
    /// Validates EditContext.Model using FluentValidation validator and writes messages into one persistent ValidationMessageStore.
    /// No duplicates.
    /// </summary>
    public static async Task<bool> ValidateWithFluentValidationAsync<TModel>(
        this EditContext editContext,
        IValidator<TModel> validator,
        CancellationToken ct = default)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(validator);

        if (editContext.Model is not TModel model)
            throw new InvalidOperationException(
                $"EditContext.Model is '{editContext.Model.GetType().Name}', but validator expects '{typeof(TModel).Name}'.");

        var store = GetOrCreateStore(editContext);

        // ✅ чистимо саме "наш" store — і дублювання зникає
        store.Clear();

        var result = await validator.ValidateAsync(model, ct).ConfigureAwait(false);

        foreach (var failure in result.Errors)
        {
            var field = new FieldIdentifier(model, failure.PropertyName);
            store.Add(field, failure.ErrorMessage);
        }

        editContext.NotifyValidationStateChanged();
        return result.IsValid;
    }

    /// <summary>
    /// Optional: clear all fluent-validation messages for this EditContext (e.g. when reopening modal).
    /// </summary>
    public static void ClearFluentValidationMessages(this EditContext editContext)
    {
        if (editContext.Properties.TryGetValue(StoreKey, out var obj) && obj is ValidationMessageStore store)
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        }
    }

    /// <summary>
    /// Individual validation error
    /// </summary>
    public static void AddFieldError<TModel>(
       this EditContext editContext,
       TModel model,
       string fieldName,
       string message)
       where TModel : class
    {
        if (editContext is null) throw new ArgumentNullException(nameof(editContext));

        var store = GetOrCreateStore(editContext);

        var field = new FieldIdentifier(model, fieldName);
        store.Add(field, message);

        editContext.NotifyValidationStateChanged();
    }
}
