using UnityEngine;
using System.Collections;

public class CommandParser : MonoBehaviour
{
    [SerializeField] private Interpreter interpreter;
    [SerializeField] private ErrorLogger errorLogger;

    public void ParseCommands(string commandText)
    {
        Lexer lexer = new Lexer();
        var (tokens, lexErrors) = lexer.Tokenize(commandText);
        if (lexErrors.Count > 0)
        {
            foreach (var e in lexErrors)
            {
                Debug.LogError($"[LEXER] {e}");
                errorLogger.Log(ErrorLogger.Level.ERROR, e);
            }
            return;
        }

        var parser = new Parser();
        var (statements, parseErrors) = parser.Parse(tokens);

        if (parseErrors.Count > 0)
        {
            foreach (var e in parseErrors)
            {
                Debug.LogError($"[PARSER] {e}");
                errorLogger.Log(ErrorLogger.Level.ERROR, e);
            }
            return;
        }

        interpreter.Execute(statements);
    }

    public void StopInterpreter()
    {
        interpreter.StopInterpreter();
    }

}