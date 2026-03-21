using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AGR_Project_Manager.Models;
using AGR_Project_Manager.Properties;
using AGR_Project_Manager.Services;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace AGR_Project_Manager.Windows
{
    /// <summary>
    /// Окно управления текстурами
    /// </summary>
    public partial class TextureManagerWindow : Window
    {
        private readonly TextureAnalysisService _analysisService;
        private readonly TextureConversionService _conversionService;
        private readonly ObservableCollection<TextureInfo> _textures;
        private bool _isProcessing;
        private string _lastFolder;

        // FileSystemWatcher для автообновления
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly HashSet<string> _watchedFolders = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource _refreshCts;
        private readonly object _refreshLock = new();

        // Отложенное удаление (для Photoshop и других редакторов с атомарным сохранением)
        private readonly Dictionary<string, CancellationTokenSource> _pendingDeletions = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _deletionLock = new();

        // Фильтр файлов изображений
        private const string ImageFilter = "Изображения|*.png;*.jpg;*.jpeg;*.tga;*.tiff;*.tif;*.bmp;*.gif;*.webp|" +
                                           "PNG файлы (*.png)|*.png|" +
                                           "JPEG файлы (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                                           "TGA файлы (*.tga)|*.tga|" +
                                           "TIFF файлы (*.tiff;*.tif)|*.tiff;*.tif|" +
                                           "Все файлы (*.*)|*.*";

        public TextureManagerWindow()
        {
            InitializeComponent();

            _analysisService = new TextureAnalysisService();
            _conversionService = new TextureConversionService();
            _textures = new ObservableCollection<TextureInfo>();

            TexturesDataGrid.ItemsSource = _textures;

            // Горячая клавиша F5
            this.KeyDown += Window_KeyDown;
        }

        /// <summary>
        /// Конструктор с указанием начальной папки
        /// </summary>
        public TextureManagerWindow(string initialFolder) : this()
        {
            if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
            {
                _lastFolder = initialFolder;
            }
        }

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            StopAllWatchers();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                RefreshBtn_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Загружаем последнюю папку
                string savedFolder = Settings.Default.TextureManagerLastFolder;
                if (!string.IsNullOrEmpty(savedFolder) && Directory.Exists(savedFolder))
                {
                    _lastFolder = savedFolder;
                    FolderPathTextBox.Text = savedFolder;
                }

                // Загружаем последние файлы
                string savedFiles = Settings.Default.TextureManagerLastFiles;
                if (!string.IsNullOrEmpty(savedFiles))
                {
                    var files = savedFiles.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(File.Exists)
                        .ToList();

                    if (files.Count > 0)
                    {
                        _ = AddFilesAsync(files);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Сохраняем последнюю папку
                Settings.Default.TextureManagerLastFolder = _lastFolder ?? "";

                // Сохраняем пути к файлам (максимум 100 для экономии места)
                var filePaths = _textures
                    .Take(100)
                    .Select(t => t.FilePath)
                    .Where(p => !string.IsNullOrEmpty(p));
                Settings.Default.TextureManagerLastFiles = string.Join("|", filePaths);

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        #endregion

        #region File Selection

        private async void BrowseFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файлы изображений",
                Filter = ImageFilter,
                Multiselect = true,
                InitialDirectory = GetInitialDirectory()
            };

            if (dialog.ShowDialog(this) == true)
            {
                // Запоминаем папку
                if (dialog.FileNames.Length > 0)
                {
                    _lastFolder = Path.GetDirectoryName(dialog.FileNames[0]);
                    FolderPathTextBox.Text = _lastFolder;
                }

                await AddFilesAsync(dialog.FileNames.ToList());
            }

            this.Activate();
            this.Focus();
        }

        private async void BrowseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку с текстурами",
                UseDescriptionForTitle = true,
                SelectedPath = GetInitialDirectory()
            };

            if (dialog.ShowDialog(this) == true)
            {
                _lastFolder = dialog.SelectedPath;
                FolderPathTextBox.Text = _lastFolder;

                // Получаем все файлы изображений из папки
                var files = Directory.GetFiles(dialog.SelectedPath)
                    .Where(f => TextureAnalysisService.IsSupportedImage(f))
                    .ToList();

                if (files.Count > 0)
                {
                    await AddFilesAsync(files);
                }
                else
                {
                    MessageBox.Show("В выбранной папке не найдено поддерживаемых изображений",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            this.Activate();
            this.Focus();
        }

        private void ClearListBtn_Click(object sender, RoutedEventArgs e)
        {
            _textures.Clear();
            StopAllWatchers();
            _watchedFolders.Clear();
            UpdateStatistics();
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing || _textures.Count == 0) return;

            await RefreshAllFilesAsync();
        }

        private string GetInitialDirectory()
        {
            if (!string.IsNullOrEmpty(_lastFolder) && Directory.Exists(_lastFolder))
            {
                return _lastFolder;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private async Task AddFilesAsync(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            _isProcessing = true;
            ShowProgress("Анализ файлов...");

            try
            {
                // Фильтруем уже добавленные файлы
                var existingPaths = new HashSet<string>(_textures.Select(t => t.FilePath), StringComparer.OrdinalIgnoreCase);
                var newFiles = filePaths.Where(f => !existingPaths.Contains(f)).ToList();

                if (newFiles.Count == 0)
                {
                    MessageBox.Show("Все выбранные файлы уже добавлены в список",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int processed = 0;
                int total = newFiles.Count;

                // Собираем папки для отслеживания
                var foldersToWatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    foreach (var file in newFiles)
                    {
                        var info = TextureAnalysisService.AnalyzeFile(file);
                        var folder = Path.GetDirectoryName(file);

                        if (!string.IsNullOrEmpty(folder))
                        {
                            foldersToWatch.Add(folder);
                        }

                        // Добавляем в UI-потоке
                        Dispatcher.Invoke(() =>
                        {
                            _textures.Add(info);

                            processed++;
                            int percent = (processed * 100) / total;
                            OperationProgress.Value = percent;
                            ProgressText.Text = $"{percent}%";
                            OperationStatusText.Text = $"Анализ: {info.FileName}";
                        });
                    }
                });

                // Настраиваем FileSystemWatcher для новых папок
                if (AutoRefreshCheckBox.IsChecked == true)
                {
                    foreach (var folder in foldersToWatch)
                    {
                        SetupWatcherForFolder(folder);
                    }
                }

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа файлов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _isProcessing = false;
            }
        }

        private void UpdateStatistics()
        {
            var stats = TextureAnalysisService.GetFolderStats(_textures);

            TotalFilesText.Text = stats.TotalFiles.ToString();
            TotalSizeText.Text = stats.TotalSizeFormatted;
            OkCountText.Text = stats.FilesOk.ToString();
            WarningCountText.Text = stats.FilesWarning.ToString();
            ErrorCountText.Text = stats.FilesError.ToString();
        }

        #endregion

        #region Double Click - Open File

        private void TexturesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TexturesDataGrid.SelectedItem is TextureInfo texture)
            {
                OpenFile(texture.FilePath);
            }
        }

        private void OpenFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Auto-Refresh (FileSystemWatcher)

        private void AutoRefreshCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_textures == null) return;

            if (AutoRefreshCheckBox.IsChecked == true)
            {
                // Включаем отслеживание для всех текущих папок
                var folders = _textures
                    .Select(t => Path.GetDirectoryName(t.FilePath))
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var folder in folders)
                {
                    SetupWatcherForFolder(folder);
                }
            }
            else
            {
                // Отключаем все watchers
                StopAllWatchers();
            }
        }

        private void SetupWatcherForFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            if (_watchedFolders.Contains(folderPath))
                return;

            try
            {
                var watcher = new FileSystemWatcher(folderPath)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                watcher.Changed += OnFileChanged;
                watcher.Renamed += OnFileRenamed;
                watcher.Deleted += OnFileDeleted;
                watcher.Created += OnFileCreated;

                _watchers.Add(watcher);
                _watchedFolders.Add(folderPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка создания FileSystemWatcher: {ex.Message}");
            }
        }

        private void StopAllWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
                watcher.Deleted -= OnFileDeleted;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Проверяем, что файл есть в нашем списке
            if (!TextureAnalysisService.IsSupportedImage(e.FullPath))
                return;

            var texture = _textures.FirstOrDefault(t =>
                t.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));

            if (texture != null)
            {
                // Debounce — ждём 500ms перед обновлением
                ScheduleRefresh(e.FullPath);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var texture = _textures.FirstOrDefault(t =>
                    t.FilePath.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase));

                if (texture != null)
                {
                    // Удаляем старую запись и добавляем новую
                    int index = _textures.IndexOf(texture);
                    _textures.RemoveAt(index);

                    if (File.Exists(e.FullPath) && TextureAnalysisService.IsSupportedImage(e.FullPath))
                    {
                        var updated = TextureAnalysisService.AnalyzeFile(e.FullPath);
                        _textures.Insert(index, updated);
                    }

                    UpdateStatistics();
                }
            });
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!TextureAnalysisService.IsSupportedImage(e.FullPath))
                return;

            // Проверяем, есть ли этот файл в нашем списке
            bool isTracked = false;
            Dispatcher.Invoke(() =>
            {
                isTracked = _textures.Any(t =>
                    t.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
            });

            if (!isTracked)
                return;

            // Отложенное удаление — ждём 1.5 секунды и проверяем, вернулся ли файл
            lock (_deletionLock)
            {
                // Отменяем предыдущее ожидание для этого файла, если есть
                if (_pendingDeletions.TryGetValue(e.FullPath, out var existingCts))
                {
                    existingCts.Cancel();
                    _pendingDeletions.Remove(e.FullPath);
                }

                var cts = new CancellationTokenSource();
                _pendingDeletions[e.FullPath] = cts;
                var token = cts.Token;
                string filePath = e.FullPath;

                Task.Delay(1500, token).ContinueWith(t =>
                {
                    if (t.IsCanceled) return;

                    Dispatcher.Invoke(() =>
                    {
                        lock (_deletionLock)
                        {
                            // Проверяем, всё ещё ли файл ожидает удаления
                            if (!_pendingDeletions.ContainsKey(filePath))
                                return;

                            _pendingDeletions.Remove(filePath);

                            // Проверяем, существует ли файл
                            if (File.Exists(filePath))
                            {
                                // Файл вернулся! (Photoshop workflow) — обновляем информацию
                                Debug.WriteLine($"Файл вернулся после удаления: {filePath}");
                                RefreshSingleFile(filePath);
                            }
                            else
                            {
                                // Файл действительно удалён — удаляем из списка
                                var texture = _textures.FirstOrDefault(tx =>
                                    tx.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                                if (texture != null)
                                {
                                    _textures.Remove(texture);
                                    UpdateStatistics();
                                    Debug.WriteLine($"Файл удалён из списка: {filePath}");
                                }
                            }
                        }
                    });
                }, token);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!TextureAnalysisService.IsSupportedImage(e.FullPath))
                return;

            lock (_deletionLock)
            {
                // Проверяем, ожидает ли этот файл удаления (Photoshop workflow)
                if (_pendingDeletions.TryGetValue(e.FullPath, out var cts))
                {
                    // Отменяем отложенное удаление — файл вернулся!
                    cts.Cancel();
                    _pendingDeletions.Remove(e.FullPath);

                    // Обновляем файл с задержкой (чтобы файл был полностью записан)
                    ScheduleRefresh(e.FullPath);
                }
            }
        }

        private void ScheduleRefresh(string filePath)
        {
            lock (_refreshLock)
            {
                // Отменяем предыдущий запрос на обновление
                _refreshCts?.Cancel();
                _refreshCts = new CancellationTokenSource();
                var token = _refreshCts.Token;

                Task.Delay(500, token).ContinueWith(t =>
                {
                    if (t.IsCanceled) return;

                    Dispatcher.Invoke(() =>
                    {
                        RefreshSingleFile(filePath);
                    });
                }, token);
            }
        }

        private void RefreshSingleFile(string filePath)
        {
            // Ждём, пока файл станет доступен (максимум 3 попытки)
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (!File.Exists(filePath)) return;

                try
                {
                    // Пробуем открыть файл для проверки доступности
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Файл доступен — выходим из цикла
                        break;
                    }
                }
                catch (IOException)
                {
                    // Файл ещё заблокирован — ждём
                    System.Threading.Thread.Sleep(300);
                    if (attempt == 2) return; // Последняя попытка неудачна
                }
            }

            var texture = _textures.FirstOrDefault(t =>
                t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            if (texture != null)
            {
                try
                {
                    int index = _textures.IndexOf(texture);
                    var updated = TextureAnalysisService.AnalyzeFile(filePath);
                    _textures[index] = updated;
                    UpdateStatistics();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка обновления файла: {ex.Message}");
                }
            }
        }

        private async Task RefreshAllFilesAsync()
        {
            if (_textures.Count == 0) return;

            _isProcessing = true;
            ShowProgress("Обновление файлов...");

            try
            {
                var filePaths = _textures.Select(t => t.FilePath).ToList();
                int processed = 0;
                int total = filePaths.Count;

                await Task.Run(() =>
                {
                    foreach (var filePath in filePaths)
                    {
                        if (File.Exists(filePath))
                        {
                            var updated = TextureAnalysisService.AnalyzeFile(filePath);

                            Dispatcher.Invoke(() =>
                            {
                                var existing = _textures.FirstOrDefault(t =>
                                    t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                                if (existing != null)
                                {
                                    int index = _textures.IndexOf(existing);
                                    _textures[index] = updated;
                                }

                                processed++;
                                int percent = (processed * 100) / total;
                                OperationProgress.Value = percent;
                                ProgressText.Text = $"{percent}%";
                            });
                        }
                        else
                        {
                            // Файл удалён — удаляем из списка
                            Dispatcher.Invoke(() =>
                            {
                                var existing = _textures.FirstOrDefault(t =>
                                    t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                                if (existing != null)
                                {
                                    _textures.Remove(existing);
                                }

                                processed++;
                            });
                        }
                    }
                });

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _isProcessing = false;
            }
        }

        #endregion

        #region Selection

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            TexturesDataGrid.SelectAll();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            TexturesDataGrid.UnselectAll();
        }

        private List<TextureInfo> GetSelectedTextures()
        {
            return TexturesDataGrid.SelectedItems.Cast<TextureInfo>().ToList();
        }

        #endregion

        #region Conversion Actions

        private async void FlattenColor_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            // Фильтруем только заглушки 256×256 с несколькими цветами
            var stubsToFlatten = selected.Where(t => t.NeedsColorFlattening).ToList();

            if (stubsToFlatten.Count == 0)
            {
                // Проверяем, есть ли вообще заглушки среди выбранных
                var allStubs = selected.Where(t => t.IsStubTexture).ToList();

                if (allStubs.Count == 0)
                {
                    MessageBox.Show("Среди выбранных файлов нет текстур 256×256",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Все выбранные заглушки 256×256 уже одноцветные",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            var result = MessageBox.Show(
                $"Выровнять цвет у {stubsToFlatten.Count} заглушек 256×256?\n\n" +
                "Каждая текстура будет залита своим доминантным цветом.\n\n" +
                "⚠️ Файлы будут перезаписаны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(stubsToFlatten, async (texture) =>
            {
                return await TextureConversionService.FlattenToDominantColorAsync(texture.FilePath);
            }, "Выравнивание цвета");
        }

        private async void ConvertTo8BitPng_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            var result = MessageBox.Show(
                $"Конвертировать {selected.Count} файл(ов) в 8-bit PNG?\n\nФайлы будут перезаписаны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(selected, async (texture) =>
            {
                var options = new TextureProcessingOptions
                {
                    ConvertTo8Bit = true,
                    ConvertToPng = true,
                    OptimizeCompression = true
                };
                return await _conversionService.ProcessTextureAsync(texture.FilePath, options);
            }, "Конвертация в 8-bit PNG");
        }

        private async void RemoveAlpha_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            var withAlpha = selected.Where(t => t.HasAlpha).ToList();
            if (withAlpha.Count == 0)
            {
                MessageBox.Show("Среди выбранных файлов нет изображений с альфа-каналом",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить альфа-канал у {withAlpha.Count} файл(ов)?\n\nФайлы будут перезаписаны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(withAlpha, async (texture) =>
            {
                return await TextureConversionService.RemoveAlphaAsync(texture.FilePath);
            }, "Удаление альфа-канала");
        }

        private async void OptimizePng_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            // Получаем выбранный режим
            var selectedItem = OptimizationModeCombo.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is not string modeStr)
            {
                modeStr = "LightBlur";
            }

            var mode = modeStr switch
            {
                "Recompress" => Services.OptimizationMode.Recompress,
                "LightBlur" => Services.OptimizationMode.LightBlur,
                "MediumBlur" => Services.OptimizationMode.MediumBlur,
                "StrongBlur" => Services.OptimizationMode.StrongBlur,
                "Denoise" => Services.OptimizationMode.Denoise,
                _ => Services.OptimizationMode.LightBlur
            };

            // Описание режима
            string description = mode switch
            {
                Services.OptimizationMode.Recompress =>
                    "Только пересжатие с максимальной компрессией.\nИзображение не изменяется. Эффект минимальный.",
                Services.OptimizationMode.LightBlur =>
                    "Лёгкое размытие по Гауссу (σ=0.4).\nПочти незаметно, уменьшает размер на 10-30%.",
                Services.OptimizationMode.MediumBlur =>
                    "Среднее размытие по Гауссу (σ=0.8).\nНемного заметно, уменьшает размер на 20-50%.",
                Services.OptimizationMode.StrongBlur =>
                    "Сильное размытие по Гауссу (σ=1.5).\nЗаметное размытие, уменьшает размер на 40-70%.",
                Services.OptimizationMode.Denoise =>
                    "Шумоподавление (размытие + контраст).\nУбирает мелкий шум, сохраняя контрастность.",
                _ => ""
            };

            var result = MessageBox.Show(
                $"Обработать {selected.Count} файл(ов)?\n\n{description}\n\n⚠️ Файлы будут перезаписаны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(selected, async (texture) =>
            {
                return await TextureConversionService.OptimizePngAsync(texture.FilePath, mode);
            }, $"Оптимизация ({modeStr})");
        }

        private async void ConvertToPng_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            var nonPng = selected.Where(t => t.WrongFormat).ToList();
            if (nonPng.Count == 0)
            {
                MessageBox.Show("Все выбранные файлы уже в формате PNG",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Конвертировать {nonPng.Count} файл(ов) в PNG?\n\nБудут созданы новые файлы с расширением .png",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(nonPng, async (texture) =>
            {
                return await TextureConversionService.ConvertToPngAsync(texture.FilePath);
            }, "Конвертация в PNG");
        }

        #endregion

        #region Resize Actions

        private async void ApplyResize_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTextures();
            if (!ValidateSelection(selected)) return;

            if (ResizePresetCombo.SelectedIndex <= 0)
            {
                MessageBox.Show("Выберите размер из списка", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = ResizePresetCombo.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is not string sizeStr || !int.TryParse(sizeStr, out int size))
            {
                return;
            }

            var result = MessageBox.Show(
                $"Изменить размер {selected.Count} файл(ов) на {size}×{size}?\n\nФайлы будут перезаписаны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await ProcessTexturesAsync(selected, async (texture) =>
            {
                return await TextureConversionService.ResizeAsync(texture.FilePath, size, size);
            }, $"Ресайз до {size}×{size}");
        }

        #endregion

        #region Processing Helper

        private bool ValidateSelection(List<TextureInfo> selected)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Дождитесь завершения текущей операции", "Подождите",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (selected.Count == 0)
            {
                MessageBox.Show("Выберите файлы для обработки", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async Task ProcessTexturesAsync(
            List<TextureInfo> textures,
            Func<TextureInfo, Task<ConversionResult>> processFunc,
            string operationName)
        {
            _isProcessing = true;
            ShowProgress(operationName);

            // Временно отключаем автообновление, чтобы не было конфликтов
            bool wasAutoRefresh = AutoRefreshCheckBox.IsChecked == true;
            if (wasAutoRefresh)
            {
                StopAllWatchers();
            }

            int processed = 0;
            int success = 0;
            int failed = 0;

            try
            {
                foreach (var texture in textures)
                {
                    OperationStatusText.Text = $"{operationName}: {texture.FileName}";

                    var result = await processFunc(texture);

                    if (result.Success)
                        success++;
                    else
                        failed++;

                    processed++;
                    int percent = (processed * 100) / textures.Count;
                    OperationProgress.Value = percent;
                    ProgressText.Text = $"{percent}%";
                }

                // Обновляем информацию о файлах после обработки
                await RefreshFilesAsync(textures.Select(t => t.FilePath).ToList());

                string message = $"Обработано: {success} успешно";
                if (failed > 0)
                    message += $", {failed} с ошибками";

                MessageBox.Show(message, "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем автообновление
                if (wasAutoRefresh)
                {
                    var folders = _textures
                        .Select(t => Path.GetDirectoryName(t.FilePath))
                        .Where(f => !string.IsNullOrEmpty(f))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var folder in folders)
                    {
                        SetupWatcherForFolder(folder);
                    }
                }

                HideProgress();
                _isProcessing = false;
            }
        }

        private async Task RefreshFilesAsync(List<string> filePaths)
        {
            await Task.Run(() =>
            {
                foreach (var filePath in filePaths)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var existing = _textures.FirstOrDefault(t =>
                            t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                        if (existing != null && File.Exists(filePath))
                        {
                            int index = _textures.IndexOf(existing);
                            var updated = TextureAnalysisService.AnalyzeFile(filePath);
                            _textures[index] = updated;
                        }
                    });
                }
            });

            UpdateStatistics();
        }

        #endregion

        #region Progress UI

        private void ShowProgress(string message)
        {
            ProgressPanel.Visibility = Visibility.Visible;
            OperationProgress.Value = 0;
            ProgressText.Text = "0%";
            OperationStatusText.Text = message;
        }

        private void HideProgress()
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}