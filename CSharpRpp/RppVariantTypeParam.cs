using System;

namespace CSharpRpp
{
    public enum TypeVariant
    {
        Covariant,
        Contravariant
    }

    public class RppVariantTypeParam : RppNamedNode
    {
        public TypeVariant Variant { get; private set; }
        public Type Runtime { get; set; }

        public RppVariantTypeParam(Type nativeType) : base(nativeType.Name)
        {
            Runtime = nativeType;
        }

        public RppVariantTypeParam(string name, TypeVariant variant) : base(name)
        {
            Variant = variant;
        }

        public void Resolve(Symbols.SymbolTable scope)
        {
        }
    }
}