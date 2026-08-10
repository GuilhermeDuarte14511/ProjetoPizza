FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props ProjetoPizza.sln ./
COPY src/ProjetoPizza.Domain/ProjetoPizza.Domain.csproj src/ProjetoPizza.Domain/
COPY src/ProjetoPizza.Application/ProjetoPizza.Application.csproj src/ProjetoPizza.Application/
COPY src/ProjetoPizza.Infrastructure/ProjetoPizza.Infrastructure.csproj src/ProjetoPizza.Infrastructure/
COPY src/ProjetoPizza.Api/ProjetoPizza.Api.csproj src/ProjetoPizza.Api/
RUN dotnet restore src/ProjetoPizza.Api/ProjetoPizza.Api.csproj

COPY src/ src/
RUN dotnet publish src/ProjetoPizza.Api/ProjetoPizza.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg libgssapi-krb5-2 \
    && curl --fail --silent --show-error https://www.postgresql.org/media/keys/ACCC4CF8.asc \
        | gpg --dearmor --output /usr/share/keyrings/postgresql.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/postgresql.gpg] https://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" \
        > /etc/apt/sources.list.d/postgresql.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/backups /app/media && chown -R $APP_UID:$APP_UID /app/backups /app/media

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "ProjetoPizza.Api.dll"]
