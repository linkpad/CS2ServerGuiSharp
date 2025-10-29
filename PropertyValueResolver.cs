#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace ServerGui;

/// <summary>
/// Information about a custom type that requires special handling.
/// </summary>
public class CustomTypeInfo
{
    public CustomTypeKind Kind { get; init; }
    public nint TargetPtr { get; init; }
    public string SchemaClassname { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public IBaseEntity? ReferencedEntity { get; init; }
    public int? ArraySize { get; init; }
    public string ArrayType { get; init; } = string.Empty;
}

/// <summary>
/// The kind of custom type being handled.
/// </summary>
public enum CustomTypeKind
{
    /// <summary>CHandle&lt;T&gt; - Entity handle reference</summary>
    EntityHandle,
    /// <summary>Pointer type (*)</summary>
    Pointer,
    /// <summary>Array type ([N])</summary>
    Array,
    /// <summary>Embedded/Value custom type</summary>
    Embedded,
    /// <summary>Null or invalid pointer</summary>
    Null
}

/// <summary>
/// Handles property value resolution for different schema types.
/// To add a new type mapping, simply add a new case to the GetPropertyValue method.
/// </summary>
public class PropertyValueResolver
{
    private readonly InterfaceBridge _bridge;
    private readonly ILogger<PropertyValueResolver> _logger;

    public PropertyValueResolver(InterfaceBridge bridge, ILogger<PropertyValueResolver> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    /// <summary>
    /// Special marker value indicating this is a custom type that requires special handling
    /// </summary>
    private const string CUSTOM_TYPE_MARKER = "__CUSTOM_TYPE__";

    /// <summary>
    /// Gets the string representation of a property value based on its type.
    /// </summary>
    public string GetPropertyValue(nint entityPtr, string classname, string propertyName, string type)
    {
        // Normalize type names
        if (type.Contains("char"))
        {
            type = "string";
        }

        try
        {
            return type switch
            {
                "float32" => GetFloatValue(entityPtr, classname, propertyName),
                "int32" => GetInt32Value(entityPtr, classname, propertyName),
                "int64" => GetInt64Value(entityPtr, classname, propertyName),
                "bool" => GetBoolValue(entityPtr, classname, propertyName),
                "uint8" => GetUInt8Value(entityPtr, classname, propertyName),
                "uint16" => GetUInt16Value(entityPtr, classname, propertyName),
                "uint32" => GetUInt32Value(entityPtr, classname, propertyName),
                "uint64" => GetUInt64Value(entityPtr, classname, propertyName),
                "string" => GetStringValue(entityPtr, classname, propertyName),
                "CUtlString" => GetUtlStringValue(entityPtr, classname, propertyName),
                "CUtlSymbolLarge" => GetUtlSymbolLargeValue(entityPtr, classname, propertyName),
                "Vector" or "VectorWS" or "CNetworkVelocityVector" or "CNetworkViewOffsetVector" or "QAngle" 
                    => GetVectorValue(entityPtr, classname, propertyName),
                "Color" => GetColor(entityPtr, classname, propertyName) ?? "Unknown color",
                "HitGroup_t" => GetHitGroup(entityPtr, classname, propertyName) ?? "Unknown hit group",
                "RenderFx_t" => GetRenderFx(entityPtr, classname, propertyName) ?? "Unknown render fx",
                "RenderMode_t" => GetRenderMode(entityPtr, classname, propertyName) ?? "Unknown render mode",
                "MoveType_t" => GetMoveType(entityPtr, classname, propertyName) ?? "Unknown move type",
                "MoveCollide_t" => GetMoveCollide(entityPtr, classname, propertyName) ?? "Unknown move collide",
                "TakeDamageFlags_t" => GetTakeDamageFlags(entityPtr, classname, propertyName) ?? "Unknown take damage flags",
                _ => IsCustomType(type) ? CUSTOM_TYPE_MARKER : $"Unknown type: {type}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving property {PropertyName} of type {Type}", propertyName, type);
            return "Error reading value";
        }
    }

    #region Numeric Type Getters

    private string GetFloatValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarFloat(entityPtr, classname, propertyName).ToString();
    }

    private string GetInt32Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarInt32(entityPtr, classname, propertyName).ToString();
    }

    private string GetInt64Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarInt64(entityPtr, classname, propertyName).ToString();
    }

    private string GetBoolValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarBool(entityPtr, classname, propertyName).ToString();
    }

    private string GetUInt8Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarByte(entityPtr, classname, propertyName).ToString();
    }

    private string GetUInt16Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarUInt16(entityPtr, classname, propertyName).ToString();
    }

    private string GetUInt32Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarUInt32(entityPtr, classname, propertyName).ToString();
    }

    private string GetUInt64Value(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarUInt64(entityPtr, classname, propertyName).ToString();
    }

    #endregion

    #region String Type Getters

    private string GetStringValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarString(entityPtr, classname, propertyName);
    }

    private string GetUtlStringValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarUtlString(entityPtr, classname, propertyName).ToString();
    }

    private string GetUtlSymbolLargeValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarUtlSymbolLarge(entityPtr, classname, propertyName).ToString();
    }

    #endregion

    #region Vector Type Getters

    private string GetVectorValue(nint entityPtr, string classname, string propertyName)
    {
        return SchemaSystem.GetNetVarVector(entityPtr, classname, propertyName).ToString();
    }

    #endregion

    /// <summary>
    /// Checks if the given type is a custom type that requires special handling.
    /// </summary>
    public bool IsCustomType(string type)
    {
        // var allowedCustomTypes = new[] { "CCollisionProperty", "CEntityIOOutput", "GameTick_t", "GameTime_t", "CHandle", "CEntityIdentity", "*" };
        // return allowedCustomTypes.Any(allowedType => type.Contains(allowedType));
        return true;
    }

    /// <summary>
    /// Checks if the result from GetPropertyValue indicates a custom type.
    /// </summary>
    public static bool IsCustomTypeResult(string result)
    {
        return result == CUSTOM_TYPE_MARKER;
    }

    private string? GetColor(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var color = *(Sharp.Shared.Types.Color*)entityPtr.Add(netvarOffset);
            return $"Color({color.R}, {color.G}, {color.B})";
        }
    }

    private string? GetHitGroup(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var hitGroup = *(Sharp.Shared.Enums.HitGroupType*)entityPtr.Add(netvarOffset);
            return hitGroup.ToString();
        }
    }

    private string? GetRenderFx(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var renderFx = *(Sharp.Shared.Enums.RenderFx*)entityPtr.Add(netvarOffset);
            return renderFx.ToString();
        }
    }

    private string? GetRenderMode(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var renderMode = *(Sharp.Shared.Enums.RenderMode*)entityPtr.Add(netvarOffset);
            return renderMode.ToString();
        }
    }
    private string? GetMoveType(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var moveType = *(Sharp.Shared.Enums.MoveType*)entityPtr.Add(netvarOffset);
            return moveType.ToString();
        }
    }

    private string? GetMoveCollide(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var moveCollide = *(Sharp.Shared.Enums.MoveCollideType*)entityPtr.Add(netvarOffset);
            return moveCollide.ToString();
        }
    }
    private string? GetTakeDamageFlags(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var takeDamageFlags = *(Sharp.Shared.Enums.TakeDamageFlags*)entityPtr.Add(netvarOffset);
            return takeDamageFlags.ToString();
        }
    }

    /// <summary>
    /// Reads array values using the appropriate span type based on the element type.
    /// Returns a string representation of the array values.
    /// </summary>
    public List<string>? ReadArrayValues(nint arrayPtr, string arrayType, int arraySize)
    {
        unsafe
        {
            var voidPtr = (void*)arrayPtr;
            var values = new List<string>();
            
            switch (arrayType)
            {
                case "uint8":
                    var byteValues = new Span<byte>(voidPtr, arraySize);
                    foreach (var value in byteValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "uint16":
                    var ushortValues = new Span<ushort>(voidPtr, arraySize);
                    foreach (var value in ushortValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "uint32":
                    var uintValues = new Span<uint>(voidPtr, arraySize);
                    foreach (var value in uintValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "uint64":
                    var ulongValues = new Span<ulong>(voidPtr, arraySize);
                    foreach (var value in ulongValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "int8":
                    var sbyteValues = new Span<sbyte>(voidPtr, arraySize);
                    foreach (var value in sbyteValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "int16":
                    var shortValues = new Span<short>(voidPtr, arraySize);
                    foreach (var value in shortValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "int32":
                    var intValues = new Span<int>(voidPtr, arraySize);
                    foreach (var value in intValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "int64":
                    var longValues = new Span<long>(voidPtr, arraySize);
                    foreach (var value in longValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "float32":
                    var floatValues = new Span<float>(voidPtr, arraySize);
                    foreach (var value in floatValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                    
                case "bool":
                    var boolValues = new Span<bool>(voidPtr, arraySize);
                    foreach (var value in boolValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                case "Vector" or "VectorWS" or "CNetworkVelocityVector" or "CNetworkViewOffsetVector" or "QAngle":
                    var vectorValues = new Span<Vector>(voidPtr, arraySize);
                    foreach (var value in vectorValues)
                    {
                        values.Add(value.ToString());
                    }
                    break;
                default:
                    _logger.LogWarning("Unsupported array element type: {arrayTypeName}", arrayType);
                    return null;
            }

            return values.Count > 0 ? values : null;
        }
    }

    /// <summary>
    /// Resolves information about a custom type property.
    /// </summary>
    public CustomTypeInfo? ResolveCustomType(nint entityPtr, string classname, string fieldName, string fieldType)
    {
        try
        {
            var schemaClassname = fieldType.Replace("*", "");
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, fieldName);

            // Handle CHandle<T> types
            if (fieldType.Contains("CHandle<"))
            {
                return ResolveEntityHandleType(entityPtr, netvarOffset, schemaClassname, fieldType);
            }

            // Handle pointer types
            if (fieldType.Contains("*"))
            {
                return ResolvePointerType(entityPtr, classname, fieldName, schemaClassname, fieldType);
            }

            // Handle array types
            if (fieldType.Contains("[") && fieldType.Contains("]"))
            {
                return ResolveArrayType(entityPtr, netvarOffset, schemaClassname, fieldType);
            }

            // Handle embedded/value custom types
            return ResolveEmbeddedType(entityPtr, netvarOffset, schemaClassname, fieldType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving custom type {FieldName} of type {FieldType}", fieldName, fieldType);
            return null;
        }
    }

    /// <summary>
    /// Resolves an entity handle (CHandle&lt;T&gt;) type.
    /// </summary>
    private CustomTypeInfo ResolveEntityHandleType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var handlePtr = entityPtr.Add(netvarOffset);
        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.EntityHandle,
            TargetPtr = handlePtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }

    /// <summary>
    /// Resolves a pointer type.
    /// </summary>
    private CustomTypeInfo ResolvePointerType(nint entityPtr, string classname, string fieldName, string schemaClassname, string fieldType)
    {
        var targetPtr = SchemaSystem.GetNetVarPointer(entityPtr, classname, fieldName);
        if (targetPtr == nint.Zero)
        {
            return new CustomTypeInfo
            {
                Kind = CustomTypeKind.Null,
                TargetPtr = nint.Zero,
                SchemaClassname = schemaClassname,
                FieldType = fieldType
            };
        }

        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Pointer,
            TargetPtr = targetPtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }

    /// <summary>
    /// Resolves an array type.
    /// </summary>
    private CustomTypeInfo ResolveArrayType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var arraySizeStr = fieldType.Split('[')[1].Split(']')[0];
        var arrayType = fieldType.Split('[')[0];

        int? arraySize = null;
        if (int.TryParse(arraySizeStr, out var parsedSize))
        {
            arraySize = parsedSize;
        }

        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Array,
            TargetPtr = entityPtr.Add(netvarOffset),
            SchemaClassname = schemaClassname,
            FieldType = fieldType,
            ArraySize = arraySize,
            ArrayType = arrayType
        };
    }

    /// <summary>
    /// Resolves an embedded/value custom type.
    /// </summary>
    private CustomTypeInfo ResolveEmbeddedType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var embeddedPtr = entityPtr.Add(netvarOffset);
        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Embedded,
            TargetPtr = embeddedPtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }

}

