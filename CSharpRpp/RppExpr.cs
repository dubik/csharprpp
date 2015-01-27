using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public abstract class RppExpr : RppNode
    {
        public abstract void Codegen(ILGenerator generator);
    }

    [DebuggerDisplay("Op = {_op}")]
    public class BinOp : RppExpr
    {
        private readonly string _op;
        private RppExpr _left;
        private RppExpr _right;

        public BinOp(string op, RppExpr left, RppExpr right)
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
            _left = _left.Analyze(scope) as RppExpr;
            Debug.Assert(_left != null);
            _right = _right.Analyze(scope) as RppExpr;
            Debug.Assert(_right != null);

            return this;
        }

        public override void Codegen(ILGenerator generator)
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
    public class RppInteger : RppExpr
    {
        private readonly int _value;

        public RppInteger(string valueStr)
        {
            _value = int.Parse(valueStr);
        }

        public override void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, _value);
        }
    }

    [DebuggerDisplay("String: {_value}")]
    public class RppString : RppExpr
    {
        private readonly string _value;

        public RppString(string valueStr)
        {
            _value = valueStr;
        }

        public override void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldstr, _value);
        }
    }

    [DebuggerDisplay("FuncCall - Name: {_funcName}, Params: {_paramList.Count}")]
    public class RppFuncCall : RppExpr
    {
        private readonly string _funcName;
        private readonly IList<RppExpr> _paramList;
        private IRppFunc _func;

        public RppFuncCall(string funcName, IList<RppExpr> paramList)
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

        public override void Codegen(ILGenerator generator)
        {
            _paramList.ForEach(p => p.Codegen(generator));
            generator.Emit(OpCodes.Call, _func.RuntimeFuncInfo);
        }
    }
}