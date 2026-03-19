using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AGR_Project_Manager.Services;

namespace AGR_Project_Manager.Windows
{
    /// <summary>
    /// Окно конвертера координат WGS84 → МСК-77
    /// </summary>
    public partial class CoordinateConverterWindow : Window
    {
        private readonly CoordinateConversionService _conversionService;
        private double _lastResultX;
        private double _lastResultY;
        private bool _hasResult;

        // Цвета для индикации
        private static readonly SolidColorBrush CopiedBrush = new(Color.FromRgb(76, 175, 80)); // Зелёный
        private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

        public CoordinateConverterWindow()
        {
            InitializeComponent();
            _conversionService = new CoordinateConversionService();

            // Автоконвертация при загрузке
            Loaded += (s, e) => Convert();
        }

        #region Conversion

        private void ConvertBtn_Click(object sender, RoutedEventArgs e)
        {
            Convert();
        }

        private void CoordsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Convert();
            }
        }

        private void Convert()
        {
            string input = CoordsTextBox.Text;

            // Сбрасываем состояние
            HideError();
            HideResult();
            _hasResult = false;

            // Парсим координаты
            var coords = CoordinateConversionService.ParseCoordinates(input);

            if (coords == null)
            {
                ShowError("❌ Не удалось распознать координаты");
                return;
            }

            // Проверяем валидность
            if (!CoordinateConversionService.ValidateCoordinates(coords.Latitude, coords.Longitude))
            {
                ShowError("❌ Координаты вне допустимого диапазона");
                return;
            }

            // Получаем поправки
            double corrX = ParseDouble(CorrXTextBox.Text, -5.0);
            double corrY = ParseDouble(CorrYTextBox.Text, -35.0);

            try
            {
                // Конвертируем
                var (x, y) = _conversionService.ConvertToMsk77(coords.Latitude, coords.Longitude, corrX, corrY);

                _lastResultX = x;
                _lastResultY = y;
                _hasResult = true;

                // Показываем распознанные координаты
                ParsedLatText.Text = coords.Latitude.ToString("F6", CultureInfo.InvariantCulture);
                ParsedLonText.Text = coords.Longitude.ToString("F6", CultureInfo.InvariantCulture);

                ShowResult();
            }
            catch (Exception ex)
            {
                ShowError($"❌ Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region Settings

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (_hasResult && IsLoaded)
            {
                UpdateResultDisplay();
            }
        }

        private void Correction_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_hasResult && IsLoaded)
            {
                Convert();
            }
        }

        private bool UseComma => CommaSeparator?.IsChecked == true;
        private int Decimals => Decimals2?.IsChecked == true ? 2 : 3;

        private static double ParseDouble(string text, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            text = text.Replace(',', '.');
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            return defaultValue;
        }

        #endregion

        #region Copy

        private void ResultX_Click(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboard(_lastResultX, ResultXBorder);
        }

        private void ResultY_Click(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboard(_lastResultY, ResultYBorder);
        }

        private void CopyToClipboard(double value, System.Windows.Controls.Border border)
        {
            string text = CoordinateConversionService.FormatNumber(value, Decimals, UseComma);
            Clipboard.SetText(text);
            ShowCopyFeedback(border);
        }

        private void ShowCopyFeedback(System.Windows.Controls.Border border)
        {
            // Мгновенная подсветка зелёным
            border.BorderBrush = CopiedBrush;

            // Сброс через 400мс
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            timer.Tick += (s, e) =>
            {
                border.BorderBrush = TransparentBrush;
                timer.Stop();
            };
            timer.Start();
        }

        #endregion

        #region UI Helpers

        private void ShowResult()
        {
            UpdateResultDisplay();
            ResultSection.Visibility = Visibility.Visible;
        }

        private void UpdateResultDisplay()
        {
            string xText = CoordinateConversionService.FormatNumber(_lastResultX, Decimals, UseComma);
            string yText = CoordinateConversionService.FormatNumber(_lastResultY, Decimals, UseComma);

            ResultXText.Text = $"{xText} м";
            ResultYText.Text = $"{yText} м";
        }

        private void HideResult()
        {
            ResultSection.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorSection.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorSection.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}