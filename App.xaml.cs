using System.Windows;
using AGR_Project_Manager.Services;

namespace AGR_Project_Manager
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Загружаем сохранённую тему при запуске
            ThemeManager.LoadSavedTheme();
        }
    }
}