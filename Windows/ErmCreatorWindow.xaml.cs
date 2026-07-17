using AGR_Project_Manager.Models;
using AGR_Project_Manager.Services;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ISImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>;
using System.Linq;

namespace AGR_Project_Manager.Windows
{
    public partial class ErmCreatorWindow : Window
    {
        private const int PreviewSize = 256;
        private static readonly int[] ExportResolutions = { 128, 256, 512, 1024, 2048, 4096 };

        private readonly ErmCreationService _ermService = new();
        private readonly ErmChannelData _emission = new();
        private readonly ErmChannelData _roughness = new();
        private readonly ErmChannelData _metalness = new();

        public ErmCreatorWindow()
        {
            InitializeComponent();

            ResolutionComboBox.ItemsSource = ExportResolutions;
            ResolutionComboBox.SelectedItem = 1024;

            RefreshPreview();
        }

        #region Работа с каналами (общие для трёх блоков)

        private ErmChannelData GetChannel(string name) => name switch
        {
            "Emission" => _emission,
            "Roughness" => _roughness,
            "Metalness" => _metalness,
            _ => throw new ArgumentException($"Неизвестный канал: {name}")
        };

        private TextBlock GetPathText(string name) => name switch
        {
            "Emission" => EmissionPathText,
            "Roughness" => RoughnessPathText,
            "Metalness" => MetalnessPathText,
            _ => throw new ArgumentException($"Неизвестный канал: {name}")
        };

        private Slider GetSlider(string name) => name switch
        {
            "Emission" => EmissionSlider,
            "Roughness" => RoughnessSlider,
            "Metalness" => MetalnessSlider,
            _ => throw new ArgumentException($"Неизвестный канал: {name}")
        };

        private TextBox GetValueTextBox(string name) => name switch
        {
            "Emission" => EmissionValueTextBox,
            "Roughness" => RoughnessValueTextBox,
            "Metalness" => MetalnessValueTextBox,
            _ => throw new ArgumentException($"Неизвестный канал: {name}")
        };

        private void ChannelDropZone_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border { Tag: string channelName }) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.tga;*.tiff)|*.png;*.jpg;*.jpeg;*.tga;*.tiff|All files (*.*)|*.*",
                Title = $"Выберите текстуру для {channelName}"
            };

            if (dialog.ShowDialog() == true)
            {
                SetChannelTexture(channelName, dialog.FileName);
            }
        }

        private void ChannelDropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void ChannelDropZone_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border { Tag: string channelName }) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                SetChannelTexture(channelName, files[0]);
            }
        }

        private void SetChannelTexture(string channelName, string path)
        {
            var channel = GetChannel(channelName);
            channel.TexturePath = path;

            GetPathText(channelName).Text = System.IO.Path.GetFileName(path);
            GetSlider(channelName).IsEnabled = false;
            GetValueTextBox(channelName).IsEnabled = false;

            RefreshPreview();
        }

        private void ClearChannel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string channelName }) return;

            var channel = GetChannel(channelName);
            channel.TexturePath = null;

            GetPathText(channelName).Text = "Перетащите текстуру или кликните";

            var slider = GetSlider(channelName);
            slider.IsEnabled = true;
            slider.Value = channel.SolidValue;

            var textBox = GetValueTextBox(channelName);
            textBox.IsEnabled = true;
            textBox.Text = channel.SolidValue.ToString();

            RefreshPreview();
        }

        private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not Slider { Tag: string channelName } slider) return;

            var value = (byte)slider.Value;
            GetChannel(channelName).SolidValue = value;
            GetValueTextBox(channelName).Text = value.ToString();

            RefreshPreview();
        }

        private void RoughnessInvert_Changed(object sender, RoutedEventArgs e)
        {
            _roughness.Invert = RoughnessInvertCheckBox.IsChecked == true;
            RefreshPreview();
        }

        private void ChannelValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем вводить только цифры
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void ChannelValueTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                (sender as TextBox)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void ChannelValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox { Tag: string channelName } textBox) return;

            if (!int.TryParse(textBox.Text, out int parsed))
                parsed = 0;

            byte clamped = (byte)Math.Clamp(parsed, 0, 255);
            textBox.Text = clamped.ToString();

            // Slider.Value уже равен clamped, если пользователь ничего не менял — ValueChanged не перевызовется повторно,
            // поэтому дублируем запись в модель на случай расхождения
            GetSlider(channelName).Value = clamped;
            GetChannel(channelName).SolidValue = clamped;

            RefreshPreview();
        }

        #endregion

        #region Превью

        private void RefreshPreview()
        {
            using ISImage composed = _ermService.Compose(_emission, _roughness, _metalness, PreviewSize);
            PreviewImage.Source = ToBitmapSource(composed);
        }

        /// <summary>
        /// Быстрая конвертация Image&lt;Rgb24&gt; в BitmapSource для отображения в Image control
        /// </summary>
        private static BitmapSource ToBitmapSource(ISImage image)
        {
            int width = image.Width;
            int height = image.Height;
            var buffer = new byte[width * height * 3];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    int rowOffset = y * width * 3;
                    for (int x = 0; x < width; x++)
                    {
                        var px = row[x];
                        int idx = rowOffset + x * 3;
                        buffer[idx] = px.B;
                        buffer[idx + 1] = px.G;
                        buffer[idx + 2] = px.R;
                    }
                }
            });

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * 3, 0);
            bitmap.Freeze();
            return bitmap;
        }

        #endregion

        #region Сохранение

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResolutionComboBox.SelectedItem is not int size)
            {
                MessageBox.Show("Выберите разрешение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "ERM.png",
                Title = "Сохранить ERM-текстуру"
            };

            if (dialog.ShowDialog() != true) return;

            SaveButton.IsEnabled = false;
            try
            {
                // Захватываем текущее состояние каналов и считаем в фоновом потоке — при 4096 сборка не мгновенная
                var emission = _emission;
                var roughness = _roughness;
                var metalness = _metalness;
                string path = dialog.FileName;

                await System.Threading.Tasks.Task.Run(() =>
                {
                    using ISImage composed = _ermService.Compose(emission, roughness, metalness, size);
                    _ermService.Save(composed, path);
                });

                MessageBox.Show("ERM-текстура сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        #endregion
    }
}