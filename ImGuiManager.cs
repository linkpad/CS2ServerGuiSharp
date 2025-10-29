#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using ServerGui.Resolvers.PropertyValueResolver;

namespace ServerGui;

public sealed class ImGuiManager
{
    private readonly ILogger<ImGuiManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly InterfaceBridge _bridge;
    private readonly IServiceProvider _serviceProvider;
    private EntityBrowser? _entityBrowser;
    
    public ImGuiManager(ILogger<ImGuiManager> logger, IConfiguration configuration, InterfaceBridge bridge, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _bridge = bridge;
        _serviceProvider = serviceProvider;
    }

    public bool Initialize()
    {
        try
        {
            _logger.LogInformation("ImGui initialized successfully");
            

            // Create a Silk.NET window as usual
            using var window = Window.Create(WindowOptions.Default);
            // window.IsVisible = false;

            // Declare some variables
            ImGuiController? controller = null;
            GL? gl = null;
            IInputContext? inputContext = null;

            // Our loading function
            window.Load += () =>
            {
                controller = new ImGuiController(
                    gl = window.CreateOpenGL(), // load OpenGL
                    window, // pass in our window
                    inputContext = window.CreateInput() // create an input context
                );
                
                // Initialize dependencies
                var propertyValueResolver = new PropertyValueResolver(
                    _bridge,
                    _serviceProvider.GetRequiredService<ILogger<PropertyValueResolver>>(),
                    _serviceProvider.GetRequiredService<ILoggerFactory>()
                );
                
                // Initialize Entity Browser without property renderer
                _entityBrowser = new EntityBrowser(
                    _serviceProvider.GetRequiredService<ILogger<EntityBrowser>>(),
                    _bridge,
                    _configuration
                );
                
                // Create property renderer with callback to EntityBrowser
                var propertyRenderer = new EntityPropertyRenderer(
                    _serviceProvider.GetRequiredService<ILogger<EntityPropertyRenderer>>(),
                    propertyValueResolver,
                    _bridge,
                    _entityBrowser.SetSelectedEntity
                );
                
                // Set the property renderer on EntityBrowser
                _entityBrowser.PropertyRenderer = propertyRenderer;
            };

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                gl?.Viewport(s);
            };

            // The render function
            window.Render += delta =>
            {
                // Make sure ImGui is up-to-date
                controller?.Update((float) delta);

                // This is where you'll do any rendering beneath the ImGui context
                // Here, we just have a blank screen.
                gl?.ClearColor(Color.FromArgb(255, (int) (.45f * 255), (int) (.55f * 255), (int) (.60f * 255)));
                gl?.Clear((uint) ClearBufferMask.ColorBufferBit);

                // Draw the main menu bar
                DrawMainMenuBar();

                // Draw the Entity Browser
                _entityBrowser?.Draw();

                // Draw the welcome modal
                DrawWelcomeModal();

                // Make sure ImGui renders too!
                controller?.Render();
            };

            // The closing function
            window.Closing += () =>
            {
                // Dispose our controller first
                controller?.Dispose();

                // Dispose the input context
                inputContext?.Dispose();

                // Unload OpenGL
                gl?.Dispose();
            };

            // Now that everything's defined, let's run this bad boy!
            window.Run();

            window.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ImGuiManager");
            return false;
        }
    }

    private void DrawMainMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Entity Browser", "Ctrl+E"))
                {
                    _entityBrowser?.ShowWindow();
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    private void DrawWelcomeModal()
    {
        var welcomeSeen = _configuration.GetValue<bool>("ImGui:WelcomeSeen");
        
        if (!welcomeSeen)
        {
            // Mark as seen and save configuration
            _configuration["ImGui:WelcomeSeen"] = "true";
            SaveConfiguration();
            
            ImGui.OpenPopup("Welcome");
        }

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new System.Numerics.Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Welcome", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("ServerGui is a debugging tool designed for development purposes.");
            ImGui.Text("It is not intended for use in production environments.");
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("I understand"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            // Get the configuration file path
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            // Read current configuration
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(configPath, false, false);
            var config = configBuilder.Build();
            
            // Create a new configuration with updated values
            var newConfig = new Dictionary<string, object>();
            
            // Copy existing values
            foreach (var section in config.GetChildren())
            {
                var sectionDict = new Dictionary<string, object>();
                foreach (var item in section.GetChildren())
                {
                    sectionDict[item.Key] = item.Value ?? "";
                }
                newConfig[section.Key] = sectionDict;
            }
            
            // Update the WelcomeSeen value
            if (newConfig.ContainsKey("ImGui") && newConfig["ImGui"] is Dictionary<string, object> imguiSection)
            {
                imguiSection["WelcomeSeen"] = true;
            }
            
            // Write back to file
            var json = System.Text.Json.JsonSerializer.Serialize(newConfig, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
        }
    }
}