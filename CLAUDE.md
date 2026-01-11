# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AwtrixHub is an Azure Functions application targeting .NET 10 with the isolated worker process model (Azure Functions v4). The project provides timer-triggered functions that publish custom apps to a TC001 pixel clock running Awtrix 3 firmware via MQTT. Initial implementation includes bin day reminders with Alexa voice control for clearing notifications.

## Build and Run Commands

### Build
```bash
cd AwtrixHub.Functions
dotnet build
```

### Run Locally
```bash
cd AwtrixHub.Functions
dotnet run
# Or use the configured launch profile:
dotnet run --port 7000
```

### Azure Functions Core Tools
```bash
cd AwtrixHub.Functions
func start  # If Azure Functions Core Tools is installed
```

### Clean
```bash
cd AwtrixHub.Functions
dotnet clean
```

## Project Structure

```
AwtrixHub/
├── AwtrixHub.slnx                      # Solution file
└── AwtrixHub.Functions/                # Functions project
    ├── Functions/                      # Timer and HTTP triggered functions
    │   └── BinDayNotify.cs            # Example timer function (daily at 2 AM)
    ├── Services/                       # Shared services (MQTT client, etc.)
    ├── Models/                         # Data models (Awtrix payloads, etc.)
    ├── Configuration/                  # Configuration classes
    ├── Program.cs                      # Application entry point
    ├── host.json                       # Azure Functions runtime config
    └── local.settings.json             # Local development settings
```

## Architecture

### Application Entry Point
- **Program.cs**: Configures the Azure Functions host with Application Insights telemetry for the isolated worker process model. Sets up dependency injection for services.

### Function Types
Functions are organized in the `Functions/` folder:
- **Timer-triggered functions**: Use `[TimerTrigger]` attribute with cron expressions for scheduled execution
- **HTTP-triggered functions**: For webhook integrations (e.g., IFTTT/Alexa commands)
- Each function receives trigger-specific parameters (e.g., `TimerInfo` for timer triggers)

### Services Layer
The `Services/` folder contains shared business logic:
- **MqttService**: Handles MQTT client connections and publishing to HiveMQ Cloud broker
- Connection lifecycle: Connect → Publish → Disconnect per function execution

### Models
The `Models/` folder contains data structures:
- **AwtrixPayload**: JSON payload structures for Awtrix 3 custom app API

### Configuration Files
- **host.json**: Azure Functions runtime configuration with Application Insights sampling settings
- **local.settings.json**: Local development settings (not deployed). Uses local storage emulator (`UseDevelopmentStorage=true`) and dotnet-isolated runtime. Contains HiveMQ credentials and connection settings
- **launchSettings.json**: Debug profile configured to run on port 7000

### Key Dependencies
- **Microsoft.Azure.Functions.Worker** - Isolated process model
- **Microsoft.Azure.Functions.Worker.Extensions.Timer** - Timer trigger support
- **Microsoft.ApplicationInsights.WorkerService** - Telemetry
- **MQTTnet** - MQTT client library for publishing to HiveMQ Cloud

### Cron Expression Format
Timer triggers use 6-field cron expressions: `{second} {minute} {hour} {day} {month} {day-of-week}`
- Example: `"0 0 6 * * *"` = Every day at 6:00:00 AM
- Example: `"0 0 2 * * *"` = Every day at 2:00:00 AM

## MQTT & Awtrix Integration

### HiveMQ Cloud Setup
- Cloud-hosted MQTT broker (free tier: 100 connections)
- TC001 connects outbound to HiveMQ (no port forwarding required)
- Azure Functions publish messages to HiveMQ topics

### Awtrix 3 Custom Apps
- **Topic structure**: `{prefix}/custom/{appname}` where prefix is configured in TC001 settings
- **To display app**: Publish JSON payload to topic
- **To clear app**: Publish empty payload to same topic
- **Persistence**: Apps with `duration: 0` remain in display rotation until explicitly cleared

### MQTT Connection Pattern
For serverless functions, use connect-per-execution pattern:
1. Create MqttClient
2. Connect to HiveMQ broker
3. Publish message
4. Disconnect
This is simple and reliable for low-volume scenarios.

## Development Notes

### .NET 10
This project targets .NET 10, the latest version. Modern .NET is cross-platform but this project is developed on Windows. All modern C# language features are available.

### Isolated Worker Process
The project uses the isolated worker process model (`FUNCTIONS_WORKER_RUNTIME: dotnet-isolated`), which runs in a separate process from the Functions host. This affects:
- Dependency injection setup (configured in Program.cs)
- Trigger bindings (attributes are on method parameters)
- Logging (uses ILogger from dependency injection)
