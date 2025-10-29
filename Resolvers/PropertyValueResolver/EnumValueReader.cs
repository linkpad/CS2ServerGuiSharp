#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using ServerGui;
using ServerGui.Schemas.Enums;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading enum values from entity properties using a generic unsafe approach.
/// </summary>
public class EnumValueReader
{
    private readonly ILogger<EnumValueReader> _logger;

    // Cache of enum type names to their Type objects
    private static readonly Dictionary<string, Type> EnumTypes = InitializeEnumTypes();

    public EnumValueReader(ILogger<EnumValueReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the string representation of an enum property value.
    /// </summary>
    public string? GetValue(nint entityPtr, string classname, string propertyName, string enumType)
    {
        enumType = NormalizeType(enumType);

        // Special handling for Color
        if (enumType == "Color")
        {
            return GetColorValue(entityPtr, classname, propertyName);
        }

        // Look up the enum type and cast directly to it
        if (!EnumTypes.TryGetValue(enumType, out var enumTypeObj))
        {
            return null;
        }

        try
        {
            return GetEnumValueDynamic(entityPtr, classname, propertyName, enumTypeObj);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading enum property {PropertyName} of type {EnumType}", propertyName, enumType);
            return null;
        }
    }

    /// <summary>
    /// Checks if the given type is an enum that this reader can handle.
    /// </summary>
    public bool CanHandle(string type)
    {
        type = NormalizeType(type);
        return type == "Color" || EnumTypes.ContainsKey(type);
    }

    /// <summary>
    /// Gets an enum value dynamically by casting directly to the enum type.
    /// </summary>
    private static string? GetEnumValueDynamic(nint entityPtr, string classname, string propertyName, Type enumType)
    {
        var underlyingType = Enum.GetUnderlyingType(enumType);
        
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            
            // Read the raw value based on underlying type
            object rawValue = underlyingType.Name switch
            {
                "Byte" => *(byte*)(entityPtr + netvarOffset),
                "SByte" => *(sbyte*)(entityPtr + netvarOffset),
                "UInt16" => *(ushort*)(entityPtr + netvarOffset),
                "Int16" => *(short*)(entityPtr + netvarOffset),
                "UInt32" => *(uint*)(entityPtr + netvarOffset),
                "Int32" => *(int*)(entityPtr + netvarOffset),
                "UInt64" => *(ulong*)(entityPtr + netvarOffset),
                "Int64" => *(long*)(entityPtr + netvarOffset),
                _ => throw new NotSupportedException($"Unsupported enum underlying type: {underlyingType.Name}")
            };
            
            // Cast to the enum type and get string representation
            var enumValue = Enum.ToObject(enumType, rawValue);
            return $"{enumValue} ({rawValue})";
        }
    }

    /// <summary>
    /// Special handling for Color type to format it nicely.
    /// </summary>
    private static string? GetColorValue(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var color = *(Color*)(entityPtr + netvarOffset);
            return $"Color({color.R}, {color.G}, {color.B})";
        }
    }

    private static string NormalizeType(string type)
    {
        return type;
    }

    /// <summary>
    /// Initializes the enum types dictionary by automatically discovering all enums from Schemas.Enums.
    /// </summary>
    private static Dictionary<string, Type> InitializeEnumTypes()
    {
        var enumTypes = new Dictionary<string, Type>();

        // Automatically discover all enum types from ServerGui.Schemas.Enums namespace
        var enumsNamespace = "ServerGui.Schemas.Enums";
        var discoveredEnumTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsEnum && t.Namespace == enumsNamespace && t.Name != "Color")
            .ToList();

        foreach (var enumType in discoveredEnumTypes)
        {
            enumTypes[enumType.Name] = enumType;
        }

        return enumTypes;
    }
}

