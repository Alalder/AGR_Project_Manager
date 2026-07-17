using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Строит план архивов по структуре папок V2 и собирает zip.
    /// Все пути на диске резолвятся относительно projectRootPath (структура V2 из FolderStructureService).
    /// </summary>
    public class ArchiveBuilderService
    {
        private readonly NameGeneratorService _nameService = new();

        /// <summary>
        /// Сканирует диск и строит план: HP-архивы (по модели + Ground) + один LP-архив.
        /// Сопоставление HP-модели с папкой текстур HP_00X идёт по порядковому индексу
        /// в project.Models (Ground — всегда отдельно, последним).
        /// </summary>
        public List<ArchivePlan> BuildPlan(Project project, string projectRootPath, string districtCode)
        {
            var plans = new List<ArchivePlan>();

            string exportPath = Path.Combine(projectRootPath, "export");
            string hpFbxPath = Path.Combine(exportPath, "HP_FBX");
            string geoJsonPath = Path.Combine(exportPath, "GeoJSON");
            string lpFbxPath = Path.Combine(exportPath, "LP_FBX");
            string hpTexturesPath = Path.Combine(projectRootPath, "work", "Textures", "HP_textures");

            // ===== HP: по одному архиву на модель + Ground (Ground всегда последний в списке) =====
            var hpNames = _nameService.GenerateArchivesHighPoly(project);
            int modelIndex = 1;

            for (int i = 0; i < hpNames.Count; i++)
            {
                bool isGround = i == hpNames.Count - 1;
                string name = hpNames[i];
                string textureFolder = isGround
                    ? Path.Combine(hpTexturesPath, "HP_Ground")
                    : Path.Combine(hpTexturesPath, $"HP_{modelIndex:D3}");

                var plan = new ArchivePlan
                {
                    ArchiveName = name,
                    Kind = ArchiveKind.HighPoly,
                    Fbx = MakeEntry("FBX модели", Path.Combine(hpFbxPath, $"{name}.fbx"), required: true),
                    Light = MakeEntry("FBX освещения", Path.Combine(hpFbxPath, $"{name}_Light.fbx"), required: false),
                    GeoJson = MakeEntry("GeoJSON", Path.Combine(geoJsonPath, $"{name}.geojson"), required: false),
                    TextureFolderPath = textureFolder
                };

                if (Directory.Exists(textureFolder))
                    plan.TextureFiles.AddRange(Directory.GetFiles(textureFolder));

                plans.Add(plan);

                if (!isGround) modelIndex++;
            }

            // ===== LP: один архив на весь проект =====
            var lpFbxNames = _nameService.GenerateFbxLowPoly(project, districtCode);
            string lpArchiveName = _nameService.GenerateArchiveLowPoly(project, districtCode).FirstOrDefault();

            var lpPlan = new ArchivePlan
            {
                ArchiveName = lpArchiveName,
                Kind = ArchiveKind.LowPoly
            };

            foreach (var lpName in lpFbxNames)
            {
                lpPlan.LowPolyFbxEntries.Add(MakeEntry(lpName, Path.Combine(lpFbxPath, $"{lpName}.fbx"), required: true));
            }

            plans.Add(lpPlan);

            return plans;
        }

        private static ArchiveFileEntry MakeEntry(string displayName, string path, bool required)
        {
            return new ArchiveFileEntry
            {
                DisplayName = displayName,
                SourcePath = File.Exists(path) ? path : null,
                IsRequired = required
            };
        }

        /// <summary>
        /// Собирает zip для одного плана. Все файлы кладутся в корень архива вперемешку.
        /// Отсутствующие необязательные компоненты (Light, GeoJson) просто пропускаются.
        /// </summary>
        public void BuildArchive(ArchivePlan plan, string archivesOutputPath)
        {
            Directory.CreateDirectory(archivesOutputPath);
            string zipPath = Path.Combine(archivesOutputPath, $"{plan.ArchiveName}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            if (plan.Kind == ArchiveKind.HighPoly)
            {
                AddIfFound(archive, plan.Fbx);
                AddIfFound(archive, plan.Light);
                AddIfFound(archive, plan.GeoJson);

                foreach (var texturePath in plan.TextureFiles)
                    archive.CreateEntryFromFile(texturePath, Path.GetFileName(texturePath));
            }
            else
            {
                foreach (var entry in plan.LowPolyFbxEntries)
                    AddIfFound(archive, entry);
            }
        }

        private static void AddIfFound(ZipArchive archive, ArchiveFileEntry entry)
        {
            if (entry != null && entry.Found)
                archive.CreateEntryFromFile(entry.SourcePath, Path.GetFileName(entry.SourcePath));
        }
    }
}