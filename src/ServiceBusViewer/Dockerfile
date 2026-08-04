FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV CONNECTION_STRING=""
ENV ROOT_CONNECTION_STRING=""

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["src/ServiceBusViewer/ServiceBusViewer.csproj", "src/ServiceBusViewer/"]
RUN dotnet restore "./src/ServiceBusViewer/ServiceBusViewer.csproj"
COPY ["src/ServiceBusViewer/", "src/ServiceBusViewer/"]
WORKDIR "/workspace/src/ServiceBusViewer"
RUN dotnet publish "./ServiceBusViewer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
LABEL org.opencontainers.image.description="Service Bus Viewer API for the split SPA architecture."
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ServiceBusViewer.dll"]
