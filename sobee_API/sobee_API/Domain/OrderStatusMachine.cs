using System;
using System.Collections.Generic;
using System.Linq;
using sobee_API.Constants;

namespace sobee_API.Domain;

public static class OrderStatusMachine
{
    public static bool CanTransition(string? from, string to)
        => OrderStatuses.CanTransition(from, to);

    public static IReadOnlyList<string> GetAllowedTransitions(string? from)
    {
        var normalized = string.IsNullOrWhiteSpace(from)
            ? OrderStatuses.Pending
            : OrderStatuses.Normalize(from);

        if (!OrderStatuses.IsKnown(normalized))
        {
            return Array.Empty<string>();
        }

        return OrderStatuses.All
            .Where(status => !string.Equals(status, normalized, StringComparison.OrdinalIgnoreCase))
            .Where(status => OrderStatuses.CanTransition(normalized, status))
            .ToArray();
    }

    public static bool IsCancellable(string? status)
        => OrderStatuses.IsCancellable(status);
}
