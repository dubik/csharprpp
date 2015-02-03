tree grammar JRppTreeGrammar;

options {
  tokenVocab=JRpp;
  ASTLabelType=CommonTree;
  language=CSharp3;
}

@header {
using System.Linq;
}

@namespace { CSharpRpp }

public walk returns [RppProgram program]
@init {
  program = new RppProgram();
}
    :   ^(RPP_PROGRAM (programDef {program.Add($programDef.node);})*)
    ;

programDef returns [RppClass node]: classDef { node = $classDef.node;}
    | objectDef { node = $objectDef.node;}
    ;

classDef returns [RppClass node]
    :   ^(RPP_CLASS id=. { node = new RppClass($id.Text, ClassKind.Class); }
            ^(RPP_FIELDS (c=classParam { node.AddField($c.node); })*)
            ^(RPP_EXTENDS (t=. { node.SetExtends($t.Text); })? )
            ^(RPP_BODY templateBody[node]?)
            )
    ;

objectDef returns [RppClass node]
    :   ^(RPP_OBJECT id=. { node = new RppClass($id.Text, ClassKind.Object); }
            ^(RPP_BODY templateBody[node]?)
        )
    ;

classParam returns [RppField node]
    :   ^(RPP_CLASSPARAM
            id=.
            m=modifiers
            t=type { node = new RppField($id.Text, $m.list, $t.node); }
         )
    ;

modifiers returns [IList<string> list]
@init {
    list = new List<string>();
}
    : ^(RPP_MODIFIERS (m =. { list.Add($m.Text); })* )
    ;

templateBody[RppClass node]
    : templateBodyStat[node]+
    ;

templateBodyStat[RppClass node]
    : ^(RPP_FUNC func=def { node.AddFunc($func.node); })
    | ^(RPP_FIELD id=.)
    ;

def returns [RppFunc node]
    : id=.
        p=params
        r=type
        e=expr { node = new RppFunc($id.Text, $p.list, $r.node, $e.node); }
    ;

params returns [IList<RppParam> list]
@init {
    list = new List<RppParam>();
    int index = 0;
}
    : ^(RPP_PARAMS (param[index] { list.Add($param.node); index++; })*)
    ;

param[int index] returns [RppParam node]
    : ^(RPP_PARAM id=. t=type {$node = new RppParam($id.Text, $index, $t.node);})
    ;

type returns [RppType node]
    : ^(RPP_TYPE t=. { node = new RppTypeName($t.Text); })
    | ^(RPP_GENERIC_TYPE id=. {RppGenericType genericType = new RppGenericType($id.Text); node = genericType;} (subType=type {genericType.AddParam($subType.node);})*)
    ;

expr returns [IRppExpr node]
    : ^(RPP_EXPR expression) { node = $expression.node; }
    ;

args returns [IList<IRppExpr> list]
@init {
    list = new List<IRppExpr>();
}
    : ^(RPP_PARAMS (e=expression {list.Add($e.node);})*)
    ;

block returns [List<IRppExpr> list]
@init {
    list = new List<IRppExpr>();
}
    : ( e=expression {list.Add($e.node);} )*
    | ^(RPP_PAT_DEF decl=. {var varNames = new List<string>();} (name=. {varNames.Add($name.Text);})+ t=type e=expression {list.AddRange(varNames.Select(n => new RppVar($decl.Text, n, $t.node, $e.node)));})
    ;

expression returns [IRppExpr node]
    : ^('+' a=expression b=expression)  { node = new BinOp("+", $a.node, $b.node); }
    | ^('-' a=expression b=expression)  { node = new BinOp("-", $a.node, $b.node); }
    | ^(RPP_FUNC_CALL id=. ar=args {node = new RppFuncCall($id.Text, $ar.list); })
    | IntegerLiteral { node = new RppInteger($IntegerLiteral.text); }
    | StringLiteral { node = new RppString($StringLiteral.text); }
    | ^(RPP_BLOCK_EXPR  (blck=block)) { node = new RppBlockExpr($blck.list); }
    | Id { node = new RppId($Id.text); }
    | ^(RPP_NEW id=. ar=args {node = new RppNew($id.Text, $ar.list); })
    ;
