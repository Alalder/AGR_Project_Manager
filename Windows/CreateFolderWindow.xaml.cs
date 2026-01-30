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

        public CreateFolderWindow()
        {
            InitializeComponent();
            _folderService = new FolderStructureService();

            // Подписываемся на изменение текста для обновления превью
            ProjectNameTextBox.TextChanged += (s, e) => UpdatePreview();
            FolderPathTextBox.TextChanged += (s, e) => UpdatePreview();
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

        private void UpdatePreview()
        {
            string projectName = ProjectNameTextBox.Text?.Trim() ?? "";
            string folderPath = FolderPathTextBox.Text?.Trim() ?? "";

            if (!string.IsNullOrEmpty(projectName))
            {
                RootFolderName.Text = $"📁 {projectName}";
            }
            else
            {
                RootFolderName.Text = "📁 [Название проекта]";
            }

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

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectNameTextBox.Text?.Trim() ?? "";
            string folderPath = FolderPathTextBox.Text?.Trim() ?? "";

            // Валидация
            if (string.IsNullOrEmpty(projectName))
            {
                MessageBox.Show("Введите название папки проекта", "Ошибка",
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

            // Проверяем недопустимые символы в имени
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (projectName.Contains(c))
                {
                    MessageBox.Show($"Название содержит недопустимый символ: {c}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Проверяем, не существует ли уже папка
            string fullPath = Path.Combine(folderPath, projectName);
            if (Directory.Exists(fullPath))
            {
                var result = MessageBox.Show(
                    $"Папка \"{projectName}\" уже существует.\nПерезаписать структуру?",
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
                    var openFolder = MessageBox.Show(
                        $"Структура папок успешно создана!\n\n{fullPath}\n\nОткрыть папку в проводнике?",
                        "Успех",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openFolder == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", fullPath);
                    }

                    Close();  // Просто закрываем без DialogResult
                }
                else
                {
                    MessageBox.Show("Не удалось создать структуру папок.\nВозможно, папка уже существует.",
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
            Close();  // Просто закрываем без DialogResult
        }
    }
}