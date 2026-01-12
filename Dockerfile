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

# Security: run as non-root
RUN addgroup --system --gid 10001 dotnetapp && adduser --system --uid 10001 --ingroup dotnetapp dotnetapp
ENV LOCAGUEST_HOME=/app/LocaGuest
RUN mkdir -p /app/LocaGuest /app/LocaGuest/Data /app/LocaGuest/log /app/Data && chown -R dotnetapp:dotnetapp /app/LocaGuest /app/Data
USER dotnetapp

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish --chown=dotnetapp:dotnetapp /app/publish .
ENTRYPOINT ["dotnet", "LocaGuest.Api.dll"]
