using FluentAssertions;
using sobee_API.Constants;
using sobee_API.Domain;
using Xunit;

namespace sobee_API.Tests.Domain;

public class OrderStatusMachineTests
{
    [Fact]
    public void CanTransition_PendingToPaid_ReturnsTrue()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Pending, OrderStatuses.Paid).Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PendingToShipped_ReturnsFalse()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Pending, OrderStatuses.Shipped).Should().BeFalse();
    }

    [Fact]
    public void CanTransition_PaidToShipped_ReturnsFalse()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Paid, OrderStatuses.Shipped).Should().BeFalse();
    }

    [Fact]
    public void CanTransition_ShippedToDelivered_ReturnsTrue()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Shipped, OrderStatuses.Delivered).Should().BeTrue();
    }

    [Fact]
    public void CanTransition_DeliveredToCancelled_ReturnsFalse()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Delivered, OrderStatuses.Cancelled).Should().BeFalse();
    }

    [Fact]
    public void CanTransition_PendingToCancelled_ReturnsTrue()
    {
        OrderStatusMachine.CanTransition(OrderStatuses.Pending, OrderStatuses.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void GetAllowedTransitions_Pending_ReturnsCorrectSet()
    {
        var allowed = OrderStatusMachine.GetAllowedTransitions(OrderStatuses.Pending);

        allowed.Should().BeEquivalentTo(new[]
        {
            OrderStatuses.Paid,
            OrderStatuses.Cancelled
        });
    }

    [Fact]
    public void IsCancellable_Pending_ReturnsTrue()
    {
        OrderStatusMachine.IsCancellable(OrderStatuses.Pending).Should().BeTrue();
    }

    [Fact]
    public void IsCancellable_Shipped_ReturnsFalse()
    {
        OrderStatusMachine.IsCancellable(OrderStatuses.Shipped).Should().BeFalse();
    }
}
