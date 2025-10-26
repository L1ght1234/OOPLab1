namespace OOP_1.Services
{
    public class InvalidCellReferenceException : Exception
    {
        public InvalidCellReferenceException(string message) : base(message) { }
    }
}