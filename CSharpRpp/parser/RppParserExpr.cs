using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public partial class RppParser
    {
        public IRppExpr ParseExpr()
        {
            return ParseExpr1();
        }

        private IRppExpr ParseExpr1()
        {
            if (Require(RppLexer.KW_If))
            {
                throw new Exception("If not supported");
            }

            if (Require(RppLexer.KW_While))
            {
                throw new Exception("While not supported");
            }

            if (Require(RppLexer.KW_Return))
            {
                throw new Exception("Return not supported");
            }

            return ParsePostfixExpr(0);
        }

        private bool ParseOperator(out string op, out int precedence, out bool leftAssoc)
        {
            leftAssoc = false;
            if (Require(RppLexer.OP_Ops))
            {
                var token = _lastToken;
                op = token.Text;
                switch (token.Text)
                {
                    case "|":
                        precedence = 2;
                        break;
                    case "^":
                        precedence = 3;
                        break;
                    case "&":
                        precedence = 4;
                        break;
                    case "=":
                    case "!":
                        precedence = 5;
                        break;
                    case "<":
                    case ">":
                        precedence = 6;
                        break;
                    case ":":
                        precedence = 7;
                        break;
                    case "+":
                    case "-":
                        precedence = 8;
                        break;
                    case "*":
                    case "/":
                    case "%":
                        precedence = 9;
                        break;
                    default:
                        precedence = 0;
                        return false;
                }

                return true;
            }

            op = null;
            precedence = 0;
            return false;
        }

        private IRppExpr ParsePostfixExpr(int minPrecedence)
        {
            IRppExpr expr = ParsePrefixExpr();
            while (expr != null)
            {
                int precedence;
                bool leftAssoc;
                string op;

                _stream.Mark();
                if (ParseOperator(out op, out precedence, out leftAssoc) && precedence >= minPrecedence)
                {
                    _stream.Release(1);
                    int nextMinPrecedence = precedence + 1;
                    IRppExpr rhs = ParsePostfixExpr(nextMinPrecedence);
                    expr = new BinOp(op, expr, rhs);
                }
                else
                {
                    _stream.Rewind();
                    break;
                }
            }

            return expr;
        }

        private IRppExpr ParsePrefixExpr()
        {
            _stream.Mark();
            if (Require(RppLexer.OP_LParen))
            {
                IRppExpr expr = ParsePostfixExpr(0);
                if (expr != null && Require(RppLexer.OP_RParen))
                {
                    _stream.Release(1);
                    return expr;
                }
            }

            _stream.Rewind();
            return ParseSimpleExpr();
        }

        private IRppExpr ParseSimpleExpr()
        {
            if (Require(RppLexer.KW_New))
            {
                throw new Exception("New is not implemented yet");
            }
            else if (_stream.LA(1) == RppLexer.OP_LBrace)
            {
                return ParseBlockExpr();
            }
            else
            {
                return ParseSimpleExpr1();
            }
        }

        private IRppExpr ParseBlockExpr()
        {
            throw new Exception("Block is not implemented yet");
        }

        private IRppExpr ParseSimpleExpr1()
        {
            IRppExpr expr;
            if (Require(RppLexer.IntegerLiteral))
            {
                expr = new RppInteger(_lastToken.Text);
            }
            else if (Require(RppLexer.StringLiteral))
            {
                expr = new RppString(_lastToken.Text);
            }
            else if (Require(RppLexer.KW_Null))
            {
                throw new Exception("Null is not implemented yet");
            }
            else
            {
                ParsePath(out expr);
            }

            return ParseSimpleExprRest(expr);
        }

        public bool ParsePath(out IRppExpr path)
        {
            path = null;

            if (Require(RppLexer.Id))
            {
                path = new RppId(_lastToken.Text);
                while (Require(RppLexer.OP_Dot))
                {
                    Expect(RppLexer.Id);
                    path = new RppSelector(path, new RppId(_lastToken.Text));
                }

                return true;
            }

            return false;
        }

        // clazz.myField
        // class.Func()
        private IRppExpr ParseSimpleExprRest(IRppExpr expr)
        {
            if (Require(RppLexer.OP_Dot))
            {
                if (Require(RppLexer.Id))
                {
                    return ParseSimpleExprRest(new RppSelector(expr, new RppId(_lastToken.Text)));
                }

                throw new Exception("After . identifier is expected " + _lastToken);
            }

            if (Peek(RppLexer.OP_LParen))
            {
                IList<IRppExpr> args = ParseArgs();
                return ParseSimpleExprRest(MakeCall(expr, args));
            }

            return expr;
        }

        private static IRppExpr MakeCall(IRppExpr expr, IList<IRppExpr> args)
        {
            if (expr is RppId)
            {
                RppId id = expr as RppId;
                return new RppFuncCall(id.Name, args);
            }

            if (expr is RppSelector)
            {
                RppSelector selector = expr as RppSelector;
                return new RppSelector(selector.Target, new RppFuncCall(selector.Path.Name, args));
            }

            return expr;
        }

        private IList<IRppExpr> ParseArgs()
        {
            Expect(RppLexer.OP_LParen);
            List<IRppExpr> exprs = new List<IRppExpr>();

            IRppExpr expr = ParseExpr();
            if (expr == null)
            {
                Expect(RppLexer.OP_RParen);
                return exprs;
            }

            exprs.Add(expr);

            while (!Require(RppLexer.OP_RParen))
            {
                Expect(RppLexer.OP_Comma);

                expr = ParseExpr();
                if (expr == null)
                {
                    throw new Exception("Expected argument");
                }

                exprs.Add(expr);
            }

            return exprs;
        }
    }
}