using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#if WINDOWS
using Velopack;
using Velopack.Sources;
#endif

namespace Xtreamium.Proxy.Services;

public class UpdateManager : IDisposable
{
#if WINDOWS
    private readonly ILogger<UpdateManager> _logger;
    private readonly Velopack.UpdateManager? _updateManager;
    private readonly string _updateUrl;

    public UpdateManager(ILogger<UpdateManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _updateUrl = configuration["UpdateUrl"] ?? "https://github.com/xtreamium/xtreamium-proxy";

        try
        {
            var source = new GithubSource(_updateUrl, null, false);
            _updateManager = new Velopack.UpdateManager(source);
            _logger.LogInformation("Update manager initialized with URL: {Url}", _updateUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize update manager");
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (_updateManager == null)
        {
            _logger.LogDebug("Update manager not available");
            return false;
        }

        try
        {
            _logger.LogInformation("Checking for updates...");
            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                _logger.LogInformation("Update available: {Version}", updateInfo.TargetFullRelease.Version);
                return true;
            }

            _logger.LogInformation("No updates available");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return false;
        }
    }

    public async Task<bool> DownloadAndInstallUpdatesAsync()
    {
        if (_updateManager == null)
        {
            _logger.LogWarning("Update manager not available");
            return false;
        }

        try
        {
            _logger.LogInformation("Downloading updates...");
            
            // Check for updates first
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo == null)
            {
                _logger.LogInformation("No updates available");
                return false;
            }

            // Download the update
            await _updateManager.DownloadUpdatesAsync(updateInfo);
            
            _logger.LogInformation("Update downloaded: {Version}. Installing and restarting...", updateInfo.TargetFullRelease.Version);
            
            // Apply the update and restart
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading/installing updates");
            return false;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void HandleVelopackEvents()
    {
        VelopackApp.Build()
            .WithFirstRun((v) => {
                // First run after installation - configure Windows Service
                try
                {
                    // Remove any desktop shortcuts that might have been created
                    RemoveDesktopShortcuts();
                    
                    InstallWindowsService();
                    
                    // Exit immediately after service installation
                    // Don't launch the GUI application
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to install service: {ex.Message}");
                    // Still exit to prevent GUI launch
                    Environment.Exit(1);
                }
            })
            .WithAfterInstallFastCallback((v) => {
                // Quick post-install actions - remove shortcuts
                RemoveDesktopShortcuts();
            })
            .WithAfterUpdateFastCallback((v) => {
                // After update - restart the service instead of launching GUI
                try
                {
                    RemoveDesktopShortcuts();
                    RestartWindowsService();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to restart service: {ex.Message}");
                    Environment.Exit(1);
                }
            })
            .WithBeforeUninstallFastCallback((v) => {
                // Uninstall Windows Service
                try
                {
                    UninstallWindowsService();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to uninstall service: {ex.Message}");
                }
            })
            .Run();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RemoveDesktopShortcuts()
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var commonDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            
            // Remove shortcuts from user desktop
            var userShortcut = Path.Combine(desktopPath, "Xtreamium Proxy.lnk");
            if (File.Exists(userShortcut))
                File.Delete(userShortcut);
            
            var userShortcut2 = Path.Combine(desktopPath, "XtreamiumProxy.lnk");
            if (File.Exists(userShortcut2))
                File.Delete(userShortcut2);
            
            // Remove shortcuts from all users desktop
            var commonShortcut = Path.Combine(commonDesktopPath, "Xtreamium Proxy.lnk");
            if (File.Exists(commonShortcut))
                File.Delete(commonShortcut);
            
            var commonShortcut2 = Path.Combine(commonDesktopPath, "XtreamiumProxy.lnk");
            if (File.Exists(commonShortcut2))
                File.Delete(commonShortcut2);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove desktop shortcuts: {ex.Message}");
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void InstallWindowsService()
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
            return;

        // Get current user account (DOMAIN\Username format)
        var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create XtreamiumProxy binPath= \"{exePath}\" DisplayName= \"Xtreamium Proxy Service\" start= auto obj= \"{userName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();

        if (process?.ExitCode == 0)
        {
            // Start the service
            var startServiceInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "start XtreamiumProxy",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var startProcess = System.Diagnostics.Process.Start(startServiceInfo);
            startProcess?.WaitForExit();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RestartWindowsService()
    {
        // Stop the service
        var stopInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = "stop XtreamiumProxy",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var stopProcess = System.Diagnostics.Process.Start(stopInfo);
        stopProcess?.WaitForExit();

        System.Threading.Thread.Sleep(2000); // Wait for service to stop

        // Start the service
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = "start XtreamiumProxy",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var startProcess = System.Diagnostics.Process.Start(startInfo);
        startProcess?.WaitForExit();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void UninstallWindowsService()
    {
        // Stop the service first
        var stopInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = "stop XtreamiumProxy",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var stopProcess = System.Diagnostics.Process.Start(stopInfo);
        stopProcess?.WaitForExit();

        System.Threading.Thread.Sleep(2000); // Wait for service to stop

        // Delete the service
        var deleteInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = "delete XtreamiumProxy",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var deleteProcess = System.Diagnostics.Process.Start(deleteInfo);
        deleteProcess?.WaitForExit();
    }

    public void Dispose()
    {
        // Velopack UpdateManager doesn't implement IDisposable
        // No cleanup needed
    }
#else
    // Dummy implementation for non-Windows platforms
    private readonly ILogger<UpdateManager> _logger;

    public UpdateManager(ILogger<UpdateManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logger.LogInformation("Update manager not available on this platform");
    }

    public Task<bool> CheckForUpdatesAsync()
    {
        return Task.FromResult(false);
    }

    public Task<bool> DownloadAndInstallUpdatesAsync()
    {
        return Task.FromResult(false);
    }

    public static void HandleVelopackEvents()
    {
        // No-op on non-Windows platforms
    }

    public void Dispose()
    {
        // No-op
    }
#endif
}
