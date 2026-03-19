using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PDFtoImage;
using SkiaSharp;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Сервис конвертации PDF в изображения
    /// </summary>
    public class PdfConversionService
    {
        /// <summary>
        /// Получить количество страниц в PDF
        /// </summary>
        public static int GetPageCount(string pdfPath)
        {
            return GetPageCountWithError(pdfPath, out _);
        }

        /// <summary>
        /// Получить количество страниц в PDF с информацией об ошибке
        /// </summary>
        public static int GetPageCountWithError(string pdfPath, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(pdfPath))
            {
                error = "Путь не указан";
                return 0;
            }

            if (!File.Exists(pdfPath))
            {
                error = "Файл не найден";
                return 0;
            }

            try
            {
                // Читаем файл в байты
                byte[] pdfBytes = File.ReadAllBytes(pdfPath);
                int pageCount = Conversion.GetPageCount(pdfBytes);
                return pageCount;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Debug.WriteLine($"PDF GetPageCount error: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Конвертировать PDF в PNG изображения
        /// </summary>
        public static async Task<PdfConversionResult> ConvertToPngAsync(
            string pdfPath,
            int dpi = 300,
            List<int> pages = null,
            IProgress<PdfConversionProgress> progress = null)
        {
            var result = new PdfConversionResult
            {
                SourceFile = pdfPath
            };

            if (!File.Exists(pdfPath))
            {
                result.Success = false;
                result.ErrorMessage = "Файл не найден";
                return result;
            }

            try
            {
                // Получаем имя файла без расширения
                string pdfName = Path.GetFileNameWithoutExtension(pdfPath);
                string pdfDirectory = Path.GetDirectoryName(pdfPath);

                // Создаём подпапку с именем PDF
                string outputFolder = Path.Combine(pdfDirectory, pdfName);
                Directory.CreateDirectory(outputFolder);

                result.OutputFolder = outputFolder;

                // Читаем PDF в байты
                byte[] pdfBytes = File.ReadAllBytes(pdfPath);

                // Получаем количество страниц
                int totalPages = Conversion.GetPageCount(pdfBytes);
                if (totalPages == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Не удалось прочитать PDF (0 страниц)";
                    return result;
                }

                // Если страницы не указаны — конвертируем все
                if (pages == null || pages.Count == 0)
                {
                    pages = new List<int>();
                    for (int i = 0; i < totalPages; i++)
                    {
                        pages.Add(i);
                    }
                }

                // Фильтруем невалидные номера страниц
                pages = pages.FindAll(p => p >= 0 && p < totalPages);

                if (pages.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Нет валидных страниц для конвертации";
                    return result;
                }

                result.TotalPages = pages.Count;

                await Task.Run(() =>
                {
                    int processed = 0;

                    foreach (int pageIndex in pages)
                    {
                        try
                        {
                            // Генерируем имя файла: name_001.png
                            string fileName = $"{pdfName}_{(pageIndex + 1):D3}.png";
                            string outputPath = Path.Combine(outputFolder, fileName);

                            // Конвертируем страницу с указанным DPI
                            var renderOptions = new RenderOptions
                            {
                                Dpi = dpi
                            };

                            using var bitmap = Conversion.ToImage(pdfBytes, pageIndex, options: renderOptions);

                            // Сохраняем как PNG
                            using var stream = File.Create(outputPath);
                            bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);

                            result.ConvertedFiles.Add(outputPath);
                            result.ConvertedPages++;

                            processed++;
                            progress?.Report(new PdfConversionProgress
                            {
                                CurrentPage = processed,
                                TotalPages = pages.Count,
                                CurrentFileName = fileName,
                                PercentComplete = (processed * 100) / pages.Count
                            });
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Страница {pageIndex + 1}: {ex.Message}");
                            Debug.WriteLine($"PDF page {pageIndex} error: {ex}");
                        }
                    }
                });

                result.Success = result.ConvertedPages > 0;

                if (result.Errors.Count > 0)
                {
                    result.ErrorMessage = $"Ошибки: {result.Errors.Count}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Debug.WriteLine($"PDF conversion error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Парсинг строки диапазона страниц (например: "1-5, 7, 10-15")
        /// </summary>
        public static List<int> ParsePageRange(string input, int maxPage)
        {
            var pages = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(input))
                return new List<int>();

            var parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();

                if (trimmed.Contains('-'))
                {
                    var rangeParts = trimmed.Split('-');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out int start) &&
                        int.TryParse(rangeParts[1].Trim(), out int end))
                    {
                        start = Math.Max(1, start) - 1;
                        end = Math.Min(maxPage, end) - 1;

                        for (int i = start; i <= end; i++)
                        {
                            if (i >= 0 && i < maxPage)
                                pages.Add(i);
                        }
                    }
                }
                else
                {
                    if (int.TryParse(trimmed, out int page))
                    {
                        int index = page - 1;
                        if (index >= 0 && index < maxPage)
                            pages.Add(index);
                    }
                }
            }

            var result = new List<int>(pages);
            result.Sort();
            return result;
        }
    }

    #region Supporting Types

    public class PdfConversionResult
    {
        public bool Success { get; set; }
        public string SourceFile { get; set; }
        public string OutputFolder { get; set; }
        public int TotalPages { get; set; }
        public int ConvertedPages { get; set; }
        public List<string> ConvertedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class PdfConversionProgress
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentFileName { get; set; }
        public int PercentComplete { get; set; }
    }

    #endregion
}