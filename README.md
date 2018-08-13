# fgdb-crawler

This is the form application version of a console application that explores ArcGIS file geodatabases and generates a csv file containing information about files contained in the File GeoDatabase. The console application can be found here:

https://github.com/scMarth/developer-support/tree/master/arcobjects-net/explore-fgdbs-and-generate-csv

# Usage

<p align="center">
  <img src="https://raw.githubusercontent.com/scMarth/fgdb-crawler/master/screenshots/fgdb-ss.png">
</p>

Launch the application and click "Open Directory". You may either select (or type in) a path to a folder that contains File GeoDatabases (.gdb folders), or a folder that contains File GeoDatabases. The program automatically detects which one it is.

Next, click Export to CSV, and select where you wish to save the output CSV file.

# About the .csv output:

The output CSV file contains information such as:
* The paths of the .gdb folders found
* What is contained in the .gdb folder (and their subsets/children)
* What types are they (File Geodatabase Feature Class, File Geodatabase Topology etc.), and where they are located in the .gdb folder
* If possible to retrieve, what are the creation times, last accessed times, dates modified, and file sizes
