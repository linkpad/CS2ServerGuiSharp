#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading UtlVector elements from entity properties.
/// </summary>
public class UtlVectorReader
{
    private readonly InterfaceBridge _bridge;
    private readonly ILogger<UtlVectorReader> _logger;

    /// <summary>
    /// Maps primitive type names to their C# type handlers.
    /// </summary>
    private static readonly Dictionary<string, Func<nint, ILogger<UtlVectorReader>, List<UtlVectorElement>?>> PrimitiveTypeHandlers = new()
    {
        // Signed integers
        ["int8"] = (ptr, logger) => ReadPrimitiveVector<sbyte>(ptr, "int8", logger),
        ["int16"] = (ptr, logger) => ReadPrimitiveVector<short>(ptr, "int16", logger),
        ["int32"] = (ptr, logger) => ReadPrimitiveVector<int>(ptr, "int32", logger),
        ["int"] = (ptr, logger) => ReadPrimitiveVector<int>(ptr, "int", logger),
        ["int64"] = (ptr, logger) => ReadPrimitiveVector<long>(ptr, "int64", logger),

        // Unsigned integers
        ["uint8"] = (ptr, logger) => ReadPrimitiveVector<byte>(ptr, "uint8", logger),
        ["uint16"] = (ptr, logger) => ReadPrimitiveVector<ushort>(ptr, "uint16", logger),
        ["uint32"] = (ptr, logger) => ReadPrimitiveVector<uint>(ptr, "uint32", logger),
        ["uint64"] = (ptr, logger) => ReadPrimitiveVector<ulong>(ptr, "uint64", logger),

        // Floating point
        ["float32"] = (ptr, logger) => ReadPrimitiveVector<float>(ptr, "float32", logger),
        ["float"] = (ptr, logger) => ReadPrimitiveVector<float>(ptr, "float", logger),

        // Boolean
        ["bool"] = (ptr, logger) => ReadPrimitiveVector<bool>(ptr, "bool", logger),
    };

    public UtlVectorReader(InterfaceBridge bridge, ILogger<UtlVectorReader> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    /// <summary>
    /// Reads UtlVector elements based on the element type.
    /// Returns a list of UtlVectorElement objects that can be rendered.
    /// </summary>
    public List<UtlVectorElement>? ReadUtlVectorElements(CustomTypeInfo customTypeInfo)
    {
        if (customTypeInfo.Kind != CustomTypeKind.UtlVector ||
            string.IsNullOrEmpty(customTypeInfo.UtlVectorElementType))
        {
            return null;
        }

        var elementType = customTypeInfo.UtlVectorElementType;
        var ptr = customTypeInfo.TargetPtr;

        try
        {
            return elementType switch
            {
                // Entity handles
                var et when et.Contains("CEntityHandle<") || et.Contains("CHandle<") 
                    => ReadUtlVectorEntityHandles(ptr),

                // Vector types
                var et when et.Contains("Vector") || et.Contains("QAngle") 
                    => ReadUtlVectorVectors(ptr),

                // Primitive types
                var et when PrimitiveTypeHandlers.TryGetValue(et, out var handler) 
                    => handler(ptr, _logger),

                // Unsupported type
                _ => HandleUnsupportedType(elementType)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading UtlVector elements of type {ElementType}", elementType);
            return null;
        }
    }

    /// <summary>
    /// Reads entity handles from a UtlVector.
    /// </summary>
    private List<UtlVectorElement> ReadUtlVectorEntityHandles(nint ptr)
    {
        unsafe
        {
            var vector = *(CUtlVector<CEntityHandle<IBaseEntity>>*)ptr;
            return ReadVectorElements(vector.Size, i =>
            {
                var handle = vector[i];
                var entity = _bridge.EntityManager.FindEntityByHandle(handle);

                return new UtlVectorElement
                {
                    ElementType = "CEntityHandle<IBaseEntity>",
                    ElementKind = UtlVectorElementKind.EntityHandle,
                    Entity = entity
                };
            });
        }
    }

    /// <summary>
    /// Reads vectors from a UtlVector.
    /// </summary>
    private List<UtlVectorElement> ReadUtlVectorVectors(nint ptr)
    {
        unsafe
        {
            var vector = *(CUtlVector<Vector>*)ptr;
            return ReadVectorElements(vector.Size, i => new UtlVectorElement
            {
                ElementType = "Vector",
                ElementKind = UtlVectorElementKind.Vector,
                StringValue = vector[i].ToString()
            });
        }
    }

    /// <summary>
    /// Generic helper to read primitive values from a UtlVector.
    /// </summary>
    private static List<UtlVectorElement>? ReadPrimitiveVector<T>(nint ptr, string elementType, ILogger<UtlVectorReader> logger) where T : unmanaged
    {
        try
        {
            unsafe
            {
                var vector = *(CUtlVector<T>*)ptr;
                return ReadVectorElements(vector.Size, i => new UtlVectorElement
                {
                    ElementType = elementType,
                    ElementKind = UtlVectorElementKind.Primitive,
                    StringValue = vector[i].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error reading UtlVector of primitive type {ElementType}", elementType);
            return null;
        }
    }

    /// <summary>
    /// Generic helper to create UtlVectorElement list from vector elements.
    /// </summary>
    private static List<UtlVectorElement> ReadVectorElements(int size, Func<int, UtlVectorElement> elementFactory)
    {
        var elements = new List<UtlVectorElement>(size);
        for (int i = 0; i < size; i++)
        {
            elements.Add(elementFactory(i));
        }
        return elements;
    }

    /// <summary>
    /// Handles unsupported element types.
    /// </summary>
    private List<UtlVectorElement>? HandleUnsupportedType(string elementType)
    {
        _logger.LogWarning("Unsupported UtlVector element type: {ElementType}", elementType);
        return null;
    }
}
