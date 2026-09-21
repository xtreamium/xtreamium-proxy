# xtreamium-tray

A small notification-area / menu-bar companion for [xtreamium-proxy](https://github.com/xtreamium/xtreamium-proxy). It shows whether a recording is currently in progress and nothing else — it does not start, stop, or configure the proxy.

xtreamium-proxy runs headless (a Windows Service or a per-user systemd daemon) and is designed to stay that way. This app is a separate, independent process: it connects to the proxy's existing SignalR hub and REST API to find out what's happening, and shows a tray icon accordingly. Closing this app does not affect the proxy.

## How it works

- On launch, reads the proxy's own config file (`~/.config/xtreamium-proxy/appsettings.json` on Linux/macOS, `%AppData%\xtreamium-proxy\appsettings.json` on Windows) to find its port and web UI URL. No changes to xtreamium-proxy are required.
- Calls `GET /recordings` once at startup (and after every reconnect) to snapshot what's currently recording.
- Subscribes to the proxy's `/hubs/proxyStatus` SignalR hub for live `RecordingChanged`/`RecordingProgress` events.
- Shows one of three icon states: idle, recording (red badge), or disconnected (can't reach the proxy — shown only after ~5s of failed connection, so brief reconnects don't flicker).
- Tray menu: **Open web UI** (opens the proxy's configured web frontend) and **Quit**.
- Windows self-installs a per-user autostart entry on first run (`HKCU\...\Run`). Linux doesn't — see [Lifecycle](#lifecycle). macOS autostart (`LaunchAgent`) is implemented but unused until macOS packaging exists.

## Lifecycle

The tray starts and stops with the proxy. It's still a separate process (the proxy has no desktop session to draw an icon in), so each OS ties the two together differently:

- **Linux** — packaged as a systemd user unit, `xtreamium-tray.service`, bound to `xtreamium-proxy.service`: starting the proxy starts the tray (`Wants=` on the proxy side), stopping or restarting the proxy stops or restarts it (`PartOf=`), and it won't start by itself while the proxy is down (`Requisite=`). A `graphical-session.target.wants` link starts it when the desktop session comes up after the proxy, which is the usual order at login. With no display available (e.g. a linger-started proxy with nobody logged in) the tray is skipped rather than failing. Quitting from the tray menu leaves it stopped until the proxy next starts or restarts.
- **Windows** — the Velopack first-run hook launches the tray after installing the service, and the `HKCU\...\Run` entry starts it at login. The tray watches the `XtreamiumProxy` service and exits once it has stayed stopped for 60s (long enough to ride out an auto-update restart). Known gap: if the service is started again by hand after the tray has exited, the tray doesn't come back until the next login. If no such service exists (proxy run from a console) the tray never auto-exits.

## Running

```
dotnet run
```

Requires the proxy to be running (or it will simply show "disconnected" and retry with backoff until it appears).

## Scope notes

- Status-only by design — no service start/stop/restart from the tray.
- macOS: the autostart/tray code path exists but there is no packaging (`.app` bundle, notarization, CI) yet, matching xtreamium-proxy's own current lack of macOS distribution.
- Packaging: the release workflow (`.github/workflows/build-installers.yaml`) publishes the tray alongside the proxy and bundles it into the Velopack installer (Windows) and the tarball, `.deb`, `.rpm` and Arch packages (Linux).

## Tests

```
dotnet test
```
