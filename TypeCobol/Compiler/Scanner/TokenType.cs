﻿using System.Linq;

namespace TypeCobol.Compiler.Scanner
{
    // WARNING : both enumerations below (families / types) must stay in sync
    // WARNING : make sure to update the tables in TokenUtils if you add one more token family or one more token type

    public enum TokenFamily
    {
        //          0 : Error
        Invalid=0,
        //   1 ->   3 : Whitespace
        // p46: The separator comma and separator semicolon can
        // be used anywhere the separator space is used.
        Whitespace=1,
        //   4 ->   5 : Comments
        Comments=4,
        // 6 ->  11 : Separators - Syntax
        SyntaxSeparator=6,
        //  12 ->  16 : Special character word - Arithmetic operators
        ArithmeticOperator=12,
        //  17 ->  21 : Special character word - Relational operators
        RelationalOperator=17,
        //  22 ->  27 : Literals - Alphanumeric
        AlphanumericLiteral=22,
        //  28 ->  31 : Literals - Numeric 
        NumericLiteral=28,
        //  32 ->  34 : Literals - Syntax tokens
        SyntaxLiteral=32,
        //  35 ->  39 : Symbols
        Symbol=35,
        //  40 ->  58 : Keywords - Compiler directive starting tokens
        CompilerDirectiveStartingKeyword=40,
        //  59 ->  153 : Keywords - Code element starting tokens
        CodeElementStartingKeyword=59,
        // 154 -> 184 : Keywords - Special registers
        SpecialRegisterKeyword=154,
        // 185 -> 198 : Keywords - Figurative constants
        FigurativeConstantKeyword=185, 
        // 199 -> 200 : Keywords - Special object identifiers
        SpecialObjetIdentifierKeyword=199,
        // 201 -> 494 : Keywords - Syntax tokens
        SyntaxKeyword=201,
        // 495 -> 497 : Keywors - Cobol V6
        CobolV6Keyword = 495,
        // 498 -> 499 : Keywords - Cobol 2002
        Cobol2002Keyword = 498,
        // 500 -> 505 : Keywords - TypeCobol
        TypeCobolKeyword = 500,

        // 506-> 506 : Operators - TypeCobol
        TypeCobolOperators= 506, 

        // 507 -> 509 : Compiler directives
        CompilerDirective = 507,
        // 510 -> 510 : Internal token groups - used by the preprocessor only
        InternalTokenGroup = 510
    }

    // INFO : the list below is generated from the file Documentation/Studies/CobolLexer.tokens.xls
    // WARNING : the list of tokens in CobolWords.g4 must stay in sync

    public enum TokenType
    {
        EndOfFile = -1,
        InvalidToken = 0,
        SpaceSeparator = 1,
        CommaSeparator = 2,
        SemicolonSeparator = 3,
        FloatingComment = 4,
        CommentLine = 5,
        PeriodSeparator = 6,
        ColonSeparator = 7,
        QualifiedNameSeparator = 8,
        LeftParenthesisSeparator = 9,
        RightParenthesisSeparator = 10,
        PseudoTextDelimiter = 11,
        PlusOperator = 12,
        MinusOperator = 13,
        DivideOperator = 14,
        MultiplyOperator = 15,
        PowerOperator = 16,
        LessThanOperator = 17,
        GreaterThanOperator = 18,
        LessThanOrEqualOperator = 19,
        GreaterThanOrEqualOperator = 20,
        EqualOperator = 21,
        AlphanumericLiteral = 22,
        HexadecimalAlphanumericLiteral = 23,
        NullTerminatedAlphanumericLiteral = 24,
        NationalLiteral = 25,
        HexadecimalNationalLiteral = 26,
        DBCSLiteral = 27,
        LevelNumber = 28,
        IntegerLiteral = 29,
        DecimalLiteral = 30,
        FloatingPointLiteral = 31,
        PictureCharacterString = 32,
        CommentEntry = 33,
        ExecStatementText = 34,
        SectionParagraphName = 35,
        IntrinsicFunctionName = 36,
        ExecTranslatorName = 37,
        PartialCobolWord = 38,
        UserDefinedWord = 39,
        ASTERISK_CBL = 40,
        ASTERISK_CONTROL = 41,
        BASIS = 42,
        CBL = 43,
        COPY = 44,
        DELETE_CD = 45,
        EJECT = 46,
        ENTER = 47,
        EXEC_SQL = 48,
        INSERT = 49,
        PROCESS = 50,
        READY = 51,
        RESET = 52,
        REPLACE = 53,
        SERVICE_CD = 54,
        SKIP1 = 55,
        SKIP2 = 56,
        SKIP3 = 57,
        TITLE = 58,
        ACCEPT = 59,
        ADD = 60,
        ALTER = 61,
        APPLY = 62,
        CALL = 63,
        CANCEL = 64,
        CLOSE = 65,
        COMPUTE = 66,
        CONFIGURATION = 67,
        CONTINUE = 68,
        DATA = 69,
        DECLARATIVES = 70,
        DECLARE = 71,
        DELETE = 72,
        DISPLAY = 73,
        DIVIDE = 74,
        ELSE = 75,
        END = 76,
        END_ADD = 77,
        END_CALL = 78,
        END_COMPUTE = 79,
        END_DECLARE = 80,
        END_DELETE = 81,
        END_DIVIDE = 82,
        END_EVALUATE = 83,
        END_EXEC = 84,
        END_IF = 85,
        END_INVOKE = 86,
        END_MULTIPLY = 87,
        END_PERFORM = 88,
        END_READ = 89,
        END_RETURN = 90,
        END_REWRITE = 91,
        END_SEARCH = 92,
        END_START = 93,
        END_STRING = 94,
        END_SUBTRACT = 95,
        END_UNSTRING = 96,
        END_WRITE = 97,
        END_XML = 98,
        ENTRY = 99,
        ENVIRONMENT = 100,
        EVALUATE = 101,
        EXEC = 102,
        EXECUTE = 103,
        EXIT = 104,
        FD = 105,
        FILE = 106,
        FILE_CONTROL = 107,
        GO = 108,
        GOBACK = 109,
        I_O_CONTROL = 110,
        ID = 111,
        IDENTIFICATION = 112,
        IF = 113,
        INITIALIZE = 114,
        INPUT_OUTPUT = 115,
        INSPECT = 116,
        INVOKE = 117,
        LINKAGE = 118,
        LOCAL_STORAGE = 119,
        MERGE = 120,
        MOVE = 121,
        MULTIPLE = 122,
        MULTIPLY = 123,
        NEXT = 124,
        OBJECT_COMPUTER = 125,
        OPEN = 126,
        PERFORM = 127,
        PROCEDURE = 128,
        READ = 129,
        RELEASE = 130,
        REPOSITORY = 131,
        RERUN = 132,
        RETURN = 133,
        REWRITE = 134,
        SAME = 135,
        SD = 136,
        SEARCH = 137,
        SELECT = 138,
        SERVICE = 139,
        SET = 140,
        SORT = 141,
        SOURCE_COMPUTER = 142,
        SPECIAL_NAMES = 143,
        START = 144,
        STOP = 145,
        STRING = 146,
        SUBTRACT = 147,
        UNSTRING = 148,
        USE = 149,
        WHEN = 150,
        WORKING_STORAGE = 151,
        WRITE = 152,
        XML = 153,
        ADDRESS = 154,
        DEBUG_CONTENTS = 155,
        DEBUG_ITEM = 156,
        DEBUG_LINE = 157,
        DEBUG_NAME = 158,
        DEBUG_SUB_1 = 159,
        DEBUG_SUB_2 = 160,
        DEBUG_SUB_3 = 161,
        JNIENVPTR = 162,
        LENGTH = 163,
        LINAGE_COUNTER = 164,
        RETURN_CODE = 165,
        SHIFT_IN = 166,
        SHIFT_OUT = 167,
        SORT_CONTROL = 168,
        SORT_CORE_SIZE = 169,
        SORT_FILE_SIZE = 170,
        SORT_MESSAGE = 171,
        SORT_MODE_SIZE = 172,
        SORT_RETURN = 173,
        TALLY = 174,
        WHEN_COMPILED = 175,
        XML_CODE = 176,
        XML_EVENT = 177,
        XML_INFORMATION = 178,
        XML_NAMESPACE = 179,
        XML_NAMESPACE_PREFIX = 180,
        XML_NNAMESPACE = 181,
        XML_NNAMESPACE_PREFIX = 182,
        XML_NTEXT = 183,
        XML_TEXT = 184,
        HIGH_VALUE = 185,
        HIGH_VALUES = 186,
        LOW_VALUE = 187,
        LOW_VALUES = 188,
        NULL = 189,
        NULLS = 190,
        QUOTE = 191,
        QUOTES = 192,
        SPACE = 193,
        SPACES = 194,
        ZERO = 195,
        ZEROES = 196,
        ZEROS = 197,
        SymbolicCharacter = 198,
        SELF = 199,
        SUPER = 200,
        ACCESS = 201,
        ADVANCING = 202,
        AFTER = 203,
        ALL = 204,
        ALPHABET = 205,
        ALPHABETIC = 206,
        ALPHABETIC_LOWER = 207,
        ALPHABETIC_UPPER = 208,
        ALPHANUMERIC = 209,
        ALPHANUMERIC_EDITED = 210,
        ALSO = 211,
        ALTERNATE = 212,
        AND = 213,
        ANY = 214,
        ARE = 215,
        AREA = 216,
        AREAS = 217,
        ASCENDING = 218,
        ASSIGN = 219,
        AT = 220,
        AUTHOR = 221,
        BEFORE = 222,
        BEGINNING = 223,
        BINARY = 224,
        BLANK = 225,
        BLOCK = 226,
        BOTTOM = 227,
        BY = 228,
        CHARACTER = 229,
        CHARACTERS = 230,
        CLASS = 231,
        CLASS_ID = 232,
        COBOL = 233,
        CODE = 234,
        CODE_SET = 235,
        COLLATING = 236,
        COM_REG = 237,
        COMMA = 238,
        COMMON = 239,
        COMP = 240,
        COMP_1 = 241,
        COMP_2 = 242,
        COMP_3 = 243,
        COMP_4 = 244,
        COMP_5 = 245,
        COMPUTATIONAL = 246,
        COMPUTATIONAL_1 = 247,
        COMPUTATIONAL_2 = 248,
        COMPUTATIONAL_3 = 249,
        COMPUTATIONAL_4 = 250,
        COMPUTATIONAL_5 = 251,
        CONTAINS = 252,
        CONTENT = 253,
        CONVERTING = 254,
        CORR = 255,
        CORRESPONDING = 256,
        COUNT = 257,
        CURRENCY = 258,
        DATE = 259,
        DATE_COMPILED = 260,
        DATE_WRITTEN = 261,
        DAY = 262,
        DAY_OF_WEEK = 263,
        DBCS = 264,
        DEBUGGING = 265,
        DECIMAL_POINT = 266,
        DELIMITED = 267,
        DELIMITER = 268,
        DEPENDING = 269,
        DESCENDING = 270,
        DISPLAY_1 = 271,
        DIVISION = 272,
        DOWN = 273,
        DUPLICATES = 274,
        DYNAMIC = 275,
        EGCS = 276,
        END_OF_PAGE = 277,
        ENDING = 278,
        EOP = 279,
        EQUAL = 280,
        ERROR = 281,
        EVERY = 282,
        EXCEPTION = 283,
        EXTEND = 284,
        EXTERNAL = 285,
        FACTORY = 286,
        FALSE = 287,
        FILLER = 288,
        FIRST = 289,
        FOOTING = 290,
        FOR = 291,
        FROM = 292,
        FUNCTION = 293,
        FUNCTION_POINTER = 294,
        GENERATE = 295,
        GIVING = 296,
        GLOBAL = 297,
        GREATER = 298,
        GROUP_USAGE = 299,
        I_O = 300,
        IN = 301,
        INDEX = 302,
        INDEXED = 303,
        INHERITS = 304,
        INITIAL = 305,
        INPUT = 306,
        INSTALLATION = 307,
        INTO = 308,
        INVALID = 309,
        IS = 310,
        JUST = 311,
        JUSTIFIED = 312,
        KANJI = 313,
        KEY = 314,
        LABEL = 315,
        LEADING = 316,
        LEFT = 317,
        LESS = 318,
        LINAGE = 319,
        LINE = 320,
        LINES = 321,
        LOCK = 322,
        MEMORY = 323,
        METHOD = 324,
        METHOD_ID = 325,
        MODE = 326,
        MODULES = 327,
        MORE_LABELS = 328,
        NATIONAL = 329,
        NATIONAL_EDITED = 330,
        NATIVE = 331,
        NEGATIVE = 332,
        NEW = 333,
        NO = 334,
        NOT = 335,
        NUMERIC = 336,
        NUMERIC_EDITED = 337,
        OBJECT = 338,
        OCCURS = 339,
        OF = 340,
        OFF = 341,
        OMITTED = 342,
        ON = 343,
        OPTIONAL = 344,
        OR = 345,
        ORDER = 346,
        ORGANIZATION = 347,
        OTHER = 348,
        OUTPUT = 349,
        OVERFLOW = 350,
        OVERRIDE = 351,
        PACKED_DECIMAL = 352,
        PADDING = 353,
        PAGE = 354,
        PASSWORD = 355,
        PIC = 356,
        PICTURE = 357,
        POINTER = 358,
        POSITION = 359,
        POSITIVE = 360,
        PROCEDURE_POINTER = 361,
        PROCEDURES = 362,
        PROCEED = 363,
        PROCESSING = 364,
        PROGRAM = 365,
        PROGRAM_ID = 366,
        RANDOM = 367,
        RECORD = 368,
        RECORDING = 369,
        RECORDS = 370,
        RECURSIVE = 371,
        REDEFINES = 372,
        REEL = 373,
        REFERENCE = 374,
        REFERENCES = 375,
        RELATIVE = 376,
        RELOAD = 377,
        REMAINDER = 378,
        REMOVAL = 379,
        RENAMES = 380,
        REPLACING = 381,
        RESERVE = 382,
        RETURNING = 383,
        REVERSED = 384,
        REWIND = 385,
        RIGHT = 386,
        ROUNDED = 387,
        RUN = 388,
        SECTION = 389,
        SECURITY = 390,
        SEGMENT_LIMIT = 391,
        SENTENCE = 392,
        SEPARATE = 393,
        SEQUENCE = 394,
        SEQUENTIAL = 395,
        SIGN = 396,
        SIZE = 397,
        SORT_MERGE = 398,
        SQL = 399,
        SQLIMS = 400,
        STANDARD = 401,
        STANDARD_1 = 402,
        STANDARD_2 = 403,
        STATUS = 404,
        SUPPRESS = 405,
        SYMBOL = 406,
        SYMBOLIC = 407,
        SYNC = 408,
        SYNCHRONIZED = 409,
        TALLYING = 410,
        TAPE = 411,
        TEST = 412,
        THAN = 413,
        THEN = 414,
        THROUGH = 415,
        THRU = 416,
        TIME = 417,
        TIMES = 418,
        TO = 419,
        TOP = 420,
        TRACE = 421,
        TRAILING = 422,
        TRUE = 423,
        TYPE = 424,
        UNBOUNDED = 425,
        UNIT = 426,
        UNTIL = 427,
        UP = 428,
        UPON = 429,
        USAGE = 430,
        USING = 431,
        VALUE = 432,
        VALUES = 433,
        VARYING = 434,
        WITH = 435,
        WORDS = 436,
        WRITE_ONLY = 437,
        XML_SCHEMA = 438,
        ALLOCATE = 439,
        CD = 440,
        CF = 441,
        CH = 442,
        CLOCK_UNITS = 443,
        COLUMN = 444,
        COMMUNICATION = 445,
        CONTROL = 446,
        CONTROLS = 447,
        DE = 448,
        DEFAULT = 449,
        DESTINATION = 450,
        DETAIL = 451,
        DISABLE = 452,
        EGI = 453,
        EMI = 454,
        ENABLE = 455,
        END_RECEIVE = 456,
        ESI = 457,
        FINAL = 458,
        FREE = 459,
        GROUP = 460,
        HEADING = 461,
        INDICATE = 462,
        INITIATE = 463,
        LAST = 464,
        LIMIT = 465,
        LIMITS = 466,
        LINE_COUNTER = 467,
        MESSAGE = 468,
        NUMBER = 469,
        PAGE_COUNTER = 470,
        PF = 471,
        PH = 472,
        PLUS = 473,
        PRINTING = 474,
        PURGE = 475,
        QUEUE = 476,
        RD = 477,
        RECEIVE = 478,
        REPORT = 479,
        REPORTING = 480,
        REPORTS = 481,
        RF = 482,
        RH = 483,
        SEGMENT = 484,
        SEND = 485,
        SOURCE = 486,
        SUB_QUEUE_1 = 487,
        SUB_QUEUE_2 = 488,
        SUB_QUEUE_3 = 489,
        SUM = 490,
        TABLE = 491,
        TERMINAL = 492,
        TERMINATE = 493,
        TEXT = 494,
        END_JSON = 495,
        JSON = 496,
        VOLATILE = 497,
        TYPEDEF = 498,
        STRONG = 499,
        UNSAFE = 500,
        PUBLIC = 501,
        PRIVATE = 502,
        IN_OUT = 503,
        STRICT = 504,
        GLOBAL_STORAGE = 505,
        QuestionMark = 506,
        CompilerDirective = 507,
        CopyImportDirective = 508,
        ReplaceDirective = 509,
        ContinuationTokenGroup = 510,
    }

    public static class TokenConst {
        private static readonly TokenType[] TypeCobolTokenType =
        {
            TokenType.DECLARE, TokenType.END_DECLARE, TokenType.PUBLIC, TokenType.PRIVATE, TokenType.IN_OUT,
            TokenType.UNSAFE, TokenType.STRICT, TokenType.QuestionMark
        };

        private static readonly TokenType[] Cobol2002TokenType = {TokenType.STRONG, TokenType.TYPEDEF};

        public static CobolLanguageLevel GetCobolLanguageLevel(TokenType tokenType) {
            if (TypeCobolTokenType.Contains(tokenType))
            {
                return CobolLanguageLevel.TypeCobol;
            }
            if (Cobol2002TokenType.Contains(tokenType))
            {
                return CobolLanguageLevel.Cobol2002;
            }
            return CobolLanguageLevel.Cobol85;
        }
    }
}