using System.Collections.Generic;

public class Parser
{

    public abstract class Expression { }

    public class NumberExpression : Expression
    {
        public double value;
    }

    public class VariableExpression : Expression
    {
        public string name;
    }

    public class StringExpression : Expression
    {
        public string value;
    }

    public class BinaryExpression : Expression
    {
        public Expression left;
        public string oper; // "+", "-", "*", "/"
        public Expression right;
    }

    public class DetectObjectExpression : Expression { }

    public class DetectObjectConfExpression : Expression { }

    public class FunctionCallExpression : Expression
    {
        public string functionName;
        public List<Expression> arguments;
    }

    public class StrExpression : Expression
    {
        public Expression inner;
    }

    public class ListExpression : Expression
    {
        public List<Expression> elements;
    }

    public class IndexExpression : Expression
    {
        public string variableName;
        public Expression index;
    }

    public abstract class Statement
    {
        public int line;
    }

    public class CommandStatement : Statement
    {
        public TokenType command;
        public Expression expression;
    }

    public class AssignStatement : Statement
    {
        public string variableName;
        public Expression expression;
    }

    public class CompoundAssignStatement : Statement
    {
        public string variableName;
        public string assignmentType;
        public Expression expression;
    }

    public class PrintStatement : Statement
    {
        public Expression printExpr;
    }

    public class SleepStatement : Statement
    {
        public int seconds;
    }

    public class ForStatement : Statement
    {
        public string iterator;
        public int times;
        public List<Statement> body;

    }

    public class ForEachStatement : Statement
    {
        public string iterator;
        public string variableName;
        public List<Statement> body;

    }

    public class WhileStatement : Statement
    {
        public Condition condition;
        public List<Statement> body;

    }

    public class IfStatement : Statement
    {
        public Condition condition;
        public List<Statement> thenBody;
        public List<(Condition, List<Statement>)> elifChain;
        public List<Statement> elseBody;
    }

    public class IndexCompoundAssignStatement : Statement
    {
        public string list;
        public Expression index;
        public string assignmentType;
        public Expression value;
    }

    public class MultiAssignStatement : Statement
    {
        public List<string> variableNames;
        public Expression call;
    }

    public class DefStatement : Statement
    {
        public string functionName;
        public List<string> arguments;
        public List<Statement> functionBody;
    }

    public class ReturnStatement : Statement
    {
        public Expression returnValue;
    }

    public class FunctionCallStatement : Statement
    {
        public FunctionCallExpression call;
    }

    public class Condition
    {
        public Expression left;
        public string oper;
        public Expression right;
        public List<(string logicalOp, Condition next)> chain;
    }

    private List<string> errors = new List<string>();


    public (List<Statement> statements, List<string> errors) Parse(List<Token> tokens)
    {
        int i = 0;
        List<Statement> statements = new List<Statement>();


        while (tokens[i].Type != TokenType.EOF && i < tokens.Count)
        {
            if (tokens[i].Type == TokenType.NEWLINE)
            {
                i += 1;
                continue;
            }
            Statement statement = ParseStatement(tokens, ref i);
            statements.Add(statement);
        }

        return (statements, errors);
    }

    public Statement ParseStatement(List<Token> tokens, ref int i)
    {
        int line = tokens[i].Line;
        Statement result = null;

        switch (tokens[i].Type)
        {
            case TokenType.DISPLAY_GREEN:
            case TokenType.DISPLAY_RED:
            case TokenType.DISPLAY_CLEAR:
            case TokenType.DISPLAY_TEXT:
            case TokenType.DISPLAY_CHAR:
            case TokenType.CMD_FORWARD:
            case TokenType.CMD_BACK:
            case TokenType.CMD_TURNLEFT:
            case TokenType.CMD_TURNRIGHT:
                result = ParseCommand(tokens, ref i); break;
            case TokenType.SLEEP:
                result = ParseSleep(tokens, ref i); break;
            case TokenType.CMD_PRINT:
                result = ParsePrint(tokens, ref i); break;
            case TokenType.KW_FOR:
                result = ParseFor(tokens, ref i); break;
            case TokenType.KW_WHILE:
                result = ParseWhile(tokens, ref i); break;
            case TokenType.KW_IF:
                result = ParseIf(tokens, ref i); break;
            case TokenType.IDN:
                result = ParseAssign(tokens, ref i); break;
            case TokenType.KW_DEF:
                result = ParseDef(tokens, ref i); break;
            case TokenType.KW_RETURN:
                result = ParseReturn(tokens, ref i); break;
            default:
                errors.Add($"Line {tokens[i].Line}: Unexpected command '{tokens[i].Value}'. " +
                $"Check for typos.");
                i++;
                return null;
        }

        if (result != null) result.line = line;
        return result;
    }

    public Statement ParseWhile(List<Token> tokens, ref int i)
    {
        i += 1;
        Condition condition = ParseCondition(tokens, ref i);
        Expect(TokenType.COLON, tokens, ref i, "while");
        Expect(TokenType.NEWLINE, tokens, ref i, "while");

        List<Statement> body = ParseBlock(tokens, ref i);
        return new WhileStatement { condition = condition, body = body };
    }

    public Condition ParseCondition(List<Token> tokens, ref int i)
    {
        Expression left = ParseExpression(tokens, ref i);

        string oper = tokens[i].Value;
        if (tokens[i].Type != TokenType.OP_EQUAL &&
            tokens[i].Type != TokenType.OP_LESS &&
            tokens[i].Type != TokenType.OP_GREATER &&
            tokens[i].Type != TokenType.OP_LESSEQ &&
            tokens[i].Type != TokenType.OP_GREATEREQ &&
            tokens[i].Type != TokenType.OP_NOTEQUAL &&
            tokens[i].Type != TokenType.KW_IN)
        {
            errors.Add($"Line {tokens[i].Line}: Expected a comparison operator " +
           $"(==, !=, <, >, <=, >=) but found '{tokens[i].Value}'.");
        }
        i++;

        Expression right = ParseExpression(tokens, ref i);
        var condition = new Condition { left = left, oper = oper, right = right };

        while (tokens[i].Type == TokenType.KW_AND || tokens[i].Type == TokenType.KW_OR)
        {
            string logicalOp = tokens[i].Value;
            i += 1;
            Condition next = ParseCondition(tokens, ref i);
            condition.chain ??= new List<(string, Condition)>();
            condition.chain.Add((logicalOp, next));
            break;
        }

        return condition;
    }

    public Statement ParseAssign(List<Token> tokens, ref int i)
    {
        string variable = tokens[i].Value;
        Expression index = null;
        Expect(TokenType.IDN, tokens, ref i, "assign");

        if (tokens[i].Type == TokenType.COMMA)
        {
            List<string> varNames = new List<string> { variable };
            while (tokens[i].Type == TokenType.COMMA)
            {
                i += 1;
                varNames.Add(tokens[i].Value);
                Expect(TokenType.IDN, tokens, ref i, "def");
            }
            Expect(TokenType.OP_ASSIGN, tokens, ref i, "assign");
            //detect_object_func slučaj
            if (tokens[i].Type == TokenType.DETECT_OBJECT_CONF)
            {
                i++;
                Expect(TokenType.L_PARENTHESIS, tokens, ref i, "detect_object_conf");
                Expect(TokenType.R_PARENTHESIS, tokens, ref i, "detect_object_conf");
                Expect(TokenType.NEWLINE, tokens, ref i, "detect_object_conf");
                return new MultiAssignStatement { variableNames = varNames, call = new DetectObjectConfExpression() };
            }

            //mora biti funkcijski poziv
            string funcName = tokens[i].Value;
            Expect(TokenType.IDN, tokens, ref i, "assign");
            Expect(TokenType.L_PARENTHESIS, tokens, ref i, "assign");
            List<Expression> arguments = new List<Expression>();
            bool first = true;
            while (tokens[i].Type != TokenType.R_PARENTHESIS &&
                   tokens[i].Type != TokenType.EOF &&
                   tokens[i].Type != TokenType.NEWLINE)
            {
                if (!first) Expect(TokenType.COMMA, tokens, ref i, "function call");
                first = false;
                arguments.Add(ParseExpression(tokens, ref i));
            }
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "assign");
            Expect(TokenType.NEWLINE, tokens, ref i, "assign");
            var callExpr = new FunctionCallExpression { functionName = funcName, arguments = arguments };
            return new MultiAssignStatement { variableNames = varNames, call = callExpr };
        }

        if (tokens[i].Type == TokenType.L_PARENTHESIS)
        {
            i++;
            List<Expression> arguments = new List<Expression>();
            bool first = true;
            while (tokens[i].Type != TokenType.R_PARENTHESIS && tokens[i].Type != TokenType.EOF && tokens[i].Type != TokenType.NEWLINE)
            {
                if (first == true)
                {
                    first = false;
                }
                else
                {
                    Expect(TokenType.COMMA, tokens, ref i, "def");
                }
                Expression expression = ParseExpression(tokens, ref i);
                arguments.Add(expression);
            }
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "function call");
            Expect(TokenType.NEWLINE, tokens, ref i, "function call");
            var callExpr = new FunctionCallExpression { functionName = variable, arguments = arguments };
            return new FunctionCallStatement { call = callExpr };
        }
        if (tokens[i].Type == TokenType.L_SQ_BRACKET)
        {
            i += 1;
            index = ParseExpression(tokens, ref i);
            Expect(TokenType.R_SQ_BRACKET, tokens, ref i, "assign");
        }
        if (tokens[i].Type == TokenType.OP_PLUSEQ ||
            tokens[i].Type == TokenType.OP_MINUSEQ ||
            tokens[i].Type == TokenType.OP_MULTIPLYEQ ||
            tokens[i].Type == TokenType.OP_DIVIDEEQ)
        {
            string assignType = tokens[i].Value;
            i++;
            Expression expression = ParseExpression(tokens, ref i);
            Expect(TokenType.NEWLINE, tokens, ref i, "assign");
            if (index != null)
                return new IndexCompoundAssignStatement { list = variable, index = index, assignmentType = assignType, value = expression };
            return new CompoundAssignStatement { variableName = variable, assignmentType = assignType, expression = expression };
        }
        else
        {
            Expect(TokenType.OP_ASSIGN, tokens, ref i, "assign");
            Expression expression = ParseExpression(tokens, ref i);
            Expect(TokenType.NEWLINE, tokens, ref i, "assign");
            if (index != null)
                return new IndexCompoundAssignStatement { list = variable, index = index, assignmentType = "=", value = expression };
            return new AssignStatement { variableName = variable, expression = expression };
        }
    }
    public Statement ParseFor(List<Token> tokens, ref int i)
    {
        i += 1;
        string currIterator = tokens[i].Value;
        Expect(TokenType.IDN, tokens, ref i, "for");
        Expect(TokenType.KW_IN, tokens, ref i, "for");
        if (tokens[i].Type == TokenType.IDN)
        {
            string variableName = tokens[i].Value;
            i += 1;
            Expect(TokenType.COLON, tokens, ref i, "for");
            Expect(TokenType.NEWLINE, tokens, ref i, "for");
            List<Statement> forEachBody = ParseBlock(tokens, ref i);
            return new ForEachStatement { iterator = currIterator, variableName = variableName, body = forEachBody };
        }
        Expect(TokenType.KW_RANGE, tokens, ref i, "for");
        Expect(TokenType.L_PARENTHESIS, tokens, ref i, "for");
        int currTimes = int.TryParse(tokens[i].Value, out int parsed) ? parsed : 0;
        Expect(TokenType.NUMBER, tokens, ref i, "for");
        Expect(TokenType.R_PARENTHESIS, tokens, ref i, "for");
        Expect(TokenType.COLON, tokens, ref i, "for");
        Expect(TokenType.NEWLINE, tokens, ref i, "for");

        List<Statement> body = ParseBlock(tokens, ref i);
        return new ForStatement { iterator = currIterator, times = currTimes, body = body };
    }

    public Statement ParseIf(List<Token> tokens, ref int i)
    {
        // if dio
        i += 1;
        Condition condition;
        if (tokens[i].Type == TokenType.L_PARENTHESIS) // condition is in parenthesis
        {
            i += 1;
            condition = ParseCondition(tokens, ref i);
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "if");
        }
        else
        {
            condition = ParseCondition(tokens, ref i);
        }
        Expect(TokenType.COLON, tokens, ref i, "if");
        Expect(TokenType.NEWLINE, tokens, ref i, "if");
        List<Statement> thenBody = ParseBlock(tokens, ref i);

        // elif dio
        List<(Condition, List<Statement>)> elifChain = new List<(Condition, List<Statement>)>();
        while (tokens[i].Type == TokenType.KW_ELIF)
        {
            i += 1;
            Condition elifCondition;
            if (tokens[i].Type == TokenType.L_PARENTHESIS) // condition is in parenthesis
            {
                i += 1;
                elifCondition = ParseCondition(tokens, ref i);
                Expect(TokenType.R_PARENTHESIS, tokens, ref i, "if");
            }
            else
            {
                elifCondition = ParseCondition(tokens, ref i);
            }
            Expect(TokenType.COLON, tokens, ref i, "if");
            Expect(TokenType.NEWLINE, tokens, ref i, "if");
            List<Statement> body = ParseBlock(tokens, ref i);
            elifChain.Add((elifCondition, body));
        }

        // else dio
        List<Statement> elseBody = null;
        if (tokens[i].Type == TokenType.KW_ELSE)
        {
            i += 1;
            Expect(TokenType.COLON, tokens, ref i, "if");
            Expect(TokenType.NEWLINE, tokens, ref i, "if");
            elseBody = ParseBlock(tokens, ref i);
        }

        return new IfStatement { condition = condition, thenBody = thenBody, elifChain = elifChain, elseBody = elseBody };
    }

    public Statement ParseDef(List<Token> tokens, ref int i)
    {
        i += 1;
        string functionName = "";
        if (tokens[i].Type == TokenType.IDN)
        {
            functionName = tokens[i].Value;
            i += 1;
        }
        Expect(TokenType.L_PARENTHESIS, tokens, ref i, "def");
        List<string> argNames = new List<string>();
        bool first = true;
        while (tokens[i].Type != TokenType.R_PARENTHESIS && tokens[i].Type != TokenType.EOF && tokens[i].Type != TokenType.NEWLINE)
        {
            if (first == true)
            {
                first = false;
            }
            else
            {
                Expect(TokenType.COMMA, tokens, ref i, "def");
            }
            argNames.Add(tokens[i].Value);
            Expect(TokenType.IDN, tokens, ref i, "def");
        }
        Expect(TokenType.R_PARENTHESIS, tokens, ref i, "def");
        Expect(TokenType.COLON, tokens, ref i, "def");
        Expect(TokenType.NEWLINE, tokens, ref i, "def");
        List<Statement> functionBody = ParseBlock(tokens, ref i);
        return new DefStatement { functionName = functionName, arguments = argNames, functionBody = functionBody };
    }

    public Statement ParseReturn(List<Token> tokens, ref int i)
    {
        i += 1;
        Expression returnValue = null;
        if (tokens[i].Type != TokenType.NEWLINE)
        {
            List<Expression> values = new List<Expression>();
            values.Add(ParseExpression(tokens, ref i));
            while (tokens[i].Type == TokenType.COMMA)
            {
                i += 1;
                values.Add(ParseExpression(tokens, ref i));
            }
            returnValue = values.Count == 1 ? values[0] : new ListExpression { elements = values };
        }
        Expect(TokenType.NEWLINE, tokens, ref i, "return");
        return new ReturnStatement { returnValue = returnValue };
    }

    public List<Statement> ParseBlock(List<Token> tokens, ref int i)
    {
        var body = new List<Statement>();
        if (tokens[i].Type != TokenType.INDENT)
        {
            errors.Add(BuildExpectMessage(TokenType.INDENT, tokens[i], ""));
            return body; // vrati prazan blok, ne ulazi u while
        }
        i++;
        while (tokens[i].Type != TokenType.DEDENT && tokens[i].Type != TokenType.EOF && i < tokens.Count)
        {
            var statement = ParseStatement(tokens, ref i);
            if (statement != null)
            {
                body.Add(statement);
            }
        }
        if (tokens[i].Type == TokenType.DEDENT)
            i++;
        return body;
    }

    public Statement ParseCommand(List<Token> tokens, ref int i)
    {
        TokenType currentCommand = tokens[i].Type;
        if (currentCommand == TokenType.DISPLAY_TEXT || currentCommand == TokenType.DISPLAY_CHAR)
        {
            i += 1;
            Expect(TokenType.L_PARENTHESIS, tokens, ref i);
            Expression expression = ParseExpression(tokens, ref i);
            Expect(TokenType.R_PARENTHESIS, tokens, ref i);
            Expect(TokenType.NEWLINE, tokens, ref i);
            return new CommandStatement { command = currentCommand, expression = expression };
        }
        else
        {
            string cmdName = currentCommand.ToString().Replace("CMD_", "").ToLower();
            i += 1;
            if (tokens[i].Type == TokenType.EOF || tokens[i].Type == TokenType.NEWLINE || i >= tokens.Count)
            {
                errors.Add($"Line {tokens[i].Line}: You forgot '(' — commands need parentheses, e.g.  {cmdName}()");
                return null;
            }
            Expect(TokenType.L_PARENTHESIS, tokens, ref i, cmdName);
            if (tokens[i].Type == TokenType.EOF || tokens[i].Type == TokenType.NEWLINE || i >= tokens.Count)
            {
                errors.Add($"Line {tokens[i].Line}: You forgot to close ')' in '{cmdName}'.");
                return null;
            }
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, cmdName);
            Expect(TokenType.NEWLINE, tokens, ref i, cmdName);
            return new CommandStatement { command = currentCommand, expression = null };
        }
    }

    public Statement ParseSleep(List<Token> tokens, ref int i)
    {
        TokenType currentCommand = tokens[i].Type;
        i += 1;
        Expect(TokenType.L_PARENTHESIS, tokens, ref i, "sleep");
        int seconds = int.TryParse(tokens[i].Value, out int parsed) ? parsed : 0;
        Expect(TokenType.NUMBER, tokens, ref i, "sleep");
        Expect(TokenType.R_PARENTHESIS, tokens, ref i, "sleep");
        Expect(TokenType.NEWLINE, tokens, ref i, "sleep");
        return new SleepStatement { seconds = seconds };
    }

    public Statement ParsePrint(List<Token> tokens, ref int i)
    {
        i += 1;
        Expect(TokenType.L_PARENTHESIS, tokens, ref i, "print");
        Expression expression = ParseExpression(tokens, ref i);
        Expect(TokenType.R_PARENTHESIS, tokens, ref i, "print");
        Expect(TokenType.NEWLINE, tokens, ref i, "print");
        return new PrintStatement { printExpr = expression };
    }

    // Expression → Term (('+' | '-') Term)*
    private Expression ParseExpression(List<Token> tokens, ref int i)
    {
        var left = ParseTerm(tokens, ref i);

        while (tokens[i].Type == TokenType.OP_PLUS ||
               tokens[i].Type == TokenType.OP_MINUS)
        {
            string op = tokens[i].Value;
            i += 1;
            var right = ParseTerm(tokens, ref i);
            left = new BinaryExpression { left = left, oper = op, right = right };
        }

        return left;
    }

    // Term → Factor (('*' | '/') Factor)*
    private Expression ParseTerm(List<Token> tokens, ref int i)
    {
        var left = ParseFactor(tokens, ref i);

        while (tokens[i].Type == TokenType.OP_MULTIPLY ||
               tokens[i].Type == TokenType.OP_DIVIDE)
        {
            string op = tokens[i].Value;
            i += 1;
            var right = ParseFactor(tokens, ref i);
            left = new BinaryExpression { left = left, oper = op, right = right };
        }

        return left;
    }

    // Factor → NUMBER | IDN | '(' Expression ')'
    private Expression ParseFactor(List<Token> tokens, ref int i)
    {
        var token = tokens[i];

        if (token.Type == TokenType.NUMBER)
        {
            i += 1;
            return new NumberExpression { value = double.Parse(token.Value) };
        }

        if (token.Type == TokenType.KW_STR)
        {
            i++;
            Expect(TokenType.L_PARENTHESIS, tokens, ref i, "str");
            Expression inner = ParseExpression(tokens, ref i);
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "str");
            return new StrExpression { inner = inner };
        }

        if (token.Type == TokenType.IDN)
        {
            i += 1;
            if (tokens[i].Type == TokenType.L_SQ_BRACKET)
            {
                i += 1;
                Expression expression = ParseExpression(tokens, ref i);
                Expect(TokenType.R_SQ_BRACKET, tokens, ref i);
                return new IndexExpression { variableName = token.Value, index = expression };
            }
            else if (tokens[i].Type == TokenType.L_PARENTHESIS)
            {
                i += 1;
                List<Expression> arguments = new List<Expression>();
                bool first = true;
                while (tokens[i].Type != TokenType.R_PARENTHESIS && tokens[i].Type != TokenType.EOF && tokens[i].Type != TokenType.NEWLINE)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        Expect(TokenType.COMMA, tokens, ref i, "def");
                    }
                    Expression expression = ParseExpression(tokens, ref i);
                    arguments.Add(expression);
                }
                Expect(TokenType.R_PARENTHESIS, tokens, ref i, "function call");
                return new FunctionCallExpression { functionName = token.Value, arguments = arguments };
            }

            return new VariableExpression { name = token.Value };
        }

        if (token.Type == TokenType.L_PARENTHESIS)
        {
            i += 1; // preskoči '('
            var expr = ParseExpression(tokens, ref i);
            Expect(TokenType.R_PARENTHESIS, tokens, ref i);
            return expr;
        }

        if (token.Type == TokenType.L_SQ_BRACKET)
        {
            i += 1;
            var elements = new List<Expression>();
            while (tokens[i].Type != TokenType.R_SQ_BRACKET)
            {
                elements.Add(ParseExpression(tokens, ref i));
                if (tokens[i].Type == TokenType.R_SQ_BRACKET) break;
                Expect(TokenType.COMMA, tokens, ref i, "list");
                if (tokens[i].Type == TokenType.R_SQ_BRACKET)
                {
                    errors.Add($"Line {tokens[i].Line}: Remove the trailing comma before ']'.");
                    break;
                }
            }
            Expect(TokenType.R_SQ_BRACKET, tokens, ref i);
            return new ListExpression { elements = elements };
        }

        if (token.Type == TokenType.OP_MINUS)
        {
            i++;
            var operand = ParseFactor(tokens, ref i);
            return new BinaryExpression
            {
                left = new NumberExpression { value = 0 },
                oper = "-",
                right = operand
            };
        }

        if (token.Type == TokenType.STRING)
        {
            i++;
            return new StringExpression { value = token.Value };
        }

        if (token.Type == TokenType.DETECT_OBJECT)
        {
            i++;
            Expect(TokenType.L_PARENTHESIS, tokens, ref i, "detect_object");
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "detect_object");
            return new DetectObjectExpression();
        }

        if (token.Type == TokenType.DETECT_OBJECT_CONF)
        {
            i++;
            Expect(TokenType.L_PARENTHESIS, tokens, ref i, "detect_object");
            Expect(TokenType.R_PARENTHESIS, tokens, ref i, "detect_object");
            return new DetectObjectConfExpression();
        }

        errors.Add($"Line {token.Line}: Unexpected '{token.Value}' in expression.");
        return null;
    }

    private void Expect(TokenType expectedType, List<Token> tokens, ref int i, string context = "")
    {
        if (i >= tokens.Count)
        {
            errors.Add("Unexpected end of code.");
            return;
        }
        if (expectedType != tokens[i].Type)
        {
            string msg = BuildExpectMessage(expectedType, tokens[i], context);
            errors.Add(msg);
        }
        i++;
    }

    private string BuildExpectMessage(TokenType expected, Token got, string context)
    {
        return (expected, context) switch
        {
            (TokenType.L_PARENTHESIS, _) =>
                $"Line {got.Line}: You forgot '(' — commands need parentheses, e.g.  {context}()",

            (TokenType.R_PARENTHESIS, _) =>
                $"Line {got.Line}: You forgot to close ')' in '{context}'.",

            (TokenType.COLON, "for") =>
                $"Line {got.Line}: You forgot ':' at the end of the for loop — e.g.  for i in range(5):",

            (TokenType.COLON, "while") =>
                $"Line {got.Line}: You forgot ':' at the end of the while loop — e.g.  while x < 5:",

            (TokenType.COLON, "if") =>
                $"Line {got.Line}: You forgot ':' at the end of the if statement — e.g.  if x == 3:",

            (TokenType.INDENT, _) =>
                $"Line {got.Line}: You forgot to indent the block. " +
                $"Everything inside for/while/if must start with a tab.",

            (TokenType.STRING, "print") =>
                $"Line {got.Line}: print() needs text in quotes — e.g.  print(\"Hello\")",

            (TokenType.NUMBER, "sleep") =>
                $"Line {got.Line}: sleep() needs a number — e.g.  sleep(2)",

            (TokenType.KW_IN, _) =>
                $"Line {got.Line}: Missing 'in' in for loop — e.g.  for i in range(5):",

            (TokenType.KW_RANGE, _) =>
                $"Line {got.Line}: Missing 'range' in for loop — e.g.  for i in range(5):",

            _ =>
                $"Line {got.Line}: Expected {expected}, got '{got.Value}'."
        };
    }
}