using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBioanalysis.TemplatesForCurve
{
    public class Position
    {
        //example
        //One instance is called P1
        //P1 has a dictionary with all its values associated
        //One dictionary entry is called <A> with a list of all points for A
        public string Name { get; set; }
        public Dictionary<string, List<Point>> Values { get; set; }

        public Position(string name, Dictionary<string, List<Point>> values)
        {
            Name = name;
            Values = values;
        }
    }
}
