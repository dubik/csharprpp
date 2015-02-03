grammar JRpp;

options
{
    output=AST;
    language=CSharp3;
}

tokens
{
    PLUS = '+';
    MINUS = '-';
    TIMES = '*';
    DIVIDE = '/';

    ASSIGN = '=';

    CLASS = 'class';
    OBJECT = 'object';
    ABSTRACT = 'abstract';
    CASE = 'case';
    CATCH = 'catch';
    DEF = 'def';
    DO = 'do';
    ELSE = 'else';
    EXTENDS = 'extends';
    FINAL = 'final';
    FOR = 'for';
    IF = 'if';
    IMPORT = 'import';
    MATCH = 'match';
    NEW = 'new';
    NULL = 'null';
    OVERRIDE = 'override';
    PACKAGE = 'package';
    PRIVATE = 'private';
    PROTECTED = 'protected';
    RETURN = 'return';
    THROW = 'throw';
    TRAIT = 'trait';
    TRY = 'try';
    TYPE = 'type';
    VAL = 'val';
    VAR = 'var';
    WHILE = 'while';
    WITH = 'with';
    YIELD = 'yield';

    UNDERSCORE = '_';

    MOD = '%';
    DOT = '.';
    LBRACE = '{';
    RBRACE = '}';
    LPAREN = '(';
    RPAREN = ')';
    SEMI = ';';
    COMMA = ',';
    LT = '<';
    GT = '>';
    OR = '||';
    AND = '&&';
    BIT_OR = '|';
    BIT_AND = '&';
    LSHIFT = '<<';
    RSHIFT = '>>';
    TILD = '~';
    NOT = '!';
    RPP_CLASS;
    RPP_OBJECT;
    RPP_FIELDS;
    RPP_BODY;
    RPP_CLASSPARAM;
    RPP_MODIFIERS;
    RPP_TYPE;
    RPP_EXTENDS;
    RPP_FUNC;
    RPP_FUNC_CALL;
    RPP_EXPR;
    RPP_BLOCK_EXPR;
    RPP_PARAMS;
    RPP_PARAM;
    RPP_BINOP;
    RPP_IF;
    RPP_THEN;
    RPP_ELSE;
    RPP_WHILE;
    RPP_STATS;
    RPP_PROGRAM;
    RPP_GENERIC_TYPE;
}

@parser::namespace { CSharpRpp }
@lexer::namespace { CSharpRpp }

@parser::members
{
        bool isRightToLeft(int type)
        {
            // return true here for any operators that are right-to-left associative
            return type == NOT || type == TILD;
        }

        int getOperatorPrecedence(int type)
        {
            switch (type)
            {
                case ASSIGN:
                    return 4;
                case PLUS:
                case MINUS:
                    return 3;
                case TIMES:
                case DIVIDE:
                    return 2;
                case NOT:
                    return 1;
                default:
                    return 0; // really this shouldn't be hit
            }
        }

        int findPivot(IList<IToken> operators, int startIndex, int stopIndex)
        {
            int pivot = startIndex;
            int pivotRank = getOperatorPrecedence(operators[pivot].Type);
            for (int i = startIndex + 1; i <= stopIndex; i++)
            {
                int type = operators[pivot].Type;
                int current = getOperatorPrecedence(type);
                bool rtl = isRightToLeft(type);
                if (current > pivotRank || (current == pivotRank && rtl))
                {
                    pivot = i;
                    pivotRank = current;
                }
            }
            return pivot;
        }

        ITree createPrecedenceTree(IList<ITree> expressions, IList<IToken> operators, int startIndex, int stopIndex)
        {
            if (stopIndex == startIndex)
                return expressions[startIndex];

            int pivot = findPivot(operators, startIndex, stopIndex - 1);
            ITree root = adaptor.Nil() as ITree;
            object root_1 = adaptor.Nil();
            root_1 = adaptor.BecomeRoot(operators[pivot], root_1);
            adaptor.AddChild(root_1, createPrecedenceTree(expressions, operators, startIndex, pivot));
            adaptor.AddChild(root_1, createPrecedenceTree(expressions, operators, pivot + 1, stopIndex));
            adaptor.AddChild(root, root_1);
            return root;
        }

        ITree createPrecedenceTree(IList<ITree> expressions, IList<IToken> operators)
        {
            return createPrecedenceTree(expressions, operators, 0, expressions.Count - 1);
        }
}

public compilationUnit : ('package' qualId)? topStatSeq -> ^(RPP_PROGRAM topStatSeq);
qualId : Id ('.' Id)*;
topStatSeq : (NewLine!)* topStat (NewLine!+ topStat?)*;
topStat : modifier* tmplDef
    ;

modifier : 'private' | 'protected' | 'sealed';
tmplDef : 'class'! classDef
    | 'object'! objectDef
    ;

objectDef : Id templateBody -> ^(RPP_OBJECT Id ^(RPP_BODY templateBody?))
    ;

classDef : Id classParamClause? classTemplateOpt -> ^(RPP_CLASS Id ^(RPP_FIELDS classParamClause?) classTemplateOpt);

classParamClause : '('! classParams? ')'!;
 
classParams : classParam (',' classParam)*;

classParam : modifier* ('val' | 'var')? Id ':' paramType -> ^(RPP_CLASSPARAM Id ^(RPP_MODIFIERS modifier*) paramType);

classTemplateOpt : ('extends' Id)? NewLine? templateBody? -> ^(RPP_EXTENDS Id?) ^(RPP_BODY templateBody?);

templateBody : '{'! NewLine!* (templateStat NewLine!*)* '}'!;

templateStat : 'def' funDef -> funDef
              ;

funDef : funSig ':' type '=' expression -> ^(RPP_FUNC funSig type ^(RPP_EXPR expression))
    ;

funSig : Id ('(' params? ')')? -> Id ^(RPP_PARAMS params?);

params : param (','! param)*;

param : Id ':' paramType -> ^(RPP_PARAM Id paramType);

paramType : type;

type :
        primitiveType -> ^(RPP_TYPE primitiveType)
    |   Id  -> ^(RPP_TYPE Id)
    |   Id  '[' type (',' type)* ']' -> ^(RPP_GENERIC_TYPE Id type*)
    ;

expression 
@init
{
    IList<ITree> expressions = new List<ITree>();
    IList<IToken> operators = new List<IToken>();
}
        :       (       left=primaryExpression
                        {
                            object leftTree = $left.tree;
                            if(leftTree != null) {
                                expressions.Add(leftTree as ITree);
                            }
                        }
                )
                (       operator
                        right=primaryExpression
                        {
                                operators.Add($operator.start);
                                object rightTree = $right.tree;
                                if(rightTree != null) {
                                    expressions.Add(rightTree as ITree);
                                }
                        }
                )*
                -> {createPrecedenceTree(expressions,operators)}
        ;

operator
        :       '+'
        |       '-'
        |       '*'
        |       '/'
        |       '='
        |       '~'
        |       '!'
        ;

primaryExpression
    :   literal
    |   '('! expression ')'!
    |   funcCall
    |   Id
    |   '{' NewLine* (block=blockExpr NewLine*)? '}' -> ^(RPP_BLOCK_EXPR $block?)
    |   'if' '(' cond=expression ')' thenExpr=expression ('else' elseExpr=expression)? -> ^(RPP_IF $cond ^(RPP_THEN $thenExpr) ^(RPP_ELSE $elseExpr?))
    |   'while' '(' cond=expression ')' block=expression -> ^(RPP_WHILE $cond ^(RPP_BODY $block))
    ;

funcCall : Id '(' exprs? ')'  -> ^(RPP_FUNC_CALL Id ^(RPP_PARAMS exprs?));

exprs : expression (','! expression)*;

blockExpr: expression (NewLine! expression)*;

literal :
        IntegerLiteral
    |   BooleanLiteral
    |   FloatingPointLiteral
    |   StringLiteral
    |   'null'
    ;

primitiveType 
    :   'Bool'
    |   'Char'
    |   'Byte'
    |   'Short'
    |   'Int'
    |   'Long'
    |   'Float'
    |   'Double'
    |   'Unit'
    ;


BooleanLiteral   :  'true' | 'false';
Id : ('a' .. 'z'| 'A' .. 'Z')+ ; 

NewLine               :  '\r'? '\n';
IntegerLiteral : (DecimalNumber) ('L' | 'l')?;
FloatingPointLiteral
                 :  Digit+ '.' Digit+ ExponentPart? FloatType?
                 |  '.' Digit+ ExponentPart? FloatType?
                 ;
StringLiteral :
              '"' StringElement* '"'
              ;

fragment StringElement : '\u0020'| '\u0021'|'\u0023' .. '\u007F' | CharEscapeSeq;

fragment DecimalNumber: Digit+;
fragment HexNumeral : '0' 'x' HexDigit+;
fragment HexDigit :  '0' .. '9'  |  'A' .. 'F'  |  'a' .. 'f' ;
fragment FloatType        :  'F' | 'f' | 'D' | 'd';
fragment ExponentPart     :  ('E' | 'e') ('+' | '-')? Digit+;
fragment Digit : '0' | NonZeroDigit;
fragment NonZeroDigit : '1' .. '9';
fragment PrintableChar    : '\u0020' .. '\u007F' ;
fragment CharEscapeSeq    : '\\' ('b' | 't' | 'n' | 'f' | 'r' | '"' | '\'' | '\\');

WS : ( '\t' | ' ' | '\u000C' )+ { $channel = Antlr.Runtime.TokenChannels.Hidden; } ;
