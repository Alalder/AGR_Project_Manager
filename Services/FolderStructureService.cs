using System;
using System.IO;

namespace AGR_Project_Manager.Services
{
    public class FolderStructureService
    {
        /// <summary>
        /// Версия структуры папок проекта
        /// </summary>
        public enum StructureVersion
        {
            V1_Legacy = 1,  // Старая версия
            V2_New = 2      // Новая версия
        }

        /// <summary>
        /// Создаёт структуру папок проекта (или дополняет существующую)
        /// </summary>
        public bool CreateProjectStructure(string basePath, string projectName, StructureVersion version = StructureVersion.V1_Legacy, int modelCount = 1)
        {
            return version switch
            {
                StructureVersion.V2_New => CreateProjectStructureV2(basePath, projectName, modelCount),
                _ => CreateProjectStructureV1(basePath, projectName)
            };
        }

        /// <summary>
        /// Создаёт структуру папок V1 (старая версия)
        /// </summary>
        private bool CreateProjectStructureV1(string basePath, string projectName)
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
                System.Diagnostics.Debug.WriteLine($"Ошибка создания структуры V1: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Создаёт структуру папок V2 (новая версия)
        /// </summary>
        private bool CreateProjectStructureV2(string basePath, string projectName, int modelCount)
        {
            try
            {
                if (modelCount < 1)
                    modelCount = 1;

                string projectPath = Path.Combine(basePath, projectName);

                // Создаём основную папку (если не существует)
                Directory.CreateDirectory(projectPath);

                // Source - исходные данные (остаётся без изменений)
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "maps"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "mesh"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "dwg"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "pln"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "pdf"));
                Directory.CreateDirectory(Path.Combine(projectPath, "source", "skp"));

                // Export - новая структура (V2)
                string exportPath = Path.Combine(projectPath, "export");
                Directory.CreateDirectory(Path.Combine(exportPath, "HP_FBX"));
                Directory.CreateDirectory(Path.Combine(exportPath, "LP_FBX"));
                Directory.CreateDirectory(Path.Combine(exportPath, "GeoJSON"));
                Directory.CreateDirectory(Path.Combine(exportPath, "Archives"));

                // Work - новая структура
                string workPath = Path.Combine(projectPath, "work");

                // work/Mesh
                Directory.CreateDirectory(Path.Combine(workPath, "Mesh", "LP_mesh"));
                Directory.CreateDirectory(Path.Combine(workPath, "Mesh", "HP_mesh"));

                // work/Textures/HP_textures
                string hpTexturesPath = Path.Combine(workPath, "Textures", "HP_textures");
                Directory.CreateDirectory(Path.Combine(hpTexturesPath, "HP_Ground"));

                // Создаём папки для каждой HP модели
                for (int i = 1; i <= modelCount; i++)
                {
                    Directory.CreateDirectory(Path.Combine(hpTexturesPath, $"HP_{i:D3}"));
                }

                // work/Textures/LP_textures
                string lpTexturesPath = Path.Combine(workPath, "Textures", "LP_textures");
                Directory.CreateDirectory(Path.Combine(lpTexturesPath, "LP_Ground"));

                // Создаём папки для каждой LP модели
                for (int i = 1; i <= modelCount; i++)
                {
                    Directory.CreateDirectory(Path.Combine(lpTexturesPath, $"LP_{i:D3}"));
                }

                // work/Bake
                Directory.CreateDirectory(Path.Combine(workPath, "Bake"));

                // work/AGR_Check
                Directory.CreateDirectory(Path.Combine(workPath, "AGR_Check", "HP"));
                Directory.CreateDirectory(Path.Combine(workPath, "AGR_Check", "LP"));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания структуры V2: {ex.Message}");
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