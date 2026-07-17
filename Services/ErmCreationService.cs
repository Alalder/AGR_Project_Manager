using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Сборка ERM-текстуры: Emission -> R, Roughness -> G, Metalness -> B.
    /// Итоговое изображение всегда 24-бит RGB без альфа-канала (альфа считается всегда 255).
    /// </summary>
    public class ErmCreationService
    {
        /// <summary>
        /// Собирает итоговое изображение заданного разрешения (квадрат size x size)
        /// </summary>
        public Image<Rgb24> Compose(ErmChannelData emission, ErmChannelData roughness, ErmChannelData metalness, int size)
        {
            byte[,] r = GetChannelData(emission, size);
            byte[,] g = GetChannelData(roughness, size);
            byte[,] b = GetChannelData(metalness, size);

            var result = new Image<Rgb24>(size, size);
            result.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < size; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < size; x++)
                    {
                        row[x] = new Rgb24(r[x, y], g[x, y], b[x, y]);
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Сохраняет изображение как 24-битный PNG без альфа-канала ("сведено в фон")
        /// </summary>
        public void Save(Image<Rgb24> image, string path)
        {
            var encoder = new PngEncoder
            {
                ColorType = PngColorType.Rgb,
                BitDepth = PngBitDepth.Bit8
            };
            image.Save(path, encoder);
        }

        private byte[,] GetChannelData(ErmChannelData channel, int size)
        {
            byte[,] data = channel.HasTexture
                ? LoadTextureAsGray(channel.TexturePath, size)
                : CreateSolid(channel.SolidValue, size);

            if (channel.Invert)
            {
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        data[x, y] = (byte)(255 - data[x, y]);
            }

            return data;
        }

        private static byte[,] CreateSolid(byte value, int size)
        {
            var data = new byte[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    data[x, y] = value;
            return data;
        }

        /// <summary>
        /// Загружает текстуру, приводит к нужному размеру и обесцвечивает по формуле
        /// Photoshop Desaturate: (max(R,G,B) + min(R,G,B)) / 2 (HSL Lightness)
        /// </summary>
        private static byte[,] LoadTextureAsGray(string path, int size)
        {
            using var image = Image.Load<Rgb24>(path);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Bicubic
            }));

            var data = new byte[size, size];
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < size; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < size; x++)
                    {
                        var px = row[x];
                        int max = Math.Max(px.R, Math.Max(px.G, px.B));
                        int min = Math.Min(px.R, Math.Min(px.G, px.B));
                        data[x, y] = (byte)((max + min) / 2);
                    }
                }
            });

            return data;
        }
    }
}