using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppExpr : IRppNode
    {
        RppType Type { get; }
        Type RuntimeType { get; }
        void Codegen(ILGenerator generator);
    }

    public class RppEmptyExpr : RppNode, IRppExpr
    {
        public RppType Type { get { return RppPrimitiveType.RppUnit; } }
        public Type RuntimeType { get { return typeof(void); } }

        public void Codegen(ILGenerator generator)
        {
        }
    }

    [DebuggerDisplay("Op = {_op}")]
    public class BinOp : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        private readonly string _op;
        private IRppExpr _left;
        private IRppExpr _right;

        public BinOp(string op, IRppExpr left, IRppExpr right)
        {
            _op = op;
            _left = left;
            _right = right;
        }

        public override void PreAnalyze(RppScope scope)
        {
            _left.PreAnalyze(scope);
            _right.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _left = _left.Analyze(scope) as IRppExpr;
            Debug.Assert(_left != null);
            _right = _right.Analyze(scope) as IRppExpr;
            Debug.Assert(_right != null);

            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            _left.Codegen(generator);
            _right.Codegen(generator);
            switch (_op)
            {
                case "+":
                    generator.Emit(OpCodes.Add);
                    break;
                default:
                    Debug.Assert(false, "Don't know how to handle " + _op);
                    break;
            }
        }
    }

    [DebuggerDisplay("Int: {_value}")]
    public class RppInteger : RppNode, IRppExpr
    {
        private readonly int _value;

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppInteger(string valueStr)
        {
            _value = int.Parse(valueStr);
            RuntimeType = typeof (int);
            Type = new RppNativeType(RuntimeType);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, _value);
        }
    }

    [DebuggerDisplay("String: {_value}")]
    public class RppString : RppNode, IRppExpr
    {
        private readonly string _value;

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppString(string valueStr)
        {
            _value = stripQuotes(valueStr);
            RuntimeType = typeof (string);
            Type = new RppNativeType(RuntimeType);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldstr, _value);
        }

        private string stripQuotes(string str)
        {
            if (str.Length > 1 && str[0] == '"' && str[str.Length - 1] == '"')
            {
                return str.Substring(1, str.Length - 2);
            }

            return str;
        }
    }

    [DebuggerDisplay("FuncCall - Name: {_funcName}, Params: {_paramList.Count}")]
    public class RppFuncCall : RppNode, IRppExpr
    {
        public RppType Type
        {
            get { return _func.ReturnType; }
        }

        public Type RuntimeType
        {
            get { return _func.RuntimeReturnType; }
        }

        private readonly string _funcName;
        private readonly IList<IRppExpr> _paramList;
        private IRppFunc _func;

        public RppFuncCall(string funcName, IList<IRppExpr> paramList)
        {
            _funcName = funcName;
            _paramList = paramList;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _func = scope.Lookup(_funcName) as IRppFunc;
            Debug.Assert(_func != null);
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            _paramList.ForEach(p => p.Codegen(generator));
            generator.Emit(OpCodes.Call, _func.RuntimeFuncInfo);
        }
    }

    public class RppBlockExpr : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        private IList<IRppExpr> _exprs;

        public RppBlockExpr(IList<IRppExpr> exprs)
        {
            _exprs = exprs;
        }

        public override void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _exprs);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _exprs = NodeUtils.Analyze(scope, _exprs);

            InitializeType();

            return this;
        }

        private void InitializeType()
        {
            if (_exprs.Any())
            {
                var lastExpr = _exprs.Last();
                Type = lastExpr.Type;
                RuntimeType = lastExpr.RuntimeType;
            }
            else
            {
                RuntimeType = typeof (void);
                Type = new RppNativeType(RuntimeType);
            }
        }

        public void Codegen(ILGenerator generator)
        {
            _exprs.ForEach(e => e.Codegen(generator));
        }
    }
}