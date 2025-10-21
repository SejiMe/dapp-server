# # syntax=docker/dockerfile:1

# # Multi-stage Dockerfile for dengue-watch-api
# # - Builds with Debug configuration
# # - Sets ASP.NET Core environment to Development

# ARG BUILD_CONFIGURATION=Debug

# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# WORKDIR /app
# ENV ASPNETCORE_ENVIRONMENT=Development \
#     DOTNET_ENVIRONMENT=Development \
#     ASPNETCORE_URLS=http://+:5000
# # Default ASP.NET Core container port (DEBUG uses 5000 via UseUrls)
# EXPOSE 5000

# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ARG BUILD_CONFIGURATION
# WORKDIR /src

# # Copy project files first to leverage Docker layer caching for restore
# COPY ["dengue.watch.api/dengue.watch.api.csproj", "dengue.watch.api/"]
# COPY ["dengue.watch.api.tests/dengue.watch.api.tests.csproj", "dengue.watch.api.tests/"]

# RUN dotnet restore "dengue.watch.api/dengue.watch.api.csproj"

# # Copy the remaining source
# COPY . .

# WORKDIR /src/dengue.watch.api
# RUN dotnet build "dengue.watch.api.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

# FROM build AS publish
# ARG BUILD_CONFIGURATION
# RUN dotnet publish "dengue.watch.api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

# FROM base AS final
# WORKDIR /app
# COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "dengue.watch.api.dll"]


# syntax=docker/dockerfile:1

# Multi-stage Dockerfile for dengue-watch-api
# - Builds with Debug configuration
# - Sets ASP.NET Core environment to Development

ARG BUILD_CONFIGURATION=Debug

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:5000
# Default ASP.NET Core container port (DEBUG uses 5000 via UseUrls)
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

# Copy project files first to leverage Docker layer caching for restore
COPY ["dengue.watch.api/dengue.watch.api.csproj", "dengue.watch.api/"]
COPY ["dengue.watch.api.tests/dengue.watch.api.tests.csproj", "dengue.watch.api.tests/"]

RUN dotnet restore "dengue.watch.api/dengue.watch.api.csproj"

# Copy the remaining source
COPY . .

WORKDIR /src/dengue.watch.api
RUN dotnet build "dengue.watch.api.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION
RUN dotnet publish "dengue.watch.api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Explicitly copy ML models from the build context
COPY --from=build /src/dengue.watch.api/infrastructure/ml/models ./infrastructure/ml/models

ENTRYPOINT ["dotnet", "dengue.watch.api.dll"]