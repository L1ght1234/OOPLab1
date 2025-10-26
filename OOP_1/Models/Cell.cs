using System.ComponentModel;

namespace OOP_1.Models
{
    public class Cell : INotifyPropertyChanged
    {
        private string _expression = string.Empty;
        private object? _value;

        public string Expression
        {
            get => _expression;
            set
            {
                if (_expression != value)
                {
                    _expression = value;
                    OnPropertyChanged(nameof(Expression));
                }
            }
        }

        public object? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public HashSet<CellAddress> Dependencies { get; } = new HashSet<CellAddress>();
        public HashSet<CellAddress> Dependents { get; } = new HashSet<CellAddress>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}