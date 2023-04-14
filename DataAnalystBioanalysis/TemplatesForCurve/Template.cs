using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBioanalysis.TemplatesForCurve
{
    public class Template
    {
        public string Name { get; set; }
        public Dictionary<string, Position> Positions { get; set; }
        public Template(string name, Dictionary<string, Position> positions) 
        {
            Name = name;
            Positions = positions;
        }
    }
}
