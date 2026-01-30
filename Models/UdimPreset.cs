using System;

namespace AGR_Project_Manager.Models
{
    public class UdimPreset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string DiffusePath { get; set; }
        public string ErmPath { get; set; }
        public string NormalPath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public UdimPreset() { }

        public UdimPreset(string name, UdimTile tile)
        {
            Name = name;
            DiffusePath = tile.DiffusePath;
            ErmPath = tile.ErmPath;
            NormalPath = tile.NormalPath;
        }

        public void ApplyTo(UdimTile tile)
        {
            tile.DiffusePath = DiffusePath;
            tile.ErmPath = ErmPath;
            tile.NormalPath = NormalPath;
        }

        public override string ToString() => Name;
    }
}