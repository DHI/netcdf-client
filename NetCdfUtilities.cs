using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.nc2;
using ucar.nc2.dataset;
using ucar.util;
using ucar;
using java;
using java.io;
using DHI.Generic.MikeZero;
using System.ComponentModel;


namespace DHI.Generic.NetCDF.MIKE
{
    public class NetCdfUtilities
    {
        private string _ncFilePath = string.Empty;
        private NetcdfFile _ncFile = null;

        public NetCdfUtilities(string ncFilePath, bool useDataSet)
        {
            this._ncFilePath = ncFilePath;
            CancelTask cancelTask = new CancelTask();
            if (useDataSet)
                _ncFile = NetcdfDataset.openDataset(_ncFilePath);
            else
                _ncFile = NetcdfFile.open(_ncFilePath);
        }

        public object[] GetDimensions()
        {
            java.util.List ncDims = _ncFile.getDimensions();
            return ncDims.toArray();
        }

        public object[] GetVariables()
        {
            java.util.List ncVars = _ncFile.getVariables();
            return ncVars.toArray();
        }

        public object GetVariable(string variableFullName)
        {
            java.util.List ncVars = _ncFile.getVariables();
            for (int i = 0; i < ncVars.size(); i++)
            {
                string varName = ((Variable)ncVars.get(i)).getFullName();
                if (variableFullName == varName) return ncVars.get(i);
            }
            return null;
        }

        public object[] GetGlobalAttributes()
        {
            java.util.List ncVars = _ncFile.getGlobalAttributes();
            return ncVars.toArray();
        }

        public java.util.List GetGlobalAttributesJava()
        {
            return _ncFile.getGlobalAttributes();
        }
        
        public ucar.ma2.Array GetAllVariableData(object variable)
        {
            ucar.ma2.Array data = ((Variable)variable).read();
            return data;
        }

        public List<DateTime> GetTime(string timeAxis)
        {
            List<DateTime> dt = new List<DateTime>();
            if (!string.IsNullOrEmpty(timeAxis))
            {
                object timeVarObj = GetVariable(timeAxis);
                ucar.ma2.Array timeArr = GetAllVariableData(timeVarObj);
                ucar.ma2.Index timeIndex = timeArr.getIndex();
                int[] timeShape = timeIndex.getShape();

                java.util.List attList = ((ucar.nc2.Variable)timeVarObj).getAttributes();
                string timeUnit = string.Empty;
                for (int i = 0; i < attList.size(); i++)
                {
                    if (((ucar.nc2.Attribute)attList.get(i)).getName() == "units")
                        timeUnit = ((ucar.nc2.Attribute)attList.get(i)).getValue(0).ToString();

                }

                switch (timeShape.Length)
                {
                    case 1: //1D

                        for (int i = 0; i < timeShape[0]; i++)
                            dt.Add(_getDateTimeValue(timeArr.getDouble(i), timeUnit));
                        break;
                }

            }
            else
            {
                object[] attList = GetGlobalAttributes();
                foreach (object att in attList)
                {
                    if (((ucar.nc2.Attribute)att).getName() == "field_date")
                        dt.Add(DateTime.Parse(((ucar.nc2.Attribute)att).getValue(0).ToString()));
                    if (((ucar.nc2.Attribute)att).getName() == "date_created")
                        dt.Add(DateTime.Parse(((ucar.nc2.Attribute)att).getValue(0).ToString()));
                    if (((ucar.nc2.Attribute)att).getName() == "date")
                        dt.Add(DateTime.Parse(((ucar.nc2.Attribute)att).getValue(0).ToString()));
                }
            }
            return dt;
        }

        private DateTime _getDateTimeValue(double dTimeFactor, string strTimeUnits)
        {
            strTimeUnits = strTimeUnits.ToLower();
            try
            {
                string unit = "years since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddYears((int)dTimeFactor);
                    return dt;
                }

                unit = "months since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddMonths((int)dTimeFactor);
                    return dt;
                }

                unit = "weeks since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddDays(dTimeFactor * 7);
                    return dt;
                }

                unit = "days since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddDays(dTimeFactor);
                    return dt;
                }

                unit = "hours since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddHours(dTimeFactor);
                    return dt;
                }

                unit = "hour since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddHours(dTimeFactor);
                    return dt.ToUniversalTime();
                }

                unit = "minutes since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddMinutes(dTimeFactor);
                    return dt;
                }

                unit = "seconds since ";
                if (strTimeUnits.IndexOf(unit) > -1)
                {
                    int pos = unit.Length;
                    strTimeUnits = strTimeUnits.Substring(pos, strTimeUnits.Length - pos).Trim();
                    DateTime dt = Convert.ToDateTime(strTimeUnits);
                    dt = dt.AddSeconds(dTimeFactor);
                    return dt;
                }

                return DateTime.MinValue; // unsupported time:units
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public double[] GetDoubleData(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, double delVal)
        {
            double[] resDouble = new double[xData.getSize()*yData.getSize()];

            int resCount = 0;
            for (int yCount = 0; yCount < yData.getSize(); yCount++)
                for (int xCount = 0; xCount < xData.getSize(); xCount++)
                {
                    ucar.ma2.Index xIndex = xData.getIndex();
                    ucar.ma2.Index yIndex = yData.getIndex();
                    ucar.ma2.Index resIndex = sourceData.getIndex();

                    int[] resShape = resIndex.getShape();
                    double resPoint = double.NaN;

                    if (xIndex.getSize() == resShape[0] & yIndex.getSize() == resShape[1])
                        resPoint = sourceData.getDouble(resIndex.set(xCount, yCount));
                    else
                        resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));

                    if (IsValueValid(attList, resPoint))
                        resDouble[resCount] = resPoint;
                    else
                        resDouble[resCount] = delVal;
                    
                    resCount++;
                }
            return resDouble;
        }

        public float[] GetFloatData(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, ucar.ma2.Array zData, java.util.List attList, float delVal, List<double> zValues)//, int timePosition, int zAxisPosition, int xAxisPosition, int yAxisPosition)
        {
            try
            {
                int resCount = 0;
                ucar.ma2.Index xIndex = xData.getIndex();
                ucar.ma2.Index yIndex = yData.getIndex();
                ucar.ma2.Index zIndex = zData.getIndex();
                ucar.ma2.Index resIndex = sourceData.getIndex();
                int[] resShape = resIndex.getShape();

                int xPosition = -1; int yPosition = -1; int zPosition = -1;

                for (int i = 0; i < resShape.Length; i++)
                {
                    if (resShape[i] == xData.getSize()) xPosition = i;
                    if (resShape[i] == yData.getSize()) yPosition = i;
                    if (resShape[i] == zData.getSize()) zPosition = i;
                }
                
                List<double> zDataList = new List<double>();
                for (int zLayerCount = 0; zLayerCount < (int)zData.getSize(); zLayerCount++)
                {
                    double zValue = zData.getDouble(zLayerCount);
                    zDataList.Add(zValue);
                }

                float[] resfloat = new float[resShape[xPosition]*resShape[yPosition]*zValues.Count];

                /*if (xData.getSize() == resShape[2] && yData.getSize() == resShape[1])
                {

                    for (int zCount = zValues.Count - 1; zCount >= 0; zCount--)
                    {
                        for (int yCount = 0; yCount < resShape[1]; yCount++)
                        {
                            for (int xCount = 0; xCount < resShape[2]; xCount++)
                            {
                                int dataCount = 0;

                                //find closest zValues
                                double closest = zDataList.Aggregate((x, y) => Math.Abs(x - zValues[zCount]) < Math.Abs(y - zValues[zCount]) ? x : y);
                                int zLayerClosestCount = zDataList.IndexOf(closest);

                                double resPoint = double.NaN;
                                resPoint = sourceData.getDouble(resIndex.set(zLayerClosestCount, yCount, xCount));
                                //zLayerCount - 1, yCount, xCount));

                                if (IsValueValid(attList, resPoint))
                                {
                                    resfloat[resCount] += (float)resPoint;
                                    dataCount++;
                                }
                                else
                                {
                                    if (resfloat[resCount] == 0.0) resfloat[resCount] = delVal;
                                    if (float.IsNaN(resfloat[resCount])) resfloat[resCount] = delVal;
                                }

                                if (resfloat[resCount] != delVal)
                                    resfloat[resCount] = resfloat[resCount] / dataCount;
                                resCount++;
                            }
                        }
                    }
                }

                else
                {*/
                     for (int zCount = zValues.Count - 1; zCount >= 0; zCount--)
                    {
                        for (int yCount = 0; yCount < resShape[yPosition]; yCount++)
                        {
                            for (int xCount = 0; xCount < resShape[xPosition]; xCount++)
                            {
                                int dataCount = 0;

                                //find closest zValues
                                double closest = zDataList.Aggregate((x, y) => Math.Abs(x - zValues[zCount]) < Math.Abs(y - zValues[zCount]) ? x : y);
                                int zLayerClosestCount = zDataList.IndexOf(closest);

                                double resPoint = double.NaN;

                                if (xPosition == 0 && yPosition == 1)
                                    resPoint = sourceData.getDouble(resIndex.set(xCount, yCount, zLayerClosestCount));
                                else if (xPosition == 1 && yPosition == 2)
                                    resPoint = sourceData.getDouble(resIndex.set(zLayerClosestCount, xCount, yCount));
                                else if (xPosition == 2 && yPosition == 1)
                                    resPoint = sourceData.getDouble(resIndex.set(zLayerClosestCount, yCount, xCount));
                                else if (xPosition == 1 && yPosition == 0)
                                    resPoint = sourceData.getDouble(resIndex.set(yCount, xCount, zLayerClosestCount));
                                //zLayerCount - 1, yCount, xCount));

                                if (IsValueValid(attList, resPoint))
                                {
                                    resfloat[resCount] += (float)resPoint;
                                    dataCount++;
                                }
                                else
                                {
                                    if (resfloat[resCount] == 0.0) resfloat[resCount] = delVal;
                                    if (float.IsNaN(resfloat[resCount])) resfloat[resCount] = delVal;
                                }

                                if (resfloat[resCount] != delVal)
                                    resfloat[resCount] = resfloat[resCount] / dataCount;
                                resCount++;
                            }
                        }
                    }
                //}
                
                return resfloat;
            }
            catch (Exception ex)
            {
                throw new Exception("Get Float Data Error: " + ex.Message);
            }
        }

        public float[] GetFloatDataInvertY(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, float delVal)
        {
            float[] resfloat = new float[sourceData.getSize()];

            int resCount = 0;
            ucar.ma2.Index xIndex = xData.getIndex();
            ucar.ma2.Index yIndex = yData.getIndex();
            ucar.ma2.Index resIndex = sourceData.getIndex();
            int[] resShape = resIndex.getShape();

            for (int yCount = resShape[0]-1; yCount >= 0; yCount--)
                for (int xCount = 0; xCount < resShape[1]; xCount++)
                {

                    double resPoint = double.NaN;

                    if (resShape.Length == 2)
                    {
                        resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));
                    }
                    else
                        throw new Exception("GetFloatData Error: not a 2D data array");

                    if (IsValueValid(attList, resPoint))
                        resfloat[resCount] = (float)resPoint;
                    else
                        resfloat[resCount] = delVal;
                    resCount++;
                }
            return resfloat;
        }

        public float[] GetFloatDataInvertX(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, float delVal)
        {
            float[] resfloat = new float[sourceData.getSize()];

            int resCount = 0;
            ucar.ma2.Index xIndex = xData.getIndex();
            ucar.ma2.Index yIndex = yData.getIndex();
            ucar.ma2.Index resIndex = sourceData.getIndex();
            int[] resShape = resIndex.getShape();

            for (int yCount = 0; yCount < resShape[0]; yCount++)
                for (int xCount = resShape[1] -1; xCount > 0; xCount--)
                {

                    double resPoint = double.NaN;

                    if (resShape.Length == 2)
                    {
                        resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));
                    }
                    else
                        throw new Exception("GetFloatData Error: not a 2D data array");

                    if (IsValueValid(attList, resPoint))
                        resfloat[resCount] = (float)resPoint;
                    else
                        resfloat[resCount] = delVal;
                    resCount++;
                }
            return resfloat;
        }

        public float[] GetFloatDataInvertXandY(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, float delVal)
        {
            float[] resfloat = new float[sourceData.getSize()];

            int resCount = 0;
            ucar.ma2.Index xIndex = xData.getIndex();
            ucar.ma2.Index yIndex = yData.getIndex();
            ucar.ma2.Index resIndex = sourceData.getIndex();
            int[] resShape = resIndex.getShape();

            for (int yCount = resShape[0] - 1; yCount > 0; yCount--)
                for (int xCount = resShape[1] - 1; xCount > 0; xCount--)
                {

                    double resPoint = double.NaN;

                    if (resShape.Length == 2)
                    {
                        resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));
                    }
                    else
                        throw new Exception("GetFloatData Error: not a 2D data array");

                    if (IsValueValid(attList, resPoint))
                        resfloat[resCount] = (float)resPoint;
                    else
                        resfloat[resCount] = delVal;
                    resCount++;
                }
            return resfloat;
        }

        public float[] GetFloatData(ucar.ma2.Array sourceData, ucar.ma2.Array xData, ucar.ma2.Array yData, java.util.List attList, float delVal, int xAxisPosition, int yAxisPosition)
        {
            float[] resfloat = new float[sourceData.getSize()];

            int resCount = 0;
            ucar.ma2.Index xIndex = xData.getIndex();
            ucar.ma2.Index yIndex = yData.getIndex();
            ucar.ma2.Index resIndex = sourceData.getIndex();
            int[] resShape = resIndex.getShape();

            int yCountMax = 0;
            int xCountMax = 0;

            if (resShape.Length == 2) //if it is a 2D file
            {
                if (xAxisPosition > yAxisPosition)
                {
                    yCountMax = resShape[0];
                    xCountMax = resShape[1];
                }
                else
                {
                    yCountMax = resShape[1];
                    xCountMax = resShape[0];
                }
            }
            else
            {
                if (yIndex.getShape()[0] > 1) // not a single value
                {
                    yCountMax = resShape[0];
                    xCountMax = 1;
                }
                else if (xIndex.getShape()[0] > 1)
                {
                    yCountMax = 1;
                    xCountMax = resShape[0];
                }
            }

            //if (xAxisPosition > yAxisPosition)
            //{
                for (int yCount = 0; yCount < yCountMax; yCount++)
                    for (int xCount = 0; xCount < xCountMax; xCount++)
                    {

                        double resPoint = double.NaN;

                        if (resShape.Length == 2)
                        {
                            resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));
                        }
                        else
                        { 
                            if (yIndex.getShape()[0] > 1) // not a single value
                            {
                                resPoint = sourceData.getDouble(resIndex.set(yCount));
                            }
                            else if (xIndex.getShape()[0] > 1)
                            {
                                resPoint = sourceData.getDouble(resIndex.set(xCount));
                            }
                        }
                            //throw new Exception("GetFloatData Error: not a 2D data array");

                        if (IsValueValid(attList, resPoint))
                            resfloat[resCount] = (float)resPoint;
                        else
                            resfloat[resCount] = delVal;
                        resCount++;
                    }
           // }
            /*else
            {
                for (int xCount = 0; xCount < xCountMax; xCount++)
                    for (int yCount = 0; yCount < yCountMax; yCount++)
                    {

                        double resPoint = double.NaN;

                        if (resShape.Length == 2)
                        {
                            resPoint = sourceData.getDouble(resIndex.set(yCount, xCount));
                        }
                        else
                            throw new Exception("GetFloatData Error: not a 2D data array");

                        if (IsValueValid(attList, resPoint))
                            resfloat[resCount] = (float)resPoint;
                        else
                            resfloat[resCount] = delVal;
                        resCount++;
                    }
            }*/

            return resfloat;
        }

        public float[] GetFloatData(ucar.ma2.Array sourceData, java.util.List attList, float delVal)
        {
            float[] resfloat = new float[sourceData.getSize()];

            int resCount = 0;

            for (int yCount = 0; yCount < sourceData.getSize(); yCount++)
            {
                ucar.ma2.Index resIndex = sourceData.getIndex();

                int[] resShape = resIndex.getShape();
                double resPoint = double.NaN;

                resPoint = sourceData.getDouble(resIndex.set(yCount));

                if (IsValueValid(attList, resPoint))
                    resfloat[resCount] = (float)resPoint;
                else
                    resfloat[resCount] = delVal;
                resCount++;
            }
            return resfloat;
        }

        public ucar.ma2.Array Get2DVariableData(object variable, CommandSettings commandSettings)
        {
            int[] varShape = ((Variable)variable).getShape();
            ucar.ma2.Array data = null;
            switch (varShape.Length)
            {
                case 0: //0D data, unable to extract
                    throw new Exception("Unrecognised 2D data shape");

                case 1: //1D data, unable to extract
                    throw new Exception("Unrecognised 2D data shape");

                case 2: //2D data
                    java.util.List varDims = ((Variable)variable).getDimensions();
                    int zPosition = -1, xAxisPosition = -1, yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                        {
                            if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        }
                        else if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                        {
                            if (commandSettings.TimeAxisDimensionName == dimName) zPosition = i;
                        }
                        //removed to accomodate nc data without time
                        /*else
                            throw new Exception("Unrecognised 2D dimensions");*/
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    int[] origin = new int[varDims.size()];
                    int[] size = new int[varDims.size()];

                    string[] sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.XLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.YLayer;
                        }
                        if (i == zPosition)
                        {
                            if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                            {
                                sectionSpec[i] = commandSettings.ZLayer.ToString();
                            }
                            else
                            {
                                sectionSpec[i] = commandSettings.TimeLayer.ToString();
                            }
                        }
                    }
                    string sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(',')).reduce();
                    break;

                case 3: //3D data
                    varDims = ((Variable)variable).getDimensions();
                    zPosition = -1; xAxisPosition = -1; yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                        {
                            if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        }
                        if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                        {
                            if (commandSettings.TimeAxisDimensionName == dimName) zPosition = i;
                        }
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

					java.util.List ranges = new java.util.ArrayList();
					ranges.add(null); ranges.add(null); ranges.add(null);
                    if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                    {
                        ranges.set(zPosition, new ucar.ma2.Range(commandSettings.ZLayer, commandSettings.ZLayer));
                    }
                    if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                    {
                        ranges.set(zPosition, new ucar.ma2.Range(commandSettings.TimeLayer, commandSettings.TimeLayer));
                        //ranges.set(zPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.TimeLayer.Split(':')[0]), Convert.ToInt32(commandSettings.TimeLayer.Split(':')[1])));
                    }
                    //for MIKE, always go for bottom left onwards
                    ucar.nc2.Variable xVariable = (ucar.nc2.Variable)GetVariable(commandSettings.XAxisName);
                    ranges.set(xAxisPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.XLayer.Split(':')[0]), Convert.ToInt32(commandSettings.XLayer.Split(':')[1])));
                    ucar.nc2.Variable yVariable = (ucar.nc2.Variable)GetVariable(commandSettings.YAxisName);
                    ranges.set(yAxisPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.YLayer.Split(':')[0]), Convert.ToInt32(commandSettings.YLayer.Split(':')[1])));
					data = ((Variable)variable).read(ranges).reduce();
                    break;

                case 4: //4D data
                    varDims = ((Variable)variable).getDimensions();
                    zPosition = -1; xAxisPosition = -1; yAxisPosition = -1;
                    int timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        if (commandSettings.TimeAxisDimensionName == dimName) timePosition = i;
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.XLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.YLayer;
                        }
                        if (i == zPosition)
                        {
                            sectionSpec[i] = commandSettings.ZLayer.ToString();
                        }
                        if (i == timePosition)
                        {
                            sectionSpec[i] = commandSettings.TimeLayer.ToString();
                        }
                    }
                    sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(',')).reduce();
                    break;

                default:
                    throw new Exception("Unrecognised 2D data shape");
            }
            return data;
        }

        public ucar.ma2.Array Get1DVariableData(object variable, CommandSettings commandSettings)
        {
            int[] varShape = ((Variable)variable).getShape();
            ucar.ma2.Array data = null;
            switch (varShape.Length)
            {
                case 0: //0D data, unable to extract
                    throw new Exception("Unrecognised 1D data shape");

                case 1: //1D data, unable to extract
                    java.util.List varDims = ((Variable)variable).getDimensions();
                    int xAxisPosition = -1, yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    int[] origin = new int[varDims.size()];
                    int[] size = new int[varDims.size()];

                    string[] sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.XLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.YLayer;
                        }
                    }
                    string sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(',')).reduce();
                    break;

                case 2: //2D data
                    varDims = ((Variable)variable).getDimensions();
                    int zPosition = -1;
                    xAxisPosition = -1; 
                    yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                        {
                            if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        }
                        else if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                        {
                            if (commandSettings.TimeAxisDimensionName == dimName) zPosition = i;
                        }
                        else
                            throw new Exception("Unrecognised 2D dimensions");
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    origin = new int[varDims.size()];
                    size = new int[varDims.size()];

                    sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.XLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.YLayer;
                        }
                        if (i == zPosition)
                        {
                            if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                            {
                                sectionSpec[i] = commandSettings.ZLayer.ToString();
                            }
                            else
                            {
                                sectionSpec[i] = commandSettings.TimeLayer.ToString();
                            }
                        }
                    }
                    sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(',')).reduce();
                    break;

                case 3: //3D data
                    varDims = ((Variable)variable).getDimensions();
                    zPosition = -1; xAxisPosition = -1; yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                        {
                            if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        }
                        if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                        {
                            if (commandSettings.TimeAxisDimensionName == dimName) zPosition = i;
                        }
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    java.util.List ranges = new java.util.ArrayList();
                    ranges.add(null); ranges.add(null); ranges.add(null);
                    if (!String.IsNullOrEmpty(commandSettings.ZAxisDimensionName))
                    {
                        ranges.set(zPosition, new ucar.ma2.Range(commandSettings.ZLayer, commandSettings.ZLayer));
                    }
                    if (!String.IsNullOrEmpty(commandSettings.TimeAxisDimensionName))
                    {
                        ranges.set(zPosition, new ucar.ma2.Range(commandSettings.TimeLayer, commandSettings.TimeLayer));
                        //ranges.set(zPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.TimeLayer.Split(':')[0]), Convert.ToInt32(commandSettings.TimeLayer.Split(':')[1])));
                    }
                    //for MIKE, always go for bottom left onwards
                    ucar.nc2.Variable xVariable = (ucar.nc2.Variable)GetVariable(commandSettings.XAxisName);
                    ranges.set(xAxisPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.XLayer.Split(':')[0]), Convert.ToInt32(commandSettings.XLayer.Split(':')[1])));
                    ucar.nc2.Variable yVariable = (ucar.nc2.Variable)GetVariable(commandSettings.YAxisName);
                    ranges.set(yAxisPosition, new ucar.ma2.Range(Convert.ToInt32(commandSettings.YLayer.Split(':')[0]), Convert.ToInt32(commandSettings.YLayer.Split(':')[1])));
                    data = ((Variable)variable).read(ranges).reduce();
                    break;

                case 4: //4D data
                    varDims = ((Variable)variable).getDimensions();
                    zPosition = -1; xAxisPosition = -1; yAxisPosition = -1;
                    int timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (commandSettings.ZAxisDimensionName == dimName) zPosition = i;
                        if (commandSettings.TimeAxisDimensionName == dimName) timePosition = i;
                        if (commandSettings.XAxisDimensionName == dimName) xAxisPosition = i;
                        if (commandSettings.YAxisDimensionName == dimName) yAxisPosition = i;
                    }

                    sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.XLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = commandSettings.YLayer;
                        }
                        if (i == zPosition)
                        {
                            sectionSpec[i] = commandSettings.ZLayer.ToString();
                        }
                        if (i == timePosition)
                        {
                            sectionSpec[i] = commandSettings.TimeLayer.ToString();
                        }
                    }
                    sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(',')).reduce();
                    break;

                default:
                    throw new Exception("Unrecognised 2D data shape");
            }
            return data;
        }

        public List<ucar.ma2.Array> Get2DVariableDataTrans(object variable, int zLayer, string zAxisName, int timeLayer, string timeAxisName, string xAxisName, string yAxisName, List<TransectPoint> transectPoints, int transPointSpaceStepsNum)
        {
            int[] varShape = ((Variable)variable).getShape();
            ucar.ma2.Array data = null;

            //interpolate transect points according to the space steps
            double maxDistance = Math.Sqrt(Math.Pow((transectPoints[0].X - transectPoints[transectPoints.Count - 1].X), 2)
                + Math.Pow((transectPoints[0].Y - transectPoints[transectPoints.Count - 1].Y), 2));
            double aveDistance = maxDistance / transPointSpaceStepsNum;
            double m = (transectPoints[transectPoints.Count - 1].Y - transectPoints[0].Y) / (transectPoints[transectPoints.Count - 1].X - transectPoints[0].X);
            double c = transectPoints[0].Y - m*transectPoints[0].X;

            List<TransectPoint> interpolatedTP = new List<TransectPoint>();
            for (int i = 0; i <= transPointSpaceStepsNum; i++)
            {
                if (i == 0)
                {
                    interpolatedTP.Add(transectPoints[0]);
                }
                else
                {
                    TransectPoint newPoint = new TransectPoint();
                    if (double.IsInfinity(m)) // vertical straight line
                    {
                        newPoint.X = transectPoints[0].X;
                        if (transectPoints[0].Y <= transectPoints[transectPoints.Count -1].Y)
                            newPoint.Y = transectPoints[0].Y + i * aveDistance;
                        else
                            newPoint.Y = transectPoints[0].Y - i * aveDistance;
                    }
                    else
                    {
                        if (transectPoints[0].X <= transectPoints[transectPoints.Count - 1].X)
                            newPoint.X = transectPoints[0].X + (i * aveDistance / Math.Sqrt(1 + Math.Pow(m, 2)));
                        else
                            newPoint.X = transectPoints[0].X - (i * aveDistance / Math.Sqrt(1 + Math.Pow(m, 2)));
                        newPoint.Y = m * newPoint.X + c;
                    }
                    interpolatedTP.Add(newPoint);
                }
            }

            List<ucar.ma2.Array> dataProfiles = new List<ucar.ma2.Array>();
            switch (varShape.Length)
            {
                case 0: //0D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 1: //1D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 2: //2D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 3: //3D data, not including time
                    java.util.List varDims = ((Variable)variable).getDimensions();
                    java.util.List varAtt = ((Variable)variable).getAttributes();
                    int zPosition = -1, xAxisPosition = -1, yAxisPosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (String.IsNullOrEmpty(zAxisName))
                            throw new Exception("Unrecognised 3D data shape");
                        if (zAxisName == dimName) zPosition = i;
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }
                    
                    // get x and y data then search within them for transect lines
                    object xVar = GetVariable(xAxisName);
                    ucar.ma2.Array xData = GetAllVariableData(xVar);
                    object yVar = GetVariable(yAxisName);
                    ucar.ma2.Array yData = GetAllVariableData(yVar);
                    
                    bool hasFoundData = false;
                    for (int transPt = 0; transPt < interpolatedTP.Count; transPt++)
                    {
                        hasFoundData = false;
                        List<ucar.ma2.Array> transectPointData = new List<ucar.ma2.Array>();

                        for (int yCount = 1; yCount < yData.getSize(); yCount++)
                            for (int xCount = 1; xCount < xData.getSize(); xCount++)
                            {
                                if (!hasFoundData)
                                {
                                    double xPoint = xData.getDouble(xCount);
                                    double xPointPrev = xData.getDouble(xCount - 1);
                                    double yPoint = yData.getDouble(yCount);
                                    double yPointPrev = yData.getDouble(yCount - 1);

                                    java.util.List ranges = new java.util.ArrayList();
                                    ranges.add(null); ranges.add(null); ranges.add(null);

                                    int sameLineTestCount = 0;
                                    int sameIntervalTestCount = 0;

                                    if (xCount != 0 && yCount != 0)
                                    {
                                        if ((xPointPrev - xPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].X >= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X <= xPoint) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X == xPoint) sameIntervalTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].X <= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X >= xPoint) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X == xPoint) sameIntervalTestCount++;
                                        }

                                        if ((yPointPrev - yPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].Y >= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y <= yPoint) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y == yPoint) sameIntervalTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].Y <= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y >= yPoint) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y == yPoint) sameIntervalTestCount++;
                                        }
                                    }
                                    else
                                    {
                                        if (interpolatedTP[transPt].X == xPoint) sameIntervalTestCount++;
                                        if (interpolatedTP[transPt].Y == yPoint) sameIntervalTestCount++;
                                    }
                                    if (sameLineTestCount == 4 || sameIntervalTestCount == 2)
                                    {
                                        ranges.set(zPosition, new ucar.ma2.Range(0, ((Dimension)varDims.get(zPosition)).getLength() - 1)); //get all z data
                                        ranges.set(xAxisPosition, new ucar.ma2.Range(xCount, xCount));
                                        ranges.set(yAxisPosition, new ucar.ma2.Range(yCount, yCount));

                                        java.util.List prevRanges = new java.util.ArrayList();
                                        prevRanges.add(null); prevRanges.add(null); prevRanges.add(null);
                                        prevRanges.set(zPosition, new ucar.ma2.Range(0, ((Dimension)varDims.get(zPosition)).getLength() - 1)); //get all z data
                                        prevRanges.set(xAxisPosition, new ucar.ma2.Range(xCount - 1, xCount - 1));
                                        prevRanges.set(yAxisPosition, new ucar.ma2.Range(yCount - 1, yCount - 1));

                                        data = ((Variable)variable).read(ranges).reduce();
                                        ucar.ma2.Array prevData = ((Variable)variable).read(prevRanges).reduce();
                                        ucar.ma2.ArrayDouble interpolatedData = new ucar.ma2.ArrayDouble.D1((int)data.getSize());

                                        //interpolation
                                        //y = y0 + (y1-y0)*((x-x0)/(x1-x0))
                                        double interpolatedValue = 0;

                                        for (int i = 0; i < data.getSize(); i++)
                                        {
                                            /*if (IsValueValid(varAtt, data.getDouble(i)) && IsValueValid(varAtt, prevData.getDouble(i)))
                                            {
                                                interpolatedValue = data.getDouble(i) +
                                                    (prevData.getDouble(i) - data.getDouble(i)) * ((interpolatedTP[transPt].X - xPoint) / (xPointPrev - xPoint));
                                            }
                                            else*/
                                            interpolatedValue = data.getDouble(i);
                                            interpolatedData.setDouble(i, interpolatedValue);
                                        }
                                        transectPointData.Add(interpolatedData);
                                        hasFoundData = true;
                                    }

                                    //check if there are many transectPointdata
                                    if (transectPointData.Count > 1)
                                    {
                                        List<double> sumList = new List<double>();
                                        //average all values in each transectPointData and then add to dataprofiles
                                        for (int i = 0; i < transectPointData.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                for (int j = 0; j < transectPointData[i].getSize(); j++)
                                                {
                                                    sumList.Add(transectPointData[i].getDouble(j));
                                                }
                                            }
                                            else
                                            {
                                                for (int j = 0; j < transectPointData[i].getSize(); j++)
                                                {
                                                    sumList[i] += transectPointData[i].getDouble(j);
                                                }
                                            }
                                        }
                                        ucar.ma2.ArrayDouble aveData = new ucar.ma2.ArrayDouble.D1(sumList.Count);
                                        for (int i = 0; i < sumList.Count; i++)
                                        {
                                            double value = sumList[i] / transectPointData.Count;
                                            aveData.setDouble(i, value);
                                        }
                                        dataProfiles.Add(aveData);
                                    }
                                    else if (transectPointData.Count > 0)
                                    {
                                        dataProfiles.Add(transectPointData[0]);
                                    }
                                }
                            }
                    }
                    break;

                case 4: //4D data
                    varDims = ((Variable)variable).getDimensions();
                    varAtt = ((Variable)variable).getAttributes();
                    zPosition = -1; xAxisPosition = -1; yAxisPosition = -1;
                    int timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (zAxisName == dimName) zPosition = i;
                        if (timeAxisName == dimName) timePosition = i;
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }

                    // get x and y data then search within them for transect lines
                    xVar = GetVariable(xAxisName);
                    xData = GetAllVariableData(xVar);
                    yVar = GetVariable(yAxisName);
                    yData = GetAllVariableData(yVar);

                    for (int transPt = 0; transPt < interpolatedTP.Count; transPt++)
                    {
                        hasFoundData = false;
                        List<ucar.ma2.Array> transectPointData = new List<ucar.ma2.Array>();

                        for (int yCount = 0; yCount < yData.getSize(); yCount++)
                            for (int xCount = 0; xCount < xData.getSize(); xCount++)
                            {
                                if (!hasFoundData)
                                {
                                    double xPoint = xData.getDouble(xCount);
                                    double xPointPrev = double.MinValue;
                                    if (xCount > 0)
                                        xPointPrev = xData.getDouble(xCount - 1);

                                    double yPoint = yData.getDouble(yCount);
                                    double yPointPrev = double.MinValue;
                                    if (yCount > 0)
                                        yPointPrev = yData.getDouble(yCount - 1);

                                    java.util.List ranges = new java.util.ArrayList();
                                    ranges.add(null); ranges.add(null); ranges.add(null); ranges.add(null);

                                    int sameLineTestCount = 0;
                                    int sameIntervalTestCount = 0;

                                    if (xCount != 0 && yCount != 0)
                                    {
                                        //increasing X and Y
                                        if ((xPointPrev - xPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].X >= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X <= xPoint) sameLineTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].X <= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X >= xPoint) sameLineTestCount++;
                                        }

                                        if ((yPointPrev - yPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].Y >= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y <= yPoint) sameLineTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].Y <= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y >= yPoint) sameLineTestCount++;
                                        }
                                    }
                                    else if (xCount != 0 && yCount == 0)
                                    {
                                        //increasing X and Y
                                        if ((xPointPrev - xPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].X >= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X <= xPoint) sameLineTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].X <= xPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].X >= xPoint) sameLineTestCount++;
                                        }

                                            //sameLineTestCount = sameLineTestCount + 2; //add 2 counts to yCount as there is only 1 y value
                                    }
                                    else if (xCount == 0 && yCount != 0)
                                    {

                                        if ((yPointPrev - yPoint) < 0)
                                        {
                                            if (interpolatedTP[transPt].Y >= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y <= yPoint) sameLineTestCount++;
                                        }
                                        else
                                        {
                                            if (interpolatedTP[transPt].Y <= yPointPrev) sameLineTestCount++;
                                            if (interpolatedTP[transPt].Y >= yPoint) sameLineTestCount++;
                                        }

                                        //sameLineTestCount = sameLineTestCount + 2; //add 2 counts to xCount as there is only 1 x value
                                    }
                                    else
                                    {
                                        if (interpolatedTP[transPt].X == xPoint) sameIntervalTestCount++;
                                        if (interpolatedTP[transPt].Y == yPoint) sameIntervalTestCount++;
                                    }
                                    if (sameLineTestCount == 4 || sameIntervalTestCount == 2)
                                    {
                                        ranges.set(zPosition, new ucar.ma2.Range(0, ((Dimension)varDims.get(zPosition)).getLength() - 1)); //get all z data
                                        ranges.set(timePosition, new ucar.ma2.Range(timeLayer, timeLayer)); //get 1 timestep data
                                        ranges.set(xAxisPosition, new ucar.ma2.Range(xCount, xCount));
                                        ranges.set(yAxisPosition, new ucar.ma2.Range(yCount, yCount));

                                        java.util.List prevRanges = new java.util.ArrayList();
                                        prevRanges.add(null); prevRanges.add(null); prevRanges.add(null); prevRanges.add(null);
                                        prevRanges.set(zPosition, new ucar.ma2.Range(0, ((Dimension)varDims.get(zPosition)).getLength() - 1)); //get all z data
                                        prevRanges.set(timePosition, new ucar.ma2.Range(timeLayer, timeLayer)); //get 1 timestep data
                                        prevRanges.set(xAxisPosition, new ucar.ma2.Range(xCount, xCount));
                                        prevRanges.set(yAxisPosition, new ucar.ma2.Range(yCount, yCount));

                                        data = ((Variable)variable).read(ranges).reduce();
                                        ucar.ma2.Array prevData = ((Variable)variable).read(prevRanges).reduce();
                                        ucar.ma2.ArrayDouble interpolatedData = new ucar.ma2.ArrayDouble.D1((int)data.getSize());

                                        //interpolation
                                        //y = y0 + (y1-y0)*((x-x0)/(x1-x0))
                                        double interpolatedValue = 0;
                                        for (int i = 0; i < data.getSize(); i++)
                                        {
                                            /*if (IsValueValid(varAtt, data.getDouble(i)) && IsValueValid(varAtt, prevData.getDouble(i)))
                                            {
                                                interpolatedValue = data.getDouble(i) +
                                                    (prevData.getDouble(i) - data.getDouble(i)) * ((interpolatedTP[transPt].X - xPoint) / (xPointPrev - xPoint));
                                            }
                                            else*/ 
                                                interpolatedValue = data.getDouble(i); 
                                            interpolatedData.setDouble(i, interpolatedValue);
                                        }
                                        transectPointData.Add(interpolatedData);
                                        hasFoundData = true;
                                    }

                                    //check if there are many transectPointdata
                                    if (transectPointData.Count > 1)
                                    {
                                        List<double> sumList = new List<double>();
                                        //average all values in each transectPointData and then add to dataprofiles
                                        for (int i = 0; i < transectPointData.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                for (int j = 0; j < transectPointData[i].getSize(); j++)
                                                {
                                                    sumList.Add(transectPointData[i].getDouble(j));
                                                }
                                            }
                                            else
                                            {
                                                for (int j = 0; j < transectPointData[i].getSize(); j++)
                                                {
                                                    sumList[i] += transectPointData[i].getDouble(j);
                                                }
                                            }
                                        }
                                        ucar.ma2.ArrayDouble aveData = new ucar.ma2.ArrayDouble.D1(sumList.Count);
                                        for (int i = 0; i < sumList.Count; i++)
                                        {
                                            double value = sumList[i] / transectPointData.Count;
                                            aveData.setDouble(i, value);
                                        }
                                        dataProfiles.Add(aveData);
                                    }
                                    else if (transectPointData.Count > 0)
                                    {
                                        dataProfiles.Add(transectPointData[0]);
                                    }
                                }
                            }
                    }

                    break;

                default:
                    throw new Exception("Unrecognised 3D data shape");
            }
            return dataProfiles;
        }

        public ucar.ma2.Array Get3DVariableData(object variable, string zAxisName, int timeLayer, string timeAxisName, string xAxisName, string yAxisName)//, out int timePosition, out int zAxisPosition, out int yAxisPosition, out int xAxisPosition)
        {
            int[] varShape = ((Variable)variable).getShape();
            ucar.ma2.Array data = null;
            switch (varShape.Length)
            {
                case 0: //0D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 1: //1D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 2: //2D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 3: //3D data
                    java.util.List varDims = ((Variable)variable).getDimensions();
                    int zAxisPosition = -1, xAxisPosition = -1, yAxisPosition = -1, timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(zAxisName))
                        {
                            if (zAxisName == dimName) zAxisPosition = i;
                        }
                        else
                            throw new Exception("Unrecognised 3D dimensions");
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }

                    int[] origin = new int[varDims.size()];
                    int[] size = new int[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        origin[i] = 0;
                        size[i] = ((Dimension)varDims.get(i)).getLength();
                    }
                    data = ((Variable)variable).read(origin, size);
                    break;

                case 4: //4D data
                    varDims = ((Variable)variable).getDimensions();
                    zAxisPosition = -1; xAxisPosition = -1; yAxisPosition = -1; timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (zAxisName == dimName) zAxisPosition = i;
                        if (timeAxisName == dimName) timePosition = i;
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }

                    origin = new int[varDims.size()];
                    size = new int[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == timePosition)
                        {
                            origin[i] = timeLayer;
                            size[i] = 1;
                        }
                        else
                        {
                            origin[i] = 0;
                            size[i] = ((Dimension)varDims.get(i)).getLength();
                        }
                    }
                    data = ((Variable)variable).read(origin, size).reduce(timePosition);
                    break;

                default:
                    throw new Exception("Unrecognised 3D data shape");
            }
            return data;
        }

        public ucar.ma2.Array Get3DVariableData(object variable, string zAxisName, int timeLayer, string timeAxisName, string xLayer, string xAxisName, string yLayer, string yAxisName)
        {
            int[] varShape = ((Variable)variable).getShape();
            ucar.ma2.Array data = null;
            switch (varShape.Length)
            {
                case 0: //0D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 1: //1D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 2: //2D data, unable to extract
                    throw new Exception("Unrecognised 3D data shape");

                case 3: //3D data
                    java.util.List varDims = ((Variable)variable).getDimensions();
                    int zAxisPosition = -1, xAxisPosition = -1, yAxisPosition = -1, timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (!String.IsNullOrEmpty(zAxisName))
                        {
                            if (zAxisName == dimName) zAxisPosition = i;
                        }
                        else
                            throw new Exception("Unrecognised 3D dimensions");
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }

                    string[] sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = xLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = yLayer;
                        }
                        if (i == zAxisPosition)
                        {
                            sectionSpec[i] = "0:" + (((Dimension)varDims.get(i)).getLength()-1).ToString();
                        }
                    }
                    string sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr.TrimEnd(','));
                    break;

                case 4: //4D data
                    varDims = ((Variable)variable).getDimensions();
                    zAxisPosition = -1; xAxisPosition = -1; yAxisPosition = -1; timePosition = -1;
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        string dimName = ((Dimension)varDims.get(i)).getName();
                        if (zAxisName == dimName) zAxisPosition = i;
                        if (timeAxisName == dimName) timePosition = i;
                        if (xAxisName == dimName) xAxisPosition = i;
                        if (yAxisName == dimName) yAxisPosition = i;
                    }

                    sectionSpec = new string[varDims.size()];
                    for (int i = 0; i < varDims.size(); i++)
                    {
                        if (i == xAxisPosition)
                        {
                            sectionSpec[i] = xLayer;
                        }
                        if (i == yAxisPosition)
                        {
                            sectionSpec[i] = yLayer;
                        }
                        if (i == zAxisPosition)
                        {
                            sectionSpec[i] = "0:" + (((Dimension)varDims.get(i)).getLength()-1).ToString();
                        }
                        if (i == timePosition)
                        {
                            sectionSpec[i] = timeLayer.ToString();
                        }
                    }
                    sectionSpecStr = string.Empty;
                    foreach (string secSpecStr in sectionSpec)
                    {
                        sectionSpecStr += secSpecStr + ",";
                    }
                    data = ((Variable)variable).read(sectionSpecStr).reduce(timePosition);
                    break;

                default:
                    throw new Exception("Unrecognised 3D data shape");
            }
            return data;
        }

        public int GetDimensionPosition(string dimName, java.util.List varDims)
        {
            int result = int.MinValue;
            for (int i = 0; i < varDims.size(); i++)
            {
                string varDimName = ((Dimension)varDims.get(i)).getName();
                if (varDimName == dimName) result = i;
            }
            return result;
        }

        public ucar.ma2.Array ProcessedVariableData(object variable, ucar.ma2.Array variableArray)
        {
            Variable var = (Variable)variable;
            int[] varShape = var.getShape();

            ucar.ma2.Array processedArr = ucar.ma2.Array.factory(ucar.ma2.DataType.DOUBLE, variableArray.getShape());
            ucar.ma2.IndexIterator indexItVarArr = variableArray.getIndexIterator();
            ucar.ma2.IndexIterator indexItProArr = processedArr.getIndexIterator();
            java.util.List varAtts = var.getAttributes();

            while (indexItVarArr.hasNext() && indexItProArr.hasNext())
            {
                double currentData = indexItVarArr.getDoubleNext();
                indexItProArr.setDoubleNext(AggregateValue(varAtts, currentData));     
            }

            return processedArr;
        }

        public double AggregateValue(java.util.List varAttributes, double sourceNumber)
        {
            double result = sourceNumber;
            for (int attCount = 0; attCount < varAttributes.size(); attCount++)
            {
                ucar.nc2.Attribute varAtt = (ucar.nc2.Attribute)varAttributes.get(attCount);
                string attName = varAtt.getName();
                if (attName == "scale_factor")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    result = sourceNumber * attVal.doubleValue();
                    sourceNumber = result;
                    break;
                }
            }

            for (int attCount = 0; attCount < varAttributes.size(); attCount++)
            {
                ucar.nc2.Attribute varAtt = (ucar.nc2.Attribute)varAttributes.get(attCount);
                string attName = varAtt.getName();
                if (attName == "add_offset")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                        result = sourceNumber + attVal.doubleValue();
                        sourceNumber = result;
                        break;
                }
            }

            return result;
        }

        public bool IsValueValid(java.util.List varAttributes, double aggSourceNumber)
        {
            double fillvalue = double.NaN, missingValue = double.NaN, validMin = double.NaN, validMax = double.NaN;
            for (int attCount = 0; attCount < varAttributes.size(); attCount++)
            {
                ucar.nc2.Attribute varAtt = (ucar.nc2.Attribute)varAttributes.get(attCount);
                string attName = varAtt.getName();
                if (attName == "_FillValue")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    fillvalue = AggregateValue(varAttributes, attVal.doubleValue());
                }
                else if (attName == "missing_value")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    missingValue = AggregateValue(varAttributes, attVal.doubleValue());
                }
                else if (attName == "valid_min")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    validMin = AggregateValue(varAttributes, attVal.doubleValue());
                }
                else if (attName == "valid_max")
                {
                    java.lang.Number attVal = (java.lang.Number)varAtt.getValue(0);
                    validMax = AggregateValue(varAttributes, attVal.doubleValue());
                }
            }

            if (aggSourceNumber == fillvalue) return false;
            else if (aggSourceNumber == missingValue) return false;
            else if (aggSourceNumber <= validMin) return false;
            else if (aggSourceNumber >= validMax) return false;
            else if (double.IsNaN(aggSourceNumber)) return false;
            else return true;
        }

    }

    public class CancelTask : ucar.nc2.util.CancelTask
    {

        public bool isCancel()
        {
            throw new NotImplementedException();
        }

        public void setError(string str)
        {
            throw new NotImplementedException();
        }

        public void setProgress(string str, int i)
        {
            throw new NotImplementedException();
        }
    }

    public class CFMapping
    {
        private List<DHICFMapping> _dhiCFMapping;
        private standard_name_table _cfStandardTable;

        /// <summary>
        /// Auto map DHI's EUM with CF Standard. Uses Levenshtein Distance method to search in CF standard names.
        /// Increase fuzzyCoefficient (0.0 to 1.0) to increase "strictness" in search. 
        /// </summary>
        /// <param name="fuzzyCoefficient"></param>
        /// <returns>Count of number of mapped CF standard names</returns>
        public void AutoMapDHICFStandard(double fuzzyCoefficient)
        {
            _GetDHIEUM();
            _GetCFStandard();

            foreach (DHICFMapping existingMap in _dhiCFMapping)
            {
                existingMap.ConversionDateTime = DateTime.Now;
                existingMap.ConversionMachineUserName = Environment.UserName + "@" + Environment.MachineName;
                int cfmatchfoundCount = 0;

                foreach (DHICFEntry dhiCfEntry in existingMap.DHICFEntries)
                {
                    bool hasFoundCFEntry = false;
                    string searchStr = dhiCfEntry.EUMItemDesc;

                    foreach (standard_name_tableEntry cfStandardEntry in _cfStandardTable.entry)
                    {
                        if (_CompareIDs(searchStr, cfStandardEntry.id, dhiCfEntry.CFStandardName, fuzzyCoefficient))
                        {
                            dhiCfEntry.CFStandardName = cfStandardEntry.id;
                            dhiCfEntry.CFStandardDesc = cfStandardEntry.description;
                            dhiCfEntry.CFStandardUnit = cfStandardEntry.canonical_units;
                            hasFoundCFEntry = true;
                            cfmatchfoundCount++;
                        }
                    }

                    //search in CF Alias if not found in entry
                    if (!hasFoundCFEntry)
                    {
                        foreach (standard_name_tableAlias cfStandardAlias in _cfStandardTable.alias)
                        {
                            if (_CompareIDs(searchStr, cfStandardAlias.id, dhiCfEntry.CFStandardName, fuzzyCoefficient))
                            {
                                dhiCfEntry.CFStandardName = cfStandardAlias.entry_id;
                                dhiCfEntry.CFStandardAlias = cfStandardAlias.id;
                                foreach (standard_name_tableEntry cfStandardEntry in _cfStandardTable.entry)
                                {
                                    if (cfStandardEntry.id == cfStandardAlias.entry_id)
                                    {
                                        dhiCfEntry.CFStandardDesc = cfStandardEntry.description;
                                        dhiCfEntry.CFStandardUnit = cfStandardEntry.canonical_units;
                                        cfmatchfoundCount++;
                                    }
                                }
                            }
                        }
                    }
                }

                existingMap.CFStandardNamesMapped = cfmatchfoundCount;
                XmlSerialiser xmlSerialier = new XmlSerialiser();
                string xmlData = xmlSerialier.SerializeObject(existingMap, typeof(DHICFMapping));
                string saveFile = @"C:\Data\scm\SolSoftware\Generic\DHI.Generic.NetCDF\CF\dhi_cf_map_" + existingMap.MikeEUMFilterName + ".xml";
                xmlSerialier.WriteXMLFile(xmlData, saveFile);
            }
        }

        /// <summary>
        /// Finds and returns a list of closest CF Standards from DHI EUM
        /// </summary>
        /// <param name="dhiEumFilterType"></param>
        /// <param name="dhiEumItemDesc"></param>
        /// <param name="fuzzyCoefficient"></param>
        /// <returns></returns>
        public List<DHICFEntry> FindClosestCFStandards(string dhiEumItemDesc, double fuzzyCoefficient)
        {
            _GetAllDHIEUM();
            _GetCFStandard();
            string undefined = "Undefined";
            List<DHICFEntry> undefinedEntries = new List<DHICFEntry>();
            List<DHICFEntry> closestEntries = new List<DHICFEntry>();
            foreach (DHICFMapping existingMap in _dhiCFMapping)
            {
                foreach (DHICFEntry existingEntry in existingMap.DHICFEntries)
                {
                    if (existingEntry.EUMItemDesc.ToLower() == dhiEumItemDesc.ToLower())
                    {
                        if (dhiEumItemDesc.Split(' ').Length > 1)
                        {
                            string[] itemNameBreakDown = dhiEumItemDesc.Split(' ');
                            foreach (string itemNameBD in itemNameBreakDown)
                            {
                                foreach (standard_name_tableEntry cfStandard in _cfStandardTable.entry)
                                {
                                    int searchWordCount = Search(itemNameBD, new List<string> { cfStandard.id }, fuzzyCoefficient);
                                    if (searchWordCount > 0)
                                    {
                                        DHICFEntry newEntry = _makeNewEntry(existingEntry, cfStandard);

                                        int existingCount = 0;
                                        foreach (DHICFEntry existingClosestEntry in closestEntries)
                                        {
                                            if (existingClosestEntry.CFStandardName == newEntry.CFStandardName)
                                            {
                                                existingCount++;
                                            }
                                        }

                                        if (existingCount == 0)
                                        {
                                            closestEntries.Add(newEntry);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (standard_name_tableEntry cfStandard in _cfStandardTable.entry)
                            {
                                int searchWordCount = Search(dhiEumItemDesc, new List<string> { cfStandard.id }, fuzzyCoefficient);
                                if (searchWordCount > 0)
                                {
                                    DHICFEntry newEntry = _makeNewEntry(existingEntry, cfStandard);

                                    int existingCount = 0;
                                    foreach (DHICFEntry existingClosestEntry in closestEntries)
                                    {
                                        if (existingClosestEntry.CFStandardName == newEntry.CFStandardName)
                                        {
                                            existingCount++;
                                        }
                                    }
                                    if (existingCount == 0)
                                    {
                                        closestEntries.Add(newEntry);
                                    }
                                }
                            }
                        }
                    }
                    else if (existingEntry.EUMItemDesc.ToLower() == undefined.ToLower())
                    {
                        DHICFEntry newEntry = new DHICFEntry();
                        newEntry.EUMItemDesc = existingEntry.EUMItemDesc;
                        newEntry.EUMItemKey = existingEntry.EUMItemKey;
                        newEntry.EUMItemUnitDesc = existingEntry.EUMItemUnitDesc;
                        newEntry.EUMItemUnitKeys = existingEntry.EUMItemUnitKeys;
                        newEntry.EUMMappedItemUnitDesc = existingEntry.EUMMappedItemUnitDesc;
                        newEntry.EUMMappedItemUnitKey = existingEntry.EUMMappedItemUnitKey;
                        newEntry.CFStandardName = "Undefined";
                        newEntry.CFStandardDesc = "Undefined";
                        newEntry.CFStandardUnit = "Undefined";
                        if (undefinedEntries.Count == 0)
                            undefinedEntries.Add(newEntry);
                    }
                }

            }
         
            if (closestEntries.Count == 0) closestEntries = undefinedEntries;
            return closestEntries;
        }

        private static DHICFEntry _makeNewEntry(DHICFEntry existingEntry, standard_name_tableEntry cfStandard)
        {
            DHICFEntry newEntry = new DHICFEntry();
            newEntry.EUMItemDesc = existingEntry.EUMItemDesc;
            newEntry.EUMItemKey = existingEntry.EUMItemKey;
            newEntry.EUMItemUnitDesc = existingEntry.EUMItemUnitDesc;
            newEntry.EUMItemUnitKeys = existingEntry.EUMItemUnitKeys;
            newEntry.EUMMappedItemUnitDesc = existingEntry.EUMMappedItemUnitDesc;
            newEntry.EUMMappedItemUnitKey = existingEntry.EUMMappedItemUnitKey;
            newEntry.CFStandardName = cfStandard.id;
            newEntry.CFStandardDesc = cfStandard.description;
            newEntry.CFStandardUnit = cfStandard.canonical_units;
            return newEntry;
        }

        /// <summary>
        /// Finds and returns a list of closest DHI EUMs from a CF Standard Name
        /// </summary>
        /// <param name="dhiEumFilterType"></param>
        /// <param name="cfStandardName"></param>
        /// <param name="fuzzyCoefficient"></param>
        /// <returns></returns>
        public List<DHICFEntry> FindClosestDHIEums(string dhiEumFilterType, string cfStandardName, double fuzzyCoefficient)
        {
            _GetAllDHIEUM();
            _GetCFStandard();

            List<DHICFEntry> closestEntries = new List<DHICFEntry>();
            foreach (standard_name_tableEntry cfStandard in _cfStandardTable.entry)
            {
                if (cfStandard.id == cfStandardName)
                {
                    foreach (DHICFMapping existingMap in _dhiCFMapping)
                    {
                        if (existingMap.MikeEUMFilterName == dhiEumFilterType)
                        {
                            foreach (DHICFEntry existingEntry in existingMap.DHICFEntries)
                            {
                                int searchWordCount = Search(cfStandardName, new List<string> { existingEntry.EUMItemDesc }, fuzzyCoefficient);
                                if (searchWordCount > 0)
                                {
                                    DHICFEntry newEntry = new DHICFEntry();
                                    newEntry.EUMItemDesc = existingEntry.EUMItemDesc;
                                    newEntry.EUMItemKey = existingEntry.EUMItemKey;
                                    newEntry.EUMItemUnitDesc = existingEntry.EUMItemUnitDesc;
                                    newEntry.EUMItemUnitKeys = existingEntry.EUMItemUnitKeys;
                                    newEntry.EUMMappedItemUnitDesc = existingEntry.EUMMappedItemUnitDesc;
                                    newEntry.EUMMappedItemUnitKey = existingEntry.EUMMappedItemUnitKey;
                                    newEntry.CFStandardName = cfStandard.id;
                                    newEntry.CFStandardDesc = cfStandard.description;
                                    newEntry.CFStandardUnit = cfStandard.canonical_units;
                                    closestEntries.Add(newEntry);
                                }

                            }
                        }
                    }
                }
            }
            return closestEntries;
        }

        /// <summary>
        /// Finds and returns a list of closest DHI EUMs from an item name
        /// </summary>
        /// <param name="dhiEumFilterType"></param>
        /// <param name="itemName"></param>
        /// <param name="fuzzyCoefficient"></param>
        /// <returns></returns>
        public List<DHICFEntry> FindDHIEums(string itemName, double fuzzyCoefficient)
        {
            _GetAllDHIEUM();

            List<DHICFEntry> closestEntries = new List<DHICFEntry>();

            foreach (DHICFMapping existingMap in _dhiCFMapping)
            {
                foreach (DHICFEntry existingEntry in existingMap.DHICFEntries)
                {
                    int searchWordCount = Search(itemName, new List<string> { existingEntry.EUMItemDesc }, fuzzyCoefficient);
                    if (searchWordCount > 0)
                    {
                        DHICFEntry newEntry = new DHICFEntry();
                        newEntry.EUMItemDesc = existingEntry.EUMItemDesc;
                        newEntry.EUMItemKey = existingEntry.EUMItemKey;
                        newEntry.EUMItemUnitDesc = existingEntry.EUMItemUnitDesc;
                        newEntry.EUMItemUnitKeys = existingEntry.EUMItemUnitKeys;
                        newEntry.EUMMappedItemUnitDesc = existingEntry.EUMMappedItemUnitDesc;
                        newEntry.EUMMappedItemUnitKey = existingEntry.EUMMappedItemUnitKey;
                        
                        int existingCount = 0;
                        foreach (DHICFEntry existingClosestEntry in closestEntries)
                        {
                            if (existingClosestEntry.EUMItemDesc == newEntry.EUMItemDesc & existingClosestEntry.EUMItemKey == newEntry.EUMItemKey)
                                existingCount++;
                        }
                        if (existingCount == 0)
                            closestEntries.Add(newEntry);
                    }
                }
            }
            return closestEntries;
        }

        /// <summary>
        /// Saves a DHI-CF Entry to an existing dhi_cf_map file.
        /// </summary>
        /// <param name="EntryToBeSaved"></param>
        /// <param name="settingsFile"></param>
        public void SaveMappingToXml(DHICFEntry EntryToBeSaved, string dhiCFMapFile)
        {
            DHICFMapping existingMapping;
            DHICFMapping newMapping = new DHICFMapping();
            XmlSerialiser xmlSerialiser = new XmlSerialiser();
            if (System.IO.File.Exists(dhiCFMapFile))
            {
                string xmlData = xmlSerialiser.ReadXMLFile(dhiCFMapFile);
                existingMapping = (DHICFMapping)xmlSerialiser.DeserializeObject(xmlData, typeof(DHICFMapping));
                newMapping.CFStandardNamesMapped = existingMapping.CFStandardNamesMapped;
                newMapping.CFStandardXmlFileName = existingMapping.CFStandardXmlFileName;
                newMapping.CFStandardXmlVersion = existingMapping.CFStandardXmlVersion;
                newMapping.ConversionDateTime = existingMapping.ConversionDateTime;
                newMapping.ConversionMachineUserName = existingMapping.ConversionMachineUserName;
                newMapping.DHICFEntries = existingMapping.DHICFEntries;
                newMapping.MikebyDHIVersion = existingMapping.MikebyDHIVersion;
                newMapping.MikeEUMFilterName = existingMapping.MikeEUMFilterName;

                for (int i = 0; i < existingMapping.DHICFEntries.Count; i++)
                {
                    if (existingMapping.DHICFEntries[i].EUMItemDesc == EntryToBeSaved.EUMItemDesc)
                    {
                        newMapping.DHICFEntries[i] = EntryToBeSaved;
                    }
                }

                xmlData = xmlSerialiser.SerializeObject(existingMapping, typeof(DHICFMapping));
                xmlSerialiser.WriteXMLFile(xmlData, dhiCFMapFile);
            }
        }

        public List<DHICFEntry> GetAllCFNames()
        {
            _GetDHIEUM();
            //List<DHICFEntry> dhiEntries = GetAllDHIEum();
            _GetCFStandard();

            List<DHICFEntry> closestEntries = new List<DHICFEntry>();

            /*foreach (DHICFMapping existingMap in _dhiCFMapping)
            {
                foreach (DHICFEntry existingEntry in dhiEntries)// existingMap.DHICFEntries)
                {*/
                    foreach (standard_name_tableEntry cfStandard in _cfStandardTable.entry)
                    {
                        DHICFEntry newEntry = new DHICFEntry();
                        /*newEntry.EUMItemDesc = existingEntry.EUMItemDesc;
                        newEntry.EUMItemKey = existingEntry.EUMItemKey;
                        newEntry.EUMItemUnitDesc = existingEntry.EUMItemUnitDesc;
                        newEntry.EUMItemUnitKeys = existingEntry.EUMItemUnitKeys;
                        newEntry.EUMMappedItemUnitDesc = existingEntry.EUMMappedItemUnitDesc;
                        newEntry.EUMMappedItemUnitKey = existingEntry.EUMMappedItemUnitKey;*/
                        newEntry.CFStandardName = cfStandard.id;
                        newEntry.CFStandardDesc = cfStandard.description;
                        newEntry.CFStandardUnit = cfStandard.canonical_units;

                        /*int existingCount = 0;
                        foreach (DHICFEntry existingClosestEntry in closestEntries)
                        {
                            //if (existingClosestEntry.EUMItemDesc == newEntry.EUMItemDesc & existingClosestEntry.EUMItemKey == newEntry.EUMItemKey)
                            if (existingClosestEntry.CFStandardDesc == newEntry.CFStandardDesc & existingClosestEntry.CFStandardName == newEntry.CFStandardName)
                            {
                                existingCount++;

                            }
                        }

                        if (existingCount == 0)*/
                        closestEntries.Add(newEntry);


                    }

                //}

            //}
            return closestEntries;
        }

        public List<DHICFEntry> GetAllDHIEum()
        {
            _GetAllDHIEUM();
            return _dhiCFMapping[0].DHICFEntries;
        }

        private void _GetCFStandard()
        {
            string defaultFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location) + "\\cf-standard-name-table.xml";
            if (!System.IO.File.Exists(defaultFile))
            {
                try
                {
                    using (System.Net.WebClient Client = new System.Net.WebClient())
                    {
                        Client.DownloadFile("http://cfconventions.org/Data/cf-standard-names/27/src/cf-standard-name-table.xml", defaultFile);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to download cf-standard-name-table from http://cfconventions.org/Data/cf-standard-names/27/src/cf-standard-name-table.xml. " + ex.Message);
                }
            }
            XmlSerialiser xmlSerialiser = new XmlSerialiser();
            string xmlData = xmlSerialiser.ReadXMLFile(defaultFile);
            if (_cfStandardTable == null)
                _cfStandardTable = (standard_name_table)xmlSerialiser.DeserializeObject(xmlData, typeof(standard_name_table));
            foreach (DHICFMapping existingMap in _dhiCFMapping)
            {
                existingMap.CFStandardXmlFileName = defaultFile;
                existingMap.CFStandardXmlVersion = _cfStandardTable.version_number;
            }

        }

        private void _GetDHIEUM()
        {
            _dhiCFMapping = new List<DHICFMapping>();
            for (int eumFilterCounter = 0; eumFilterCounter < MikeZero.EUMWrapper.eumGetFilterCount(); eumFilterCounter++)
            {
                DHICFMapping newMap = new DHICFMapping(); //todo: add additional meta information
                int filterCount = 0;
                foreach (object filter in Enum.GetValues(typeof(eumFilter)))
                {
                    if (filterCount == eumFilterCounter) newMap.MikeEUMFilterName = filter.ToString();
                    filterCount++;
                }

                for (int eumCounter = 0; eumCounter < MikeZero.EUMWrapper.eumGetItemTypeCount(); eumCounter++)
                {
                    DHICFEntry newEntry = new DHICFEntry();
                    newEntry.EUMItemUnitDesc = new List<string>();
                    newEntry.EUMItemUnitKeys = new List<int>();
                    int itemKey;
                    string itemDesc;

                    if (MikeZero.EUMWrapper.eumGetFilteredItemTypeSeq(eumFilterCounter, eumCounter, out itemKey, out itemDesc))
                    {
                        //MikeZero.EUMWrapper.eumGetItemTypeSeq(eumCounter, out newEntry.EUMItemKey, out newEntry.EUMItemDesc);
                        newEntry.EUMItemKey = itemKey;
                        newEntry.EUMItemDesc = itemDesc;

                        string itemUnitDesc = string.Empty;
                        int itemUnitKey = 0;
                        for (int unitSegNum = 0; unitSegNum < MikeZero.EUMWrapper.eumGetItemUnitCount(newEntry.EUMItemKey); unitSegNum++)
                        {
                            if (MikeZero.EUMWrapper.eumGetItemUnitSeq(newEntry.EUMItemKey, unitSegNum, out itemUnitKey, out itemUnitDesc))
                            {
                                newEntry.EUMItemUnitDesc.Add(itemUnitDesc);
                                newEntry.EUMItemUnitKeys.Add(itemUnitKey);
                            }
                        }
                        newMap.DHICFEntries.Add(newEntry);
                    }
                }
                _dhiCFMapping.Add(newMap);
            }
        }

        private void _GetAllDHIEUM()
        {
            _dhiCFMapping = new List<DHICFMapping>();
            string[] eumNames = Enum.GetNames(typeof(MikeZero.eumItem));
            Array eumValues = Enum.GetValues(typeof(MikeZero.eumItem));

            DHICFMapping newMap = new DHICFMapping();
            for (int eumCount = 0; eumCount < eumNames.Length; eumCount++)
            {
                DHICFEntry newEntry = new DHICFEntry();
                newEntry.EUMItemUnitDesc = new List<string>();
                newEntry.EUMItemUnitKeys = new List<int>();

                newEntry.EUMItemKey = Convert.ToInt32(eumValues.GetValue(eumCount));
                string itemDesc = string.Empty;
                MikeZero.EUMWrapper.eumGetItemTypeKey(newEntry.EUMItemKey, out itemDesc);
                newEntry.EUMItemDesc = itemDesc;

                string itemUnitDesc = string.Empty;
                int itemUnitKey = 0;
                for (int unitSegNum = 0; unitSegNum < MikeZero.EUMWrapper.eumGetItemUnitCount(newEntry.EUMItemKey); unitSegNum++)
                {
                    if (MikeZero.EUMWrapper.eumGetItemUnitSeq(newEntry.EUMItemKey, unitSegNum, out itemUnitKey, out itemUnitDesc))
                    {
                        newEntry.EUMItemUnitDesc.Add(itemUnitDesc);
                        newEntry.EUMItemUnitKeys.Add(itemUnitKey);
                    }
                }
                newMap.DHICFEntries.Add(newEntry);
            }
            _dhiCFMapping.Add(newMap);
        }

        private bool _CompareIDs(string DHIEUM, string CFStandardName, string PrevCFStandardName, double fuzzyness)
        {
            int curWordCount = Search(DHIEUM, new List<string> { CFStandardName }, fuzzyness);

            if (PrevCFStandardName == null) PrevCFStandardName = "";
            int prevWordCount = Search(DHIEUM, new List<string> { PrevCFStandardName }, fuzzyness);
            if (curWordCount >= prevWordCount && curWordCount > 0) return true;
            else return false;
        }

        public int Search(string word, List<string> wordList, double fuzzyness)
        {
            List<string> foundWords =
                (
                    from s in wordList
                    let levenshteinDistance = _LevenshteinDistance(word, s)
                    let length = Math.Max(s.Length, word.Length)
                    let score = 1.0 - (double)levenshteinDistance / length
                    where score > fuzzyness
                    select s
                ).ToList();

            return foundWords.Count;
        }

        public int Search(string word, List<string> wordList, double fuzzyness, out string firstFoundWord)
        {
            List<string> foundWords =
                (
                    from s in wordList
                    let levenshteinDistance = _LevenshteinDistance(word, s)
                    let length = Math.Max(s.Length, word.Length)
                    let score = 1.0 - (double)levenshteinDistance / length
                    where score > fuzzyness
                    select s
                ).ToList();
            if (foundWords.Count > 0)
                firstFoundWord = foundWords[0];
            else
                firstFoundWord = "None";
            return foundWords.Count;
        }

        private static int _LevenshteinDistance(string src, string dest)
        {
            int[,] d = new int[src.Length + 1, dest.Length + 1];
            int i, j, cost;
            char[] str1 = src.ToCharArray();
            char[] str2 = dest.ToCharArray();

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {

                    if (str1[i - 1] == str2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,              // Deletion
                            Math.Min(
                                d[i, j - 1] + 1,          // Insertion
                                d[i - 1, j - 1] + cost)); // Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] ==
                        str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[str1.Length, str2.Length];
        }
    }

    public class DHICFMapping
    {
        public string CFStandardXmlFileName;
        public string CFStandardXmlVersion;
        public DateTime ConversionDateTime;
        public string ConversionMachineUserName;
        public string MikebyDHIVersion;
        public string MikeEUMFilterName;
        public int CFStandardNamesMapped;
        private List<DHICFEntry> _dhiCFEntries = new List<DHICFEntry>();

        public List<DHICFEntry> DHICFEntries
        {
            get { return _dhiCFEntries; }
            set { this._dhiCFEntries = value; }
        }

    }

    public class DHICFEntry
    {
        private int _eumItemKey, _eumMappedItemUnitKey;
        private string _eumItemDesc, _eumMappedItemUnitDesc, _cfStandardName, _cfStandardUnit, _cfStandardDesc, _cfStandardAlias;
        private List<string> _eumItemUnitDesc;
        private List<int> _eumItemUnitKeys;

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public int EUMItemKey
        {
            get { return _eumItemKey; }
            set { _eumItemKey = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public int EUMMappedItemUnitKey
        {
            get { return _eumMappedItemUnitKey; }
            set { _eumMappedItemUnitKey = value; }
        }

        [TypeConverter(typeof(EUMItemListConverter)),
        CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string EUMItemDesc
        {
            get { return _eumItemDesc; }
            set { _eumItemDesc = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string EUMMappedItemUnitDesc
        {
            get { return _eumMappedItemUnitDesc; }
            set { _eumMappedItemUnitDesc = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor(
       "System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
       "System.Drawing.Design.UITypeEditor, System.Drawing, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public List<string> EUMItemUnitDesc
        {
            get { return _eumItemUnitDesc; }
            set { _eumItemUnitDesc = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public List<int> EUMItemUnitKeys
        {
            get { return _eumItemUnitKeys; }
            set { _eumItemUnitKeys = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string CFStandardName
        {
            get { return _cfStandardName; }
            set { _cfStandardName = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string CFStandardUnit
        {
            get { return _cfStandardUnit; }
            set { _cfStandardUnit = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string CFStandardDesc
        {
            get { return _cfStandardDesc; }
            set { _cfStandardDesc = value; }
        }

        [CategoryAttribute("DHI and CF Standard Items"), ReadOnlyAttribute(false)]
        public string CFStandardAlias
        {
            get { return _cfStandardAlias; }
            set { _cfStandardAlias = value; }
        }
    }

    public class EUMItemListConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {

            List<string> eumItems = new List<string>();

            int itemKey = 0;
            string itemDesc;

            for (int eumFilterCounter = 0; eumFilterCounter < MikeZero.EUMWrapper.eumGetFilterCount(); eumFilterCounter++)
            {
                for (int eumCounter = 0; eumCounter < MikeZero.EUMWrapper.eumGetItemTypeCount(); eumCounter++)
                {
                    MikeZero.EUMWrapper.eumGetFilteredItemTypeSeq(eumFilterCounter, eumCounter, out itemKey, out itemDesc);
                    eumItems.Add(itemDesc);
                }
            }

            return new StandardValuesCollection(eumItems.ToArray());
        }

    }
}
