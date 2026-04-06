using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace AGR_Project_Manager.Services
{
    public static class TextureResolutionHelper
    {
        /// <summary>
        /// Определяет разрешение текстуры и возвращает его в виде строки (4K, 2K, 1K, 512, 256-)
        /// </summary>
        public static string GetResolutionLabel(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);

                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        int maxDimension = Math.Max(frame.PixelWidth, frame.PixelHeight);

                        // Определяем категорию разрешения
                        if (maxDimension >= 4096)
                            return "4K";
                        else if (maxDimension >= 2048)
                            return "2K";
                        else if (maxDimension >= 1024)
                            return "1K";
                        else if (maxDimension >= 512)
                            return "512";
                        else
                            return "256-";
                    }
                }
            }
            catch
            {
                // Если не удалось прочитать - возвращаем null
            }

            return null;
        }

        /// <summary>
        /// Получает цвет фона для бейджа разрешения
        /// </summary>
        public static string GetResolutionColor(string resolution)
        {
            return resolution switch
            {
                "4K" => "Resolution4K",
                "2K" => "Resolution2K",
                "1K" => "Resolution1K",
                "512" => "Resolution512",
                "256-" => "Resolution256",
                _ => "TextureZoneEmpty"
            };
        }
    }
}