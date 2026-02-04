using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;
using AGR_Project_Manager.Models;
using AGR_Project_Manager.Services;
using AGR_Project_Manager.Windows;

namespace AGR_Project_Manager
{
    public partial class MainWindow : Window
    {
        private readonly ProjectService _projectService;
        private readonly TextureExportService _exportService;
        private readonly PresetService _presetService;
        private Project _selectedProject;
        private UdimTile _copiedTile;
        private UdimTile _selectedTile;

        public MainWindow()
        {
            InitializeComponent();
            InitializeThemeSelector();
            _projectService = new ProjectService();
            _exportService = new TextureExportService();
            _presetService = new PresetService();  // НОВОЕ

            ProjectsList.ItemsSource = _projectService.Projects;
            PresetComboBox.ItemsSource = _presetService.Presets;  // НОВОЕ
        }

        private void InitializeThemeSelector()
        {
            // Заполняем ComboBox списком тем
            ThemeComboBox.ItemsSource = ThemeManager.AvailableThemes;

            // Выбираем текущую тему
            ThemeComboBox.SelectedItem = ThemeManager.GetCurrentTheme();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ThemeManager.ThemeInfo selectedTheme)
            {
                ThemeManager.ChangeTheme(selectedTheme);
            }
        }

        

        #region Project Management

        private void NewProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProjectDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                _projectService.AddProject(dialog.Project);
                ProjectsList.SelectedItem = dialog.Project;
            }
        }

        private void EditProject_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var project = button?.Tag as Project;
            if (project == null) return;

            var dialog = new ProjectDialog(project);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                _projectService.UpdateProject(project);
                UpdateProjectPanel(project);
            }
        }

        private void CloneProject_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var project = button?.Tag as Project;
            if (project == null) return;

            var clone = _projectService.CloneProject(project);
            ProjectsList.SelectedItem = clone;
        }

        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var project = button?.Tag as Project;
            if (project == null) return;

            var result = MessageBox.Show(
                $"Удалить проект \"{project.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _projectService.DeleteProject(project);
                if (_selectedProject == project)
                {
                    _selectedProject = null;
                    ShowPlaceholder();
                }
            }
        }

        private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var project = ProjectsList.SelectedItem as Project;
            if (project != null)
            {
                _selectedProject = project;
                UpdateProjectPanel(project);
                ShowProjectPanel();
            }
            else
            {
                ShowPlaceholder();
            }
        }

        private void UpdateProjectPanel(Project project)
        {
            ProjectTitle.Text = project.Name;
            ProjectDescription.Text = string.IsNullOrEmpty(project.Description)
                ? "Нет описания" : project.Description;

            RefreshModelTabs();
            UpdateFileNameTemplate();
        }

        private void RefreshModelTabs()
        {
            if (_selectedProject == null) return;

            int previousIndex = ModelsTabControl.SelectedIndex;

            ModelsTabControl.Items.Clear();

            foreach (var model in _selectedProject.Models)
            {
                var tabItem = new TabItem
                {
                    Header = model.Name,
                    Tag = model,
                    DataContext = model  // Важно для биндинга в шаблоне!
                };
                ModelsTabControl.Items.Add(tabItem);
            }

            // Восстанавливаем выбор
            if (ModelsTabControl.Items.Count > 0)
            {
                if (previousIndex >= 0 && previousIndex < ModelsTabControl.Items.Count)
                {
                    ModelsTabControl.SelectedIndex = previousIndex;
                }
                else
                {
                    ModelsTabControl.SelectedIndex = 0;
                }
            }
        }

        private void ShowProjectPanel()
        {
            PlaceholderText.Visibility = Visibility.Collapsed;
            ProjectPanel.Visibility = Visibility.Visible;
        }

        private void ShowPlaceholder()
        {
            PlaceholderText.Visibility = Visibility.Visible;
            ProjectPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Presets

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTile == null)
            {
                MessageBox.Show("Сначала выберите UDIM тайл для сохранения", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_selectedTile.HasAnyTexture)
            {
                MessageBox.Show("Выбранный тайл не содержит текстур", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Запрашиваем имя пресета
            var dialog = new RenameDialog(_selectedTile.Name ?? "Новый пресет");
            dialog.Title = "Сохранить пресет";
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var preset = new UdimPreset(dialog.NewName, _selectedTile);
                _presetService.AddPreset(preset);

                PresetComboBox.SelectedItem = preset;

                MessageBox.Show($"Пресет \"{dialog.NewName}\" сохранён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = PresetComboBox.SelectedItem != null;
            ApplyPresetBtn.IsEnabled = hasSelection && _selectedTile != null;
            DeletePresetBtn.IsEnabled = hasSelection;
        }

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = PresetComboBox.SelectedItem as UdimPreset;

            if (preset == null)
            {
                MessageBox.Show("Выберите пресет из списка", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_selectedTile == null)
            {
                MessageBox.Show("Сначала выберите UDIM тайл", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            preset.ApplyTo(_selectedTile);
            _projectService.UpdateProject(_selectedProject);
        }

        private void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = PresetComboBox.SelectedItem as UdimPreset;

            if (preset == null) return;

            var result = MessageBox.Show(
                $"Удалить пресет \"{preset.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _presetService.DeletePreset(preset);
            }
        }

        #endregion

        #region Model Management

        private void AddModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null) return;

            _selectedProject.AddModel();
            RefreshModelTabs();

            if (ModelsTabControl.Items.Count > 1)
            {
                ModelsTabControl.SelectedIndex = ModelsTabControl.Items.Count - 2;
            }

            _projectService.UpdateProject(_selectedProject);
            UpdateFileNameTemplate();
        }

        private void RenameModel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var model = button?.Tag as ModelData;

            if (model == null) return;

            var dialog = new RenameDialog(model.Name);
            dialog.Title = "Переименовать модель";
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                model.Name = dialog.NewName;
                RefreshModelTabs();
                _projectService.UpdateProject(_selectedProject);
                UpdateFileNameTemplate();
            }
        }

        private void DuplicateModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null) return;

            var button = sender as System.Windows.Controls.Button;
            var model = button?.Tag as ModelData;

            if (model == null) return;

            // Находим индекс модели
            int index = _selectedProject.Models.IndexOf(model);
            if (index >= 0)
            {
                _selectedProject.DuplicateModel(index);
                RefreshModelTabs();

                // Выбираем новую (дублированную) вкладку
                ModelsTabControl.SelectedIndex = index + 1;

                _projectService.UpdateProject(_selectedProject);
            }
        }

        private void RemoveModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null) return;

            var button = sender as System.Windows.Controls.Button;
            var model = button?.Tag as ModelData;

            if (model == null) return;

            if (_selectedProject.Models.Count <= 1)
            {
                MessageBox.Show("Нельзя удалить последнюю модель", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить модель \"{model.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                int index = _selectedProject.Models.IndexOf(model);
                _selectedProject.Models.Remove(model);
                RefreshModelTabs();

                if (ModelsTabControl.Items.Count > 0)
                {
                    ModelsTabControl.SelectedIndex = Math.Min(index, ModelsTabControl.Items.Count - 1);
                }

                _projectService.UpdateProject(_selectedProject);
                UpdateFileNameTemplate();
            }
        }

        private void UpdateFileNameTemplate()
        {
            if (_selectedProject == null) return;

            var model = GetCurrentModel();
            if (model == null) return;

            string template = _exportService.GetFileNameTemplate(_selectedProject, model);
            FileNameTemplateText.Text = template;
        }

        private void ModelsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabItem = ModelsTabControl.SelectedItem as TabItem;
            var model = tabItem?.Tag as ModelData;

            if (model != null)
            {
                UdimGrid.ItemsSource = model.UdimRows;
                UpdateFileNameTemplate();
            }
        }

        #endregion

        #region UDIM Management

        private void RenameTile_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var tile = FindParentDataContext<UdimTile>(border);

            if (tile == null) return;

            var dialog = new RenameDialog(tile.Name ?? "");
            dialog.Title = $"Название UDIM {tile.UdimNumber}";
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                tile.Name = dialog.NewName;
                _projectService.UpdateProject(_selectedProject);
            }

            e.Handled = true; // Чтобы не срабатывал выбор тайла
        }

        private void AddUdimRow_Click(object sender, RoutedEventArgs e)
        {
            var model = GetCurrentModel();
            if (model != null)
            {
                model.AddRow();
                _projectService.UpdateProject(_selectedProject);
            }
        }

        private void RemoveUdimRow_Click(object sender, RoutedEventArgs e)
        {
            var model = GetCurrentModel();
            if (model != null && model.UdimRows.Count > 1)
            {
                model.RemoveTopRow();
                _projectService.UpdateProject(_selectedProject);
            }
        }

        private Border _selectedTileBorder;

        private void UdimTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var tile = border?.DataContext as UdimTile;

            if (tile != null)
            {
                // Убираем выделение с предыдущего
                if (_selectedTileBorder != null)
                {
                    // Берём цвет из текущей темы!
                    _selectedTileBorder.Background = (SolidColorBrush)Application.Current.FindResource("BackgroundTertiary");
                }

                // Выделяем новый
                _selectedTile = tile;
                _selectedTileBorder = border;

                // Берём цвет выделения из текущей темы!
                border.Background = (SolidColorBrush)Application.Current.FindResource("SelectionBackground");

                // Активируем кнопку применения пресета если есть выбранный пресет
                ApplyPresetBtn.IsEnabled = PresetComboBox.SelectedItem != null;
            }
        }

        private void LoadTexture_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Ищем UdimTile через визуальное дерево
            UdimTile tile = FindParentDataContext<UdimTile>(button);

            if (tile == null) return;

            string textureType = button.Tag?.ToString();

            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.tga;*.tiff)|*.png;*.jpg;*.jpeg;*.tga;*.tiff|All files (*.*)|*.*",
                Title = $"Выберите {textureType} текстуру"
            };

            if (dialog.ShowDialog() == true)
            {
                switch (textureType)
                {
                    case "Diffuse":
                        tile.DiffusePath = dialog.FileName;
                        break;
                    case "ERM":
                        tile.ErmPath = dialog.FileName;
                        break;
                    case "Normal":
                        tile.NormalPath = dialog.FileName;
                        break;
                }
                _projectService.UpdateProject(_selectedProject);
            }
        }

        /// <summary>
        /// Вспомогательный метод для поиска DataContext нужного типа в родительских элементах
        /// </summary>
        private T FindParentDataContext<T>(FrameworkElement element) where T : class
        {
            var current = element;
            while (current != null)
            {
                if (current.DataContext is T result)
                {
                    return result;
                }
                current = current.Parent as FrameworkElement;
            }
            return null;
        }

        private void CopyTile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTile != null)
            {
                _copiedTile = _selectedTile.Clone();
                PasteTileBtn.IsEnabled = true;
                PasteTileBtn.Content = $"📥 Вставить ({_copiedTile.UdimNumber})";
            }
            else
            {
                MessageBox.Show("Сначала выберите UDIM тайл (кликните на него)", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PasteTile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTile != null && _copiedTile != null)
            {
                _selectedTile.CopyFrom(_copiedTile);
                _projectService.UpdateProject(_selectedProject);
            }
            else if (_selectedTile == null)
            {
                MessageBox.Show("Сначала выберите UDIM тайл для вставки", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private ModelData GetCurrentModel()
        {
            var tabItem = ModelsTabControl.SelectedItem as TabItem;
            return tabItem?.Tag as ModelData;
        }

        #endregion

        #region Export

        private void BrowseExportPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для экспорта текстур",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                ExportPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void ExportCurrentModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null) return;

            var model = GetCurrentModel();
            if (model == null) return;

            if (string.IsNullOrEmpty(ExportPathTextBox.Text))
            {
                MessageBox.Show("Выберите папку для экспорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int count = await _exportService.ExportModelAsync(_selectedProject, model, ExportPathTextBox.Text);
                MessageBox.Show($"Экспортировано {count} текстур для модели {model.Name}!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportAllModels_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null) return;

            if (string.IsNullOrEmpty(ExportPathTextBox.Text))
            {
                MessageBox.Show("Выберите папку для экспорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int count = await _exportService.ExportAllModelsAsync(_selectedProject, ExportPathTextBox.Text);
                MessageBox.Show($"Экспортировано {count} текстур для всех моделей!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Utilities

        private void RalColorsBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new RalColorsWindow();
            // Убираем Owner чтобы окно было независимым
            window.Show();
        }

        private void NameGeneratorBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new NameGeneratorWindow(_projectService, _selectedProject);
            window.Show();
        }

        private void GeoJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new GeoJsonGeneratorWindow(_projectService, _selectedProject);
            window.Show();
        }

        private void CreateFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateFolderWindow();
            window.Show();
        }

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new HelpWindow();
            window.Show();
        }

        private void RequirementsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.mos.ru/upload/content/files/3082f7e5e5f574e2be658d0542484d4c/RasporyajeniepoVPMiNPM.pdf",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}