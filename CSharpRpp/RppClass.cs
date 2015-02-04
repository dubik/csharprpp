using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

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
        private IList<RppField> _classParams = new List<RppField>();
        private IList<IRppFunc> _funcs = new List<IRppFunc>();
        private RppScope _scope;

        public ClassKind Kind { get; private set; }

        public IEnumerable<IRppFunc> Functions
        {
            get { return _funcs.AsEnumerable(); }
        }

        public Type RuntimeType
        {
            get { return _typeBuilder; }
        }

        #region Codegen

        private TypeBuilder _typeBuilder;

        #endregion

        public RppClass(string name, ClassKind kind, IList<RppField> classParams, IList<IRppNode> classBody) : base(name)
        {
            Kind = kind;

            if (classParams != null && classParams.Count > 0)
            {
                _classParams = classParams;
            }

            if (classBody != null && classBody.Count > 0)
            {
                _funcs = classBody.OfType<IRppFunc>().ToList();
                _funcs.ForEach(func => func.IsStatic = kind == ClassKind.Object);
            }
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
            _classParams.ForEach(field => field.Codegen(ctx));
            _funcs.ForEach(func => func.Codegen(ctx));

            _typeBuilder.CreateType();
        }

        #endregion
    }
}