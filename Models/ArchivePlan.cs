using System.Collections.Generic;
using System.Linq;

namespace AGR_Project_Manager.Models
{
    public enum ArchiveKind { HighPoly, LowPoly }

    /// <summary>Один файл-компонент архива (может отсутствовать на диске)</summary>
    public class ArchiveFileEntry
    {
        public string DisplayName { get; set; }
        public string SourcePath { get; set; }
        public bool IsRequired { get; set; }

        public bool Found => !string.IsNullOrEmpty(SourcePath);
    }

    /// <summary>План одного архива: HP-модель / HP-Ground / единственный LP-архив проекта</summary>
    public class ArchivePlan
    {
        public string ArchiveName { get; set; }
        public ArchiveKind Kind { get; set; }

        // HP
        public ArchiveFileEntry Fbx { get; set; }
        public ArchiveFileEntry Light { get; set; }
        public ArchiveFileEntry GeoJson { get; set; }
        public string TextureFolderPath { get; set; }
        public List<string> TextureFiles { get; } = new();

        // LP (один архив содержит FBX всех LP-моделей + Ground)
        public List<ArchiveFileEntry> LowPolyFbxEntries { get; } = new();

        public bool HasWarnings => Kind == ArchiveKind.HighPoly
            ? !Fbx.Found || TextureFiles.Count == 0
            : LowPolyFbxEntries.Any(e => !e.Found);
    }
}