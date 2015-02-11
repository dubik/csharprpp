using System;
using System.Collections.Generic;
using Antlr.Runtime;

namespace CSharpRpp
{
    class QualifiedId
    {
        private string _text;

        public QualifiedId(string text)
        {
            _text = text;
        }
    }

    enum ObjectModifier
    {
        OmNone,
        OmPrivate,
        OmProtected,
        OmOverride,
        OmFinal,
        OmSealed,
        OmImplicit,
        OmLazy
    }

    public partial class RppParser
    {
        private readonly ITokenStream _stream;

        private IToken _lastToken;

        public RppParser(ITokenStream stream)
        {
            _stream = stream;
        }

        private bool Require(int token)
        {
            IToken nextToken = _stream.LT(1);
            if (nextToken.Type == token)
            {
                _lastToken = nextToken;
                _stream.Consume();
                return true;
            }

            return false;
        }

        private void Expect(int token)
        {
            if (!Require(token))
            {
                throw new Exception("Expected token : " + token + " but got " + _stream.LT(1).Text);
            }
        }

        private bool Peek(int token)
        {
            return _stream.LA(1) == token;
        }

        private bool ParseSemi()
        {
            if (Require(RppLexer.OP_Semi) || Require(RppLexer.EOF))
            {
                return true;
            }

            bool ok = false;
            while (Require(RppLexer.NewLine))
            {
                ok = true;
            }

            return ok;
        }

        private void ExpectSemi()
        {
            if (!ParseSemi())
            {
                throw new Exception("Expected ; or a new line but got: " + _lastToken.Text);
            }
        }

        public RppProgram CompilationUnit()
        {
            RppProgram program = new RppProgram();

            if (Require(RppLexer.KW_Package))
            {
                QualifiedId id = ParseQualifiedId();
                ExpectSemi();
            }

            ParseTopStatSeq(program);

            Expect(RppLexer.EOF);

            return program;
        }

        private QualifiedId ParseQualifiedId()
        {
            Expect(RppLexer.Id);

            return new QualifiedId(_lastToken.Text);
        }

        private void ParseTopStatSeq(RppProgram program)
        {
            while (ParseTopStat(program))
            {
                ParseSemi();
            }
        }

        private bool ParseTopStat(RppProgram program)
        {
            var modifiers = ParseObjectModifier();
            return ParseTmplDef(modifiers, program);
        }

        private bool ParseTmplDef(HashSet<ObjectModifier> modifiers, RppProgram program)
        {
            if (Require(RppLexer.KW_Trait))
            {
                return true;
            }

            if (Require(RppLexer.KW_Class))
            {
                ParseClassDef();
            }

            if (Require(RppLexer.KW_Object))
            {
                RppClass obj = ParseObjectDef();
                program.Add(obj);
                return true;
            }

            return false;
        }

/*
 
    ClassDef ::= id [TypeParamClause] {Annotation} [AccessModifier] ClassParamClauses ClassTemplateOpt
 
bool RppParser::parse_class_def(ObjectNode * objectNode)
{
    if (require(ID_Ident)) {
        objectNode->setNameToken(lastToken);
        vector<string> typeParams;
        if (parse_type_param_clause(typeParams))
        {
            objectNode->setGenericsTypes(typeParams);
        }

        parse_class_param_clauses(objectNode);
        return parse_class_template_opt(objectNode);
    } else {
        throw UnexpectedTokenException(lastToken, ID_Ident);
    }
}
*/
        // ClassDef ::= id [TypeParamClause] {Annotation} [AccessModifier] ClassParamClauses ClassTemplateOpt
        public void ParseClassDef()
        {
            if (Require(RppLexer.Id))
            {
                string name = _lastToken.Text;
                IList<RppType> typeParams = ParseTypeParamClause();
                IList<RppField> classParams = ParseClassParamClause();
            }
            else
            {
                throw new Exception("Expected identifier but got : " + _lastToken.Text);
            }
        }

        // ClassParamClause ::= '(' [ClassParams] ')'
        public IList<RppField> ParseClassParamClause()
        {
            var classParams = new List<RppField>();
            if (Require(RppLexer.OP_LParen))
            {
                while (!Require(RppLexer.OP_RParen))
                {
                    RppField classParam;
                    if (!ParseClassParam(out classParam))
                    {
                        throw new Exception("Class param was expected but got " + _lastToken.Text);
                    }

                    classParams.Add(classParam);
                }
            }

            return classParams;
        }

        // ClassParams ::= {Annotation} [{Modifier} (‘val’ | ‘var’)] id [‘:’ ParamType] [‘=’ Expr]
        public bool ParseClassParam(out RppField classParam)
        {
            classParam = null;

            MutabilityFlag mutability = MutabilityFlag.MF_Val;
            if (Require(RppLexer.KW_Var))
            {
                mutability = MutabilityFlag.MF_Var;
            }
            else
            {
                Require(RppLexer.KW_Val);
            }

            if (!Require(RppLexer.Id))
            {
                return false;
            }

            string name = _lastToken.Text;
            Expect(RppLexer.OP_Colon);
            RppType paramType;
            if (!ParseType(out paramType))
            {
                throw new Exception("Expected type but found: " + _lastToken.Text);
            }

            classParam = new RppField(mutability, name, new List<string>(), paramType);
            return true;
        }

        // [T, N]
        private IList<RppType> ParseTypeParamClause()
        {
            IList<RppType> typeParams = new List<RppType>();
            if (Require(RppLexer.OP_LBracket))
            {
                RppType type;

                while (ParseType(out type))
                {
                    typeParams.Add(type);
                    if (Require(RppLexer.OP_RBracket))
                    {
                        break;
                    }

                    if (!Require(RppLexer.OP_Comma))
                    {
                        throw new Exception("Expected , or ] but got " + _lastToken.Text);
                    }
                }
            }

            return typeParams;
        }

        public IList<IRppNode> ParseClassTemplateOpt(string name)
        {
            if (Require(RppLexer.KW_Extends))
            {
                throw new Exception("Extending a class is not implemented yet");
            }

            return ParseTemplateBody();
        }

        public IList<IRppNode> ParseTemplateBody()
        {
            Require(RppLexer.NewLine);

            List<IRppNode> stats = new List<IRppNode>();
            if (Require(RppLexer.OP_LBrace))
            {
                IRppNode stat;
                while (ParseSemi() && (stat = ParseTemplateStat()) != null)
                {
                    stats.Add(stat);
                }

                Expect(RppLexer.OP_RBrace);
            }

            return stats;
        }

        public IRppNode ParseTemplateStat()
        {
            ParseModifier(); // TODO handle modifiers

            IRppNode stat = ParseDef();
            if (stat != null)
            {
                return stat;
            }

            stat = ParseDcl();
            if (stat != null)
            {
                return stat;
            }

            stat = ParseExpr1();
            return stat;
        }

        private static IRppNode ParseDcl()
        {
            return null;
        }

        private IRppNode ParseDef()
        {
            if (Require(RppLexer.KW_Val))
            {
                return ParsePatDef(MutabilityFlag.MF_Val);
            }

            if (Require(RppLexer.KW_Var))
            {
                return ParsePatDef(MutabilityFlag.MF_Var);
            }

            if (Require(RppLexer.KW_Def))
            {
                return ParseFunDef();
            }

            return null;
        }

        // FunSig [‘:’ Type] ‘=’ Expr
        // FunSig ::= id [FunTypeParamClause] ParamClauses

        private RppFunc ParseFunDef()
        {
            Expect(RppLexer.Id);
            string name = _lastToken.Text;
            IList<RppType> typeParams = ParseTypeParamClause();
            IEnumerable<IRppParam> funcParams = ParseParamClauses();
            RppType funcReturnType;
            if (!ParseType(out funcReturnType))
            {
                throw new Exception("Expecting type but got " + _lastToken);
            }

            Expect(RppLexer.OP_Eq);
            IRppExpr expr = ParseExpr();

            return new RppFunc(name, funcParams, funcReturnType, expr);
        }


        public IEnumerable<IRppParam> ParseParamClauses()
        {
            if (Require(RppLexer.OP_LParen))
            {
                var res = ParseParams();
                Expect(RppLexer.OP_RParen);
                return res;
            }

            return RppFunc.EmptyParams;
        }

        // param {, param}
        public IEnumerable<IRppParam> ParseParams()
        {
            IList<IRppParam> funcParams = new List<IRppParam>();
            int paramIndex = 0;
            while (true)
            {
                RppParam funcParam;
                if (ParseParam(paramIndex, out funcParam))
                {
                    paramIndex++;
                    funcParams.Add(funcParam);
                }
                if (!Require(RppLexer.OP_Comma))
                {
                    break;
                }
            }

            return funcParams;
        }

        //param ::= {Annotation} id [‘:’ ParamType] [‘=’ Expr]
        private bool ParseParam(int paramIndex, out RppParam funcParam)
        {
            funcParam = null;
            if (!Require(RppLexer.Id))
            {
                return false;
            }
            string name = _lastToken.Text;
            Expect(RppLexer.OP_Colon);
            RppType type;
            if (!ParseType(out type))
            {
                throw new Exception("Expected type but got " + _lastToken.Text);
            }

            funcParam = new RppParam(name, paramIndex, type);
            return true;
        }

        // PatDef ::= Pattern2 {',' Pattern2} [':' Type] '=' Expr
        public RppField ParsePatDef(MutabilityFlag mutabilityFlag)
        {
            Expect(RppLexer.Id);
            string varId = _lastToken.Text;

            Expect(RppLexer.OP_Colon);
            RppType type;
            if (!ParseType(out type))
            {
                throw new Exception("Expected type after ':' but got " + _lastToken.Text);
            }

            IRppExpr expr = ParseExpr();
            RppField var = new RppField(mutabilityFlag, varId, null, type, expr);
            return var;
        }


        public bool ParseModifier()
        {
            return ParseLocalModifier() && ParseAccessModifier();
        }

        private bool ParseAccessModifier()
        {
            return false;
        }

        private bool ParseLocalModifier()
        {
            return false;
        }

        public bool ParseType(out RppType type)
        {
            if (Require(RppLexer.Id))
            {
                string typeName = _lastToken.Text;
                if (Require(RppLexer.OP_LBracket))
                {
                    RppGenericType genericType = new RppGenericType(typeName);
                    type = genericType;

                    RppType subType;
                    if (!ParseType(out subType))
                    {
                        throw new Exception("Expected type but got " + _lastToken.Text);
                    }

                    genericType.AddParam(subType);

                    while (true)
                    {
                        if (Require(RppLexer.OP_RBracket))
                        {
                            break;
                        }

                        if (Require(RppLexer.OP_Comma))
                        {
                            if (!ParseType(out subType))
                            {
                                throw new Exception("Expected type but got " + _lastToken.Text);
                            }

                            genericType.AddParam(subType);
                        }
                        else
                        {
                            throw new Exception("Expected comma but got " + _lastToken.Text);
                        }
                    }

                    return true;
                }

                type = new RppTypeName(typeName);
                return true;
            }

            type = null;
            return false;
        }

        private RppClass ParseObjectDef()
        {
            Expect(RppLexer.Id);
            string objectName = _lastToken.Text;

            IList<IRppNode> stats = ParseClassTemplateOpt(objectName);
            return new RppClass(objectName, ClassKind.Object, new List<RppField>(), stats);
        }

        private HashSet<ObjectModifier> ParseObjectModifier()
        {
            HashSet<ObjectModifier> modifiers = new HashSet<ObjectModifier>();
            if (Require(RppLexer.KW_Lazy))
            {
                modifiers.Add(ObjectModifier.OmLazy);
            }

            return modifiers;
        }
    }
}