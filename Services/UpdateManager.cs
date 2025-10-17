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
            // Only initialize update manager if we're running from Velopack installation
            if (VelopackApp.Build().IsInstalled)
            {
                var source = new GithubSource(_updateUrl, null, false);
                _updateManager = new Velopack.UpdateManager(source);
                _logger.LogInformation("Update manager initialized with URL: {Url}", _updateUrl);
            }
            else
            {
                _logger.LogInformation("Not running from Velopack installation, updates disabled");
            }
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
                // First run after installation - version parameter available
                // Can create shortcuts, configure service, etc.
            })
            .WithAfterInstallFastCallback((v) => {
                // Quick post-install actions
            })
            .WithBeforeUninstallFastCallback((v) => {
                // Quick pre-uninstall actions
            })
            .Run();
    }

    public void Dispose()
    {
        _updateManager?.Dispose();
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
