using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;

namespace DHI.Generic.NetCDF.MIKE.Commands
{
    public class ConvertDfs2ToNc: iCommand
    {
        private CommandSettings _settings = null;
        
        public void Execute(CommandSettings settings)
        {
            try
            {
                this._settings = settings;
                dfs2ToNc();
            }
            catch (Exception ex)
            {
                throw new Exception("Command error: " + ex.Message);
            }

        }

        public string CommandDescription()
        {
            return "Converts dfs2 file to a structured grid netcdf file";
        }

        public string CommandInputFileExtension()
        {
            return ".dfs2";
        }

        public string CommandOutputFileExtension()
        {
            return ".nc";
        }

        public void dfs2ToNc()
        {
            //strategy: 
            // 1) read the dfs2 file and dissect them into dimensions, variables and attributes with cf convention
            // 2) write the netcdf file

            DfsUtilities df_in = new DfsUtilities();
            int rc = df_in.ReadDfsFile(_settings.InputFileName); //read header info for input file
            if (rc != 0) throw new Exception(df_in.errMsg);

            //create netcdf file
            ucar.nc2.NetcdfFileWriteable newNetcdfFile = ucar.nc2.NetcdfFileWriteable.createNew(_settings.OutputFileName, false);

            //create dimensions (for dfs2 - time, x, and y)
            ucar.nc2.Dimension timeDim, xDim, yDim = null;
            _addDimensionVariables(df_in, newNetcdfFile, out timeDim, out xDim, out yDim);

            //adding item variables
            for (int itemCount = 0; itemCount < _settings.Variables.Count; itemCount++)
            {
                if (_settings.IsVariablesSelected[itemCount])
                {
                    _addItemVariable(df_in.Items[itemCount], newNetcdfFile, timeDim, xDim, yDim, df_in.delVal, itemCount);
                }
            }

            //global attributes
            _addGlobalAttributes(df_in, newNetcdfFile);

            //write data to ncFile
            _writeDimensionData(df_in, newNetcdfFile);
            _writeItemData(df_in, newNetcdfFile);

            //close the ncFile
            newNetcdfFile.close();
        }

        private void _writeItemData(DfsUtilities df_in, ucar.nc2.NetcdfFileWriteable newNetcdfFile)
        {
            for (int itemCount = 0; itemCount < _settings.Variables.Count; itemCount++)
            {
                if (_settings.IsVariablesSelected[itemCount])
                {
                    ucar.ma2.ArrayDouble dataArr = new ucar.ma2.ArrayDouble.D3(df_in.tAxis_nTSteps, df_in.Items[0].nPointsX, df_in.Items[0].nPointsY);
                    ucar.ma2.Index dataIndex = dataArr.getIndex();
                    for (int i = 0; i < df_in.tAxis_nTSteps; i++)
                    {
                        float[] dfsData = null;
                        df_in.ReadDynData(i, itemCount + 1, out dfsData);

                        int dataCount = 0;
                        for (int j = 0; j < df_in.Items[0].nPointsY; j++)
                            for (int k = 0; k < df_in.Items[0].nPointsX; k++)
                            {
                                dataArr.setDouble(dataIndex.set(i, k, j), dfsData[dataCount]);
                                dataCount++;
                            }
                    }
                    newNetcdfFile.write(df_in.Items[itemCount].Name.Replace(' ', '_'), dataArr);
                }
            }
        }

        private void _writeDimensionData(DfsUtilities df_in, ucar.nc2.NetcdfFileWriteable newNetcdfFile)
        {
            //time
            ucar.ma2.ArrayDouble timeDataArr = new ucar.ma2.ArrayDouble.D1(df_in.tAxis_nTSteps);

            int dtStep = (int)df_in.tAxis_dTStep;
            int multiples = 1;
            switch (dtStep) //todo: to add multiples of other time units. now only seconds is supported
            {
                case 31556926:
                    break;

                case 2629743:
                    break;

                case 604800:
                    break;

                case 86400:
                    break;

                case 3600:
                    break;

                case 60:
                    break;

                case 1:
                    break;

                default:
                    multiples = dtStep / 1;
                    break;
            }

            for (int i = 0; i < df_in.tAxis_nTSteps; i++)
            {
                double timeStepValue;
                if (df_in.tAxis_nTSteps == 1)
                {
                    timeStepValue = dtStep/dtStep*multiples;
                }
                else
                {
                    timeStepValue = (df_in.tAxis_dTStep / dtStep) * i*multiples ; //convert to hours or days or whatever
                }
                timeDataArr.setDouble(i, timeStepValue);
            }
            newNetcdfFile.write(_settings.TimeAxisName, timeDataArr);

            //lon
            ucar.ma2.ArrayDouble xDataArr = new ucar.ma2.ArrayDouble.D1(df_in.Items[0].nPointsX);
            for (int i = 0; i < df_in.Items[0].nPointsX; i++)
            {
                double xValue = df_in.Longitude + df_in.Items[0].DX * i;
                xDataArr.setDouble(i, xValue);
            }
            newNetcdfFile.write(_settings.XAxisName, xDataArr);

            //lat
            ucar.ma2.ArrayDouble yDataArr = new ucar.ma2.ArrayDouble.D1(df_in.Items[0].nPointsY);
            for (int i = 0; i < df_in.Items[0].nPointsY; i++)
            {
                double yValue = df_in.Latitude + df_in.Items[0].DY * i;
                yDataArr.setDouble(i, yValue);
            }
            newNetcdfFile.write(_settings.YAxisName, yDataArr);
        }

        private void _addGlobalAttributes(DfsUtilities df_in, ucar.nc2.NetcdfFileWriteable newNetcdfFile)
        {
            newNetcdfFile.addGlobalAttribute("title", System.IO.Path.GetFileName(_settings.OutputFileName));
            newNetcdfFile.addGlobalAttribute("projection", df_in.Projection);
            newNetcdfFile.addGlobalAttribute("history", "converted from " + this.GetType().Assembly.FullName
                + ", at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", by " + Environment.MachineName + ", with " + Environment.OSVersion);
            newNetcdfFile.addGlobalAttribute("source", System.IO.Path.GetFileName(_settings.InputFileName));
            //newNetcdfFile.addGlobalAttribute("conventions", "CF-1.0");
            newNetcdfFile.create();
        }

        private void _addItemVariable(DfsItemInfo dfsItem, ucar.nc2.NetcdfFileWriteable newNetcdfFile, ucar.nc2.Dimension timeDim, ucar.nc2.Dimension xDim, ucar.nc2.Dimension yDim, float delVal, int itemCount)
        {
            java.util.ArrayList varDims = new java.util.ArrayList();
            varDims.add(timeDim);
            varDims.add(xDim);
            varDims.add(yDim);

            newNetcdfFile.addVariable(dfsItem.Name.Replace(' ', '_'), ucar.ma2.DataType.FLOAT, varDims);
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "standard_name", dfsItem.Name.Replace(' ', '_'));
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "units", _settings.VariablesMappings[itemCount].CFStandardUnit);
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "long_name", _settings.VariablesMappings[itemCount].CFStandardName);
            if (!String.IsNullOrEmpty(_settings.VariablesMappings[itemCount].CFStandardDesc))
                newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "description", _settings.VariablesMappings[itemCount].CFStandardDesc);
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "missing_value", new java.lang.Float(delVal));
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "_FillValue", new java.lang.Float(delVal));
            newNetcdfFile.addVariableAttribute(dfsItem.Name.Replace(' ', '_'), "DHIUnitName", dfsItem.EUMUnitString);
        }

        private void _addDimensionVariables(DfsUtilities df_in, ucar.nc2.NetcdfFileWriteable newNetcdfFile, out ucar.nc2.Dimension timeDim, out ucar.nc2.Dimension xDim, out ucar.nc2.Dimension yDim)
        {
            timeDim = newNetcdfFile.addDimension(_settings.TimeAxisName, df_in.tAxis_nTSteps);
            xDim = newNetcdfFile.addDimension(_settings.XAxisName, df_in.Items[0].nPointsX);
            yDim = newNetcdfFile.addDimension(_settings.YAxisName, df_in.Items[0].nPointsY);

            java.util.ArrayList dims = new java.util.ArrayList();
            dims.add(timeDim);
            newNetcdfFile.addVariable(_settings.TimeAxisName, ucar.ma2.DataType.FLOAT, dims);

            string unit = "";
            int dtStep = (int)df_in.tAxis_dTStep;
            switch (dtStep)
            {
                case 31556926:
                    unit = "years since ";
                    break;

                case 2629743:
                    unit = "months since ";
                    break;

                case 604800:
                    unit = "weeks since ";
                    break;
            
                case 86400:
                    unit = "days since ";
                    break;
            
                case 3600:
                    unit = "hours since ";
                    break;
                
                case 60:
                    unit = "minutes since ";
                    break;
                
                case 1:
                    unit = "seconds since ";
                    break;

                default:
                    unit = "seconds since ";
                    break;
            }
            newNetcdfFile.addVariableAttribute(_settings.TimeAxisName, "units", unit + df_in.tAxis_StartDateStr + " " + df_in.tAxis_StartTimeStr);
            newNetcdfFile.addVariableAttribute(_settings.TimeAxisName, "long_name", "time");

            dims = new java.util.ArrayList();
            dims.add(xDim);
            newNetcdfFile.addVariable(_settings.XAxisName, ucar.ma2.DataType.FLOAT, dims);
            newNetcdfFile.addVariableAttribute(_settings.XAxisName, "units", "degrees east");//df_in.Items[0].axisEUMUnitString);
            newNetcdfFile.addVariableAttribute(_settings.XAxisName, "long_name", "longitude");
            newNetcdfFile.addVariableAttribute(_settings.XAxisName, "axis", "x");
            newNetcdfFile.addVariableAttribute(_settings.XAxisName, "DHIUnitName", df_in.Items[0].axisEUMUnitString);
            newNetcdfFile.addVariableAttribute(_settings.XAxisName, "projection", df_in.Projection);

            dims = new java.util.ArrayList();
            dims.add(yDim);
            newNetcdfFile.addVariable(_settings.YAxisName, ucar.ma2.DataType.FLOAT, dims);
            newNetcdfFile.addVariableAttribute(_settings.YAxisName, "units", "degrees north");//df_in.Items[0].axisEUMUnitString);
            newNetcdfFile.addVariableAttribute(_settings.YAxisName, "long_name", "latitude");
            newNetcdfFile.addVariableAttribute(_settings.YAxisName, "axis", "y");
            newNetcdfFile.addVariableAttribute(_settings.YAxisName, "DHIUnitName", df_in.Items[0].axisEUMUnitString);
            newNetcdfFile.addVariableAttribute(_settings.YAxisName, "projection", df_in.Projection);
        }

        public bool CanPlot()
        {
            return false;
        }
    }
}
