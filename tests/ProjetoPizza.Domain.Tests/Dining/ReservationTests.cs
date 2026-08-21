using FluentAssertions;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Dining;

public sealed class ReservationTests
{
    [Fact]
    public void Reservation_should_follow_reception_lifecycle()
    {
        var reservation = new Reservation(
            ReservationId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", 4,
            DateTimeOffset.UtcNow.AddHours(2), 90, "Aniversário");
        var tableSessionId = TableSessionId.New();

        reservation.Transition(ReservationStatus.Confirmed);
        reservation.Seat(tableSessionId);
        reservation.Transition(ReservationStatus.Completed);

        reservation.Status.Should().Be(ReservationStatus.Completed);
        reservation.TableSessionId.Should().Be(tableSessionId);
        reservation.SeatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Finished_reservation_should_not_change_again()
    {
        var reservation = new Reservation(
            ReservationId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", 2,
            DateTimeOffset.UtcNow.AddHours(1), 60, null);
        reservation.Transition(ReservationStatus.Cancelled);

        var act = () => reservation.Transition(ReservationStatus.Confirmed);

        act.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("reservation.finished");
    }

    [Fact]
    public void Waitlist_should_record_notification_time()
    {
        var entry = new WaitlistEntry(
            WaitlistEntryId.New(), RestaurantUnitId.New(), "João Lima", "11988887777", 3, 25, null);

        entry.Transition(WaitlistStatus.Notified);

        entry.Status.Should().Be(WaitlistStatus.Notified);
        entry.NotifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirmed_reservation_should_not_be_seated_without_table_session()
    {
        var reservation = new Reservation(
            ReservationId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", 2,
            DateTimeOffset.UtcNow.AddHours(1), 60, null);
        reservation.Transition(ReservationStatus.Confirmed);

        var action = () => reservation.Transition(ReservationStatus.Seated);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("reservation.transition");
    }
}
