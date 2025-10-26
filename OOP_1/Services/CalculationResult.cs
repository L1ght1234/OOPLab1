using OOP_1.Models;

namespace OOP_1.Services
{
    public class CalculationResult
    {
        public object Value { get; set; }
        public HashSet<CellAddress> Dependencies { get; set; }
        public bool HasError { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}