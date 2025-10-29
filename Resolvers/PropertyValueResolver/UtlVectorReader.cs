#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Utilities;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles reading UtlVector elements from entity properties.
/// </summary>
public class UtlVectorReader
{
    private readonly InterfaceBridge _bridge;
    private readonly ILogger<UtlVectorReader> _logger;

    private static readonly string[] PrimitiveTypes = {
        "int32", "int", "float32", "float", "bool",
        "uint8", "uint16", "uint32", "uint64", "int8", "int16", "int64"
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
            // Handle CEntityHandle<IBaseEntity> or similar entity handle types
            if (elementType.Contains("CEntityHandle<") || elementType.Contains("CHandle<"))
            {
                return ReadUtlVectorEntityHandles(ptr);
            }

            // Handle Vector types (Vector, VectorWS, QAngle, etc.)
            if (elementType.Contains("Vector") || elementType.Contains("QAngle"))
            {
                return ReadUtlVectorVectors(ptr);
            }

            // Handle primitive types (int, float, bool, etc.)
            if (IsPrimitiveType(elementType))
            {
                return ReadUtlVectorPrimitives(ptr, elementType);
            }

            _logger.LogWarning("Unsupported UtlVector element type: {ElementType}", elementType);
            return null;
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
            var elements = new List<UtlVectorElement>();

            for (int i = 0; i < vector.Size; i++)
            {
                var handle = vector[i];
                var entity = _bridge.EntityManager.FindEntityByHandle(handle);

                elements.Add(new UtlVectorElement
                {
                    ElementType = "CEntityHandle<IBaseEntity>",
                    ElementKind = UtlVectorElementKind.EntityHandle,
                    Entity = entity
                });
            }

            return elements;
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
            var elements = new List<UtlVectorElement>();

            for (int i = 0; i < vector.Size; i++)
            {
                var vec = vector[i];
                elements.Add(new UtlVectorElement
                {
                    ElementType = "Vector",
                    ElementKind = UtlVectorElementKind.Vector,
                    StringValue = vec.ToString()
                });
            }

            return elements;
        }
    }

    /// <summary>
    /// Reads primitive values from a UtlVector.
    /// </summary>
    private List<UtlVectorElement> ReadUtlVectorPrimitives(nint ptr, string elementType)
    {
        unsafe
        {
            var elements = new List<UtlVectorElement>();

            switch (elementType)
            {
                case "int32":
                case "int":
                    ReadPrimitiveVector<int>(ptr, elementType, elements);
                    break;

                case "float32":
                case "float":
                    ReadPrimitiveVector<float>(ptr, elementType, elements);
                    break;

                case "bool":
                    ReadPrimitiveVector<bool>(ptr, elementType, elements);
                    break;

                default:
                    _logger.LogWarning("Unsupported primitive type for UtlVector: {ElementType}", elementType);
                    return new List<UtlVectorElement>();
            }

            return elements;
        }
    }

    /// <summary>
    /// Generic helper to read primitive values from a UtlVector.
    /// </summary>
    private static void ReadPrimitiveVector<T>(nint ptr, string elementType, List<UtlVectorElement> elements) where T : unmanaged
    {
        unsafe
        {
            var vector = *(CUtlVector<T>*)ptr;
            for (int i = 0; i < vector.Size; i++)
            {
                elements.Add(new UtlVectorElement
                {
                    ElementType = elementType,
                    ElementKind = UtlVectorElementKind.Primitive,
                    StringValue = vector[i].ToString()
                });
            }
        }
    }

    /// <summary>
    /// Checks if the given type is a primitive type.
    /// </summary>
    private static bool IsPrimitiveType(string type)
    {
        return PrimitiveTypes.Contains(type);
    }
}

