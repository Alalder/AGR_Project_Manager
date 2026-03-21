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
        /// Залить изображение доминантным цветом
        /// </summary>
        public static async Task<ConversionResult> FlattenToDominantColorAsync(string inputPath, string outputPath = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    outputPath ??= inputPath;
                    string tempPath = GetTempPath(outputPath);

                    // Получаем доминантный цвет
                    var (r, g, b, a) = TextureAnalysisService.GetDominantColor(inputPath);

                    using (var image = Image.Load<Rgba32>(inputPath))
                    {
                        // Заливаем всё изображение одним цветом
                        var fillColor = new Rgba32(r, g, b, a);

                        image.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < accessor.Height; y++)
                            {
                                Span<Rgba32> row = accessor.GetRowSpan(y);
                                for (int x = 0; x < row.Length; x++)
                                {
                                    row[x] = fillColor;
                                }
                            }
                        });

                        // Сохраняем
                        bool hasAlpha = a < 255;
                        if (hasAlpha)
                        {
                            SavePngRgba(image, tempPath);
                        }
                        else
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath);
                        }
                    }

                    FinalizeFile(tempPath, outputPath);

                    string colorHex = $"#{r:X2}{g:X2}{b:X2}";
                    return new ConversionResult
                    {
                        Success = true,
                        OutputPath = outputPath,
                        Message = $"Залито цветом {colorHex}"
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
                        // Сохраняем как полноцветный 8-bit
                        using var rgba = image.CloneAs<Rgba32>();
                        SavePngRgba(rgba, tempPath);
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

                        // Сохраняем как полноцветный
                        using var rgba = image.CloneAs<Rgba32>();
                        SavePngRgba(rgba, tempPath);
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
        /// Оптимизировать PNG с размытием для уменьшения размера
        /// </summary>
        /// <param name="inputPath">Путь к файлу</param>
        /// <param name="mode">Режим оптимизации</param>
        /// <param name="outputPath">Путь для сохранения (null = перезаписать)</param>
        public static async Task<ConversionResult> OptimizePngAsync(
            string inputPath,
            OptimizationMode mode = OptimizationMode.LightBlur,
            string outputPath = null)
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
                        // Применяем обработку в зависимости от режима
                        switch (mode)
                        {
                            case OptimizationMode.Recompress:
                                // Просто пересохранение с максимальным сжатием
                                break;

                            case OptimizationMode.LightBlur:
                                // Лёгкое размытие (sigma 0.3-0.5) — почти незаметно, но помогает
                                image.Mutate(x => x.GaussianBlur(0.4f));
                                break;

                            case OptimizationMode.MediumBlur:
                                // Среднее размытие (sigma 0.7-1.0) — немного заметно
                                image.Mutate(x => x.GaussianBlur(0.8f));
                                break;

                            case OptimizationMode.StrongBlur:
                                // Сильное размытие (sigma 1.5-2.0) — заметно, но сильное сжатие
                                image.Mutate(x => x.GaussianBlur(1.5f));
                                break;

                            case OptimizationMode.Denoise:
                                // Медианный фильтр — хорошо убирает шум, сохраняет края
                                // ImageSharp не имеет встроенного медианного фильтра,
                                // используем лёгкое размытие + повышение контраста
                                image.Mutate(x => x
                                    .GaussianBlur(0.5f)
                                    .Contrast(1.05f));
                                break;
                        }

                        // Сохраняем как полноцветный 8-bit RGB или RGBA
                        using var rgba = image.CloneAs<Rgba32>();
                        bool hasAlpha = HasMeaningfulAlpha(rgba);

                        if (hasAlpha)
                        {
                            SavePngRgba(rgba, tempPath);
                        }
                        else
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath);
                        }
                    }

                    long newSize = new FileInfo(tempPath).Length;

                    FinalizeFile(tempPath, outputPath);

                    long saved = originalSize - newSize;
                    double percent = originalSize > 0 ? (double)saved / originalSize * 100 : 0;

                    string modeDesc = mode switch
                    {
                        OptimizationMode.Recompress => "пересжатие",
                        OptimizationMode.LightBlur => "лёгкое размытие",
                        OptimizationMode.MediumBlur => "среднее размытие",
                        OptimizationMode.StrongBlur => "сильное размытие",
                        OptimizationMode.Denoise => "шумоподавление",
                        _ => ""
                    };

                    if (saved > 0)
                    {
                        return new ConversionResult
                        {
                            Success = true,
                            OutputPath = outputPath,
                            Message = $"-{percent:F1}% ({modeDesc}, -{FormatSize(saved)})"
                        };
                    }
                    else
                    {
                        return new ConversionResult
                        {
                            Success = true,
                            OutputPath = outputPath,
                            Message = $"Обработано ({modeDesc})"
                        };
                    }
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
        /// Применить размытие по Гауссу с указанным радиусом
        /// </summary>
        public static async Task<ConversionResult> ApplyGaussianBlurAsync(
            string inputPath,
            float sigma,
            string outputPath = null)
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
                        image.Mutate(x => x.GaussianBlur(sigma));

                        // Сохраняем как полноцветный 8-bit
                        using var rgba = image.CloneAs<Rgba32>();
                        bool hasAlpha = HasMeaningfulAlpha(rgba);

                        if (hasAlpha)
                        {
                            SavePngRgba(rgba, tempPath);
                        }
                        else
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath);
                        }
                    }

                    FinalizeFile(tempPath, outputPath);

                    long newSize = new FileInfo(outputPath).Length;
                    long saved = originalSize - newSize;
                    double percent = originalSize > 0 ? (double)saved / originalSize * 100 : 0;

                    if (saved > 0)
                    {
                        return new ConversionResult
                        {
                            Success = true,
                            OutputPath = outputPath,
                            Message = $"Размытие σ={sigma:F1}, -{percent:F1}% (-{FormatSize(saved)})"
                        };
                    }
                    else
                    {
                        return new ConversionResult
                        {
                            Success = true,
                            OutputPath = outputPath,
                            Message = $"Размытие σ={sigma:F1} применено"
                        };
                    }
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

                        // Сохранение как полноцветный 8-bit
                        if (options.RemoveAlpha)
                        {
                            using var rgb = image.CloneAs<Rgb24>();
                            SavePngRgb(rgb, tempPath);
                        }
                        else
                        {
                            using var rgba = image.CloneAs<Rgba32>();
                            bool hasAlpha = HasMeaningfulAlpha(rgba);

                            if (hasAlpha)
                            {
                                SavePngRgba(rgba, tempPath);
                            }
                            else
                            {
                                using var rgb = image.CloneAs<Rgb24>();
                                SavePngRgb(rgb, tempPath);
                            }
                        }
                    }

                    FinalizeFile(tempPath, outputPath);

                    long newSize = new FileInfo(outputPath).Length;
                    string sizeInfo = newSize < originalSize
                        ? $" (-{(originalSize - newSize) * 100 / originalSize}%)"
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

        private static void SavePngRgb(Image<Rgb24> image, string path)
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Rgb,
                BitDepth = PngBitDepth.Bit8,
                FilterMethod = PngFilterMethod.Adaptive
            };
            image.SaveAsPng(path, encoder);
        }

        private static void SavePngRgba(Image<Rgba32> image, string path)
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.RgbWithAlpha,
                BitDepth = PngBitDepth.Bit8,
                FilterMethod = PngFilterMethod.Adaptive
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
    /// Режим оптимизации
    /// </summary>
    public enum OptimizationMode
    {
        /// <summary>
        /// Только пересжатие (без изменения изображения)
        /// </summary>
        Recompress,

        /// <summary>
        /// Лёгкое размытие по Гауссу (σ=0.4) — почти незаметно
        /// </summary>
        LightBlur,

        /// <summary>
        /// Среднее размытие (σ=0.8) — немного заметно
        /// </summary>
        MediumBlur,

        /// <summary>
        /// Сильное размытие (σ=1.5) — заметно, сильное сжатие
        /// </summary>
        StrongBlur,

        /// <summary>
        /// Шумоподавление (размытие + контраст)
        /// </summary>
        Denoise
    }

       /// <summary>
    /// Опции обработки текстуры
    /// </summary>
    public class TextureProcessingOptions
    {
        public bool ConvertTo8Bit { get; set; } = true;
        public bool ConvertToPng { get; set; } = true;
        public bool RemoveAlpha { get; set; }
        public bool OptimizeCompression { get; set; }
        public int ResizeWidth { get; set; }
        public int ResizeHeight { get; set; }
    }

    #endregion
}