using System;
using System.IO;
using System.Windows;
using AGR_Project_Manager.Services;
using Ookii.Dialogs.Wpf;

namespace AGR_Project_Manager.Windows
{
    public partial class CreateFolderWindow : Window
    {
        private readonly FolderStructureService _folderService;
        private readonly TransliterationService _translitService;
        private bool _useTranslit = true; // По умолчанию используем транслит (стрелка вправо)

        public CreateFolderWindow()
        {
            InitializeComponent();

            _folderService = new FolderStructureService();
            _translitService = new TransliterationService();

            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (ProjectNameTextBox != null)
            {
                ProjectNameTextBox.TextChanged += OnProjectNameChanged;
            }

            if (FolderPathTextBox != null)
            {
                FolderPathTextBox.TextChanged += (s, ev) => UpdatePreview();
            }

            UpdatePreview();
            UpdateArrowIcon();
        }

        private void OnProjectNameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ProjectNameTextBox == null || TranslitTextBox == null)
                return;

            // Автоматически обновляем поле транслита
            string input = ProjectNameTextBox.Text?.Trim() ?? "";
            string transliterated = _translitService.Transliterate(input);

            TranslitTextBox.TextChanged -= TranslitTextBox_TextChanged;
            TranslitTextBox.Text = transliterated;
            TranslitTextBox.TextChanged += TranslitTextBox_TextChanged;

            UpdatePreview();
        }

        private void TranslitTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void ToggleSourceButton_Click(object sender, RoutedEventArgs e)
        {
            // Переключаем источник названия папки
            _useTranslit = !_useTranslit;
            UpdateArrowIcon();
            UpdatePreview();
        }

        private void UpdateArrowIcon()
        {
            if (ArrowIcon == null || ToggleSourceButton == null)
                return;

            if (_useTranslit)
            {
                ArrowIcon.Text = "→"; // Стрелка вправо - используем транслит
                ToggleSourceButton.ToolTip = "Используется транслит (→)\nНажмите для переключения на оригинал";
            }
            else
            {
                ArrowIcon.Text = "←"; // Стрелка влево - используем оригинал
                ToggleSourceButton.ToolTip = "Используется оригинал (←)\nНажмите для переключения на транслит";
            }
        }

        private void UpdatePreview()
        {
            if (RootFolderName == null || FullPathPreview == null ||
                ProjectNameTextBox == null || TranslitTextBox == null || FolderPathTextBox == null)
                return;

            string projectName = GetFinalProjectName();
            string folderPath = FolderPathTextBox.Text?.Trim() ?? "";

            // Обновляем название корневой папки в дереве
            if (!string.IsNullOrEmpty(projectName))
            {
                RootFolderName.Text = $"📁 {projectName}";
            }
            else
            {
                RootFolderName.Text = "📁 [Название проекта]";
            }

            // Обновляем полный путь
            if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(projectName))
            {
                FullPathPreview.Text = Path.Combine(folderPath, projectName);
            }
            else if (!string.IsNullOrEmpty(folderPath))
            {
                FullPathPreview.Text = folderPath + "\\...";
            }
            else
            {
                FullPathPreview.Text = "";
            }
        }

        /// <summary>
        /// Получает финальное название проекта в зависимости от выбранного источника
        /// </summary>
        private string GetFinalProjectName()
        {
            if (ProjectNameTextBox == null || TranslitTextBox == null)
                return "";

            if (_useTranslit)
            {
                // Используем транслит (стрелка вправо →)
                return TranslitTextBox.Text?.Trim() ?? "";
            }
            else
            {
                // Используем оригинал (стрелка влево ←)
                return ProjectNameTextBox.Text?.Trim() ?? "";
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите рабочую папку",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectNameTextBox == null || FolderPathTextBox == null || TranslitTextBox == null)
            {
                MessageBox.Show("Ошибка инициализации окна", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string originalName = ProjectNameTextBox.Text?.Trim() ?? "";
            string translitName = TranslitTextBox.Text?.Trim() ?? "";
            string projectName = GetFinalProjectName();
            string folderPath = FolderPathTextBox.Text?.Trim() ?? "";

            // Валидация
            if (string.IsNullOrEmpty(originalName))
            {
                MessageBox.Show("Введите название проекта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(projectName))
            {
                string message = _useTranslit
                    ? "Поле транслитерации пустое"
                    : "Поле названия проекта пустое";
                MessageBox.Show(message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                MessageBox.Show("Выберите рабочую папку", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("Выбранная папка не существует", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем недопустимые символы
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (projectName.Contains(c))
                {
                    MessageBox.Show($"Название папки содержит недопустимый символ: {c}\n\nИспользуйте только допустимые символы для имени папки.",
                        "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Проверяем, не существует ли уже папка
            string fullPath = Path.Combine(folderPath, projectName);
            if (Directory.Exists(fullPath))
            {
                var result = MessageBox.Show(
                    $"Папка \"{projectName}\" уже существует.\n\nСоздать недостающие папки в структуре?",
                    "Папка существует",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Создаём структуру
            try
            {
                bool success = _folderService.CreateProjectStructure(folderPath, projectName);

                if (success)
                {
                    string sourceInfo = _useTranslit
                        ? $"📝 Оригинал: {originalName}\n📁 Транслит: {translitName}"
                        : $"📝 Название: {originalName}";

                    var openFolder = MessageBox.Show(
                        $"Структура папок успешно создана!\n\n{sourceInfo}\n\n📂 {fullPath}\n\nОткрыть папку в проводнике?",
                        "✅ Успех",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openFolder == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", fullPath);
                    }

                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось создать структуру папок.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания структуры:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}