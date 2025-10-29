#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;

namespace ServerGui;

public class EntityBrowser : IEntityListener
{
    private readonly ILogger<EntityBrowser> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IConfiguration _configuration;
    private EntityPropertyRenderer? _propertyRenderer;
    
    private bool _isOpen = false;
    private string _entityFilter = "";
    private IBaseEntity? _selectedEntity;
    private readonly List<IBaseEntity> _entities = new();

    public EntityPropertyRenderer? PropertyRenderer
    {
        get => _propertyRenderer;
        set => _propertyRenderer = value;
    }

    public EntityBrowser(
        ILogger<EntityBrowser> logger,
        InterfaceBridge bridge,
        IConfiguration configuration,
        EntityPropertyRenderer? propertyRenderer = null)
    {
        _logger = logger;
        _bridge = bridge;
        _configuration = configuration;
        _propertyRenderer = propertyRenderer;
        
        // Load initial state from configuration
        _isOpen = _configuration.GetValue<bool>("ImGui:EntityBrowser:IsOpen");
        
        // Install entity listener to track entities
        _bridge.EntityManager.InstallEntityListener(this);
    }

    public void Draw()
    {
        if (!_isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Entity Browser", ref _isOpen))
        {
            int availableHeight = (int)(ImGui.GetWindowSize().Y - 50);

            // Entity List Section
            ImGui.BeginChild("Entity List", new Vector2(0, availableHeight / 2), ImGuiChildFlags.Border);
            DrawEntityList();
            ImGui.EndChild();

            // Entity Info Section
            if (_selectedEntity != null)
            {
                ImGui.PushID(_selectedEntity.Index);
                ImGui.BeginChild("Entity Info", new Vector2(0, availableHeight / 2), ImGuiChildFlags.Border);
                DrawEntityInfo();
                ImGui.EndChild();
                ImGui.PopID();
            }
        }
        ImGui.End();
    }

    private void DrawEntityList()
    {
        // Search filter
        ImGui.InputText("Search", ref _entityFilter, 256);

        if (ImGui.BeginTable("Entity Table", 2))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Entity Index");
            ImGui.TableHeadersRow();

            try
            {
                // Use tracked entities
                foreach (var entity in _entities)
                {
                    // Apply filter
                    if (!string.IsNullOrEmpty(_entityFilter) && 
                        !entity.Classname.Contains(_entityFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    
                    ImGui.PushID(entity.Index);
                    bool isSelected = _selectedEntity?.Index == entity.Index;
                    if (ImGui.Selectable(entity.Classname, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _selectedEntity = entity;
                    }
                    ImGui.PopID();
                    
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{entity.Index}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entities for Entity Browser");
                ImGui.Text("Error loading entities");
            }

            ImGui.EndTable();
        }
    }

    private void DrawEntityInfo()
    {
        if (_selectedEntity == null) return;
        _propertyRenderer?.DrawEntityInfo(_selectedEntity);
    }

    public void ShowWindow()
    {
        _isOpen = true;
        SaveWindowState();
    }

    public void HideWindow()
    {
        _isOpen = false;
        SaveWindowState();
    }

    public void SetSelectedEntity(IBaseEntity? entity)
    {
        _logger.LogInformation("Setting selected entity to {Entity}", entity?.Classname);
        _selectedEntity = entity;
    }

    public void SetSelectedEntityByIndex(int index)
    {
        _selectedEntity = _entities.FirstOrDefault(e => e.Index == index);
    }

    public bool IsOpen => _isOpen;

    private void SaveWindowState()
    {
        try
        {
            // Update configuration with current state
            _configuration["ImGui:EntityBrowser:IsOpen"] = _isOpen.ToString().ToLower();
            
            // Save to file
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
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
            
            // Update the EntityBrowser state
            if (newConfig.ContainsKey("ImGui") && newConfig["ImGui"] is Dictionary<string, object> imguiSection)
            {
                if (!imguiSection.ContainsKey("EntityBrowser"))
                    imguiSection["EntityBrowser"] = new Dictionary<string, object>();
                
                if (imguiSection["EntityBrowser"] is Dictionary<string, object> entityBrowserSection)
                {
                    entityBrowserSection["IsOpen"] = _isOpen;
                }
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
            _logger.LogError(ex, "Failed to save Entity Browser window state");
        }
    }

    // IEntityListener implementation
    public void OnEntitySpawned(IBaseEntity entity)
    {
        _entities.Add(entity);
    }

    public void OnEntityDeleted(IBaseEntity entity)
    {
        _entities.RemoveAll(e => e.Index == entity.Index);
        
        // Clear selection if the selected entity was deleted
        if (_selectedEntity?.Index == entity.Index)
        {
            _selectedEntity = null;
        }
    }

    public int ListenerVersion => IEntityListener.ApiVersion;
    public int ListenerPriority => 0;

    public void Dispose()
    {
        _bridge.EntityManager.RemoveEntityListener(this);
    }
}
