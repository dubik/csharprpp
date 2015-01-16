using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppClass : RppNamedNode
    {
        private IList<RppField> _fields = new List<RppField>();
        private IList<RppFunc> _funcs = new List<RppFunc>();
        private RppScope _scope;

        public RppClass(string name) : base(name)
        {
        }

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

        public override void Codegen(CodegenContext ctx)
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
    }
}