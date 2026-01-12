using System;
using System.Collections.Generic;
using System.Linq;

namespace sobee_API.Constants
{
    public static class OrderStatuses
    {
        // Canonical status strings stored in TOrders.StrOrderStatus
        public const string Pending = "Pending";        // order created
        public const string Paid = "Paid";              // payment captured (if applicable)
        public const string Processing = "Processing";  // being prepared / packed
        public const string Shipped = "Shipped";        // handed to carrier
        public const string Delivered = "Delivered";    // delivered to customer
        public const string Cancelled = "Cancelled";    // cancelled before fulfillment
        public const string Refunded = "Refunded";      // refunded after payment

        private static readonly string[] _all =
        {
            Pending, Paid, Processing, Shipped, Delivered, Cancelled, Refunded
        };

        // Allowed transitions (edit these rules as your business process changes)
        private static readonly Dictionary<string, HashSet<string>> _allowedTransitions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Pending] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Paid, Cancelled },
                [Paid] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Processing, Refunded },
                [Processing] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Shipped, Cancelled },
                [Shipped] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Delivered },
                [Delivered] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Refunded },
                [Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { },
                [Refunded] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { }
            };

        public static IReadOnlyList<string> All => _all;

        public static bool IsKnown(string? status)
            => !string.IsNullOrWhiteSpace(status) && _all.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string status)
        {
            status = status.Trim();

            foreach (var s in _all)
            {
                if (string.Equals(s, status, StringComparison.OrdinalIgnoreCase))
                    return s;
            }

            return status; // caller should reject unknown values via IsKnown
        }

        public static bool CanTransition(string? from, string to)
        {
            from = string.IsNullOrWhiteSpace(from) ? Pending : Normalize(from);
            to = Normalize(to);

            if (!IsKnown(from) || !IsKnown(to))
                return false;

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return true; // no-op

            return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
        }

        public static bool IsCancellable(string? status)
        {
            status = string.IsNullOrWhiteSpace(status) ? Pending : Normalize(status);

            // Business rule: cancel allowed before shipment
            return string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, Paid, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase);
        }
    }
}
