using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using ucar.nc2;
using ucar.nc2.dataset;
using ucar.util;
using ucar;
using java;
using java.io;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;

namespace DHI.Generic.NetCDF.MIKE.Commands
{
    public class ConvertNcToDfs1 : iCommand
    {
        private CommandSettings _settings = null;
        private NetCdfUtilities _util = null;
        private CFMapping _cfMap = null;
        private float _fdel = -1e-30f;
        private bool _invertxData = false;
        private bool _invertyData = false;
        private bool _customDFSGrid = false;
        private List<DataIndex> _ncIndexes;

        public string CommandDescription()
        {
            return "Converts a structured grid netcdf file (CF standard) to a dfs1 file";
        }

        public string CommandInputFileExtension()
        {
            return ".nc";
        }

        public string CommandOutputFileExtension()
        {
            return ".dfs1";
        }

        public void Execute(CommandSettings settings)
        {
            try
            {
                this._settings = settings;
                this._util = new NetCdfUtilities(settings.InputFileName, settings.UseDataSet);
                this._cfMap = new CFMapping();

                _checkMike();
                _convert2Dfs1();
            }
            catch (Exception ex)
            {
                throw new Exception("Command error: " + ex.Message);
            }
        }

        private void _convert2Dfs1()
        {
            IntPtr headerPointer = new IntPtr();
            IntPtr filePointer = new IntPtr();

            try
            {
                int maxTimeStep = _getTimeSteps();
                _customDFSGrid = false;

                // Create header
                System.Reflection.AssemblyName assName = this.GetType().Assembly.GetName();
                //if (maxTimeStep <= 1)
                headerPointer = DfsDLLWrapper.dfsHeaderCreate(FileType.EqtimeFixedspaceAllitems,
                System.IO.Path.GetFileNameWithoutExtension(_settings.InputFileName), assName.Name,
                    assName.Version.Major, _getItemNum(), StatType.NoStat);

                // Setup header
                DfsDLLWrapper.dfsSetDataType(headerPointer, 0);
                double x0 = 0, y0 = 0, dx = 0, dy = 0, j = 0, k = 0, lon0 = 0, lat0 = 0;
                _getGridOrigo(out x0, out y0, out dx, out dy, out j, out k, out lat0, out lon0);
                DfsDLLWrapper.dfsSetGeoInfoUTMProj(headerPointer, _settings.MZMapProjectionString, lon0, lat0, _settings.OverwriteRotation);
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
                        //swap range if dx or dy is negative
                        if (dx <= 0)
                        {
                            dx = Math.Abs(dx);
                            _invertxData = true;
                        }
                        if (dy <= 0)
                        {
                            dy = Math.Abs(dy);
                            _invertyData = true;
                        }
                        DfsDLLWrapper.dfsSetItemAxisEqD1(itemPointer, (int)eumUnit.eumUdegree, (int)j, (float)x0, (float)dx);

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
                            DfsDLLWrapper.dfsWriteItemTimeStep(headerPointer, filePointer, dTotalSeconds, _getFloatData(itemName, timeSteps, j, k, lat0, lon0, dx, dy));
                            selectedItemCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Convert2Dfs2 Error: " + ex.Message);
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

        private void _getGridOrigo(out double x0, out double y0, out double dx, out double dy, out double j, out double k, out double lat0, out double lon0)
        {
            x0 = 0; y0 = 0; dx = 0; dy = 0; j = 0; k = 0; lat0 = 0; lon0 = 0;
            object[] ncVars = _util.GetVariables();
            foreach (object ncVar in ncVars)
            {
                ucar.nc2.Variable var = (ucar.nc2.Variable)ncVar;
                string varName = var.getFullName();

                if (_settings.XAxisName == varName)
                {
                    _getMinAndInterval(var, _settings.XLayer, out dx, out lon0, out j);
                    x0 = 0;

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
                else if (_settings.YAxisName == varName)
                {
                    _getMinAndInterval(var, _settings.YLayer, out dy, out lat0, out k);
                    y0 = 0;

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
            }
            if (_settings.OverwriteOriginX != -999 && _settings.OverwriteOriginX != -999)
            {
                lon0 = _settings.OverwriteOriginX;
                lat0 = _settings.OverwriteOriginY;
            }
        }

        private void _getMinAndInterval(ucar.nc2.Variable var, string minMaxStr, out double interval, out double minVal, out double count)
        {
            ucar.ma2.Array dataArr = _util.GetAllVariableData(var);
            bool stepExists = false;
            interval = 1.0; minVal = double.MaxValue; count = 0;
            java.util.List ncVarAtt = var.getAttributes();
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

            double prevVal = 0.0;
            int minCount = 0, maxCount = (int)dataArr.getSize() - 1; count = 0;
            if (!string.IsNullOrEmpty(minMaxStr))
            {
                minCount = Convert.ToInt32(minMaxStr.Split(':')[0]);
                maxCount = Convert.ToInt32(minMaxStr.Split(':')[1]);
                count = maxCount - minCount + 1;
            }
            else
                count = maxCount + 1;

            for (int dCount = minCount; dCount <= maxCount; dCount++)
            {
                ucar.ma2.Index dIndex = dataArr.getIndex();
                double data = dataArr.getDouble(dIndex.set(dCount));
                if (minVal >= data) minVal = data;
                if (!stepExists)
                {
                    if (dCount > 0)
                    {
                        prevVal = dataArr.getDouble(dIndex.set(dCount - 1));
                        interval = data - prevVal;
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
            float[] itemFloatData = null;
            object[] ncVars = _util.GetVariables();
            ucar.ma2.Array xData = null;
            ucar.ma2.Array yData = null;
            ucar.ma2.Array itemData = null;
            _settings.TimeLayer = timeStep;

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

                if (varName == itemName)
                {
                    java.util.List varDims = ((Variable)var).getDimensions();
                    int xAxisPosition = -1, yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (_settings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (_settings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    itemData = _util.Get1DVariableData(var, _settings);
                    itemData = _util.ProcessedVariableData(var, itemData);
                    java.util.List ncVarAtt = var.getAttributes();

                    if (!_customDFSGrid)
                    {
                        if (_invertxData && _invertyData)
                        {
                            //invert xData and yData
                            itemFloatData = _util.GetFloatDataInvertXandY(itemData, xData, yData, ncVarAtt, _fdel);
                        }
                        else if (_invertyData)
                        {
                            //invert yData
                            itemFloatData = _util.GetFloatDataInvertY(itemData, xData, yData, ncVarAtt, _fdel);
                        }
                        else if (_invertxData)
                        {
                            //invert xData
                            itemFloatData = _util.GetFloatDataInvertX(itemData, xData, yData, ncVarAtt, _fdel);
                        }
                        else
                            itemFloatData = _util.GetFloatData(itemData, xData, yData, ncVarAtt, _fdel, xAxisPosition, yAxisPosition);

                    }
                    else
                    {
                        //reassign data to grid
                        if (yAxisPosition > xAxisPosition)
                            itemFloatData = _reassignData(itemData, xData, yData, ncVarAtt, _fdel, j, k, lat0, lon0, dx, dy, 0, 1);
                        else
                            itemFloatData = _reassignData(itemData, xData, yData, ncVarAtt, _fdel, j, k, lat0, lon0, dx, dy, 1, 0);
                    }
                }
            }
            return itemFloatData;
        }

        public float[] _reassignData(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, float delVal, double j, double k, double lat0, double lon0, double dx, double dy, int xPosition, int yPosition)
        {
            try
            {
                int resCount = 0;
                ucar.ma2.Index xIndex = xData.getIndex();
                ucar.ma2.Index yIndex = yData.getIndex();

                ucar.ma2.Index resIndex = sourceData.getIndex();
                int[] resShape = resIndex.getShape();

                List<double> xCoorList = new List<double>();
                for (int xLayerCount = 0; xLayerCount < j; xLayerCount++)
                {
                    double lon = lon0 + xLayerCount * dx;
                    xCoorList.Add(lon);
                }

                List<double> yCoorList = new List<double>();
                for (int yLayerCount = 0; yLayerCount < k; yLayerCount++)
                {
                    double lat = lat0 + yLayerCount * dy;
                    yCoorList.Add(lat);
                }

                //get indexes and values from nc file
                if (resShape.Length > 1)
                    _ncIndexes = _generateNCIndexes(sourceData, xData, yData, xIndex, yIndex, resShape, xPosition, yPosition, attList, xCoorList, yCoorList);
                else
                    _ncIndexes = _generateNCIndexes1D(sourceData, xData, yData, xIndex, yIndex, resShape, xPosition, yPosition, attList, xCoorList, yCoorList);

                //assign values to dfs2 grid
                List<DataIndex> matchedIndex = new List<DataIndex>();

                float[] resfloat = new float[(int)j * (int)k];
                for (int i = 0; i < resfloat.Length; i++)
                {
                    resfloat[i] = delVal;
                }

                int resCountNC = 0;
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


                    for (int y = 0; y < yCounts.Count; y++)
                    {
                        for (int x = 0; x < xCounts.Count; x++)
                        {
                            resCountNC = (xCounts[x]) +
                                             (yCounts[y]) * (xCoorList.Count);

                            if (resCountNC >= resfloat.Length || resCountNC < 0)
                                throw new Exception("out of bounds - " + resCountNC.ToString() + " + >= array size of " + resfloat.Length.ToString());

                            resfloat[resCountNC] = (float)_ncIndexes[i].data;

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

        private List<DataIndex> _generateNCIndexes(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, ucar.ma2.Index xIndex, ucar.ma2.Index yIndex, int[] resShape, int xPosition, int yPosition, java.util.List attList, List<double> xList, List<double> yList)
        {
            List<DataIndex> ncIndexes = new List<DataIndex>();

            ucar.ma2.Index resIndex = sourceData.getIndex();
            for (int yCount = 1; yCount < (int)resShape[yPosition]; yCount++)
            {
                for (int xCount = 1; xCount < (int)resShape[xPosition]; xCount++)
                {
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


                    DataIndex newIndex = new DataIndex();
                    newIndex.xIndex = xCount;
                    newIndex.prevXIndex = xCount - 1;
                    newIndex.yIndex = yCount;
                    newIndex.prevYIndex = yCount - 1;
                    newIndex.nc_X = lonFromNC;
                    newIndex.nc_Y = latFromNC;
                    newIndex.pnc_X = prevLonFromNC;
                    newIndex.pnc_Y = prevLatFromNC;

                    newIndex.data = sourceData.getDouble(resIndex.set(yCount, xCount));
                    newIndex.prevData = sourceData.getDouble(resIndex.set(yCount - 1, xCount - 1));

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

                    if (_util.IsValueValid(attList, newIndex.data) && rangeCount == 4)
                        ncIndexes.Add(newIndex);
                }
            }

            return ncIndexes;
        }

        private List<DataIndex> _generateNCIndexes1D(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, ucar.ma2.Index xIndex, ucar.ma2.Index yIndex, int[] resShape, int xPosition, int yPosition, java.util.List attList, List<double> xList, List<double> yList)
        {
            //assuming that the file is 1D with the same lat. or same lon. 

            List<DataIndex> ncIndexes = new List<DataIndex>();
            ucar.ma2.Index resIndex = sourceData.getIndex();

            //find closest x and y points
            int[] ydataShape = yData.getShape();
            int[] xdataShape = xData.getShape();

            int yCount = 0; //static

            for (int xCount = 1; xCount < (int)resShape[xPosition]; xCount++)
            {
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

                }

                int rangeCount = 0;

                if (latFromNC >= yList.Min()) rangeCount++;
                if (latFromNC <= yList.Max()) rangeCount++;
                if (lonFromNC >= xList.Min()) rangeCount++;
                if (lonFromNC <= xList.Max()) rangeCount++;


                DataIndex newIndex = new DataIndex();
                newIndex.xIndex = xCount;
                newIndex.prevXIndex = xCount - 1;
                newIndex.yIndex = yCount;
                newIndex.prevYIndex = yCount - 1;
                newIndex.nc_X = lonFromNC;
                newIndex.nc_Y = latFromNC;
                newIndex.pnc_X = prevLonFromNC;
                newIndex.pnc_Y = prevLatFromNC;

                newIndex.data = sourceData.getDouble(resIndex.set(xCount));
                newIndex.prevData = sourceData.getDouble(resIndex.set(xCount - 1));

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
                    else if (xPoint == lonFromNC)
                    {
                        newIndex.lonFromDfs.Add(xPoint);
                    }
                }

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
                    else if (yPoint == latFromNC)
                    {
                        newIndex.latFromDfs.Add(yPoint);
                    }
                }

                if (_util.IsValueValid(attList, newIndex.data) && rangeCount == 4)
                    ncIndexes.Add(newIndex);
            }


            return ncIndexes;
        }

        private static List<DataIndex> _getNcIndex(List<double> xCoorList, List<double> yCoorList, List<DataIndex> ncIndexes, int yCount, int xCount)
        {
            double latFromDfs = yCoorList[yCount];
            double lonFromDfs = xCoorList[xCount];
            List<DataIndex> ncIndexesCount = new List<DataIndex>();

            for (int i = 1; i < ncIndexes.Count; i++)
            {
                if (ncIndexes[i].lonFromDfs.Count > 0 && ncIndexes[i].latFromDfs.Count > 0)
                {

                    int xcheckCount = 0, ycheckCount = 0;

                    foreach (double xPoint in ncIndexes[i].lonFromDfs)
                        if (lonFromDfs == xPoint) xcheckCount++;

                    foreach (double yPoint in ncIndexes[i].latFromDfs)
                        if (latFromDfs == yPoint) ycheckCount++;


                    if (xcheckCount >= 1 && ycheckCount >= 1)
                        ncIndexesCount.Add(ncIndexes[i]);
                }

            }
            return ncIndexesCount;
        }

        private class DataIndex
        {
            public int xIndex, yIndex, zIndex;
            public int prevXIndex, prevYIndex, prevZIndex;
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
