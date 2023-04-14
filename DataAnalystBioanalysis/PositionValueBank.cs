using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBioanalysis
{
    public class PositionValueBank
    {
        public string Name { get;set; } //P0, P1, etc.
        public Dictionary<string, double> Values { get; set; }  //string = A, B, etc. double = value of that letter
        public double Weight { get; set; }
        public PositionValueBank() { }
        public PositionValueBank(string name, Dictionary<string, double> values, double weight)
        {
            Name = name;
            Values = values;
            Weight = weight;
        }
    }
}
