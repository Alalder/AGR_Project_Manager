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

namespace AGR_Project_Manager.Windows
{
    public partial class NameGeneratorWindow : Window
    {
        private readonly NameGeneratorService _nameService;
        private readonly ProjectService _projectService;
        private Project _currentProject;
        private District _currentDistrict;
        private bool _isUpdatingDistrict = false;
        private List<District> _allDistricts;

        public NameGeneratorWindow(ProjectService projectService, Project selectedProject = null)
        {
            InitializeComponent();
            _nameService = new NameGeneratorService();
            _projectService = projectService;
            _currentProject = selectedProject;
            _allDistricts = DistrictDatabase.Districts.ToList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectComboBox.ItemsSource = _projectService.Projects;
            DistrictComboBox.ItemsSource = _allDistricts;

            if (_currentProject != null)
            {
                ProjectComboBox.SelectedItem = _currentProject;
            }
            else if (_projectService.Projects.Count > 0)
            {
                ProjectComboBox.SelectedIndex = 0;
            }
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentProject = ProjectComboBox.SelectedItem as Project;
            LoadProjectDistrict();
            RefreshAllNames();
        }

        private void LoadProjectDistrict()
        {
            if (_currentProject == null) return;

            _isUpdatingDistrict = true;

            try
            {
                DistrictComboBox.ItemsSource = _allDistricts;

                if (!string.IsNullOrEmpty(_currentProject.DistrictCode))
                {
                    _currentDistrict = _allDistricts
                        .FirstOrDefault(d => d.Number == _currentProject.DistrictCode);

                    if (_currentDistrict != null)
                    {
                        DistrictComboBox.SelectedItem = _currentDistrict;
                        DistrictCodeText.Text = _currentDistrict.Number;
                    }
                    else
                    {
                        ClearDistrictSelection();
                    }
                }
                else
                {
                    ClearDistrictSelection();
                }
            }
            finally
            {
                _isUpdatingDistrict = false;
            }
        }

        private void ClearDistrictSelection()
        {
            _currentDistrict = null;
            DistrictComboBox.SelectedItem = null;
            DistrictComboBox.Text = "";
            DistrictCodeText.Text = "----";
        }

        private void SaveProjectDistrict()
        {
            if (_currentProject == null || _isUpdatingDistrict) return;

            string newCode = _currentDistrict?.Number ?? "";

            if (_currentProject.DistrictCode != newCode)
            {
                _currentProject.DistrictCode = newCode;
                _projectService.UpdateProject(_currentProject);
            }
        }

        private void DistrictComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingDistrict) return;

            var selected = DistrictComboBox.SelectedItem as District;

            if (selected != null)
            {
                _currentDistrict = selected;
                DistrictCodeText.Text = selected.Number;
                SaveProjectDistrict();
                RefreshFbxAndArchives();
            }
        }

        private void DistrictComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string currentText = comboBox.Text ?? "";
            string newText = currentText + e.Text;

            var filtered = _allDistricts
                .Where(d => d.Name.IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           d.Number.Contains(newText))
                .ToList();

            if (filtered.Count > 0)
            {
                comboBox.ItemsSource = filtered;
                comboBox.IsDropDownOpen = true;
            }
        }

        private void RefreshAllNames()
        {
            if (_currentProject == null)
            {
                ClearAllLists();
                return;
            }

            PopulateList(GeometryHighPolyList, _nameService.GenerateGeometryHighPoly(_currentProject));
            PopulateList(GeometryLowPolyList, _nameService.GenerateGeometryLowPoly(_currentProject));
            PopulateList(MaterialsHighPolyList, _nameService.GenerateMaterialsHighPoly(_currentProject));
            PopulateList(MaterialsLowPolyList, _nameService.GenerateMaterialsLowPoly(_currentProject));
            PopulateList(TexturesHighPolyList, _nameService.GenerateTexturesHighPoly(_currentProject));
            PopulateList(TexturesLowPolyList, _nameService.GenerateTexturesLowPoly(_currentProject));
            PopulateList(CollisionsList, _nameService.GenerateCollisions(_currentProject));
            PopulateList(LightingList, _nameService.GenerateLighting(_currentProject));
            RefreshFbxAndArchives();
        }

        private void RefreshFbxAndArchives()
        {
            if (_currentProject == null) return;

            string districtCode = _currentDistrict?.Number;

            PopulateList(FbxHighPolyList, _nameService.GenerateFbxHighPoly(_currentProject));
            PopulateList(FbxLowPolyList, _nameService.GenerateFbxLowPoly(_currentProject, districtCode));
            PopulateList(ArchivesHighPolyList, _nameService.GenerateArchivesHighPoly(_currentProject));
            PopulateList(ArchiveLowPolyList, _nameService.GenerateArchiveLowPoly(_currentProject, districtCode));
        }

        private void PopulateList(ItemsControl itemsControl, List<string> items)
        {
            itemsControl.Items.Clear();

            foreach (var item in items)
            {
                var border = CreateCopyableItem(item);
                itemsControl.Items.Add(border);
            }
        }

        private Border CreateCopyableItem(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6a9955")),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2c35")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                Child = textBlock,
                ToolTip = "Нажмите для копирования"
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3d3c45"));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2c35"));
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                CopyToClipboard(text, textBlock);
            };

            return border;
        }

        private void CopyToClipboard(string text, TextBlock textBlock)
        {
            try
            {
                Clipboard.SetText(text);

                string originalText = textBlock.Text;
                var originalBrush = textBlock.Foreground;

                textBlock.Text = "✓ Скопировано!";
                textBlock.Foreground = new SolidColorBrush(Colors.LightGreen);

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(800)
                };
                timer.Tick += (s, args) =>
                {
                    textBlock.Text = originalText;
                    textBlock.Foreground = originalBrush;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка");
            }
        }

        private void ClearAllLists()
        {
            GeometryHighPolyList.Items.Clear();
            GeometryLowPolyList.Items.Clear();
            MaterialsHighPolyList.Items.Clear();
            MaterialsLowPolyList.Items.Clear();
            TexturesHighPolyList.Items.Clear();
            TexturesLowPolyList.Items.Clear();
            CollisionsList.Items.Clear();
            LightingList.Items.Clear();
            FbxHighPolyList.Items.Clear();
            FbxLowPolyList.Items.Clear();
            ArchivesHighPolyList.Items.Clear();
            ArchiveLowPolyList.Items.Clear();
        }
    }
}