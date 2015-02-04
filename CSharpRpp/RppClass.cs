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

    [DebuggerDisplay("Name = {Name}, Fields = {_fields.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode, IRppClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<RppField> _fields = new List<RppField>();
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<IRppFunc> _funcs = new List<IRppFunc>();
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

        public RppClass(string name, ClassKind kind) : base(name)
        {
            Kind = kind;
        }

        public void AddField(RppField field)
        {
            _fields.Add(field);
        }

        public void AddFunc(IRppFunc func)
        {
            if (Kind == ClassKind.Object)
            {
                func.IsStatic = true;
            }

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