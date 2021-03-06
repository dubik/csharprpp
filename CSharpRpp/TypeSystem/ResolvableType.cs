using System;
using System.Linq;
using CSharpRpp.Symbols;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public class ResolvableType
    {
        public static ResolvableType UnitTy = new ResolvableType(RppTypeSystem.UnitTy);
        public static ResolvableType IntTy = new ResolvableType(RppTypeSystem.IntTy);
        public static ResolvableType NullTy = new ResolvableType(RppTypeSystem.NullTy);
        public static ResolvableType AnyTy = new ResolvableType(RppTypeSystem.AnyTy);
        public static ResolvableType BooleanTy = new ResolvableType(RppTypeSystem.BooleanTy);
        public static ResolvableType NothingTy = new ResolvableType(RppTypeSystem.NothingTy);
        public static ResolvableType UndefinedTy = new ResolvableType(RppTypeSystem.Undefined);

        [NotNull]
        public RTypeName Name { get; }

        [NotNull]
        public RType Value
        {
            get
            {
                if (_type == null)
                    throw new Exception("Type wasn't resolved");

                return _type;
            }
        }

        private RType _type;

        public ResolvableType([NotNull] RTypeName name)
        {
            Name = name;
        }

        public ResolvableType([NotNull] RType type)
        {
            _type = type;
            Name = new RTypeName(type.Name); // TODO this doesn't work for generics
        }

        public void Resolve([NotNull] SymbolTable scope)
        {
            if (_type == null)
            {
                _type = Name.Resolve(scope);
            }
        }

        public RType ReResolve([NotNull] SymbolTable scope)
        {
            return ReResolve(Value, scope);
        }

        public static RType ReResolve(RType type, [NotNull] SymbolTable scope)
        {
            RTypeName typeName = Reconstruct(type);
            return typeName.Resolve(scope);
        }

        private static RTypeName Reconstruct(RType type)
        {
            RTypeName typeName = new RTypeName(type.Name);
            type.GenericArguments.ForEach(ga => typeName.AddGenericArgument(Reconstruct(ga)));
            return typeName;
        }

        public override string ToString()
        {
            if (_type != null)
            {
                return $"{_type}";
            }

            return $"'{Name}'";
        }

        protected bool Equals(ResolvableType other)
        {
            if (_type != null && other._type != null)
                return _type.Equals(other._type);

            return Name.Equals(other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((ResolvableType) obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    public static class ResolvableTypeExtension
    {
        public static bool IsDefined(this ResolvableType resolvableType)
        {
            return !IsUndefined(resolvableType);
        }

        public static bool IsUndefined(this ResolvableType resolvableType)
        {
            return Equals(resolvableType, ResolvableType.UndefinedTy);
        }

        public static ResolvableType AsResolvable(this RType type)
        {
            return new ResolvableType(type);
        }
    }
}