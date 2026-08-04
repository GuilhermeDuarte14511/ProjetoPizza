namespace ProjetoPizza.Application.Client;

public interface IClientSessionService
{
    Task<ClientActivationDto> ActivateAsync(
        ActivateClientSessionCommand command,
        CancellationToken cancellationToken);

    Task<ClientSessionContext?> ValidateSessionAsync(
        string token,
        CancellationToken cancellationToken);

    Task<ClientBootstrapDto> StartTableSessionAsync(
        ClientSessionContext session,
        StartClientTableSessionCommand command,
        CancellationToken cancellationToken);

    Task<ClientBootstrapDto> CompleteTableSessionAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken);

    Task UpdateTelemetryAsync(
        ClientSessionContext session,
        UpdateClientTelemetryCommand command,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken);
}

public interface IClientQueryService
{
    Task<ClientBootstrapDto> GetBootstrapAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken);

    Task<ClientStateDto> GetStateAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken);
}

public interface IClientOrderingService
{
    Task<ClientOrderDto> SubmitOrderAsync(
        ClientSessionContext session,
        SubmitClientOrderCommand command,
        CancellationToken cancellationToken);
}

public interface IClientAssistanceService
{
    Task<ClientCommandResultDto> CreateServiceCallAsync(
        ClientSessionContext session,
        CreateClientServiceCallCommand command,
        CancellationToken cancellationToken);

    Task<ClientBillDto> RequestBillAsync(
        ClientSessionContext session,
        RequestClientBillCommand command,
        CancellationToken cancellationToken);
}

public interface IClientService :
    IClientSessionService,
    IClientQueryService,
    IClientOrderingService,
    IClientAssistanceService
{
}
