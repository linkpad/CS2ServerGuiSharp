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
        unsafe
        {
            var voidPtr = (void*)arrayPtr;
            var values = new List<string>();

            switch (arrayType)
            {
                case "uint8":
                    ReadSpanValues(new Span<byte>(voidPtr, arraySize), values);
                    break;

                case "uint16":
                    ReadSpanValues(new Span<ushort>(voidPtr, arraySize), values);
                    break;

                case "uint32":
                    ReadSpanValues(new Span<uint>(voidPtr, arraySize), values);
                    break;

                case "uint64":
                    ReadSpanValues(new Span<ulong>(voidPtr, arraySize), values);
                    break;

                case "int8":
                    ReadSpanValues(new Span<sbyte>(voidPtr, arraySize), values);
                    break;

                case "int16":
                    ReadSpanValues(new Span<short>(voidPtr, arraySize), values);
                    break;

                case "int32":
                    ReadSpanValues(new Span<int>(voidPtr, arraySize), values);
                    break;

                case "int64":
                    ReadSpanValues(new Span<long>(voidPtr, arraySize), values);
                    break;

                case "float32":
                    ReadSpanValues(new Span<float>(voidPtr, arraySize), values);
                    break;

                case "bool":
                    ReadSpanValues(new Span<bool>(voidPtr, arraySize), values);
                    break;

                case "Vector" or "VectorWS" or "CNetworkVelocityVector" or "CNetworkViewOffsetVector" or "QAngle":
                    ReadSpanValues(new Span<Vector>(voidPtr, arraySize), values);
                    break;

                default:
                    _logger.LogWarning("Unsupported array element type: {ArrayType}", arrayType);
                    return null;
            }

            return values.Count > 0 ? values : null;
        }
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
}

