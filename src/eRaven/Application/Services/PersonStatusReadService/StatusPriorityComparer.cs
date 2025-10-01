using System.Collections.Generic;
using System.Linq;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PersonStatusReadService;

internal static class StatusPriorityComparer
{
    public static int Compare(StatusKind a, StatusKind b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return 1;
        if (b is null) return -1;
        if (ReferenceEquals(a, b)) return 0;

        var orderComparison = a.Order.CompareTo(b.Order);
        if (orderComparison != 0) return orderComparison;

        return a.Id.CompareTo(b.Id);
    }

    public static IOrderedQueryable<PersonStatus> OrderForHistory(IQueryable<PersonStatus> q)
        => q.OrderBy(ps => ps.OpenDate)
            .ThenBy(ps => ps.StatusKind == null ? int.MaxValue : ps.StatusKind.Order)
            .ThenBy(ps => ps.Id);

    public static IOrderedQueryable<PersonStatus> OrderForPointInTime(IQueryable<PersonStatus> q)
        => q.OrderBy(ps => ps.OpenDate)
            .ThenBy(ps => ps.StatusKind == null ? int.MaxValue : ps.StatusKind.Order);
}
