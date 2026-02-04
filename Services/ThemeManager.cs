using System;
using System.Windows;

namespace AGR_Project_Manager.Services
{
    public static class ThemeManager
    {
        public enum Theme
        {
            Dark,
            Light
        }

        private static Theme _currentTheme = Theme.Dark;

        public static Theme CurrentTheme => _currentTheme;

        public static void ChangeTheme(Theme theme)
        {
            _currentTheme = theme;

            var themeName = theme switch
            {
                Theme.Light => "LightTheme",
                Theme.Dark => "DarkTheme",
                _ => "DarkTheme"
            };

            var uri = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);

            var app = Application.Current.Resources.MergedDictionaries;

            // Ищем и удаляем старую тему (первый словарь - это тема)
            if (app.Count > 0)
            {
                // Удаляем только словарь темы (первый)
                app.RemoveAt(0);
            }

            // Добавляем новую тему в начало
            var newTheme = new ResourceDictionary { Source = uri };
            app.Insert(0, newTheme);

            // Сохраняем выбор пользователя
            Properties.Settings.Default.Theme = theme.ToString();
            Properties.Settings.Default.Save();
        }

        public static void LoadSavedTheme()
        {
            try
            {
                var savedTheme = Properties.Settings.Default.Theme;
                if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<Theme>(savedTheme, out var theme))
                {
                    ChangeTheme(theme);
                }
                else
                {
                    // По умолчанию тёмная тема
                    ChangeTheme(Theme.Dark);
                }
            }
            catch
            {
                ChangeTheme(Theme.Dark);
            }
        }

        public static void ToggleTheme()
        {
            ChangeTheme(_currentTheme == Theme.Dark ? Theme.Light : Theme.Dark);
        }
    }
}