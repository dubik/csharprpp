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
    :   ^(RPP_CLASS id=.
            ^(RPP_FIELDS c=classParams)
            ^(RPP_EXTENDS (t=.)? )
            ^(RPP_BODY b=templateBody?)
            ) { node = new RppClass($id.Text, ClassKind.Class, $c.list, $b.list); }
    ;

objectDef returns [RppClass node]
    :   ^(RPP_OBJECT id=. ^(RPP_BODY b=templateBody?)) { node = new RppClass($id.Text, ClassKind.Object, new List<RppField>(), $b.list); }
    ;

classParams returns [IList<RppField> list]
@init {
    list = new List<RppField>();
}
    : (p =classParam { list.Add($p.node); })*
    ;

classParam returns [RppField node]
    :   ^(RPP_CLASSPARAM
            id=.
            m=modifiers
            t=type { node = new RppField(MutabilityFlag.MF_Val, $id.Text, $m.list, $t.node); }
         )
    ;

modifiers returns [IList<string> list]
@init {
    list = new List<string>();
}
    : ^(RPP_MODIFIERS (m =. { list.Add($m.Text); })* )
    ;

templateBody returns [IList<IRppNode> list]
@init {
    list = new List<IRppNode>();
}
    : (b=templateBodyStat { list.Add($b.node); })+
    ;

templateBodyStat returns [IRppNode node]
    : ^(RPP_FUNC func=def { node = $func.node; })
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
    : ( ^(RPP_PAT_DEF p=pat_def) { list.AddRange($p.list); } | e=expression {list.Add($e.node);} )*
    ;

pat_def returns [List<IRppExpr> list]
@init {
    list = new List<IRppExpr>();
}
    : decl=. {var varNames = new List<string>(); } (name=Id {varNames.Add($name.Text);})+ t=type e=expression {list.AddRange(varNames.Select(n => new RppVar($decl.Text, n, $t.node, $e.node)));}
    ;

expression returns [IRppExpr node]
    : ^('+' a=expression b=expression)  { node = new BinOp("+", $a.node, $b.node); }
    | ^('-' a=expression b=expression)  { node = new BinOp("-", $a.node, $b.node); }
    | ^('*' a=expression b=expression)  { node = new BinOp("*", $a.node, $b.node); }
    | ^('/' a=expression b=expression)  { node = new BinOp("/", $a.node, $b.node); }
    | ^(RPP_FUNC_CALL id=. ar=args {node = new RppFuncCall($id.Text, $ar.list); })
    | IntegerLiteral { node = new RppInteger($IntegerLiteral.text); }
    | StringLiteral { node = new RppString($StringLiteral.text); }
    | ^(RPP_BLOCK_EXPR  blck=block) { node = new RppBlockExpr($blck.list); }
    | Id { node = new RppId($Id.text); }
    | ^(RPP_NEW id=. ar=args {node = new RppNew($id.Text, $ar.list); })
    ;
