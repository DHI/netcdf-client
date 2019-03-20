using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;

namespace DHI.Generic.NetCDF.MIKE.Commands
{
    public class ConvertNcToDfs2Trans : iCommand
    {
        private CommandSettings _settings = null;
        private NetCdfUtilities _util = null;
        private CFMapping _cfMap = null;
        private float _fdel = -1e-30f;
        private double _floatArraySize;
        private double _deleteValTolerancePercent = 90;

        public string CommandDescription()
        {
            return "Converts a structured grid netcdf file (CF standard) to a dfs2 file (as transects)";
        }

        public string CommandInputFileExtension()
        {
            return ".nc";
        }

        public string CommandOutputFileExtension()
        {
            return ".dfs2";
        }

        public void Execute(CommandSettings settings)
        {
            try
            {
            this._settings = settings;
            this._util = new NetCdfUtilities(settings.InputFileName, settings.UseDataSet);
            this._cfMap = new CFMapping();

            _checkMike();
            _convert2Dfs2Trans();
            }
            catch (Exception ex)
            {
                throw new Exception("Command error: " + ex.Message);
            }
        }

        private void _convert2Dfs2Trans()
        {
            IntPtr headerPointer = new IntPtr();
            IntPtr filePointer = new IntPtr();

            try
            {
                int maxTimeStep = _getTimeSteps();
                
                // Create header
                System.Reflection.AssemblyName assName = this.GetType().Assembly.GetName();
                //if (maxTimeStep <= 1)
                    headerPointer = DfsDLLWrapper.dfsHeaderCreate(FileType.EqtimeFixedspaceAllitems,
                    System.IO.Path.GetFileNameWithoutExtension(_settings.InputFileName), assName.Name,
                        assName.Version.Major, _getItemNum(), StatType.NoStat);
                /*else
                headerPointer = DfsDLLWrapper.dfsHeaderCreate(FileType.NeqtimeFixedspaceAllitems,
                    System.IO.Path.GetFileNameWithoutExtension(_settings.InputFileName), assName.Name,
                        assName.Version.Major, _getItemNum(), StatType.NoStat);*/

                // Setup header
                DfsDLLWrapper.dfsSetDataType(headerPointer, 1);
                DfsDLLWrapper.dfsSetGeoInfoUTMProj(headerPointer, _settings.MZMapProjectionString, 0, 0, 0);
                DfsDLLWrapper.dfsSetDeleteValFloat(headerPointer, _fdel);
                List<DateTime> dateTimes = _util.GetTime(_settings.TimeAxisName);

                //compute timesteps
                double timestepSec = 0;
                for (int timeSteps = 1; timeSteps < dateTimes.Count; timeSteps++)
                {
                    timestepSec = Math.Round((dateTimes[timeSteps].ToOADate() - dateTimes[timeSteps - 1].ToOADate()) * 86400);
                }

                if (maxTimeStep <= 1)
                    DfsDLLWrapper.dfsSetEqCalendarAxis(headerPointer, dateTimes[0].ToString("yyyy-MM-dd"), dateTimes[0].ToString("HH:mm:ss"), (int)eumUnit.eumUsec, 0, _settings.TimeStepSeconds, 0);
                else
                    DfsDLLWrapper.dfsSetEqCalendarAxis(headerPointer, dateTimes[0].ToString("yyyy-MM-dd"), dateTimes[0].ToString("HH:mm:ss"), (int)eumUnit.eumUsec, 0, (int)timestepSec, 0);

                // Add Items by looping through selected variables
                int selectedItemCount = 0;
                for (int itemCount = 0; itemCount < _settings.Variables.Count; itemCount++)
                {
                    if (_settings.IsVariablesSelected[itemCount])
                    {
                        IntPtr itemPointer = DfsDLLWrapper.dfsItemD(headerPointer, selectedItemCount + 1);
                        string itemName = _settings.Variables[itemCount];

                        DfsDLLWrapper.dfsSetItemInfo(headerPointer, itemPointer, _settings.VariablesMappings[itemCount].EUMItemKey, _settings.VariablesMappings[itemCount].EUMItemDesc,
                                                     _settings.VariablesMappings[itemCount].EUMMappedItemUnitKey, DfsSimpleType.Float);
                        DfsDLLWrapper.dfsSetItemValueType(itemPointer, DataValueType.Instantaneous);

                        //get grid data from nc dimensions
                        double x0 = 0, y0 = 0, dx = 0, dy = 0, j = 0, k = 0;
                        List<float[]> floatDataList = _getFloatData(itemName, 0);
                        if (floatDataList.Count == 0) throw new Exception("No data found for specified transect points. Extend ranges and try again.");
                        _getGridOrigo(out x0, out y0, out dx, out dy, out j, out k, floatDataList);
                        DfsDLLWrapper.dfsSetItemAxisEqD2(itemPointer, (int)eumUnit.eumUmeter, (int)j, (int)k, (float)x0, (float)y0, (float)dx, (float)dy);

                        selectedItemCount++;
                    }
                }

                // Create file
                DfsDLLWrapper.dfsFileCreate(_settings.OutputFileName, headerPointer, out filePointer);

                //write data to file (time loop > item loop)
                for (int timeSteps = 0; timeSteps < dateTimes.Count; timeSteps++)
                {
                    selectedItemCount = 0;
                    for (int itemCount = 0; itemCount < _settings.Variables.Count; itemCount++)
                    {
                        if (_settings.IsVariablesSelected[itemCount])
                        {
                            string itemName = _settings.Variables[itemCount];
                            double dTotalSeconds = (dateTimes[timeSteps].ToOADate() - dateTimes[0].ToOADate()) * 86400;
                            dTotalSeconds = Math.Round(dTotalSeconds, 0, MidpointRounding.AwayFromZero);
                            DfsDLLWrapper.dfsWriteItemTimeStep(headerPointer, filePointer, dTotalSeconds, _processFloatData(_getFloatData(itemName, timeSteps)));
                            selectedItemCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Convert2Dfs2Trans Error: " + ex.Message);
            }
            finally
            {
                // close file and destroy header
                if (null != filePointer)
                    DfsDLLWrapper.dfsFileClose(headerPointer, ref filePointer);

                if (null != headerPointer)
                    DfsDLLWrapper.dfsHeaderDestroy(ref headerPointer);
            }
        }

        private void _getGridOrigo(out double x0, out double y0, out double dx, out double dy, out double j, out double k, List<float[]> dataList)
        {
            x0 = 0; y0 = 0; dx = 0; dy = 0; j = 0; k = 0;
            object[] ncDims = _util.GetDimensions();
            List<double> interpolatedYVals = new List<double>();
            List<double> originalYVals = new List<double>();
            ucar.nc2.Variable depthVar = null;
            foreach (object ncDim in ncDims)
            {
                string dimName = ((ucar.nc2.Dimension)ncDim).getName();
                if (_settings.XAxisName == dimName) //number of points is x axis
                {
                    ucar.nc2.Variable ncVar = (ucar.nc2.Variable)_util.GetVariable(dimName);
                    List<double> interpolatedXVals = new List<double>();
                    List<double> originalXVals = new List<double>();
                    _getMinAndInterval(ncVar, out dx, out x0, out interpolatedXVals, out originalXVals);
                    //x0 = _settings.TransectPoints[0].X;
                    x0 = 0;
                    j = dataList.Count;
                }
                else if (_settings.ZAxisName == dimName) //depth is y axis
                {
                    ucar.nc2.Variable ncVar = (ucar.nc2.Variable)_util.GetVariable(dimName);
                    depthVar = ncVar;

                    _getMinAndInterval(ncVar, out dy, out y0, out interpolatedYVals, out originalYVals);
                    if (interpolatedYVals.Count != 0)
                    {
                        k = interpolatedYVals.Count;
                        y0 = interpolatedYVals[0];
                    }
                    else
                    {
                        k = originalYVals.Count;
                        y0 = originalYVals[0];
                    }
                }
            }

            //calculate the size of the possible float array and limit it to _settings.MaxBlockSizeMB
            _floatArraySize = j * k * 4 / 1024 / 1024; //in mb
            if (_floatArraySize > _settings.MaxBlockSizeMB)
            {
                _settings.DZ = (float)(originalYVals.Max() / (_settings.MaxBlockSizeMB / j / k / 4 * 1024 * 1024));
                _getMinAndInterval(depthVar, null, out dy, out y0, out interpolatedYVals, out originalYVals, true);
                y0 = 0;
            }
        }

        private void _getMinAndInterval(ucar.nc2.Variable ncDim, out double interval, out double minVal, out List<double> interpolatedVal, out List<double> originalVal)
        {
            ucar.ma2.Array dataArr = _util.GetAllVariableData(ncDim);

            minVal = double.MaxValue;
            double maxVal = double.MinValue;

            double prevVal = 0.0;
            List<double> intervals = new List<double>();
            interpolatedVal = new List<double>();
            originalVal = new List<double>();
            interval = 0.0;

            for (int dCount = 0; dCount < dataArr.getSize(); dCount++)
            {
                ucar.ma2.Index dIndex = dataArr.getIndex();
                double data = dataArr.getDouble(dIndex.set(dCount));
                originalVal.Add(data);
                if (minVal >= data) minVal = data;
                if (maxVal <= data) maxVal = data;
                if (dCount > 0)
                {
                    prevVal = dataArr.getDouble(dIndex.set(dCount - 1));
                    interval = data - prevVal;
                    intervals.Add(interval);
                }
            }

            double dz;
            if (_settings.DZ > 0)
            {
                dz = _settings.DZ;
                if (intervals.Average() != interval)
                {
                    //generate a list of interpolatedVal
                    for (double min = minVal; min <= maxVal; min += dz)
                    {
                        interpolatedVal.Add(min);
                    }
                    interval = dz;
                }
            }
            else
            {
                if (intervals.Average() != interval)
                {
                    //generate a list of interpolatedVal
                    for (double min = minVal; min <= maxVal; min += intervals.Min())
                    {
                        interpolatedVal.Add(min);
                    }
                    interval = intervals.Min();
                }
            }

        }

        private void _getMinAndInterval(ucar.nc2.Variable ncDim, string minMaxStr, out double interval, out double minVal, out List<double> interpolatedVal, out List<double> originalVal, bool useDefinedDZ)
        {

            ucar.ma2.Array dataArr = _util.GetAllVariableData(ncDim);
            int minCount = 0, maxCount = (int)dataArr.getSize() - 1;

            minVal = double.MaxValue;
            double maxVal = double.MinValue;

            double prevVal = 0.0;
            List<double> intervals = new List<double>();
            interpolatedVal = new List<double>();
            originalVal = new List<double>();
            interval = 0.0;

            if (!string.IsNullOrEmpty(minMaxStr))
            {
                minCount = Convert.ToInt32(minMaxStr.Split(':')[0]);
                maxCount = Convert.ToInt32(minMaxStr.Split(':')[1]);
            }

            java.util.List ncVarAtt = ncDim.getAttributes();
            bool stepExists = false;
            for (int attCount = 0; attCount < ncVarAtt.size(); attCount++)
            {
                ucar.nc2.Attribute varAtt = (ucar.nc2.Attribute)ncVarAtt.get(attCount);
                string attName = varAtt.getName();
                if (attName == "step")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    interval = attVal.doubleValue();
                    stepExists = true;
                }
            }

            for (int dCount = minCount; dCount <= maxCount; dCount++)
            {
                ucar.ma2.Index dIndex = dataArr.getIndex();
                double data = dataArr.getDouble(dIndex.set(dCount));
                originalVal.Add(data);
                if (minVal >= data) minVal = data;
                if (maxVal <= data) maxVal = data;

                if (!stepExists)
                {
                    if (dCount > 0)
                    {
                        prevVal = dataArr.getDouble(dIndex.set(dCount - 1));
                        interval = data - prevVal;
                        intervals.Add(interval);
                    }
                }
            }

            if (!stepExists)
            {
                if (intervals.Average() != interval)
                {
                    if (useDefinedDZ)
                    {
                        for (double min = minVal; min <= maxVal; min += Convert.ToDouble(_settings.DZ))
                        {
                            interpolatedVal.Add(min);
                        }
                        interval = Convert.ToDouble(_settings.DZ);
                    }
                    else
                    {
                        //generate a list of interpolatedVal
                        for (double min = 0; min <= maxVal; min += intervals.Min())
                        {
                            interpolatedVal.Add(min);
                        }
                        interval = intervals.Min();
                    }
                }
            }

        }

        private int _getTimeSteps()
        {
            object[] ncDims = _util.GetDimensions();
            int timestepsCount = 0;
            foreach (object ncDim in ncDims)
            {
                if (((ucar.nc2.Dimension)ncDim).getName() == _settings.TimeAxisName)
                    timestepsCount = ((ucar.nc2.Dimension)ncDim).getLength();
            }
            return timestepsCount;
        }

        private int _getItemNum()
        {
            int itemNum = 0;
            foreach (bool isSelected in _settings.IsVariablesSelected)
            {
                if (isSelected) itemNum++;
            }
            return itemNum;
        }

        private List<float[]> _getFloatData(string itemName, int timeStep)
        {
            float[] itemFloatData = null;
            List<float[]> itemFloatDataList = new List<float[]>();
            object[] ncVars = _util.GetVariables();
            ucar.ma2.Array xData = null;
            ucar.ma2.Array yData = null;

            foreach (object ncVar in ncVars)
            {
                ucar.nc2.Variable var = ((ucar.nc2.Variable)ncVar);
                string varName = var.getFullName();

                if (varName == _settings.ZAxisName)
                    yData = _util.GetAllVariableData(var);
            }

            foreach (object ncVar in ncVars)
            {
                ucar.nc2.Variable var = ((ucar.nc2.Variable)ncVar);
                string varName = var.getFullName();

                if (varName == _settings.XAxisName)
                    xData = _util.GetAllVariableData(var);
            }

            foreach (object ncVar in ncVars)
            {
                ucar.nc2.Variable var = ((ucar.nc2.Variable)ncVar);
                string varName = var.getFullName();

                if (varName == itemName)
                {
                    List<ucar.ma2.Array> itemDataLst = _util.Get2DVariableDataTrans(var, _settings.ZLayer, _settings.ZAxisName, timeStep, _settings.TimeAxisName, _settings.XAxisName, _settings.YAxisName, _settings.TransectPoints, _settings.TransectSpaceStepsNumber);
                    for (int i = 0; i < itemDataLst.Count; i++ )
                    {
                        ucar.ma2.Array itemData = itemDataLst[i];
                        itemData = _util.ProcessedVariableData(var, itemData);
                        java.util.List ncVarAtt = var.getAttributes();
                        itemFloatData = _util.GetFloatData(itemData, ncVarAtt, _fdel);
                        itemFloatDataList.Add(itemFloatData);
                    }
                }
            }
            return itemFloatDataList;
        }

        private float[] _processFloatData(List<float[]> floatDataList)
        {
            try
            {
                List<float> compiledData = new List<float>();

                double y0 = 0, dy = 0;
                List<double> interpolatedYVals = new List<double>();
                List<double> originalYVals = new List<double>();
                ucar.nc2.Variable depthVar = null;
                object[] ncDims = _util.GetDimensions();
                foreach (object ncDim in ncDims)
                {
                    string dimName = ((ucar.nc2.Dimension)ncDim).getName();
                    if (_settings.ZAxisName == dimName) //depth is y axis
                    {
                       ucar.nc2.Variable ncVar = (ucar.nc2.Variable)_util.GetVariable(dimName);
                       depthVar = ncVar;
                        _getMinAndInterval(ncVar, out dy, out y0, out interpolatedYVals, out originalYVals);
                    }
                }

                if (_floatArraySize > _settings.MaxBlockSizeMB)
                {
                    _getMinAndInterval(depthVar, null, out dy, out y0, out interpolatedYVals, out originalYVals, true);
                    y0 = 0;
                }

                //loop through original depths
                if (interpolatedYVals.Count == 0)
                {
                    for (int i = originalYVals.Count - 1; i >= 0; i--)
                    {
                        foreach (float[] data in floatDataList)
                        {
                            compiledData.Add(data[i]);
                        }
                    }
                }
                else
                {
                    int originalValCount = originalYVals.Count - 1;
                    //int prevOriginalValCount = 0;
                    for (int i = interpolatedYVals.Count - 1; i >= 0; i--)
                    {
                        //find originalYValCount closest to interpolatedYVal
                        double closest = originalYVals.Aggregate((x, y) => Math.Abs(x - interpolatedYVals[i]) < Math.Abs(y - interpolatedYVals[i]) ? x : y);
                        originalValCount = originalYVals.IndexOf(closest);
                        
                        /*if (interpolatedYVals[i] < originalYVals[originalValCount])
                        {
                            originalValCount--;
                            prevOriginalValCount = originalValCount-1;
                        }
                        if (originalValCount <= 0) originalValCount = 0;
                        if (prevOriginalValCount <= 0) prevOriginalValCount = 0;*/

                        foreach (float[] data in floatDataList)
                        {
                            //interpolation
                            //y = y0 + (y1-y0)*((x-x0)/(x1-x0))
                            float interpolatedData = 0;
                            /*if (prevOriginalValCount != originalValCount)
                            {
                                if (data[prevOriginalValCount] != _fdel && data[originalValCount] != _fdel)
                                {
                                    interpolatedData = data[originalValCount] +
                                    (float)((data[prevOriginalValCount] - data[originalValCount]) * ((interpolatedYVals[i] - originalYVals[originalValCount]) / (originalYVals[prevOriginalValCount] - originalYVals[originalValCount])));

                                    if (((Math.Abs(data[originalValCount]) - Math.Abs(interpolatedData)) / Math.Abs(data[originalValCount]) * 100) <= _deleteValTolerancePercent)
                                        interpolatedData = data[originalValCount];
                                }
                                else
                                    interpolatedData = data[originalValCount];
                            }
                            else*/
                                interpolatedData = data[originalValCount];
                            compiledData.Add(interpolatedData);
                        }
                    }
                }
                return compiledData.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Process data error: " + ex.Message);
            }
        }

        private void _checkMike()
        {
            string mikeBinFolder = string.Empty;
            System.Reflection.Assembly asmDHIFl = System.Reflection.Assembly.LoadWithPartialName("DHI.DHIfl"); //only for GAC assemblies           

            string sDHIInstallPath = (string)_runMethod("GetInstallationRoot", "DHI.DHIfl.DHIConfigFiles", asmDHIFl, null, null);

            if (sDHIInstallPath == null) throw new Exception("Mike by DHI not found on this computer");
            else
            {
                mikeBinFolder = System.IO.Path.Combine(sDHIInstallPath, "bin");
            }
        }

        private object _runMethod(string methodName, string typeName, System.Reflection.Assembly asmName, object invokeObj, object[] invokeParam)
        {
            System.Type asmType = asmName.GetType(typeName, true, true);
            System.Reflection.MethodInfo asmMethod = asmType.GetMethod(methodName);
            object returnObj = asmMethod.Invoke(invokeObj, invokeParam);
            return returnObj;
        }

        public bool CanPlot()
        {
            return true;
        }
    }
}
