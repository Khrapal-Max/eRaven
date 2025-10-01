namespace eRaven.Application.Services.PersonStatusReadService;

using System;
using eRaven.Domain.Models;

public sealed class PersonMonthStatus
{
    public PersonMonthStatus(PersonStatus?[] days, DateTime? firstPresenceUtc)
    {
        Days = days;
        FirstPresenceUtc = firstPresenceUtc;
    }

    public PersonStatus?[] Days { get; }

    public DateTime? FirstPresenceUtc { get; }
}
