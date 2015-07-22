using System;
using System.Collections.Generic;
using System.Text;

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

            if (Peek(RppLexer.KW_While))
            {
                return ParseWhile();
            }

            if (Require(RppLexer.KW_Return))
            {
                throw new Exception("Return not supported");
            }

            return ParsePostfixExpr(0);
        }

        private RppWhile ParseWhile()
        {
            Expect(RppLexer.KW_While);
            Expect(RppLexer.OP_LParen);
            IRppExpr condition = ParseExpr();
            if (condition == null)
            {
                throw new Exception("Expected expression");
            }
            Expect(RppLexer.OP_RParen);
            ParseSemi();
            IRppExpr body = ParseExpr();
            if (body == null)
            {
                throw new Exception("Expected body");
            }

            return new RppWhile(condition, body);
        }

        private bool ParseOperator(out string op, out int precedence, out bool leftAssoc)
        {
            leftAssoc = false;
            if (Require(RppLexer.OP_Ops) || Require(RppLexer.OP_Star) || Require(RppLexer.OP_Eq))
            {
                var token = _lastToken;
                op = token.Text;
                switch (token.Text)
                {
                    case "||":
                        precedence = 2;
                        break;
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
                    expr = BinOp.Create(op, expr, rhs);
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
                return ParseNewExpr();
            }

            if (Peek(RppLexer.OP_LBrace))
            {
                return ParseBlockExpr();
            }

            return ParseSimpleExpr1();
        }

        private RppNew ParseNewExpr()
        {
            Expect(RppLexer.Id);
            string name = _lastToken.Text;

            IList<IRppExpr> args = ParseArgsOpt();
            return new RppNew(name, args);
        }

        /*
    Block ::= {BlockStat semi} [ResultExpr]

    BlockStat ::= Import
                | {Annotation} [‘implicit’ | ‘lazy’] Def
                | {Annotation} {LocalModifier} TmplDef
                | Expr1
                |
        */

        public RppBlockExpr ParseBlockExpr()
        {
            Expect(RppLexer.OP_LBrace);
            IList<IRppNode> exprs = new List<IRppNode>();
            while (true)
            {
                SkipNewLines();
                IRppNode defNode = ParseDef();
                if (defNode != null)
                {
                    exprs.Add(defNode);
                    continue;
                }

                IRppExpr expr = ParseExpr();
                if (expr == null)
                {
                    break;
                }

                exprs.Add(expr);
                SkipNewLines();
            }

            Expect(RppLexer.OP_RBrace);

            return new RppBlockExpr(exprs);
        }

        private IRppExpr ParseSimpleExpr1()
        {
            IRppExpr expr;
            if (Require(RppLexer.IntegerLiteral))
            {
                expr = new RppInteger(_lastToken.Text);
            }
            else if (Require(RppLexer.FloatingPointLiteral))
            {
                expr = new RppFloat(_lastToken.Text);
            }
            else if (Require(RppLexer.StringLiteral))
            {
                expr = new RppString(_lastToken.Text);
            }
            else if (Require(RppLexer.InterpolatedStringLiteral))
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

        // Creating expressions which can process s"..."
        private IRppExpr ProcessInterpolatedString(string str)
        {
            // s"My $p"
            // s"my ${p.Text}"
            // s"My ${p.Calculate() - 10}"
            string s = str.Substring(2, str.Length - 3);
            int index = 0;
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                if (index == s.Length)
                {
                    break;
                }

                if (s[index] == '$')
                {
                    index++;
                    if (index == s.Length)
                    {
                        throw new Exception("id or $ or { should follow single $");
                    }

                    if (s[index] == '$')
                    {
                        builder.Append('$');
                        index++;
                    }
                    else
                    {
                        if (s[index] == '{')
                        {
                            index++;
                            StringBuilder exprStr = new StringBuilder();
                            while (s[index] != '}')
                            {
                                if (index + 1 == s.Length)
                                {
                                    throw new Exception("{ should be closed with }");
                                }

                                exprStr.Append(s[index]);
                            }
                        }
                    }
                }
            }

            return null;
        }

        public bool ParsePath(out IRppExpr path)
        {
            path = null;

            if (Require(RppLexer.Id))
            {
                path = new RppId(_lastToken.Text) {Token = _lastToken};
                while (Require(RppLexer.OP_Dot))
                {
                    Expect(RppLexer.Id);
                    path = new RppSelector(path, new RppId(_lastToken.Text) {Token = _lastToken});
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
                    return ParseSimpleExprRest(new RppSelector(expr, new RppId(_lastToken.Text) {Token = _lastToken}));
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

        private IList<IRppExpr> ParseArgsOpt()
        {
            if (Peek(RppLexer.OP_LParen))
            {
                return ParseArgs();
            }

            return Collections.NoExprs;
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
                return new RppSelector(selector.Target, new RppMessage(selector.Path.Name, args));
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