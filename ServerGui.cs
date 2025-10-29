#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Listeners;
using Sharp.Shared.GameEntities;

namespace ServerGui;

public sealed class ServerGui : IModSharpModule
{
    public string DisplayName   => "ServerGui module";
    public string DisplayAuthor => "Linkpad";

    private readonly ILogger<ServerGui> _logger;
    private readonly InterfaceBridge  _bridge;
    private readonly IServiceProvider  _serviceProvider;
    private ImGuiManager? _imguiManager;

    public ServerGui(ISharedSystem sharedSystem,
        string?                  dllPath,
        string?                  sharpPath,
        Version?                 version,
        IConfiguration?          coreConfiguration,
        bool                     hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);

        var configuration = new ConfigurationBuilder()
                            .AddJsonFile(Path.Combine(dllPath, "appsettings.json"), false, false)
                            .Build();

        _bridge          = new InterfaceBridge(dllPath, sharpPath, version, sharedSystem);
        var services = new ServiceCollection();

        services.AddSingleton(sharedSystem);
        services.AddSingleton(_bridge);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(sharedSystem.GetLoggerFactory());
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        _logger          = sharedSystem.GetLoggerFactory().CreateLogger<ServerGui>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        return true;
    }

    public void PostInit()
    {
        // Initialize ImGui
        try
        {
            // run imguiManager in a dedicated detached thread
            var thread = new Thread(() => {
                _imguiManager = new ImGuiManager(
                    _serviceProvider.GetRequiredService<ILogger<ImGuiManager>>(),
                    _serviceProvider.GetRequiredService<IConfiguration>(),
                    _bridge,
                    _serviceProvider);
                if (_imguiManager.Initialize())
                {
                    _logger.LogInformation("ImGui initialized successfully with Win32 D3D9 backend");
                }
                else
                {
                    _logger.LogError("Failed to initialize ImGui");
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ImGui");
        }
    }

    public void Shutdown()
    {
    }
}
