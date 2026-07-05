using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    public class ModelData : INotifyPropertyChanged
    {
        private string _name;
        private ObservableCollection<ObservableCollection<UdimTile>> _udimRows;
        private string _coordX;
        private string _coordY;
        private string _base64Image;
        private ObservableCollection<GlassMaterial> _glasses;
        private string _fnoName;
        private string _fnoCode;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        // Двумерная структура: ряды -> тайлы в ряду
        public ObservableCollection<ObservableCollection<UdimTile>> UdimRows
        {
            get => _udimRows;
            set { _udimRows = value; OnPropertyChanged(); }
        }

        public string CoordX
        {
            get => _coordX;
            set { _coordX = value; OnPropertyChanged(); }
        }

        public string CoordY
        {
            get => _coordY;
            set { _coordY = value; OnPropertyChanged(); }
        }

        // Base64 изображение модели
        public string Base64Image
        {
            get => _base64Image;
            set { _base64Image = value; OnPropertyChanged(); }
        }

        // Стёкла модели
        public ObservableCollection<GlassMaterial> Glasses
        {
            get => _glasses;
            set { _glasses = value; OnPropertyChanged(); }
        }

        // FNO_name для каждой модели
        public string FnoName
        {
            get => _fnoName;
            set { _fnoName = value; OnPropertyChanged(); }
        }

        // FNO_code для каждой модели
        public string FnoCode
        {
            get => _fnoCode;
            set { _fnoCode = value; OnPropertyChanged(); }
        }

        public ModelData(string name)
        {
            Name = name;
            UdimRows = new ObservableCollection<ObservableCollection<UdimTile>>();

            // Начальный ряд 1001-1010
            AddRow();
            CoordX = "";
            CoordY = "";
            Base64Image = null;
            Glasses = new ObservableCollection<GlassMaterial>();
            FnoName = "";
            FnoCode = "";
        }

        public void AddRow()
        {
            int rowIndex = UdimRows.Count;
            var newRow = new ObservableCollection<UdimTile>();

            // UDIM нумерация: первый ряд 1001-1010, второй 1011-1020, и т.д.
            int startUdim = 1001 + (rowIndex * 10);

            for (int i = 0; i < 10; i++)
            {
                newRow.Add(new UdimTile(startUdim + i));
            }

            UdimRows.Insert(0, newRow); // Добавляем сверху
        }

        public void RemoveTopRow()
        {
            if (UdimRows.Count > 1) // Оставляем минимум 1 ряд
            {
                UdimRows.RemoveAt(0);
            }
        }

        public ModelData Clone(string newName)
        {
            var clone = new ModelData(newName);
            clone.UdimRows.Clear();

            foreach (var row in UdimRows)
            {
                var newRow = new ObservableCollection<UdimTile>();
                foreach (var tile in row)
                {
                    newRow.Add(tile.Clone());
                }
                clone.UdimRows.Add(newRow);
            }

            // Копируем данные GeoJSON
            clone.CoordX = this.CoordX;
            clone.CoordY = this.CoordY;
            clone.Base64Image = this.Base64Image;
            clone.FnoName = this.FnoName;
            clone.FnoCode = this.FnoCode;

            // Копируем стёкла
            clone.Glasses.Clear();
            foreach (var glass in this.Glasses)
            {
                clone.Glasses.Add(new GlassMaterial
                {
                    Name = glass.Name,
                    Red = glass.Red,
                    Green = glass.Green,
                    Blue = glass.Blue,
                    Transparency = glass.Transparency,
                    Refraction = glass.Refraction,
                    Roughness = glass.Roughness,
                    Metallicity = glass.Metallicity
                });
            }

            return clone;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}