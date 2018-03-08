using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;

namespace DHI.Generic.NetCDF.MIKE.Commands
{
    public class ConvertGrib2ToDfs3: iCommand
    {
        private CommandSettings _settings = null;
        private NetCdfUtilities _util = null;
        private CFMapping _cfMap = null;
        private float _fdel = -1e-30f;
        private List<double> _zValues;
        private bool _customDFSGrid = false;
        private List<DataIndex> _ncIndexes;

        public string CommandDescription()
        {
            return "Converts a structured grid GRIB2 to a dfs3 file";
        }

        public string CommandInputFileExtension()
        {
            return ".grib2";
        }

        public string CommandOutputFileExtension()
        {
            return ".dfs3";
        }

        public void Execute(CommandSettings settings)
        {
            try
            {
                this._settings = settings;
                this._util = new NetCdfUtilities(settings.InputFileName, settings.UseDataSet);
                this._cfMap = new CFMapping();

                _checkMike();
                _convert2Dfs3();
            }
            catch (Exception ex)
            {
                throw new Exception("Command error: " + ex.Message);
            }
        }

        private void _convert2Dfs3()
        {
            IntPtr headerPointer = new IntPtr();
            IntPtr filePointer = new IntPtr();
            double x0 = 0, y0 = 0, z0 = 0, dx = 0, dy = 0, dz = 0, j = 0, k = 0, l = 0, lat0 = 0, lon0 = 0;
            
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
                DfsDLLWrapper.dfsSetDataType(headerPointer, 2);
                
                DfsDLLWrapper.dfsSetDeleteValFloat(headerPointer, _fdel);
                List<DateTime> dateTimes = _util.GetTime(_settings.TimeAxisName);

                _getGridOrigo(out x0, out y0, out z0, out dx, out dy, out dz, out j, out k, out l, out lat0, out lon0);

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
                        string firstItemName = _settings.Variables[0];
                        
                        DfsDLLWrapper.dfsSetItemAxisEqD3(itemPointer, (int)eumUnit.eumUdegree, (int)j, (int)k, (int)l, (float)x0, (float)y0, (float)z0, (float)dx, (float)dy, (float)dz);

                        selectedItemCount++;
                    }
                }

                DfsDLLWrapper.dfsSetGeoInfoUTMProj(headerPointer, _settings.MZMapProjectionString, lon0, lat0, _settings.OverwriteRotation);

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
                            float[] data = _getFloatData(itemName, timeSteps, j, k, lat0, lon0, dx, dy);
                            DfsDLLWrapper.dfsWriteItemTimeStep(headerPointer, filePointer, dTotalSeconds, data);
                            selectedItemCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Convert2Dfs3 Error: " + ex.Message);
            }
            finally
            {
                // close file and destroy header
                if (null != filePointer && 0 != filePointer.ToInt32())
                    DfsDLLWrapper.dfsFileClose(headerPointer, ref filePointer);

                if (null != headerPointer && 0 != headerPointer.ToInt32())
                    DfsDLLWrapper.dfsHeaderDestroy(ref headerPointer);
            }
        }

        private void _getGridOrigo(out double x0, out double y0, out double z0, out double dx, out double dy, out double dz, out double j, out double k, out double l, out double lat0, out double lon0)
        {
            x0 = 0; y0 = 0; z0 = 0; dx = 0; dy = 0; dz = 0; j = 0; k = 0; l = 0; lat0 = 0; lon0 = 0;
            object[] ncDims = _util.GetDimensions();
            
            List<double> interpolatedZVals = new List<double>();
            List<double> originalZVals = new List<double>();
            ucar.nc2.Variable depthVar = null;
            foreach (object ncDim in ncDims)
            {
                string dimName = ((ucar.nc2.Dimension)ncDim).getName();
                if (_settings.XAxisName == dimName)
                {
                    ucar.nc2.Variable ncVar = (ucar.nc2.Variable)_util.GetVariable(dimName);
                    List<double> interpolatedYVals = new List<double>();
                    List<double> originalYVals = new List<double>();
                    _getMinAndInterval(ncVar, _settings.XLayer, out dx, out lon0, out interpolatedYVals, out originalYVals, false);
                    j = originalYVals.Count;
                    x0 = 0;// originalYVals[0];

                    //overwrite dx, j
                    if (_settings.NumberXCells > 0)
                    {
                        j = _settings.NumberXCells;
                        //if (!double.TryParse(_settings.NumberXCells, out j))
                        //throw new Exception("Cannot convert " + _settings.NumberXCells + " to double");
                        _customDFSGrid = true;
                    }
                    if (_settings.DX > 0)
                    {
                        dx = _settings.DX;
                        //if (!double.TryParse(_settings.DX, out dx))
                        //throw new Exception("Cannot convert " + _settings.DX + " to double");
                        _customDFSGrid = true;
                    }

                    if (!String.IsNullOrEmpty(_settings.ProjectionEastNorthMultiplier))
                    {
                        Microsoft.JScript.Vsa.VsaEngine myEngine = Microsoft.JScript.Vsa.VsaEngine.CreateEngine();
                        double result = (double)Microsoft.JScript.Eval.JScriptEvaluate(_settings.ProjectionEastNorthMultiplier, myEngine);
                        dx = dx * result;
                    }
                }
                else if (_settings.YAxisName == dimName)
                {
                    ucar.nc2.Variable ncVar = (ucar.nc2.Variable)_util.GetVariable(dimName);
                    List<double> interpolatedYVals = new List<double>();
                    List<double> originalYVals = new List<double>();
                    _getMinAndInterval(ncVar, _settings.YLayer, out dy, out lat0, out interpolatedYVals, out originalYVals, false);
                    k = originalYVals.Count;

                    y0 = 0;// originalYVals[0];

                    //overwrite dy, k
                    if (_settings.NumberYCells > 0)
                    {
                        k = _settings.NumberYCells;
                        //if (!double.TryParse(_settings.NumberYCells, out k))
                        //throw new Exception("Cannot convert " + _settings.NumberYCells + " to double");
                        _customDFSGrid = true;
                    }
                    if (_settings.DY > 0)
                    {
                        dy = _settings.DY;
                        //if (!double.TryParse(_settings.DY, out dy))
                        //throw new Exception("Cannot convert " + _settings.DY + " to double");
                        _customDFSGrid = true;
                    }

                    if (!String.IsNullOrEmpty(_settings.ProjectionEastNorthMultiplier))
                    {
                        Microsoft.JScript.Vsa.VsaEngine myEngine = Microsoft.JScript.Vsa.VsaEngine.CreateEngine();
                        double result = (double)Microsoft.JScript.Eval.JScriptEvaluate(_settings.ProjectionEastNorthMultiplier, myEngine);
                        dy = dy * result;
                    }
                }
                else if (_settings.ZAxisName == dimName) //depth is y axis
                {
                    depthVar = (ucar.nc2.Variable)_util.GetVariable(dimName);

                    if (_settings.DZ==0)
                    {
                        _getMinAndInterval(depthVar, null, out dz, out z0, out interpolatedZVals, out originalZVals, false);
                        if (interpolatedZVals.Count != 0)
                        {
                            l = interpolatedZVals.Count;
                            _zValues = interpolatedZVals;
                        }
                        else
                        {
                            l = originalZVals.Count;
                            _zValues = originalZVals;
                        }
                        z0 = 0;
                    }
                    else
                    {
                        _getMinAndInterval(depthVar, null, out dz, out z0, out interpolatedZVals, out originalZVals, true);
                        if (interpolatedZVals.Count != 0)
                        {
                            l = interpolatedZVals.Count;
                            _zValues = interpolatedZVals;
                            z0 = interpolatedZVals[interpolatedZVals.Count - 1];
                        }
                        else
                        {
                            l = originalZVals.Count;
                            _zValues = originalZVals;
                            z0 = originalZVals[originalZVals.Count - 1];
                        }
                        z0 = 0;
                    }
                }
            }

            if (_settings.OverwriteOriginX != -999 && _settings.OverwriteOriginX != -999)
            {
                lon0 = _settings.OverwriteOriginX;
                lat0 = _settings.OverwriteOriginY;
            }

            //calculate the size of the possible float array and limit it to _settings.MaxBlockSizeMB
            double floatArraySize = j * k * l * 4 / 1024 / 1024; //in mb
            if (floatArraySize > _settings.MaxBlockSizeMB)
            {
                _settings.DZ = (float)(originalZVals.Max() / (_settings.MaxBlockSizeMB / j / k / 4 * 1024 * 1024));
                _getMinAndInterval(depthVar, null, out dz, out z0, out interpolatedZVals, out originalZVals, true);
                if (interpolatedZVals.Count != 0)
                {
                    l = interpolatedZVals.Count;
                    _zValues = interpolatedZVals;
                }
                else
                {
                    l = originalZVals.Count;
                    _zValues = originalZVals;
                }
                z0 = 0;
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
                        for (double min = minVal; min <= maxVal; min += intervals.Min())
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

        private float[] _getFloatData(string itemName, int timeStep, double j, double k, double lat0, double lon0, double dx, double dy)
        {
            try
            {
                float[] itemFloatData = null;
                object[] ncVars = _util.GetVariables();
                ucar.ma2.Array xData = null;
                ucar.ma2.Array yData = null;
                ucar.ma2.Array zData = null;

                foreach (object ncVar in ncVars)
                {
                    ucar.nc2.Variable var = ((ucar.nc2.Variable)ncVar);
                    string varName = var.getFullName();

                    if (varName == _settings.YAxisName)
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

                    if (varName == _settings.ZAxisName)
                        zData = _util.GetAllVariableData(var);
                }

                foreach (object ncVar in ncVars)
                {
                    ucar.nc2.Variable var = ((ucar.nc2.Variable)ncVar);
                    string varName = var.getFullName();

                    java.util.List varDims = ((ucar.nc2.Variable)var).getDimensions();
                    int zPosition = -1, xAxisPosition = -1, yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((ucar.nc2.Dimension)varDims.get(i)).getName();
                        if (_settings.ZAxisDimensionName == dimName) zPosition = i;
                        if (_settings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (_settings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    if (varName == itemName)
                    {
                        ucar.ma2.Array itemData = _util.Get3DVariableData(var, _settings.ZAxisName, timeStep, _settings.TimeAxisName, _settings.XLayer, _settings.XAxisName, _settings.YLayer, _settings.YAxisName);
                        itemData = _util.ProcessedVariableData(var, itemData);
                        java.util.List ncVarAtt = var.getAttributes();
                        itemFloatData = _util.GetFloatData(itemData, xData, yData, zData, ncVarAtt, _fdel, _zValues);
                        if (_customDFSGrid)
                        {
                            //reassign the data to the grid (nearest neighbour)
                            itemFloatData = _reassignData(itemData, xData, yData, zData, ncVarAtt, _fdel, _zValues, j, k, lat0, lon0, dx, dy, xAxisPosition, yAxisPosition, zPosition);
                        }
                    }
                }
                return itemFloatData;
            }
            catch (Exception ex)
            {
                throw new Exception("_getFloatData Error: " + ex.Message);
            }
        }

        public float[] _reassignData(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, ucar.ma2.Array zData, java.util.List attList, float delVal, List<double> zValues, double j, double k, double lat0, double lon0, double dx, double dy, int xPosition, int yPosition, int zPosition)
        {
            try
            {
                int resCountNC = 0;
                ucar.ma2.Index xIndex = xData.getIndex();
                ucar.ma2.Index yIndex = yData.getIndex();
                ucar.ma2.Index zIndex = zData.getIndex();
                ucar.ma2.Index resIndex = sourceData.getIndex();
                int[] resShape = resIndex.getShape();

                List<double> xCoorList = new List<double>();
                for (int xLayerCount = 0; xLayerCount < j; xLayerCount++)
                {
                    //double xValue = xData.getDouble(xLayerCount);
                    double lon = lon0 + xLayerCount * dx;
                    xCoorList.Add(lon);
                }

                List<double> yCoorList = new List<double>();
                for (int yLayerCount = 0; yLayerCount < k; yLayerCount++)
                {
                    //double yValue = yData.getDouble(yLayerCount);
                    double lat = lat0 + yLayerCount * dy;
                    yCoorList.Add(lat);
                }

                float[] resfloat = new float[(int)j * (int)k * zValues.Count];
                for (int i = 0; i < resfloat.Length; i++)
                {
                    resfloat[i] = delVal;
                }

                //get indexes and values from nc file
                _ncIndexes = _generateNCIndexes(sourceData, xData, yData, zData, xIndex, yIndex, resShape, xPosition, yPosition, zPosition, attList, xCoorList, yCoorList, zValues);

                //assign values to dfs3 grid
                for (int i = 0; i < _ncIndexes.Count; i++)
                {
                    List<int> xCounts = new List<int>();
                    for (int a = 0; a < _ncIndexes[i].lonFromDfs.Count; a++)
                    {
                        int xCount = xCoorList.FindIndex(x => x == _ncIndexes[i].lonFromDfs[a]);
                        xCounts.Add(xCount);
                    }

                    List<int> yCounts = new List<int>();
                    for (int a = 0; a < _ncIndexes[i].latFromDfs.Count; a++)
                    {
                        int yCount = yCoorList.FindIndex(x => x == _ncIndexes[i].latFromDfs[a]);
                        yCounts.Add(yCount);
                    }

                    List<int> zCounts = new List<int>();
                    for (int a = 0; a < _ncIndexes[i].zFromDfs.Count; a++)
                    {
                        int zCount = zValues.FindIndex(x => x == _ncIndexes[i].zFromDfs[a]);
                        zCounts.Add(zCount);
                    }

                    for (int z = 0; z < zCounts.Count; z++)
                    {
                        for (int y = 0; y < yCounts.Count; y++)
                        {
                            for (int x = 0; x < xCounts.Count; x++)
                            {
                                resCountNC = (xCounts[x]) +
                                                 (yCounts[y]) * (xCoorList.Count) +
                                                 (zValues.Count - 1 -zCounts[z]) * (xCoorList.Count) * (yCoorList.Count);

                                if (resCountNC >= resfloat.Length || resCountNC < 0)
                                    throw new Exception("out of bounds - " + resCountNC.ToString() + " + >= array size of " + resfloat.Length.ToString());
                                
                                resfloat[resCountNC] = (float)_ncIndexes[i].data;
                                
                            }
                        }
                    }
                }

                return resfloat;
            }
            catch (Exception ex)
            {
                throw new Exception("_reassignData Error: " + ex.Message);
            }
        }

        private List<DataIndex> _generateNCIndexes(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, ucar.ma2.Array zData, ucar.ma2.Index xIndex, ucar.ma2.Index yIndex, int[] resShape, int xPosition, int yPosition, int zPosition, java.util.List attList, List<double> xList, List<double> yList, List<double> zList)
        {
            List<DataIndex> ncIndexes = new List<DataIndex>();
            ucar.ma2.Index resIndex = sourceData.getIndex();

            for (int zCount = (int)resShape[zPosition-1] - 1; zCount > 0; zCount--)
            {
                //find closest zValues
                double zValueFromNC = zData.getDouble(zCount);
                double prevZValueFromNC = 0;

                if (zCount > 1)
                    prevZValueFromNC = zData.getDouble(zCount - 1);

                if (zValueFromNC <= zList.Max())
                {
                    foreach (double xPoint in xList)
                    {
                        foreach (double yPoint in yList)
                        {
                            DataIndex newIndex = new DataIndex();
                            newIndex.xPoint = xPoint;
                            newIndex.yPoint = yPoint;

                            newIndex.zFromDfs = new List<double>();
                            foreach (double zPoint in zList)
                            {
                                if (zPoint >= prevZValueFromNC && zPoint <= zValueFromNC) newIndex.zFromDfs.Add(zPoint);
                            }

                            if (zCount == 1) newIndex.zFromDfs.Add(zList.Min());

                            newIndex.zIndex = zCount;
                            newIndex.prevZIndex = zCount - 1;
                            newIndex.nc_Z = zValueFromNC;
                            newIndex.pnc_Z = prevZValueFromNC;

                            int dataCount = 0;//(int)resShape[xPos];
                            List<double> distances = new List<double>();
                            List<double> distancesData = new List<double>();


                            for (int yCount = 0; yCount < (int)resShape[yPosition-1]-1; yCount++)
                            {
                                for (int xCount = 0; xCount < (int)resShape[xPosition-1]-1; xCount++)
                                {
                                    double latFromNC = yData.getDouble(yIndex.set(yCount, xCount));
                                    double lonFromNC = xData.getDouble(xIndex.set(yCount, xCount));

                                    int rangeCount = 0;
                                    if (latFromNC >= yList.Min()-1) rangeCount++;
                                    if (latFromNC <= yList.Max()+1) rangeCount++;
                                    if (lonFromNC >= xList.Min()-1) rangeCount++;
                                    if (lonFromNC <= xList.Max()+1) rangeCount++;

                                    if (rangeCount == 4)
                                    {
                                        //find closest x and y points
                                        try
                                        {
                                            double distancex1 = Math.Sqrt((lonFromNC - xPoint) * (lonFromNC - xPoint) + (latFromNC - yPoint) * (latFromNC - yPoint));
                                            double data = sourceData.getDouble(resIndex.set(zCount, yCount, xCount));
                                            distances.Add(distancex1);
                                            distancesData.Add(data);
                                            dataCount++;
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                }
                            }

                            int smallestIndex = IndexOfMin(distances);
                            double smallestDistance = distances[smallestIndex];
                            newIndex.data = distancesData[smallestIndex];

                            if (_util.IsValueValid(attList, newIndex.data) && smallestDistance <= 3 * Convert.ToDouble(_settings.DX) && newIndex.zFromDfs.Count > 0)
                                ncIndexes.Add(newIndex);
                        }
                    }
            
            /*List<DataIndex> ncIndexes = new List<DataIndex>();

            ucar.ma2.Index resIndex = sourceData.getIndex();
            for (int zCount = (int)resShape[zPosition - 1] - 1; zCount > 0; zCount--)
            {
                for (int yCount = 1; yCount < (int)resShape[yPosition - 1]; yCount++)
                {
                    for (int xCount = 1; xCount < (int)resShape[xPosition - 1]; xCount++)
                    {
                        //find closest zValues
                        double zValueFromNC = zData.getDouble(zCount);
                        double prevZValueFromNC = zData.getDouble(zCount - 1);

                        //find closest x and y points
                        int[] ydataShape = yData.getShape();
                        int[] xdataShape = xData.getShape();

                        double latFromNC = 0;
                        double prevLatFromNC = 0;
                        double prevPrevLatFromNC = 0;
                        if (ydataShape.Length == 2)
                        {
                            latFromNC = yData.getDouble(yIndex.set(yCount, xCount));
                            prevLatFromNC = yData.getDouble(yIndex.set(yCount - 1, xCount));
                            if (yCount >= 2)
                                prevPrevLatFromNC = yData.getDouble(yIndex.set(yCount - 2, xCount));
                        }
                        else if (ydataShape.Length == 1)
                        {
                            latFromNC = yData.getDouble(yCount);
                            prevLatFromNC = yData.getDouble(yCount - 1);
                            if (yCount >= 2)
                                prevPrevLatFromNC = yData.getDouble(yCount - 2);
                        }

                        double lonFromNC = 0;
                        double prevLonFromNC = 0;
                        double prevPrevLonFromNC = 0;
                        if (xdataShape.Length == 2)
                        {
                            lonFromNC = xData.getDouble(xIndex.set(yCount, xCount));
                            prevLonFromNC = xData.getDouble(xIndex.set(yCount, xCount - 1));
                            if (xCount >= 2)
                                prevPrevLonFromNC = xData.getDouble(xIndex.set(yCount, xCount - 2));
                        }
                        else if (xdataShape.Length == 1)
                        {
                            lonFromNC = xData.getDouble(xCount);
                            prevLonFromNC = xData.getDouble(xCount - 1);
                            if (xCount >= 2)
                                prevPrevLonFromNC = xData.getDouble(xCount - 2);
                        }

                        int rangeCount = 0;

                        if (latFromNC >= yList.Min()) rangeCount++;
                        if (latFromNC <= yList.Max()) rangeCount++;
                        if (lonFromNC >= xList.Min()) rangeCount++;
                        if (lonFromNC <= xList.Max()) rangeCount++;
                        if (zValueFromNC >= zList.Min()) rangeCount++;
                        if (zValueFromNC <= zList.Max()) rangeCount++;

                        DataIndex newIndex = new DataIndex();
                        newIndex.xIndex = xCount;
                        newIndex.prevXIndex = xCount - 1;
                        newIndex.yIndex = yCount;
                        newIndex.prevYIndex = yCount - 1;
                        newIndex.zIndex = zCount;
                        newIndex.prevZIndex = zCount - 1;
                        newIndex.nc_X = lonFromNC;
                        newIndex.nc_Y = latFromNC;
                        newIndex.nc_Z = zValueFromNC;
                        newIndex.pnc_X = prevLonFromNC;
                        newIndex.pnc_Y = prevLatFromNC;
                        newIndex.pnc_Z = prevZValueFromNC;

                        newIndex.data = sourceData.getDouble(resIndex.set(zCount, yCount, xCount));
                        newIndex.prevData = sourceData.getDouble(resIndex.set(zCount - 1, yCount - 1, xCount - 1));

                        //double closestX = xList.Aggregate((x, y) => Math.Abs(x - lonFromNC) < Math.Abs(y - lonFromNC) ? x : y);
                        //int closestXinNC = xList.IndexOf(closestX);
                        newIndex.lonFromDfs = new List<double>();
                        foreach (double xPoint in xList)
                        {
                            if (xPoint >= prevLonFromNC && xPoint <= lonFromNC)
                            {
                                newIndex.lonFromDfs.Add(xPoint);
                            }
                            else if (xPoint >= prevPrevLonFromNC && xPoint <= lonFromNC)
                            {
                                newIndex.lonFromDfs.Add(xPoint);
                            }
                        }

                        newIndex.zFromDfs = new List<double>();

                        double closest = zList.Aggregate((x, y) => Math.Abs(x - zValueFromNC) < Math.Abs(y - zValueFromNC) ? x : y);
                        newIndex.zFromDfs.Add(closest);

                        if (newIndex.zFromDfs.Count == 0)
                            throw new Exception("No z point?");

                        newIndex.latFromDfs = new List<double>();
                        foreach (double yPoint in yList)
                        {
                            if (yPoint >= prevLatFromNC && yPoint <= latFromNC)
                            {
                                newIndex.latFromDfs.Add(yPoint);
                            }
                            else if (yPoint >= prevPrevLatFromNC && yPoint <= latFromNC)
                            {
                                newIndex.latFromDfs.Add(yPoint);
                            }
                        }

                        if (_util.IsValueValid(attList, newIndex.data) && rangeCount == 6)
                            ncIndexes.Add(newIndex);

                    }*/
                }
            }
            return ncIndexes;
        }

        private static List<DataIndex> _getNcIndex(List<double> zValues, List<double> xCoorList, List<double> yCoorList, List<DataIndex> ncIndexes, int zCount, int yCount, int xCount)
        {
            double latFromDfs = yCoorList[yCount];
            double lonFromDfs = xCoorList[xCount];
            double depthFromDfs = zValues[zCount];
            List<DataIndex> ncIndexesCount = new List<DataIndex>();

            for (int i = 0; i < 1; i++)
            {
                if (latFromDfs == ncIndexes[i].nc_Y && lonFromDfs == ncIndexes[i].nc_X && depthFromDfs == ncIndexes[i].nc_Z)
                {
                    int xcheckCount = 0, ycheckCount = 0, zcheckCount = 0;
                    if (ncIndexes[i].lonFromDfs.Count > 0 && ncIndexes[i].latFromDfs.Count > 0)
                    {
                        foreach (double xPoint in ncIndexes[i].lonFromDfs)
                            if (lonFromDfs == xPoint) xcheckCount++;

                        foreach (double yPoint in ncIndexes[i].latFromDfs)
                            if (latFromDfs == yPoint) ycheckCount++;

                        foreach (double zPoint in ncIndexes[i].zFromDfs)
                            if (depthFromDfs == zPoint) zcheckCount++;

                        if (xcheckCount >= 1 && ycheckCount >= 1 && zcheckCount >= 1)
                            //return ncIndexes[i];
                            ncIndexesCount.Add(ncIndexes[i]);
                    }
                }
            }
            return ncIndexesCount;
        }

        public int IndexOfMin(List<double> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            double min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }

            return minIndex;
        }

        private class DataIndex
        {
            public int xIndex, yIndex, zIndex;
            public int prevXIndex, prevYIndex, prevZIndex;
            public double xPoint, yPoint;
            public double nc_X, nc_Y, nc_Z;
            public double pnc_X, pnc_Y, pnc_Z;
            public double data, prevData;
            public List<double> lonFromDfs, latFromDfs, zFromDfs;

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
