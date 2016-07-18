// IBM Enterprise Cobol 5.1 for zOS

// -----------------------------------------------------------------------
// Grammar used by the preprocessor to parse the Cobol compiler directives
// -----------------------------------------------------------------------

grammar CobolCompilerDirectives;

options { superClass=TypeCobol.Compiler.AntlrUtils.LineAwareParser; }

import CobolWords;

// A typical COBOL compiler has a pre-step where comments and compiler directing 
// statements are processed to generate a source text suitable for subsequent parsing. 
// The "text manipulation" phase drops comment lines, processes continuation lines, 
// processes COPY directives and builds compiler directives to guide subsequent compilation.

// **************
// Step 4 : Compiler-directing statements

// p57: Compiler-directing statements
// Most compiler-directing statements, including COPY and REPLACE, can start in
// either Area A or Area B.
// BASIS, CBL (PROCESS), *CBL (*CONTROL), DELETE, EJECT, INSERT, SKIP1,
// SKIP2, SKIP3, and TITLE statements can also start in Area A or Area B.

// p527: A compiler-directing statement is a statement that causes the compiler to take a
// specific action during compilation.
// You can use compiler-directing statements for the following purposes:
// - Extended source library control (BASIS, DELETE, and INSERT statements)
// - Source text manipulation (COPY and REPLACE statements)
// - Exception handling (USE statement)
// - Controlling compiler listings (*CONTROL, *CBL, EJECT, TITLE, SKIP1, SKIP2,
//   and SKIP3 statements)
// - Specifying compiler options (CBL and PROCESS statements)
// - Specifying COBOL exception handling procedures (USE statements)
// The SERVICE LABEL statement is used with Language Environment condition
// handling. It is also generated by the CICS integrated translator (and the separate
// CICS translator).
// The following compiler directing statements have no effect: ENTER, READY or
// RESET TRACE, and SERVICE RELOAD.

compilerDirectingStatement:
                              basisCompilerStatement |
                              cblProcessCompilerStatement |
                              controlCblCompilerStatement |
                              copyCompilerStatement |
                              deleteCompilerStatement |
                              ejectCompilerStatement |
                              enterCompilerStatement |
                              execSqlIncludeStatement |
                              insertCompilerStatement |
                              readyOrResetTraceCompilerStatement |
                              replaceCompilerStatement |
                              serviceLabelCompilerStatement |
                              serviceReloadCompilerStatement |
                              skipCompilerStatement |
                              titleCompilerStatement;

// p527: BASIS statement 
// The BASIS statement is an extended source text library statement. It provides a
// complete COBOL program as the source for a compilation.
// A complete program can be stored as an entry in a user-defined library and can be
// used as the source for a compilation. Compiler input is a BASIS statement,
// optionally followed by any number of INSERT and DELETE statements.
// sequence-number
// Can optionally appear in columns 1 through 6, followed by a space. The
// content of this field is ignored.
// BASIS
// >>> Can appear anywhere in columns 1 through 72, followed by basis-name.
// There must be no other text in the statement.
// basis-name, literal-1
// Is the name by which the library entry is known to the system
// environment.
// For rules of formation and processing rules, see the description under
// literal-1 and text-name of the �COPY statement� on page 530.
// The source file remains unchanged after execution of the BASIS statement.
// Usage note: If INSERT or DELETE statements are used to modify the COBOL
// source text provided by a BASIS statement, the sequence field of the COBOL
// source text must contain numeric sequence numbers in ascending order.

basisCompilerStatement: 
                  /* sequenceNumber? */ BASIS textName;

// Same as textName ... "For rules of formation and processing rules, see the description under literal-1 and text-name of the �COPY statement�")
//basisName : UserDefinedWord | AlphanumericLiteral;

// p528: CBL (PROCESS) statement                            
// With the CBL (PROCESS) statement, you can specify compiler options to be used
// in the compilation of the program. The CBL (PROCESS) statement is placed before
// the IDENTIFICATION DIVISION header of an outermost program.
// options-list
// A series of one or more compiler options, each one separated by a comma
// or a space.
// For more information about compiler options, see Compiler options in the
// Enterprise COBOL Programming Guide.
// >>> The CBL (PROCESS) statement can be preceded by a sequence number in columns
// >>> 1 through 6. The first character of the sequence number must be numeric, and CBL
// >>> or PROCESS can begin in column 8 or after; if a sequence number is not specified,
// >>> CBL or PROCESS can begin in column 1 or after.
// >>> The CBL (PROCESS) statement must end before or at column 72, and options
// >>> cannot be continued across multiple CBL (PROCESS) statements. 
// However, you
// can use more than one CBL (PROCESS) statement. Multiple CBL (PROCESS)
// statements must follow one another with no intervening statements of any other
// type.
// >>> The CBL (PROCESS) statement must be placed before any comment lines or other
// >>> compiler-directing statements.

// ---------------------------------------
// !!!! NB : because the compiler options set by this compiler directive 
// !!!!      can have an impact on the lexical analysis, the CBL and
// !!!!      PROCESS compiler directives are analyzed by the Scanner
// !!!!      => the parser directly receives a CompilerDirectiveToken
// !!!:         and never needs to execute the rules below

cblProcessCompilerStatement:
                       (CBL | PROCESS) optionsList?;
  
optionsList: compilerOption (CommaSeparator? compilerOption)*;

// Cobol Programming Guide p299 : Compiler options
// Samples :
// NOADATA
// AFP(VOLATILE)
// ARCH(6) | CODEPAGE(1140)
// BUFSIZE(1K)
// CICS(�string2�) | CICS("string3")
// CURRENCY(AlphanumericLiteral | HexadecimalAlphanumericLiteral)
// EXIT( INEXIT([�str1�,]mod1) LIBEXIT([�str2�,]mod2) )
// FLAG(I,I)
// FLAGSTD(x[yy][,0])

// NB : this rule is matched by the Scanner
// => the approximate grammar below is never used by the directive parser
compilerOption: name=UserDefinedWord (LeftParenthesisSeparator ~RightParenthesisSeparator* RightParenthesisSeparator)?;

// -------------------------------------------

// p528: *CONTROL (*CBL) statement
// With the *CONTROL (or *CBL) statement, you can selectively display or suppress
// the listing of source code, object code, and storage maps throughout the source
// text.                            
// For a complete discussion of the output produced by these options, see Getting
// listings in the Enterprise COBOL Programming Guide.
// The *CONTROL and *CBL statements are synonymous. *CONTROL is accepted
// anywhere that *CBL is accepted.
// >>> The characters *CONTROL or *CBL can start in any column beginning with
// >>> column 7, followed by at least one space or comma and one or more option
// >>> keywords. The option keywords must be separated by one or more spaces or
// >>> commas. This statement must be the only statement on the line, and continuation
// >>> is not allowed. The statement can be terminated with a period.
// The *CONTROL and *CBL statements must be embedded in a program source. For
// example, in the case of batch applications, the *CONTROL and *CBL statements
// must be placed between the PROCESS (CBL) statement and the end of the
// program (or END PROGRAM marker, if specified).
// The source line containing the *CONTROL (*CBL) statement will not appear in the
// source listing.
// If an option is defined at installation as a fixed option, that fixed option takes
// precedence over all of the following parameter and statements:
// - PARM (if available)
// - CBL statement
// - *CONTROL (*CBL) statement
// The requested options are handled in the following manner:
// 1. If an option or its negation appears more than once in a *CONTROL statement,
// the last occurrence of the option word is used.
// 2. If the corresponding option has been requested as a parameter to the compiler,
// then a *CONTROL statement with the negation of the option word must
// precede the portions of the source text for which listing output is to be
// inhibited. Listing output then resumes when a *CONTROL statement with the
// affirmative option word is encountered.
// 3. If the negation of the corresponding option has been requested as a parameter
// to the compiler, then that listing is always inhibited.
// 4. The *CONTROL statement is in effect only within the source program in which
// it is written, including any contained programs. It does not remain in effect
// across batch compiles of two or more COBOL source programs.
// Source code listing
// The topic lists statements that control the listing of the input source text lines.
// The statement can be any of the following one:
// *CONTROL SOURCE [*CBL SOURCE]
// *CONTROL NOSOURCE [*CBL NOSOURCE]
// If a *CONTROL NOSOURCE statement is encountered and SOURCE has been
// requested as a compilation option, printing of the source listing is suppressed from
// this point on. An informational (I-level) message is issued stating that printing of
// the source has been suppressed.
// Object code listing
// The topic lists statements that control the listing of generated object code in the
// PROCEDURE DIVISION.
// The statement can be any of the following one:
// *CONTROL LIST [*CBL LIST]
// *CONTROL NOLIST [*CBL NOLIST]
// If a *CONTROL NOLIST statement is encountered, and LIST has been requested as
// a compilation option, listing of generated object code is suppressed from this point
// on.
// Storage map listing
// The topic lists statements that control the listing of storage map entries occurring
// in the DATA DIVISION.
// The statement can be any of the following one:
// *CONTROL MAP [*CBL MAP]
// *CONTROL NOMAP [*CBL NOMAP]
// If a *CONTROL NOMAP statement is encountered, and MAP has been requested
// as a compilation option, listing of storage map entries is suppressed from this
// point on.
// For example, either of the following sets of statements produces a storage map
// listing in which A and B will not appear:
// *CONTROL NOMAP *CBL NOMAP
// 01 A 01 A
// 02 B 02 B
// *CONTROL MAP *CBL MAP

controlCblCompilerStatement:
                       (ASTERISK_CONTROL | ASTERISK_CBL) 
                       //(SOURCE | NOSOURCE | LIST | NOLIST | MAP | NOMAP)+ 
                       controlCblOption+
                       PeriodSeparator?;

// p530: COPY statement                              
// The COPY statement is a library statement that places prewritten text in a COBOL
// compilation unit.
// Prewritten source code entries can be included in a compilation unit at compile
// time. Thus, an installation can use standard file descriptions, record descriptions,
// or procedures without recoding them. These entries and procedures can then be
// saved in user-created libraries; they can then be included in programs and class
// definitions by means of the COPY statement.
// Compilation of the source code containing COPY statements is logically equivalent
// to processing all COPY statements before processing the resulting source text.
// The effect of processing a COPY statement is that the library text associated with
// text-name is copied into the compilation unit, logically replacing the entire COPY
// statement, beginning with the word COPY and ending with the period, inclusive.
// When the REPLACING phrase is not specified, the library text is copied
// unchanged.      
// * text-name , library-name
// text-name identifies the copy text. library-name identifies where the copy text
// exists.
// - Can be from 1-30 characters in length
// - Can contain the following characters: Latin uppercase letters A-Z, Latin
//   lowercase letters a-z, digits 0-9, and hyphen
// - The first or last character must not be a hyphen
// - Cannot contain an underscore
// Neither text-name nor library-name need to be unique within a program.
// They can be identical to other user-defined words in the program.
// text-name need not be qualified. If text-name is not qualified, a library-name
// of SYSLIB is assumed.
// When compiling from JCL or TSO, only the first eight characters are used
// as the identifying name. When compiling with the cob2 command and
// processing COPY text residing in the z/OS UNIX file system, all characters
// are significant.
// * literal-1 , literal-2
// Must be alphanumeric literals. literal-1 identifies the copy text. literal-2
// identifies where the copy text exists.
// When compiling from JCL or TSO:
// - Literals can be from 1-30 characters in length.
// - Literals can contain characters: A-Z, a-z, 0-9, hyphen, @, #, or $.
// - The first or last character must not be a hyphen.
// - Literals cannot contain an underscore.
// - Only the first eight characters are used as the identifying name.
// When compiling with the cob2 command and processing COPY text
// residing in the z/OS UNIX file system, the literal can be from 1 to 160
// characters in length.
// The uniqueness of text-name and library-name is determined after the formation and
// conversion rules for a system-dependent name have been applied.
// For information about the mapping of characters in the text-name, library-name, and
// literals, see Compiler-directing statements in the Enterprise COBOL Programming
// Guide.

// p66: References to COPY libraries
// If library-name-1 is not specified, SYSLIB is assumed as the library name.
// For rules on referencing COPY libraries, see �COPY statement� on page 530.

// * operand-1, operand-2
// Can be either pseudo-text, an identifier, a function-identifier, a literal, or a
// COBOL word (except the word COPY). For details, see �REPLACING
// phrase� on page 533.
// Library text and pseudo-text can consist of or include any words (except
// COPY), identifiers, or literals that can be written in the source text. This
// includes DBCS user-defined words, DBCS literals, and national literals.
// DBCS user-defined words must be wholly formed; that is, there is no
// partial-word replacement for DBCS words.
// Words or literals containing DBCS characters cannot be continued across
// lines.
// >>> Each COPY statement must be preceded by a space and ended with a separator
// >>> period.
// >>> A COPY statement can appear in the source text anywhere a character string or a
// >>> separator can appear.
// COPY statements can be nested. However, nested COPY statements cannot contain
// the REPLACING phrase, and a COPY statement with the REPLACING phrase
// cannot contain nested COPY statements.
// A nested COPY statement cannot cause recursion. That is, a COPY member can be
// named only once in a set of nested COPY statements until the end-of-file for that
// COPY member is reached. For example, assume that the source text contains the
// statement: COPY X. and library text X contains the statement: COPY Y..
// In this case, library text Y must not have a COPY X or a COPY Y statement.
// Debugging lines are permitted within library text and pseudo-text. Text words
// within a debugging line participate in the matching rules as if the "D" did not
// appear in the indicator area. A debugging line is specified within pseudo-text if the
// debugging line begins in the source text after the opening pseudo-text delimiter
// but before the matching closing pseudo-text delimiter.
// If additional lines are introduced into the source text as a result of a COPY
// statement, each text word introduced appears on a debugging line if the COPY
// statement begins on a debugging line or if the text word being introduced appears
// on a debugging line in library text. When a text word specified in the BY phrase is
// introduced, it appears on a debugging line if the first library text word being
// replaced is specified on a debugging line.
// When a COPY statement is specified on a debugging line, the copied text is treated
// as though it appeared on a debugging line, except that comment lines in the text
// appear as comment lines in the resulting source text.
// If the word COPY appears in a comment-entry, or in the place where a
// comment-entry can appear, it is considered part of the comment-entry.
// After all COPY and REPLACE statements have been processed, a debugging line
// will be considered to have all the characteristics of a comment line, if the WITH
// DEBUGGING MODE clause is not specified in the SOURCE-COMPUTER
// paragraph.
// Comment lines, inline comments, or blank lines can occur in library text. Comment
// lines, inline comments, or blank lines appearing in library text are copied into the
// resultant source text unchanged with the following exception: a comment line, an
// inline comment, or blank line in library text is not copied if that comment line,
// inline comment, or blank line appears within the sequence of text words that
// match operand-1 (see �Replacement and comparison rules� on page 534).
// Lines containing *CONTROL (*CBL), EJECT, SKIP1, SKIP2, SKIP3, or TITLE
// statements can occur in library text. Such lines are treated as comment lines during
// COPY statement processing.
// The syntactic correctness of the entire COBOL source text cannot be determined
// until all COPY and REPLACE statements have been completely processed, because
// the syntactic correctness of the library text cannot be independently determined.
// Library text copied from the library is placed into the same area of the resultant
// program as it is in the library. Library text must conform to the rules for Standard
// COBOL 85 format.
// Note: Characters outside those defined for COBOL words and separators must not
// appear in library text or pseudo-text except in comment lines, inline comments,
// comment-entries, alphanumeric literals, DBCS literals, or national literals.

// SUPPRESS phrase
// The SUPPRESS phrase specifies that the library text is not to be printed on the
// source listing.

// REPLACING phrase
// When the REPLACING phrase is specified, the library text is copied, and each
// properly matched occurrence of operand-1 within the library text is replaced by the
// associated operand-2.
// In the discussion that follows, each operand can consist of one of the following
// items:
// - Pseudo-text
// - An identifier
// - A literal
// - A COBOL word (except the word COPY)
// - Function identifier
// * pseudo-text
// A sequence of character-strings or separators, or both, bounded by, but not
// including, pseudo-text delimiters (==). Both characters of each pseudo-text
// delimiter must appear on one line; however, character-strings within
// pseudo-text can be continued.
// Individual character-strings within pseudo-text can be up to 322 characters
// long; they can be continued subject to the normal continuation rules for
// source code format.
// Keep in mind that a character-string must be delimited by separators. For
// more information, see Chapter 1, �Characters,� on page 3.
// pseudo-text-1 refers to pseudo-text when used for operand-1, and
// pseudo-text-2 refers to pseudo-text when used for operand-2.
// pseudo-text-1 can consist solely of the separator comma or separator
// semicolon. pseudo-text-2 can be null; it can consist solely of space
// characters, comment lines or inline comments.
// Pseudo-text must not contain the word COPY.
// Each text word in pseudo-text-2 that is to be copied into the program is
// placed in the same area of the resultant program as the area in which it
// appears in pseudo-text-2.
// Pseudo-text can consist of or include any words (except COPY), identifiers,
// or literals that can be written in the source text. This includes DBCS
// user-defined words, DBCS literals, and national literals.
// DBCS user-defined words must be wholly formed; that is, there is no
// partial-word replacement for DBCS words.
// Words or literals containing DBCS characters cannot be continued across
// lines.
// * identifier
// Can be defined in any section of the DATA DIVISION.
// * literal
// Can be numeric, alphanumeric, DBCS, or national.
// * word 
// Can be any single COBOL word (except COPY), including DBCS
// user-defined words. DBCS user-defined words must be wholly formed.
// You cannot replace part of a DBCS word.
// You can include the nonseparator COBOL characters (for example, +, *, /,
// $, <, >, and =) as part of a COBOL word when used as REPLACING
// operands. In addition, a hyphen or underscore can be at the beginning of
// the word or a hyphen can be at the end of the word.
// For purposes of matching, each identifier-1, literal-1, or word-1 is treated as
// pseudo-text containing only identifier-1, literal-1, or word-1, respectively.

// ... more details on Replacement and comparison rules p534 -> p537 ...

copyCompilerStatement:
                         COPY copyCompilerStatementBody PeriodSeparator;
             
copyCompilerStatementBody:
                             qualifiedTextName
                             SUPPRESS?
                             (REPLACING (copyReplacingOperand BY copyReplacingOperand)+)?;

copyReplacingOperand:
                        pseudoText | 
                        literalOrUserDefinedWordOReservedWordExceptCopy;

pseudoText:
              PseudoTextDelimiter /* any kind of token except PseudoTextDelimiter and the word COPY */ pseudoTextTokens+= ~(PseudoTextDelimiter | COPY)* PseudoTextDelimiter;

// p537: DELETE statement
// The DELETE statement is an extended source library statement. It removes COBOL
// statements from a source program that was included by a BASIS statement.
// sequence-number
// Can optionally appear in columns 1 through 6, followed by a space. The
// content of this field is ignored.
// DELETE
// >>> Can appear anywhere within columns 1 through 72. The keyword DELETE
// >>> must be followed by a space and the sequence-number-field. There must be
// >>> no other text in the statement.
// sequence-number-field
// Each number must be equal to a sequence-number in the BASIS source
// program. This sequence-number is the six-digit number the programmer
// assigns in columns 1 through 6 of the COBOL coding form. The numbers
// referenced in the sequence-number-field of INSERT or DELETE statements
// must always be specified in ascending numeric order.
// The sequence-number-field must be one of the following options:
// - A single number
// - A series of single numbers
// - A range of numbers (indicated by separating the two bounding numbers
//   of the range by a hyphen)
// - A series of ranges of numbers
// - Any combination of one or more single numbers and one or more
//   ranges of numbers
// Each entry in the sequence-number-field must be separated from the
// preceding entry by a comma followed by a space. For example:
// 000250 DELETE 000010-000050, 000400, 000450
// Source program statements can follow a DELETE statement. These source program
// statements are then inserted into the BASIS source program before the statement
// following the last statement deleted (that is, in the example above, before the next
// statement following deleted statement 000450).
// >>> If a DELETE statement begins in column 12 or higher and a valid
// >>> sequence-number-field does not follow the keyword DELETE, the compiler assumes
// >>> that this DELETE statement is a COBOL DELETE statement.
// Usage note: If INSERT or DELETE statements are used to modify the COBOL
// source program provided by a BASIS statement, the sequence field of the COBOL
// source program must contain numeric sequence numbers in ascending order. The
// source file remains unchanged. Any INSERT or DELETE statements referring to
// these sequence numbers must occur in ascending order.

deleteCompilerStatement:
                   /* sequenceNumber? */ DELETE_CD sequenceNumberField;

// Implementation detail 1 : CommaSeparator is considered as whitespace by the parser
// so, even if it is required in the spec, we won't be able to use it here
// Implementation detail 2 : 0000001-0000099 is recognized as IntegerLiteral IntegerLiteral
// by the scanner (two tokens, first one >0, second one <0), that's why we don't use MinusOperator
// Implementation detail 3 : the spec constraint below
// Each entry in the sequence-number-field must be separated from the preceding entry 
// by a comma >>> followed by a space <<<.
sequenceNumberField:
	IntegerLiteral IntegerLiteral*;

// p539: EJECT statement 
// The EJECT statement specifies that the next source statement is to be printed at the
// top of the next page.
// >>> The EJECT statement must be the only statement on the line. It can be written in
// >>> either Area A or Area B, and can be terminated with a separator period.
// The EJECT statement must be embedded in a program source. For example, in the
// case of batch applications, the EJECT statement must be placed between the CBL
// (PROCESS) statement and the end of the program (or the END PROGRAM marker,
// if specified).
// The EJECT statement has no effect on the compilation of the source unit itself.

ejectCompilerStatement:
                          ({IsNextTokenOnTheSameLine()}? EJECT PeriodSeparator?) |
						  (EJECT);

// p539: ENTER statement
// The ENTER statement is designed to facilitate the use of more than one source
// language in the same source program. However, only COBOL is allowed in the
// source program.
// The ENTER statement is syntax checked but has no effect on the execution of the
// program.
// language-name-1
// A system name that has no defined meaning. It must be either a correctly
// formed user-defined word or the word "COBOL." At least one character
// must be alphabetic.
// routine-name-1
// Must follow the rules for formation of a user-defined word. At least one
// character must be alphabetic.

enterCompilerStatement:
                          ENTER languageName routineName? PeriodSeparator;

languageName: UserDefinedWord;
routineName: UserDefinedWord;

// p424: An SQL INCLUDE statement is treated identically to a native COBOL COPY statement
// when you use the SQL compiler option.
// The following two lines are therefore treated the same way. (The period that ends
// the EXEC SQL INCLUDE statement is required.)
// EXEC SQL INCLUDE name END-EXEC.
// COPY "name".
// The processing of the name in an SQL INCLUDE statement follows the same rules as
// those of the literal in a COPY literal-1 statement that does not have a REPLACING
// phrase.
// The library search order for SQL INCLUDE statements is the same SYSLIB
// concatenation as the compiler uses to resolve COBOL COPY statements that do not
// specify a library-name.

// p431: Precompiler: The DB2 precompiler does not require that a period end each EXEC
// SQL INCLUDE statement. If a period is specified, the precompiler processes it as part
// of the statement. If a period is not specified, the precompiler accepts the statement
// as if a period had been specified.
// Coprocessor: The DB2 coprocessor treats each EXEC SQL INCLUDE statement like a
// COPY statement, and requires that a period end the statement. For example:
// IF A = B THEN
// EXEC SQL INCLUDE some_code_here END-EXEC.
// ELSE
// . . .
// END-IF
// Note that the period does not terminate the IF statement.

// p431: Precompiler: With the DB2 precompiler, an EXEC SQL INCLUDE statement can
// reference a copybook that contains a COPY statement that uses the REPLACING
// phrase.
// Coprocessor: With the DB2 coprocessor, an EXEC SQL INCLUDE statement cannot
// reference a copybook that contains a COPY statement that uses the REPLACING
// phrase. The coprocessor processes each EXEC SQL INCLUDE statement identically to a
// COPY statement, and nested COPY statements cannot have the REPLACING phrase.

// p424: Code an EXEC SQL INCLUDE statement to include an SQL communication area
// (SQLCA) in the WORKING-STORAGE SECTION or LOCAL-STORAGE SECTION of the
// outermost program. LOCAL-STORAGE is recommended for recursive programs and
// programs that use the THREAD compiler option.

execSqlIncludeStatement:
                          (EXEC | EXECUTE) ExecTranslatorName
                          EXEC_SQL_INCLUDE 
                          copyCompilerStatementBody
                          END_EXEC PeriodSeparator;

// p540: INSERT statement
// The INSERT statement is a library statement that adds COBOL statements to a
// source program that was included by a BASIS statement.
// sequence-number
// Can optionally appear in columns 1 through 6, followed by a space. The
// content of this field is ignored.
// INSERT
// >>> Can appear anywhere within columns 1 through 72, followed by a space
// >>> and the sequence-number-field. There must be no other text in the statement.
// sequence-number-field
// A number that must be equal to a sequence-number in the BASIS source
// program. This sequence-number is a six-digit number that the programmer
// assigns in columns 1 through 6 of the COBOL source line.
// The numbers referenced in the sequence-number-field of INSERT or DELETE
// statements must always be specified in ascending numeric order.
// The sequence-number-field must be a single number (for example, 000130). At
// least one new source program statement must follow the INSERT
// statement for insertion after the statement number specified by the
// sequence-number-field.
// New source program statements following the INSERT statement can include any
// COBOL syntax.
// Usage note: If INSERT or DELETE statements are used to modify the COBOL
// source program provided by a BASIS statement, the sequence field of the COBOL
// source program must contain numeric sequence numbers in ascending order. The
// source file remains unchanged. Any INSERT or DELETE statements referring to
// these sequence numbers must occur in ascending order.

insertCompilerStatement:
                   /* sequenceNumber? */ INSERT sequenceNumber;

// A number that must be equal to a sequence-number in the BASIS source
// program. This sequence-number is a six-digit number that the programmer
// assigns in columns 1 through 6 of the COBOL source line.

sequenceNumber: IntegerLiteral;

// p540: READY or RESET TRACE statement
// The READY or RESET TRACE statement was designed to trace the execution of
// procedures. The READY or RESET TRACE statement can appear only in the
// PROCEDURE DIVISION, but has no effect on your program.
// You can trace the execution of procedures by using the USE FOR DEBUGGING
// declarative as described in Example: USE FOR DEBUGGING in the Enterprise
// COBOL Programming Guide.

readyOrResetTraceCompilerStatement:
                                      (READY | RESET) TRACE PeriodSeparator;

// p541: REPLACE statement
// The REPLACE statement is used to replace source text.
// >>> A REPLACE statement can occur anywhere in the source text that a
// >>> character-string can occur. It must be preceded by a separator period except when
// >>> it is the first statement in a separately compiled program. It must be terminated by
// >>> a separator period.
// The REPLACE statement provides a means of applying a change to an entire
// COBOL compilation group, or part of a compilation group, without manually
// having to find and modify all places that need to be changed. It is an easy method
// of doing simple string substitutions. It is similar in action to the REPLACING
// phrase of the COPY statement, except that it acts on the entire source text, not just
// on the text in COPY libraries.
// >>> If the word REPLACE appears in a comment-entry or in the place where a
// >>> comment-entry can appear, it is considered part of the comment-entry.
// Format 1 : Each matched occurrence of pseudo-text-1 in the source text is replaced by the
// corresponding pseudo-text-2.
// Format 2 : Any text replacement currently in effect is discontinued with the format-2 form of
// REPLACE. If format 2 is not specified, a given occurrence of the REPLACE
// statement is in effect from the point at which it is specified until the next
// occurrence of a REPLACE statement or the end of the separately compiled
// program.
// >>> The compiler processes REPLACE statements in source text after the processing of
// >>> any COPY statements. COPY must be processed first, to assemble complete source
// >>> text. Then REPLACE can be used to modify that source text, performing simple
// >>> string substitution. REPLACE statements cannot themselves contain COPY
// statements.
// The text produced as a result of the processing of a REPLACE statement must not
// contain a REPLACE statement.
// ... more rules on REPLACE behavior p542 -> p544

replaceCompilerStatement:
                            REPLACE ((pseudoText BY pseudoText)+ | OFF) PeriodSeparator;

// p544: SERVICE LABEL statement
// This statement is generated by the CICS integrated language translator (and the separate CICS translator) 
// to indicate control flow. It is also used after calls to CEE3SRP when using Language Environment condition handling. 
// For more information about CEE3SRP, see the Language Environment Programming Guide.
// The SERVICE LABEL statement can appear only in the PROCEDURE DIVISION, but not in the declaratives section.        

serviceLabelCompilerStatement:
                                 SERVICE LABEL;

// p545: SERVICE RELOAD statement
// The SERVICE RELOAD statement is syntax checked, but has no effect on the execution of the program.

serviceReloadCompilerStatement:
                                  SERVICE RELOAD UserDefinedWord;

// p545: SKIP statements
// The SKIP1, SKIP2, and SKIP3 statements specify blank lines that the compiler should add when printing the source listing. 
// SKIP statements have no effect on the compilation of the source text itself.
// SKIP1
// Specifies a single blank line to be inserted in the source listing.
// SKIP2
// Specifies two blank lines to be inserted in the source listing.
// SKIP3
// Specifies three blank lines to be inserted in the source listing.
// SKIP1, SKIP2, or SKIP3 can be written anywhere in either Area A or Area B, and can be terminated with a separator period. 
// It must be the only statement on the line.
// The SKIP statements must be embedded in a program source. 
// For example, in the case of batch applications, a SKIP1, SKIP2, or SKIP3 statement must be placed between the CBL (PROCESS) statement and the end of the program or class (or the END CLASS marker or END PROGRAM marker, if specified).

skipCompilerStatement:
                         ({IsNextTokenOnTheSameLine()}? (SKIP1 | SKIP2 | SKIP3) PeriodSeparator?) |
						 (SKIP1 | SKIP2 | SKIP3);				

// p545: TITLE statement
// The TITLE statement specifies a title to be printed at the top of each page of the source listing produced during compilation.
// If no TITLE statement is found, a title containing the identification of the compiler and the current release level is generated. 
// The title is left-justified on the title line.
// literal
// Must be an alphanumeric literal, DBCS literal, or national literal and can be followed by a separator period. Must not be a figurative constant.
//In addition to the default or chosen title, the right side of the title line contains the following items: 
// - For programs, the name of the program from the PROGRAM-ID paragraph for the outermost program. (This space is blank on pages preceding the PROGRAM-ID paragraph for the outermost program.) 
// - For classes, the name of the class from the CLASS-ID paragraph. 
// - Current page number. 
// - Date and time of compilation.
// The TITLE statement: 
// - Forces a new page immediately, if the SOURCE compiler option is in effect 
// - Is not itself printed on the source listing 
// - Has no other effect on compilation 
// - Has no effect on program execution 
// - Cannot be continued on another line 
// - Can appear anywhere in any of the divisions
// A title line is produced for each page in the listing produced by the LIST option. This title line uses the last TITLE statement found in the source statements or the default.
// The word TITLE can begin in either Area A or Area B.
// The TITLE statement must be embedded in a class or program source. For example, in the case of batch applications, the TITLE statement must be placed between the CBL (PROCESS) statement and the end of the class or program (or the END CLASS marker or END PROGRAM marker, if specified).
// No other statement can appear on the same line as the TITLE statement.

titleCompilerStatement:
                          ({IsNextTokenOnTheSameLine()}? TITLE alphanumericValue2 PeriodSeparator?) |
						  (TITLE alphanumericValue2);
						 
// p546: USE statement
// -> see the DECLARATIVES section in CobolCodeElements.g4

