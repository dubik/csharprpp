using System.Collections.Generic;

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
        EDouble
    }

    public class RppType
    {
        virtual public RppType Resolve(RppScope scope)
        {
            return this;
        }
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
            {"Double", new RppPrimitiveType(ERppPrimitiveType.EDouble)}
        };

        public RppPrimitiveType(ERppPrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
        }

        public static bool IsPrimitive(string name, out RppPrimitiveType type)
        {
            return PrimitiveTypesMap.TryGetValue(name, out type);
        }
    }

    public class RppObjectType : RppType
    {
    }

    public class RppTypeName : RppType
    {
        public readonly string Name;

        public RppTypeName(string name)
        {
            Name = name;
        }

        public override RppType Resolve(RppScope scope)
        {
            RppPrimitiveType primitiveType;
            if (RppPrimitiveType.IsPrimitive(Name, out primitiveType))
            {
                return primitiveType;
            }
            else
            {
                scope.Lookup(Name);
            }

            return null;
        }
    }

}
