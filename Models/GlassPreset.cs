using System;

namespace AGR_Project_Manager.Models
{
    public class GlassPreset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public string Transparency { get; set; }
        public string Refraction { get; set; }
        public string Roughness { get; set; }
        public string Metallicity { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public GlassPreset() { }

        public GlassPreset(string name, GlassMaterial material)
        {
            Name = name;
            Red = material.Red;
            Green = material.Green;
            Blue = material.Blue;
            Transparency = material.Transparency;
            Refraction = material.Refraction;
            Roughness = material.Roughness;
            Metallicity = material.Metallicity;
        }

        public void ApplyTo(GlassMaterial material)
        {
            material.Red = Red;
            material.Green = Green;
            material.Blue = Blue;
            material.Transparency = Transparency;
            material.Refraction = Refraction;
            material.Roughness = Roughness;
            material.Metallicity = Metallicity;
        }

        public GlassMaterial ToMaterial(string materialName)
        {
            return new GlassMaterial
            {
                Name = materialName,
                Red = Red,
                Green = Green,
                Blue = Blue,
                Transparency = Transparency,
                Refraction = Refraction,
                Roughness = Roughness,
                Metallicity = Metallicity
            };
        }

        public override string ToString() => Name;
    }
}