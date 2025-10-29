#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace ServerGui;

/// <summary>
/// Handles rendering of entity properties in the ImGui table.
/// </summary>
public class EntityPropertyRenderer
{
    private readonly ILogger<EntityPropertyRenderer> _logger;
    private readonly PropertyValueResolver _valueResolver;
    private readonly InterfaceBridge _bridge;
    private readonly Action<IBaseEntity>? _onEntitySelected;
    private string _propertyFilter = "";

    public EntityPropertyRenderer(ILogger<EntityPropertyRenderer> logger, PropertyValueResolver valueResolver, InterfaceBridge bridge, Action<IBaseEntity>? onEntitySelected = null)
    {
        _logger = logger;
        _valueResolver = valueResolver;
        _bridge = bridge;
        _onEntitySelected = onEntitySelected;
    }

    public string PropertyFilter
    {
        get => _propertyFilter;
        set => _propertyFilter = value;
    }

    public void DrawEntityInfo(IBaseEntity entity)
    {
        if (entity == null) return;

        ImGui.Text($"Classname: {entity.Classname}");
        ImGui.Text($"Index: {entity.Index}");

        // Property search filter
        ImGui.InputText("Property Search", ref _propertyFilter, 256);

        if (ImGui.BeginTable("Schema", 3, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            try
            {
                DrawEntityProperties(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying entity properties");
                ImGui.Text("Error loading entity properties");
            }

            ImGui.EndTable();
        }
    }

    private void DrawEntityProperties(ISchemaObject entity)
    {
        try
        {
            var schemaInfo = Sharp.Shared.SharedGameObject.SchemaInfo[entity.GetSchemaClassname()];
            DumpEntitySchema(entity.GetAbsPtr(), schemaInfo, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error drawing entity schema");
            ImGui.Text("Error loading entity schema");
        }
    }

    private void DumpEntitySchema(nint entityPtr, SchemaClass schemaInfo, bool root)
    {
        if (schemaInfo == null) return;

        // Display schema class name
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), schemaInfo.ClassName);

        // Process fields
        if (schemaInfo.Fields != null)
        {
            foreach (var field in schemaInfo.Fields)
            {
                // Apply property filter for root level
                if (root && !string.IsNullOrEmpty(_propertyFilter) &&
                    !field.Key.Contains(_propertyFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    // Handle properties using the value resolver
                    var value = _valueResolver.GetPropertyValue(entityPtr, schemaInfo.ClassName, field.Key, field.Value.Type);

                    // Check if this is a custom type that needs special handling (entity handles, pointers, arrays, etc.)
                    if (PropertyValueResolver.IsCustomTypeResult(value))
                    {
                        var customTypeInfo = _valueResolver.ResolveCustomType(entityPtr, schemaInfo.ClassName, field.Key, field.Value.Type);
                        if (customTypeInfo != null)
                        {
                            RenderCustomType(field.Key, customTypeInfo);
                            continue;
                        }
                    }

                    // Handle regular properties
                    DrawProperty(field.Key, field.Value.Type, value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing field {FieldName} - type {FieldType}", field.Key, field.Value.Type);
                }
            }
        }

        // this should be called to get all fields from all base classes, but atm its kinda bugged out
        // as the schemaInfo.Fields already contains all fields from the base classes (it looks like)
        /* 
        if (schemaInfo.BaseClasses != null)
        {
            foreach (var baseClass in schemaInfo.BaseClasses)
            {
                if (baseClass == schemaInfo.ClassName) continue;
                if (Sharp.Shared.SharedGameObject.SchemaInfo.ContainsKey(baseClass))
                {
                    var baseSchemaInfo = Sharp.Shared.SharedGameObject.SchemaInfo[baseClass];
                    DumpEntitySchema(entityPtr, baseSchemaInfo, true);
                }
            }
        } */
    }

    /// <summary>
    /// Renders a custom type based on the resolved information.
    /// </summary>
    private void RenderCustomType(string fieldName, CustomTypeInfo customTypeInfo)
    {
        // Handle array types - render as a simple property
        if (customTypeInfo.Kind == CustomTypeKind.Array)
        {
            RenderArrayType(fieldName, customTypeInfo);
            return;
        }

        // Handle entity handle types - resolve the handle here since we have access to unsafe code
        if (customTypeInfo.Kind == CustomTypeKind.EntityHandle)
        {
            RenderEntityHandleType(fieldName, customTypeInfo);
            return;
        }

        // Handle nested schema types (pointer, embedded)
        RenderNestedSchemaType(fieldName, customTypeInfo);
    }

    /// <summary>
    /// Renders an array type using DrawProperty.
    /// </summary>
    private void RenderArrayType(string fieldName, CustomTypeInfo customTypeInfo)
    {
        if (customTypeInfo.ArraySize.HasValue && customTypeInfo.ArraySize.Value > 0)
        {
            try
            {
                var arrayValue = _valueResolver.ReadArrayValues(customTypeInfo.TargetPtr, customTypeInfo.ArrayType, customTypeInfo.ArraySize.Value);
                
                if (arrayValue != null)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text("");
                    ImGui.TableSetColumnIndex(0);
                    bool open = ImGui.TreeNodeEx(fieldName, ImGuiTreeNodeFlags.SpanAllColumns);
                    ImGui.TableNextColumn();
                    ImGui.Text(customTypeInfo.FieldType);
                    ImGui.TableNextColumn();
                    
                    if (open)
                    {
                        for (int index = 0; index < arrayValue.Count; index++)
                        {
                            var value = arrayValue[index];
                            DrawProperty($"{fieldName}[{index}]", customTypeInfo.ArrayType, value);
                        }
                        ImGui.TreePop();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error rendering array {FieldName}", fieldName);
                DrawProperty(fieldName, customTypeInfo.FieldType, "Error reading array");
            }
        }
        else
        {
            DrawProperty(fieldName, customTypeInfo.FieldType, "Invalid array size");
        }
    }

    /// <summary>
    /// Renders an entity handle type.
    /// </summary>
    private void RenderEntityHandleType(string fieldName, CustomTypeInfo customTypeInfo)
    {
        var entityFound = ResolveEntityHandle(customTypeInfo.TargetPtr);
        
        if (entityFound != null)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TreeNodeEx(fieldName, ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf);

            ImGui.TableSetColumnIndex(1);
            ImGui.Text(customTypeInfo.FieldType);

            ImGui.TableSetColumnIndex(2);
            if (ImGui.SmallButton($"{entityFound.Classname} ({entityFound.Index})"))
            {
                _logger.LogInformation("Setting selected entity to {Entity}", entityFound.Classname);
                _onEntitySelected?.Invoke(entityFound);
            }
        }
        else
        {
            DrawProperty(fieldName, customTypeInfo.FieldType, "Entity not found");
        }
    }

    /// <summary>
    /// Renders a nested schema type (pointer or embedded).
    /// </summary>
    private void RenderNestedSchemaType(string fieldName, CustomTypeInfo customTypeInfo)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(2);
        ImGui.Text("");
        ImGui.TableSetColumnIndex(0);
        bool open = ImGui.TreeNodeEx(fieldName, ImGuiTreeNodeFlags.SpanAllColumns);
        ImGui.TableNextColumn();
        ImGui.Text(customTypeInfo.FieldType);
        ImGui.TableNextColumn();
        
        if (open)
        {
            if (Sharp.Shared.SharedGameObject.SchemaInfo.ContainsKey(customTypeInfo.SchemaClassname))
            {
                var schemaInfo = Sharp.Shared.SharedGameObject.SchemaInfo[customTypeInfo.SchemaClassname];
                DumpEntitySchema(customTypeInfo.TargetPtr, schemaInfo, false);
            }
            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Resolves an entity handle from a pointer location.
    /// </summary>
    private IBaseEntity? ResolveEntityHandle(nint handlePtr)
    {
        unsafe
        {
            var handlePtr_typed = (CEntityHandle<IBaseEntity>*)handlePtr;
            return _bridge.EntityManager.FindEntityByHandle(*handlePtr_typed);
        }
    }

    private void DrawProperty(string name, string type, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.SpanAllColumns | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf);

        ImGui.TableSetColumnIndex(1);
        ImGui.Text(type);

        ImGui.TableSetColumnIndex(2);
        ImGui.Text(value);
    }
}


