#nullable enable
using System;
using System.Collections.Generic;
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

    // Single source of truth for all supported enum types and their handlers
    private static readonly Dictionary<string, Func<nint, string, string, string?>> TypeHandlers = new()
    {
        ["Color"] = GetColorValue,
        ["HitGroup_t"] = CreateHandler<HitGroupType>(),
        ["RenderFx_t"] = CreateHandler<RenderFx>(),
        ["RenderMode_t"] = CreateHandler<RenderMode>(),
        ["MoveType_t"] = CreateHandler<MoveType>(),
        ["MoveCollide_t"] = CreateHandler<MoveCollideType>(),
        ["TakeDamageFlags_t"] = CreateHandler<TakeDamageFlags>(),
        ["DecalMode_t"] = CreateHandler<byte>(),
    };

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

        if (!TypeHandlers.TryGetValue(enumType, out var handler))
        {
            return null;
        }

        try
        {
            return handler(entityPtr, classname, propertyName);
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
        return TypeHandlers.ContainsKey(type);
    }

    /// <summary>
    /// Gets a generic enum value using unsafe code.
    /// </summary>
    private static string? GetEnumValue<T>(nint entityPtr, string classname, string propertyName) where T : unmanaged
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

    private static Func<nint, string, string, string?> CreateHandler<T>() where T : unmanaged
    {
        return GetEnumValue<T>;
    }
}

