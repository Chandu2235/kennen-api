# Multi-stage build: the runtime image carries no SDK, source or build tooling.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, using only the project files, so a source-only change reuses the layer.
COPY Kennen.sln ./
COPY src/Kennen.Domain/Kennen.Domain.csproj src/Kennen.Domain/
COPY src/Kennen.Infrastructure/Kennen.Infrastructure.csproj src/Kennen.Infrastructure/
COPY src/Kennen.Api/Kennen.Api.csproj src/Kennen.Api/
COPY tests/Kennen.Api.Tests/Kennen.Api.Tests.csproj tests/Kennen.Api.Tests/
RUN dotnet restore Kennen.sln

COPY . .
RUN dotnet publish src/Kennen.Api/Kennen.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as the non-root user provided by the base image.
USER $APP_UID

# Résumés live on a mounted volume so uploads survive container replacement.
# Point FileStorage__RootPath at this path and mount persistent storage here.
VOLUME /app/storage

ENV ASPNETCORE_URLS=http://+:8080 \
    FileStorage__RootPath=/app/storage/resumes
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kennen.Api.dll"]
