using System.Windows;
using System.Windows.Input;

namespace AGR_Project_Manager.Windows
{
    public partial class ProjectDialog : Window
    {
        public Models.Project Project { get; private set; }
        public bool IsEditMode { get; }

        // Конструктор для нового проекта
        public ProjectDialog()
        {
            InitializeComponent();
            Title = "Новый проект";
            Project = new Models.Project();
            IsEditMode = false;
        }

        // Конструктор для редактирования
        public ProjectDialog(Models.Project project)
        {
            InitializeComponent();
            Title = "Редактирование проекта";
            Project = project;
            IsEditMode = true;

            NameTextBox.Text = project.Name;
            DescriptionTextBox.Text = project.Description;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.Focus();
            if (IsEditMode)
            {
                NameTextBox.SelectAll();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter для сохранения (т.к. Enter нужен для описания)
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                TrySave();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            TrySave();
        }

        private void TrySave()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название проекта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            Project.Name = NameTextBox.Text.Trim();
            Project.Description = DescriptionTextBox.Text?.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}