tree grammar JRppTreeGrammar;

options {
  tokenVocab=JRpp;
  ASTLabelType=CommonTree;
  language=CSharp3;
}

@header {
  //package org.dubik.jrpp.parser;
  //import org.dubik.jrpp.*;
}

@namespace { CSharpRpp }

walk returns [RppProgram program]
@init {
  program = new RppProgram();
}
    :   ^(RPP_PROGRAM (classDef {program.add($classDef.node);})*)
    ;
 
classDef returns [RppClass node]
    :   ^(RPP_CLASS id=. { node = new RppClass($id.getText()); }
            ^(RPP_FIELDS (c=classParam { node.addField($c.node); })*)
            ^(RPP_EXTENDS (t=. { node.setExtends($t.getText()); })? )
            ^(RPP_BODY templateBody[node]?)
            )
    ;


classParam returns [RppField node]
    :   ^(RPP_CLASSPARAM
            id=.
            m=modifiers
            t=type { node = new RppField($id.getText(), $m.list, $t.node); }
         )
    ;

modifiers returns [List<String> list]
@init {
    list = new ArrayList<String>();
}
    : ^(RPP_MODIFIERS (m =. { list.add($m.getText()); })* )
    ;

templateBody[RppClass node]
    : ^(RPP_FUNC func=def { node.addFunc($func.node); })
    | ^(RPP_FIELD id=.)
    ;

def returns [RppFunc node]
    : id=.
        p=params
        r=type
        e=expr { node = new RppFunc($id.getText(), $p.list, $r.node, $e.node); }
    ;

params returns [List<RppParam> list]
@init {
    list = new ArrayList<RppParam>();
}
    : ^(RPP_PARAMS (param { list.add($param.node); })*)
    ;

param returns [RppParam node]
    : ^(RPP_PARAM id=. t=. {$node = new RppParam($id.getText(), new RppType($t.getText()));})
    ;

type returns [RppType node]
    : ^(RPP_TYPE t=. { node = new RppType($t.getText()); })
    ;

expr returns [RppExpr node]
    : ^(RPP_EXPR expression) { node = $expression.node; }
    ;

expression returns [RppExpr node]
    : ^('+' a=expression b=expression)  { node = new BinOp("+", $a.node, $b.node); }
    | ^('-' a=expression b=expression)  { node = new BinOp("-", $a.node, $b.node); }
    | IntegerLiteral { node = new RppInteger($IntegerLiteral.text); }
    ;