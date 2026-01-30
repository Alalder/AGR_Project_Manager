using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    public class UdimTile : INotifyPropertyChanged
    {
        private string _name;
        private string _diffusePath;
        private string _ermPath;
        private string _normalPath;

        public int UdimNumber { get; set; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? "—" : Name;

        public string DiffusePath
        {
            get => _diffusePath;
            set { _diffusePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDiffuse)); }
        }

        public string ErmPath
        {
            get => _ermPath;
            set { _ermPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasErm)); }
        }

        public string NormalPath
        {
            get => _normalPath;
            set { _normalPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNormal)); }
        }

        public bool HasDiffuse => !string.IsNullOrEmpty(DiffusePath);
        public bool HasErm => !string.IsNullOrEmpty(ErmPath);
        public bool HasNormal => !string.IsNullOrEmpty(NormalPath);
        public bool HasAnyTexture => HasDiffuse || HasErm || HasNormal;

        public UdimTile(int udimNumber)
        {
            UdimNumber = udimNumber;
            Name = "";
        }

        public UdimTile Clone()
        {
            return new UdimTile(UdimNumber)
            {
                Name = this.Name,
                DiffusePath = this.DiffusePath,
                ErmPath = this.ErmPath,
                NormalPath = this.NormalPath
            };
        }

        public void CopyFrom(UdimTile source)
        {
            if (source == null) return;
            Name = source.Name;
            DiffusePath = source.DiffusePath;
            ErmPath = source.ErmPath;
            NormalPath = source.NormalPath;
        }

        public void Clear()
        {
            Name = "";
            DiffusePath = null;
            ErmPath = null;
            NormalPath = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}