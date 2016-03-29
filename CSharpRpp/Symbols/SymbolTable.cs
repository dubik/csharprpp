using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Symbols
{
    public class SymbolTable
    {
        protected readonly SymbolTable Parent;
        private readonly RType _classType;

        private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();

        private readonly SymbolTable _outerSymbolTable;

        public RppClosureContext ClosureContext { get; }

        public bool IsInsideClosure => ClosureContext != null;

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
        }

        private void AddGenericParametersToScope(RType classType)
        {
            if (classType.IsGenericType)
            {
                classType.GenericParameters.ForEach(p => AddType(p.Type));
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

                return methods;
            }

            return Parent != null ? Parent.LookupFunction(name) : Collections.NoRFuncsCollection;
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

                return null;
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