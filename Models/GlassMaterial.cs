using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    public class GlassMaterial : INotifyPropertyChanged
    {
        private string _name;
        private int _red;
        private int _green;
        private int _blue;
        private string _transparency;
        private string _refraction;
        private string _roughness;
        private string _metallicity;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Red
        {
            get => _red;
            set { _red = value; OnPropertyChanged(); }
        }

        public int Green
        {
            get => _green;
            set { _green = value; OnPropertyChanged(); }
        }

        public int Blue
        {
            get => _blue;
            set { _blue = value; OnPropertyChanged(); }
        }

        public string Transparency
        {
            get => _transparency;
            set { _transparency = value; OnPropertyChanged(); }
        }

        public string Refraction
        {
            get => _refraction;
            set { _refraction = value; OnPropertyChanged(); }
        }

        public string Roughness
        {
            get => _roughness;
            set { _roughness = value; OnPropertyChanged(); }
        }

        public string Metallicity
        {
            get => _metallicity;
            set { _metallicity = value; OnPropertyChanged(); }
        }

        public GlassMaterial Clone()
        {
            return new GlassMaterial
            {
                Name = this.Name,
                Red = this.Red,
                Green = this.Green,
                Blue = this.Blue,
                Transparency = this.Transparency,
                Refraction = this.Refraction,
                Roughness = this.Roughness,
                Metallicity = this.Metallicity
            };
        }

        public override string ToString()
        {
            return $"{Name} | RGB({Red}, {Green}, {Blue})";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}