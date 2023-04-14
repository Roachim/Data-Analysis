using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBioanalysis
{
    //An object that stores information from a bioanalysis format JSON
    public class AnalysisObject
    {
        public string TemplateKey { get; set; }
        public string Date { get; set; }
        public List<PositionValueBank> PValues { get; set; }

        public AnalysisObject(string templateKey, string date, List<PositionValueBank> pValues) 
        {
            TemplateKey = templateKey;
            Date = date;
            PValues = pValues;
        }

    }
}
