using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AGR_Project_Manager.Services
{
    public class TransliterationService
    {
        private static readonly Dictionary<char, string> TranslitMap = new Dictionary<char, string>
        {
            // Заглавные буквы
            {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"},
            {'Е', "E"}, {'Ё', "E"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"},
            {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
            {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"},
            {'У', "U"}, {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"},
            {'Ш', "Sh"}, {'Щ', "Shch"}, {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""},
            {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"},

            // Строчные буквы
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
            {'е', "e"}, {'ё', "e"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
            {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
            {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
            {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"},
            {'ш', "sh"}, {'щ', "shch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
            {'э', "e"}, {'ю', "yu"}, {'я', "ya"}
        };

        /// <summary>
        /// Транслитерация русского текста в английский с заменой спецсимволов
        /// </summary>
        /// <param name="input">Исходный текст</param>
        /// <returns>Транслитерированный текст</returns>
        public string Transliterate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder();

            foreach (char c in input)
            {
                if (TranslitMap.ContainsKey(c))
                {
                    // Русская буква - транслитерируем
                    sb.Append(TranslitMap[c]);
                }
                else if (IsEnglishLetterOrDigit(c))
                {
                    // Английская буква или цифра - оставляем как есть
                    sb.Append(c);
                }
                else
                {
                    // Любой другой символ (пробел, запятая, слеш и т.д.) - заменяем на подчеркивание
                    sb.Append('_');
                }
            }

            // Удаляем множественные подчеркивания и подчеркивания в начале/конце
            string result = sb.ToString();
            result = Regex.Replace(result, "_+", "_"); // __ -> _
            result = result.Trim('_'); // Убираем с концов

            return result;
        }

        /// <summary>
        /// Проверка, является ли символ английской буквой или цифрой
        /// </summary>
        private bool IsEnglishLetterOrDigit(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9');
        }

        /// <summary>
        /// Проверка, содержит ли строка русские буквы
        /// </summary>
        public bool ContainsRussian(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (TranslitMap.ContainsKey(c))
                    return true;
            }

            return false;
        }
    }
}