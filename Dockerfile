# Set the global ARG and use that value in each build-stage
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base

WORKDIR /app

# Set environment variables and container port
ENV TZ=America/Mexico_City
ARG BUILD_CONFIGURATION
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://+80"
ENV ForwardedHeaders_Enabled=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Install required packages for Android tools (adb, scrcpy)
RUN apk update && \
    apk add --no-cache \
    android-tools \
    ffmpeg \
    curl \
    wget

EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION

# Copy solution file and project files
WORKDIR /src

COPY ["Mobile.Remote.Toolkit.sln", "."]
COPY ["Mobile.Remote.Toolkit/Mobile.Remote.Toolkit.Api.csproj", "Mobile.Remote.Toolkit/"]
COPY ["Mobile.Remote.Toolkit.Business/Mobile.Remote.Toolkit.Business/Mobile.Remote.Toolkit.Business.csproj", "Mobile.Remote.Toolkit.Business/Mobile.Remote.Toolkit.Business/"]
COPY ["Mobile.Remote.Toolkit.Business/Mobile.Remote.Toolkit.Business.vbproj", "Mobile.Remote.Toolkit.Business/"]
COPY ["Mobile.Remote.Toolkit.Domain/Mobile.Remote.Toolkit.Domain/Mobile.Remote.Toolkit.Domain.csproj", "Mobile.Remote.Toolkit.Domain/Mobile.Remote.Toolkit.Domain/"]

# Restore packages
RUN dotnet restore "Mobile.Remote.Toolkit/Mobile.Remote.Toolkit.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/Mobile.Remote.Toolkit"
RUN dotnet build "Mobile.Remote.Toolkit.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
RUN dotnet publish "Mobile.Remote.Toolkit.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Runtime stage
FROM base AS runtime
ARG BUILD_CONFIGURATION

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy Tools directory with Android utilities
COPY --from=build /src/Tools ./Tools

# Make sure Android tools have execute permissions
RUN chmod +x /app/Tools/Android/adb/* || true
RUN chmod +x /app/Tools/Android/scrcpy/* || true

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "Mobile.Remote.Toolkit.Api.dll"]
