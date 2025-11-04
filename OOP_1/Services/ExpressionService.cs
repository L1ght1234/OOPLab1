using Antlr4.Runtime;
using OOP_1.Models;
using OOP1.Grammar;
using System.Diagnostics;

namespace OOP_1.Services
{
    public class ExpressionService
    {
        public CalculationResult Calculate(string expression, Func<CellAddress, object> dependencyResolver)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return new CalculationResult
                {
                    Value = string.Empty,
                    Dependencies = new System.Collections.Generic.HashSet<CellAddress>(),
                    HasError = false
                };
            }

            SpreadsheetVisitor visitor = null;

            try
            {
                Debug.WriteLine($"[ExpressionService] Обчислення виразу: {expression}");

                var inputStream = new AntlrInputStream(expression);
                var lexer = new ExpressionLexer(inputStream);

                lexer.RemoveErrorListeners();

                var lexerErrorListener = new ExpressionErrorListener();
                lexer.AddErrorListener(lexerErrorListener);

                var tokenStream = new CommonTokenStream(lexer);

                if (lexerErrorListener.HasErrors)
                {
                    Debug.WriteLine($"[ExpressionService] Лексична помилка: {lexerErrorListener.GetErrorMessage()}");
                    return new CalculationResult
                    {
                        Value = "#СИНТАКСИС!",
                        Dependencies = new System.Collections.Generic.HashSet<CellAddress>(),
                        HasError = true,
                        ErrorMessage = lexerErrorListener.GetErrorMessage()
                    };
                }

                var parser = new ExpressionParser(tokenStream);
                parser.RemoveErrorListeners();

                var parserErrorListener = new ExpressionErrorListener();
                parser.AddErrorListener(parserErrorListener);

                var tree = parser.parse();

                if (parserErrorListener.HasErrors)
                {
                    Debug.WriteLine($"[ExpressionService] Синтаксична помилка: {parserErrorListener.GetErrorMessage()}");
                    return new CalculationResult
                    {
                        Value = "#СИНТАКСИС!",
                        Dependencies = new System.Collections.Generic.HashSet<CellAddress>(),
                        HasError = true,
                        ErrorMessage = parserErrorListener.GetErrorMessage()
                    };
                }

                visitor = new SpreadsheetVisitor(dependencyResolver);
                object resultValue = visitor.Visit(tree);

                Debug.WriteLine($"[ExpressionService] Результат: {resultValue}, Залежності: {visitor.Dependencies.Count}");

                return new CalculationResult
                {
                    Value = resultValue,
                    Dependencies = visitor.Dependencies,
                    HasError = false,
                    ErrorMessage = string.Empty
                };
            }
            catch (DivideByZeroException ex)
            {
                Debug.WriteLine($"[ExpressionService] Ділення на нуль: {ex.Message}");
                return new CalculationResult
                {
                    Value = "#ДІЛ/0!",
                    Dependencies = visitor?.Dependencies ?? new HashSet<CellAddress>(),
                    HasError = true,
                    ErrorMessage = "Ділення на нуль"
                };
            }
            catch (InvalidCellReferenceException ex)
            {
                Debug.WriteLine($"[ExpressionService] Невірне посилання: {ex.Message}");
                return new CalculationResult
                {
                    Value = "#ПОСИЛАННЯ!",
                    Dependencies = visitor?.Dependencies ?? new HashSet<CellAddress>(),
                    HasError = true,
                    ErrorMessage = ex.Message
                };
            }
            catch (FormatException ex)
            {
                Debug.WriteLine($"[ExpressionService] Помилка формату: {ex.Message}");
                return new CalculationResult
                {
                    Value = "#ЗНАЧЕННЯ!",
                    Dependencies = visitor?.Dependencies ?? new HashSet<CellAddress>(),
                    HasError = true,
                    ErrorMessage = "Невірний формат числа"
                };
            }
            catch (OverflowException ex)
            {
                Debug.WriteLine($"[ExpressionService] Переповнення: {ex.Message}");
                return new CalculationResult
                {
                    Value = "#ЧИСЛО!",
                    Dependencies = visitor?.Dependencies ?? new HashSet<CellAddress>(),
                    HasError = true,
                    ErrorMessage = "Число надто велике або надто мале"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExpressionService] Загальна помилка: {ex.Message}");
                Debug.WriteLine($"[ExpressionService] Stack trace: {ex.StackTrace}");
                return new CalculationResult
                {
                    Value = "#ПОМИЛКА!",
                    Dependencies = visitor?.Dependencies ?? new HashSet<CellAddress>(),
                    HasError = true,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}