using Antlr4.Runtime;
using System.Text;

namespace OOP_1.Services
{
    public class ExpressionErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
    {
        public List<string> Errors { get; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            AddError(charPositionInLine, msg, offendingSymbol?.Text);
        }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            AddError(charPositionInLine, msg, null);
        }

        private void AddError(int charPositionInLine, string msg, string token)
        {
            var errorMessage = new StringBuilder();
            errorMessage.Append($"Позиція {charPositionInLine}: ");

            if (msg.Contains("mismatched input"))
            {
                var tokenText = token ?? "?";
                errorMessage.Append($"Неочікуваний символ '{tokenText}'");
            }
            else if (msg.Contains("missing"))
            {
                errorMessage.Append("Відсутній очікуваний символ");
            }
            else if (msg.Contains("extraneous input"))
            {
                var tokenText = token ?? "?";
                errorMessage.Append($"Зайвий символ '{tokenText}'");
            }
            else if (msg.Contains("no viable alternative"))
            {
                errorMessage.Append("Невірний синтаксис виразу");
            }
            else if (msg.Contains("token recognition error"))
            {
                errorMessage.Append("Невідомий символ");
            }
            else
            {
                errorMessage.Append(msg);
            }

            Errors.Add(errorMessage.ToString());
        }

        public string GetErrorMessage()
        {
            if (!HasErrors) return string.Empty;
            return string.Join("; ", Errors);
        }
    }
}