using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public enum ClassKind
    {
        Class,
        Object
    }

    public interface IRppClass : IRppNamedNode
    {
        IEnumerable<IRppFunc> Functions { get; }
    }

    [DebuggerDisplay("{Kind} {Name}, Fields = {_classParams.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode, IRppClass
    {
        private IList<RppVar> _classParams;
        private IList<IRppFunc> _funcs;
        private RppScope _scope;

        public ClassKind Kind { get; private set; }

        [NotNull]
        public IEnumerable<IRppFunc> Functions
        {
            get { return _funcs.AsEnumerable(); }
        }

        [NotNull]
        public IEnumerable<IRppFunc> Constructors
        {
            get { return Collections.NoFuncs; }
        }

        [NotNull]
        public Type RuntimeType
        {
            get { return _typeBuilder; }
        }

        #region Codegen

        private TypeBuilder _typeBuilder;

        #endregion

        public RppClass(ClassKind kind, [NotNull] string name) : base(name)
        {
            Kind = kind;
            _classParams = Collections.NoFields;
            _funcs = Collections.NoFuncs;
        }

        public RppClass(ClassKind kind, [NotNull] string name, [NotNull] IList<RppVar> classParams, [NotNull] IEnumerable<IRppNode> classBody) : base(name)
        {
            Kind = kind;

            _classParams = classParams;

            _funcs = classBody.OfType<IRppFunc>().ToList();
            _funcs.ForEach(func => func.IsStatic = kind == ClassKind.Object);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            _funcs.ForEach(func => func.Accept(visitor));
            visitor.VisitExit(this);
        }

        #region Semantic

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);

            _classParams.ForEach(_scope.Add);
            _funcs.ForEach(_scope.Add);

            NodeUtils.PreAnalyze(_scope, _classParams);
            NodeUtils.PreAnalyze(_scope, _funcs);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classParams = NodeUtils.Analyze(_scope, _classParams);
            _funcs = NodeUtils.Analyze(_scope, _funcs);
            return this;
        }

        #endregion

        #region Codegen

        public void CodegenType(RppScope scope, ModuleBuilder moduleBuilder)
        {
            _typeBuilder = moduleBuilder.DefineType(Name);
        }

        public void CodegenMethodStubs(RppScope scope)
        {
            Debug.Assert(_typeBuilder != null);

            _funcs.ForEach(func => func.CodegenMethodStubs(_typeBuilder));
        }

        public void Codegen(CodegenContext ctx)
        {
            // TODO define fields in here
            // _classParams.ForEach(field => field.Codegen(ctx));
            _funcs.ForEach(func => func.Codegen(ctx));
            _typeBuilder.CreateType();
        }

        #endregion

        #region Equality

        protected bool Equals(RppClass other)
        {
            return Kind == other.Kind && _funcs.SequenceEqual(other._funcs) && _classParams.SequenceEqual(other._classParams) && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RppClass) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Kind;
                return hashCode;
            }
        }

        #endregion
    }
}