using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public class RppClass : RppNamedNode
    {
        private IList<RppField> _fields = new List<RppField>();
        private IList<RppFunc> _funcs = new List<RppFunc>();
        private RppScope _scope;

        #region Codegen

        private TypeBuilder _typeBuilder;

        #endregion

        public RppClass(string name) : base(name)
        {
        }

        public void AddField(RppField field)
        {
            _fields.Add(field);
        }

        public void AddFunc(RppFunc func)
        {
            _funcs.Add(func);
        }

        public void SetExtends(string name)
        {
        }

        #region Semantic

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);

            _fields.ForEach(_scope.Add);
            _funcs.ForEach(_scope.Add);

            NodeUtils.PreAnalyze(_scope, _fields);
            NodeUtils.PreAnalyze(_scope, _funcs);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _fields = NodeUtils.Analyze(_scope, _fields);
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
            _fields.ForEach(field => field.Codegen(ctx));
            _funcs.ForEach(func => func.Codegen(ctx));

            _typeBuilder.CreateType();
        }

        #endregion

    }
}