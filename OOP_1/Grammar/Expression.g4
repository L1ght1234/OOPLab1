grammar Expression;

parse: expression EOF;

expression
    : additiveExpr ( (EQ | NEQ | LT | GT | LTE | GTE) additiveExpr )?
    ;

additiveExpr
    : multiplicativeExpr ( (ADD | SUB) multiplicativeExpr )*
    ;

multiplicativeExpr
    : unaryExpr ( (MUL | DIV | MOD | DIV_INT) unaryExpr )*
    ;

unaryExpr
    : (ADD | SUB)? atom
    ;

atom
    : functionCall              // mmax(...)
    | CELL_REF                  // A1, B23
    | NUMBER                    // 123
    | '(' expression ')'        // (5 + A1)
    ;

functionCall
    : (MMAX | MMIN) '(' expression (',' expression)* ')'
    ;

ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';

MOD:     'mod';
DIV_INT: 'div';

MMAX: 'mmax';
MMIN: 'mmin';

EQ: '=';
LT: '<';
GT: '>';

LTE: '<=';
GTE: '>=';
NEQ: '<>'; // '<>' - це "не дорівнює"

LPAREN: '(';
RPAREN: ')';
COMMA:  ',';

CELL_REF: [A-Za-z]+ [0-9]+;

NUMBER: [0-9]+;

WS: [ \t\r\n]+ -> skip;