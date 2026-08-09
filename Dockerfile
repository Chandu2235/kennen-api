# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files first for Docker layer caching
COPY Kennen.sln ./

COPY src/Kennen.Domain/Kennen.Domain.csproj src/Kennen.Domain/
COPY src/Kennen.Infrastructure/Kennen.Infrastructure.csproj src/Kennen.Infrastructure/
COPY src/Kennen.Api/Kennen.Api.csproj src/Kennen.Api/
COPY tests/Kennen.Api.Tests/Kennen.Api.Tests.csproj tests/Kennen.Api.Tests/

# Restore dependencies
RUN dotnet restore Kennen.sln

# Copy source code
COPY . .

# Publish API
RUN dotnet publish src/Kennen.Api/Kennen.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Render will route traffic to this port
ENV ASPNETCORE_URLS=http://+:8080

# Application file-storage location
ENV FileStorage__RootPath=/app/storage/resumes

EXPOSE 8080

# Copy published application
COPY --from=build /app/publish .

# Start ASP.NET Core API
ENTRYPOINT ["dotnet", "Kennen.Api.dll"]
