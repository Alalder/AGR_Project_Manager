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

        /// <summary>
        /// Парсит HEX код в RalColor объект
        /// Поддерживает форматы: #RRGGBB, RRGGBB
        /// </summary>
        public RalColor ParseHexColor(string hexCode)
        {
            if (string.IsNullOrWhiteSpace(hexCode))
                throw new ArgumentException("HEX код не может быть пуст");

            // Удаляем # если есть
            string cleanHex = hexCode.Trim().TrimStart('#');

            // Проверяем длину
            if (cleanHex.Length != 6)
                throw new ArgumentException("HEX код должен содержать 6 символов (например: RRGGBB или #RRGGBB)");

            // Проверяем что это шестнадцатеричные цифры
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanHex, @"^[0-9A-Fa-f]{6}$"))
                throw new ArgumentException("HEX код должен содержать только цифры (0-9) и буквы (A-F)");

            // Парсим RGB значения
            byte r = byte.Parse(cleanHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(cleanHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(cleanHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            // Создаём RalColor с HEX кодом как "Code" и "Custom HEX Color" как Name
            var color = new RalColor
            {
                Code = $"#{cleanHex.ToUpper()}",
                Name = "Custom HEX Color",
                Hex = $"#{cleanHex.ToUpper()}",
                R = r,
                G = g,
                B = b
            };

            return color;
        }

        /// <summary>
        /// Валидирует HEX код
        /// </summary>
        public bool ValidateHexCode(string hexCode)
        {
            if (string.IsNullOrWhiteSpace(hexCode))
                return false;

            string cleanHex = hexCode.Trim().TrimStart('#');

            if (cleanHex.Length != 6)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(cleanHex, @"^[0-9A-Fa-f]{6}$");
        }

        /// <summary>
        /// Генерирует имя файла для HEX цвета
        /// </summary>
        public string GenerateFileNameForHex(string hexCode, int size)
        {
            // Очищаем HEX от #
            string cleanHex = hexCode.Trim().TrimStart('#').ToUpper();
            return $"HEX_{cleanHex}_{size}.png";
        }

        /// <summary>
        /// Сохраняет HEX цвет как PNG
        /// </summary>
        public bool SaveHexColorAsPng(string hexCode, string filePath, int size)
        {
            try
            {
                var color = ParseHexColor(hexCode);
                return SaveColorAsPng(color, filePath, size);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения HEX цвета: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Конвертирует цветовую температуру (Кельвины) в RGB
        /// Алгоритм аппроксимации чёрного тела (Tanner Helland)
        /// </summary>
        public RalColor FromKelvin(int kelvin)
        {
            double temp = kelvin / 100.0;
            double red, green, blue;

            // Red
            red = temp <= 66
                ? 255
                : 329.698727446 * Math.Pow(temp - 60, -0.1332047592);

            // Green
            green = temp <= 66
                ? 99.4708025861 * Math.Log(temp) - 161.1195681661
                : 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);

            // Blue
            if (temp >= 66) blue = 255;
            else if (temp <= 19) blue = 0;
            else blue = 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;

            byte r = (byte)Math.Clamp(red, 0, 255);
            byte g = (byte)Math.Clamp(green, 0, 255);
            byte b = (byte)Math.Clamp(blue, 0, 255);

            return new RalColor
            {
                Code = $"{kelvin}K",
                Name = "Цветовая температура",
                Hex = $"#{r:X2}{g:X2}{b:X2}",
                R = r,
                G = g,
                B = b
            };
        }

        /// <summary>
        /// Генерирует имя файла для цвета по температуре
        /// </summary>
        public string GenerateFileNameForKelvin(int kelvin, int size)
        {
            return $"CCT_{kelvin}K_{size}.png";
        }

    }
}
