using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    public class Project : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private DateTime _createdDate;
        private DateTime _modifiedDate;
        private ObservableCollection<ModelData> _models;
        private int _selectedModelIndex;
        private string _districtCode;  // НОВОЕ: код района

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(); }
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set { _modifiedDate = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ModelData> Models
        {
            get => _models;
            set { _models = value; OnPropertyChanged(); }
        }

        public int SelectedModelIndex
        {
            get => _selectedModelIndex;
            set { _selectedModelIndex = value; OnPropertyChanged(); }
        }

        // НОВОЕ: Код района (4 цифры)
        public string DistrictCode
        {
            get => _districtCode;
            set { _districtCode = value; OnPropertyChanged(); }
        }

        public Project()
        {
            Models = new ObservableCollection<ModelData>();
            Models.Add(new ModelData("001"));
            Models.Add(new ModelData("Ground"));
            SelectedModelIndex = 0;
            DistrictCode = "";
        }

        public void AddModel()
        {
            int maxNumber = 0;
            foreach (var model in Models)
            {
                string name = model.Name;
                if (name.Contains("_"))
                {
                    name = name.Split('_')[0];
                }
                if (int.TryParse(name, out int num))
                {
                    if (num > maxNumber) maxNumber = num;
                }
            }

            string newName = (maxNumber + 1).ToString("D3");

            int insertIndex = Models.Count;
            if (Models.Count > 0 && Models[Models.Count - 1].Name.ToLower() == "ground")
            {
                insertIndex = Models.Count - 1;
            }

            Models.Insert(insertIndex, new ModelData(newName));
        }

        public void DuplicateModel(int index)
        {
            if (index < 0 || index >= Models.Count) return;

            var original = Models[index];
            string newName = original.Name + "_copy";
            var clone = original.Clone(newName);
            Models.Insert(index + 1, clone);
        }

        public void RemoveModel(int index)
        {
            if (Models.Count > 1 && index >= 0 && index < Models.Count)
            {
                Models.RemoveAt(index);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}