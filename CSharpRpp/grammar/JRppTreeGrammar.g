tree grammar JRppTreeGrammar;

options {
  tokenVocab=JRpp;
  ASTLabelType=CommonTree;
  language=CSharp3;
}

@header {
}

@namespace { CSharpRpp }

public walk returns [RppProgram program]
@init {
  program = new RppProgram();
}
    :   ^(RPP_PROGRAM (classDef {program.Add($classDef.node);})*)
    ;
 
classDef returns [RppClass node]
    :   ^(RPP_CLASS id=. { node = new RppClass($id.Text); }
            ^(RPP_FIELDS (c=classParam { node.AddField($c.node); })*)
            ^(RPP_EXTENDS (t=. { node.SetExtends($t.Text); })? )
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
}
    : ^(RPP_PARAMS (param { list.Add($param.node); })*)
    ;

param returns [RppParam node]
    : ^(RPP_PARAM id=. t=. {$node = new RppParam($id.Text, new RppTypeName($t.Text));})
    ;

type returns [RppType node]
    : ^(RPP_TYPE t=. { node = new RppTypeName($t.Text); })
    ;

expr returns [RppExpr node]
    : ^(RPP_EXPR expression) { node = $expression.node; }
    ;

expression returns [RppExpr node]
    : ^('+' a=expression b=expression)  { node = new BinOp("+", $a.node, $b.node); }
    | ^('-' a=expression b=expression)  { node = new BinOp("-", $a.node, $b.node); }
    | IntegerLiteral { node = new RppInteger($IntegerLiteral.text); }
    ;