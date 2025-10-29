#nullable enable
using Sharp.Shared.GameEntities;

namespace ServerGui.Resolvers.PropertyValueResolver;

/// <summary>
/// Information about a custom type that requires special handling.
/// </summary>
public class CustomTypeInfo
{
    public CustomTypeKind Kind { get; init; }
    public nint TargetPtr { get; init; }
    public string SchemaClassname { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public IBaseEntity? ReferencedEntity { get; init; }
    public int? ArraySize { get; init; }
    public string ArrayType { get; init; } = string.Empty;
    public string? UtlVectorElementType { get; init; }
}

/// <summary>
/// Represents a single element from a UtlVector with its type information.
/// </summary>
public class UtlVectorElement
{
    public string ElementType { get; init; } = string.Empty;
    public UtlVectorElementKind ElementKind { get; init; }
    public IBaseEntity? Entity { get; init; }
    public string? StringValue { get; init; }
    public nint? PointerValue { get; init; }
}

/// <summary>
/// The kind of UtlVector element.
/// </summary>
public enum UtlVectorElementKind
{
    EntityHandle,
    Vector,
    Primitive,
    Pointer,
    Embedded
}

/// <summary>
/// The kind of custom type being handled.
/// </summary>
public enum CustomTypeKind
{
    /// <summary>CHandle&lt;T&gt; - Entity handle reference</summary>
    EntityHandle,
    /// <summary>CEntityIndex - Entity index</summary>
    EntityIndex,
    /// <summary>Pointer type (*)</summary>
    Pointer,
    /// <summary>UtlVector type (CNetworkUtlVectorBase&lt;T&gt;)</summary>
    UtlVector,
    /// <summary>Array type ([N])</summary>
    Array,
    /// <summary>Embedded/Value custom type</summary>
    Embedded,
    /// <summary>Null or invalid pointer</summary>
    Null
}

