using System.Collections.Generic;

public enum TokenType
{
    // Keywords
    KW_FOR, KW_WHILE, KW_IN, KW_RANGE, KW_TRUE, KW_FALSE, KW_IF, KW_ELIF, KW_ELSE,
    KW_STR, KW_DEF, KW_RETURN, KW_AND, KW_OR,
    // Komande robota
    CMD_FORWARD, CMD_BACK, CMD_TURNLEFT, CMD_TURNRIGHT, CMD_PRINT,
    // Komande LED zaslona
    DISPLAY_TEXT, DISPLAY_CHAR, DISPLAY_GREEN, DISPLAY_RED, DISPLAY_CLEAR, SLEEP,
    // Prepoznavanje slike
    DETECT_OBJECT, DETECT_OBJECT_CONF,
    // Operatori
    OP_PLUS, OP_MINUS, OP_MULTIPLY, OP_DIVIDE,
    OP_PLUSEQ, OP_MINUSEQ, OP_MULTIPLYEQ, OP_DIVIDEEQ,
    OP_ASSIGN,   // =
    OP_EQUAL,    // ==
    OP_NOTEQUAL, // !=
    OP_LESS,      //  <
    OP_GREATER,       // >
    OP_LESSEQ,      //  <=
    OP_GREATEREQ,       // >=
    // Interpunkcija
    L_PARENTHESIS, R_PARENTHESIS, COLON,
    L_SQ_BRACKET, R_SQ_BRACKET, COMMA,

    // Vrijednosti
    NUMBER, IDN, STRING, CHAR,
    // Struktura
    INDENT, DEDENT, NEWLINE,
    // Kraj
    EOF
}

public class Token
{
    public TokenType Type;
    public string Value;
    public int Line;

    public Token(TokenType type, string value, int line)
    {
        Type = type;
        Value = value;
        Line = line;
    }

    public override string ToString() => $"{Type} {Line} {Value}";
}

public class Lexer
{
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        // Keywords
        { "for", TokenType.KW_FOR},
        { "while", TokenType.KW_WHILE},
        { "in", TokenType.KW_IN},
        { "range", TokenType.KW_RANGE},
        { "True", TokenType.KW_TRUE},
        { "False", TokenType.KW_FALSE},
        { "if", TokenType.KW_IF},
        { "elif", TokenType.KW_ELIF},
        { "else", TokenType.KW_ELSE},
        { "str", TokenType.KW_STR},
        { "def", TokenType.KW_DEF},
        { "return", TokenType.KW_RETURN},
        { "and", TokenType.KW_AND},
        { "or", TokenType.KW_OR},
        // Komande robota
        { "forward", TokenType.CMD_FORWARD},
        { "back", TokenType.CMD_BACK},
        { "turn_left", TokenType.CMD_TURNLEFT},
        { "turn_right", TokenType.CMD_TURNRIGHT},
        {"print", TokenType.CMD_PRINT},
        // Komande LED zaslona
        { "display_text", TokenType.DISPLAY_TEXT},
        { "display_char", TokenType.DISPLAY_CHAR},
        { "display_green", TokenType.DISPLAY_GREEN},
        { "display_red", TokenType.DISPLAY_RED},
        { "display_clear", TokenType.DISPLAY_CLEAR},
        { "sleep",TokenType.SLEEP},
        // Prepoznavanje slike
        { "detect_object", TokenType.DETECT_OBJECT},
        { "detect_object_conf", TokenType.DETECT_OBJECT_CONF},
    };

    public (List<Token> tokens, List<string> errors) Tokenize(string sourceCode)
    {
        var tokens = new List<Token>();
        var errors = new List<string>();
        var lines = sourceCode.Split('\n');

        var indentStack = new Stack<int>();
        indentStack.Push(0);

        int lineCounter = 0;

        foreach (var rawLine in lines)
        {
            lineCounter++;

            // preskakanje praznih linija i linija koje započinju # - komentari
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            if (rawLine.TrimStart().StartsWith("#")) continue;

            // INDENT/DEDENT
            int tabCount = 0;
            int spaceCount = 0;
            int pos = 0;
            while (pos < rawLine.Length && (rawLine[pos] == '\t' || rawLine[pos] == ' '))
            {
                if (rawLine[pos] == '\t') tabCount++;
                else spaceCount++;
                pos++;
            }

            int indent;
            if (tabCount > 0 && spaceCount > 0)
            {
                errors.Add($"Line {lineCounter}: Don't mix tabs and spaces for indentation.");
                indent = tabCount;
            }
            else if (tabCount > 0)
            {
                indent = tabCount;
            }
            else if (spaceCount > 0)
            {
                if (spaceCount % 4 != 0)
                    errors.Add($"Line {lineCounter}: Use 4 spaces per indent level (found {spaceCount} spaces). " +
                               "Each level must be exactly 4 spaces.");
                indent = spaceCount / 4;
            }
            else
            {
                indent = 0;
            }

            int currentIndent = indentStack.Peek();

            if (indent > currentIndent)
            {
                indentStack.Push(indent);
                tokens.Add(new Token(TokenType.INDENT, "", lineCounter));
            }
            else
            {
                while (indent < indentStack.Peek())
                {
                    indentStack.Pop();
                    tokens.Add(new Token(TokenType.DEDENT, "", lineCounter));
                }

                // Provjeri je li indent validan (mora odgovarati nekoj prethodnoj razini)
                if (indent != indentStack.Peek())
                    errors.Add($"Line {lineCounter}: Indentation error — the indentation on this line doesn't match any " +
                    "open block. Make sure you use tabs or 4 spaces consistently inside for/while/if blocks.");
            }

            // --- Tokenizacija linije ---
            string line = rawLine.TrimStart();
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                // KOMENTAR
                if (c == '#') break;

                // RAZMACI
                if (c == ' ') { i++; continue; }

                // OPERATORI (VIŠEZNAČNI)
                if (c == '=' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_EQUAL, "==", lineCounter)); i += 2; continue;
                }
                if (c == '!' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_NOTEQUAL, "!=", lineCounter)); i += 2; continue;
                }
                if (c == '<' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_LESSEQ, "<=", lineCounter)); i += 2; continue;
                }
                if (c == '>' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_GREATEREQ, ">=", lineCounter)); i += 2; continue;
                }
                if (c == '+' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_PLUSEQ, "+=", lineCounter)); i += 2; continue;
                }
                if (c == '-' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_MINUSEQ, "-=", lineCounter)); i += 2; continue;
                }
                if (c == '*' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_MULTIPLYEQ, "*=", lineCounter)); i += 2; continue;
                }
                if (c == '/' && i + 1 < line.Length && line[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.OP_DIVIDEEQ, "/=", lineCounter)); i += 2; continue;
                }

                // OPERATORI (JEDNOZNAČNI)
                switch (c)
                {
                    case '+': tokens.Add(new Token(TokenType.OP_PLUS, "+", lineCounter)); i++; continue;
                    case '-': tokens.Add(new Token(TokenType.OP_MINUS, "-", lineCounter)); i++; continue;
                    case '*': tokens.Add(new Token(TokenType.OP_MULTIPLY, "*", lineCounter)); i++; continue;
                    case '/': tokens.Add(new Token(TokenType.OP_DIVIDE, "/", lineCounter)); i++; continue;
                    case '=': tokens.Add(new Token(TokenType.OP_ASSIGN, "=", lineCounter)); i++; continue;
                    case '<': tokens.Add(new Token(TokenType.OP_LESS, "<", lineCounter)); i++; continue;
                    case '>': tokens.Add(new Token(TokenType.OP_GREATER, ">", lineCounter)); i++; continue;
                    case '(': tokens.Add(new Token(TokenType.L_PARENTHESIS, "(", lineCounter)); i++; continue;
                    case ')': tokens.Add(new Token(TokenType.R_PARENTHESIS, ")", lineCounter)); i++; continue;
                    case '[': tokens.Add(new Token(TokenType.L_SQ_BRACKET, "[", lineCounter)); i++; continue;
                    case ']': tokens.Add(new Token(TokenType.R_SQ_BRACKET, "]", lineCounter)); i++; continue;
                    case ',': tokens.Add(new Token(TokenType.COMMA, ",", lineCounter)); i++; continue;
                    case ':': tokens.Add(new Token(TokenType.COLON, ":", lineCounter)); i++; continue;
                }

                // STRING LITERAL
                if (c == '\'' || c == '"')
                {
                    char closing = c; // pamtimo koji znak zatvara (' ili ")
                    i++; // preskoči otvarajući znak
                    string str = "";
                    while (i < line.Length && line[i] != closing)
                    {
                        str += line[i];
                        i++;
                    }
                    if (i >= line.Length)
                        errors.Add($"Line {lineCounter}: You forgot to close the quotation marks on this line. " +
                        "Every string must end with the same character it started with — " +
                        $"e.g.  print(\"Hello\")  or  print('Hello')");
                    else
                        i++; // preskoči zatvarajući znak
                    tokens.Add(new Token(TokenType.STRING, str, lineCounter));
                    continue;
                }

                // CHAR LITERAL
                if (c == '\'')
                {
                    i++; // preskoči otvarajući '
                    if (i < line.Length && line[i] != '\'' && i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        string ch = line[i].ToString();
                        i += 2; // preskoči znak i zatvarajući '
                        tokens.Add(new Token(TokenType.CHAR, ch, lineCounter));
                    }
                    else
                    {
                        errors.Add(c switch
                        {
                            ';' => "[Lexer] You used ';' at the end of the line — Python doesn't need semicolons.",
                            '{' => "[Lexer] You used '{' — Python doesn't use curly braces. " +
                                   "Use a colon ':' and indent the next line instead.",
                            '}' => "[Lexer] You used '}' — Python doesn't use curly braces. " +
                                   "Use indentation to close blocks.",
                            _ => $"[Lexer] Unknown character '{c}'."
                        });
                    }
                    continue;
                }

                // BROJ
                if (char.IsDigit(c))
                {
                    string num = "";
                    bool hasDot = false;

                    while (i < line.Length)
                    {
                        char current = line[i];
                        if (char.IsDigit(current))
                        {
                            num += current;
                        }
                        else if (current == '.' && !hasDot)
                        {
                            // Dopusti samo jednu točku u broju
                            hasDot = true;
                            num += current;
                        }
                        else
                        {
                            // Ako nije znamenka ni prva točka, broj je gotov
                            break;
                        }
                        i++;
                    }
                    tokens.Add(new Token(TokenType.NUMBER, num, lineCounter));
                    continue;
                }

                // IDENTIFIKATOR/KEYWORD
                if (char.IsLetter(c) || c == '_')
                {
                    string word = "";
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    {
                        word += line[i];
                        i++;
                    }
                    if (Keywords.TryGetValue(word, out TokenType kwType))
                        tokens.Add(new Token(kwType, word, lineCounter));
                    else
                        tokens.Add(new Token(TokenType.IDN, word, lineCounter));
                    continue;
                }

                // Nepoznati znak
                errors.Add($"Line {lineCounter}: Unknown character '{c}'.");
                i++;
            }

            tokens.Add(new Token(TokenType.NEWLINE, "", lineCounter));
        }

        // Zatvori sve otvorene blokove na kraju fajla
        while (indentStack.Peek() > 0)
        {
            indentStack.Pop();
            tokens.Add(new Token(TokenType.DEDENT, "", lineCounter));
        }

        tokens.Add(new Token(TokenType.EOF, "", lineCounter));
        return (tokens, errors);
    }
}