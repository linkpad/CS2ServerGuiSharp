#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Types;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading array values from entity properties using appropriate span types.
/// </summary>
public class ArrayValueReader
{
    private readonly ILogger<ArrayValueReader> _logger;

    // Single source of truth for all supported array types and their handlers
    private static readonly Dictionary<string, Action<nint, int, List<string>>> TypeHandlers = new()
    {
        ["uint8"] = CreateHandler<byte>(),
        ["uint16"] = CreateHandler<ushort>(),
        ["uint32"] = CreateHandler<uint>(),
        ["uint64"] = CreateHandler<ulong>(),
        ["int8"] = CreateHandler<sbyte>(),
        ["int16"] = CreateHandler<short>(),
        ["int32"] = CreateHandler<int>(),
        ["int64"] = CreateHandler<long>(),
        ["float32"] = CreateHandler<float>(),
        ["bool"] = CreateHandler<bool>(),
        ["Vector"] = CreateHandler<Vector>(),
        ["VectorWS"] = CreateHandler<Vector>(),
        ["CNetworkVelocityVector"] = CreateHandler<Vector>(),
        ["CNetworkViewOffsetVector"] = CreateHandler<Vector>(),
        ["QAngle"] = CreateHandler<Vector>(),
    };

    public ArrayValueReader(ILogger<ArrayValueReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads array values using the appropriate span type based on the element type.
    /// Returns a string representation of the array values.
    /// </summary>
    public List<string>? ReadArrayValues(nint arrayPtr, string arrayType, int arraySize)
    {
        var values = new List<string>();

        arrayType = NormalizeType(arrayType);

        if (!TypeHandlers.TryGetValue(arrayType, out var handler))
        {
            _logger.LogWarning("Unsupported array element type: {ArrayType}", arrayType);
            return null;
        }

        try
        {
            handler(arrayPtr, arraySize, values);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading array values of type {ArrayType}", arrayType);
            return null;
        }

        return values.Count > 0 ? values : null;
    }

    /// <summary>
    /// Checks if the given type is an array element type that this reader can handle.
    /// </summary>
    public bool CanHandle(string type)
    {
        type = NormalizeType(type);
        return TypeHandlers.ContainsKey(type);
    }

    /// <summary>
    /// Generic helper to read values from a span and add them to the list.
    /// </summary>
    private static void ReadSpanValues<T>(Span<T> span, List<string> values) where T : struct
    {
        foreach (var value in span)
        {
            values.Add(value.ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// Unsafe helper to read values from a pointer and add them to the list.
    /// </summary>
    private static void ReadSpanValuesUnsafe<T>(nint ptr, int size, List<string> values) where T : struct
    {
        unsafe
        {
            var voidPtr = (void*)ptr;
            ReadSpanValues(new Span<T>(voidPtr, size), values);
        }
    }

    private static string NormalizeType(string type)
    {
        return type;
    }

    private static Action<nint, int, List<string>> CreateHandler<T>() where T : struct
    {
        return ReadSpanValuesUnsafe<T>;
    }
}

