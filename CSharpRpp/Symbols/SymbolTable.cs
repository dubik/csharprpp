using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Symbols
{
    public class SymbolTable
    {
        protected readonly SymbolTable Parent;
        private readonly RType _classType;
        private readonly SymbolTable _baseClassSymbolTable;

        private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();

        public SymbolTable()
        {
        }

        public SymbolTable(SymbolTable parent)
        {
            Parent = parent;
        }

        public SymbolTable(SymbolTable parent, RType classType, SymbolTable baseClassSymbolTable)
        {
            Parent = parent;
            _classType = classType;
            _baseClassSymbolTable = baseClassSymbolTable;
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

        public TypeSymbol LookupType(string name)
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

        public void AddLocalVar(string name, RType type)
        {
            Add(name, new LocalVarSymbol(name, type));
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
                var baseMethods = _baseClassSymbolTable?.LookupFunction(name) ?? Collections.NoRFuncsCollection;
                methods.AddRange(baseMethods);
                return methods;
            }

            return Parent != null ? Parent.LookupFunction(name) : Collections.NoRFuncsCollection;
        }

        public IReadOnlyCollection<RppFieldInfo> LookupField(string name)
        {
            throw new NotImplementedException("Not yet");
        }

        public static string GetObjectName(string name)
        {
            return name + "$";
        }
    }
}