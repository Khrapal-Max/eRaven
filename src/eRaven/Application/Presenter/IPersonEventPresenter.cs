//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonEventPresenter
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;

namespace eRaven.Application.Presenter;

public interface IPersonEventPresenter
{
    PersonEventListItemDto ToListItem(PersonEventDto e);
}