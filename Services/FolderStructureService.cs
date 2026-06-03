using System;
using System.IO;

namespace AGR_Project_Manager.Services
{
    public class FolderStructureService
    {
        /// <summary>
        /// Создаёт структуру папок проекта (или дополняет существующую)
        /// </summary>
        public bool CreateProjectStructure(string basePath, string projectName)
        {
            try
            {
                string projectPath = Path.Combine(basePath, projectName);

                // Создаём основную папку (если не существует)
                Directory.CreateDirectory(projectPath);

                // Source - исходные данные
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "maps"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "mesh"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "dwg"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "pln"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "pdf"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "skp"));

                // Export - финальные файлы
                Directory.CreateDirectory(Path.Combine(projectPath, "export", "HP"));
                Directory.CreateDirectory(Path.Combine(projectPath, "export", "LP"));

                // Work - рабочие файлы
                Directory.CreateDirectory(Path.Combine(projectPath, "work", "texture"));
                Directory.CreateDirectory(Path.Combine(projectPath, "work", "mesh"));
                Directory.CreateDirectory(Path.Combine(projectPath, "work", "bake"));
                Directory.CreateDirectory(Path.Combine(projectPath, "work", "AGR_Check", "HP"));
                Directory.CreateDirectory(Path.Combine(projectPath, "work", "AGR_Check", "LP"));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания структуры: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Возвращает полный путь к папке проекта
        /// </summary>
        public string GetProjectPath(string basePath, string projectName)
        {
            return Path.Combine(basePath, projectName);
        }
    }
}