This is developed by CZC (czc@dhigroup.com) under the FIELD_AC project (JTS)
Date: 18th October 2011

Background:
The program converts structured grid Netcdf files with CF standard to DFS files and vice versa. The netcdf files written by this program follows the 
best practices of creating a netcdf file and also uses the cf standard table names. Automatic mapping between DHI EUM items and cf standard table names
are done as much as possible but will require user to input if no matches are found.

Netcdf java library is used instead of a C# wrapper for the C version of Netcdf.dll because all latest developments are updated in the Java library.
The library also supports THREDDS data server as well as Opendab. 
NetCDF-4 format is also supported in the netcdf java library, however it is a read only function. Writing NetCDF files remains a NetCDF-3 format.
Updating the netcdf java library is quick and easy using ikvm.

=== 3rd party drivers:
1) IKVM (http://www.ikvm.net/)
IKVM.NET is an implementation of Java for Mono and the Microsoft .NET Framework. It includes the following components:
•A Java Virtual Machine implemented in .NET
•A .NET implementation of the Java class libraries
•Tools that enable Java and .NET interoperability
2) Netcdf Java Library 4.5 (http://www.unidata.ucar.edu/downloads/netcdf/index.jsp)
3) MIKE Zero DFS drivers 

=== Steps to compile:
1) Download IKVM and install it
2) Upgrade the Java Library if needed (refer to ikvm website for tutorial)
3) Reference IKVM.OpenJDK.Core and netcdf driver which is converted from ikvm
4) Install MIKE Zero 2011 and onwards (assuming backward compatibility with 2011)
5) Install Steema.Teechart components (DHI has 2 licenses)
6) Compile

=== To run:
1) Windows Form - running the exe file without any arguments will bring up a windows form which quickly guides the user on how to run the program. It is
also the form to save the settings file. (Editing of the settings file will have to be done by notepad or text editting software)
2) Command line - running the exe file with -auto settingsfile.xml will run the stored command(s) and settings saved in the settings file
3) Command line - running the exe file with -autoprefix settingsfile.xml will run the stored command(s) and use the prefix settings for input and output files
4) Command line - running the exe file with -help will bring up a quick help
