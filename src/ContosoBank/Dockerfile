# ============================================================
# Stage 1: Build and publish the application
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project file first for better layer caching
COPY ["ContosoBank.csproj", "./"]

# Restore NuGet packages
RUN dotnet restore "ContosoBank.csproj"

# Copy source code
COPY . .

# Build and publish the application
RUN dotnet publish "ContosoBank.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ============================================================
# Stage 2: Final runtime image
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application from build stage
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Expose the port the application listens on
EXPOSE 8080

# Health check using the liveness endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Switch to non-root user for security
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoBank.dll"]
