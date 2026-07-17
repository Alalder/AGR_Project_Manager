namespace AGR_Project_Manager.Models
{
    /// <summary>
    /// Данные одного канала ERM-текстуры (Emission / Roughness / Metalness).
    /// Либо путь к загруженной текстуре, либо сплошное значение 0-255.
    /// </summary>
    public class ErmChannelData
    {
        public string TexturePath { get; set; }
        public byte SolidValue { get; set; } = 0;

        /// <summary>Инверсия канала (актуально для Roughness, если загружена Glossiness-текстура)</summary>
        public bool Invert { get; set; } = false;

        public bool HasTexture => !string.IsNullOrEmpty(TexturePath);

        public void Clear()
        {
            TexturePath = null;
            SolidValue = 0;
            Invert = false;
        }
    }
}