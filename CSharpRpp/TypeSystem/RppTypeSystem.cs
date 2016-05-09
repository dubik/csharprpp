using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Symbols;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public sealed class RppTypeSystem
    {
        public static readonly RppTypeSystem Instance = new RppTypeSystem();

        public static RType UnitTy = GetOrCreateType("Unit", typeof(void));
        public static RType CharTy = GetOrCreateType("Char", typeof(char));
        public static RType BooleanTy = GetOrCreateType("Boolean", typeof(bool));
        public static RType ShortTy = GetOrCreateType("Short", typeof(short));
        public static RType IntTy = GetOrCreateType("Int", typeof(int));
        public static RType ByteTy = GetOrCreateType("Byte", typeof(byte));
        public static RType LongTy = GetOrCreateType("Long", typeof(long));
        public static RType FloatTy = GetOrCreateType("Float", typeof(float));
        public static RType DoubleTy = GetOrCreateType("Double", typeof(double));
        public static RType NullTy = ImportClass("Null", typeof(object));
        public static RType AnyTy = ImportClass("Any", typeof(object));
        public static RType StringTy = ImportClass("String", typeof(string));
        public static RType NothingTy = ImportClass("Nothing", typeof(object));
        public static RType Undefined = new RType("Undefined");
        public static RType ArrayTy = CreateArrayType();

        private readonly Dictionary<string, RType> _allTypes = new Dictionary<string, RType>();

        public static RType GetOrCreateType(string name)
        {
            return Instance.GetOrCreate(name, () => new RType(name));
        }

        /// <summary>
        /// Gets already created wrapper around system type or creates and remebers new one.
        /// </summary>
        /// <param name="alias">Alias of a type, for instance 'Unit' is alias for 'void'</param>
        /// <param name="systemType">system type</param>
        /// <returns>RType which represents system type</returns>
        public static RType GetOrCreateType(string alias, Type systemType)
        {
            if (systemType.IsConstructedGenericType)
            {
                return InflateFromSystemType(alias, systemType);
            }

            return GetOrCreateTypeImpl(alias, systemType);
        }

        private static RType GetOrCreateTypeImpl(string alias, Type systemType)
        {
            return Instance.GetOrCreate(alias, () => CreateTypeInstance(alias, systemType));
        }

        private static RType CreateTypeInstance(string alias, Type systemType)
        {
            return new RType(alias, systemType, type => GetOrCreateType(type.Name, type));
        }

        /// <summary>
        /// Creates wrapper around native type by inflating wrapper of generic type definition of specified type.
        /// This will initialize properly generic arguments for the returned type. Let say native type looks like this:
        /// <code>
        /// class Foo&lt;B&gt; : Bar&lt;B&gt;
        /// </code>
        /// B is a generic parameter but for Bar that is a generic argument. We can't inherit generic class we have to
        /// specialized it with generic parameter.
        /// </summary>
        /// <param name="alias">name of the created wrapped type</param>
        /// <param name="systemType">specialized native type</param>
        /// <returns></returns>
        private static RType InflateFromSystemType(string alias, Type systemType)
        {
            Type nativeTypeDefinition = systemType.GetGenericTypeDefinition();
            RType typeDefinition = GetOrCreateTypeImpl(alias, nativeTypeDefinition);
            RType[] genericArguments =
                systemType.GenericTypeArguments.Select(typeArg => new RType(typeArg.Name, typeArg, type => GetOrCreateType(typeArg.Name, type))).ToArray();
            RType resType = typeDefinition.MakeGenericType(genericArguments);
            return resType;
        }

        public static RType GetOrCreateType([NotNull] string name, RTypeAttributes attributes, [CanBeNull] RType parent, [CanBeNull] RType declaringType)
        {
            return Instance.GetOrCreate(name, () => new RType(name, attributes, parent, declaringType));
        }

        private RType GetOrCreate(string name, Func<RType> typeFactory)
        {
            RType type;
            if (_allTypes.TryGetValue(name, out type))
            {
                return type;
            }

            type = typeFactory();
            _allTypes.Add(name, type);
            return type;
        }

        public static void PopulateBuiltinTypes([NotNull] SymbolTable scope)
        {
            Instance._allTypes.Clear();

            scope.AddType(UnitTy);
            scope.AddType(CharTy);
            scope.AddType(BooleanTy);
            scope.AddType(ShortTy);
            scope.AddType(IntTy);
            scope.AddType(LongTy);
            scope.AddType(FloatTy);
            scope.AddType(DoubleTy);
            scope.AddType(ImportClass(typeof(string)));
            scope.AddType(AnyTy);
            scope.AddType(NothingTy);
            scope.AddType(ArrayTy);

            Instance._allTypes.Add(typeof(void).Name, UnitTy);
            Instance._allTypes.Add(typeof(char).Name, CharTy);
            Instance._allTypes.Add(typeof(bool).Name, BooleanTy);
            Instance._allTypes.Add(typeof(short).Name, ShortTy);
            Instance._allTypes.Add(typeof(int).Name, IntTy);
            Instance._allTypes.Add(typeof(byte).Name, ByteTy);
            Instance._allTypes.Add(typeof(long).Name, LongTy);
            Instance._allTypes.Add(typeof(float).Name, FloatTy);
            Instance._allTypes.Add(typeof(object).Name, AnyTy);
        }

        public static RType ImportClass(Type systemType)
        {
            RType type = GetOrCreateType(systemType.Name, systemType);
            return type;
        }

        public static RType ImportClass(string alias, Type systemType)
        {
            return GetOrCreateType(alias, systemType);
        }

        private static RType CreateArrayType()
        {
            RType arrayType = new RType("Array") {IsArray = true};
            RppGenericParameter genericParameter = arrayType.DefineGenericParameters("A")[0];
            arrayType.DefineConstructor(RMethodAttributes.Public, new[] {new RppParameterInfo("size", IntTy)});
            arrayType.DefineMethod("length", RMethodAttributes.Public, IntTy, new RppParameterInfo[0]);
            arrayType.DefineMethod("apply", RMethodAttributes.Public, genericParameter.Type, new[] {new RppParameterInfo("index", IntTy)},
                new RppGenericParameter[0]);
            arrayType.DefineMethod("update", RMethodAttributes.Public, UnitTy,
                new[] {new RppParameterInfo("index", IntTy), new RppParameterInfo("value", genericParameter.Type)}, new RppGenericParameter[0]);
            return arrayType;
        }

        [NotNull]
        public static RType ImportPrimitive([NotNull] Type type)
        {
            if (type == typeof(void))
            {
                return UnitTy;
            }

            if (type == typeof(char))
            {
                return CharTy;
            }

            if (type == typeof(bool))
            {
                return BooleanTy;
            }

            if (type == typeof(short))
            {
                return ShortTy;
            }

            if (type == typeof(int))
            {
                return IntTy;
            }

            if (type == typeof(long))
            {
                return LongTy;
            }

            if (type == typeof(float))
            {
                return FloatTy;
            }

            if (type == typeof(double))
            {
                return DoubleTy;
            }

            if (type == typeof(string))
            {
                return StringTy;
            }

            throw new Exception($"Can't match {type}");
        }
    }
}