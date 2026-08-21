using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Dining;

public enum ReservationStatus { Pending, Confirmed, Seated, Completed, Cancelled, NoShow }
public enum WaitlistStatus { Waiting, Notified, Seated, Cancelled }

public sealed class Reservation : AggregateRoot<ReservationId>
{
    private Reservation() : base(default) { }

    public Reservation(
        ReservationId id, RestaurantUnitId unitId, string customerName, string phone,
        int partySize, DateTimeOffset scheduledAt, int durationMinutes, string? notes,
        CustomerId? customerId = null) : base(id)
    {
        if (scheduledAt < DateTimeOffset.UtcNow.AddMinutes(-5))
            throw new BusinessRuleException("reservation.past", "Reservation time cannot be in the past.");
        UnitId = unitId;
        CustomerId = customerId;
        CustomerName = Guard.Required(customerName, nameof(customerName), 120);
        Phone = Customer.NormalizePhone(phone);
        PartySize = Guard.Positive(partySize, nameof(partySize));
        DurationMinutes = Guard.Positive(durationMinutes, nameof(durationMinutes));
        ScheduledAt = scheduledAt;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.Required(notes, nameof(notes), 500);
        Status = ReservationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public CustomerId? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public int PartySize { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public TableSessionId? TableSessionId { get; private set; }
    public DateTimeOffset? SeatedAt { get; private set; }

    public void Transition(ReservationStatus status)
    {
        if (Status is ReservationStatus.Completed or ReservationStatus.Cancelled or ReservationStatus.NoShow)
            throw new BusinessRuleException("reservation.finished", "A finished reservation cannot be changed.");
        var valid = (Status, status) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Confirmed or ReservationStatus.Cancelled) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled or ReservationStatus.NoShow) => true,
            (ReservationStatus.Seated, ReservationStatus.Completed) => true,
            _ => false,
        };
        if (!valid) throw new BusinessRuleException("reservation.transition", "Invalid reservation status transition.");
        Status = status;
    }

    public void Seat(TableSessionId tableSessionId)
    {
        EnsureCanSeat();
        TableSessionId = tableSessionId;
        SeatedAt = DateTimeOffset.UtcNow;
        Status = ReservationStatus.Seated;
    }

    public void EnsureCanSeat()
    {
        if (Status != ReservationStatus.Confirmed)
        {
            throw new BusinessRuleException("reservation.seat", "Only a confirmed reservation can be seated.");
        }
    }
}

public sealed class WaitlistEntry : AggregateRoot<WaitlistEntryId>
{
    private WaitlistEntry() : base(default) { }

    public WaitlistEntry(
        WaitlistEntryId id, RestaurantUnitId unitId, string customerName, string phone,
        int partySize, int estimatedWaitMinutes, string? notes, CustomerId? customerId = null) : base(id)
    {
        UnitId = unitId;
        CustomerId = customerId;
        CustomerName = Guard.Required(customerName, nameof(customerName), 120);
        Phone = Customer.NormalizePhone(phone);
        PartySize = Guard.Positive(partySize, nameof(partySize));
        EstimatedWaitMinutes = (int)Guard.NonNegative(estimatedWaitMinutes, nameof(estimatedWaitMinutes));
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.Required(notes, nameof(notes), 500);
        Status = WaitlistStatus.Waiting;
        EnteredAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public CustomerId? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public int PartySize { get; private set; }
    public int EstimatedWaitMinutes { get; private set; }
    public string? Notes { get; private set; }
    public WaitlistStatus Status { get; private set; }
    public DateTimeOffset EnteredAt { get; private set; }
    public DateTimeOffset? NotifiedAt { get; private set; }
    public TableSessionId? TableSessionId { get; private set; }
    public DateTimeOffset? SeatedAt { get; private set; }

    public void Transition(WaitlistStatus status)
    {
        if (Status is WaitlistStatus.Seated or WaitlistStatus.Cancelled)
            throw new BusinessRuleException("waitlist.finished", "A finished waitlist entry cannot be changed.");
        var valid = (Status, status) switch
        {
            (WaitlistStatus.Waiting, WaitlistStatus.Notified or WaitlistStatus.Cancelled) => true,
            (WaitlistStatus.Notified, WaitlistStatus.Cancelled) => true,
            _ => false,
        };
        if (!valid) throw new BusinessRuleException("waitlist.transition", "Invalid waitlist status transition.");
        Status = status;
        if (status == WaitlistStatus.Notified) NotifiedAt = DateTimeOffset.UtcNow;
    }

    public void Seat(TableSessionId tableSessionId)
    {
        EnsureCanSeat();
        TableSessionId = tableSessionId;
        SeatedAt = DateTimeOffset.UtcNow;
        Status = WaitlistStatus.Seated;
    }

    public void EnsureCanSeat()
    {
        if (Status is not (WaitlistStatus.Waiting or WaitlistStatus.Notified))
        {
            throw new BusinessRuleException("waitlist.seat", "Only a waiting customer can be seated.");
        }
    }
}
