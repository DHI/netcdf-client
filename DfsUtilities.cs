using System;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using System.Runtime.InteropServices;
using System.Globalization;

namespace DHI.Generic.NetCDF.MIKE
{
    public class dfsuCustomBlock
    {
        public int NoNodesTot; //number of nodes in each layer
        public int NoElemTot; // number af elements in each layer
        public int Dim; // dimension (2 or 3)
        public int NoLayers; // number of layers

        public int NoNodesInEachLayer { get { if (NoLayers < 1) return NoNodesTot; else return NoNodesTot / NoLayers; } }
        public int NoElemInEachLayer { get { if (NoLayers < 1) return NoElemTot; else return NoElemTot / NoLayers; } }
    }

    public struct m21CustomBlock
    {
        public float ori;
        public float f1; //LITOrientation
        public float f2;
        public float f3; //landvalue
        public float f4;
        public float f5;
        public float f6; //GISLITOrientation
    }

    /// <summary>
    /// Summary description for DFSFileInfo. Taken and adapted from JdfsFileInfo from Water Forecast
    /// </summary>
    public class DfsUtilities
    {

        public FileType dfsFileType = FileType.EqtimeFixedspaceAllitems; //FileType.F_UNDEFINED_FILE_TYPE
        public int DataType;
        public float delVal = (float)-1e-30;
        public string FileTitle = "Untitled";
        public StatType statType = StatType.NoStat;
        public IntPtr pHeader;
        public IntPtr pFile;
        public ProjectionType Projection_type = ProjectionType.Projection; //F_UNDEFINED_PROJECTION;
        public string Projection = "UTM-32";
        public double Orientation = 0.0; //COMMath.DOUBLE_NaN;
        public double Longitude = 0.0; //COMMath.DOUBLE_NaN;
        public double Latitude = 0.0; //COMMath.DOUBLE_NaN;
        //public double  Easting = 0.0; //COMMath.DOUBLE_NaN;
        //public double  Northing = 0.0; //COMMath.DOUBLE_NaN;

        public string CustomBlockName = "Unknown";
        public float[] custBlockDataFloat;
        public int[] custBlockDataInt;
        public dfsuCustomBlock dfsuCustBlock = new dfsuCustomBlock();
        public m21CustomBlock m21CustBlock = new m21CustomBlock();

        public DfsItemInfo[] Items = null;
        public DfsItemInfo[] staticItems = null;
        public bool readStaticDataOnRead = true;
        public bool writeStaticDataOnWrite = true;

        public bool compressed = false;
        public int encodeKeySize = 0;
        public int[] compress_XKey, compress_YKey, compress_ZKey;

        public TimeAxisType tAxisType = TimeAxisType.CalendarEquidistant; //.F_UNDEFINED_TAXIS;
        public string tAxis_StartDateStr = string.Empty; //"2000-01-01", expect something in this format
        public string tAxis_StartTimeStr = string.Empty; //"00:00:00", expect something in this format
        public double tAxis_dTStep = 0.0;
        public int tAxis_nTSteps = 0;
        public int tAxis_EUMUnit = (int)eumUnit.eumUsec;
        public string tAxis_EUMUnitStr = "";
        private double tAxis_dTStart = 0.0; 
        private int tAxis_indexTStart = 0; 
        private bool isLandPointCalled = false;
        private bool[] landPoint;

        private string m_fileName = "";

        public string errMsg = "";

        //Constructor
        public DfsUtilities()
        {
            // set default values for custom blocks
            dfsuCustBlock.NoNodesTot = -1;
            dfsuCustBlock.NoElemTot = -1;
            dfsuCustBlock.Dim = -1;
            dfsuCustBlock.NoLayers = -1;

            m21CustBlock.ori = 0;
            m21CustBlock.f1 = delVal;
            m21CustBlock.f2 = -900f;
            m21CustBlock.f3 = delVal;
            m21CustBlock.f4 = delVal;
            m21CustBlock.f5 = delVal;
            m21CustBlock.f6 = 0;
        }

        //copy all field values, loop items and copy all field values
        //http://www.codeproject.com/csharp/cloneimpl_class.asp
        public DfsUtilities Clone()
        {
            DfsUtilities dolly = new DfsUtilities();
            dolly.compressed = compressed;
            dolly.DataType = DataType;
            dolly.delVal = delVal;
            dolly.dfsFileType = dfsFileType;
            dolly.FileTitle = FileTitle;
            dolly.Latitude = Latitude;
            dolly.Longitude = Longitude;
            dolly.Orientation = Orientation;
            dolly.pFile = pFile;
            dolly.pHeader = pHeader;
            dolly.Projection = Projection;
            dolly.Projection_type = Projection_type;
            dolly.statType = statType;
            dolly.tAxis_dTStart = tAxis_dTStart;
            dolly.tAxis_dTStep = tAxis_dTStep;
            dolly.tAxis_EUMUnit = tAxis_EUMUnit;
            dolly.tAxis_EUMUnitStr = tAxis_EUMUnitStr;
            dolly.tAxis_indexTStart = tAxis_indexTStart;
            dolly.tAxis_nTSteps = tAxis_nTSteps;
            dolly.tAxis_StartDateStr = tAxis_StartDateStr;
            dolly.tAxis_StartTimeStr = tAxis_StartTimeStr;
            dolly.tAxisType = tAxisType;
            dolly.CustomBlockName = CustomBlockName;
            dolly.m21CustBlock.ori = m21CustBlock.ori;
            dolly.m21CustBlock.f1 = m21CustBlock.f1;
            dolly.m21CustBlock.f2 = m21CustBlock.f2;
            dolly.m21CustBlock.f3 = m21CustBlock.f3;
            dolly.m21CustBlock.f4 = m21CustBlock.f4;
            dolly.m21CustBlock.f5 = m21CustBlock.f5;
            dolly.m21CustBlock.f6 = m21CustBlock.f6;
            dolly.dfsuCustBlock.Dim = dfsuCustBlock.Dim;
            dolly.dfsuCustBlock.NoElemTot = dfsuCustBlock.NoElemTot;
            dolly.dfsuCustBlock.NoLayers = dfsuCustBlock.NoLayers;
            dolly.dfsuCustBlock.NoNodesTot = dfsuCustBlock.NoNodesTot;
            dolly.encodeKeySize = encodeKeySize;
            if (compressed)
            {
                dolly.compress_XKey = compress_XKey;
                dolly.compress_YKey = compress_YKey;
                dolly.compress_ZKey = compress_ZKey;
            }
            if (custBlockDataFloat != null) { dolly.custBlockDataFloat = new float[custBlockDataFloat.Length]; System.Array.Copy(custBlockDataFloat, dolly.custBlockDataFloat, custBlockDataFloat.Length); }
            if (custBlockDataInt != null) { dolly.custBlockDataInt = new int[custBlockDataInt.Length]; System.Array.Copy(custBlockDataInt, dolly.custBlockDataInt, custBlockDataInt.Length); }

            dolly.readStaticDataOnRead = readStaticDataOnRead;
            dolly.writeStaticDataOnWrite = writeStaticDataOnWrite;
            dolly.staticItems = new DfsItemInfo[staticItems.Length];
            for (int i = 0; i < staticItems.Length; i++) dolly.staticItems[i] = staticItems[i].Clone();

            dolly.Items = new DfsItemInfo[Items.Length];
            for (int i = 0; i < Items.Length; i++) dolly.Items[i] = Items[i].Clone();

            return dolly;
        }

        public string FileName { get { return m_fileName; } }

        public DateTime TimeStepDateTime(int timeStepNo)
        {
            return StartDateTime.AddSeconds(tAxis_dTStep * timeStepNo);
        }

        public DateTime StartDateTime
        {
            get { return DfsDateTime2DateTime(tAxis_StartDateStr, tAxis_StartTimeStr); }
            set { tAxis_StartDateStr = MakeDfsDate(value); tAxis_StartTimeStr = MakeDfsTime(value); }
        }

        public DateTime EndDateTime
        {
            get
            {
                if (tAxis_EUMUnit != (int)eumUnit.eumUsec) { _err("Only seconds as timeaxis unit supported."); return DateTime.MinValue; }
                DateTime edt = StartDateTime;
                return StartDateTime.AddSeconds(tAxis_dTStep * (tAxis_nTSteps - 1));
            }
        }

        //will return -1 if dt is after last time step
        public int FirstTimeStepAfter(DateTime dt)
        {
            if (tAxis_EUMUnit != (int)eumUnit.eumUsec) { _err("Only seconds as timeaxis unit supported."); return -2; }
            TimeSpan tsp = dt - StartDateTime;
            int res = Convert.ToInt32(Math.Ceiling(tsp.TotalSeconds / tAxis_dTStep));
            if (res > tAxis_nTSteps) return -1; else return res;
        }

        //error handling
        private int _err(string msg)
        {
            return _err(msg, 1);
        }

        private int _err(string msg, int errNo)
        {
            //errMsg += System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\t" + msg + "\r\n";
            errMsg += "\t" + msg + "\r\n";
            return errNo;
        }

        public int ReadDfsFile(string dfsFileName)
        {
            int rc;

            DfsDLLWrapper.dfsFileRead(dfsFileName, out pHeader, out pFile);
            m_fileName = dfsFileName;

            compressed = (DfsDLLWrapper.dfsIsFileCompressed(pHeader));
            if (compressed)
            {
                encodeKeySize = DfsDLLWrapper.dfsGetEncodeKeySize(pHeader);
                if (encodeKeySize > 0)
                {
                    compress_XKey = new int[encodeKeySize];
                    compress_YKey = new int[encodeKeySize];
                    compress_ZKey = new int[encodeKeySize];
                    DfsDLLWrapper.dfsGetEncodeKey(pHeader, compress_XKey, compress_YKey, compress_ZKey);
                }
                else compressed = false;
            }

            // general info about file
            dfsFileType = (FileType)DfsDLLWrapper.dfsGetFileType(pHeader);
            DataType = DfsDLLWrapper.dfsGetDataType(pHeader);

            //delete value
            delVal = DfsDLLWrapper.dfsGetDeleteValFloat(pHeader);

            //statisics type
            statType = DfsDLLWrapper.dfsGetItemStatsType(pHeader);

            //Custom blocks
            DfsSimpleType iDataType = DfsSimpleType.Float;
            int iMiscVarNos = 0;
            IntPtr pData = pHeader;
            IntPtr pNextBlock = pHeader;
            IntPtr pBlock = pHeader;
            pBlock = DfsDLLWrapper.dfsGetCustomBlockRef(pHeader);

            if (pBlock.ToInt32() != 0)
            {
                DfsDLLWrapper.dfsGetCustomBlock(pBlock, out iDataType, out CustomBlockName, out iMiscVarNos, ref pData, out pNextBlock);
                switch ((DfsSimpleType)iDataType)
                {
                    case DfsSimpleType.Float:
                        custBlockDataFloat = new float[iMiscVarNos];
                        Marshal.Copy(pData, custBlockDataFloat, 0, custBlockDataFloat.Length); // copy data from pointer to array
                        break;
                    case DfsSimpleType.Int:
                        custBlockDataInt = new int[iMiscVarNos];
                        Marshal.Copy(pData, custBlockDataInt, 0, custBlockDataInt.Length); // copy data from pointer to array
                        break;
                    default:
                        throw new Exception("Unsupported CustomBlock data tyoe encountered (" + iDataType + ".");
                }
                if (CustomBlockName == "MIKE_FM")
                {
                    //dfsu
                    if (custBlockDataInt.Length > 0) dfsuCustBlock.NoNodesTot = custBlockDataInt[0];
                    if (custBlockDataInt.Length > 1) dfsuCustBlock.NoElemTot = custBlockDataInt[1];
                    if (custBlockDataInt.Length > 2) dfsuCustBlock.Dim = custBlockDataInt[2];
                    if (custBlockDataInt.Length > 3) dfsuCustBlock.NoLayers = custBlockDataInt[3];
                }
                else if (CustomBlockName == "M21_Misc")
                {
                    if (custBlockDataFloat.Length > 0) m21CustBlock.ori = custBlockDataFloat[0]; //m_LITOrientation
                    if (custBlockDataFloat.Length > 1) m21CustBlock.f1 = custBlockDataFloat[1];
                    if (custBlockDataFloat.Length > 2) m21CustBlock.f2 = custBlockDataFloat[2];
                    if (custBlockDataFloat.Length > 3) m21CustBlock.f3 = custBlockDataFloat[3]; //m_LandValue
                    if (custBlockDataFloat.Length > 4) m21CustBlock.f4 = custBlockDataFloat[4];
                    if (custBlockDataFloat.Length > 5) m21CustBlock.f5 = custBlockDataFloat[5];
                    if (custBlockDataFloat.Length > 6) m21CustBlock.f6 = custBlockDataFloat[6]; //m_GISLITOrientation
                }
            }

            //time axis 
            tAxisType = (TimeAxisType)DfsDLLWrapper.dfsGetTimeAxisType(pHeader);
            switch (tAxisType)
            {
                case TimeAxisType.CalendarEquidistant:
                    DfsDLLWrapper.dfsGetEqCalendarAxis(pHeader, out tAxis_StartDateStr, out tAxis_StartTimeStr, out tAxis_EUMUnit, out tAxis_EUMUnitStr, out tAxis_dTStart, out tAxis_dTStep, out tAxis_nTSteps, out tAxis_indexTStart);
                    break;
                case TimeAxisType.Undefined:
                    DfsDLLWrapper.dfsGetEqCalendarAxis(pHeader, out tAxis_StartDateStr, out tAxis_StartTimeStr, out tAxis_EUMUnit, out tAxis_EUMUnitStr, out tAxis_dTStart, out tAxis_dTStep, out tAxis_nTSteps, out tAxis_indexTStart);
                    break;
                case TimeAxisType.CalendarNonEquidistant:
                    DfsDLLWrapper.dfsGetNeqCalendarAxis(pHeader, out tAxis_StartDateStr, out tAxis_StartTimeStr, out tAxis_EUMUnit, out tAxis_EUMUnitStr, out tAxis_dTStart, out tAxis_dTStep, out tAxis_nTSteps, out tAxis_indexTStart);
                    break;
                case TimeAxisType.TimeEquidistant:
                    DfsDLLWrapper.dfsGetEqTimeAxis(pHeader, out tAxis_EUMUnit, out tAxis_EUMUnitStr, out tAxis_dTStart, out tAxis_dTStep, out tAxis_nTSteps, out tAxis_indexTStart);
                    break;
                case TimeAxisType.TimeNonEquidistant:
                    DfsDLLWrapper.dfsGetNeqTimeAxis(pHeader, out tAxis_EUMUnit, out tAxis_EUMUnitStr, out tAxis_dTStart, out tAxis_dTStep, out tAxis_nTSteps, out tAxis_indexTStart);
                    break;
                default:
                    return _err(tAxisType.ToString() + " not supported");
            }

            //Projection
            Projection_type = (ProjectionType)DfsDLLWrapper.dfsGetGeoInfoType(pHeader);
            if (Projection_type == ProjectionType.Projection)
            {
                DfsDLLWrapper.dfsGetGeoInfoUTMProj(pHeader, out Projection, out Longitude, out Latitude, out Orientation);
            }

            //Dynamic Items
            int ItemCount = DfsDLLWrapper.dfsGetNoOfItems(pHeader);
            Items = new DfsItemInfo[ItemCount];
            for (int i = 1; i < Items.Length + 1; i++)
            {
                Items[i - 1] = new DfsItemInfo();
                Items[i - 1].fileInfoRef = this;
                Items[i - 1].Read(i); // reads header
            }

            //Static Items
            rc = 0;
            int sItemNo = 0;
            while (true)
            {
                sItemNo++;
                try
                { DfsDLLWrapper.dfsFindItemStatic(pHeader, pFile, sItemNo); }
                catch
                { break; }// no more static items
            }
            if (sItemNo > 0)
            {
                staticItems = new DfsItemInfo[sItemNo - 2];
                for (int i = 0; i < staticItems.Length; i++)
                {
                    staticItems[i] = new DfsItemInfo();
                    staticItems[i].fileInfoRef = this;
                    rc = staticItems[i].ReadStatic(i + 1); // read header
                }
                if (readStaticDataOnRead) rc = ReadStaticData();
            }

            return rc;
        }

        //read all static data into static arrays in each item.
        public int ReadStaticData()
        {
            int rc;
            for (int i = 0; i < staticItems.Length; i++)
            {
                rc = staticItems[i].ReadStaticData();
                if (rc != 0) return rc;
            }
            return 0;
        }

        public int WriteToFile(string dfsFileName)
        {
            //create header
            FileType d = dfsFileType;
            pHeader = DfsDLLWrapper.dfsHeaderCreate(d, FileTitle, "DfsFileInfo", 1, Items.Length, statType);

            DfsDLLWrapper.dfsSetDataType(pHeader, DataType);

            //delval
            DfsDLLWrapper.dfsSetDeleteValFloat(pHeader, delVal);

            int rc = 0;

            switch (CustomBlockName)
            {
                case "MIKE_FM":
                    int[] dfsuCBData = new int[4];
                    dfsuCBData[0] = dfsuCustBlock.NoNodesTot;
                    dfsuCBData[1] = dfsuCustBlock.NoElemTot;
                    dfsuCBData[2] = dfsuCustBlock.Dim;
                    dfsuCBData[3] = dfsuCustBlock.NoLayers;
                    DfsDLLWrapper.dfsAddCustomBlock(pHeader, "MIKE_FM", dfsuCBData);
                    break;
                case "M21_Misc":
                    float[] dM21CBData = new float[7];
                    dM21CBData[0] = (float)Orientation;
                    dM21CBData[1] = m21CustBlock.f1;
                    dM21CBData[2] = m21CustBlock.f2;
                    dM21CBData[3] = m21CustBlock.f3;
                    dM21CBData[4] = m21CustBlock.f4;
                    dM21CBData[5] = m21CustBlock.f5;
                    dM21CBData[6] = m21CustBlock.f6;
                    DfsDLLWrapper.dfsAddCustomBlock(pHeader, "M21_Misc", dM21CBData);
                    break;
                case "Unknown":
                    break;
                default:
                    //JdfsMisc.log("Warning: unsupported CustomBlockName encountered (" + CustomBlockName + "). Custom block not written.");
                    break;
            }

            //projection
            if (Projection_type == ProjectionType.Projection)
            {
                DfsDLLWrapper.dfsSetGeoInfoUTMProj(pHeader, Projection, Longitude, Latitude, Orientation);
            }

            //timeaxis
            switch (this.tAxisType)
            {
                case TimeAxisType.CalendarEquidistant:
                    DfsDLLWrapper.dfsSetEqCalendarAxis(pHeader, this.tAxis_StartDateStr, this.tAxis_StartTimeStr, (int)tAxis_EUMUnit, tAxis_dTStart, this.tAxis_dTStep, this.tAxis_indexTStart);
                    break;
                case TimeAxisType.CalendarNonEquidistant:
                    DfsDLLWrapper.dfsSetNeqCalendarAxis(pHeader, this.tAxis_StartDateStr, this.tAxis_StartTimeStr, (int)tAxis_EUMUnit, this.tAxis_dTStart, this.tAxis_indexTStart);
                    break;
                default:
                    _err("write of " + tAxisType.ToString() + " not supported");
                    break;
            }

            if (compressed)
            {
                if ((compress_XKey.Length < 1) || (compress_XKey.Length != compress_YKey.Length || compress_XKey.Length != compress_ZKey.Length))
                {
                    _err("Compress keys does not have same length or is empty. Compression disabled.");
                    compressed = false;
                }
                else
                {
                    DfsDLLWrapper.dfsItemEnableCompression(pHeader);
                    DfsDLLWrapper.dfsSetEncodeKey(pHeader, compress_XKey, compress_YKey, compress_ZKey, compress_XKey.Length);
                }
            }

            //Dynamic Items
            for (int i = 1; i < Items.Length + 1; i++)
            {
                Items[i - 1].fileInfoRef = this;
                rc = Items[i - 1].Write(i);
                if (rc != 0) return rc;
            }

            //Static Items
            if (staticItems != null)
            {
                for (int i = 1; i < staticItems.Length + 1; i++)
                {
                    staticItems[i - 1].fileInfoRef = this;
                    rc = staticItems[i - 1].WriteStatic(i);
                    if (rc != 0) return rc;
                }
            }

            pFile = (IntPtr)0;
            DfsDLLWrapper.dfsFileCreate(dfsFileName, pHeader, out pFile);

            //write static data
            if (staticItems != null && staticItems.Length > 0 && writeStaticDataOnWrite)
            {
                rc = WriteStaticData();
                if (rc != 0) return rc;
            }

            m_fileName = dfsFileName;
            return rc;
        }

        public int WriteStaticData()
        {
            int rc;
            for (int i = 1; i < staticItems.Length + 1; i++)
            {
                rc = staticItems[i - 1].WriteStaticData();
                if (rc != 0) return rc;
            }
            return 0;
        }

        public int AddItem(DfsItemInfo itm)
        {
            int l = 0;
            if (Items != null) l = Items.Length;
            DfsItemInfo[] tmpArr = new DfsItemInfo[l + 1];
            for (int i = 0; i < l; i++) tmpArr[i] = Items[i];
            tmpArr[l] = itm;
            Items = tmpArr;
            return l;
        }

        public int RemoveItem(int itemNo)
        {
            if (itemNo < 0 || itemNo > (Items.Length + 1)) return _err("Invalid item number (" + itemNo + ")", 7001);
            DfsItemInfo[] tmpArr = new DfsItemInfo[Items.Length - 1];
            int newi = -1;
            for (int i = 0; i < Items.Length; i++)
            {
                if (i != itemNo - 1)
                {
                    newi++;
                    tmpArr[newi] = Items[i];
                }
            }
            Items = tmpArr;
            return 0;
        }

        //Given csv string of item numbers, remove items from header info if necessary and return integer array of original item numbers
        //if keepItemCsv ="-1" all items are removed
        public int[] CheckRemoveItems(string keepItemCsv)
        {
            int[] itemNoArr = null;
            if (keepItemCsv == "")
            {
                //keep all items
                itemNoArr = new int[Items.Length];
                for (int i = 1; i < itemNoArr.Length + 1; i++) itemNoArr[i - 1] = i;
            }
            else
            {
                //remove some items
                if (keepItemCsv == "-1") keepItemCsv = "";
                itemNoArr = Csv2intArr(keepItemCsv, ',');
                for (int i = Items.Length; i > 0; i--)
                {
                    if (System.Array.IndexOf(itemNoArr, i) == -1) RemoveItem(i);
                }
            }
            return itemNoArr;
        }

        //check that given timesteps are within range - if not, adjust.
        public void CheckTimeStartandEnd(ref int timeStep_start, ref int timeStep_end)
        {
            if (timeStep_start == -1) timeStep_start = 0;
            if (timeStep_end == -1 || timeStep_end > tAxis_nTSteps - 1) timeStep_end = tAxis_nTSteps - 1;
        }

        // redefine the timeAxis, given another DfsfileInfo, new interval and spacing
        public int TimeAxis_Project(DfsUtilities fi_project, ref int timeStep_start, ref int timeStep_end, int timeStep_freq)
        {
            //edit timeaxis
            CheckTimeStartandEnd(ref timeStep_start, ref timeStep_end);
            if (timeStep_start > 0)
            {
                System.DateTime sdt = DfsDateTime2DateTime(fi_project.tAxis_StartDateStr, fi_project.tAxis_StartTimeStr);
                if (fi_project.tAxis_EUMUnit != (int)DHI.Generic.MikeZero.eumUnit.eumUsec) return _err("JdfsFileInfo only supports files with timeaxis in eumUsec");
                sdt = sdt.AddSeconds(tAxis_dTStep * timeStep_start);
                tAxis_StartDateStr = MakeDfsDate(sdt);
                tAxis_StartTimeStr = MakeDfsTime(sdt);
            }
            if (timeStep_freq != 1)
            {
                if (timeStep_freq < 1) return _err("JdfsFileInfo timeStepFreq must be integer>=1");
                tAxis_dTStep = fi_project.tAxis_dTStep * timeStep_freq;
            }
            return 0;
        }

        //determine if the given point (idx = x + nx*y + nx*ny*z) is a water point
        public bool IsLandPoint(int idx)
        {
            if (!isLandPointCalled)
            {
                //populate array of land/water points
                landPoint = new bool[Items[0].TotNoPoints];
                for (int p = 0; p < landPoint.Length; p++) landPoint[p] = false;
                float landValue = 10;
                if (m21CustBlock.f3 != delVal) landValue = m21CustBlock.f3;
                if (Items[0].sAxisType == SpaceAxisType.EqD2 || (Items[0].sAxisType == SpaceAxisType.EqD3 && !compressed))
                {
                    //dfs2 or dfs3 not compressed -> bathy item = 1
                    if (staticItems.Length > 0 && staticItems[0].staticDataFloat != null && staticItems[0].staticDataFloat.Length == landPoint.Length)
                    {
                        for (int p = 0; p < landPoint.Length; p++) landPoint[p] = (staticItems[0].staticDataFloat[p] >= landValue);
                    }
                }
                else if (Items[0].sAxisType == SpaceAxisType.EqD3 && compressed)
                {
                    for (int p = 0; p < landPoint.Length; p++) landPoint[p] = true;
                    if (staticItems.Length >= 3 && staticItems[0].staticDataInt.Length == encodeKeySize && staticItems[1].staticDataInt.Length == encodeKeySize && staticItems[2].staticDataInt.Length == encodeKeySize)
                    {
                        for (int l = 0; l < encodeKeySize; l++) landPoint[staticItems[0].staticDataInt[l] + staticItems[1].staticDataInt[l] * Items[0].nPointsX + staticItems[2].staticDataInt[l] * Items[0].nPointsX * Items[0].nPointsY] = false;
                    }
                }
                isLandPointCalled = true;
            }
            return landPoint[idx];
        }

        public int Close()
        {
            int rc = 0;
            DfsDLLWrapper.dfsFileClose(pHeader, ref pFile);
            DfsDLLWrapper.dfsHeaderDestroy(ref pHeader);
            return rc;
        }

        public override string ToString()
        {
            string res = "dfsFileType = " + dfsFileType.ToString() + "\r\n" +
                "Compressed = " + compressed.ToString() + "\r\n" +
                "DataType = " + DataType.ToString() + "\r\n" +
                "delVal = " + delVal.ToString() + "\r\n" +
                "FileTitle = " + FileTitle + "\r\n" +
                "statType = " + statType.ToString() + "\r\n" +
                "Projection_type = " + Projection_type.ToString() + "\r\n" +
                "Projection = " + Projection + "\r\n" +
                "Orientation = " + Orientation.ToString() + "\r\n" +
                "Longitude = " + Longitude.ToString() + "\r\n" +
                "Latitude = " + Latitude.ToString() + "\r\n" +
                "CustomBlockName= " + CustomBlockName + "\r\n";
            if (CustomBlockName == "MIKE_FM")
            {
                res += "\tNoNodesTot = " + dfsuCustBlock.NoNodesTot.ToString() + "\r\n" +
                    "\tNoElemTot = " + dfsuCustBlock.NoElemTot.ToString() + "\r\n" +
                    "\tDim = " + dfsuCustBlock.Dim.ToString() + "\r\n" +
                    "\tNoLayers = " + dfsuCustBlock.NoLayers.ToString() + "\r\n";
            }
            else if (CustomBlockName == "M21_Misc")
            {
                res += "\tori = " + m21CustBlock.ori + "\r\n" +
                    "\tf1 = " + m21CustBlock.f1 + "\r\n" +
                    "\tf2 = " + m21CustBlock.f2 + "\r\n" +
                    "\tf3 = " + m21CustBlock.f3 + "\r\n" +
                    "\tf4 = " + m21CustBlock.f4 + "\r\n" +
                    "\tf5 = " + m21CustBlock.f5 + "\r\n" +
                    "\tf6 = " + m21CustBlock.f6 + "\r\n";
            }
            res += "tAxisType = " + tAxisType.ToString() + "\r\n" +
                "tAxis_StartDateStr = " + tAxis_StartDateStr + "\r\n" +
                "tAxis_StartTimeStr = " + tAxis_StartTimeStr + "\r\n" +
                "tAxis_dTStep = " + tAxis_dTStep.ToString() + "\r\n" +
                "tAxis_nTSteps = " + tAxis_nTSteps.ToString() + "\r\n" +
                "tAxis_EUMUnit = " + tAxis_EUMUnit.ToString() + "\r\n" +
                "tAxis_EUMUnitStr = " + tAxis_EUMUnitStr + "\r\n" +
                "tAxis_dTStart = " + tAxis_dTStart.ToString() + "\r\n" +
                "tAxis_indexTStart = " + tAxis_indexTStart.ToString() + "\r\n";
            for (int i = 0; i < staticItems.Length; i++)
                res += "\r\nStatic item no " + (i + 1).ToString() + "\r\n" + staticItems[i].ToString();

            for (int i = 0; i < Items.Length; i++)
                res += "\r\nItem no " + (i + 1).ToString() + "\r\n" + Items[i].ToString();
            return res;
        }

        //read dynamic data from the given item number and timestep into the output array
        public int ReadDynData(int timeStep, int itemNo, out float[] data)
        {
            double t_rtn = 0;
            return ReadDynData(timeStep, itemNo, out data, out t_rtn);
        }

        //read dynamic data from the given item number and timestep into the output array and give the time offset of the given timestep from the start of the file in dT
        public int ReadDynData(int timeStep, int itemNo, out float[] data, out double dT)
        {
            dT = 0;
            data = new float[Items[itemNo - 1].TotNoPoints];
            DfsDLLWrapper.dfsFindItemDynamic(pHeader, pFile, timeStep, itemNo); // find item

            //dfsReadItemTimeStep always returns uncompressed data 
            bool isRead = DfsDLLWrapper.dfsReadItemTimeStep(pHeader, pFile, out dT, data);
            if (!isRead) throw new Exception("dfsReadItemTimeStep fail.");
            return 0;
        }

        //write dynamic data for this item and the given timestep to the file. 
        //NB make sure to write items (=call this function) in the right order (=the order of the items)!
        public int WriteDynData(double timeStep, float[] data)
        {
            if (Items.Length < 1) return _err("Add dynamic items before calling writeDynData.");
            if (compressed)
            {
                //compress data using the compress keys
                int nox = Items[0].nPointsX;
                int noy = Items[0].nPointsY;
                for (int i = 1; i < Items.Length; i++)
                    if (Items[i].nPointsX != nox || Items[i].nPointsY != noy) return _err("writeDynData, compressed data: not all dynamic items have same length", -2);

                float[] cdata = new float[compress_XKey.Length];
                for (int i = 0; i < cdata.Length; i++) cdata[i] = data[compress_ZKey[i] * noy * nox + nox * compress_YKey[i] + compress_XKey[i]];
                DfsDLLWrapper.dfsWriteItemTimeStep(pHeader, pFile, timeStep, cdata);
            }
            else
                DfsDLLWrapper.dfsWriteItemTimeStep(pHeader, pFile, timeStep, data);
            return 0;
        }

        //this function sets compress_XKey, compress_YKey and compress_ZKey, to the coordinates to the non delete values in the given data array.
        //NB always call this function before writing the header (calling write)! and ensure that data.length=totNoPoints of dyn items
        public int Compress_SetKeysFromDynData(float[] data)
        {
            if (Items.Length > 0)
                for (int i = 0; i < Items.Length; i++)
                    if (Items[i].TotNoPoints != data.Length) return _err("compress_SetKeysFromDynData: data.length!=totNoPoints of item no " + i, -1);
            int n = 0;
            int nox = Items[0].nPointsX;
            int noy = Items[0].nPointsY;
            for (int i = 0; i < data.Length; i++) if (data[i] != delVal) n++;
            compress_XKey = new int[n];
            compress_YKey = new int[n];
            compress_ZKey = new int[n];
            int l = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != delVal)
                {
                    compress_ZKey[l] = (int)Math.Floor((double)(i / (nox * noy)));
                    compress_YKey[l] = (int)Math.Floor((double)((i - compress_ZKey[l] * (nox * noy)) / nox));
                    compress_XKey[l] = i - compress_ZKey[l] * (nox * noy) - compress_YKey[l] * nox;
                    l++;
                }
            }
            return 0;
        }

        public static string MakeDfsDate(System.DateTime dt)
        {
            return dt.Year.ToString("0000") + "-" + dt.Month.ToString("00") + "-" + dt.Day.ToString("00");
        }

        public static string MakeDfsTime(System.DateTime dt)
        {
            return dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00") + ":" + dt.Second.ToString("00");
        }

        public static System.DateTime DfsDateTime2DateTime(string dfsDate, string dfsTime)
        {
            return new DateTime(Convert.ToInt32(dfsDate.Substring(0, 4)), Convert.ToInt32(dfsDate.Substring(5, 2)), Convert.ToInt32(dfsDate.Substring(8, 2)), Convert.ToInt32(dfsTime.Substring(0, 2)), Convert.ToInt32(dfsTime.Substring(3, 2)), Convert.ToInt32(dfsTime.Substring(6, 2)));
        }

        public static int[] Csv2intArr(string csvStr, char sep)
        {
            if (csvStr == "") return new int[0];
            string[] arrTmp = csvStr.Split(sep);
            int[] res = new int[arrTmp.Length];
            for (int i = 0; i < arrTmp.Length; i++) res[i] = Convert.ToInt32(arrTmp[i]);
            return res;
        }

    }

    public class DfsItemInfo
    {

        public string Name = "";
        public eumItem EUMType = eumItem.eumIItemUndefined;
        public eumUnit EUMUnit = eumUnit.eumUUnitUndefined;
        public string EUMUnitString = "";
        public string EUMTypeString = "";

        public SpaceAxisType sAxisType = SpaceAxisType.EqD2;
        public eumUnit axisEUMUnit = eumUnit.eumUmeter;
        public string axisEUMUnitString = "";
        public int dim = -1;
        private int m_nPointsX = -1, m_nPointsY = -1, m_nPointsZ = -1;
        public float XMinLimit = 0, YMinLimit = 0, ZMinLimit = 0, DX = -1, DY = -1, DZ = -1;

        public DfsSimpleType dataType = DfsSimpleType.Float;
        public DataValueType dataValType = DataValueType.Instantaneous; // default (there is no Invalid)

        public float[] staticDataFloat = null;
        public int[] staticDataInt = null;
        IntPtr staticVectorPtr = (IntPtr)0;

        public DfsUtilities fileInfoRef = null;

        public DfsItemInfo() { }

        public int nPointsX { get { return m_nPointsX; } set { m_nPointsX = value; } }
        public int nPointsY { get { return m_nPointsY; } set { m_nPointsY = value; } }
        public int nPointsZ { get { return m_nPointsZ; } set { m_nPointsZ = value; } }

        public DfsItemInfo Clone()
        {
            DfsItemInfo dolly = new DfsItemInfo();
            dolly.axisEUMUnit = axisEUMUnit;
            dolly.axisEUMUnitString = axisEUMUnitString;
            dolly.dataType = dataType;
            dolly.dataValType = dataValType;
            dolly.dim = dim;
            dolly.DX = DX;
            dolly.DY = DY;
            dolly.DZ = DZ;
            dolly.EUMType = EUMType;
            dolly.EUMTypeString = EUMTypeString;
            dolly.EUMUnit = EUMUnit;
            dolly.EUMUnitString = EUMUnitString;
            dolly.fileInfoRef = fileInfoRef;
            dolly.Name = Name;
            dolly.nPointsX = nPointsX;
            dolly.nPointsY = nPointsY;
            dolly.nPointsZ = nPointsZ;
            dolly.sAxisType = sAxisType;
            dolly.XMinLimit = XMinLimit;
            dolly.YMinLimit = YMinLimit;
            dolly.ZMinLimit = ZMinLimit;
            if (staticDataFloat != null) { dolly.staticDataFloat = new float[staticDataFloat.Length]; for (int i = 0; i < staticDataFloat.Length; i++) dolly.staticDataFloat[i] = staticDataFloat[i]; }
            if (staticDataInt != null) { dolly.staticDataInt = new int[staticDataInt.Length]; for (int i = 0; i < staticDataInt.Length; i++) dolly.staticDataInt[i] = staticDataInt[i]; }
            dolly.staticVectorPtr = staticVectorPtr;
            return dolly;
        }

        //error handling
        private int _err(string msg)
        {
            return _err(msg, 1);
        }

        private int _err(string msg, int errNo)
        {
            fileInfoRef.errMsg += "(item)\t" + msg + "\r\n";
            return errNo;
        }

        public int ReadStaticData()
        {
            switch (dataType)
            {
                case DfsSimpleType.Float:
                    staticDataFloat = new float[TotNoPoints];
                    DfsDLLWrapper.dfsStaticGetData(staticVectorPtr, staticDataFloat);
                    break;
                case DfsSimpleType.Int:
                    staticDataInt = new int[TotNoPoints];
                    DfsDLLWrapper.dfsStaticGetData(staticVectorPtr, staticDataInt);
                    break;
                default:
                    return _err("Unsupported static datatype (" + dataType.ToString() + ")", -16);
            }
            DfsDLLWrapper.dfsStaticDestroy(ref staticVectorPtr);
            return 0;
        }

        public int ReadStatic(int staticItemNo)
        {
            DfsDLLWrapper.dfsFindItemStatic(fileInfoRef.pHeader, fileInfoRef.pFile, staticItemNo);

            staticVectorPtr = DfsDLLWrapper.dfsStaticRead(fileInfoRef.pFile);

            IntPtr sItem = DfsDLLWrapper.dfsItemS(staticVectorPtr);
            return Read(sItem);
        }

        public int Read(int itemNo)
        {
            IntPtr pItem = DfsDLLWrapper.dfsItemD(fileInfoRef.pHeader, itemNo);
            return Read(pItem);
        }

        public int Read(IntPtr pItem)
        {
            int eumT = 0, eumU = 0;
            DfsSimpleType dataT = DfsSimpleType.Int;
            DfsDLLWrapper.dfsGetItemInfo(pItem, out eumT, out EUMTypeString, out Name, out eumU, out EUMUnitString, out dataT);

            EUMType = (eumItem)eumT;
            EUMUnit = (eumUnit)eumU;
            dataType = (DfsSimpleType)dataT;

            //if (dataType != UfsSimpleType.UFS_FLOAT)return err("Only float dataType supported.");

            dim = DfsDLLWrapper.dfsGetItemDim(pItem);

            dataValType = DfsDLLWrapper.dfsGetItemValueType(pItem);

            sAxisType = (SpaceAxisType)DfsDLLWrapper.dfsGetItemAxisType(pItem);
            switch (sAxisType)
            {
                case SpaceAxisType.EqD0:
                    DfsDLLWrapper.dfsGetItemAxisEqD0(pItem, out eumU, out axisEUMUnitString);
                    nPointsX = 1;
                    break;
                case SpaceAxisType.EqD1:
                    DfsDLLWrapper.dfsGetItemAxisEqD1(pItem, out eumU, out axisEUMUnitString, out m_nPointsX, out XMinLimit, out DX);
                    break;
                case SpaceAxisType.EqD2:
                    DfsDLLWrapper.dfsGetItemAxisEqD2(pItem, out eumU, out axisEUMUnitString, out m_nPointsX, out m_nPointsY, out XMinLimit, out YMinLimit, out DX, out DY);
                    break;
                case SpaceAxisType.EqD3:
                    DfsDLLWrapper.dfsGetItemAxisEqD3(pItem, out eumU, out axisEUMUnitString, out m_nPointsX, out m_nPointsY, out m_nPointsZ, out XMinLimit, out YMinLimit, out ZMinLimit, out DX, out DY, out DZ);
                    break;
                default:
                    return _err("Unsupported space axis " + sAxisType.ToString());
            }

            axisEUMUnit = (eumUnit)eumU;
            return 0;
        }

        //write static data. NB: call DFSWrapper.dfsFileCreate before thsi function (or else there is no file to write data to)
        public int WriteStaticData()
        {
            switch (dataType)
            {
                case DfsSimpleType.Float:
                    DfsDLLWrapper.dfsStaticWrite(staticVectorPtr, fileInfoRef.pFile, staticDataFloat);
                    break;
                case DfsSimpleType.Int:
                    DfsDLLWrapper.dfsStaticWrite(staticVectorPtr, fileInfoRef.pFile, staticDataInt);
                    break;
                default:
                    return _err("Unsupported static datatype (" + dataType.ToString() + ")", -16);
            }

            DfsDLLWrapper.dfsStaticDestroy(ref staticVectorPtr);
            return 0;
        }

        public int WriteStatic(int itemNo)
        {
            staticVectorPtr = DfsDLLWrapper.dfsStaticCreate();

            IntPtr sItm = DfsDLLWrapper.dfsItemS(staticVectorPtr);
            int rc = Write(sItm); //write header information
            if (rc != 0) return rc;
            return 0;
        }

        public int Write(int itemNo)
        {
            IntPtr pItem = DfsDLLWrapper.dfsItemD(fileInfoRef.pHeader, itemNo);
            return Write(pItem);
        }

        public int Write(IntPtr pItem)
        {
            DfsDLLWrapper.dfsSetItemInfo(fileInfoRef.pHeader, pItem, (int)this.EUMType, this.Name, (int)this.EUMUnit, this.dataType);

            switch (this.sAxisType)
            {
                case SpaceAxisType.EqD0:
                    DfsDLLWrapper.dfsSetItemAxisEqD0(pItem, (int)axisEUMUnit);
                    break;
                case SpaceAxisType.EqD1:
                    DfsDLLWrapper.dfsSetItemAxisEqD1(pItem, (int)axisEUMUnit, nPointsX, XMinLimit, DX);
                    break;
                case SpaceAxisType.EqD2:
                    DfsDLLWrapper.dfsSetItemAxisEqD2(pItem, (int)axisEUMUnit, nPointsX, nPointsY, XMinLimit, YMinLimit, DX, DY);
                    break;
                case SpaceAxisType.EqD3:
                    DfsDLLWrapper.dfsSetItemAxisEqD3(pItem, (int)axisEUMUnit, nPointsX, nPointsY, nPointsZ, XMinLimit, YMinLimit, ZMinLimit, DX, DY, DZ);
                    break;
                default:
                    return _err("write does not support space axis " + sAxisType.ToString());
            }
            return 0;
        }

        public int TotNoPoints
        {
            get
            {
                int res = -1;
                /*if ( nPointsX > 0) res = (int)XMinLimit + nPointsX;
                if ( nPointsY > 0) res *= (int)YMinLimit + nPointsY;
                if ( nPointsZ > 0) res *= (int)ZMinLimit + nPointsZ;*/
                if (nPointsX > 0) res = nPointsX;
                if (nPointsY > 0) res *= nPointsY;
                if (nPointsZ > 0) res *= nPointsZ;
                return res;
            }
        }

        //return item data as a string
        public override string ToString()
        {
            return "Name = " + Name + "\r\n" +
                "EUMType = " + EUMType.ToString() + " (" + ((int)EUMType).ToString() + ")\r\n" +
                "EUMUnit = " + EUMUnit.ToString() + " (" + ((int)EUMUnit).ToString() + ")\r\n" +
                "EUMUnitString = " + EUMUnitString + "\r\n" +
                "EUMTypeString = " + EUMTypeString + "\r\n" +
                "sAxisType = " + sAxisType.ToString() + "\r\n" +
                "axisEUMUnit = " + axisEUMUnit.ToString() + "\r\n" +
                "axisEUMUnitString = " + axisEUMUnitString + "\r\n" +
                "dim = " + dim.ToString() + "\r\n" +
                "nPointsX = " + nPointsX.ToString() + "\r\n" +
                "nPointsY = " + nPointsY.ToString() + "\r\n" +
                "nPointsZ = " + nPointsZ.ToString() + "\r\n" +
                "XMinLimit = " + XMinLimit.ToString() + "\r\n" +
                "YMinLimit = " + YMinLimit.ToString() + "\r\n" +
                "ZMinLimit = " + ZMinLimit.ToString() + "\r\n" +
                "DX = " + DX.ToString() + "\r\n" +
                "DY = " + DY.ToString() + "\r\n" +
                "DZ = " + DZ.ToString() + "\r\n" +
                "dataType = " + dataType.ToString() + "\r\n" +
                "dataValType = " + dataValType.ToString() + "\r\n";
        }



    }

}