#nullable enable
using System;
using Microsoft.Extensions.Logging;
using ServerGui;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Handles resolution of custom types (handles, pointers, arrays, UtlVectors, embedded types).
/// </summary>
public class CustomTypeResolver
{
    private readonly ILogger<CustomTypeResolver> _logger;

    public CustomTypeResolver(ILogger<CustomTypeResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves information about a custom type property.
    /// </summary>
    public CustomTypeInfo? ResolveCustomType(nint entityPtr, string classname, string fieldName, string fieldType)
    {
        try
        {
            var schemaClassname = fieldType.Replace("*", "");
            var netvarOffset = SchemaSystem.GetNetVarOffset(classname, fieldName);

            if (fieldType.Contains("CNetworkUtlVectorBase<") || fieldType.Contains("CUtlVector<"))
            {
                return ResolveUtlVectorType(entityPtr, netvarOffset, schemaClassname, fieldType);
            }

            if (fieldType.Contains("CHandle<"))
            {
                return ResolveEntityHandleType(entityPtr, netvarOffset, schemaClassname, fieldType);
            }

            // Handle pointer types
            if (fieldType.Contains("*"))
            {
                return ResolvePointerType(entityPtr, classname, fieldName, schemaClassname, fieldType);
            }

            // Handle array types
            if (fieldType.Contains("[") && fieldType.Contains("]"))
            {
                return ResolveArrayType(entityPtr, netvarOffset, schemaClassname, fieldType);
            }

            // Handle embedded/value custom types
            return ResolveEmbeddedType(entityPtr, netvarOffset, schemaClassname, fieldType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving custom type {FieldName} of type {FieldType}", fieldName, fieldType);
            return null;
        }
    }

    private CustomTypeInfo ResolveUtlVectorType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var vectorPtr = entityPtr + netvarOffset;
        var elementType = ExtractUtlVectorElementType(fieldType);
        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.UtlVector,
            TargetPtr = vectorPtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType,
            UtlVectorElementType = elementType
        };
    }

    /// <summary>
    /// Extracts the element type from a UtlVector field type string.
    /// Example: "CUtlVector<CEntityHandle<IBaseEntity>>" -> "CEntityHandle<IBaseEntity>"
    /// </summary>
    private string? ExtractUtlVectorElementType(string fieldType)
    {
        var startIndex = fieldType.IndexOf('<');
        if (startIndex == -1) return null;

        var endIndex = fieldType.LastIndexOf('>');
        if (endIndex == -1 || endIndex <= startIndex) return null;

        return fieldType.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

    /// <summary>
    /// Resolves an entity handle (CHandle&lt;T&gt;) type.
    /// </summary>
    private CustomTypeInfo ResolveEntityHandleType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var handlePtr = entityPtr + netvarOffset;
        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.EntityHandle,
            TargetPtr = handlePtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }

    /// <summary>
    /// Resolves a pointer type.
    /// </summary>
    private CustomTypeInfo ResolvePointerType(nint entityPtr, string classname, string fieldName, string schemaClassname, string fieldType)
    {
        var targetPtr = SchemaSystem.GetNetVarPointer(entityPtr, classname, fieldName);
        if (targetPtr == nint.Zero)
        {
            return new CustomTypeInfo
            {
                Kind = CustomTypeKind.Null,
                TargetPtr = nint.Zero,
                SchemaClassname = schemaClassname,
                FieldType = fieldType
            };
        }

        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Pointer,
            TargetPtr = targetPtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }

    /// <summary>
    /// Resolves an array type.
    /// </summary>
    private CustomTypeInfo ResolveArrayType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var arraySizeStr = fieldType.Split('[')[1].Split(']')[0];
        var arrayType = fieldType.Split('[')[0];

        int? arraySize = null;
        if (int.TryParse(arraySizeStr, out var parsedSize))
        {
            arraySize = parsedSize;
        }

        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Array,
            TargetPtr = entityPtr + netvarOffset,
            SchemaClassname = schemaClassname,
            FieldType = fieldType,
            ArraySize = arraySize,
            ArrayType = arrayType
        };
    }

    /// <summary>
    /// Resolves an embedded/value custom type.
    /// </summary>
    private CustomTypeInfo ResolveEmbeddedType(nint entityPtr, int netvarOffset, string schemaClassname, string fieldType)
    {
        var embeddedPtr = entityPtr + netvarOffset;
        return new CustomTypeInfo
        {
            Kind = CustomTypeKind.Embedded,
            TargetPtr = embeddedPtr,
            SchemaClassname = schemaClassname,
            FieldType = fieldType
        };
    }
}

