namespace AGR_Project_Manager.Models
{
    public class District
    {
        public string Number { get; set; }
        public string Name { get; set; }

        public District(string number, string name)
        {
            Number = number;
            Name = name;
        }

        public override string ToString() => $"{Number} - {Name}";
    }
}