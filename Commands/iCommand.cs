using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.nc2;
using ucar.nc2.dataset;
using ucar.nc2.ncml;
using ucar.util;

namespace DHI.Generic.NetCDF.MIKE.Commands
{
    public interface iCommand
    {
        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="argument"></param>
        void Execute(CommandSettings settings);

        /// <summary>
        /// Returns the command description
        /// </summary>
        string CommandDescription();

        /// <summary>
        /// Returns the command input file extension
        /// </summary>
        string CommandInputFileExtension();

        /// <summary>
        /// Returns the command output file extension
        /// </summary>
        string CommandOutputFileExtension();

        /// <summary>
        /// Returns the boolean if the input data can be plotted with the program
        /// </summary>
        /// <returns></returns>
        bool CanPlot();

    }
}
