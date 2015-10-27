using CSharpRpp.TypeSystem;

namespace CSharpRpp.Symbols
{
    public class Symbol
    {
        public RType Type { get; }
        public string Name { get; }

        public bool IsClass { get; protected set; }
        public bool IsField { get; protected set; }
        public bool IsLocalVar { get; protected set; }

        public Symbol(string name, RType type)
        {
            Name = name;
            Type = type;
            IsClass = false;
            IsField = false;
            IsLocalVar = false;
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
        public LocalVarSymbol(string name, RType type) : base(name, type)
        {
            IsLocalVar = true;
        }
    }
}