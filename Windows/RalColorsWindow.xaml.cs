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
using System.Windows.Threading;

namespace AGR_Project_Manager.Windows
{
    public partial class RalColorsWindow : Window
    {
        private readonly RalColorService _colorService;
        private RalColor _currentColor;
        private List<RalColor> _searchResults;
        private int _currentResultIndex;
        private enum ColorInputMode { Ral, Hex, Kelvin }
        private ColorInputMode _mode = ColorInputMode.Ral;

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

        #region RAL Color Operations

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

        #endregion

        #region Display & Save

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
            HexText.Text = color.Hex.TrimStart('#'); // "#" рисуется отдельным TextBlock рядом
            RgbText.Text = $"{color.R}, {color.G}, {color.B}";
        }

        private void HideResult()
        {
            _currentColor = null;
            PlaceholderText.Text = _mode switch
            {
                ColorInputMode.Hex => "Введите HEX код для поиска",
                ColorInputMode.Kelvin => "Задайте цветовую температуру",
                _ => "Введите номер RAL для поиска"
            };
            PlaceholderText.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;
            SavePanel.Visibility = Visibility.Collapsed;
        }

        private DispatcherTimer _copyToastTimer;

        private void CopyHexWithHash_Click(object sender, MouseButtonEventArgs e)
        {
            CopyHexToClipboard(withHash: true);
            e.Handled = true;
        }

        private void CopyHexWithoutHash_Click(object sender, MouseButtonEventArgs e)
        {
            CopyHexToClipboard(withHash: false);
            e.Handled = true;
        }

        private void CopyHexToClipboard(bool withHash)
        {
            if (_currentColor == null) return;

            string hexNoHash = _currentColor.Hex.TrimStart('#');
            string textToCopy = withHash ? $"#{hexNoHash}" : hexNoHash;

            try
            {
                Clipboard.SetText(textToCopy);
                ShowCopyToast(textToCopy);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показывает всплывающую подсказку "Скопировано" над HEX-блоком на ~1.2 сек
        /// </summary>
        private void ShowCopyToast(string copiedText)
        {
            CopyToastText.Text = $"✓ Скопировано: {copiedText}";
            CopyToast.IsOpen = true;

            _copyToastTimer?.Stop();
            _copyToastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            _copyToastTimer.Tick += (s, args) =>
            {
                CopyToast.IsOpen = false;
                _copyToastTimer.Stop();
            };
            _copyToastTimer.Start();
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
                // Генерируем имя файла в зависимости от режима
                string fileName = _mode switch
                {
                    ColorInputMode.Hex => _colorService.GenerateFileNameForHex(_currentColor.Hex, size),
                    ColorInputMode.Kelvin => _colorService.GenerateFileNameForKelvin(GetCurrentKelvinValue(), size),
                    _ => _colorService.GenerateFileName(_currentColor, size)
                };

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

        #endregion

        #region HEX Color Operations

        private void ModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Защита: RadioButton с IsChecked="True" в XAML вызывает Checked
            // ещё во время InitializeComponent(), пока нижестоящие элементы = null
            if (RalInputPanel == null || HexInputPanel == null || KelvinInputPanel == null)
                return;

            if (sender == ModeRalRadio) _mode = ColorInputMode.Ral;
            else if (sender == ModeHexRadio) _mode = ColorInputMode.Hex;
            else if (sender == ModeKelvinRadio) _mode = ColorInputMode.Kelvin;

            RalInputPanel.Visibility = _mode == ColorInputMode.Ral ? Visibility.Visible : Visibility.Collapsed;
            HexInputPanel.Visibility = _mode == ColorInputMode.Hex ? Visibility.Visible : Visibility.Collapsed;
            KelvinInputPanel.Visibility = _mode == ColorInputMode.Kelvin ? Visibility.Visible : Visibility.Collapsed;

            HideResult();
            ClearInputs();

            if (_mode == ColorInputMode.Kelvin)
                PerformKelvinSearch((int)KelvinSlider.Value);
        }

        private void ClearInputs()
        {
            SearchTextBox.Clear();
            HexInputTextBox.Clear();
            SearchPlaceholder.Visibility = Visibility.Visible;
            HexPlaceholder.Visibility = Visibility.Visible;
        }

        private void HexInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Управление видимостью плейсхолдера
            HexPlaceholder.Visibility = string.IsNullOrEmpty(HexInputTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void HexInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidateAndDisplayHexColor(HexInputTextBox.Text.Trim());
                e.Handled = true;
            }
        }

        private void ValidateHexBtn_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndDisplayHexColor(HexInputTextBox.Text.Trim());
        }

        private void ValidateAndDisplayHexColor(string hexCode)
        {
            if (string.IsNullOrWhiteSpace(hexCode))
            {
                HideResult();
                PlaceholderText.Text = "Введите HEX код для поиска";
                return;
            }

            if (!_colorService.ValidateHexCode(hexCode))
            {
                HideResult();
                PlaceholderText.Text = "Некорректный HEX код. Используйте формат: #RRGGBB или RRGGBB";
                PlaceholderText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var color = _colorService.ParseHexColor(hexCode);
                DisplayColor(color);
            }
            catch (Exception ex)
            {
                HideResult();
                PlaceholderText.Text = $"Ошибка: {ex.Message}";
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Kelvin Color Operations

        private void KelvinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Защита: событие может сработать при парсинге XAML, ещё до выбора режима
            if (_mode != ColorInputMode.Kelvin) return;

            int kelvin = (int)e.NewValue;
            KelvinTextBox.Text = kelvin.ToString();
            PerformKelvinSearch(kelvin);
        }

        private void KelvinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (int.TryParse(KelvinTextBox.Text.Trim(), out int kelvin))
            {
                kelvin = Math.Clamp(kelvin, (int)KelvinSlider.Minimum, (int)KelvinSlider.Maximum);
                KelvinSlider.Value = kelvin; // вызовет KelvinSlider_ValueChanged и обновит превью
            }
            else
            {
                MessageBox.Show("Введите целое число от 2000 до 12000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        private void PerformKelvinSearch(int kelvin)
        {
            var color = _colorService.FromKelvin(kelvin);
            DisplayColor(color);
        }

        private int GetCurrentKelvinValue()
        {
            return int.TryParse(KelvinTextBox.Text.Trim(), out int kelvin)
                ? kelvin
                : (int)KelvinSlider.Value;
        }

        #endregion
    }
}
