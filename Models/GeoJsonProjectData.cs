using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGR_Project_Manager.Models
{
    /// <summary>
    /// Общие данные GeoJSON, одинаковые для всех моделей проекта
    /// </summary>
    public class GeoJsonProjectData : INotifyPropertyChanged
    {
        private string _address;
        private string _okrug;
        private string _rajon;
        private string _name;
        private string _developer;
        private string _designer;
        private string _cadNum;
        
        private string _fnoName;
        private string _zuArea;
        private string _hRelief;
        private string _hOtn;
        private string _hAbs;
        private string _sObsh;
        private string _sNaz;
        private string _sPodz;
        private string _sppGns;
        private string _actAgr;
        private string _other;

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public string Okrug
        {
            get => _okrug;
            set { _okrug = value; OnPropertyChanged(); }
        }

        public string Rajon
        {
            get => _rajon;
            set { _rajon = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Developer
        {
            get => _developer;
            set { _developer = value; OnPropertyChanged(); }
        }

        public string Designer
        {
            get => _designer;
            set { _designer = value; OnPropertyChanged(); }
        }

        public string CadNum
        {
            get => _cadNum;
            set { _cadNum = value; OnPropertyChanged(); }
        }

        

        public string FnoName
        {
            get => _fnoName;
            set { _fnoName = value; OnPropertyChanged(); }
        }

        public string ZuArea
        {
            get => _zuArea;
            set { _zuArea = value; OnPropertyChanged(); }
        }

        public string HRelief
        {
            get => _hRelief;
            set { _hRelief = value; OnPropertyChanged(); }
        }

        public string HOtn
        {
            get => _hOtn;
            set { _hOtn = value; OnPropertyChanged(); }
        }

        public string HAbs
        {
            get => _hAbs;
            set { _hAbs = value; OnPropertyChanged(); }
        }

        public string SObsh
        {
            get => _sObsh;
            set { _sObsh = value; OnPropertyChanged(); }
        }

        public string SNaz
        {
            get => _sNaz;
            set { _sNaz = value; OnPropertyChanged(); }
        }

        public string SPodz
        {
            get => _sPodz;
            set { _sPodz = value; OnPropertyChanged(); }
        }

        public string SppGns
        {
            get => _sppGns;
            set { _sppGns = value; OnPropertyChanged(); }
        }

        public string ActAgr
        {
            get => _actAgr;
            set { _actAgr = value; OnPropertyChanged(); }
        }

        public string Other
        {
            get => _other;
            set { _other = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}