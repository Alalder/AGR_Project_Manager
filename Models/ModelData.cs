using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    public class ModelData : INotifyPropertyChanged
    {
        private string _name;
        private ObservableCollection<ObservableCollection<UdimTile>> _udimRows;

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

        public ModelData(string name)
        {
            Name = name;
            UdimRows = new ObservableCollection<ObservableCollection<UdimTile>>();

            // Начальный ряд 1001-1010
            AddRow();
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

            return clone;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}