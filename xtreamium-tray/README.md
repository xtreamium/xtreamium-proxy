# xtreamium-tray

A small notification-area / menu-bar companion for [xtreamium-proxy](https://github.com/xtreamium/xtreamium-proxy). It shows whether a recording is currently in progress and nothing else — it does not start, stop, or configure the proxy.

xtreamium-proxy runs headless (a Windows Service or a per-user systemd daemon) and is designed to stay that way. This app is a separate, independent process: it connects to the proxy's existing SignalR hub and REST API to find out what's happening, and shows a tray icon accordingly. Closing this app does not affect the proxy.

## How it works

- On launch, reads the proxy's own config file (`~/.config/xtreamium-proxy/appsettings.json` on Linux/macOS, `%AppData%\xtreamium-proxy\appsettings.json` on Windows) to find its port and web UI URL. No changes to xtreamium-proxy are required.
- Calls `GET /recordings` once at startup (and after every reconnect) to snapshot what's currently recording.
- Subscribes to the proxy's `/hubs/proxyStatus` SignalR hub for live `RecordingChanged`/`RecordingProgress` events.
- Shows one of three icon states: idle, recording (red badge), or disconnected (can't reach the proxy — shown only after ~5s of failed connection, so brief reconnects don't flicker).
- Tray menu: **Open web UI** (opens the proxy's configured web frontend) and **Quit**.
- Self-installs a per-user autostart entry on first run (Windows: `HKCU\...\Run`; Linux: `~/.config/autostart/xtreamium-tray.desktop`). macOS autostart (`LaunchAgent`) is implemented but unused until macOS packaging exists.

## Running

```
dotnet run
```

Requires the proxy to be running (or it will simply show "disconnected" and retry with backoff until it appears).

## Scope notes

- Status-only by design — no service start/stop/restart from the tray.
- macOS: the autostart/tray code path exists but there is no packaging (`.app` bundle, notarization, CI) yet, matching xtreamium-proxy's own current lack of macOS distribution.
- Packaging (nfpm/Velopack/CI) for Windows and Linux is a follow-up; today this is run from a local build.

## Tests

```
dotnet test
```
