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
        private FolderStructureService.StructureVersion _selectedStructureVersion = FolderStructureService.StructureVersion.V1_Legacy;
        private int _modelCount = 1;

        public CreateFolderWindow()
        {
            InitializeComponent();

            _folderService = new FolderStructureService();

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

            // Инициализируем структуру версионирования
            if (StructureVersionComboBox != null)
            {
                StructureVersionComboBox.SelectedIndex = 0;
            }

            if (ModelCountUpDown != null)
            {
                ModelCountUpDown.Text = "1";
            }

            UpdatePreview();
            UpdateFieldsVisibility();
        }

        private void OnProjectNameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void TranslitTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void StructureVersionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StructureVersionComboBox == null)
                return;

            int selectedIndex = StructureVersionComboBox.SelectedIndex;
            _selectedStructureVersion = selectedIndex == 0 
                ? FolderStructureService.StructureVersion.V1_Legacy 
                : FolderStructureService.StructureVersion.V2_New;

            UpdateFieldsVisibility();
            UpdatePreview();
        }

        private void UpdateFieldsVisibility()
        {
            if (ModelCountPanel == null)
                return;

            // Поле "Количество моделей" видимо только для V2
            bool isV2 = _selectedStructureVersion == FolderStructureService.StructureVersion.V2_New;
            ModelCountPanel.Visibility = isV2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ModelCountUp_Click(object sender, RoutedEventArgs e)
        {
            if (ModelCountUpDown != null && int.TryParse(ModelCountUpDown.Text, out int value))
            {
                _modelCount = value + 1;
                ModelCountUpDown.Text = _modelCount.ToString();
                UpdatePreview();
            }
        }

        private void ModelCountDown_Click(object sender, RoutedEventArgs e)
        {
            if (ModelCountUpDown != null && int.TryParse(ModelCountUpDown.Text, out int value) && value > 1)
            {
                _modelCount = value - 1;
                ModelCountUpDown.Text = _modelCount.ToString();
                UpdatePreview();
            }
        }

        private void ModelCountUpDown_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void UpdatePreview()
        {
            if (RootFolderName == null || FullPathPreview == null || ProjectNameTextBox == null || FolderPathTextBox == null)
                return;

            string projectName = ProjectNameTextBox.Text?.Trim() ?? "";
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

            UpdateFolderTreePreview();
        }

        private void UpdateFolderTreePreview()
        {
            // Обновляем дерево папок в зависимости от выбранной версии
            if (FolderTreeStackPanel == null)
                return;

            bool isV2 = _selectedStructureVersion == FolderStructureService.StructureVersion.V2_New;

            // Для V2 показываем новую структуру, для V1 - старую
            if (FolderTreeV1 != null)
                FolderTreeV1.Visibility = isV2 ? Visibility.Collapsed : Visibility.Visible;

            if (FolderTreeV2 != null)
                FolderTreeV2.Visibility = isV2 ? Visibility.Visible : Visibility.Collapsed;
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
            if (ProjectNameTextBox == null || FolderPathTextBox == null)
            {
                MessageBox.Show("Ошибка инициализации окна", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string projectName = ProjectNameTextBox.Text?.Trim() ?? "";
            string folderPath = FolderPathTextBox.Text?.Trim() ?? "";

            // Валидация
            if (string.IsNullOrEmpty(projectName))
            {
                MessageBox.Show("Введите название проекта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
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
                bool success = _folderService.CreateProjectStructure(folderPath, projectName, _selectedStructureVersion, _modelCount);

                if (success)
                {
                    string versionInfo = _selectedStructureVersion == FolderStructureService.StructureVersion.V2_New
                        ? $"структура V2 (моделей: {_modelCount})"
                        : "структура V1";

                    var openFolder = MessageBox.Show(
                        $"Структура папок успешно создана!\n\n📝 Название: {projectName}\n📊 {versionInfo}\n\n📂 {fullPath}\n\nОткрыть папку в проводнике?",
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