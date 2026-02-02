using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AGR_Project_Manager.Models;
using AGR_Project_Manager.Services;
using AGR_Project_Manager.Windows;
using Ookii.Dialogs.Wpf;

namespace AGR_Project_Manager.Windows
{
    public partial class GeoJsonGeneratorWindow : Window
    {
        private readonly GeoJsonService _geoJsonService;
        private readonly ProjectService _projectService;
        private readonly GlassPresetService _glassPresetService;
        private Project _currentProject;
        private Dictionary<string, GeoJsonData> _modelDataMap;
        private string _currentModelName;
        private bool _isLoading = false;
        private int _selectedMaterialIndex = -1;

        public GeoJsonGeneratorWindow(ProjectService projectService, Project selectedProject = null)
        {
            InitializeComponent();
            _geoJsonService = new GeoJsonService();
            _projectService = projectService;
            _glassPresetService = new GlassPresetService();  // НОВОЕ
            _modelDataMap = new Dictionary<string, GeoJsonData>();

            ProjectComboBox.ItemsSource = _projectService.Projects;
            GlassPresetComboBox.ItemsSource = _glassPresetService.Presets;  // НОВОЕ

            if (selectedProject != null)
            {
                ProjectComboBox.SelectedItem = selectedProject;
            }
            else if (_projectService.Projects.Count > 0)
            {
                ProjectComboBox.SelectedIndex = 0;
            }
        }

        #region Project & Model Selection

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentProject = ProjectComboBox.SelectedItem as Project;
            _modelDataMap.Clear();

            if (_currentProject == null) return;

            // Инициализируем данные для каждой модели
            foreach (var model in _currentProject.Models)
            {
                var data = new GeoJsonData();

                // Для Ground устанавливаем специальные значения
                if (model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase))
                {
                    data.FnoName = "Благоустройство территории";
                }

                _modelDataMap[model.Name] = data;
            }

            RefreshModelTabs();
        }

        private void RefreshModelTabs()
        {
            ModelsTabControl.Items.Clear();

            if (_currentProject == null) return;

            foreach (var model in _currentProject.Models)
            {
                var tabItem = new TabItem
                {
                    Header = model.Name,
                    Tag = model.Name
                };
                ModelsTabControl.Items.Add(tabItem);
            }

            if (ModelsTabControl.Items.Count > 0)
            {
                ModelsTabControl.SelectedIndex = 0;
            }
        }

        private void ModelsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            // Сохраняем текущие данные
            SaveCurrentModelData();

            var tabItem = ModelsTabControl.SelectedItem as TabItem;
            _currentModelName = tabItem?.Tag as string;

            if (_currentModelName != null)
            {
                LoadModelData(_currentModelName);
                UpdateFileNamePreview();
                UpdateFieldsVisibility();
            }
        }

        private void UpdateFieldsVisibility()
        {
            bool isGround = _currentModelName?.Equals("Ground", StringComparison.OrdinalIgnoreCase) ?? false;

            // Для Ground скрываем/делаем readonly некоторые поля
            var disabledStyle = FindResource("DisabledFieldStyle") as Style;
            var normalStyle = FindResource("FieldInputStyle") as Style;

            if (isGround)
            {
                FnoNameTextBox.Text = "Благоустройство территории";
                FnoNameTextBox.IsReadOnly = true;
                FnoNameTextBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525"));

                HOtnTextBox.Style = disabledStyle;
                HAbsTextBox.Style = disabledStyle;
                SObshTextBox.Style = disabledStyle;
                SNazTextBox.Style = disabledStyle;
                SPodzTextBox.Style = disabledStyle;
                SppGnsTextBox.Style = disabledStyle;

                HOtnTextBox.Text = "";
                HAbsTextBox.Text = "";
                SObshTextBox.Text = "";
                SNazTextBox.Text = "";
                SPodzTextBox.Text = "";
                SppGnsTextBox.Text = "";
            }
            else
            {
                FnoNameTextBox.IsReadOnly = false;
                FnoNameTextBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2c35"));

                HOtnTextBox.Style = normalStyle;
                HAbsTextBox.Style = normalStyle;
                SObshTextBox.Style = normalStyle;
                SNazTextBox.Style = normalStyle;
                SPodzTextBox.Style = normalStyle;
                SppGnsTextBox.Style = normalStyle;
            }
        }

        private void UpdateFileNamePreview()
        {
            if (_currentProject == null || string.IsNullOrEmpty(_currentModelName)) return;

            bool needsSuffix = _currentProject.Models.Count(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;

            string fileName = _geoJsonService.GetFileName(_currentProject.Name, _currentModelName, needsSuffix);
            FileNamePreviewText.Text = fileName;
        }

        #endregion

        #region Data Load/Save

        private void SaveCurrentModelData()
        {
            if (string.IsNullOrEmpty(_currentModelName) || !_modelDataMap.ContainsKey(_currentModelName)) return;

            var data = _modelDataMap[_currentModelName];

            data.Address = AddressTextBox.Text;
            data.Okrug = OkrugTextBox.Text;
            data.Rajon = RajonTextBox.Text;
            data.Name = NameTextBox.Text;
            data.Developer = DeveloperTextBox.Text;
            data.Designer = DesignerTextBox.Text;
            data.CadNum = CadNumTextBox.Text;
            data.FnoCode = FnoCodeTextBox.Text;
            data.FnoName = FnoNameTextBox.Text;
            data.ZuArea = ZuAreaTextBox.Text;
            data.HRelief = HReliefTextBox.Text;
            data.HOtn = HOtnTextBox.Text;
            data.HAbs = HAbsTextBox.Text;
            data.SObsh = SObshTextBox.Text;
            data.SNaz = SNazTextBox.Text;
            data.SPodz = SPodzTextBox.Text;
            data.SppGns = SppGnsTextBox.Text;
            data.ActAgr = ActAgrTextBox.Text;
            data.Other = OtherTextBox.Text;
            data.CoordX = CoordXTextBox.Text;
            data.CoordY = CoordYTextBox.Text;
        }

        private void LoadModelData(string modelName)
        {
            if (!_modelDataMap.ContainsKey(modelName)) return;

            _isLoading = true;
            var data = _modelDataMap[modelName];

            AddressTextBox.Text = data.Address;
            OkrugTextBox.Text = data.Okrug;
            RajonTextBox.Text = data.Rajon;
            NameTextBox.Text = data.Name;
            DeveloperTextBox.Text = data.Developer;
            DesignerTextBox.Text = data.Designer;
            CadNumTextBox.Text = data.CadNum;
            FnoCodeTextBox.Text = data.FnoCode;
            FnoNameTextBox.Text = data.FnoName;
            ZuAreaTextBox.Text = data.ZuArea;
            HReliefTextBox.Text = data.HRelief;
            HOtnTextBox.Text = data.HOtn;
            HAbsTextBox.Text = data.HAbs;
            SObshTextBox.Text = data.SObsh;
            SNazTextBox.Text = data.SNaz;
            SPodzTextBox.Text = data.SPodz;
            SppGnsTextBox.Text = data.SppGns;
            ActAgrTextBox.Text = data.ActAgr;
            OtherTextBox.Text = data.Other;
            CoordXTextBox.Text = data.CoordX;
            CoordYTextBox.Text = data.CoordY;

            // Загружаем стёкла
            MaterialsListBox.Items.Clear();
            foreach (var glass in data.Glasses)
            {
                MaterialsListBox.Items.Add(glass.ToString());
            }
            ClearMaterialForm();

            // Загружаем изображение
            LoadImagePreview(data.ImageBase64);

            _isLoading = false;
        }

        private void CommonField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading || string.IsNullOrEmpty(_currentModelName)) return;

            // Сохраняем текущую модель
            SaveCurrentModelData();

            // Получаем данные первой модели (не Ground)
            var firstModel = _currentProject?.Models.FirstOrDefault(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase));

            if (firstModel == null || !_modelDataMap.ContainsKey(firstModel.Name)) return;

            var sourceData = _modelDataMap[firstModel.Name];

            // Синхронизируем с остальными моделями
            foreach (var model in _currentProject.Models)
            {
                if (model.Name == _currentModelName) continue;

                var targetData = _modelDataMap[model.Name];
                bool isGround = model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase);
                targetData.CopyCommonFieldsFrom(sourceData, isGround);
            }
        }

        #endregion

        #region Materials

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;
            if (string.IsNullOrWhiteSpace(MaterialNameTextBox.Text))
            {
                MessageBox.Show("Введите название материала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var material = CreateMaterialFromForm();
            _modelDataMap[_currentModelName].Glasses.Add(material);
            RefreshMaterialsList();
            ClearMaterialForm();
        }

        private void UpdateMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaterialIndex < 0 || string.IsNullOrEmpty(_currentModelName)) return;

            var glasses = _modelDataMap[_currentModelName].Glasses;
            if (_selectedMaterialIndex >= glasses.Count) return;

            glasses[_selectedMaterialIndex] = CreateMaterialFromForm();
            RefreshMaterialsList();
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;

            var glasses = _modelDataMap[_currentModelName].Glasses;
            if (glasses.Count == 0) return;

            if (_selectedMaterialIndex >= 0 && _selectedMaterialIndex < glasses.Count)
            {
                glasses.RemoveAt(_selectedMaterialIndex);
            }
            else
            {
                glasses.RemoveAt(glasses.Count - 1);
            }

            _selectedMaterialIndex = -1;
            RefreshMaterialsList();
            ClearMaterialForm();
        }

        private void MaterialsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMaterialIndex = MaterialsListBox.SelectedIndex;

            if (_selectedMaterialIndex >= 0 && !string.IsNullOrEmpty(_currentModelName))
            {
                var glasses = _modelDataMap[_currentModelName].Glasses;
                if (_selectedMaterialIndex < glasses.Count)
                {
                    LoadMaterialToForm(glasses[_selectedMaterialIndex]);
                }
            }
        }

        private GlassMaterial CreateMaterialFromForm()
        {
            int.TryParse(RedTextBox.Text, out int r);
            int.TryParse(GreenTextBox.Text, out int g);
            int.TryParse(BlueTextBox.Text, out int b);

            return new GlassMaterial
            {
                Name = MaterialNameTextBox.Text,
                Red = Math.Clamp(r, 0, 255),
                Green = Math.Clamp(g, 0, 255),
                Blue = Math.Clamp(b, 0, 255),
                Transparency = TransparencyTextBox.Text,
                Refraction = RefractionTextBox.Text,
                Roughness = RoughnessTextBox.Text,
                Metallicity = MetallicityTextBox.Text
            };
        }

        private void LoadMaterialToForm(GlassMaterial material)
        {
            MaterialNameTextBox.Text = material.Name;
            RedTextBox.Text = material.Red.ToString();
            GreenTextBox.Text = material.Green.ToString();
            BlueTextBox.Text = material.Blue.ToString();
            TransparencyTextBox.Text = material.Transparency;
            RefractionTextBox.Text = material.Refraction;
            RoughnessTextBox.Text = material.Roughness;
            MetallicityTextBox.Text = material.Metallicity;
            UpdateColorPreview();
        }

        private void ClearMaterialForm()
        {
            MaterialNameTextBox.Text = "";
            RedTextBox.Text = "";
            GreenTextBox.Text = "";
            BlueTextBox.Text = "";
            TransparencyTextBox.Text = "";
            RefractionTextBox.Text = "";
            RoughnessTextBox.Text = "";
            MetallicityTextBox.Text = "";
            ColorPreviewBorder.Background = new SolidColorBrush(Colors.Black);
            _selectedMaterialIndex = -1;
        }

        private void RefreshMaterialsList()
        {
            MaterialsListBox.Items.Clear();
            if (!string.IsNullOrEmpty(_currentModelName) && _modelDataMap.ContainsKey(_currentModelName))
            {
                foreach (var glass in _modelDataMap[_currentModelName].Glasses)
                {
                    MaterialsListBox.Items.Add(glass.ToString());
                }
            }
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            int.TryParse(RedTextBox.Text, out int r);
            int.TryParse(GreenTextBox.Text, out int g);
            int.TryParse(BlueTextBox.Text, out int b);
            ColorPreviewBorder.Background = new SolidColorBrush(Color.FromRgb((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255)));
        }

        private void ColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            int.TryParse(RedTextBox.Text, out int r);
            int.TryParse(GreenTextBox.Text, out int g);
            int.TryParse(BlueTextBox.Text, out int b);
            colorDialog.Color = System.Drawing.Color.FromArgb(r, g, b);

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RedTextBox.Text = colorDialog.Color.R.ToString();
                GreenTextBox.Text = colorDialog.Color.G.ToString();
                BlueTextBox.Text = colorDialog.Color.B.ToString();
            }
        }

        #endregion

        #region Glass Presets

        private void SaveGlassPreset_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что форма заполнена
            if (string.IsNullOrWhiteSpace(RedTextBox.Text) &&
                string.IsNullOrWhiteSpace(GreenTextBox.Text) &&
                string.IsNullOrWhiteSpace(BlueTextBox.Text))
            {
                MessageBox.Show("Сначала заполните параметры материала", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Запрашиваем имя пресета
            var dialog = new RenameDialog("Новый пресет стекла");
            dialog.Title = "Сохранить пресет стекла";
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var material = CreateMaterialFromForm();
                var preset = new GlassPreset(dialog.NewName, material);
                _glassPresetService.AddPreset(preset);

                GlassPresetComboBox.SelectedItem = preset;

                MessageBox.Show($"Пресет \"{dialog.NewName}\" сохранён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyGlassPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = GlassPresetComboBox.SelectedItem as GlassPreset;

            if (preset == null)
            {
                MessageBox.Show("Выберите пресет из списка", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Применяем параметры пресета к форме (кроме имени материала)
            RedTextBox.Text = preset.Red.ToString();
            GreenTextBox.Text = preset.Green.ToString();
            BlueTextBox.Text = preset.Blue.ToString();
            TransparencyTextBox.Text = preset.Transparency;
            RefractionTextBox.Text = preset.Refraction;
            RoughnessTextBox.Text = preset.Roughness;
            MetallicityTextBox.Text = preset.Metallicity;
            UpdateColorPreview();
        }

        private void DeleteGlassPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = GlassPresetComboBox.SelectedItem as GlassPreset;

            if (preset == null)
            {
                MessageBox.Show("Выберите пресет для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить пресет \"{preset.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _glassPresetService.DeletePreset(preset);
            }
        }

        #endregion

        #region Image

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string base64 = _geoJsonService.ImageToBase64(dialog.FileName);
                    _modelDataMap[_currentModelName].ImageBase64 = base64;
                    LoadImagePreview(base64);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PasteImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;

            try
            {
                if (Clipboard.ContainsImage())
                {
                    var bitmapSource = Clipboard.GetImage();

                    // Конвертируем в System.Drawing.Image
                    using (var ms = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(ms);
                        ms.Position = 0;

                        using (var image = System.Drawing.Image.FromStream(ms))
                        {
                            string base64 = _geoJsonService.ImageToBase64(image);
                            _modelDataMap[_currentModelName].ImageBase64 = base64;
                            LoadImagePreview(base64);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("В буфере обмена нет изображения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;
            _modelDataMap[_currentModelName].ImageBase64 = null;
            ImagePreview.Source = null;
        }

        private void LoadImagePreview(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                ImagePreview.Source = null;
                return;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImagePreview.Source = bitmap;
            }
            catch
            {
                ImagePreview.Source = null;
            }
        }

        #endregion

        #region GeoJSON Operations

        private void OpenGeoJson_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentModelName)) return;

            var dialog = new OpenFileDialog
            {
                Filter = "GeoJSON files (*.geojson)|*.geojson",
                Title = "Открыть GeoJSON"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = _geoJsonService.LoadFromFile(dialog.FileName);
                    var data = _geoJsonService.ParseGeoJson(json);
                    _modelDataMap[_currentModelName] = data;
                    LoadModelData(_currentModelName);
                    MessageBox.Show("Файл загружен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PreviewGeoJson_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentModelData();
            if (string.IsNullOrEmpty(_currentModelName)) return;

            var data = _modelDataMap[_currentModelName];
            string json = _geoJsonService.GenerateGeoJson(data);

            var previewWindow = new Window
            {
                Title = "GeoJSON Preview",
                Width = 800,
                Height = 600,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1d22")),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var textBox = new TextBox
            {
                Text = json,
                IsReadOnly = true,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2c35")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6a9955")),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.NoWrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            previewWindow.Content = textBox;
            previewWindow.Show();
        }

        private void ExportCurrentGeoJson_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentModelData();
            if (_currentProject == null || string.IsNullOrEmpty(_currentModelName)) return;

            bool needsSuffix = _currentProject.Models.Count(m => !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;
            string fileName = _geoJsonService.GetFileName(_currentProject.Name, _currentModelName, needsSuffix);

            var dialog = new SaveFileDialog
            {
                Filter = "GeoJSON files (*.geojson)|*.geojson",
                FileName = fileName,
                Title = "Сохранить GeoJSON"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _modelDataMap[_currentModelName];
                    string json = _geoJsonService.GenerateGeoJson(data);
                    _geoJsonService.ExportToFile(json, dialog.FileName);
                    MessageBox.Show($"Сохранено: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportAllGeoJson_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentModelData();
            if (_currentProject == null) return;

            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для экспорта всех GeoJSON",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    int count = 0;
                    bool needsSuffix = _currentProject.Models.Count(m => !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;

                    foreach (var model in _currentProject.Models)
                    {
                        if (!_modelDataMap.ContainsKey(model.Name)) continue;

                        // Создаём папку для модели
                        string modelFolder = Path.Combine(dialog.SelectedPath, model.Name);
                        Directory.CreateDirectory(modelFolder);

                        // Генерируем и сохраняем файл
                        var data = _modelDataMap[model.Name];
                        string fileName = _geoJsonService.GetFileName(_currentProject.Name, model.Name, needsSuffix);
                        string filePath = Path.Combine(modelFolder, fileName);
                        string json = _geoJsonService.GenerateGeoJson(data);
                        _geoJsonService.ExportToFile(json, filePath);
                        count++;
                    }

                    MessageBox.Show($"Экспортировано {count} файлов!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}