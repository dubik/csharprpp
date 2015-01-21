﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    public enum ERppPrimitiveType
    {
        EBool,
        EChar,
        EShort,
        EByte,
        EInt,
        ELong,
        EFloat,
        EDouble,
        EUnit
    }

    public abstract class RppType
    {
        public abstract Type Resolve(RppScope scope);
    }

    public class RppPrimitiveType : RppType
    {
        public readonly ERppPrimitiveType PrimitiveType;

        private static readonly Dictionary<string, RppPrimitiveType> PrimitiveTypesMap = new Dictionary<string, RppPrimitiveType>
        {
            {"Bool", new RppPrimitiveType(ERppPrimitiveType.EBool)},
            {"Char", new RppPrimitiveType(ERppPrimitiveType.EChar)},
            {"Short", new RppPrimitiveType(ERppPrimitiveType.EShort)},
            {"Int", new RppPrimitiveType(ERppPrimitiveType.EInt)},
            {"Long", new RppPrimitiveType(ERppPrimitiveType.ELong)},
            {"Float", new RppPrimitiveType(ERppPrimitiveType.EFloat)},
            {"Double", new RppPrimitiveType(ERppPrimitiveType.EDouble)},
            {"Unit", new RppPrimitiveType(ERppPrimitiveType.EUnit)}
        };

        private static readonly Dictionary<ERppPrimitiveType, Type> SystemTypesMap = new Dictionary<ERppPrimitiveType, Type>
        {
            {ERppPrimitiveType.EBool, typeof (bool)},
            {ERppPrimitiveType.EChar, typeof (char)},
            {ERppPrimitiveType.EShort, typeof (short)},
            {ERppPrimitiveType.EInt, typeof (int)},
            {ERppPrimitiveType.ELong, typeof (long)},
            {ERppPrimitiveType.EFloat, typeof (float)},
            {ERppPrimitiveType.EDouble, typeof (double)},
            {ERppPrimitiveType.EUnit, typeof (void)}
        };

        public RppPrimitiveType(ERppPrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
        }

        public static bool IsPrimitive(string name, out RppPrimitiveType type)
        {
            return PrimitiveTypesMap.TryGetValue(name, out type);
        }

        public override Type Resolve(RppScope scope)
        {
            return SystemTypesMap[PrimitiveType];
        }
    }

    public class RppObjectType : RppType
    {
        public override Type Resolve(RppScope scope)
        {
            throw new NotImplementedException();
        }
    }

    public class RppGenericType : RppType
    {
        private readonly IList<RppType> _params = new List<RppType>();

        private readonly RppTypeName _typeName;

        public RppGenericType(string typeName)
        {
            _typeName = new RppTypeName(typeName);
        }

        public void AddParam(RppType param)
        {
            _params.Add(param);
        }

        public override Type Resolve(RppScope scope)
        {
            var paramsType = _params.Select(par => par.Resolve(scope)).ToList();
            RppNamedNode genericType = scope.Lookup(_typeName.Name);
            return null;
        }
    }

    [DebuggerDisplay("{Name}")]
    public class RppTypeName : RppType
    {
        public readonly string Name;

        public RppTypeName(string name)
        {
            Name = name;
        }

        public override Type Resolve(RppScope scope)
        {
            RppPrimitiveType primitiveType;
            if (RppPrimitiveType.IsPrimitive(Name, out primitiveType))
            {
                return primitiveType.Resolve(scope);
            }
            else
            {
                scope.Lookup(Name);
                Debug.Assert(false, "Not implemented yet");
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}