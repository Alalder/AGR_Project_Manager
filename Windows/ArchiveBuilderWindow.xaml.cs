using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using AGR_Project_Manager.Models;
using AGR_Project_Manager.Services;

namespace AGR_Project_Manager.Windows
{
    public partial class ArchiveBuilderWindow : Window
    {
        private readonly ProjectService _projectService;
        private readonly ArchiveBuilderService _archiveService = new();
        private Project _currentProject;
        private List<ArchivePlan> _plans = new();

        public ArchiveBuilderWindow(ProjectService projectService, Project selectedProject = null)
        {
            InitializeComponent();
            _projectService = projectService;
            _currentProject = selectedProject;
        }

        private static SolidColorBrush GetThemeBrush(string resourceKey)
        {
            return (SolidColorBrush)Application.Current.FindResource(resourceKey);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectComboBox.ItemsSource = _projectService.Projects;

            if (_currentProject != null)
                ProjectComboBox.SelectedItem = _currentProject;
            else if (_projectService.Projects.Count > 0)
                ProjectComboBox.SelectedIndex = 0;
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentProject = ProjectComboBox.SelectedItem as Project;
        }

        private void BrowseProjectPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите корневую папку проекта (со структурой V2)",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                ProjectPathTextBox.Text = dialog.SelectedPath;
            }
        }

        #region Сканирование

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject == null)
            {
                MessageBox.Show("Выберите проект", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(ProjectPathTextBox.Text) || !Directory.Exists(ProjectPathTextBox.Text))
            {
                MessageBox.Show("Укажите существующую папку проекта", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _plans = _archiveService.BuildPlan(_currentProject, ProjectPathTextBox.Text, _currentProject.DistrictCode);
            RefreshTree();
        }

        private void RefreshTree()
        {
            ArchivesTreeView.Items.Clear();

            foreach (var plan in _plans)
            {
                var rootItem = new TreeViewItem
                {
                    Header = CreateArchiveHeader(plan),
                    IsExpanded = plan.HasWarnings // сразу разворачиваем то, на что стоит посмотреть
                };

                if (plan.Kind == ArchiveKind.HighPoly)
                {
                    rootItem.Items.Add(CreateFileEntryItem(plan.Fbx, () => BrowseFile(plan.Fbx, "FBX files (*.fbx)|*.fbx")));
                    rootItem.Items.Add(CreateFileEntryItem(plan.Light, () => BrowseFile(plan.Light, "FBX files (*.fbx)|*.fbx")));
                    rootItem.Items.Add(CreateFileEntryItem(plan.GeoJson, () => BrowseFile(plan.GeoJson, "GeoJSON files (*.geojson)|*.geojson")));
                    rootItem.Items.Add(CreateTexturesItem(plan));
                }
                else
                {
                    foreach (var entry in plan.LowPolyFbxEntries)
                        rootItem.Items.Add(CreateFileEntryItem(entry, () => BrowseFile(entry, "FBX files (*.fbx)|*.fbx")));
                }

                ArchivesTreeView.Items.Add(rootItem);
            }

            StatusText.Text = $"Архивов к сборке: {_plans.Count}";
        }

        private UIElement CreateArchiveHeader(ArchivePlan plan)
        {
            string icon = plan.HasWarnings ? "⚠️" : "✅";
            return new TextBlock
            {
                Text = $"{icon} {plan.ArchiveName}.zip",
                FontWeight = FontWeights.SemiBold,
                Foreground = GetThemeBrush("ForegroundPrimary")
            };
        }

        private TreeViewItem CreateFileEntryItem(ArchiveFileEntry entry, Action onBrowse)
        {
            string status = entry.Found ? "✅" : (entry.IsRequired ? "❌" : "—");
            string fileLabel = entry.Found ? Path.GetFileName(entry.SourcePath) : "не найден";

            var text = new TextBlock
            {
                Text = $"{status} {entry.DisplayName}: {fileLabel}",
                Foreground = GetThemeBrush(entry.Found ? "ForegroundSecondary" : "ForegroundTertiary"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            var browseButton = new Button
            {
                Content = "Обзор...",
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                Background = GetThemeBrush("ButtonBackground"),
                Foreground = GetThemeBrush("ForegroundPrimary"),
                Cursor = Cursors.Hand
            };
            browseButton.Click += (s, e) => onBrowse();

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(text);
            panel.Children.Add(browseButton);

            return new TreeViewItem { Header = panel };
        }

        private TreeViewItem CreateTexturesItem(ArchivePlan plan)
        {
            string status = plan.TextureFiles.Count > 0 ? "✅" : "⚠️";
            var text = new TextBlock
            {
                Text = $"{status} Текстуры: {plan.TextureFiles.Count} файл(ов) — {plan.TextureFolderPath}",
                Foreground = GetThemeBrush(plan.TextureFiles.Count > 0 ? "ForegroundSecondary" : "ForegroundTertiary"),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            var browseButton = new Button
            {
                Content = "Обзор папки...",
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                Background = GetThemeBrush("ButtonBackground"),
                Foreground = GetThemeBrush("ForegroundPrimary"),
                Cursor = Cursors.Hand
            };
            browseButton.Click += (s, e) => BrowseTextureFolder(plan);

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(text);
            panel.Children.Add(browseButton);

            return new TreeViewItem { Header = panel };
        }

        private void BrowseFile(ArchiveFileEntry entry, string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter + "|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                entry.SourcePath = dialog.FileName;
                RefreshTree();
            }
        }

        private void BrowseTextureFolder(ArchivePlan plan)
        {
            var dialog = new VistaFolderBrowserDialog { Description = "Выберите папку с текстурами" };
            if (dialog.ShowDialog() == true)
            {
                plan.TextureFolderPath = dialog.SelectedPath;
                plan.TextureFiles.Clear();
                plan.TextureFiles.AddRange(Directory.GetFiles(dialog.SelectedPath));
                RefreshTree();
            }
        }

        #endregion

        #region Сборка

        private void BuildAll_Click(object sender, RoutedEventArgs e)
        {
            if (_plans.Count == 0)
            {
                MessageBox.Show("Сначала выполните сканирование", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string archivesPath = Path.Combine(ProjectPathTextBox.Text, "export", "Archives");

            try
            {
                foreach (var plan in _plans)
                {
                    _archiveService.BuildArchive(plan, archivesPath);
                }

                MessageBox.Show($"Собрано архивов: {_plans.Count}\n📂 {archivesPath}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сборки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}