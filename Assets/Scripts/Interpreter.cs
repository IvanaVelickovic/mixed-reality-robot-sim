using UnityEngine;
using System.Collections;
using static Parser;
using System.Collections.Generic;
public class Interpreter : MonoBehaviour
{
    [SerializeField] private RobotCommands robotCommands;
    [SerializeField] private WebRequestsManager webRequestsManager;
    [SerializeField] private ErrorLogger errorLogger;
    private Dictionary<string, object> variables;
    private Dictionary<string, DefStatement> functions;
    List<string> runtimeErrors;
    private bool isReturning = false;
    private object returnValue = null;

    public void StopInterpreter()
    {
        StopAllCoroutines();
        Debug.Log("Interpreter zaustavljen");
    }
    public void Execute(List<Statement> statements)
    {
        variables = new Dictionary<string, object>();
        functions = new Dictionary<string, DefStatement>();
        runtimeErrors = new List<string>();
        StartCoroutine(ExecuteCoroutine(statements));
    }

    private IEnumerator ExecuteCoroutine(List<Statement> statements)
    {
        foreach (Statement statement in statements)
        {
            yield return ExecuteStatement(statement);
        }
    }

    private IEnumerator ExecuteStatement(Statement statement)
    {
        switch (statement)
        {
            case CommandStatement cmdStmt:
                yield return ExecuteCommand(cmdStmt);
                break;
            case AssignStatement assignStmt:
                yield return ExecuteAssign(assignStmt);
                break;
            case CompoundAssignStatement cmpAssignStmt:
                yield return ExecuteCompoundAssign(cmpAssignStmt);
                break;
            case PrintStatement printStmt:
                yield return ExecutePrint(printStmt);
                break;
            case SleepStatement sleepStmt:
                yield return ExecuteSleep(sleepStmt);
                break;
            case ForStatement forStmt:
                yield return ExecuteFor(forStmt);
                break;
            case ForEachStatement forEachStmt:
                yield return ExecuteForEach(forEachStmt);
                break;
            case WhileStatement whileStmt:
                yield return ExecuteWhile(whileStmt);
                break;
            case IfStatement ifStmt:
                yield return ExecuteIf(ifStmt);
                break;
            case IndexCompoundAssignStatement idxComAssignStmt:
                yield return ExecuteListIndexAssignment(idxComAssignStmt);
                break;
            case DefStatement defStmt:
                functions[defStmt.functionName] = defStmt;
                break;
            case FunctionCallStatement funStmt:
                yield return ExecuteFunction(funStmt.call);
                isReturning = false;
                returnValue = null;
                break;
            case ReturnStatement retStmt:
                returnValue = retStmt.returnValue != null ? EvaluateExpression((Expression)retStmt.returnValue) : null;
                isReturning = true;
                yield break;
            case MultiAssignStatement multiStmt:
                List<object> resultList = null;
                if (multiStmt.call is DetectObjectConfExpression)
                {
                    resultList = EvaluateExpression(multiStmt.call) as List<object>;
                }
                else if (multiStmt.call is FunctionCallExpression funCallExpr)
                {
                    yield return ExecuteFunction(funCallExpr);
                    isReturning = false;
                    resultList = returnValue as List<object>;
                    returnValue = null;
                }
                if (resultList == null)
                {
                    LogRuntimeError("Multi-assign expects function to return multiple values.");
                    yield break;
                }
                if (resultList.Count != multiStmt.variableNames.Count)
                {
                    LogRuntimeError($"Expected {multiStmt.variableNames.Count} values but got {resultList.Count}.");
                    yield break;
                }
                for (int j = 0; j < multiStmt.variableNames.Count; j++)
                    variables[multiStmt.variableNames[j]] = resultList[j];
                break;
            default:
                yield return null;
                break;
        }
    }

    private IEnumerator ExecuteCommand(CommandStatement cmdStmt)
    {
        switch (cmdStmt.command)
        {
            case TokenType.CMD_FORWARD:
                yield return StartCoroutine(robotCommands.Forward());
                break;
            case TokenType.CMD_BACK:
                yield return StartCoroutine(robotCommands.Back());
                break;
            case TokenType.CMD_TURNLEFT:
                yield return StartCoroutine(robotCommands.TurnLeft());
                break;
            case TokenType.CMD_TURNRIGHT:
                yield return StartCoroutine(robotCommands.TurnRight());
                break;
            case TokenType.DISPLAY_GREEN:
                robotCommands.DisplayColor("green");
                yield return null;
                break;
            case TokenType.DISPLAY_RED:
                robotCommands.DisplayColor("red");
                yield return null;
                break;
            case TokenType.DISPLAY_CLEAR:
                robotCommands.DisplayClear();
                yield return null;
                break;
            case TokenType.DISPLAY_TEXT:
            case TokenType.DISPLAY_CHAR:
                object print = EvaluateExpression(cmdStmt.expression);
                if (print is not string output)
                {
                    LogRuntimeError("print() only works with text (strings). " +
                                    "If you want to print a number, convert it first — e.g.  print(str(i))");
                    yield return null;
                    yield break;
                }
                if (cmdStmt.command == TokenType.DISPLAY_CHAR && output.Length > 1)
                {
                    LogRuntimeError("display_char() only works with characters. " +
                                    "If you want to print a string, use display_text()");
                    yield return null;
                    yield break;
                }
                robotCommands.DisplayText(output);
                yield return null;
                break;
            default:
                yield return null;
                break;
        }
    }

    private IEnumerator ExecuteAssign(AssignStatement assignStmt)
    {
        if (assignStmt.expression is FunctionCallExpression funCall)
        {
            yield return ExecuteFunction(funCall);
            variables[assignStmt.variableName] = returnValue;
            isReturning = false;
            returnValue = null;
        }
        else
        {
            object value = EvaluateExpression(assignStmt.expression);
            variables[assignStmt.variableName] = value;
            yield return null;
        }
    }

    private IEnumerator ExecuteCompoundAssign(CompoundAssignStatement cmpAssignStmt)
    {
        if (!variables.TryGetValue(cmpAssignStmt.variableName, out object variable))
        {
            LogRuntimeError($"[Runtime] Variable '{cmpAssignStmt.variableName}' doesn't exist. " +
                            $"You can only use += on a variable that already has a value.");
            yield return null;
            yield break;
        }
        object rightValue = EvaluateExpression(cmpAssignStmt.expression);
        if (variable is not double leftDouble || rightValue is not double rightDouble)
        {
            LogRuntimeError($"[Runtime] '{cmpAssignStmt.assignmentType}' only works with numbers.");
            yield return null;
            yield break;
        }
        double finalValue = 0;
        switch (cmpAssignStmt.assignmentType)
        {
            case "+=":
                finalValue = leftDouble + rightDouble;
                break;
            case "-=":
                finalValue = leftDouble - rightDouble;
                break;
            case "*=":
                finalValue = leftDouble * rightDouble;
                break;
            case "/=":
                if (rightDouble == 0)
                {
                    LogRuntimeError("You can't divide by zero!");
                    yield return null;
                    yield break;
                }
                finalValue = leftDouble / rightDouble;
                break;
        }
        variables[cmpAssignStmt.variableName] = finalValue;
        yield return null;
    }

    private object EvaluateExpression(Expression expression)
    {
        switch (expression)
        {
            case NumberExpression numExpr:
                return numExpr.value;
            case VariableExpression varExpr:
                if (variables.TryGetValue(varExpr.name, out object val))
                    return val;
                LogRuntimeError($"Variable '{varExpr.name}' doesn't exist. " +
                    $"Make sure you assigned it before using it — e.g.  {varExpr.name} = 5");
                return null;
            case StringExpression strExpr:
                return strExpr.value;
            case StrExpression strConv:
                object strVal = EvaluateExpression(strConv.inner);
                if (strVal is double d)
                    return d % 1 == 0
                        ? ((int)d).ToString()   // 5.0 → "5"
                        : d.ToString(System.Globalization.CultureInfo.InvariantCulture); // 5.5 → "5.5"
                return strVal?.ToString() ?? "null";
            case BinaryExpression binExpr:
                object left = EvaluateExpression(binExpr.left);
                object right = EvaluateExpression(binExpr.right);
                if (left is double leftDouble && right is double rightDouble)
                {
                    switch (binExpr.oper)
                    {
                        case "+":
                            return leftDouble + rightDouble;
                        case "-":
                            return leftDouble - rightDouble;
                        case "*":
                            return leftDouble * rightDouble;
                        case "/":
                            if (rightDouble == 0)
                            {
                                LogRuntimeError("You can't divide by zero!");
                                return null;
                            }
                            return leftDouble / rightDouble;
                        default:
                            return null;
                    }
                }
                else if (binExpr.oper == "+")
                {
                    if (left is string && right is string)
                    {
                        return left.ToString() + right.ToString();
                    }
                    else
                    {
                        LogRuntimeError("Cannot concatenate two objects if they are both not string. Use str() to convert a number to a string.");
                    }
                }
                break;
            case DetectObjectExpression detObjExpr:
                return robotCommands.DetectObject();
            case DetectObjectConfExpression detObjConfExpr:
                var (detectedObject, confidence) = robotCommands.DetectObjectConf();
                return new List<object> { detectedObject, confidence };
            case ListExpression listExpr:
                var list = new List<object>();
                foreach (var expr in listExpr.elements)
                {
                    list.Add(EvaluateExpression(expr));
                }
                return list;
            case IndexExpression indexExpr:
                object index = EvaluateExpression(indexExpr.index);
                if (index is not double indexDouble)
                {
                    LogRuntimeError("Invalid list access.");
                    return null;
                }
                int indexInt = (int)indexDouble;

                if (!variables.TryGetValue(indexExpr.variableName, out object obj))
                {
                    LogRuntimeError($"Variable '{indexExpr.variableName}' doesn't exist.");
                    return null;
                }

                if (obj is List<object> lst)
                {
                    if (indexInt < 0 || indexInt >= lst.Count)
                    {
                        LogRuntimeError($"Index {indexInt} is out of range. List has {lst.Count} elements.");
                        return null;
                    }
                    return lst[indexInt];
                }
                else if (obj is string str)
                {
                    if (indexInt < 0 || indexInt >= str.Length)
                    {
                        LogRuntimeError($"Index {indexInt} is out of range. String has {str.Length} characters.");
                        return null;
                    }
                    return str[indexInt].ToString();
                }
                else
                {
                    LogRuntimeError($"'{indexExpr.variableName}' is not a list or string, can't use [] on it.");
                    return null;
                }
            default:
                return null;
        }
        return null;
    }

    private IEnumerator ExecutePrint(PrintStatement printStmt)
    {
        object print = EvaluateExpression(printStmt.printExpr);
        if (print is not string output)
        {
            LogRuntimeError("print() only works with text (strings). " +
                            "If you want to print a number, convert it first — e.g.  print(str(i))");
            yield return null;
            yield break;
        }
        yield return StartCoroutine(webRequestsManager.PostOutputCoroutine(output));
    }

    private IEnumerator ExecuteSleep(SleepStatement sleepStmt)
    {
        yield return new WaitForSeconds(sleepStmt.seconds);
    }

    private IEnumerator ExecuteFor(ForStatement forStmt)
    {
        object copy = null;
        if (variables.ContainsKey(forStmt.iterator))
        {
            //uzimanje globalne vrijednosti varijable koja se zove isto kao iterator
            copy = variables[forStmt.iterator];
        }

        variables[forStmt.iterator] = 0.0;
        int limit = forStmt.times;
        for (int i = 0; i < limit; i += 1)
        {
            foreach (Statement statement in forStmt.body)
            {
                yield return ExecuteStatement(statement);
                if (isReturning) break;
            }
            double newValue = (double)variables[forStmt.iterator] + 1;
            variables[forStmt.iterator] = newValue;
        }

        if (copy != null)
        {
            //vraćanje globalne vrijednosti varijable koja se zove isto kao iterator
            variables[forStmt.iterator] = copy;
        }
        else
        {
            variables.Remove(forStmt.iterator);
        }
    }

    private IEnumerator ExecuteForEach(ForEachStatement forEachStmt)
    {
        object copy = null;
        if (variables.ContainsKey(forEachStmt.iterator))
        {
            //uzimanje globalne vrijednosti varijable koja se zove isto kao iterator
            copy = variables[forEachStmt.iterator];
        }

        if (!variables.TryGetValue(forEachStmt.variableName, out object listObj) || listObj is not List<object> list)
        {
            LogRuntimeError($"Cannot iterate: '{forEachStmt.variableName}' is not a list.");
            yield break;
        }

        int limit = list.Count;
        for (int i = 0; i < limit; i += 1)
        {
            variables[forEachStmt.iterator] = list[i];
            foreach (Statement statement in forEachStmt.body)
            {
                yield return ExecuteStatement(statement);
                if (isReturning) break;
            }
        }

        if (copy != null)
        {
            //vraćanje globalne vrijednosti varijable koja se zove isto kao iterator
            variables[forEachStmt.iterator] = copy;
        }
        else
        {
            variables.Remove(forEachStmt.iterator);
        }
    }

    private IEnumerator ExecuteWhile(WhileStatement whileStmt)
    {
        int maxRepetition = 100;
        while (CheckCondition(whileStmt.condition) == true && maxRepetition > 0)
        {
            foreach (Statement statement in whileStmt.body)
            {
                yield return ExecuteStatement(statement);
                if (isReturning) break;
            }
            maxRepetition -= 1;
        }
        if (maxRepetition == 0)
            LogRuntimeError("Your while loop ran 100 times and was stopped. " +
                            "Check if the condition ever becomes false.");
    }

    private IEnumerator ExecuteIf(IfStatement ifStmt)
    {
        bool oneIfStatementTrue = false;
        if (CheckCondition(ifStmt.condition) == true)
        {
            oneIfStatementTrue = true;
            foreach (Statement statement in ifStmt.thenBody)
            {
                yield return ExecuteStatement(statement);
                if (isReturning) break;
            }
        }
        else
        {
            if (ifStmt.elifChain != null)
            {
                foreach (var (elifCondition, elifBody) in ifStmt.elifChain)
                {
                    if (CheckCondition(elifCondition) == true)
                    {
                        oneIfStatementTrue = true;
                        foreach (Statement statement in elifBody)
                        {
                            yield return ExecuteStatement(statement);
                            if (isReturning) break;
                        }
                        break;
                    }
                }
            }
            if (!oneIfStatementTrue && ifStmt.elseBody != null)
            {
                foreach (Statement statement in ifStmt.elseBody)
                {
                    yield return ExecuteStatement(statement);
                    if (isReturning) break;
                }
            }
        }
    }

    private IEnumerator ExecuteListIndexAssignment(IndexCompoundAssignStatement idxStmt)
    {
        if (!variables.TryGetValue(idxStmt.list, out object listObj) || listObj is not List<object> list)
        {
            LogRuntimeError($"'{idxStmt.list}' is not a list.");
            yield break;
        }
        object index = EvaluateExpression(idxStmt.index);
        if (index is not double indexDouble)
        {
            LogRuntimeError("List index must be a number.");
            yield break;
        }
        int indexInt = (int)indexDouble;
        if (indexInt < 0 || indexInt >= list.Count)
        {
            LogRuntimeError($"Index {indexInt} is out of range. List has {list.Count} elements.");
            yield break;
        }


        object value = EvaluateExpression(idxStmt.value);
        if (idxStmt.assignmentType == "=")
        {
            list[indexInt] = value;
        }
        else
        {
            if (list[indexInt] is not double current || value is not double rightDouble)
            {
                LogRuntimeError($"'{idxStmt.assignmentType}' only works with numbers.");
                yield break;
            }
            double result = idxStmt.assignmentType switch
            {
                "+=" => current + rightDouble,
                "-=" => current - rightDouble,
                "*=" => current * rightDouble,
                "/=" when rightDouble == 0 => double.NaN,
                "/=" => current / rightDouble,
                _ => current
            };
            if (double.IsNaN(result)) { LogRuntimeError("You can't divide by zero!"); yield break; }
            list[indexInt] = result;
        }

        yield return null;
    }

    private IEnumerator ExecuteFunction(FunctionCallExpression funCallExpr)
    {
        Dictionary<string, object> variablesCopy = new Dictionary<string, object>(variables);
        if (!functions.ContainsKey(funCallExpr.functionName))
        {
            LogRuntimeError($"Function {funCallExpr.functionName} is not defined.");
            yield break;
        }
        DefStatement defStmt = functions[funCallExpr.functionName];
        if (defStmt.arguments.Count != funCallExpr.arguments.Count)
        {
            LogRuntimeError($"Definition of {funCallExpr.functionName} and its call have a different number of arguments.");
            yield break;
        }
        for (int i = 0; i < defStmt.arguments.Count; i++)
        {
            variables[defStmt.arguments[i]] = EvaluateExpression(funCallExpr.arguments[i]);
        }
        foreach (Statement stmt in defStmt.functionBody)
        {
            yield return ExecuteStatement(stmt);
            if (isReturning) break;
        }
        variables = variablesCopy;
    }

    private bool CheckCondition(Condition condition)
    {
        bool result = EvaluateSingleCondition(condition);

        if (condition.chain != null)
        {
            foreach (var (logicalOp, next) in condition.chain)
            {
                bool nextResult = CheckCondition(next);
                result = logicalOp == "and" ? result && nextResult : result || nextResult;
            }
        }
        return result;
    }

    private bool EvaluateSingleCondition(Condition condition)
    {
        object left = EvaluateExpression(condition.left);
        object right = EvaluateExpression(condition.right);
        if (condition.oper == "in")
        {
            if (right is System.Collections.IEnumerable enumerable && right is not string)
            {
                foreach (object item in enumerable)
                {
                    if (item == null && left == null) return true;
                    if (item == null || left == null) continue;

                    if (item is string itemStr && left is string leftStr)
                    {
                        if (itemStr == leftStr) return true;
                        continue;
                    }

                    if (item is double itemDble && left is double leftDble)
                    {
                        if (itemDble == leftDble) return true;
                        continue;
                    }

                    if (item.Equals(left)) return true;
                }
            }
            return false;
        }

        if (left is double leftDouble && right is double rightDouble)
        {
            switch (condition.oper)
            {
                case "==":
                    return leftDouble == rightDouble;
                case "!=":
                    return leftDouble != rightDouble;
                case ">=":
                    return leftDouble >= rightDouble;
                case "<=":
                    return leftDouble <= rightDouble;
                case ">":
                    return leftDouble > rightDouble;
                case "<":
                    return leftDouble < rightDouble;
            }
        }
        else if (left is string leftString && right is string rightString)
        {
            switch (condition.oper)
            {
                case "==":
                    return leftString == rightString;
                case "!=":
                    return leftString != rightString;
            }
        }
        return false;
    }

    private void LogRuntimeError(string message)
    {
        runtimeErrors.Add(message);
        errorLogger.Log(ErrorLogger.Level.ERROR, message);
    }
}
