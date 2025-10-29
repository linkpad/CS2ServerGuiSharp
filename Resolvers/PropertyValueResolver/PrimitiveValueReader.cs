#nullable enable
using System;
using Microsoft.Extensions.Logging;
using ServerGui;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading primitive values (numeric, string, vector types) from entity properties.
/// </summary>
public class PrimitiveValueReader
{
    private readonly ILogger<PrimitiveValueReader> _logger;

    public PrimitiveValueReader(ILogger<PrimitiveValueReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the string representation of a primitive property value based on its type.
    /// </summary>
    public string? GetValue(nint entityPtr, string classname, string propertyName, string type)
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
                // Numeric types
                "float32" => SchemaSystem.GetNetVarFloat(entityPtr, classname, propertyName).ToString(),
                "int32" => SchemaSystem.GetNetVarInt32(entityPtr, classname, propertyName).ToString(),
                "int64" => SchemaSystem.GetNetVarInt64(entityPtr, classname, propertyName).ToString(),
                "bool" => SchemaSystem.GetNetVarBool(entityPtr, classname, propertyName).ToString(),
                "uint8" => SchemaSystem.GetNetVarByte(entityPtr, classname, propertyName).ToString(),
                "uint16" => SchemaSystem.GetNetVarUInt16(entityPtr, classname, propertyName).ToString(),
                "uint32" => SchemaSystem.GetNetVarUInt32(entityPtr, classname, propertyName).ToString(),
                "uint64" => SchemaSystem.GetNetVarUInt64(entityPtr, classname, propertyName).ToString(),
                
                // String types
                "string" => SchemaSystem.GetNetVarString(entityPtr, classname, propertyName),
                "CUtlString" => SchemaSystem.GetNetVarUtlString(entityPtr, classname, propertyName).ToString(),
                "CUtlSymbolLarge" => SchemaSystem.GetNetVarUtlSymbolLarge(entityPtr, classname, propertyName).ToString(),
                
                // Vector types
                "Vector" or "VectorWS" or "CNetworkVelocityVector" or "CNetworkViewOffsetVector" or "QAngle"
                    => SchemaSystem.GetNetVarVector(entityPtr, classname, propertyName).ToString(),
                
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading primitive property {PropertyName} of type {Type}", propertyName, type);
            return null;
        }
    }

    /// <summary>
    /// Checks if the given type is a primitive type that this reader can handle.
    /// </summary>
    public bool CanHandle(string type)
    {
        if (type.Contains("char"))
        {
            type = "string";
        }

        return type switch
        {
            "float32" or "int32" or "int64" or "bool" or "uint8" or "uint16" or "uint32" or "uint64" => true,
            "string" or "CUtlString" or "CUtlSymbolLarge" => true,
            "Vector" or "VectorWS" or "CNetworkVelocityVector" or "CNetworkViewOffsetVector" or "QAngle" => true,
            _ => false
        };
    }
}

