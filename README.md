# Azure DB Login Helper

A cross-platform tray/menu-bar app for Azure PostgreSQL access tokens after PIM activation. Works on **Windows** and **macOS**.

## Features

- **Tray / menu bar icon** — runs in the background
- **PIM shortcut** — opens the Azure PIM group activation page
- **Token copy** — MSAL-based tokens (respects PIM group membership when a role is selected)
- **Role switching** — read / write (configured in `appsettings.local.json`), or **Plain token (no role check)**
- **Group-claim check** — warns if the active PIM group is missing from the token

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Entra ID account with eligible PIM roles (when using role-based config)

---

## Configuration

Copy the example file and edit your local settings (not committed to git):

```bash
cp appsettings.example.json appsettings.local.json
```

Edit `appsettings.local.json`.

**Plain token** (no PIM role switching or group-claim check):

```json
{
  "AzureDbLogin": {
    "TenantId": "your-tenant-id"
  }
}
```

**With PIM roles** (menu to switch read/write or **Plain token (no role check)**; verifies the Entra group is in the token when a role is selected):

```json
{
  "AzureDbLogin": {
    "TenantId": "...",
    "PostgresHost": "your-server.postgres.database.azure.com",
    "Database": "yourdb",
    "DefaultRole": "read",
    "Roles": [
      {
        "Key": "read",
        "DisplayName": "Read (SELECT only)",
        "PgUser": "your-read-group-role",
        "GroupObjectId": "..."
      }
    ]
  }
}
```

`PostgresHost` / `Database` / `PgUser` are optional metadata for your own reference. `GroupObjectId` is required per role when `Roles` is configured.

---

## Windows

### Run locally

```powershell
dotnet run
```

- Look for the icon in the **system tray** (click **^** if hidden).
- **Right-click** the icon for the menu.
- **Double-click** copies a cached token.
- **Quit:** tray **Exit**, or **Ctrl+C** in the terminal when using `dotnet run`.

### Install at login

```bat
scripts\install-startup.bat
```

Removes with `scripts\uninstall-startup.bat`.

### Troubleshooting (Windows)

- **Build fails (file locked)** — exit the app from the tray or Task Manager, then rebuild.
- **Token missing group claim** — PIM not active, or stale cache; use **Regenerate token**.
- **Groups overage** — too many Entra groups; token omits `groups` claim — contact an admin.

---

## macOS

### First-time setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for macOS (Apple Silicon or Intel as appropriate).
2. Clone or open this repository in a terminal.
3. Create your config:

   ```bash
   cp appsettings.example.json appsettings.local.json
   ```

   Edit `appsettings.local.json` with your tenant ID, roles, and group object IDs.

### Run locally

From the project directory:

```bash
dotnet run
```

- Look for the **menu bar icon** at the top right of the screen (blue circle with a database glyph).
- **Click** the icon to open the menu (macOS does not use right-click on menu bar items).
- Use **Copy token (use cache)** or **Regenerate token** from the menu.
- macOS shows short **notifications** when a token is copied or a role is selected.
- **Quit:** choose **Exit** in the menu, or press **Ctrl+C** in the terminal when using `dotnet run`.

If the icon is crowded out, open **System Settings → Control Center → Menu Bar** and adjust spacing, or drag other menu bar icons to make room.

### Build a Release binary

```bash
dotnet build -c Release -f net10.0
./bin/Release/net10.0/AzureDbLoginHelper
```

`appsettings.local.json` is copied into `bin/Release/net10.0/` on build. Keep it next to the executable if you move the app elsewhere.

### Install at login (LaunchAgent)

The install script builds Release, registers a user LaunchAgent, and starts the app at login:

```bash
chmod +x scripts/install-startup-macos.sh
./scripts/install-startup-macos.sh
```

This creates `~/Library/LaunchAgents/com.azuredblogin.helper.plist` pointing at:

`bin/Release/net10.0/AzureDbLoginHelper`

Re-run the script after pulling code changes so the binary is rebuilt and the agent is reloaded.

**Uninstall:**

```bash
launchctl unload ~/Library/LaunchAgents/com.azuredblogin.helper.plist
rm ~/Library/LaunchAgents/com.azuredblogin.helper.plist
```

**Stop without uninstalling:**

```bash
launchctl unload ~/Library/LaunchAgents/com.azuredblogin.helper.plist
```

**Start again:**

```bash
launchctl load ~/Library/LaunchAgents/com.azuredblogin.helper.plist
```

### Troubleshooting (macOS)

- **Menu bar icon not visible** — macOS hides overflow icons when the menu bar is full; free space or use **Control Center** settings. Confirm the process is running: `pgrep -l AzureDbLoginHelper`.
- **App exits immediately after install** — ensure `appsettings.local.json` exists before running `scripts/install-startup-macos.sh` (it must be copied into `bin/Release/net10.0/`). Check logs: `log show --predicate 'process == "AzureDbLoginHelper"' --last 5m` or run the binary manually in Terminal to see errors.
- **Browser does not open for sign-in** — allow the app when macOS prompts for network or automation; run once via `dotnet run` to complete the interactive login.
- **Token missing group claim** — activate PIM in the portal, select the correct **Role** in the menu (not **Plain token**), then **Regenerate token**.
- **Groups overage** — too many Entra groups; token omits `groups` claim — contact an admin.

---

## Typical workflow

1. **Open PIM activation** from the tray/menu
2. Activate the role in the [Azure portal](https://portal.azure.com)
3. Select **Role** in the app (read / write), or **Plain token (no role check)** if you only need an unscoped token
4. **Regenerate token** and paste into your database tool (Azure Data Studio, DBeaver, etc.)
