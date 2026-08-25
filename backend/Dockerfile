FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Servit.Domain/Servit.Domain.csproj src/Servit.Domain/
COPY src/Servit.Infrastructure/Servit.Infrastructure.csproj src/Servit.Infrastructure/
COPY src/Servit.Api/Servit.Api.csproj src/Servit.Api/
RUN dotnet restore src/Servit.Api/Servit.Api.csproj

COPY src/ src/
RUN dotnet publish src/Servit.Api/Servit.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Servit.Api.dll"]
