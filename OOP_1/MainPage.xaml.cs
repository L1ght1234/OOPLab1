using OOP_1.Models;
using Cell = OOP_1.Models.Cell;

namespace OOP_1
{
    public partial class MainPage : ContentPage
    {
        private readonly Spreadsheet _spreadsheet;
        private Cell _selectedCell;
        private CellAddress _selectedCellAddress;

        public MainPage()
        {
            InitializeComponent();

            _spreadsheet = new Spreadsheet();
            _selectedCellAddress = new CellAddress(0, 0);
            _selectedCell = _spreadsheet.GetCell(_selectedCellAddress);
            SelectedCellLabel.Text = _selectedCellAddress.ToString();

            BuildGrid();
        }

        void BuildGrid()
        {
            CellGrid.Children.Clear();
            CellGrid.RowDefinitions.Clear();
            CellGrid.ColumnDefinitions.Clear();

            CellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < _spreadsheet.RowCount; i++)
            {
                CellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            CellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < _spreadsheet.ColumnCount; i++)
            {
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            for (int c = 0; c < _spreadsheet.ColumnCount; c++)
            {
                var label = new Label
                {
                    Text = CellAddress.ColumnToName(c),
                    FontAttributes = FontAttributes.Bold,
                    Padding = new Thickness(10, 5),
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, c + 1);
                CellGrid.Children.Add(label);
            }

            for (int r = 0; r < _spreadsheet.RowCount; r++)
            {
                var label = new Label
                {
                    Text = (r + 1).ToString(),
                    FontAttributes = FontAttributes.Bold,
                    Padding = new Thickness(10, 5),
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, r + 1);
                Grid.SetColumn(label, 0);
                CellGrid.Children.Add(label);
            }

            bool showFormulas = ShowFormulaSwitch.IsToggled;

            for (int r = 0; r < _spreadsheet.RowCount; r++)
            {
                for (int c = 0; c < _spreadsheet.ColumnCount; c++)
                {
                    var cellModel = _spreadsheet.GetCell(r, c);
                    var cellAddress = new CellAddress(r, c);

                    var cellLabel = new Label
                    {
                        Padding = new Thickness(10, 5),
                        MinimumHeightRequest = 30,
                        MinimumWidthRequest = 80
                    };

                    string bindingPath = showFormulas ? nameof(Cell.Expression) : nameof(Cell.Value);
                    cellLabel.SetBinding(Label.TextProperty, new Binding(bindingPath, source: cellModel));

                    var cellBorder = new Border
                    {
                        Stroke = Colors.Gray,
                        StrokeThickness = 0.5,
                        Content = cellLabel
                    };

                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => OnCellTapped(cellModel, cellAddress);
                    cellBorder.GestureRecognizers.Add(tapGesture);

                    Grid.SetRow(cellBorder, r + 1);
                    Grid.SetColumn(cellBorder, c + 1);
                    CellGrid.Children.Add(cellBorder);
                }
            }
        }

        private async void OnCellTapped(Cell cell, CellAddress address)
        {
            _selectedCell = cell;
            _selectedCellAddress = address;

            SelectedCellLabel.Text = address.ToString();
            FormulaEntry.Text = cell.Expression;

            if (cell.Value is string errorValue && errorValue.StartsWith("#"))
            {
                // The error is displayed in the cell
            }
        }

        private async void OnFormulaEntryCompleted(object sender, EventArgs e)
        {
            if (_selectedCell == null) return;

            var newExpression = ((Entry)sender).Text;

            try
            {
                _spreadsheet.SetCellExpression(
                    _selectedCellAddress.Row,
                    _selectedCellAddress.Column,
                    newExpression
                );

                if (_selectedCell.Value is string errorValue && errorValue.StartsWith("#"))
                {
                    await DisplayAlert("Помилка",
                        $"Помилка у формулі: {errorValue}\n\nПеревірте правильність виразу.",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка",
                    $"Не вдалося обчислити вираз:\n{ex.Message}",
                    "ОК");
            }
        }

        private void OnAddRowClicked(object sender, EventArgs e)
        {
            _spreadsheet.AddRow();
            BuildGrid();
        }

        private void OnAddColClicked(object sender, EventArgs e)
        {
            _spreadsheet.AddColumn();
            BuildGrid();
        }

        private void OnShowFormulaToggled(object sender, ToggledEventArgs e)
        {
            BuildGrid();
        }
    }
}