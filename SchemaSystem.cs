/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.CompilerServices;
using Sharp.Shared;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Utilities;

// ReSharper disable InconsistentNaming

namespace ServerGui;

public static class SchemaSystem
{
    public static SchemaField GetSchemaField(string classname, string field)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        var arraySize     = 1;
        var fieldTypeSpan = schemaField.Type.AsSpan();

        var startPos = fieldTypeSpan.IndexOf('[');
        var endPos   = fieldTypeSpan.IndexOf(']');

        if (startPos > -1 && endPos > -1 && endPos > startPos)
        {
            var arraySizeStr = fieldTypeSpan[(startPos + 1)..endPos];
            arraySize = int.Parse(arraySizeStr);
        }

        return new SchemaField
        {
            Networked   = schemaField.Networked,
            ChainOffset = schemaClass.ChainOffset,
            Offset      = schemaField.Offset,
            ArraySize   = arraySize,
        };
    }

    private static SchemaClass GetSchemaClass(string classname)
    {
        if (!SharedGameObject.SchemaInfo.TryGetValue(classname, out var schemaClass))
        {
            throw new ArgumentException($"Invalid class {classname}");
        }

        return schemaClass;
    }

    private static SchemaClassField GetSchemaClassField(SchemaClass schemaClass, string field)
    {
        if (!schemaClass.Fields.TryGetValue(field, out var schemaField))
        {
            throw new ArgumentException($"Invalid NetVar {field} for class {schemaClass.ClassName}");
        }

        return schemaField;
    }

    public static int GetNetVarOffset(string classname, string field)
        => GetSchemaClassField(GetSchemaClass(classname), field).Offset;

    public static int GetNetVarOffset(string classname, string field, ushort extraOffset)
        => (ushort) (GetSchemaClassField(GetSchemaClass(classname), field).Offset + extraOffset);

    public static bool GetNetVarBool(nint ptr, string classname, string field, ushort extraOffset = 0)
        => GetNetVarByte(ptr, classname, field, extraOffset) != 0;

    public static byte GetNetVarByte(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetByte(GetNetVarOffset(classname, field) + extraOffset);

    public static short GetNetVarInt16(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt16(GetNetVarOffset(classname, field) + extraOffset);

    public static ushort GetNetVarUInt16(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt16(GetNetVarOffset(classname, field) + extraOffset);

    public static int GetNetVarInt32(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt32(GetNetVarOffset(classname, field) + extraOffset);

    public static uint GetNetVarUInt32(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt32(GetNetVarOffset(classname, field) + extraOffset);

    public static long GetNetVarInt64(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt64(GetNetVarOffset(classname, field) + extraOffset);

    public static ulong GetNetVarUInt64(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt64(GetNetVarOffset(classname, field) + extraOffset);

    public static float GetNetVarFloat(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetFloat(GetNetVarOffset(classname, field) + extraOffset);

    public static nint GetNetVarPointer(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetObjectPtr(GetNetVarOffset(classname, field) + extraOffset);

    public static string GetNetVarString(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.ReadStringUtf8(GetNetVarOffset(classname, field) + extraOffset);

    public static unsafe Vector GetNetVarVector(nint ptr, string classname, string field, ushort extraOffset = 0)
        => *(Vector*) ptr.Add(GetNetVarOffset(classname, field) + extraOffset);

    public static unsafe string GetNetVarUtlSymbolLarge(nint ptr,
        string                                               classname,
        string                                               field,
        ushort                                               extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlSymbolLarge*) (ptr + offset);

        return pointer->Get();
    }

    public static unsafe ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(nint ptr,
        string                                                               classname,
        string                                                               field,
        ushort                                                               extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlSymbolLarge*) (ptr + offset);

        return ref Unsafe.AsRef<CUtlSymbolLarge>(pointer);
    }

    public static unsafe string GetNetVarUtlString(nint ptr, string classname, string field, ushort extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlString*) (ptr + offset);

        return pointer->Get();
    }

    public static unsafe ref CUtlString GetNetVarUtlStringRef(nint ptr, string classname, string field, ushort extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlString*) (ptr + offset);

        return ref Unsafe.AsRef<CUtlString>(pointer);
    }
}