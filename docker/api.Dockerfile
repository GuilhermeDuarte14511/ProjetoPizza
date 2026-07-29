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
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "ProjetoPizza.Api.dll"]
