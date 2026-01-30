using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class PresetService
    {
        private readonly string _dataPath;

        public ObservableCollection<UdimPreset> Presets { get; private set; }

        public PresetService()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exeFolder, "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            _dataPath = Path.Combine(dataFolder, "presets.json");

            Presets = new ObservableCollection<UdimPreset>();
            LoadPresets();
        }

        public void AddPreset(UdimPreset preset)
        {
            Presets.Add(preset);
            SavePresets();
        }

        public void DeletePreset(UdimPreset preset)
        {
            Presets.Remove(preset);
            SavePresets();
        }

        public void RenamePreset(UdimPreset preset, string newName)
        {
            preset.Name = newName;
            SavePresets();
        }

        private void LoadPresets()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var presets = JsonSerializer.Deserialize<ObservableCollection<UdimPreset>>(json);

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
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки пресетов: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения пресетов: {ex.Message}");
            }
        }
    }
}