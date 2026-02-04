using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AGR_Project_Manager.Data;
using AGR_Project_Manager.Models;
using AGR_Project_Manager.Services;
using Ookii.Dialogs.Wpf;

namespace AGR_Project_Manager.Windows
{
    public partial class RalColorsWindow : Window
    {
        private readonly RalColorService _colorService;
        private RalColor _currentColor;
        private List<RalColor> _searchResults;
        private int _currentResultIndex;

        public RalColorsWindow()
        {
            InitializeComponent();
            _colorService = new RalColorService();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Управление видимостью плейсхолдера
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            // Автопоиск при вводе
            string searchText = SearchTextBox.Text.Trim();

            if (searchText.Length >= 3)
            {
                PerformSearch(searchText);
            }
            else if (string.IsNullOrEmpty(searchText))
            {
                HideResult();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch(SearchTextBox.Text.Trim());
                e.Handled = true;
            }
            else if (e.Key == Key.Down && _searchResults != null && _searchResults.Count > 1)
            {
                // Переключение между результатами
                _currentResultIndex = (_currentResultIndex + 1) % _searchResults.Count;
                DisplayColor(_searchResults[_currentResultIndex]);
                e.Handled = true;
            }
            else if (e.Key == Key.Up && _searchResults != null && _searchResults.Count > 1)
            {
                _currentResultIndex = (_currentResultIndex - 1 + _searchResults.Count) % _searchResults.Count;
                DisplayColor(_searchResults[_currentResultIndex]);
                e.Handled = true;
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch(SearchTextBox.Text.Trim());
        }

        private void PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                HideResult();
                return;
            }

            // Убираем "RAL " если пользователь его ввёл
            query = query.ToUpper().Replace("RAL", "").Trim();

            // Ищем по номеру или названию
            _searchResults = RalDatabase.Colors
                .Where(c => c.Code.ToUpper().Contains(query) ||
                           c.Name.ToUpper().Contains(query))
                .ToList();

            if (_searchResults.Any())
            {
                _currentResultIndex = 0;
                DisplayColor(_searchResults[0]);

                // Показываем подсказку если найдено несколько
                if (_searchResults.Count > 1)
                {
                    ColorNameText.Text += $" (↑↓ ещё {_searchResults.Count - 1})";
                }
            }
            else
            {
                HideResult();
                PlaceholderText.Text = $"Цвет RAL {query} не найден";
            }
        }

        private void DisplayColor(RalColor color)
        {
            _currentColor = color;

            // Показываем панели
            PlaceholderText.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            SavePanel.Visibility = Visibility.Visible;

            // Устанавливаем цвет плитки
            var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            ColorTile.Background = brush;

            // Устанавливаем текст
            ColorCodeText.Text = color.Code;
            ColorNameText.Text = color.Name;
            HexText.Text = color.Hex;
            RgbText.Text = $"{color.R}, {color.G}, {color.B}";
        }

        private void HideResult()
        {
            _currentColor = null;
            PlaceholderText.Text = "Введите номер RAL для поиска";
            PlaceholderText.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;
            SavePanel.Visibility = Visibility.Collapsed;
        }

        private void CopyHex_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentColor == null) return;

            try
            {
                Clipboard.SetText(_currentColor.Hex);

                // Визуальная обратная связь
                string originalText = HexText.Text;
                HexText.Text = "✓ Скопировано!";
                HexText.Foreground = new SolidColorBrush(Colors.LightGreen);

                // Возвращаем через 1 секунду
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += (s, args) =>
                {
                    HexText.Text = originalText;
                    HexText.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#6a9955"));
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePng_Click(object sender, RoutedEventArgs e)
        {
            if (_currentColor == null) return;

            // Получаем размер
            int size = GetSelectedSize();

            // Диалог выбора папки
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для сохранения",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                string fileName = _colorService.GenerateFileName(_currentColor, size);
                string filePath = System.IO.Path.Combine(dialog.SelectedPath, fileName);

                if (_colorService.SaveColorAsPng(_currentColor, filePath, size))
                {
                    MessageBox.Show($"Сохранено: {fileName}\nРазмер: {size}x{size} px",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения файла", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int GetSelectedSize()
        {
            return SizeComboBox.SelectedIndex switch
            {
                0 => 256,
                1 => 512,
                2 => 2048,
                3 => 4096,
                _ => 512
            };
        }
    }
}