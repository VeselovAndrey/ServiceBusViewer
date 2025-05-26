# Service Bus Viewer

> **IMPORTANT NOTE:** This project was initially designed as a temporary solution (based on AI-generated code) to view messages in the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer) until either the emulator supports the management API 
> or [Service Bus Explorer](https://github.com/paolosalvatori/ServiceBusExplorer) adds support for the emulator.
> There are no plans for long-term support/maintenance of this project.

Service Bus Viewer is a web application for viewing and interacting with Azure Service Bus queues and topics.  
**The main goal of this application is to support local development and testing using a Service Bus Emulator.**  

For more information on running and configuring the Azure Service Bus Emulator, see:
- [Official Microsoft documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator).
- [Azure Service Bus Emulator Installer on GitHub](https://github.com/Azure/azure-service-bus-emulator-installer)

## Features

- Connect to any Service Bus instance using a connection string.
- Peek and receive messages from queues or topics.
- View message details and properties.
- Send messages to queues or topics.

---

## Running the Service Bus Emulator Viewer

### Running with Docker
Use the provided `Dockerfile` to build the Service Bus Viewer image. Run the build command in the solution directory (where the solution file is located):

Build the container image using Docker
```bash
docker build -f ServiceBusViewer/Dockerfile -t service-bus-viewer .
```

or build the container image using Podman
```bash
podman build -f ServiceBusViewer/Dockerfile -t service-bus-viewer .
```

No special parameters are required to run the container. E.g. to run the container using Docker, use the following command:
```bash
docker run --name ServiceBusViewer -p 5000:8080 -d service-bus-viewer
```

### Running from source
To run the Service Bus Viewer from source, you need to have the following prerequisites installed:
- .NET SDK 9.0 or later

Navigate to the project directory and use 
```powershell
dotnet run
```
to restore dependencies and start the application.

## Accessing the Service Bus Emulator

- Use the `localhost` address when running the emulator on the same host as the viewer.
- Use `host.docker.internal` to connect from a container to a Service Bus emulator running on the host machine. 
- Use the host machine's network address when connecting from a container to a Service Bus emulator running on the host.
- Use the service name defined in your `docker-compose.yaml` when both the emulator and Service Bus Viewer are running in containers on the same network.
- Use the connection string from the Azure Portal for cloud Azure Service Bus instances (but it is recommended to use [Service Bus Explorer](https://github.com/paolosalvatori/ServiceBusExplorer) or the Azure Portal built-in tool to work with cloud resources).

Hint: add port to the connection string if needed, e.g. if you are running multiple Service Bus emulators on the same host, use
```
Endpoint=sb://localhost:[PORT];SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```