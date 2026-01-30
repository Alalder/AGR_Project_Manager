using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class RalColorService
    {
        /// <summary>
        /// Сохраняет цвет как PNG без альфа-канала (Background layer для Photoshop)
        /// </summary>
        public bool SaveColorAsPng(RalColor color, string filePath, int size)
        {
            try
            {
                // Создаём bitmap с форматом BGR24 (без альфа-канала!)
                // Это создаёт "Background" слой в Photoshop
                var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr24, null);

                // Заполняем цветом
                int bytesPerPixel = 3; // BGR24 = 3 байта на пиксель
                int stride = size * bytesPerPixel;
                byte[] pixels = new byte[size * stride];

                for (int i = 0; i < pixels.Length; i += bytesPerPixel)
                {
                    pixels[i] = color.B;     // Blue
                    pixels[i + 1] = color.G; // Green
                    pixels[i + 2] = color.R; // Red
                }

                bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);

                // Сохраняем как PNG
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Генерирует имя файла для цвета
        /// </summary>
        public string GenerateFileName(RalColor color, int size)
        {
            // RAL 1000 -> RAL_1000_256.png
            string safeName = color.Code.Replace(" ", "_");
            return $"{safeName}_{size}.png";
        }
    }
}