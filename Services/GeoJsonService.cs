using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class GeoJsonService
    {
        public string GenerateGeoJson(GeoJsonData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"type\": \"FeatureCollection\",");
            sb.AppendLine("  \"features\": [{");
            sb.AppendLine("    \"type\": \"ObjectFeature\",");
            sb.AppendLine("    \"properties\": {");

            // Properties
            sb.AppendLine($"      \"address\": \"{Escape(data.Address)}\",");
            sb.AppendLine($"      \"okrug\": \"{Escape(data.Okrug)}\",");
            sb.AppendLine($"      \"rajon\": \"{Escape(data.Rajon)}\",");
            sb.AppendLine($"      \"name\": \"{Escape(data.Name)}\",");
            sb.AppendLine($"      \"developer\": \"{Escape(data.Developer)}\",");
            sb.AppendLine($"      \"designer\": \"{Escape(data.Designer)}\",");
            sb.AppendLine($"      \"cadNum\": \"{Escape(data.CadNum)}\",");
            sb.AppendLine($"      \"FNO_code\": \"{Escape(data.FnoCode)}\",");
            sb.AppendLine($"      \"FNO_name\": \"{Escape(data.FnoName)}\",");
            sb.AppendLine($"      \"ZU_area\": \"{Escape(data.ZuArea)}\",");
            sb.AppendLine($"      \"h_relief\": \"{Escape(data.HRelief)}\",");
            sb.AppendLine($"      \"h_otn\": \"{Escape(data.HOtn)}\",");
            sb.AppendLine($"      \"h_abs\": \"{Escape(data.HAbs)}\",");
            sb.AppendLine($"      \"s_obsh\": \"{Escape(data.SObsh)}\",");
            sb.AppendLine($"      \"s_naz\": \"{Escape(data.SNaz)}\",");
            sb.AppendLine($"      \"s_podz\": \"{Escape(data.SPodz)}\",");
            sb.AppendLine($"      \"spp_gns\": \"{Escape(data.SppGns)}\",");
            sb.AppendLine($"      \"act_AGR\": \"{Escape(data.ActAgr)}\",");
            sb.AppendLine($"      \"imageBase64\": \"{data.ImageBase64 ?? ""}\",");
            sb.AppendLine($"      \"other\": \"{Escape(data.Other)}\"");
            sb.AppendLine("    },");

            // Geometry
            double.TryParse(data.CoordX?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double x);
            double.TryParse(data.CoordY?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double y);

            sb.AppendLine("    \"geometry\": {");
            sb.AppendLine("      \"type\": \"Point\",");
            sb.AppendLine($"      \"coordinates\": [{FormatDouble(x)}, {FormatDouble(y)}]");
            sb.AppendLine("    },");

            // Glasses
            sb.AppendLine("    \"Glasses\": [");
            if (data.Glasses != null && data.Glasses.Count > 0)
            {
                sb.AppendLine("      {");
                var glassList = new List<string>();
                foreach (var glass in data.Glasses)
                {
                    var glassStr = new StringBuilder();
                    glassStr.AppendLine($"        \"{Escape(glass.Name)}\": {{");
                    glassStr.AppendLine("          \"color_RGB\": {");
                    glassStr.AppendLine($"            \"Red\": \"{glass.Red}\",");
                    glassStr.AppendLine($"            \"Green\": \"{glass.Green}\",");
                    glassStr.AppendLine($"            \"Blue\": \"{glass.Blue}\"");
                    glassStr.AppendLine("          },");
                    glassStr.AppendLine($"          \"transparency\": \"{Escape(glass.Transparency)}\",");
                    glassStr.AppendLine($"          \"refraction\": \"{Escape(glass.Refraction)}\",");
                    glassStr.AppendLine($"          \"roughness\": \"{Escape(glass.Roughness)}\",");
                    glassStr.AppendLine($"          \"metallicity\": \"{Escape(glass.Metallicity)}\"");
                    glassStr.Append("        }");
                    glassList.Add(glassStr.ToString());
                }
                sb.AppendLine(string.Join(",\n", glassList));
                sb.AppendLine("      }");
            }
            sb.AppendLine("    ]");
            sb.AppendLine("  }]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public GeoJsonData ParseGeoJson(string json)
        {
            var data = new GeoJsonData();

            try
            {
                var doc = JsonNode.Parse(json);
                var features = doc?["features"]?.AsArray();

                if (features == null || features.Count == 0)
                    throw new Exception("Некорректная структура GEOJSON файла");

                var feature = features[0];
                var props = feature?["properties"];

                if (props != null)
                {
                    data.Address = props["address"]?.ToString() ?? "";
                    data.Okrug = props["okrug"]?.ToString() ?? "";
                    data.Rajon = props["rajon"]?.ToString() ?? "";
                    data.Name = props["name"]?.ToString() ?? "";
                    data.Developer = props["developer"]?.ToString() ?? "";
                    data.Designer = props["designer"]?.ToString() ?? "";
                    data.CadNum = props["cadNum"]?.ToString() ?? "";
                    data.FnoCode = props["FNO_code"]?.ToString() ?? "";
                    data.FnoName = props["FNO_name"]?.ToString() ?? "";
                    data.ZuArea = props["ZU_area"]?.ToString() ?? "";
                    data.HRelief = props["h_relief"]?.ToString() ?? "";
                    data.HOtn = props["h_otn"]?.ToString() ?? "";
                    data.HAbs = props["h_abs"]?.ToString() ?? "";
                    data.SObsh = props["s_obsh"]?.ToString() ?? "";
                    data.SNaz = props["s_naz"]?.ToString() ?? "";
                    data.SPodz = props["s_podz"]?.ToString() ?? "";
                    data.SppGns = props["spp_gns"]?.ToString() ?? "";
                    data.ActAgr = props["act_AGR"]?.ToString() ?? "";
                    data.Other = props["other"]?.ToString() ?? "";
                    data.ImageBase64 = props["imageBase64"]?.ToString() ?? "";
                }

                var geometry = feature?["geometry"];
                var coordinates = geometry?["coordinates"]?.AsArray();
                if (coordinates != null && coordinates.Count >= 2)
                {
                    data.CoordX = coordinates[0]?.ToString() ?? "";
                    data.CoordY = coordinates[1]?.ToString() ?? "";
                }

                var glassesNode = feature?["Glasses"]?.AsArray();
                if (glassesNode != null && glassesNode.Count > 0)
                {
                    var glassObj = glassesNode[0]?.AsObject();
                    if (glassObj != null)
                    {
                        foreach (var glass in glassObj)
                        {
                            var material = new GlassMaterial { Name = glass.Key };
                            var glassData = glass.Value?.AsObject();
                            if (glassData != null)
                            {
                                var colorRgb = glassData["color_RGB"]?.AsObject();
                                if (colorRgb != null)
                                {
                                    int.TryParse(colorRgb["Red"]?.ToString(), out int r);
                                    int.TryParse(colorRgb["Green"]?.ToString(), out int g);
                                    int.TryParse(colorRgb["Blue"]?.ToString(), out int b);
                                    material.Red = r;
                                    material.Green = g;
                                    material.Blue = b;
                                }
                                material.Transparency = glassData["transparency"]?.ToString() ?? "";
                                material.Refraction = glassData["refraction"]?.ToString() ?? "";
                                material.Roughness = glassData["roughness"]?.ToString() ?? "";
                                material.Metallicity = glassData["metallicity"]?.ToString() ?? "";
                            }
                            data.Glasses.Add(material);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга: {ex.Message}");
            }

            return data;
        }

        public string GetFileName(string projectName, string modelName, bool needsModelSuffix)
        {
            bool isGround = modelName.Equals("Ground", StringComparison.OrdinalIgnoreCase);

            if (isGround)
            {
                return $"SM_{projectName}_Ground.geojson";
            }
            else if (needsModelSuffix)
            {
                return $"SM_{projectName}_{modelName}.geojson";
            }
            else
            {
                return $"SM_{projectName}.geojson";
            }
        }

        public void ExportToFile(string json, string filePath)
        {
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public string LoadFromFile(string filePath)
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Конвертирует изображение в base64 с ресайзом до 256x256
        /// </summary>
        public string ImageToBase64(string imagePath)
        {
            using (var original = Image.FromFile(imagePath))
            using (var resized = ResizeImage(original, 256, 256))
            using (var ms = new MemoryStream())
            {
                resized.Save(ms, ImageFormat.Jpeg);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Конвертирует изображение из буфера обмена в base64
        /// </summary>
        public string ImageToBase64(Image image)
        {
            using (var resized = ResizeImage(image, 256, 256))
            using (var ms = new MemoryStream())
            {
                resized.Save(ms, ImageFormat.Jpeg);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private string Escape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ").Replace("\r", "");
        }

        private string FormatDouble(double value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}