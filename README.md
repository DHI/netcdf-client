# NetCDF_Client
The program converts structured grid Netcdf files with CF standard to DFS files and vice versa. The netcdf files written by this program follows the best practices of creating a netcdf file and also uses the cf standard table names. Automatic mapping between DHI EUM items and cf standard table names are done as much as possible but will require user to input if no matches are found.

The library also supports THREDDS data server as well as Opendab. NetCDF-4 format is also supported in the netcdf java library, however it is a read only function. Writing NetCDF files remains a NetCDF-3 format.

Other NetCDF files which are non-compliant with CF convention will not work.

This tool is developed for MIKE Powered by DHI 2017 and is not actively maintained. If you find this tool beneficial to your work and would like to contribute to the code, please contact me at czc@dhigroup.com

*Important - Please unzip the file "netcdfAll-4.5.exe.7z" in the 3rd folder before compiling. It is zipped as it exceeded the 25mb file limit from GitHub.*

=== 3rd party drivers:

Netcdf java library is used in conjunction with IKVM. All 3rd party drivers are included in the 3rd folder except for MIKE SDK. Please download MIKE SDK (free) from the website. (https://www.mikepoweredbydhi.com/download/mike-2017)

1) IKVM (http://www.ikvm.net/)
IKVM.NET is an implementation of Java for Mono and the Microsoft .NET Framework. It includes the following components:
•A Java Virtual Machine implemented in .NET
•A .NET implementation of the Java class libraries
•Tools that enable Java and .NET interoperability
2) Netcdf Java Library 4.5 (http://www.unidata.ucar.edu/downloads/netcdf/index.jsp)
3) MIKE SDK (https://www.mikepoweredbydhi.com/download/mike-2017)


