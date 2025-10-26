using OOP_1.Models;
using OOP_1.Services;
using Cell = OOP_1.Models.Cell;

namespace OOP_1
{
    public partial class MainPage : ContentPage
    {
        private readonly Spreadsheet _spreadsheet;
        private Cell _selectedCell;
        private CellAddress _selectedCellAddress;
        private readonly GoogleDriveService _googleDriveService;
        private bool _isConnectedToGoogleDrive = false;

        public MainPage()
        {
            InitializeComponent();

            _spreadsheet = new Spreadsheet();
            _selectedCellAddress = new CellAddress(0, 0);
            _selectedCell = _spreadsheet.GetCell(_selectedCellAddress);
            SelectedCellLabel.Text = _selectedCellAddress.ToString();

            _googleDriveService = new GoogleDriveService();

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

        // ==================== НОВІ МЕТОДИ ДЛЯ GOOGLE DRIVE ====================

        private async void OnConnectToGoogleDriveClicked(object sender, EventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = false;
                ConnectButton.Text = "Підключення...";

                bool authenticated = await _googleDriveService.AuthenticateAsync();

                if (authenticated)
                {
                    _isConnectedToGoogleDrive = true;
                    SaveButton.IsEnabled = true;
                    LoadButton.IsEnabled = true;
                    ConnectButton.Text = "Підключено ✓";
                    ConnectButton.BackgroundColor = Colors.Green;

                    await DisplayAlert("Успіх",
                        "Успішно підключено до Google Drive!",
                        "ОК");
                }
                else
                {
                    ConnectButton.IsEnabled = true;
                    ConnectButton.Text = "Підключити Google Drive";
                    await DisplayAlert("Помилка",
                        "Не вдалося підключитися до Google Drive",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                ConnectButton.IsEnabled = true;
                ConnectButton.Text = "Підключити Google Drive";
                await DisplayAlert("Помилка",
                    $"Помилка підключення: {ex.Message}",
                    "ОК");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (!_isConnectedToGoogleDrive)
            {
                await DisplayAlert("Помилка",
                    "Спочатку підключіться до Google Drive",
                    "ОК");
                return;
            }

            try
            {
                string fileName = await DisplayPromptAsync("Зберегти файл",
                    "Введіть ім'я файлу:",
                    "Зберегти",
                    "Скасувати",
                    placeholder: "Моя таблиця",
                    initialValue: "Spreadsheet");

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                SaveButton.IsEnabled = false;
                SaveButton.Text = "Збереження...";

                string fileId = await _googleDriveService.SaveSpreadsheetAsync(_spreadsheet, fileName);

                SaveButton.IsEnabled = true;
                SaveButton.Text = "Зберегти";

                await DisplayAlert("Успіх",
                    $"Таблиця '{fileName}' успішно збережена на Google Drive!\nID файлу: {fileId}",
                    "ОК");
            }
            catch (Exception ex)
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "Зберегти";
                await DisplayAlert("Помилка",
                    $"Не вдалося зберегти файл: {ex.Message}",
                    "ОК");
            }
        }

        private async void OnLoadClicked(object sender, EventArgs e)
        {
            if (!_isConnectedToGoogleDrive)
            {
                await DisplayAlert("Помилка",
                    "Спочатку підключіться до Google Drive",
                    "ОК");
                return;
            }

            try
            {
                LoadButton.IsEnabled = false;
                LoadButton.Text = "Завантаження списку...";

                var files = await _googleDriveService.ListSpreadsheetFilesAsync();

                LoadButton.IsEnabled = true;
                LoadButton.Text = "Завантажити";

                if (files.Count == 0)
                {
                    await DisplayAlert("Інформація",
                        "На Google Drive не знайдено файлів з таблицями",
                        "ОК");
                    return;
                }

                // Показуємо список файлів для вибору
                var fileNames = files.Select(f =>
                    $"{f.Name} ({f.ModifiedTime?.ToString("dd.MM.yyyy HH:mm") ?? "?"})").ToArray();

                string selectedFileName = await DisplayActionSheet(
                    "Виберіть файл для завантаження",
                    "Скасувати",
                    null,
                    fileNames);

                if (string.IsNullOrEmpty(selectedFileName) || selectedFileName == "Скасувати")
                    return;

                int selectedIndex = Array.IndexOf(fileNames, selectedFileName);
                var selectedFile = files[selectedIndex];

                LoadButton.Text = "Завантаження...";
                LoadButton.IsEnabled = false;

                var spreadsheetData = await _googleDriveService.LoadSpreadsheetAsync(selectedFile.Id);

                // Очищуємо поточну таблицю
                _spreadsheet.Cells.Clear();

                // Завантажуємо дані
                foreach (var kvp in spreadsheetData.Cells)
                {
                    var address = CellAddress.FromString(kvp.Key);
                    _spreadsheet.SetCellExpression(address.Row, address.Column, kvp.Value);
                }

                // Оновлюємо розміри таблиці
                while (_spreadsheet.RowCount < spreadsheetData.RowCount)
                    _spreadsheet.AddRow();

                while (_spreadsheet.ColumnCount < spreadsheetData.ColumnCount)
                    _spreadsheet.AddColumn();

                BuildGrid();

                LoadButton.IsEnabled = true;
                LoadButton.Text = "Завантажити";

                await DisplayAlert("Успіх",
                    $"Таблиця '{selectedFile.Name}' успішно завантажена!",
                    "ОК");
            }
            catch (Exception ex)
            {
                LoadButton.IsEnabled = true;
                LoadButton.Text = "Завантажити";
                await DisplayAlert("Помилка",
                    $"Не вдалося завантажити файл: {ex.Message}",
                    "ОК");
            }
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Про програму",
                "Лабораторна робота 1\nВаріант 28\n\nВиконав: Сидоренко Артем\nГрупа: К-24",
                "ОК");
        }
    }
}