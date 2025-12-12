# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/LocaGuest.Api/LocaGuest.Api.csproj", "src/LocaGuest.Api/"]
COPY ["src/LocaGuest.Domain/LocaGuest.Domain.csproj", "src/LocaGuest.Domain/"]
COPY ["src/LocaGuest.Application/LocaGuest.Application.csproj", "src/LocaGuest.Application/"]
COPY ["src/LocaGuest.Infrastructure/LocaGuest.Infrastructure.csproj", "src/LocaGuest.Infrastructure/"]

RUN dotnet restore "src/LocaGuest.Api/LocaGuest.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/LocaGuest.Api"
RUN dotnet build "LocaGuest.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LocaGuest.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Create Data directory for SQLite
RUN mkdir -p /app/Data

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocaGuest.Api.dll"]
