using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class TextureExportService
    {
        /// <summary>
        /// Проверяет, нужен ли суффикс модели в имени файла
        /// (если больше одной модели, не считая Ground)
        /// </summary>
        private bool NeedsModelSuffix(Project project)
        {
            int modelCount = project.Models.Count(m =>
                !m.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase));
            return modelCount > 1;
        }

        /// <summary>
        /// Генерирует шаблон имени файла для отображения в UI
        /// </summary>
        public string GetFileNameTemplate(Project project, ModelData model)
        {
            if (project == null || model == null)
                return "T_[Проект]_[Модель]_[Тип]_1.[UDIM].png";

            bool needsSuffix = NeedsModelSuffix(project);
            bool isGround = model.Name.Equals("Ground", StringComparison.OrdinalIgnoreCase);

            if (needsSuffix || isGround)
            {
                return $"T_{project.Name}_{model.Name}_[D/E/N]_1.[UDIM].png";
            }
            else
            {
                return $"T_{project.Name}_[D/E/N]_1.[UDIM].png";
            }
        }

        /// <summary>
        /// Экспорт текстур для одной модели
        /// </summary>
        public async Task<int> ExportModelAsync(Project project, ModelData model, string outputFolder)
        {
            int exportedCount = 0;
            bool needsSuffix = NeedsModelSuffix(project);

            await Task.Run(() =>
            {
                foreach (var row in model.UdimRows)
                {
                    foreach (var tile in row)
                    {
                        exportedCount += ExportTile(project.Name, model.Name, tile, outputFolder, needsSuffix);
                    }
                }
            });

            return exportedCount;
        }

        /// <summary>
        /// Экспорт текстур для всех моделей (каждая в свою папку)
        /// </summary>
        public async Task<int> ExportAllModelsAsync(Project project, string outputFolder)
        {
            int exportedCount = 0;
            bool needsSuffix = NeedsModelSuffix(project);

            await Task.Run(() =>
            {
                foreach (var model in project.Models)
                {
                    // Создаём папку для каждой модели
                    string modelFolder = Path.Combine(outputFolder, model.Name);
                    Directory.CreateDirectory(modelFolder);

                    foreach (var row in model.UdimRows)
                    {
                        foreach (var tile in row)
                        {
                            exportedCount += ExportTile(project.Name, model.Name, tile, modelFolder, needsSuffix);
                        }
                    }
                }
            });

            return exportedCount;
        }

        private int ExportTile(string projectName, string modelName, UdimTile tile, string outputFolder, bool includeModelName)
        {
            int count = 0;
            bool isGround = modelName.Equals("Ground", StringComparison.OrdinalIgnoreCase);

            // Ground всегда включает имя модели
            bool useModelSuffix = includeModelName || isGround;

            if (tile.HasDiffuse)
            {
                string fileName = BuildFileName(projectName, modelName, "Diffuse", tile.UdimNumber, useModelSuffix);
                if (CopyTexture(tile.DiffusePath, Path.Combine(outputFolder, fileName)))
                    count++;
            }

            if (tile.HasErm)
            {
                string fileName = BuildFileName(projectName, modelName, "ERM", tile.UdimNumber, useModelSuffix);
                if (CopyTexture(tile.ErmPath, Path.Combine(outputFolder, fileName)))
                    count++;
            }

            if (tile.HasNormal)
            {
                string fileName = BuildFileName(projectName, modelName, "Normal", tile.UdimNumber, useModelSuffix);
                if (CopyTexture(tile.NormalPath, Path.Combine(outputFolder, fileName)))
                    count++;
            }

            return count;
        }

        private string BuildFileName(string projectName, string modelName, string textureType, int udimNumber, bool includeModel)
        {
            // Очищаем имена от недопустимых символов
            projectName = SanitizeFileName(projectName);
            modelName = SanitizeFileName(modelName);

            if (includeModel)
            {
                // T_ProjectName_ModelName_Diffuse_1.1001.png
                return $"T_{projectName}_{modelName}_{textureType}_1.{udimNumber}.png";
            }
            else
            {
                // T_ProjectName_Diffuse_1.1001.png
                return $"T_{projectName}_{textureType}_1.{udimNumber}.png";
            }
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(" ", "_");
        }

        private bool CopyTexture(string sourcePath, string destPath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка копирования: {ex.Message}");
            }
            return false;
        }
    }
}