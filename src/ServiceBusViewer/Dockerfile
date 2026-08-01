FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV CONNECTION_STRING=""

# This stage restores npm dependencies and builds the static UI assets.
FROM node:24-slim AS assets
WORKDIR /workspace
COPY ["package.json", "package-lock.json", "./"]
RUN npm ci
COPY ["scripts/", "scripts/"]
COPY ["src/ServiceBusViewer/Pages/", "src/ServiceBusViewer/Pages/"]
COPY ["src/ServiceBusViewer/Styles/", "src/ServiceBusViewer/Styles/"]
COPY ["src/ServiceBusViewer/wwwroot/", "src/ServiceBusViewer/wwwroot/"]
RUN npm run build:assets

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["src/ServiceBusViewer/ServiceBusViewer.csproj", "src/ServiceBusViewer/"]
RUN dotnet restore "./src/ServiceBusViewer/ServiceBusViewer.csproj"
COPY ["src/", "src/"]
COPY --from=assets /workspace/src/ServiceBusViewer/wwwroot ./src/ServiceBusViewer/wwwroot
WORKDIR "/workspace/src/ServiceBusViewer"
RUN dotnet build "./ServiceBusViewer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ServiceBusViewer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
LABEL org.opencontainers.image.description="Service Bus Viewer is a web application for viewing and interacting with Azure Service Bus queues and topics."
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ServiceBusViewer.dll"]

