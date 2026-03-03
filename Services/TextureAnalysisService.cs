using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AGR_Project_Manager.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Сервис анализа текстур
    /// </summary>
    public class TextureAnalysisService
    {
        // Поддерживаемые расширения
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".tiff", ".tif", ".bmp", ".gif", ".webp"
        };

        /// <summary>
        /// Проверяет, является ли файл поддерживаемым изображением
        /// </summary>
        public static bool IsSupportedImage(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return SupportedExtensions.Contains(ext);
        }

        /// <summary>
        /// Анализ одного файла
        /// </summary>
        public static TextureInfo AnalyzeFile(string filePath)
        {
            var info = new TextureInfo
            {
                FilePath = filePath
            };

            try
            {
                // Получаем размер файла
                var fileInfo = new FileInfo(filePath);
                info.FileSize = fileInfo.Length;

                // Используем Identify для быстрого получения метаданных без полной загрузки
                ImageInfo imageInfo = Image.Identify(filePath);

                if (imageInfo == null)
                {
                    info.HasError = true;
                    info.ErrorMessage = "Не удалось прочитать изображение";
                    info.UpdateStatus();
                    return info;
                }

                info.Width = imageInfo.Width;
                info.Height = imageInfo.Height;

                // Определяем формат
                info.Format = GetFormatName(imageInfo.Metadata.DecodedImageFormat);

                // Определяем битность и каналы
                PixelTypeInfo pixelType = imageInfo.PixelType;
                info.BitsPerChannel = GetBitsPerChannel(pixelType);
                info.ChannelCount = GetChannelCount(pixelType);

                info.UpdateStatus();
            }
            catch (Exception ex)
            {
                info.HasError = true;
                info.ErrorMessage = ex.Message;
                info.Status = TextureStatus.Error;
                info.StatusMessage = $"Ошибка: {ex.Message}";
            }

            return info;
        }

        /// <summary>
        /// Анализ папки с текстурами
        /// </summary>
        public async Task<List<TextureInfo>> AnalyzeFolderAsync(string folderPath, IProgress<int> progress = null)
        {
            var results = new List<TextureInfo>();

            if (!Directory.Exists(folderPath))
                return results;

            var files = Directory.GetFiles(folderPath)
                .Where(f => IsSupportedImage(f))
                .ToList();

            int processed = 0;
            int total = files.Count;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    var info = AnalyzeFile(file);
                    results.Add(info);

                    processed++;
                    progress?.Report(total > 0 ? (processed * 100) / total : 100);
                }
            });

            return results;
        }

        /// <summary>
        /// Анализ папки с подпапками
        /// </summary>
        public async Task<List<TextureInfo>> AnalyzeFolderRecursiveAsync(string folderPath, IProgress<int> progress = null)
        {
            var results = new List<TextureInfo>();

            if (!Directory.Exists(folderPath))
                return results;

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSupportedImage(f))
                .ToList();

            int processed = 0;
            int total = files.Count;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    var info = AnalyzeFile(file);
                    results.Add(info);

                    processed++;
                    progress?.Report(total > 0 ? (processed * 100) / total : 100);
                }
            });

            return results;
        }

        /// <summary>
        /// Получить суммарный размер файлов
        /// </summary>
        public static long GetTotalSize(IEnumerable<TextureInfo> textures)
        {
            return textures.Sum(t => t.FileSize);
        }

        /// <summary>
        /// Получить статистику по папке
        /// </summary>
        public static TextureFolderStats GetFolderStats(IEnumerable<TextureInfo> textures)
        {
            var list = textures.ToList();

            return new TextureFolderStats
            {
                TotalFiles = list.Count,
                TotalSize = list.Sum(t => t.FileSize),
                FilesOk = list.Count(t => t.Status == TextureStatus.Ok),
                FilesWarning = list.Count(t => t.Status == TextureStatus.Warning),
                FilesError = list.Count(t => t.Status == TextureStatus.Error),
                Files16Bit = list.Count(t => t.BitsPerChannel > 8),
                FilesWithAlpha = list.Count(t => t.HasAlpha),
                FilesNotPng = list.Count(t => t.WrongFormat),
                FilesNotPow2 = list.Count(t => !t.IsPowerOfTwo)
            };
        }

        #region Private Helpers

        private static string GetFormatName(IImageFormat format)
        {
            if (format == null)
                return "Unknown";

            return format.Name.ToUpperInvariant();
        }

        private static int GetBitsPerChannel(PixelTypeInfo pixelType)
        {
            int bitsPerPixel = pixelType.BitsPerPixel;

            // Определяем количество компонентов
            int components = GetChannelCount(pixelType);

            if (components > 0)
            {
                return bitsPerPixel / components;
            }

            // Fallback: предполагаем стандартные варианты
            return bitsPerPixel switch
            {
                8 => 8,     // Grayscale 8bit
                16 => 8,    // Grayscale 16bit или RGB565
                24 => 8,    // RGB 8bit
                32 => 8,    // RGBA 8bit
                48 => 16,   // RGB 16bit
                64 => 16,   // RGBA 16bit
                96 => 32,   // RGB 32bit (float)
                128 => 32,  // RGBA 32bit (float)
                _ => 8
            };
        }

        private static int GetChannelCount(PixelTypeInfo pixelType)
        {
            int bpp = pixelType.BitsPerPixel;

            // Определяем по общему количеству бит
            return bpp switch
            {
                8 => 1,      // Grayscale
                16 => 1,     // Grayscale 16bit
                24 => 3,     // RGB
                32 => 4,     // RGBA
                48 => 3,     // RGB 16bit
                64 => 4,     // RGBA 16bit
                96 => 3,     // RGB 32bit float
                128 => 4,    // RGBA 32bit float
                _ => (bpp + 7) / 8
            };
        }

        #endregion
    }

    /// <summary>
    /// Статистика по папке с текстурами
    /// </summary>
    public class TextureFolderStats
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public string TotalSizeFormatted => TextureInfo.FormatSize(TotalSize);

        public int FilesOk { get; set; }
        public int FilesWarning { get; set; }
        public int FilesError { get; set; }

        public int Files16Bit { get; set; }
        public int FilesWithAlpha { get; set; }
        public int FilesNotPng { get; set; }
        public int FilesNotPow2 { get; set; }
    }
}