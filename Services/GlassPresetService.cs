using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class GlassPresetService
    {
        private readonly string _dataPath;

        public ObservableCollection<GlassPreset> Presets { get; private set; }

        public GlassPresetService()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exeFolder, "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            _dataPath = Path.Combine(dataFolder, "glass_presets.json");

            Presets = new ObservableCollection<GlassPreset>();
            LoadPresets();
        }

        public void AddPreset(GlassPreset preset)
        {
            Presets.Add(preset);
            SavePresets();
        }

        public void DeletePreset(GlassPreset preset)
        {
            Presets.Remove(preset);
            SavePresets();
        }

        private void LoadPresets()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var presets = JsonSerializer.Deserialize<ObservableCollection<GlassPreset>>(json);

                    if (presets != null)
                    {
                        foreach (var preset in presets)
                        {
                            Presets.Add(preset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки пресетов стёкол: {ex.Message}");
            }
        }

        private void SavePresets()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Presets, options);
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения пресетов стёкол: {ex.Message}");
            }
        }
    }
}