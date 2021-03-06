﻿using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Symbols
{
    public class SymbolTable
    {
        public RppClosureContext ClosureContext { get; }

        public bool IsInsideClosure => ClosureContext != null;

        /// <summary>
        /// All available generics for a given scope, class and function ones combined
        /// </summary>
        public IEnumerable<RppGenericParameter> AvailableGenericArguments => Parent?.AvailableGenericArguments.Concat(GenericArguments) ?? GenericArguments;

        public IEnumerable<RppGenericParameter> GenericArguments => _genericArguments ?? Enumerable.Empty<RppGenericParameter>();

        private RppGenericParameter[] _genericArguments;

        private readonly SymbolTable Parent;

        private readonly RType _classType;

        private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();

        private readonly SymbolTable _outerSymbolTable;

        public SymbolTable()
        {
        }

        public SymbolTable(SymbolTable parent)
        {
            Parent = parent;
        }

        public SymbolTable([CanBeNull] SymbolTable parent, [NotNull] RppClosureContext closureContext)
        {
            Parent = parent;
            ClosureContext = closureContext;
        }

        public SymbolTable(SymbolTable parent, RType classType, SymbolTable outerScope)
        {
            Parent = parent;
            _classType = classType;
            _outerSymbolTable = outerScope;

            AddGenericParametersToScope(_classType);
            AddNestedToScope(_classType);
        }

        private void AddNestedToScope(RType classType)
        {
            classType.Nested?.ForEach(AddType);
        }

        public SymbolTable([CanBeNull] SymbolTable parent, [NotNull] RppMethodInfo methodInfo)
        {
            Parent = parent;
            AddGenericParametersToScope(methodInfo);
        }

        private void AddGenericParametersToScope([NotNull] RppMethodInfo methodInfo)
        {
            methodInfo.GenericParameters?.ForEach(p => AddType(p.Type));
            _genericArguments = methodInfo.GenericParameters?.ToArray();
        }

        private void AddGenericParametersToScope(RType classType)
        {
            if (classType.IsGenericType)
            {
                classType.GenericParameters.ForEach(p => AddType(p.Type));
                _genericArguments = classType.GenericParameters.ToArray();
            }
        }

        public Symbol Lookup(string name)
        {
            Symbol symbol;
            if (_symbols.TryGetValue(name, out symbol))
            {
                return symbol;
            }

            return Parent?.Lookup(name);
        }

        [CanBeNull]
        public TypeSymbol LookupType([NotNull] string name)
        {
            Symbol symbol = Lookup(name);
            return symbol as TypeSymbol;
        }

        public TypeSymbol LookupObject(string name)
        {
            return LookupType(GetObjectName(name));
        }

        public void AddType(RType type)
        {
            Add(type.Name, new TypeSymbol(type));
        }

        // TODO associating ast node with symbol is quite weird, we need to map symbols to builders but may be using some other means
        public void AddLocalVar(string name, RType type, IRppNamedNode obj)
        {
            Add(name, new LocalVarSymbol(name, type, obj));
        }

        protected void Add(string name, Symbol symbol)
        {
            if (_symbols.ContainsKey(name))
            {
                throw new ArgumentException($"Already containes {name}");
            }

            _symbols.Add(symbol.Name, symbol);
        }

        [NotNull]
        public IReadOnlyCollection<RppMethodInfo> LookupFunction(string name)
        {
            if (_classType != null)
            {
                if (name == "this")
                {
                    return _classType.Constructors;
                }

                var methods = _classType.Methods.Where(m => m.Name == name).ToList();
                RType baseClass = _classType.BaseType;
                while (baseClass != null)
                {
                    var baseMethods = baseClass.Methods.Where(m => m.Name == name).ToList();
                    methods.AddRange(baseMethods);
                    baseClass = baseClass.BaseType;
                }

                if (methods.NonEmpty())
                {
                    return methods;
                }
            }

            return Parent?.LookupFunction(name) ?? Collections.NoRFuncsCollection;
        }

        public RppFieldInfo LookupField(string name)
        {
            if (_classType != null)
            {
                var field = _classType.Fields.FirstOrDefault(f => f.MangledName == name || f.MangledName == RppFieldInfo.GetMangledName(name));
                if (field != null)
                {
                    return field;
                }

                RType baseClass = _classType.BaseType;
                while (baseClass != null)
                {
                    field = _classType.Fields.FirstOrDefault(f => f.MangledName == name || f.MangledName == RppFieldInfo.GetMangledName(name));
                    if (field != null)
                    {
                        return field;
                    }
                    baseClass = baseClass.BaseType;
                }

            }

            return Parent?.LookupField(name);
        }

        public static string GetObjectName(string name)
        {
            return name + "$";
        }

        [CanBeNull]
        public RType GetEnclosingType()
        {
            if (_classType != null)
            {
                return _classType;
            }

            return Parent?.GetEnclosingType();
        }

        public SymbolTable GetOuterSymbolTable()
        {
            return _outerSymbolTable ?? this;
        }
    }
}