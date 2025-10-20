# Azure DB Login Helper

An application that provides easy access to Azure PostgreSQL access tokens through a system tray icon.

## Features

- **System Tray Integration**: Runs quietly in the background with a system tray icon
- **One-Click Access**: Right-click the tray icon to get a new access token
- **Automatic Clipboard**: Access tokens are automatically copied to your clipboard
- **Visual Feedback**: Status updates and balloon notifications keep you informed
- **Azure CLI Integration**: Uses the Azure CLI to retrieve tokens securely

## Installation

### Prerequisites

- .NET 8.0 Runtime
- Azure CLI installed and configured (`az login`)

### Setup: Install as Startup Application (Recommended)

1. Build the application:
   ```bash
   dotnet build --configuration Release
   ```

2. Install as a startup application:
   ```bash
   install-startup.bat
   ```

3. The application will start automatically when you log in and show the tray icon.

### Manual Installation

1. Run the executable directly:
   ```bash
   dotnet run
   ```

## Usage

1. Look for the "Azure DB Login Helper" icon in your system tray
2. Double-click the icon to get a new access token
3. The token will be automatically copied to your clipboard
4. You'll see a balloon notification confirming the action

## Configuration

The service automatically uses the Azure CLI command:
```bash
az account get-access-token --resource-type oss-rdbms --query accessToken -o tsv
```

Make sure you're logged in to Azure CLI with appropriate permissions:
```bash
az login
```

## Troubleshooting

- **"Azure CLI not found"**: Install Azure CLI and ensure it's in your PATH
- **"Not logged in"**: Run `az login` to authenticate with Azure
- **Service won't start**: Check Windows Event Log for error details
- **Token retrieval fails**: Verify your Azure account has the necessary permissions

## Uninstallation

### Startup Application
To remove the startup application:
```bash
uninstall-startup.bat
```