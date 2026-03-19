using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace AGR_Project_Manager.Services
{
    /// <summary>
    /// Сервис конвертации координат WGS84 → МСК-77
    /// </summary>
    public class CoordinateConversionService
    {
        private readonly CoordinateTransformationFactory _transformFactory;
        private readonly CoordinateSystemFactory _csFactory;
        private readonly ICoordinateTransformation _wgs84ToMsk77;

        // Параметры МСК-77 (Gauss-Kruger для Москвы)
        private const string Msk77Wkt = @"
            PROJCS[""MSK-77"",
                GEOGCS[""Pulkovo 1942"",
                    DATUM[""Pulkovo_1942"",
                        SPHEROID[""Krassowsky 1940"",6378245,298.3],
                        TOWGS84[23.57,-140.95,-79.8,0,0.35,0.79,-0.22]],
                    PRIMEM[""Greenwich"",0],
                    UNIT[""degree"",0.0174532925199433]],
                PROJECTION[""Transverse_Mercator""],
                PARAMETER[""latitude_of_origin"",55.66666666667],
                PARAMETER[""central_meridian"",37.5],
                PARAMETER[""scale_factor"",1],
                PARAMETER[""false_easting"",0],
                PARAMETER[""false_northing"",0],
                UNIT[""metre"",1]]";

        public CoordinateConversionService()
        {
            _csFactory = new CoordinateSystemFactory();
            _transformFactory = new CoordinateTransformationFactory();

            // WGS84
            var wgs84 = GeographicCoordinateSystem.WGS84;

            // МСК-77
            var msk77 = _csFactory.CreateFromWkt(Msk77Wkt);

            // Создаём трансформацию
            _wgs84ToMsk77 = _transformFactory.CreateFromCoordinateSystems(wgs84, msk77);
        }

        /// <summary>
        /// Конвертация WGS84 (широта, долгота) в МСК-77 (X, Y)
        /// </summary>
        /// <param name="latitude">Широта в градусах</param>
        /// <param name="longitude">Долгота в градусах</param>
        /// <param name="correctionX">Поправка X в метрах</param>
        /// <param name="correctionY">Поправка Y в метрах</param>
        /// <returns>Координаты МСК-77 (X - восток, Y - север)</returns>
        public (double X, double Y) ConvertToMsk77(
            double latitude, 
            double longitude, 
            double correctionX = -5.0, 
            double correctionY = -35.0)
        {
            // ProjNet принимает координаты в порядке [lon, lat]
            var source = new double[] { longitude, latitude };
            var result = _wgs84ToMsk77.MathTransform.Transform(source);

            // Применяем поправки
            double x = result[0] + correctionX;
            double y = result[1] + correctionY;

            return (x, y);
        }

        /// <summary>
        /// Парсинг координат из строки (поддерживает множество форматов)
        /// </summary>
        public static ParsedCoordinates ParseCoordinates(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();
            double? lat = null, lon = null;

            // Попытка 1: DMS формат с направлениями
            // 55°52'46.6"N 37°43'26.1"E
            var dmsPattern = new Regex(
                @"([NSEW]?)\s*(\d+)[°\s]+(\d+)['\s]+(\d+(?:[.,]\d+)?)[""″\s]*([NSEW]?)",
                RegexOptions.IgnoreCase);
            
            var dmsMatches = dmsPattern.Matches(input);
            
            if (dmsMatches.Count >= 2)
            {
                foreach (Match match in dmsMatches)
                {
                    string dir1 = match.Groups[1].Value.ToUpper();
                    string dir2 = match.Groups[5].Value.ToUpper();
                    string direction = !string.IsNullOrEmpty(dir1) ? dir1 : dir2;

                    double degrees = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    double minutes = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    double seconds = double.Parse(match.Groups[4].Value.Replace(',', '.'), CultureInfo.InvariantCulture);

                    double decimalDegrees = degrees + minutes / 60.0 + seconds / 3600.0;

                    if (direction == "S" || direction == "W")
                        decimalDegrees = -decimalDegrees;

                    if (direction == "N" || direction == "S")
                        lat = decimalDegrees;
                    else if (direction == "E" || direction == "W")
                        lon = decimalDegrees;
                }

                if (lat.HasValue && lon.HasValue)
                {
                    return new ParsedCoordinates { Latitude = lat.Value, Longitude = lon.Value };
                }
            }

            // Попытка 2: DMS без явных направлений
            // 55°52'46.6" 37°43'26.1"
            var dmsPattern2 = new Regex(@"(\d+)[°\s]+(\d+)['\s]+(\d+(?:[.,]\d+)?)[""″\s]*");
            var dmsMatches2 = dmsPattern2.Matches(input);

            if (dmsMatches2.Count >= 2)
            {
                var coords = new double[2];
                for (int i = 0; i < 2; i++)
                {
                    var m = dmsMatches2[i];
                    double degrees = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    double minutes = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    double seconds = double.Parse(m.Groups[3].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    coords[i] = degrees + minutes / 60.0 + seconds / 3600.0;
                }
                return new ParsedCoordinates { Latitude = coords[0], Longitude = coords[1] };
            }

            // Попытка 3: Формат с пробелами
            // 55 52 46.6 N 37 43 26.1 E
            var spacePattern = new Regex(
                @"(\d+)\s+(\d+)\s+(\d+(?:[.,]\d+)?)\s*([NSEW])",
                RegexOptions.IgnoreCase);
            
            var spaceMatches = spacePattern.Matches(input);

            if (spaceMatches.Count >= 2)
            {
                foreach (Match match in spaceMatches)
                {
                    double degrees = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double minutes = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    double seconds = double.Parse(match.Groups[3].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    string direction = match.Groups[4].Value.ToUpper();

                    double decimalDegrees = degrees + minutes / 60.0 + seconds / 3600.0;

                    if (direction == "S" || direction == "W")
                        decimalDegrees = -decimalDegrees;

                    if (direction == "N" || direction == "S")
                        lat = decimalDegrees;
                    else if (direction == "E" || direction == "W")
                        lon = decimalDegrees;
                }

                if (lat.HasValue && lon.HasValue)
                {
                    return new ParsedCoordinates { Latitude = lat.Value, Longitude = lon.Value };
                }
            }

            // Попытка 4: Десятичный формат
            // 55.879056, 37.724472 или 55.879056 37.724472
            var decimalPattern = new Regex(@"(-?\d+[.,]\d+)");
            var decimalMatches = decimalPattern.Matches(input);

            if (decimalMatches.Count >= 2)
            {
                lat = double.Parse(decimalMatches[0].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                lon = double.Parse(decimalMatches[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                return new ParsedCoordinates { Latitude = lat.Value, Longitude = lon.Value };
            }

            // Попытка 5: Целые числа (градусы)
            var intPattern = new Regex(@"(-?\d+)");
            var intMatches = intPattern.Matches(input);

            if (intMatches.Count >= 2)
            {
                lat = double.Parse(intMatches[0].Value, CultureInfo.InvariantCulture);
                lon = double.Parse(intMatches[1].Value, CultureInfo.InvariantCulture);
                return new ParsedCoordinates { Latitude = lat.Value, Longitude = lon.Value };
            }

            return null;
        }

        /// <summary>
        /// Проверка валидности координат
        /// </summary>
        public static bool ValidateCoordinates(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        /// <summary>
        /// Форматирование числа с выбранным разделителем
        /// </summary>
        public static string FormatNumber(double value, int decimals, bool useComma)
        {
            string formatted = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
            return useComma ? formatted.Replace('.', ',') : formatted;
        }
    }

    /// <summary>
    /// Распознанные координаты
    /// </summary>
    public class ParsedCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}