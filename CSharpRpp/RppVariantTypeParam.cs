using System;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public enum TypeVariant
    {
        Invariant,
        Covariant,
        Contravariant
    }

    public class RppVariantTypeParam : RppNamedNode
    {
        public TypeVariant Variant { get; private set; }
        public Type Runtime { get; set; }

        [CanBeNull] private readonly RTypeName _constraint;

        public RType ConstraintType { get; private set; }

        public RppVariantTypeParam(Type nativeType) : base(nativeType.Name)
        {
            Runtime = nativeType;
            Variant = TypeVariant.Invariant;
        }

        public RppVariantTypeParam([NotNull] string name, TypeVariant variant, [CanBeNull] RTypeName constraintTypeName) : base(name)
        {
            Variant = variant;
            _constraint = constraintTypeName;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            ConstraintType = _constraint?.Resolve(scope);
            return this;
        }
    }
}