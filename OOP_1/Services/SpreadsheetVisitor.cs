using Antlr4.Runtime.Tree;
using OOP_1.Models;
using System.Numerics;
using System.Diagnostics;
using OOP1.Grammar;
using static OOP1.Grammar.ExpressionParser;

namespace OOP_1.Services
{
    public class SpreadsheetVisitor : ExpressionBaseVisitor<object>
    {
        private readonly Func<CellAddress, object> _dependencyResolver;
        public HashSet<CellAddress> Dependencies { get; } = new HashSet<CellAddress>();

        public SpreadsheetVisitor(Func<CellAddress, object> dependencyResolver)
        {
            _dependencyResolver = dependencyResolver;
        }

        private BigInteger ToBigInt(object value)
        {
            try
            {
                if (value is BigInteger bi) return bi;
                if (value is bool b) return b ? BigInteger.One : BigInteger.Zero;
                if (value is string s)
                {
                    if (s.StartsWith("#"))
                    {
                        throw new InvalidOperationException($"Неможливо використати помилку '{s}' у обчисленні");
                    }
                    if (BigInteger.TryParse(s, out var biS)) return biS;
                    return BigInteger.Zero;
                }
                if (value is int i) return new BigInteger(i);
                if (value is long l) return new BigInteger(l);
                if (value is double d) return new BigInteger(d);

                return BigInteger.Zero;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Visitor] Помилка ToBigInt: {ex.Message}");
                return BigInteger.Zero;
            }
        }

        public override object VisitParse(ParseContext context)
        {
            Debug.WriteLine("[Visitor] VisitParse викликано");
            var result = Visit(context.expression());
            Debug.WriteLine($"[Visitor] VisitParse результат: {result}");
            return result;
        }

        public override object VisitAtom(AtomContext context)
        {
            Debug.WriteLine($"[Visitor] VisitAtom: {context.GetText()}");

            if (context.NUMBER() != null)
            {
                try
                {
                    var number = BigInteger.Parse(context.NUMBER().GetText());
                    Debug.WriteLine($"[Visitor] VisitAtom NUMBER: {number}");
                    return number;
                }
                catch (FormatException)
                {
                    throw new FormatException($"Невірний формат числа: {context.NUMBER().GetText()}");
                }
                catch (OverflowException)
                {
                    throw new OverflowException($"Число надто велике: {context.NUMBER().GetText()}");
                }
            }

            if (context.CELL_REF() != null)
            {
                try
                {
                    string cellId = context.CELL_REF().GetText();
                    Debug.WriteLine($"[Visitor] VisitAtom CELL_REF: {cellId}");

                    var address = CellAddress.FromString(cellId);
                    Dependencies.Add(address);

                    var value = _dependencyResolver(address);
                    Debug.WriteLine($"[Visitor] VisitAtom CELL_REF {cellId} = {value}");

                    return value;
                }
                catch (FormatException)
                {
                    throw new InvalidCellReferenceException($"Невірний формат посилання: {context.CELL_REF().GetText()}");
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidCellReferenceException($"Помилка у посиланні {context.CELL_REF().GetText()}: {ex.Message}");
                }
            }

            if (context.functionCall() != null)
            {
                var result = Visit(context.functionCall());
                Debug.WriteLine($"[Visitor] VisitAtom functionCall: {result}");
                return result;
            }

            if (context.expression() != null)
            {
                var result = Visit(context.expression());
                Debug.WriteLine($"[Visitor] VisitAtom expression: {result}");
                return result;
            }

            Debug.WriteLine("[Visitor] VisitAtom повертає Zero");
            return BigInteger.Zero;
        }

        public override object VisitUnaryExpr(UnaryExprContext context)
        {
            Debug.WriteLine($"[Visitor] VisitUnaryExpr: {context.GetText()}");
            var value = ToBigInt(Visit(context.atom()));

            if (context.SUB() != null)
            {
                Debug.WriteLine($"[Visitor] VisitUnaryExpr SUB: {value} -> {-value}");
                return BigInteger.Negate(value);
            }
            Debug.WriteLine($"[Visitor] VisitUnaryExpr результат: {value}");
            return value;
        }

        public override object VisitMultiplicativeExpr(MultiplicativeExprContext context)
        {
            Debug.WriteLine($"[Visitor] VisitMultiplicativeExpr: {context.GetText()}");

            if (context.unaryExpr().Length == 1)
            {
                return Visit(context.unaryExpr(0));
            }

            var left = ToBigInt(Visit(context.unaryExpr(0)));
            Debug.WriteLine($"[Visitor] MultExpr left: {left}");

            for (int i = 1; i < context.unaryExpr().Length; i++)
            {
                var right = ToBigInt(Visit(context.unaryExpr(i)));
                Debug.WriteLine($"[Visitor] MultExpr right: {right}");

                var op = ((ITerminalNode)context.children[i * 2 - 1]).Symbol.Type;
                Debug.WriteLine($"[Visitor] MultExpr operator: {op}");

                switch (op)
                {
                    case MUL:
                        left = BigInteger.Multiply(left, right);
                        Debug.WriteLine($"[Visitor] MUL: {left}");
                        break;

                    case DIV:
                        if (right == BigInteger.Zero)
                        {
                            throw new DivideByZeroException("Ділення на нуль");
                        }
                        left = BigInteger.Divide(left, right);
                        Debug.WriteLine($"[Visitor] DIV: {left}");
                        break;

                    case MOD:
                        if (right == BigInteger.Zero)
                        {
                            throw new DivideByZeroException("Ділення на нуль (mod)");
                        }
                        left = BigInteger.Remainder(left, right);
                        Debug.WriteLine($"[Visitor] MOD: {left}");
                        break;

                    case DIV_INT:
                        if (right == BigInteger.Zero)
                        {
                            throw new DivideByZeroException("Ділення на нуль (div)");
                        }
                        left = BigInteger.Divide(left, right);
                        Debug.WriteLine($"[Visitor] DIV_INT: {left}");
                        break;
                }
            }
            Debug.WriteLine($"[Visitor] MultExpr результат: {left}");
            return left;
        }

        public override object VisitAdditiveExpr(AdditiveExprContext context)
        {
            Debug.WriteLine($"[Visitor] VisitAdditiveExpr: {context.GetText()}");

            if (context.multiplicativeExpr().Length == 1)
            {
                return Visit(context.multiplicativeExpr(0));
            }

            var left = ToBigInt(Visit(context.multiplicativeExpr(0)));
            Debug.WriteLine($"[Visitor] AddExpr left: {left}");

            for (int i = 1; i < context.multiplicativeExpr().Length; i++)
            {
                var right = ToBigInt(Visit(context.multiplicativeExpr(i)));
                Debug.WriteLine($"[Visitor] AddExpr right: {right}");

                var op = ((ITerminalNode)context.children[i * 2 - 1]).Symbol.Type;
                Debug.WriteLine($"[Visitor] AddExpr operator: {op}");

                if (op == ADD)
                {
                    left = BigInteger.Add(left, right);
                    Debug.WriteLine($"[Visitor] ADD: {left}");
                }
                else if (op == SUB)
                {
                    left = BigInteger.Subtract(left, right);
                    Debug.WriteLine($"[Visitor] SUB: {left}");
                }
            }
            Debug.WriteLine($"[Visitor] AddExpr результат: {left}");
            return left;
        }

        public override object VisitExpression(ExpressionContext context)
        {
            Debug.WriteLine($"[Visitor] VisitExpression: {context.GetText()}");

            if (context.additiveExpr().Length == 1)
            {
                var singleResult = Visit(context.additiveExpr(0));
                Debug.WriteLine($"[Visitor] Expression без порівняння: {singleResult}");
                return singleResult;
            }

            var left = ToBigInt(Visit(context.additiveExpr(0)));
            var right = ToBigInt(Visit(context.additiveExpr(1)));
            Debug.WriteLine($"[Visitor] Expression порівняння: {left} ? {right}");

            var op = ((ITerminalNode)context.children[1]).Symbol.Type;
            Debug.WriteLine($"[Visitor] Expression operator: {op}");

            bool comparisonResult;
            switch (op)
            {
                case EQ: comparisonResult = left == right; break;
                case NEQ: comparisonResult = left != right; break;
                case LT: comparisonResult = left < right; break;
                case GT: comparisonResult = left > right; break;
                case LTE: comparisonResult = left <= right; break;
                case GTE: comparisonResult = left >= right; break;
                default: comparisonResult = false; break;
            }

            Debug.WriteLine($"[Visitor] Expression результат: {comparisonResult}");
            return comparisonResult;
        }

        public override object VisitFunctionCall(FunctionCallContext context)
        {
            Debug.WriteLine($"[Visitor] VisitFunctionCall: {context.GetText()}");

            var arguments = context.expression()
                .Select(expr => ToBigInt(Visit(expr)))
                .ToList();

            Debug.WriteLine($"[Visitor] FunctionCall аргументи: {string.Join(", ", arguments)}");

            if (arguments.Count == 0)
            {
                throw new ArgumentException("Функція повинна мати принаймні один аргумент");
            }

            if (context.MMAX() != null)
            {
                var result = arguments.Aggregate(BigInteger.Max);
                Debug.WriteLine($"[Visitor] MMAX результат: {result}");
                return result;
            }

            if (context.MMIN() != null)
            {
                var result = arguments.Aggregate(BigInteger.Min);
                Debug.WriteLine($"[Visitor] MMIN результат: {result}");
                return result;
            }

            throw new InvalidOperationException("Невідома функція");
        }
    }
}