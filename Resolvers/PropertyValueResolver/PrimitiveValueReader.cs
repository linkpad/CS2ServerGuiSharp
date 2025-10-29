#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ServerGui;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading primitive values (numeric, string, vector types) from entity properties.
/// </summary>
public class PrimitiveValueReader
{
    private readonly ILogger<PrimitiveValueReader> _logger;

    // Single source of truth for all supported types and their handlers
    private static readonly Dictionary<string, Func<nint, string, string, string?>> TypeHandlers = new()
    {
        // Numeric types
        ["float32"] = (ptr, cls, prop) => SchemaSystem.GetNetVarFloat(ptr, cls, prop).ToString(),
        ["int32"] = (ptr, cls, prop) => SchemaSystem.GetNetVarInt32(ptr, cls, prop).ToString(),
        ["int64"] = (ptr, cls, prop) => SchemaSystem.GetNetVarInt64(ptr, cls, prop).ToString(),
        ["bool"] = (ptr, cls, prop) => SchemaSystem.GetNetVarBool(ptr, cls, prop).ToString(),
        ["uint8"] = (ptr, cls, prop) => SchemaSystem.GetNetVarByte(ptr, cls, prop).ToString(),
        ["uint16"] = (ptr, cls, prop) => SchemaSystem.GetNetVarUInt16(ptr, cls, prop).ToString(),
        ["uint32"] = (ptr, cls, prop) => SchemaSystem.GetNetVarUInt32(ptr, cls, prop).ToString(),
        ["uint64"] = (ptr, cls, prop) => SchemaSystem.GetNetVarUInt64(ptr, cls, prop).ToString(),
        
        // String types
        ["string"] = (ptr, cls, prop) => SchemaSystem.GetNetVarString(ptr, cls, prop),
        ["CUtlString"] = (ptr, cls, prop) => SchemaSystem.GetNetVarUtlString(ptr, cls, prop).ToString(),
        ["CUtlSymbolLarge"] = (ptr, cls, prop) => SchemaSystem.GetNetVarUtlSymbolLarge(ptr, cls, prop).ToString(),
        
        // Vector types (all map to the same handler)
        ["Vector"] = (ptr, cls, prop) => SchemaSystem.GetNetVarVector(ptr, cls, prop).ToString(),
        ["VectorWS"] = (ptr, cls, prop) => SchemaSystem.GetNetVarVector(ptr, cls, prop).ToString(),
        ["CNetworkVelocityVector"] = (ptr, cls, prop) => SchemaSystem.GetNetVarVector(ptr, cls, prop).ToString(),
        ["CNetworkViewOffsetVector"] = (ptr, cls, prop) => SchemaSystem.GetNetVarVector(ptr, cls, prop).ToString(),
        ["QAngle"] = (ptr, cls, prop) => SchemaSystem.GetNetVarVector(ptr, cls, prop).ToString(),
    };

    public PrimitiveValueReader(ILogger<PrimitiveValueReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the string representation of a primitive property value based on its type.
    /// </summary>
    public string? GetValue(nint entityPtr, string classname, string propertyName, string type)
    {
        type = NormalizeType(type);

        if (!TypeHandlers.TryGetValue(type, out var handler))
        {
            return null;
        }

        try
        {
            return handler(entityPtr, classname, propertyName);
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
        type = NormalizeType(type);
        return TypeHandlers.ContainsKey(type);
    }

    private static string NormalizeType(string type)
    {
        return type.Contains("char") ? "string" : type;
    }
}

