using OOP_1.Services;
using System.Diagnostics;

namespace OOP_1.Models
{
    public class Spreadsheet
    {
        public Dictionary<CellAddress, Cell> Cells { get; } = new Dictionary<CellAddress, Cell>();
        public int RowCount { get; private set; } = 20;
        public int ColumnCount { get; private set; } = 10;

        private readonly ExpressionService _expressionService;

        public Spreadsheet()
        {
            _expressionService = new ExpressionService();
        }

        public Cell GetCell(int row, int col)
        {
            return GetCell(new CellAddress(row, col));
        }

        public Cell GetCell(CellAddress address)
        {
            if (!Cells.ContainsKey(address))
            {
                Cells[address] = new Cell();
            }
            return Cells[address];
        }

        public bool IsValidAddress(CellAddress address)
        {
            return address.Row >= 0 && address.Row < RowCount &&
                   address.Column >= 0 && address.Column < ColumnCount;
        }

        public void SetCellExpression(int row, int col, string expression)
        {
            var address = new CellAddress(row, col);
            var cell = GetCell(address);

            if (cell.Expression == expression)
            {
                return;
            }

            Debug.WriteLine($"[Spreadsheet] Встановлення виразу {address}: '{expression}'");
            cell.Expression = expression;

            RecalculateWithDependents(address);
        }

        private void RecalculateWithDependents(CellAddress address)
        {
            var recalculationChain = new HashSet<CellAddress>();

            RecalculateRecursive(address, recalculationChain);

            RecalculateDependentsRecursive(address, new HashSet<CellAddress>());
        }

        private void RecalculateDependentsRecursive(CellAddress address, HashSet<CellAddress> visited)
        {
            if (visited.Contains(address))
            {
                return;
            }
            visited.Add(address);

            var cell = GetCell(address);
            var dependents = cell.Dependents.ToList();

            foreach (var dependentAddress in dependents)
            {
                Debug.WriteLine($"[Spreadsheet] Каскадний перерахунок залежної клітинки {dependentAddress}");

                var chain = new HashSet<CellAddress>();
                RecalculateRecursive(dependentAddress, chain);

                RecalculateDependentsRecursive(dependentAddress, visited);
            }
        }

        private void RecalculateRecursive(CellAddress address, HashSet<CellAddress> chain)
        {
            if (chain.Contains(address))
            {
                var cell = GetCell(address);
                cell.Value = "#ЦИКЛ!";
                Debug.WriteLine($"[Spreadsheet] Виявлено циркулярне посилання у {address}");
                return;
            }

            chain.Add(address);

            var currentCell = GetCell(address);
            Debug.WriteLine($"[Spreadsheet] Перерахунок {address}: '{currentCell.Expression}'");

            foreach (var oldDependency in currentCell.Dependencies)
            {
                GetCell(oldDependency).Dependents.Remove(address);
            }
            currentCell.Dependencies.Clear();

            var result = _expressionService.Calculate(
                currentCell.Expression,
                addr => GetCellValueSafe(addr, chain)
            );

            currentCell.Value = result.Value;

            if (result.HasError)
            {
                Debug.WriteLine($"[Spreadsheet] Помилка у {address}: {result.ErrorMessage}");
            }
            else
            {
                Debug.WriteLine($"[Spreadsheet] Результат {address}: {result.Value}");
            }

            foreach (var newDependency in result.Dependencies)
            {
                currentCell.Dependencies.Add(newDependency);
                GetCell(newDependency).Dependents.Add(address);
                Debug.WriteLine($"[Spreadsheet] {address} залежить від {newDependency}");
            }

            chain.Remove(address);
        }

        private object GetCellValueSafe(CellAddress address, HashSet<CellAddress> chain)
        {
            if (chain.Contains(address))
            {
                Debug.WriteLine($"[Spreadsheet] Циркулярне посилання виявлено при зверненні до {address}");
                throw new InvalidCellReferenceException($"Циркулярне посилання на {address}");
            }

            if (!IsValidAddress(address))
            {
                Debug.WriteLine($"[Spreadsheet] Посилання на неіснуючу клітинку: {address}");
                throw new InvalidCellReferenceException(
                  $"Клітинка {address} поза межами таблиці (рядків: {RowCount}, стовпців: {ColumnCount})"
                );
            }

            var cell = GetCell(address);

            if (string.IsNullOrWhiteSpace(cell.Expression))
            {
                Debug.WriteLine($"[Spreadsheet] Клітинка {address} порожня");
                throw new InvalidCellReferenceException($"Клітинка {address} порожня");
            }

            RecalculateRecursive(address, chain);

            if (cell.Value is string errorValue && errorValue.StartsWith("#"))
            {
                Debug.WriteLine($"[Spreadsheet] Клітинка {address} містить помилку: {errorValue}");
                throw new InvalidCellReferenceException($"Клітинка {address} містить помилку: {errorValue}");
            }

            if (cell.Value is null)
            {
                Debug.WriteLine($"[Spreadsheet] Клітинка {address} не має значення після перерахунку");
                throw new InvalidCellReferenceException($"Клітинка {address} не обчислена");
            }

            return cell.Value;
        }

        public void AddRow()
        {
            RowCount++;
            Debug.WriteLine($"[Spreadsheet] Додано рядок. Всього: {RowCount}");
        }

        public void AddColumn()
        {
            ColumnCount++;
            Debug.WriteLine($"[Spreadsheet] Додано стовпчик. Всього: {ColumnCount}");
        }

        public void DeleteRow(int row)
        {
            if (row < 0 || row >= RowCount) return;

            var cellsToRemove = Cells.Keys.Where(addr => addr.Row == row).ToList();
            foreach (var addr in cellsToRemove)
            {
                Cells.Remove(addr);
            }

            RowCount--;
            Debug.WriteLine($"[Spreadsheet] Видалено рядок {row}. Залишилось: {RowCount}");
        }

        public void DeleteColumn(int col)
        {
            if (col < 0 || col >= ColumnCount) return;

            var cellsToRemove = Cells.Keys.Where(addr => addr.Column == col).ToList();
            foreach (var addr in cellsToRemove)
            {
                Cells.Remove(addr);
            }

            ColumnCount--;
            Debug.WriteLine($"[Spreadsheet] Видалено стовпчик {col}. Залишилось: {ColumnCount}");
        }
    }
}