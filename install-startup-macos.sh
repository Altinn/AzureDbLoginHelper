#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Building Azure DB Login Helper..."
dotnet build --configuration Release

APP_PATH="$SCRIPT_DIR/bin/Release/net8.0/AzureDbLoginHelper"
if [[ ! -f "$APP_PATH" ]]; then
  echo "Executable not found at: $APP_PATH"
  exit 1
fi

PLIST_DIR="$HOME/Library/LaunchAgents"
PLIST_PATH="$PLIST_DIR/com.azuredblogin.helper.plist"

mkdir -p "$PLIST_DIR"

cat > "$PLIST_PATH" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key>
  <string>com.azuredblogin.helper</string>
  <key>ProgramArguments</key>
  <array>
    <string>$APP_PATH</string>
  </array>
  <key>RunAtLoad</key>
  <true/>
  <key>KeepAlive</key>
  <false/>
</dict>
</plist>
EOF

launchctl unload "$PLIST_PATH" 2>/dev/null || true
launchctl load "$PLIST_PATH"

echo ""
echo "Installed LaunchAgent: $PLIST_PATH"
echo "The app will start at login. Look for the menu bar icon after login."
echo ""
echo "To uninstall: launchctl unload \"$PLIST_PATH\" && rm \"$PLIST_PATH\""
