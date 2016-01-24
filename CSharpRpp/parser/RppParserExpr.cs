using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Expr;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.RppLexer;

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
            if (ParseBindings(out bindings) && Peek(OP_Follow))
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
            if (Peek(KW_If))
            {
                return ParseIf();
            }

            if (Peek(KW_While))
            {
                return ParseWhile();
            }

            if (Require(KW_Return))
            {
                throw new Exception("Return not supported");
            }

            if (Require(KW_Throw))
            {
                IToken throwToken = _lastToken;
                return new RppThrow(ParseExpr()) {Token = throwToken};
            }

            return ParsePostfixExpr(0);
        }

        private IRppExpr ParseIf()
        {
            Expect(KW_If);
            Expect(OP_LParen);
            IRppExpr condition = ParseExpr();
            if (condition == null)
            {
                throw new Exception("Expected expression");
            }
            Expect(OP_RParen);
            IRppExpr thenExpr = ParseExpr();
            if (thenExpr == null)
            {
                throw new Exception("Expected then expression");
            }

            IRppExpr elseExpr = RppEmptyExpr.Instance;
            if (Peek(KW_Else))
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
            Expect(KW_While);
            Expect(OP_LParen);
            IRppExpr condition = ParseExpr();
            if (condition == null)
            {
                throw new Exception("Expected expression");
            }
            Expect(OP_RParen);
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
            if (Require(OP_Ops) || Require(OP_Star) || Require(OP_Eq))
            {
                var token = _lastToken;
                op = token.Text;
                switch (token.Text)
                {
                    case "==":
                    case "||":
                    case "&&":
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
                    case ">=":
                    case "<=":
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
            if (Require(KW_Match))
            {
                return ParseMatch(expr);
            }

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
                    expr = RppBinOp.Create(op, expr, rhs);
                }
                else
                {
                    _stream.Rewind();
                    break;
                }
            }

            return expr;
        }

        private IRppExpr ParseMatch(IRppExpr expr)
        {
            Expect(OP_LBrace);
            SkipNewLines();
            IEnumerable<RppCaseClause> clauses = ParseCaseClauses();
            SkipNewLines();
            Expect(OP_RBrace);
            return new RppMatch(expr, clauses);
        }

        [NotNull]
        private IEnumerable<RppCaseClause> ParseCaseClauses()
        {
            List<RppCaseClause> items = new List<RppCaseClause>();
            RppCaseClause item = ParseClause();
            if (item == null)
            {
                throw new SyntaxException("Expected at least one 'case' clause", _lastToken);
            }

            while (item != null)
            {
                items.Add(item);
                SkipNewLines();
                if (!Peek(KW_Case))
                {
                    break;
                }

                item = ParseClause();
            }

            return items;
        }

        [NotNull]
        private IEnumerable<T> OneOrMore<T>(Func<T> generator, string errorMessage, int? separator)
        {
            List<T> items = new List<T>();
            T item = generator();
            if (item == null)
            {
                throw new SyntaxException(errorMessage, _lastToken);
            }

            while (item != null)
            {
                items.Add(item);
                if (separator.HasValue && Peek(separator.Value))
                {
                    Consume();
                }

                item = generator();
            }

            return items;
        }

        [CanBeNull]
        private RppCaseClause ParseClause()
        {
            SkipNewLines();
            Expect(KW_Case);
            IEnumerable<RppMatchPattern> pattern = ParsePattern();
            Expect(OP_Follow);
            IRppExpr block = ParseExpr();
            return new RppCaseClause(pattern.First(), block);
        }

        /*

            Pattern ::= Pattern1 { ‘|’ Pattern1 }
            Pattern1 ::= varid ‘:’ TypePat
                    | ‘_’ ‘:’ TypePat
                    | Pattern2
            Pattern2 ::= varid [‘@’ Pattern3]
                    | Pattern3
            Pattern3 ::= SimplePattern
                    | SimplePattern { id [nl] SimplePattern }
            SimplePattern ::= ‘_’
                    | varid
                    | Literal
                    | StableId
                    | StableId ‘(’ [Patterns ‘)’
                    | StableId ‘(’ [Patterns ‘,’] [varid ‘@’] ‘_’ ‘*’ ‘)’
                    | ‘(’ [Patterns] ‘)’

        */

        private IEnumerable<RppMatchPattern> ParsePattern()
        {
            return OneOrMore(ParsePattern1, "Pattern expected", OP_Bar);
        }

        private RppMatchPattern ParsePattern1()
        {
            if (Require(Id))
            {
                IToken varid = _lastToken;
                if (Require(OP_Colon)) // varid ':' TypePat
                {
                    RTypeName type;
                    if (ParseType(out type, false))
                    {
                        return new RppTypedPattern(varid, type);
                    }

                    throw new SyntaxException("Expected type name but got", _lastToken);
                }

                if (Require(OP_At)) // varid '@' SimplePattern
                {
                    RppMatchPattern simplePattern = ParseSimplePattern();
                    if (simplePattern == null)
                    {
                        throw new SyntaxException("Expected simple patter", _lastToken);
                    }

                    return new RppBinderPattern(varid, simplePattern);
                }
            }

            return ParseSimplePattern();
        }

        /*
            SimplePattern ::= ‘_’
                        | varid
                        | Literal
                        | StableId
                        | StableId ‘(’ [Patterns ‘)’
                        | StableId ‘(’ [Patterns ‘,’] [varid ‘@’] ‘_’ ‘*’ ‘)’
                        | ‘(’ [Patterns] ‘)’

            case Expr("+", Expr(_,_), Num(0)) => 
        */

        [CanBeNull]
        private RppMatchPattern ParseSimplePattern()
        {
            if (Require(OP_Underscore))
            {
                return new RppVariablePattern();
            }

            IRppExpr expr;
            if (ParseLiteral(out expr))
            {
                return new RppLiteralPattern(expr);
            }

            if (ParsePath(out expr))
            {
                if (Require(OP_LParen))
                {
                    IEnumerable<RppMatchPattern> pattern = ParsePattern();
                    Expect(OP_RParen);

                    return new RppConstructorPattern(expr, pattern);
                }
            }

            return null;
        }

        private IRppExpr ParsePrefixExpr()
        {
            int position = _stream.Mark();
            if (Require(OP_LParen))
            {
                IRppExpr expr = ParsePostfixExpr(0);
                if (expr != null && Require(OP_RParen))
                {
                    _stream.Release(position);
                    return expr;
                }
            }

            _stream.Rewind(position);
            return ParseSimpleExpr();
        }

        private IRppExpr ParseSimpleExpr()
        {
            if (Require(KW_New))
            {
                return ParseNewExpr();
            }

            if (Peek(OP_LBrace))
            {
                return ParseBlockExpr();
            }

            return ParseSimpleExpr1();
        }

        private RppNew ParseNewExpr()
        {
            Expect(Id);
            IToken typeNameToken = _lastToken;
            RTypeName typeName = new RTypeName(typeNameToken);
            if (Peek(OP_LBracket))
            {
                IList<RTypeName> genericArguments = ParseTypeParamClause();
                genericArguments.ForEach(typeName.AddGenericArgument);
            }

            IList<IRppExpr> args = ParseArgsOpt();
            return new RppNew(new ResolvableType(typeName), args) {Token = typeNameToken};
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
            Expect(OP_LBrace);
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

            Expect(OP_RBrace);

            return new RppBlockExpr(exprs);
        }

        private IRppExpr ParseSimpleExpr1()
        {
            IRppExpr expr;
            if (ParseLiteral(out expr))
            {
                return expr;
            }

            // Parse tuple (expr, expr, ...)
            if (Peek(OP_LParen))
            {
                int position = _stream.Mark();
                try
                {
                    IList<IRppExpr> exprs = ParseArgs();
                    _stream.Release(position);
                    // Create Tuple<Count>(args...)
                    return new RppNew(new ResolvableType(new RTypeName(CreateTupleClassName(exprs.Count))), exprs);
                }
                catch
                {
                    _stream.Rewind(position);
                }
            }
            else
            {
                ParsePath(out expr);
            }

            return ParseSimpleExprRest(expr);
        }

        private bool ParseLiteral(out IRppExpr literal)
        {
            literal = null;
            if (Require(IntegerLiteral))
            {
                literal = new RppInteger(_lastToken);
            }
            else if (Require(FloatingPointLiteral))
            {
                literal = new RppFloat(_lastToken);
            }
            else if (Require(StringLiteral))
            {
                literal = new RppString(_lastToken);
            }
            else if (Require(InterpolatedStringLiteral))
            {
                literal = new RppString(_lastToken);
            }
            else if (Require(BooleanLiteral))
            {
                literal = new RppBooleanLiteral(_lastToken);
            }
            else if (Require(KW_Null))
            {
                literal = new RppNull();
            }

            return literal != null;
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

            if (Require(Id))
            {
                path = new RppId(_lastToken.Text) {Token = _lastToken};
                while (Require(OP_Dot))
                {
                    Expect(Id);
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
            if (Require(OP_Dot))
            {
                if (Require(Id))
                {
                    return ParseSimpleExprRest(new RppSelector(expr, new RppFieldSelector(_lastToken.Text) {Token = _lastToken}));
                }

                throw new Exception("After . identifier is expected " + _lastToken);
            }

            if (Peek(OP_LBracket))
            {
                IList<RTypeName> typeArgs = ParseTypeParamClause();
                if (!Peek(OP_LParen))
                {
                    throw new SyntaxException("Expecting function call after type arguments", _lastToken);
                }

                IList<IRppExpr> args = ParseArgs();
                IList<ResolvableType> genericArguments = typeArgs.Select(ta => new ResolvableType(ta)).ToList();
                return ParseSimpleExprRest(MakeCall(expr, args, genericArguments));
            }

            if (Peek(OP_LParen))
            {
                IList<IRppExpr> args = ParseArgs();
                return ParseSimpleExprRest(MakeCall(expr, args, Collections.NoResolvableTypes));
            }

            return expr;
        }

        private IList<IRppExpr> ParseArgsOpt()
        {
            if (Peek(OP_LParen))
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
                return new RppFuncCall(id.Name, args, typeArgs) {Token = id.Token};
            }

            if (expr is RppSelector)
            {
                RppSelector selector = expr as RppSelector;
                RppFuncCall funcCall = new RppFuncCall(selector.Path.Name, args, typeArgs) {Token = selector.Path.Token};
                return new RppSelector(selector.Target, funcCall);
            }

            return expr;
        }

        private IList<IRppExpr> ParseArgs()
        {
            Expect(OP_LParen);
            List<IRppExpr> exprs = new List<IRppExpr>();

            IRppExpr expr = ParseExpr();
            if (expr == null)
            {
                Expect(OP_RParen);
                return exprs;
            }

            exprs.Add(expr);

            while (!Require(OP_RParen))
            {
                Expect(OP_Comma);

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