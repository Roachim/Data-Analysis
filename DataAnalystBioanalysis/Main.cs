using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using Excel = Microsoft.Office.Interop.Excel;   //A COM reference to handle the excel file
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using ScottPlot.Drawing.Colormaps;
using System.Drawing.Imaging;
using DataAnalystBioanalysis.TemplatesForCurve;

namespace DataAnalystBioanalysis
{
    public static class Main
    {
        private static string pngLocation = "..\\..\\..\\..\\"; //should save at databioanalysis folder. This gets the absolute path
        
        public static string testFilePath = "C:\\Users\\KOM\\Desktop\\Opgaver\\Data analyst\\Data\\2020\\318A-0010878_Assay_Replicate_1 2022-04-28T12_11_58Z.json";
        public static string testFolderPath = "C:\\Users\\KOM\\Desktop\\Opgaver\\Data analyst\\Data";
        
        public static List<AnalysisObject> analysisObjects = new List<AnalysisObject>();
        public static Dictionary<string,Template> templateObjects = new Dictionary<string, Template>();

        //private static Dictionary<string, List<double>> Motherload = new Dictionary<string, List<double>>();

        


        /// <summary>
        /// Primary method. Only one meant to be run outside from Main.
        /// </summary>
        public static void Run()
        {
            InstantiateMotherload();

            List<string> letters = new List<string>();
            letters.Add("A");
            letters.Add("B");
            letters.Add("C");
            letters.Add("D");
            letters.Add("weight");
            letters.Add("Weight");

            string[] files = GetJsonFromFolder(testFolderPath);       //use this and foreach below for full program   

            foreach (var filePath in files)
            {
                DocumentSingleJson(filePath);
            }
            foreach (string letter in letters)
            {
                CurveCycle(letter);
            }

            ExcelCreator();


            //DocumentSingleJson(testFilePath);       //for testing against a single JSON file. It's quicker when testing.
            //DisplayCurrentTemplates();    //for testing
        }

        /// <summary>
        /// Gets all JSON from folder and subfolders in the given path
        /// </summary>
        /// <param name="path">path to folder, stating from disk name: C:, D: etc.</param>
        /// <returns></returns>
        private static string[] GetJsonFromFolder(string path)
        {
            string[] files =
                Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            //foreach (string file in files) { Console.WriteLine(file); }
            return files;
        }


        /// <summary>
        /// runs through a single BioAnalysisJson and populates Object with relevant data
        /// </summary>
        /// <param name="JsonPath"></param>
        private static void DocumentSingleJson(string JsonPath)
        {
            
            //Create attributes for analysisObject
            string templateKey = "";
            string date = "";
            
            List<PositionValueBank> valueBankList = new List<PositionValueBank> {};

            //---------------------------

            string jsonString = File.ReadAllText(JsonPath);

            Console.WriteLine("------------Start of DocumentSingleJson-------------");
            JObject objects;
            try     // If the JSON cannot be parsed; Skip
            {
                objects = JObject.Parse(jsonString);
            }
            catch (Exception ex) { return; }
            


            var InnerJobject = objects["QuantitativeResponseAssay"];
            if (InnerJobject["AssayResults"] == null) { return; }


            templateKey = (string)InnerJobject["Meta"]["Template"]["Key"]; //Find the KEY, assumes there is only on per JSON

            if (!templateObjects.ContainsKey(templateKey))
            {
                Dictionary<string, Position> dic = new Dictionary<string, Position>();
                Template obj = new Template(templateKey,dic);
                templateObjects.Add(templateKey,obj);
            }

            date = (string)InnerJobject["Meta"]["Creation"]["Time"];    //Find the Date, assumes there is only on per JSON

            //manipulate date to remove time at end
            int index = date.IndexOf(@":");
            index -= 3;
            if (index >= 0)
                date = date.Substring(0, index);

            double cleanDate = CleanDate(date); //date to be used as a double, for a point that can be stored and used for the plots

            var AssayResults = InnerJobject["AssayResults"].Children().Children(); //twice for some reason


            foreach (var jtoken in AssayResults) //inside assayResults
            {
                //contain values here for each iteration
                Dictionary<string, double> values = new Dictionary<string, double>();
                PositionValueBank valueBank = new PositionValueBank("p", values, 0);
                //contain values here for each iteration

                if (jtoken["FullModel"] == null)
                {
                    continue;
                }

                try
                {
                    var catchEx = jtoken["FullModel"]["FitResult"].Children().Children();
                }
                catch (Exception ex) { return; }
                var FitResult = jtoken["FullModel"]["FitResult"].Children().Children();
                foreach (var parameter in FitResult)   //inside FitResult
                {
                    if (parameter.HasValues == false) { continue; }
                    if (parameter["AssayElementName"] == null) { continue; }
                    
                    if (parameter["AssayElementName"].ToString().Contains("Position"))
                    {
                        //Value == value
                        //parameterName == a, b, c etc.
                        //AssayElementName == position
                        //Console.WriteLine($" Found value {parameter["Value"]} in {parameter["ParameterName"]} in {parameter["AssayElementName"]}");
                        string positionName = (string)parameter["AssayElementName"];
                        string parameterName = (string)parameter["ParameterName"];
                        double parameterValue = (double)parameter["Value"];
                        //valuebank for xcel
                        valueBank.Name = (string)parameter["AssayElementName"];
                        valueBank.Values.Add((string)parameter["ParameterName"], (double)parameter["Value"]);

                        //position for curve
                        if (!templateObjects[templateKey].Positions.ContainsKey(positionName))  //add the specific position to the template
                        {
                            Dictionary<string, List<TemplatesForCurve.Point>> positions = new Dictionary<string, List<TemplatesForCurve.Point>>();
                            Position pos = new Position(positionName, positions);
                            templateObjects[templateKey].Positions.Add(positionName, pos);
                        }
                        if (!templateObjects[templateKey].Positions[positionName].Values.ContainsKey(parameterName))    //add a list of A, B, C or etc. to store values
                        {
                            List<TemplatesForCurve.Point> points = new List<TemplatesForCurve.Point>();
                            templateObjects[templateKey].Positions[positionName].Values.Add(parameterName, points);
                        }
                        
                        TemplatesForCurve.Point point = new TemplatesForCurve.Point(parameterValue, cleanDate);
                        templateObjects[templateKey].Positions[positionName].Values[parameterName].Add(point);
                    }
                }

                if(jtoken["StatisticTestResults"] == null) { continue; }
                var StatisticTestResults = jtoken["StatisticTestResults"].Children().Children();
                foreach (var parameter in StatisticTestResults) //finding the weight value
                {
                    if (parameter.HasValues == false) { continue; }
                    if (parameter["TestName"].ToString() != "TestMinimalWeight") { continue; }

                    if(parameter["InvolvedAssayElements"] == null) { continue;  }
                    if(parameter["InvolvedAssayElements"].HasValues == false) { continue; }
                    //Console.WriteLine(parameter["InvolvedAssayElements"]["Name"]+" is the name of the weight position");  //double check the position
                    //Console.WriteLine($"Weight is {parameter["Value"]}");  //get the weight value, not named weight for some reason
                    if(parameter["Value"] != null)
                    {
                        valueBank.Weight = (double)parameter["Value"];
                    }else { valueBank.Weight = 0; }

                    string positionName = (string)parameter["InvolvedAssayElements"]["Name"];   //positionName
                    string parameterName = "weight";
                    double parameterValue = 0;
                    try
                    {
                        parameterValue = (double)parameter["Value"];
                    }
                    catch (Exception ex) { return; }
                    //double parameterValue = (double)parameter["Value"];
                    //position for curve
                    if (!templateObjects[templateKey].Positions.ContainsKey(positionName))  //add the specific position to the template
                    {
                        Dictionary<string, List<TemplatesForCurve.Point>> positions = new Dictionary<string, List<TemplatesForCurve.Point>>();
                        Position pos = new Position(positionName, positions);
                        templateObjects[templateKey].Positions.Add(positionName, pos);
                    }
                    if (!templateObjects[templateKey].Positions[positionName].Values.ContainsKey(parameterName))    //add a list of A, B, C or etc. to store values
                    {
                        List<TemplatesForCurve.Point> points = new List<TemplatesForCurve.Point>();
                        templateObjects[templateKey].Positions[positionName].Values.Add(parameterName, points);
                    }

                    TemplatesForCurve.Point point = new TemplatesForCurve.Point(parameterValue, cleanDate);
                    templateObjects[templateKey].Positions[positionName].Values[parameterName].Add(point);

                }

                if(valueBank.Name != "p")
                {
                    valueBankList.Add(valueBank);
                }
                
            }


            AnalysisObject newObject = new AnalysisObject(templateKey, date, valueBankList);
            analysisObjects.Add(newObject);
            Console.WriteLine("JSON Done");
            DisplayAnalysisObjectData(newObject);
            Console.WriteLine("------------End of DocumentSingleJson-------------");
        }

        /// <summary>
        /// Help function for checking the insides of an AnalysisObject efter it has been instantiated 
        /// and attribute filled by 'DocumentSingleJson'
        /// </summary>
        /// <param name="obj">AnalysisObject with filled parameters</param>
        private static void DisplayAnalysisObjectData(AnalysisObject obj)
        {
            Console.WriteLine($"----------------------------------");
            Console.WriteLine($"Information extracted from a JSON");
            Console.WriteLine($"----------------------------------");
            Console.WriteLine($"TemplateKey = {obj.TemplateKey}");
            Console.WriteLine($"Date = {obj.Date}");
            foreach(var pValue in obj.PValues)
            {
                Console.WriteLine($"Position is {pValue.Name}");
                

                foreach(var str in pValue.Values)
                {
                    Console.WriteLine($"Letter {str.Key} with value {str.Value}");
                }

                Console.WriteLine($"Weight is {pValue.Weight}");
            }
        }
        
        /// <summary>
        /// help method for running through all the objects in templateObjects and all inner objects of those
        /// </summary>
        private static void DisplayCurrentTemplates()
        {
            foreach (var template in templateObjects.Values)
            {
                foreach (var position in template.Positions.Values)
                {
                    foreach (var points in position.Values)
                    {
                        Console.WriteLine(points.Key);
                        foreach (var point in points.Value)
                        {
                            Console.WriteLine(point.Value);
                            Console.WriteLine(point.Date);
                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create an excel sheet and input data from JSON
        /// </summary>
        private static void ExcelCreator()  //use the list of analysisObjects for this
        {
            if(analysisObjects.Count == 0)
            {
                return;
            }
            //https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/how-to-access-office-interop-objects
            Console.WriteLine($"x");

            var excelApp = new Excel.Application();
            // Make the object visible.
            excelApp.Visible = true;

            excelApp.Workbooks.Add();
            Excel._Worksheet workSheet = (Excel.Worksheet)excelApp.ActiveSheet;

            // Establish column headings.
            workSheet.Cells[1, "A"] = "Template Key";
            workSheet.Cells[1, "B"] = "Date";
            workSheet.Cells[1, "C"] = "Position";
            workSheet.Cells[1, "D"] = "A";
            workSheet.Cells[1, "E"] = "B";
            workSheet.Cells[1, "F"] = "C";
            workSheet.Cells[1, "G"] = "D";
            workSheet.Cells[1, "H"] = "Weight";


            var row = 1;
            foreach (var anObj in analysisObjects)
            {
                foreach (var PosVal in anObj.PValues)
                {
                    row++;
                    workSheet.Cells[row, "A"] = anObj.TemplateKey;
                    workSheet.Cells[row, "B"] = anObj.Date;
                    workSheet.Cells[row, "C"] = PosVal.Name;
                    foreach (var val in PosVal.Values)
                    {
                        if(val.Key == "A") { workSheet.Cells[row, "D"] = val.Value; }
                        if(val.Key == "B") { workSheet.Cells[row, "E"] = val.Value; }
                        if(val.Key == "C") { workSheet.Cells[row, "F"] = val.Value; }
                        if(val.Key == "D") { workSheet.Cells[row, "G"] = val.Value; }
                    }
                    workSheet.Cells[row, "H"] = PosVal.Weight;
                }
                
            }
            //use this to fit columns width to content
            workSheet.Columns[1].AutoFit();
            workSheet.Columns[2].AutoFit();
            workSheet.Columns[3].AutoFit();
        }

        /// <summary>
        /// runs through the template enumerator and gathers specific ones into list, 
        /// which is then given to makecurve for every position in every template
        /// </summary>
        /// <param name="letter">The name of the value. A, B, C etc. including "weight"</param>
        private static void CurveCycle(string letter)
        {
            //Make list
            List<double> xVal = new List<double>(); //this is dates
            List<double> yVal = new List<double>(); //this is values
            string templateName;
            string positionName;
            

            foreach (var template in templateObjects.Values)
            {
                templateName = template.Name;
                foreach (var position in template.Positions.Values)
                {
                    positionName = position.Name;
                    foreach (var points in position.Values)
                    {
                        if(points.Key == letter)
                        {
                            foreach (var point in points.Value)
                            {
                                xVal.Add(point.Date);
                                yVal.Add(point.Value);
                            }
                        }
                        
                    }
                    MakeCurve(xVal, yVal, templateName, positionName, letter);
                    xVal.Clear();
                    yVal.Clear();
                }
            }


        }

        /// <summary>
        /// Creates a plot png based the given lists.
        /// names the png using the template/position names and letters
        /// </summary>
        /// <param name="dates">a list of dates</param>
        /// <param name="values">a list of values for the dates</param>
        /// <param name="templateName">Name of template key</param>
        /// <param name="positionName">position name</param>
        /// <param name="letter">A, B, C, weight, etc.</param>
        private static void MakeCurve(List<double> dates, List<double> values, string templateName, string positionName, string letter)
        {
            //one possibilty vvvvvvv
            //https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.drawcurve?view=windowsdesktop-8.0
            //alternative vvvvvvvv
            //Scottplot nugetpackage

            //double[] dataX = new double[] { 1, 2, 3, 4, 5 };    //This is X - Horizontal - This is for the date
            //double[] dataY = new double[] { 1, 4, 9, 16, 25 };  //This is Y - Vertical - This is for the value
            //var plt = new ScottPlot.Plot(400, 300); //probably the height and width of the picture
            


            double[] dataX = new double[] { 5, 5, 5, 5, 5};    //This is X - Horizontal - This is for the date
            double[] dataY = new double[] { 5, 5, 5, 5, 5};  //This is Y - Vertical - This is for the value
            //List<double> tempDates
            //foreach (var date in dates)
            //{

            //}


            if(dates.Count > 0)
            {
                dataX = dates.ToArray();
            }
            if(values.Count > 0)
            {
                dataY = values.ToArray();
            }

            var plt = new ScottPlot.Plot(800, 600); //probably the height and width of the picture

            plt.AddScatter(dataX, dataY, lineWidth: 0);   //add data to plot // linewidth: 0 removes lines

            //plt.Margins(x: 0.5, y: 5); //add margin to better show data

            //experimental for using dates------------------
            //plt.XAxis.DateTimeFormat(true); //use dateTime format for the x-axis

            //// define tick spacing as 1 day (every day will be shown)
            //plt.XAxis.ManualTickSpacing(1, ScottPlot.Ticks.DateTimeUnit.Day);
            //plt.XAxis.TickLabelStyle(rotation: 45);

            //// add some extra space for rotated ticks
            //plt.XAxis.SetSizeLimit(min: 50);
            //experimental are end------------------------

            // customize the axis labels
            plt.Title($"Template key {templateName}: Position {positionName}:");
            plt.XLabel("Date");
            plt.YLabel($"Value of {letter}");


            plt.SaveFig(pngLocation + $"{templateName}_{positionName}_{letter}.png");  //save png

        }

        /// <summary>
        /// Loads the motherload with lists
        /// </summary>
        private static void InstantiateMotherload()
        {
            ////idea, string of dictionary be A, B so on and forth: problem, Key must be original
            //Dictionary<string, double> PInnerValues = new Dictionary<string, double>();   //values
            //Dictionary<string, double> PInnerDate = new Dictionary<string, double>();     //dates

            //List<double> Values = new List<double>();   //here insert A+value
            //List<double> Dates = new List<double>();    //here insert A+date

            //Motherload.Add("P0Val", Values);
            //Motherload.Add("P0Date", Dates);
            //Motherload.Add("P1Val", Values);
            //Motherload.Add("P1Date", Dates);
            //Motherload.Add("P2Val", Values);
            //Motherload.Add("P2Date", Dates);
            //Motherload.Add("P3Val", Values);
            //Motherload.Add("P3Date", Dates);
            //Motherload.Add("P4Val", Values);
            //Motherload.Add("P4Date", Dates);
        }

        private static void ObjectToList(string position, string letter, List<double> xList, List<double> yList)
        {
            foreach(AnalysisObject obj in analysisObjects) //list of analysis objects
            {

                foreach (var innObj in obj.PValues) //list of position values in a given analysis object
                {

                    foreach (var value in innObj.Values)    //The dictionary of values, a, b, c, etc. in a positionValue
                    {


                    }
                }
            }
        } //experimental

        private static void RetrievePositionValues(PositionValueBank innObj, int ListNumber)
        {
            if (innObj.Name.Contains(""+ListNumber+""))
            {
                foreach (var value in innObj.Values)    //The dictionary of values, a, b, c, etc. in a positionValue
                {

                }
            }
        }   //maybe delete

        /// <summary>
        /// This method makes a date into yearMonthDay instead and removes the slash. 
        /// This is so the date can be used as a double.
        /// </summary>
        /// <param name="date">day/month/year</param>
        /// <returns>yearMonthDay - as a double</returns>
        private static double CleanDate(string date)
        {
            string day = "";
            string month = "";
            string year = "";
            day = date.Substring(0, 2);
            month = date.Substring(3, 2);
            year = date.Substring(6, 4);
            date = year + month + day;
            double ret = Convert.ToDouble(date);

            return ret;
        }

        /// <summary>
        /// makes a datetime from a date formed from CleanDate
        /// </summary>
        /// <param name="cleanedDate"></param>
        /// <returns>DateTime</returns>
        private static DateTime ReMakeDate(double cleanedDate)
        {
            string stringDate = cleanedDate.ToString();
            int year = 0;
            int month=0;
            int day = 0;
            if (cleanedDate < 0)
            {
                // if (cleanedDate >= 10000) cleanedDate /= 10000; //better performance
                year = int.Parse(stringDate.Substring(0, 4));
                month = int.Parse(stringDate.Substring(4, 2));
                day = int.Parse(stringDate.Substring(6, 2));
            }
            return new DateTime(year, month, day);
        }
    }

}



//foreach (var parameter in FitResult)   //inside FitResult
//{
//    if (parameter.HasValues == false) { continue; }
//    if (parameter["AssayElementName"] == null) { continue; }

//    if (parameter["AssayElementName"].ToString().Contains("Position"))
//    {
//        //Console.WriteLine(parameter["Value"]);
//        //Console.WriteLine(parameter["AssayElementName"]);
//        Console.WriteLine($" Found value {parameter["Value"]} in {parameter["ParameterName"]} in {parameter["AssayElementName"]}");

//    }
//}

//var jo = JObject.Parse(jsonString);

//var data = (JObject)jo["response"]["user"]["data"];

//foreach (var item in data)
//{
//    Console.WriteLine("{0}: {1}", item.Key, item.Value);
//}


//JToken outer = JToken.Parse(jsonString); //this has no name
//JObject QuantitativeResponseAssay = outer["QuantitativeResponseAssay"].Value<JObject>();
//JObject Meta = QuantitativeResponseAssay["Meta"].Value<JObject>();
//JObject Template = Meta["Template"].Value<JObject>();
//string Key = (string)Template.SelectToken("Key"); //finding the key here

//JObject Creation = Meta["Creation"].Value<JObject>();
//string Time = (string)Creation.SelectToken("Time"); //finding time here

//////-------------------------------------------- FULLMODEL
////JObject FullModel = Meta["FullModel"].Value<JObject>();
////JObject FitResult = FullModel["FitResult"].Value<JObject>();
////JObject ParameterEstimate2 = FitResult["ParameterEstimate[2]"].Value<JObject>();
////string A = (string)FitResult.SelectToken("Value"); //finding Weight here
//////--------------------------------------------


////JObject StatisticTestResult3 = Meta["StatisticTestResult[3]"].Value<JObject>();
////string Weight = (string)StatisticTestResult3.SelectToken("Value"); //finding Weight here

////Console.WriteLine(Key);
////Console.WriteLine(Time);
////Console.WriteLine(Weight);
///
//List<JToken> tokens = jsonData.Children();

//var analysisObjects = JsonConvert.DeserializeObject<List<AnalysisObject>>(jsonString);

//Console.WriteLine(analysisObjects);

//var jsonData = JsonConvert.DeserializeObject<dynamic>(jsonString);


//JsonTextReader reader = new JsonTextReader(new StringReader(jsonString));
//while (reader.Read())
//{
//    if (reader.Value != null)
//    {

//        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);

//    }
//    else
//    {

//    }
//}

//position 0, A list
//position 0, B list
//position 1, A list
//Etc.
//A list of lists?
//a list that holds lists that holds lists<double>
//List<List<List<double>>> MotherList = new List<List<List<double>>>();
//List<List<double>> P0 = new List<List<double>>();
//List<List<double>> P1 = new List<List<double>>();
//List<List<double>> P2 = new List<List<double>>();
//List<List<double>> P3 = new List<List<double>>();
//List<List<double>> P4 = new List<List<double>>();

//List<double> A = new List<double>();
//List<double> B = new List<double>();
//List<double> C = new List<double>();
//List<double> D = new List<double>();
//List<double> Weight = new List<double>();

//P0.Add(A);

//foreach (AnalysisObject obj in analysisObjects) //list of analysis objects
//{

//    foreach (var innObj in obj.PValues) //list of position values in a given analysis object
//    {
//        for (int i = 0; i < 4; i++)
//        {
//            if (innObj.Name.Contains("0"))
//            {

//                foreach (var value in innObj.Values)    //The dictionary of values, a, b, c, etc. in a positionValue
//                {

//                }
//            }
//            RetrievePositionValues(innObj, i);
//        }
//    }
//}