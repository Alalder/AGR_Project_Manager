using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    /// <summary>
    /// Информация о текстуре для анализа и отображения
    /// </summary>
    public class TextureInfo : INotifyPropertyChanged
    {
        private bool _isSelected;

        #region Properties

        /// <summary>
        /// Полный путь к файлу
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Имя файла без пути
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        /// Только имя без расширения
        /// </summary>
        public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FilePath);

        /// <summary>
        /// Ширина в пикселях
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Высота в пикселях
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Разрешение в формате "1024 × 1024"
        /// </summary>
        public string Resolution => $"{Width} × {Height}";

        /// <summary>
        /// Бит на канал (8, 16, 32)
        /// </summary>
        public int BitsPerChannel { get; set; }

        /// <summary>
        /// Отображение битности
        /// </summary>
        public string BitsDisplay => $"{BitsPerChannel} bit";

        /// <summary>
        /// Количество каналов (3 = RGB, 4 = RGBA)
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// Есть ли альфа-канал
        /// </summary>
        public bool HasAlpha => ChannelCount == 4;

        /// <summary>
        /// Индексированное изображение (палитра)
        /// </summary>
        public bool IsIndexed => ChannelCount == 1 && BitsPerChannel <= 8;

        /// <summary>
        /// Отображение каналов
        /// </summary>
        public string ChannelsDisplay
        {
            get
            {
                if (IsIndexed)
                    return "Indexed";
                return ChannelCount switch
                {
                    4 => "RGBA",
                    3 => "RGB",
                    1 => "Gray",
                    _ => $"{ChannelCount}ch"
                };
            }
        }

        /// <summary>
        /// Размер файла в байтах
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Отформатированный размер файла
        /// </summary>
        public string FileSizeDisplay => FormatFileSize(FileSize);

        /// <summary>
        /// Формат файла (PNG, JPG, TGA и т.д.)
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Расширение файла
        /// </summary>
        public string Extension => Path.GetExtension(FilePath)?.ToUpperInvariant().TrimStart('.') ?? "";

        /// <summary>
        /// Выбран ли файл в таблице
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Статус текстуры
        /// </summary>
        public TextureStatus Status { get; set; } = TextureStatus.Ok;

        /// <summary>
        /// Описание проблемы (если есть)
        /// </summary>
        public string StatusMessage { get; set; } = "";

        /// <summary>
        /// Квадратная ли текстура
        /// </summary>
        public bool IsSquare => Width == Height;

        /// <summary>
        /// Является ли размер степенью двойки
        /// </summary>
        public bool IsPowerOfTwo => IsPow2(Width) && IsPow2(Height);

        /// <summary>
        /// Ошибка при загрузке
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string ErrorMessage { get; set; }

        #endregion

        #region Color Analysis (для заглушек 256×256)

        /// <summary>
        /// Является ли текстура заглушкой (256×256)
        /// </summary>
        public bool IsStubTexture => Width == 256 && Height == 256;

        /// <summary>
        /// Количество уникальных цветов (только для 256×256)
        /// </summary>
        public int? UniqueColorCount { get; set; }

        /// <summary>
        /// Отображение количества цветов
        /// </summary>
        public string UniqueColorsDisplay
        {
            get
            {
                if (!IsStubTexture)
                    return "—";
                if (UniqueColorCount == null)
                    return "?";
                return UniqueColorCount.Value.ToString();
            }
        }

        /// <summary>
        /// Нужно ли выравнивание цвета (больше 1 уникального цвета)
        /// </summary>
        public bool NeedsColorFlattening => IsStubTexture && UniqueColorCount.HasValue && UniqueColorCount.Value > 1;

        #endregion

        #region Status Checking

        /// <summary>
        /// Нужна ли конвертация в 8 bit
        /// </summary>
        public bool Needs8BitConversion => BitsPerChannel > 8;

        /// <summary>
        /// Нужно ли удаление альфа-канала (можно пометить вручную)
        /// </summary>
        public bool NeedsAlphaRemoval { get; set; }

        /// <summary>
        /// Неправильный формат (не PNG)
        /// </summary>
        public bool WrongFormat => !Format.Equals("PNG", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Обновить статус на основе параметров
        /// </summary>
        public void UpdateStatus()
        {
            if (HasError)
            {
                Status = TextureStatus.Error;
                StatusMessage = ErrorMessage;
                return;
            }

            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            // Ошибки (критичные проблемы)
            if (IsIndexed)
                errors.Add("Индексированные цвета");

            if (Needs8BitConversion)
                errors.Add($"{BitsPerChannel} bit → нужен 8 bit");

            if (WrongFormat)
                errors.Add($"Формат {Format} → нужен PNG");

            // Проблема с заглушкой (много цветов)
            if (NeedsColorFlattening)
                errors.Add($"Заглушка: {UniqueColorCount} цветов → нужен 1");

            // Предупреждения (некритичные)
            if (HasAlpha)
                warnings.Add("Есть альфа-канал");

            // Определяем итоговый статус
            if (errors.Count > 0)
            {
                Status = TextureStatus.Error;
                StatusMessage = string.Join("; ", errors);
            }
            else if (warnings.Count > 0)
            {
                Status = TextureStatus.Warning;
                StatusMessage = string.Join("; ", warnings);
            }
            else
            {
                Status = TextureStatus.Ok;
                StatusMessage = "✓ OK";
            }
        }

        #endregion

        #region Helpers

        private static bool IsPow2(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        /// <summary>
        /// Форматирование размера (статический метод для суммарного размера)
        /// </summary>
        public static string FormatSize(long bytes) => FormatFileSize(bytes);

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Статус текстуры
    /// </summary>
    public enum TextureStatus
    {
        Ok,
        Warning,
        Error
    }
}