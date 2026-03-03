using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Сервис конвертации и обработки текстур
    /// </summary>
    public class TextureConversionService
    {
        #region Public Methods

        /// <summary>
        /// Конвертировать изображение в 8-bit PNG
        /// </summary>
        public static async Task<ConversionResult> ConvertTo8BitPngAsync(
            string inputPath,
            string outputPath = null,
            bool removeAlpha = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        if (removeAlpha)
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath);
                        }
                        else
                        {
                            using var rgba = image.CloneAs<Rgba32>();
                            SavePngRgba(rgba, tempPath);
                        }
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = "Конвертация завершена"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Удалить альфа-канал из изображения
        /// </summary>
        public static async Task<ConversionResult> RemoveAlphaAsync(string inputPath, string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        using var rgb = image.CloneAs<Rgb24>();
                        SavePngRgb(rgb, tempPath);
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = "Альфа-канал удалён"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Добавить альфа-канал (RGB → RGBA)
        /// </summary>
        public static async Task<ConversionResult> AddAlphaAsync(string inputPath, string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        using var rgba = image.CloneAs<Rgba32>();
                        SavePngRgba(rgba, tempPath);
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = "Альфа-канал добавлен"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Конвертировать в PNG формат
        /// </summary>
        public static async Task<ConversionResult> ConvertToPngAsync(string inputPath, string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        outputPath = Path.ChangeExtension(inputPath, ".png");
                    }

                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        var encoder = new PngEncoder
                        {
                            CompressionLevel = PngCompressionLevel.BestCompression,
                            BitDepth = PngBitDepth.Bit8
                        };
                        image.SaveAsPng(tempPath, encoder);
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = "Конвертировано в PNG"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Изменить размер изображения
        /// </summary>
        public static async Task<ConversionResult> ResizeAsync(
            string inputPath,
            int newWidth,
            int newHeight,
            string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        if (newWidth == 0 && newHeight > 0)
                        {
                            newWidth = (int)((float)image.Width / image.Height * newHeight);
                        }
                        else if (newHeight == 0 && newWidth > 0)
                        {
                            newHeight = (int)((float)image.Height / image.Width * newWidth);
                        }

                        image.Mutate(x => x.Resize(newWidth, newHeight));

                        var encoder = new PngEncoder
                        {
                            CompressionLevel = PngCompressionLevel.BestCompression
                        };
                        image.SaveAsPng(tempPath, encoder);
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = $"Размер изменён на {newWidth}×{newHeight}"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Изменить размер до ближайшей степени двойки
        /// </summary>
        public static async Task<ConversionResult> ResizeToPowerOfTwoAsync(
            string inputPath,
            PowerOfTwoMode mode = PowerOfTwoMode.Nearest,
            string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    using (var image = Image.Load(inputPath))
                    {
                        int newWidth = GetPowerOfTwo(image.Width, mode);
                        int newHeight = GetPowerOfTwo(image.Height, mode);

                        if (newWidth != image.Width || newHeight != image.Height)
                        {
                            image.Mutate(x => x.Resize(newWidth, newHeight));
                        }

                        var encoder = new PngEncoder
                        {
                            CompressionLevel = PngCompressionLevel.BestCompression
                        };
                        image.SaveAsPng(tempPath, encoder);
                    }

                    FinalizeFile(tempPath, outputPath);

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = "Размер приведён к степени двойки"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Оптимизировать PNG (максимальное сжатие)
        /// </summary>
        public static async Task<ConversionResult> OptimizePngAsync(string inputPath, string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    long originalSize = new FileInfo(inputPath).Length;

                    using (var image = Image.Load(inputPath))
                    {
                        var encoder = new PngEncoder
                        {
                            CompressionLevel = PngCompressionLevel.BestCompression,
                            FilterMethod = PngFilterMethod.Adaptive,
                            BitDepth = PngBitDepth.Bit8
                        };
                        image.SaveAsPng(tempPath, encoder);
                    }

                    long newSize = new FileInfo(tempPath).Length;

                    if (newSize >= originalSize && outputPath == inputPath)
                    {
                        File.Delete(tempPath);
                        return new ConversionResult
                        {
                            Success = true,
                            OutputPath = outputPath,
                            Message = "Файл уже оптимален"
                        };
                    }

                    FinalizeFile(tempPath, outputPath);

                    long saved = originalSize - newSize;
                    double percent = (double)saved / originalSize * 100;

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = $"Сжато на {percent:F1}% (сохранено {FormatSize(saved)})"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Комплексная обработка: 8-bit + PNG + опционально удаление альфы
        /// </summary>
        public async Task<ConversionResult> ProcessTextureAsync(
            string inputPath,
            TextureProcessingOptions options,
            string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;

                    if (options.ConvertToPng && !inputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath = Path.ChangeExtension(outputPath, ".png");
                    }

                    string tempPath = GetTempPath(outputPath);
                    long originalSize = new FileInfo(inputPath).Length;

                    using (var image = Image.Load(inputPath))
                    {
                        // Ресайз если нужно
                        if (options.ResizeWidth > 0 || options.ResizeHeight > 0)
                        {
                            int w = options.ResizeWidth > 0 ? options.ResizeWidth : image.Width;
                            int h = options.ResizeHeight > 0 ? options.ResizeHeight : image.Height;

                            if (options.ResizeWidth == 0)
                                w = (int)((float)image.Width / image.Height * h);
                            if (options.ResizeHeight == 0)
                                h = (int)((float)image.Height / image.Width * w);

                            image.Mutate(x => x.Resize(w, h));
                        }
                        else if (options.ResizeToPowerOfTwo)
                        {
                            int newW = GetPowerOfTwo(image.Width, PowerOfTwoMode.Nearest);
                            int newH = GetPowerOfTwo(image.Height, PowerOfTwoMode.Nearest);
                            if (newW != image.Width || newH != image.Height)
                            {
                                image.Mutate(x => x.Resize(newW, newH));
                            }
                        }

                        // Сохранение с нужными параметрами
                        if (options.RemoveAlpha)
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath, options.OptimizeCompression);
                        }
                        else if (options.AddAlpha)
                        {
                            using var rgba = image.CloneAs<Rgba32>();
                            SavePngRgba(rgba, tempPath, options.OptimizeCompression);
                        }
                        else if (options.ConvertTo8Bit)
                        {
                            using var rgba = image.CloneAs<Rgba32>();
                            bool hasAlpha = HasMeaningfulAlpha(rgba);

                            if (hasAlpha)
                            {
                                SavePngRgba(rgba, tempPath, options.OptimizeCompression);
                            }
                            else
                            {
                                using var rgb = image.CloneAs<Rgb24>();
                                SavePngRgb(rgb, tempPath, options.OptimizeCompression);
                            }
                        }
                        else
                        {
                            var encoder = new PngEncoder
                            {
                                CompressionLevel = options.OptimizeCompression
                                    ? PngCompressionLevel.BestCompression
                                    : PngCompressionLevel.DefaultCompression
                            };
                            image.SaveAsPng(tempPath, encoder);
                        }
                    }

                    FinalizeFile(tempPath, outputPath);

                    long newSize = new FileInfo(outputPath).Length;
                    string sizeInfo = newSize < originalSize
                        ? $" (сжато на {(originalSize - newSize) * 100 / originalSize}%)"
                        : "";

                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = $"Обработка завершена{sizeInfo}"
                    };
                }
                catch (Exception ex)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"Ошибка: {ex.Message}"
                    };
                }
            });
        }

        #endregion

        #region Private Helpers

        private static void SavePngRgb(Image<Rgb24> image, string path, bool optimize = true)
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = optimize ? PngCompressionLevel.BestCompression : PngCompressionLevel.DefaultCompression,
                ColorType = PngColorType.Rgb,
                BitDepth = PngBitDepth.Bit8,
                FilterMethod = optimize ? PngFilterMethod.Adaptive : PngFilterMethod.None
            };
            image.SaveAsPng(path, encoder);
        }

        private static void SavePngRgba(Image<Rgba32> image, string path, bool optimize = true)
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = optimize ? PngCompressionLevel.BestCompression : PngCompressionLevel.DefaultCompression,
                ColorType = PngColorType.RgbWithAlpha,
                BitDepth = PngBitDepth.Bit8,
                FilterMethod = optimize ? PngFilterMethod.Adaptive : PngFilterMethod.None
            };
            image.SaveAsPng(path, encoder);
        }

        private static string GetTempPath(string outputPath)
        {
            return outputPath + ".tmp";
        }

        private static void FinalizeFile(string tempPath, string outputPath)
        {
            if (File.Exists(outputPath) && tempPath != outputPath)
            {
                File.Delete(outputPath);
            }
            File.Move(tempPath, outputPath);
        }

        private static int GetPowerOfTwo(int value, PowerOfTwoMode mode)
        {
            if (value <= 0) return 1;

            int lower = 1;
            while (lower * 2 <= value) lower *= 2;
            int upper = lower * 2;

            return mode switch
            {
                PowerOfTwoMode.Up => upper,
                PowerOfTwoMode.Down => lower,
                PowerOfTwoMode.Nearest => (value - lower) < (upper - value) ? lower : upper,
                _ => lower
            };
        }

        private static bool HasMeaningfulAlpha(Image<Rgba32> image)
        {
            bool hasTransparency = false;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        if (row[x].A < 255)
                        {
                            hasTransparency = true;
                            return;
                        }
                    }
                    if (hasTransparency) return;
                }
            });

            return hasTransparency;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Результат конвертации
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Режим округления до степени двойки
    /// </summary>
    public enum PowerOfTwoMode
    {
        Nearest,
        Up,
        Down
    }

    /// <summary>
    /// Опции обработки текстуры
    /// </summary>
    public class TextureProcessingOptions
    {
        public bool ConvertTo8Bit { get; set; } = true;
        public bool ConvertToPng { get; set; } = true;
        public bool RemoveAlpha { get; set; }
        public bool AddAlpha { get; set; }
        public bool OptimizeCompression { get; set; } = true;
        public bool ResizeToPowerOfTwo { get; set; }
        public int ResizeWidth { get; set; }
        public int ResizeHeight { get; set; }
        public bool DeleteOriginalOnFormatChange { get; set; }
    }

    #endregion
}