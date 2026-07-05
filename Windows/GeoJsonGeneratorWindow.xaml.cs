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
        private ModelData _currentModel;
        private bool _isLoading = false;
        private int _selectedMaterialIndex = -1;

        public GeoJsonGeneratorWindow(ProjectService projectService, Project selectedProject = null)
        {
            InitializeComponent();
            _geoJsonService = new GeoJsonService();
            _projectService = projectService;
            _glassPresetService = new GlassPresetService();

            ProjectComboBox.ItemsSource = _projectService.Projects;
            GlassPresetComboBox.ItemsSource = _glassPresetService.Presets;

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

            if (_currentProject == null) return;

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
                    Tag = model
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

            // Сохраняем данные предыдущей модели
            SaveCurrentModelData();

            var tabItem = ModelsTabControl.SelectedItem as TabItem;
            _currentModel = tabItem?.Tag as ModelData;

            if (_currentModel != null)
            {
                LoadModelData();
                UpdateFileNamePreview();
                UpdateFieldsVisibility();
            }
        }

        private void UpdateFieldsVisibility()
        {
            bool isGround = _currentModel?.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase) ?? false;

            var disabledStyle = FindResource("DisabledFieldStyle") as Style;
            var normalStyle = FindResource("FieldInputStyle") as Style;

            if (isGround)
            {
                // Для Ground модели: устанавливаем заголовок и запрещаем редактирование
                // Если поле было пусто, заполняем его значением по умолчанию
                if (string.IsNullOrEmpty(FnoNameTextBox.Text))
                {
                    FnoNameTextBox.Text = "Благоустройство территории";
                }
                FnoNameTextBox.IsReadOnly = true;
                FnoNameTextBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525"));

                // Отключаем редактирование, но НЕ стираем текст
                HOtnTextBox.Style = disabledStyle;
                HOtnTextBox.IsEnabled = false;

                HAbsTextBox.Style = disabledStyle;
                HAbsTextBox.IsEnabled = false;

                SObshTextBox.Style = disabledStyle;
                SObshTextBox.IsEnabled = false;

                SNazTextBox.Style = disabledStyle;
                SNazTextBox.IsEnabled = false;

                SPodzTextBox.Style = disabledStyle;
                SPodzTextBox.IsEnabled = false;

                SppGnsTextBox.Style = disabledStyle;
                SppGnsTextBox.IsEnabled = false;
            }
            else
            {
                // Для обычных моделей: разрешаем редактирование и восстанавливаем стиль
                FnoNameTextBox.IsReadOnly = false;
                FnoNameTextBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2c35"));

                HOtnTextBox.Style = normalStyle;
                HOtnTextBox.IsEnabled = true;

                HAbsTextBox.Style = normalStyle;
                HAbsTextBox.IsEnabled = true;

                SObshTextBox.Style = normalStyle;
                SObshTextBox.IsEnabled = true;

                SNazTextBox.Style = normalStyle;
                SNazTextBox.IsEnabled = true;

                SPodzTextBox.Style = normalStyle;
                SPodzTextBox.IsEnabled = true;

                SppGnsTextBox.Style = normalStyle;
                SppGnsTextBox.IsEnabled = true;
            }
        }

        private void UpdateFileNamePreview()
        {
            if (_currentProject == null || _currentModel == null) return;

            bool needsSuffix = _currentProject.Models.Count(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;

            string fileName = _geoJsonService.GetFileName(_currentProject.Name, _currentModel.Name, needsSuffix);
            FileNamePreviewText.Text = fileName;
        }

        #endregion

        #region Data Load/Save

        private void SaveCurrentModelData()
        {
            if (_currentModel == null || _currentProject == null) return;

            // Сохраняем индивидуальные данные модели
            _currentModel.CoordX = CoordXTextBox.Text;
            _currentModel.CoordY = CoordYTextBox.Text;
            // FnoName уже сохраняется в CommonField_TextChanged через ApplyFnoNameToModels
            // здесь дополнительно убеждаемся, что сохранили текущее значение
            ApplyFnoNameToModels(FnoNameTextBox.Text);
            ApplyFnoCodeToModels(FnoCodeTextBox.Text);

            // Сохраняем в проект
            _projectService.UpdateProject(_currentProject);
        }

        private void LoadModelData()
        {
            if (_currentModel == null || _currentProject == null) return;

            _isLoading = true;

            // Загружаем ОБЩИЕ данные из проекта
            var projectData = _currentProject.GeoJsonData;
            AddressTextBox.Text = projectData.Address ?? "";
            OkrugTextBox.Text = projectData.Okrug ?? "";
            RajonTextBox.Text = projectData.Rajon ?? "";
            NameTextBox.Text = projectData.Name ?? "";
            DeveloperTextBox.Text = projectData.Developer ?? "";
            DesignerTextBox.Text = projectData.Designer ?? "";
            CadNumTextBox.Text = projectData.CadNum ?? "";
            
            ZuAreaTextBox.Text = projectData.ZuArea ?? "";
            HReliefTextBox.Text = projectData.HRelief ?? "";
            HOtnTextBox.Text = projectData.HOtn ?? "";
            HAbsTextBox.Text = projectData.HAbs ?? "";
            SObshTextBox.Text = projectData.SObsh ?? "";
            SNazTextBox.Text = projectData.SNaz ?? "";
            SPodzTextBox.Text = projectData.SPodz ?? "";
            SppGnsTextBox.Text = projectData.SppGns ?? "";
            ActAgrTextBox.Text = projectData.ActAgr ?? "";
            OtherTextBox.Text = projectData.Other ?? "";

            // Загружаем ИНДИВИДУАЛЬНЫЕ данные модели
            CoordXTextBox.Text = _currentModel.CoordX ?? "";
            CoordYTextBox.Text = _currentModel.CoordY ?? "";
            FnoNameTextBox.Text = _currentModel.FnoName ?? "";
            FnoCodeTextBox.Text = _currentModel.FnoCode ?? "";

            // Загружаем стёкла
            RefreshMaterialsList();
            ClearMaterialForm();

            // Загружаем изображение
            LoadImagePreview(_currentModel.Base64Image);

            _isLoading = false;
        }

        private void CommonField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading || _currentProject == null) return;

            // Сохраняем общие поля в проект с заменой двойных кавычек на одинарные
            var projectData = _currentProject.GeoJsonData;
            projectData.Address = ReplaceQuotes(AddressTextBox.Text);
            projectData.Okrug = ReplaceQuotes(OkrugTextBox.Text);
            projectData.Rajon = ReplaceQuotes(RajonTextBox.Text);
            projectData.Name = ReplaceQuotes(NameTextBox.Text);
            projectData.Developer = ReplaceQuotes(DeveloperTextBox.Text);
            projectData.Designer = ReplaceQuotes(DesignerTextBox.Text);
            projectData.CadNum = ReplaceQuotes(CadNumTextBox.Text);
            
            projectData.ZuArea = ReplaceQuotes(ZuAreaTextBox.Text);
            projectData.HRelief = ReplaceQuotes(HReliefTextBox.Text);
            projectData.HOtn = ReplaceQuotes(HOtnTextBox.Text);
            projectData.HAbs = ReplaceQuotes(HAbsTextBox.Text);
            projectData.SObsh = ReplaceQuotes(SObshTextBox.Text);
            projectData.SNaz = ReplaceQuotes(SNazTextBox.Text);
            projectData.SPodz = ReplaceQuotes(SPodzTextBox.Text);
            projectData.SppGns = ReplaceQuotes(SppGnsTextBox.Text);
            projectData.ActAgr = ReplaceQuotes(ActAgrTextBox.Text);
            projectData.Other = ReplaceQuotes(OtherTextBox.Text);

            // Сохраняем FnoName в текущую модель и дублируем на остальные (кроме Ground)
            if (_currentModel != null)
            {
                ApplyFnoNameToModels(ReplaceQuotes(FnoNameTextBox.Text));
                ApplyFnoCodeToModels(ReplaceQuotes(FnoCodeTextBox.Text));
            }

            _projectService.UpdateProject(_currentProject);
        }

        /// <summary>
        /// Заменяет двойные кавычки на одинарные
        /// </summary>
        private string ReplaceQuotes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Прямые двойные + все типографские кавычки (рус./англ./нем./франц. варианты) -> одинарная '
                       return text
                            .Replace("\"", "'")   // прямая двойная
                            .Replace('«', '\'')
                            .Replace('»', '\'')
                            .Replace('“', '\'')   // левая англ. верхняя
                            .Replace('”', '\'')   // правая англ. верхняя
                            .Replace('„', '\'')   // нижняя (нем./рус.)
                            .Replace('‘', '\'')   // левая одинарная типографская
                            .Replace('’', '\'');  // правая одинарная типографская (апостроф в Word)
        }

        #endregion

        #region Materials

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;
            if (string.IsNullOrWhiteSpace(MaterialNameTextBox.Text))
            {
                MessageBox.Show("Введите название материала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var material = CreateMaterialFromForm();
            _currentModel.Glasses.Add(material);
            RefreshMaterialsList();
            ClearMaterialForm();
            _projectService.UpdateProject(_currentProject);
        }

        private void UpdateMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaterialIndex < 0 || _currentModel == null) return;

            var glasses = _currentModel.Glasses;
            if (_selectedMaterialIndex >= glasses.Count) return;

            glasses[_selectedMaterialIndex] = CreateMaterialFromForm();
            RefreshMaterialsList();
            _projectService.UpdateProject(_currentProject);
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            var glasses = _currentModel.Glasses;
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
            _projectService.UpdateProject(_currentProject);
        }

        private void MaterialsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMaterialIndex = MaterialsListBox.SelectedIndex;

            if (_selectedMaterialIndex >= 0 && _currentModel != null)
            {
                var glasses = _currentModel.Glasses;
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
            if (_currentModel != null)
            {
                foreach (var glass in _currentModel.Glasses)
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
            ColorPreviewBorder.Background = new SolidColorBrush(Color.FromRgb(
                (byte)Math.Clamp(r, 0, 255),
                (byte)Math.Clamp(g, 0, 255),
                (byte)Math.Clamp(b, 0, 255)));
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
            if (string.IsNullOrWhiteSpace(RedTextBox.Text) &&
                string.IsNullOrWhiteSpace(GreenTextBox.Text) &&
                string.IsNullOrWhiteSpace(BlueTextBox.Text))
            {
                MessageBox.Show("Сначала заполните параметры материала", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            if (_currentModel == null) return;

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
                    ApplyImageToModel(base64);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PasteImage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            try
            {
                if (Clipboard.ContainsImage())
                {
                    var bitmapSource = Clipboard.GetImage();

                    using (var ms = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(ms);
                        ms.Position = 0;

                        using (var image = System.Drawing.Image.FromStream(ms))
                        {
                            string base64 = _geoJsonService.ImageToBase64(image);
                            ApplyImageToModel(base64);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("В буфере обмена нет изображения", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет изображение к модели. 
        /// Если это первая модель (не Ground) - копирует на все модели проекта
        /// </summary>
        private void ApplyImageToModel(string base64)
        {
            if (_currentModel == null || _currentProject == null) return;

            // Находим первую модель (не Ground)
            var firstModel = _currentProject.Models.FirstOrDefault(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase));

            // Если редактируем первую модель - копируем на все
            if (firstModel == _currentModel)
            {
                foreach (var model in _currentProject.Models)
                {
                    model.Base64Image = base64;
                }
                MessageBox.Show("Изображение установлено для всех моделей проекта", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Иначе - только для текущей модели
                _currentModel.Base64Image = base64;
            }

            LoadImagePreview(base64);
            _projectService.UpdateProject(_currentProject);
        }

        /// <summary>
        /// Применяет FNO_name к модели. 
        /// Если это первая модель (не Ground) - копирует на все модели проекта (кроме Ground)
        /// </summary>
        private void ApplyFnoNameToModels(string fnoName)
        {
            if (_currentModel == null || _currentProject == null) return;

            // Находим первую модель (не Ground)
            var firstModel = _currentProject.Models.FirstOrDefault(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase));

            // Если редактируем первую модель - копируем на все модели (кроме Ground)
            if (firstModel == _currentModel)
            {
                foreach (var model in _currentProject.Models)
                {
                    // Пропускаем Ground модель
                    if (!model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase))
                    {
                        model.FnoName = fnoName;
                    }
                }
            }
            else
            {
                // Иначе - только для текущей модели
                _currentModel.FnoName = fnoName;
            }
        }

        private void ApplyFnoCodeToModels(string fnoCode)
        {
            if (_currentModel == null || _currentProject == null) return;

            var firstModel = _currentProject.Models.FirstOrDefault(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase));

            // Правим первую модель — копируем на все обычные модели (Ground не трогаем)
            if (firstModel == _currentModel)
            {
                foreach (var model in _currentProject.Models)
                {
                    if (!model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase))
                    {
                        model.FnoCode = fnoCode;
                    }
                }
            }
            else
            {
                // Ground или любая другая модель, кроме первой — правим только её
                _currentModel.FnoCode = fnoCode;
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;
            _currentModel.Base64Image = null;
            ImagePreview.Source = null;
            _projectService.UpdateProject(_currentProject);
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
            if (_currentModel == null) return;

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

                    // Импортируем данные в модель
                    ImportGeoJsonData(data);

                    MessageBox.Show("Файл загружен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportGeoJsonData(GeoJsonData data)
        {
            if (_currentModel == null || _currentProject == null) return;

            _isLoading = true;

            // Импортируем общие данные в проект
            var projectData = _currentProject.GeoJsonData;
            projectData.Address = data.Address;
            projectData.Okrug = data.Okrug;
            projectData.Rajon = data.Rajon;
            projectData.Name = data.Name;
            projectData.Developer = data.Developer;
            projectData.Designer = data.Designer;
            projectData.CadNum = data.CadNum;
            
            projectData.ZuArea = data.ZuArea;
            projectData.HRelief = data.HRelief;
            projectData.HOtn = data.HOtn;
            projectData.HAbs = data.HAbs;
            projectData.SObsh = data.SObsh;
            projectData.SNaz = data.SNaz;
            projectData.SPodz = data.SPodz;
            projectData.SppGns = data.SppGns;
            projectData.ActAgr = data.ActAgr;
            projectData.Other = data.Other;

            // Импортируем индивидуальные данные модели
            _currentModel.CoordX = data.CoordX;
            _currentModel.CoordY = data.CoordY;
            _currentModel.Base64Image = data.ImageBase64;
            _currentModel.FnoName = data.FnoName;
            _currentModel.FnoCode = data.FnoCode;

            // Импортируем стёкла
            _currentModel.Glasses.Clear();
            foreach (var glass in data.Glasses)
            {
                _currentModel.Glasses.Add(glass);
            }

            _projectService.UpdateProject(_currentProject);
            LoadModelData();

            _isLoading = false;
        }

        private void PreviewGeoJson_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentModelData();
            if (_currentModel == null) return;

            var data = BuildGeoJsonData();
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
            if (_currentProject == null || _currentModel == null) return;

            bool needsSuffix = _currentProject.Models.Count(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;
            string fileName = _geoJsonService.GetFileName(_currentProject.Name, _currentModel.Name, needsSuffix);

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
                    var data = BuildGeoJsonData();
                    string json = _geoJsonService.GenerateGeoJson(data);
                    _geoJsonService.ExportToFile(json, dialog.FileName);
                    MessageBox.Show($"Сохранено: {dialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                    bool needsSuffix = _currentProject.Models.Count(m =>
                        !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) > 1;

                    foreach (var model in _currentProject.Models)
                    {
                        // Создаём папку для модели
                        string modelFolder = Path.Combine(dialog.SelectedPath, model.Name);
                        Directory.CreateDirectory(modelFolder);

                        // Генерируем и сохраняем файл
                        var data = BuildGeoJsonDataForModel(model);
                        string fileName = _geoJsonService.GetFileName(_currentProject.Name, model.Name, needsSuffix);
                        string filePath = Path.Combine(modelFolder, fileName);
                        string json = _geoJsonService.GenerateGeoJson(data);
                        _geoJsonService.ExportToFile(json, filePath);
                        count++;
                    }

                    MessageBox.Show($"Экспортировано {count} файлов!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Строит GeoJsonData для текущей модели
        /// </summary>
        private GeoJsonData BuildGeoJsonData()
        {
            return BuildGeoJsonDataForModel(_currentModel);
        }

        /// <summary>
        /// Строит GeoJsonData для указанной модели
        /// </summary>
        private GeoJsonData BuildGeoJsonDataForModel(ModelData model)
        {
            if (model == null || _currentProject == null) return new GeoJsonData();

            var projectData = _currentProject.GeoJsonData;
            bool isGround = model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase);

            var data = new GeoJsonData
            {
                // Общие данные из проекта
                Address = projectData.Address,
                Okrug = projectData.Okrug,
                Rajon = projectData.Rajon,
                Name = projectData.Name,
                Developer = projectData.Developer,
                Designer = projectData.Designer,
                CadNum = projectData.CadNum,
               
                // FnoName берём из модели, для Ground если пусто - используем значение по умолчанию
                FnoName = string.IsNullOrEmpty(model.FnoName) && isGround 
                    ? "Благоустройство территории" 
                    : model.FnoName,
                ZuArea = projectData.ZuArea,
                HRelief = projectData.HRelief,
                HOtn = isGround ? "" : projectData.HOtn,
                HAbs = isGround ? "" : projectData.HAbs,
                SObsh = isGround ? "" : projectData.SObsh,
                SNaz = isGround ? "" : projectData.SNaz,
                SPodz = isGround ? "" : projectData.SPodz,
                SppGns = isGround ? "" : projectData.SppGns,
                ActAgr = projectData.ActAgr,
                Other = projectData.Other,

                // Индивидуальные данные модели
                CoordX = model.CoordX,
                CoordY = model.CoordY,
                ImageBase64 = model.Base64Image
            };

            // Копируем стёкла
            data.Glasses.Clear();
            foreach (var glass in model.Glasses)
            {
                data.Glasses.Add(glass);
            }

            return data;
        }

        #endregion
    }
}