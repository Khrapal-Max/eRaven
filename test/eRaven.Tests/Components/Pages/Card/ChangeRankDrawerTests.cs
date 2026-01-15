//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankCommandHandlerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Card.Drawwers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using FluentValidation;

namespace eRaven.Tests.Components.Pages.Card;

public sealed class ChangeRankDrawerTests : BunitContext
{
    [Fact]
    public void Submit_InvokesCallback_AndClosesDrawer()
    {
    }
}