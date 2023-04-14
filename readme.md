# Data Analyst Bioanalysis Assignment
A small console program made with visual studio .NET 6.

This program uses LINQ-to-JSON to parse the JSON files given.
This program uses Scottplot to generate plots from data


This small program has the purpose of:
 - Reading from Json
 - Generating a tabel with values from said Json file
 - Generating a line chart based on the same values.

 JSON > GRID > CHART


 ## Done
 * Method for deserializing JSON into proper values 
 * Method for finding the values from the file 
 * A class that holds the values extracted from JSON.
 * Save the data to an excel file
 * Generate plot from data



 ## Yet To Be Done
 - read from JSON ------
 * method for getting file as input
 * configure plot for dates
 * change plot to accomodate for outliers

 ## Remarks
 * Currently; if a JSON file has invalid JSON data, then it will be ignored completely.
 * If a JSON is missing data, then it will be ignored completely OR break the program. It must have:
	- A template Key.
	- A date.
	- At least one Position. P1, P2, etc.
	- At least one value; A or b or c, etc. for each position.
	- A weight for each positon.
* It is assumed that there is only 1 Template Key per JSON
* It is assumed that there is only 1 Date per Json
* The generated excel is to be saved by the user at the end of the program








 ```

 Use # in front of titles. 

The more # in a row: ##, ###, ####, the smaller the title text.

Title indicated by ## falls under the text indicated by #, and so forthe.

Use * to make dots, for lists as an example

Use 3 ` in a row at top and bottom of text to make into example, like this box of text.

put text in bold like this **in bold** outside of example text boxes.

Put in cursive like this *cursive* outside of example text boxes.

Put in a link by using [NameOfLink](actual link)


```