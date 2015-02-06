using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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

    public class RppParser
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
                throw new Exception("Expected token : " + token + " but got " + _stream.LA(1));
            }
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
                ParseObjectDef();
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

        private void ParseObjectDef()
        {
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