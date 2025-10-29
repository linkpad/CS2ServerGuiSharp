#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ServerGui;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles property value resolution for different schema types.
/// Orchestrates various specialized readers for different type categories.
/// </summary>
public class PropertyValueResolver
{
    /// <summary>
    /// Special marker value indicating this is a custom type that requires special handling
    /// </summary>
    public const string CUSTOM_TYPE_MARKER = "__CUSTOM_TYPE__";

    private readonly PrimitiveValueReader _primitiveReader;
    private readonly EnumValueReader _enumReader;
    private readonly ArrayValueReader _arrayReader;
    private readonly UtlVectorReader _utlVectorReader;
    private readonly CustomTypeResolver _customTypeResolver;
    private readonly ILogger<PropertyValueResolver> _logger;

    public PropertyValueResolver(InterfaceBridge bridge, ILogger<PropertyValueResolver> logger)
        : this(bridge, logger, NullLoggerFactory.Instance)
    {
    }

    public PropertyValueResolver(InterfaceBridge bridge, ILogger<PropertyValueResolver> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        
        // Initialize specialized readers
        _primitiveReader = new PrimitiveValueReader(
            loggerFactory.CreateLogger<PrimitiveValueReader>());
        _enumReader = new EnumValueReader(
            loggerFactory.CreateLogger<EnumValueReader>());
        _arrayReader = new ArrayValueReader(
            loggerFactory.CreateLogger<ArrayValueReader>());
        _utlVectorReader = new UtlVectorReader(bridge,
            loggerFactory.CreateLogger<UtlVectorReader>());
        _customTypeResolver = new CustomTypeResolver(
            loggerFactory.CreateLogger<CustomTypeResolver>());
    }

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
            // Try primitive types first
            if (_primitiveReader.CanHandle(type))
            {
                var value = _primitiveReader.GetValue(entityPtr, classname, propertyName, type);
                if (value != null)
                    return value;
            }

            // Try enum types
            if (_enumReader.CanHandle(type))
            {
                var enumValue = _enumReader.GetValue(entityPtr, classname, propertyName, type);
                if (enumValue != null)
                    return enumValue;
                
                // Provide default message for enum types
                return type switch
                {
                    "Color" => "Unknown color",
                    "HitGroup_t" => "Unknown hit group",
                    "RenderFx_t" => "Unknown render fx",
                    "RenderMode_t" => "Unknown render mode",
                    "MoveType_t" => "Unknown move type",
                    "MoveCollide_t" => "Unknown move collide",
                    "TakeDamageFlags_t" => "Unknown take damage flags",
                    _ => $"Unknown {type}"
                };
            }

            // Check if it's a custom type
            return IsCustomType(type) ? CUSTOM_TYPE_MARKER : $"Unknown type: {type}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving property {PropertyName} of type {Type}", propertyName, type);
            return "Error reading value";
        }
    }

    /// <summary>
    /// Checks if the given type is a custom type that requires special handling.
    /// </summary>
    public bool IsCustomType(string type)
    {
        // Currently returns true for all unrecognized types, allowing custom type resolution to handle them
        // Can be refined to check against a whitelist if needed
        return true;
    }

    /// <summary>
    /// Checks if the result from GetPropertyValue indicates a custom type.
    /// </summary>
    public static bool IsCustomTypeResult(string result)
    {
        return result == CUSTOM_TYPE_MARKER;
    }

    /// <summary>
    /// Reads array values using the appropriate span type based on the element type.
    /// Returns a string representation of the array values.
    /// </summary>
    public List<string>? ReadArrayValues(nint arrayPtr, string arrayType, int arraySize)
    {
        return _arrayReader.ReadArrayValues(arrayPtr, arrayType, arraySize);
    }

    /// <summary>
    /// Reads UtlVector elements based on the element type.
    /// Returns a list of UtlVectorElement objects that can be rendered.
    /// </summary>
    public List<UtlVectorElement>? ReadUtlVectorElements(CustomTypeInfo customTypeInfo)
    {
        return _utlVectorReader.ReadUtlVectorElements(customTypeInfo);
    }

    /// <summary>
    /// Resolves information about a custom type property.
    /// </summary>
    public CustomTypeInfo? ResolveCustomType(nint entityPtr, string classname, string fieldName, string fieldType)
    {
        return _customTypeResolver.ResolveCustomType(entityPtr, classname, fieldName, fieldType);
    }
}

