using CSharpRpp.TypeSystem;

namespace CSharpRpp.Symbols
{
    public class Symbol
    {
        public RType Type { get; }
        public string Name { get; }

        public bool IsClass { get; protected set; }
        public bool IsField { get; protected set; }
        public bool IsLocal { get; protected set; }

        public Symbol(string name, RType type)
        {
            Name = name;
            Type = type;
            IsClass = false;
            IsField = false;
            IsLocal = false;
        }
    }

    public class TypeSymbol : Symbol
    {
        public TypeSymbol(RType type) : base(type.Name, type)
        {
            IsClass = true;
        }
    }

    public class FieldSymbol : Symbol
    {
        public FieldSymbol(string name, RType type) : base(name, type)
        {
            IsField = true;
        }
    }

    public class LocalVarSymbol : Symbol
    {
        public IRppNamedNode Var { get; set; }

        public LocalVarSymbol(string name, RType type, IRppNamedNode var) : base(name, type)
        {
            IsLocal = true;
            Var = var;
        }
    }
}