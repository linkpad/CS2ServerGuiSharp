#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using ServerGui;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading enum values from entity properties using a generic unsafe approach.
/// </summary>
public class EnumValueReader
{
    private readonly ILogger<EnumValueReader> _logger;

    public EnumValueReader(ILogger<EnumValueReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the string representation of an enum property value.
    /// </summary>
    public string? GetValue(nint entityPtr, string classname, string propertyName, string enumType)
    {
        try
        {
            return enumType switch
            {
                "Color" => GetColorValue(entityPtr, classname, propertyName),
                "HitGroup_t" => GetEnumValue<HitGroupType>(entityPtr, classname, propertyName),
                "RenderFx_t" => GetEnumValue<RenderFx>(entityPtr, classname, propertyName),
                "RenderMode_t" => GetEnumValue<RenderMode>(entityPtr, classname, propertyName),
                "MoveType_t" => GetEnumValue<MoveType>(entityPtr, classname, propertyName),
                "MoveCollide_t" => GetEnumValue<MoveCollideType>(entityPtr, classname, propertyName),
                "TakeDamageFlags_t" => GetEnumValue<TakeDamageFlags>(entityPtr, classname, propertyName),
                _ => null
            };
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
        return type switch
        {
            "Color" or "HitGroup_t" or "RenderFx_t" or "RenderMode_t" or 
            "MoveType_t" or "MoveCollide_t" or "TakeDamageFlags_t" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a generic enum value using unsafe code.
    /// </summary>
    private string? GetEnumValue<T>(nint entityPtr, string classname, string propertyName) where T : unmanaged
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var enumValue = *(T*)(entityPtr + netvarOffset);
            return enumValue.ToString();
        }
    }

    /// <summary>
    /// Special handling for Color type to format it nicely.
    /// </summary>
    private string? GetColorValue(nint entityPtr, string classname, string propertyName)
    {
        unsafe
        {
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, propertyName);
            var color = *(Color*)(entityPtr + netvarOffset);
            return $"Color({color.R}, {color.G}, {color.B})";
        }
    }
}

