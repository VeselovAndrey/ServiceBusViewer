FROM node:24-slim AS build
WORKDIR /workspace
COPY ["src/ServiceBusViewer.Web/package.json", "src/ServiceBusViewer.Web/package-lock.json", "./"]
RUN npm ci
COPY ["src/ServiceBusViewer.Web/", "./"]
RUN npm run build

FROM nginx:1.29-alpine AS final
ENV ServiceBusViewer_UPSTREAM=http://ServiceBusViewer:8080
LABEL org.opencontainers.image.description="Service Bus Viewer SPA served by nginx with API reverse proxy support."
COPY ["src/ServiceBusViewer.Web/nginx/default.conf.template", "/etc/nginx/templates/default.conf.template"]
COPY --from=build /workspace/dist /usr/share/nginx/html
EXPOSE 8080
