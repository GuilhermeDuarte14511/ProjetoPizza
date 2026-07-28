namespace ProjetoPizza.Application.Admin;

public interface IAdminManagementService
{
    Task<IReadOnlyCollection<OrderManagementDto>> ListOrdersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PizzaCrustDto>> ListPizzaCrustsAsync(CancellationToken cancellationToken);
    Task<UnitSettingsDto> GetUnitSettingsAsync(CancellationToken cancellationToken);
    Task<OperationSettingsDto> GetOperationSettingsAsync(CancellationToken cancellationToken);
    Task<PizzaRulesDto> GetPizzaRulesAsync(CancellationToken cancellationToken);
    Task<CashShiftDto?> GetCurrentCashShiftAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentMethodDto>> ListPaymentMethodsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentDto>> ListPaymentsAsync(CancellationToken cancellationToken);
    Task<FinancialReportDto> GetFinancialReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DeviceDto>> ListDevicesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLogDto>> ListAuditLogsAsync(CancellationToken cancellationToken);
    Task<SystemSnapshotDto> CreateSystemSnapshotAsync(CancellationToken cancellationToken);

    Task UpdateUnitAsync(UpdateUnitCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task UpdateOperationSettingsAsync(UpdateOperationSettingsCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task UpdatePizzaRulesAsync(UpdatePizzaRulesCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> SaveCategoryAsync(SaveCategoryCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> SaveProductAsync(SaveProductCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> SavePizzaSizeAsync(SavePizzaSizeCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> SavePizzaCrustAsync(SavePizzaCrustCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> SavePizzaFlavorAsync(SavePizzaFlavorCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> OpenTableAsync(OpenTableCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> RequestBillAsync(Guid tableSessionId, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> TransitionOrderAsync(Guid id, string transition, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> TransitionKitchenTicketAsync(Guid id, string transition, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> ResolveServiceCallAsync(Guid id, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> RecordPaymentAsync(RecordPaymentCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> RegisterCashMovementAsync(RegisterCashMovementCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> CloseCashShiftAsync(CloseCashShiftCommand command, Guid identityUserId, CancellationToken cancellationToken);
    Task<CommandResultDto> UpdateDeviceAsync(Guid id, UpdateDeviceCommand command, Guid identityUserId, CancellationToken cancellationToken);
}
