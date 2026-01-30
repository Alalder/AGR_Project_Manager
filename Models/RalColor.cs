using System;
using System.Collections.Generic;
using System.Text;

namespace AGR_Project_Manager.Models
{
    public class RalColor
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Hex { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public RalColor() { }

        public RalColor(string code, string name, string hex, byte r, byte g, byte b)
        {
            Code = code;
            Name = name;
            Hex = hex;
            R = r;
            G = g;
            B = b;
        }

        public override string ToString() => $"{Code} - {Name}";
    }
}
