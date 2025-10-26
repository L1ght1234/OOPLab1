namespace OOP_1.Models
{
    public readonly struct CellAddress
    {
        public int Row { get; }
        public int Column { get; }

        public CellAddress(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public static string ColumnToName(int column)
        {
            string colName = "";
            int c = column + 1;

            while (c > 0)
            {
                c--;
                colName = (char)('A' + (c % 26)) + colName;
                c /= 26;
            }

            return colName;
        }

        public static CellAddress FromString(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Адреса комірки не може бути порожньою");
            }

            string colName = new string(address.TakeWhile(char.IsLetter).ToArray());
            string rowName = new string(address.SkipWhile(char.IsLetter).ToArray());

            if (string.IsNullOrEmpty(colName) || string.IsNullOrEmpty(rowName))
            {
                throw new FormatException($"Невірний формат адреси комірки: {address}");
            }

            int col = 0;
            foreach (char c in colName.ToUpper())
            {
                if (c < 'A' || c > 'Z')
                {
                    throw new FormatException($"Невірний символ у назві стовпця: {c}");
                }
                col = col * 26 + (c - 'A' + 1);
            }
            col--;

            if (!int.TryParse(rowName, out int row) || row < 1)
            {
                throw new FormatException($"Невірний номер рядка: {rowName}");
            }

            return new CellAddress(row - 1, col);
        }

        public override string ToString()
        {
            string colName = "";
            int c = Column + 1;

            while (c > 0)
            {
                c--;
                colName = (char)('A' + (c % 26)) + colName;
                c /= 26;
            }

            return $"{colName}{Row + 1}";
        }

        public override bool Equals(object? obj)
        {
            return obj is CellAddress address &&
                   Row == address.Row &&
                   Column == address.Column;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(CellAddress left, CellAddress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellAddress left, CellAddress right)
        {
            return !(left == right);
        }
    }
}