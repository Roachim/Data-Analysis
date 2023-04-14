using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBioanalysis.TemplatesForCurve
{
    public class Point
    {
        //public string Name { get; set; }
        public double Value { get; set; }
        public double Date { get; set; }

        public Point(double val, double date) 
        {
            Value = val;
            Date = date;
        }
    }
}
