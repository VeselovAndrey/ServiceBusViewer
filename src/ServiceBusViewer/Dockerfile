FROM node:24-slim AS frontend-build
WORKDIR /workspace
COPY ["src/ServiceBusViewer.Web/package.json", "src/ServiceBusViewer.Web/package-lock.json", "./"]
RUN npm ci
COPY ["src/ServiceBusViewer.Web/", "./"]
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["src/ServiceBusViewer/ServiceBusViewer.csproj", "src/ServiceBusViewer/"]
RUN dotnet restore "./src/ServiceBusViewer/ServiceBusViewer.csproj"
COPY ["src/ServiceBusViewer/", "src/ServiceBusViewer/"]
WORKDIR "/workspace/src/ServiceBusViewer"
RUN dotnet publish "./ServiceBusViewer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV SERVICEBUSVIEWER_CONNECTION_STRING=""
ENV SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING=""
ENV SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME=""
ENV SERVICEBUSVIEWER_SUBSCRIPTION_NAME=""
ENV SERVICEBUSVIEWER_RUNNING_IN_CONTAINER=true
LABEL org.opencontainers.image.description="Service Bus Viewer SPA and API."
COPY --from=build /app/publish .
COPY --from=frontend-build /workspace/dist ./wwwroot
ENTRYPOINT ["dotnet", "ServiceBusViewer.dll"]
