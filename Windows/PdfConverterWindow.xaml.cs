using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AGR_Project_Manager.Services;
using Microsoft.Win32;

namespace AGR_Project_Manager.Windows
{
    /// <summary>
    /// Информация о PDF файле
    /// </summary>
    public class PdfFileInfo
    {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileName(FilePath);
        public int PageCount { get; set; }
        public long FileSize { get; set; }
        public string FileSizeDisplay => FormatSize(FileSize);
        public string Status { get; set; } = "Готов";

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    /// <summary>
    /// Окно конвертации PDF в изображения
    /// </summary>
    public partial class PdfConverterWindow : Window
    {
        private readonly ObservableCollection<PdfFileInfo> _pdfFiles;
        private bool _isProcessing;
        private string _lastOutputFolder; // Папка последнего успешно конвертированного файла

        public PdfConverterWindow()
        {
            InitializeComponent();

            _pdfFiles = new ObservableCollection<PdfFileInfo>();
            PdfFilesDataGrid.ItemsSource = _pdfFiles;

            // Обновляем видимость placeholder при изменении коллекции
            _pdfFiles.CollectionChanged += (s, e) => UpdatePlaceholderVisibility();
            UpdatePlaceholderVisibility();
        }

        #region Drag & Drop

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasPdf = files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                e.Effects = hasPdf ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var pdfFiles = files
                    .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (pdfFiles.Length > 0)
                {
                    AddPdfFiles(pdfFiles);

                    FolderPathTextBox.Text = Path.GetDirectoryName(pdfFiles[0]);
                }
            }
        }

        #endregion

        #region File Selection

        private void BrowseFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите PDF файлы",
                Filter = "PDF файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                AddPdfFiles(dialog.FileNames);

                if (dialog.FileNames.Length > 0)
                {
                    FolderPathTextBox.Text = Path.GetDirectoryName(dialog.FileNames[0]);
                }
            }

            this.Activate();
        }

        private void ClearListBtn_Click(object sender, RoutedEventArgs e)
        {
            _pdfFiles.Clear();
            _lastOutputFolder = null;
            UpdateStatistics();
        }

        private void AddPdfFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                // Проверяем, не добавлен ли уже
                if (_pdfFiles.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var pdfInfo = new PdfFileInfo
                {
                    FilePath = filePath
                };

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    pdfInfo.FileSize = fileInfo.Length;

                    // Получаем количество страниц с детальной ошибкой
                    int pageCount = PdfConversionService.GetPageCountWithError(filePath, out string error);

                    pdfInfo.PageCount = pageCount;

                    if (pageCount > 0)
                    {
                        pdfInfo.Status = "Готов";
                    }
                    else
                    {
                        pdfInfo.Status = $"Ошибка: {error ?? "неизвестно"}";
                    }
                }
                catch (Exception ex)
                {
                    pdfInfo.PageCount = 0;
                    pdfInfo.FileSize = 0;
                    pdfInfo.Status = $"Ошибка: {ex.Message}";
                }

                _pdfFiles.Add(pdfInfo);
            }

            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            TotalFilesText.Text = _pdfFiles.Count.ToString();
            TotalPagesText.Text = _pdfFiles.Sum(f => f.PageCount).ToString();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (DropPlaceholder != null)
            {
                DropPlaceholder.Visibility = _pdfFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Settings

        private void PagesRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (PageRangeTextBox != null)
            {
                PageRangeTextBox.IsEnabled = RangePagesRadio.IsChecked == true;
            }
        }

        private int GetSelectedDpi()
        {
            if (DpiComboBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string dpiStr &&
                int.TryParse(dpiStr, out int dpi))
            {
                return dpi;
            }
            return 300;
        }

        private List<int> GetSelectedPages(int maxPages)
        {
            if (AllPagesRadio.IsChecked == true)
            {
                return null;
            }

            return PdfConversionService.ParsePageRange(PageRangeTextBox.Text, maxPages);
        }

        #endregion

        #region Conversion

        private async void ConvertBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Конвертация уже выполняется", "Подождите",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_pdfFiles.Count == 0)
            {
                MessageBox.Show("Добавьте PDF файлы для конвертации", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var validFiles = _pdfFiles.Where(f => f.PageCount > 0).ToList();
            if (validFiles.Count == 0)
            {
                var errorDetails = string.Join("\n", _pdfFiles.Select(f => $"• {f.FileName}: {f.Status}"));
                MessageBox.Show(
                    $"Нет валидных PDF файлов для конвертации.\n\nДетали:\n{errorDetails}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _isProcessing = true;
            _lastOutputFolder = null; // Сбрасываем перед началом
            ShowProgress();

            int dpi = GetSelectedDpi();
            int totalConverted = 0;
            int totalErrors = 0;

            try
            {
                int fileIndex = 0;

                foreach (var pdfFile in validFiles)
                {
                    fileIndex++;
                    pdfFile.Status = "Конвертация...";
                    PdfFilesDataGrid.Items.Refresh();

                    var pages = GetSelectedPages(pdfFile.PageCount);

                    var progress = new Progress<PdfConversionProgress>(p =>
                    {
                        int overallPercent = ((fileIndex - 1) * 100 + p.PercentComplete) / validFiles.Count;
                        ConversionProgress.Value = overallPercent;
                        ProgressText.Text = $"{overallPercent}%";
                        ProgressStatusText.Text = $"{pdfFile.FileName}: {p.CurrentFileName}";
                    });

                    var result = await PdfConversionService.ConvertToPngAsync(
                        pdfFile.FilePath,
                        dpi,
                        pages,
                        progress
                    );

                    if (result.Success)
                    {
                        pdfFile.Status = $"✓ {result.ConvertedPages} стр.";
                        totalConverted += result.ConvertedPages;

                        // Запоминаем последнюю успешную папку
                        _lastOutputFolder = result.OutputFolder;
                    }
                    else
                    {
                        pdfFile.Status = $"✗ {result.ErrorMessage}";
                        totalErrors++;
                    }

                    PdfFilesDataGrid.Items.Refresh();
                }

                string message = $"Конвертация завершена!\n\n" +
                                 $"Файлов обработано: {validFiles.Count}\n" +
                                 $"Изображений создано: {totalConverted}";

                if (totalErrors > 0)
                {
                    message += $"\nОшибок: {totalErrors}";
                }

                // Предлагаем открыть папку только если есть успешные конвертации
                if (!string.IsNullOrEmpty(_lastOutputFolder) && Directory.Exists(_lastOutputFolder))
                {
                    var openFolder = MessageBox.Show(
                        message + "\n\nОткрыть папку с результатами?",
                        "Готово",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openFolder == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = _lastOutputFolder,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    MessageBox.Show(message, "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка конвертации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _isProcessing = false;
            }
        }

        #endregion

        #region Progress UI

        private void ShowProgress()
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ConversionProgress.Value = 0;
            ProgressText.Text = "0%";
            ProgressStatusText.Text = "Подготовка...";
            ConvertBtn.IsEnabled = false;
        }

        private void HideProgress()
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
            ConvertBtn.IsEnabled = true;
        }

        #endregion
    }
}