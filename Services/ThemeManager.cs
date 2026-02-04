using System;
using System.Collections.Generic;
using System.Windows;

namespace AGR_Project_Manager.Services
{
    public static class ThemeManager
    {
        // Модель для темы
        public class ThemeInfo
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string FileName { get; set; }
        }

        // Список всех доступных тем
        public static List<ThemeInfo> AvailableThemes { get; } = new List<ThemeInfo>
        {
            new ThemeInfo { Id = "Dark", DisplayName = "🌙 Тёмная", FileName = "DarkTheme" },
            new ThemeInfo { Id = "DarkBlue", DisplayName = "🌊 Тёмно-синяя", FileName = "DarkBlueTheme" },
            new ThemeInfo { Id = "DarkPurple", DisplayName = "🔮 Фиолетовая", FileName = "DarkPurpleTheme" },
            new ThemeInfo { Id = "Light", DisplayName = "☀️ Светлая", FileName = "LightTheme" },
            new ThemeInfo { Id = "LightBlue", DisplayName = "🌤️ Светло-голубая", FileName = "LightBlueTheme" }
        };

        private static string _currentThemeId = "Dark";

        public static string CurrentThemeId => _currentThemeId;

        public static void ChangeTheme(string themeId)
        {
            var theme = AvailableThemes.Find(t => t.Id == themeId);
            if (theme == null)
            {
                theme = AvailableThemes[0]; // Fallback на первую тему
            }

            _currentThemeId = theme.Id;

            var uri = new Uri($"Themes/{theme.FileName}.xaml", UriKind.Relative);

            var app = Application.Current.Resources.MergedDictionaries;

            // Удаляем старую тему (первый словарь)
            if (app.Count > 0)
            {
                app.RemoveAt(0);
            }

            // Добавляем новую тему в начало
            var newTheme = new ResourceDictionary { Source = uri };
            app.Insert(0, newTheme);

            // Сохраняем выбор пользователя
            SaveThemePreference(themeId);
        }

        public static void ChangeTheme(ThemeInfo theme)
        {
            if (theme != null)
            {
                ChangeTheme(theme.Id);
            }
        }

        public static void LoadSavedTheme()
        {
            try
            {
                var savedThemeId = Properties.Settings.Default.Theme;
                if (!string.IsNullOrEmpty(savedThemeId))
                {
                    ChangeTheme(savedThemeId);
                }
                else
                {
                    ChangeTheme("Dark");
                }
            }
            catch
            {
                ChangeTheme("Dark");
            }
        }

        public static ThemeInfo GetCurrentTheme()
        {
            return AvailableThemes.Find(t => t.Id == _currentThemeId) ?? AvailableThemes[0];
        }

        private static void SaveThemePreference(string themeId)
        {
            try
            {
                Properties.Settings.Default.Theme = themeId;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // Игнорируем ошибки сохранения
            }
        }
    }
}