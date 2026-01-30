using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class ProjectService
    {
        private readonly string _dataPath;

        public ObservableCollection<Project> Projects { get; private set; }

        public ProjectService()
        {
            // Portable: данные хранятся рядом с exe
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exeFolder, "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            _dataPath = Path.Combine(dataFolder, "projects.json");

            Projects = new ObservableCollection<Project>();
            LoadProjects();
        }

        public void AddProject(Project project)
        {
            project.CreatedDate = DateTime.Now;
            project.ModifiedDate = DateTime.Now;
            Projects.Add(project);
            SaveProjects();
        }

        public void UpdateProject(Project project)
        {
            project.ModifiedDate = DateTime.Now;
            SaveProjects();
        }

        public void DeleteProject(Project project)
        {
            Projects.Remove(project);
            SaveProjects();
        }

        public Project CloneProject(Project original)
        {
            var json = JsonSerializer.Serialize(original);
            var clone = JsonSerializer.Deserialize<Project>(json);

            clone.Id = Guid.NewGuid();
            clone.Name = original.Name + " (копия)";
            clone.CreatedDate = DateTime.Now;
            clone.ModifiedDate = DateTime.Now;

            Projects.Add(clone);
            SaveProjects();
            return clone;
        }

        private void LoadProjects()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var projects = JsonSerializer.Deserialize<ObservableCollection<Project>>(json);

                    if (projects != null)
                    {
                        foreach (var project in projects)
                        {
                            Projects.Add(project);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void SaveProjects()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Projects, options);
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
    }
}