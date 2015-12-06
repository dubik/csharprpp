using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Expr;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public partial class RppParser
    {
        public IRppExpr ParseExpr()
        {
            // TODO scala parser does it in a better way, it doesn't backtrack, just accomulates comma
            // separated things and when it notice => it creates a closure. Doing backtracking here is quite heavy task
            int rememberedPos = _stream.Mark();
            IEnumerable<IRppParam> bindings;
            if (ParseBindings(out bindings) && Peek(RppLexer.OP_Follow))
            {
                _stream.Release(rememberedPos);
                Consume(); // '=>'
                IRppExpr body = ParseExpr();
                return new RppClosure(bindings, body);
            }

            _stream.Rewind(rememberedPos);
            return ParseExpr1();
        }

        private IRppExpr ParseExpr1()
        {
            if (Peek(RppLexer.KW_If))
            {
                return ParseIf();
            }

            if (Peek(RppLexer.KW_While))
            {
                return ParseWhile();
            }

            if (Require(RppLexer.KW_Return))
            {
                throw new Exception("Return not supported");
            }

            if (Require(RppLexer.KW_Throw))
            {
                IToken throwToken = _lastToken;
                return new RppThrow(ParseExpr()) {Token = throwToken};
            }

            return ParsePostfixExpr(0);
        }

        private IRppExpr ParseIf()
        {
            Expect(RppLexer.KW_If);
            Expect(RppLexer.OP_LParen);
            IRppExpr condition = ParseExpr();
            if (condition == null)
            {
                throw new Exception("Expected expression");
            }
            Expect(RppLexer.OP_RParen);
            IRppExpr thenExpr = ParseExpr();
            if (thenExpr == null)
            {
                throw new Exception("Expected then expression");
            }

            IRppExpr elseExpr = RppEmptyExpr.Instance;
            if (Peek(RppLexer.KW_Else))
            {
                Consume();
                elseExpr = ParseExpr();
                if (elseExpr == null)
                {
                    throw new Exception("Expected else expression");
                }
            }

            return new RppIf(condition, thenExpr, elseExpr);
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
            RTypeName typeName = new RTypeName(_lastToken);
            if (Peek(RppLexer.OP_LBracket))
            {
                IList<RTypeName> genericArguments = ParseTypeParamClause();
                genericArguments.ForEach(typeName.AddGenericArgument);
            }

            IList<IRppExpr> args = ParseArgsOpt();
            return new RppNew(new ResolvableType(typeName), args);
        }

        /*
    Block ::= {BlockStat semi} [ResultExpr]

    BlockStat ::= ImportClass
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
                IRppNode defNode = ParseDef(ParseObjectModifier());
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
                expr = new RppNull();
            }
            else if (Require(RppLexer.BooleanLiteral))
            {
                expr = new RppBooleanLiteral(_lastToken.Text);
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
                    path = new RppSelector(path, new RppFieldSelector(_lastToken.Text) {Token = _lastToken});
                }

                return true;
            }

            return false;
        }

        // clazz.myField
        // class.Func()
        // class.Func[Int]()
        private IRppExpr ParseSimpleExprRest(IRppExpr expr)
        {
            if (Require(RppLexer.OP_Dot))
            {
                if (Require(RppLexer.Id))
                {
                    return ParseSimpleExprRest(new RppSelector(expr, new RppFieldSelector(_lastToken.Text) {Token = _lastToken}));
                }

                throw new Exception("After . identifier is expected " + _lastToken);
            }

            if (Peek(RppLexer.OP_LBracket))
            {
                IList<RTypeName> typeArgs = ParseTypeParamClause();
                if (!Peek(RppLexer.OP_LParen))
                {
                    throw new SyntaxException("Expecting function call after type arguments", _lastToken);
                }

                IList<IRppExpr> args = ParseArgs();
                IList<ResolvableType> genericArguments = typeArgs.Select(ta => new ResolvableType(ta)).ToList();
                return ParseSimpleExprRest(MakeCall(expr, args, genericArguments));
            }

            if (Peek(RppLexer.OP_LParen))
            {
                IList<IRppExpr> args = ParseArgs();
                return ParseSimpleExprRest(MakeCall(expr, args, Collections.NoResolvableTypes));
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

        private static IRppExpr MakeCall(IRppExpr expr, IList<IRppExpr> args, IList<ResolvableType> typeArgs)
        {
            if (expr is RppId)
            {
                RppId id = expr as RppId;
                return new RppFuncCall(id.Name, args, typeArgs);
            }

            if (expr is RppSelector)
            {
                RppSelector selector = expr as RppSelector;
                return new RppSelector(selector.Target, new RppFuncCall(selector.Path.Name, args, typeArgs));
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