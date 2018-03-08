using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DHI.Generic.NetCDF.MIKE
{
    public class EngineSettings
    {
        private string _inputFilePreFix, _inputFileExtension, _outputFilePrefix, _workingDirectory;
        private List<CommandSettings> _commandSettings;

        [CategoryAttribute("Instance Settings"), ReadOnlyAttribute(false)]
        public string InputFilePrefix
        {
            get { return _inputFilePreFix; }
            set { _inputFilePreFix = value; }
        }

        [CategoryAttribute("Instance Settings"), ReadOnlyAttribute(false)]
        public string InputFileExtension
        {
            get { return _inputFileExtension; }
            set { _inputFileExtension = value; }
        }

        [CategoryAttribute("Instance Settings"), ReadOnlyAttribute(false)]
        public string OutputFilePrefix
        {
            get { return _outputFilePrefix; }
            set { _outputFilePrefix = value; }
        }

        [CategoryAttribute("Instance Settings"), ReadOnlyAttribute(false)]
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { _workingDirectory = value; }
        }

        [CategoryAttribute("Instance Settings"), ReadOnlyAttribute(false)]
        public List<CommandSettings> Commands
        {
            get { return _commandSettings; }
            set { _commandSettings = value; }
        }
    }

    public class CommandSettings
    {

        private string _commandName, _inputFileName, _outputFileName, _xAxisName, _yAxisName, _zAxisName, _timeAxisName, _inputFileExtension, _outputFileExtension;
        private string _xAxisDimensionName, _yAxisDimensionName, _zAxisDimensionName, _timeAxisDimensionName;
        private int _zLayer;
        private int _timeLayer;
        private bool _useDataSet;
        private string _yLayer, _xLayer;
        private List<string> _selectedVariables;
        private List<DHICFEntry> _selectedVariablesMapping;
        private List<bool> _isVariablesSelected;
        private List<TransectPoint> _transectPoints;
        private string _mapProjectionString;
        private string _eastNorthMultiplier;
        private float _dz, _dx, _dy;
        private int _num_x_cells, _num_y_cells, _num_z_cells;
        private int _maxBlockSizeMB;
        private int _transectSpaceStepsNum;
        private int _timeStepSeconds;
        private double _overwriteOriginX;
        private double _overwriteOriginY;
        private double _overwriteRotation;


        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string CommandName
        {
            get { return _commandName; }
            set { _commandName = value; }
        }

        public bool UseDataSet
        {
            get { return _useDataSet; }
            set { _useDataSet = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string InputFileName
        {
            get { return _inputFileName; }
            set { _inputFileName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string InputFileExtension
        {
            get { return _inputFileExtension; }
            set { _inputFileExtension = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string OutputFileName
        {
            get { return _outputFileName; }
            set { _outputFileName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string OutputFileExtension
        {
            get { return _outputFileExtension; }
            set { _outputFileExtension = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int MaxBlockSizeMB
        {
            get { return _maxBlockSizeMB; }
            set { this._maxBlockSizeMB = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor(
       "System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
       "System.Drawing.Design.UITypeEditor, System.Drawing, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public List<string> Variables
        {
            get { return _selectedVariables; }
            set { _selectedVariables = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public List<DHICFEntry> VariablesMappings
        {
            get { return _selectedVariablesMapping; }
            set { _selectedVariablesMapping = value; }
        }

        public List<bool> IsVariablesSelected
        {
            get { return _isVariablesSelected; }
            set { _isVariablesSelected = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string XAxisName
        {
            get { return _xAxisName; }
            set { _xAxisName = value; }
        }
        
        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string XAxisDimensionName
        {
            get { return _xAxisDimensionName; }
            set { _xAxisDimensionName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string XLayer
        {
            get { return _xLayer; }
            set { _xLayer = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public double OverwriteOriginX
        {
            get { return _overwriteOriginX; }
            set { _overwriteOriginX = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public double OverwriteOriginY
        {
            get { return _overwriteOriginY; }
            set { _overwriteOriginY = value; }
        }
        
        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public double OverwriteRotation
        {
            get { return _overwriteRotation; }
            set { _overwriteRotation = value; }
        }


        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string YAxisName
        {
            get { return _yAxisName; }
            set { _yAxisName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string YAxisDimensionName
        {
            get { return _yAxisDimensionName; }
            set { _yAxisDimensionName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string YLayer
        {
            get { return _yLayer; }
            set { _yLayer = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string ZAxisName
        {
            get { return _zAxisName; }
            set { _zAxisName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string ZAxisDimensionName
        {
            get { return _zAxisDimensionName; }
            set { _zAxisDimensionName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int ZLayer
        {
            get { return _zLayer; }
            set { _zLayer = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string TimeAxisName
        {
            get { return _timeAxisName; }
            set { _timeAxisName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string TimeAxisDimensionName
        {
            get { return _timeAxisDimensionName; }
            set { _timeAxisDimensionName = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int TimeLayer
        {
            get { return _timeLayer; }
            set { _timeLayer = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public float DZ
        {
            get { return _dz; }
            set { _dz = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string MZMapProjectionString
        {
            get { return _mapProjectionString; }
            set { _mapProjectionString = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public string ProjectionEastNorthMultiplier
        {
            get { return _eastNorthMultiplier; }
            set { _eastNorthMultiplier = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public List<TransectPoint> TransectPoints
        {
            get { return _transectPoints; }
            set { _transectPoints = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int TransectSpaceStepsNumber
        {
            get { return _transectSpaceStepsNum; }
            set { _transectSpaceStepsNum = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int TimeStepSeconds
        {
            get { return _timeStepSeconds; }
            set { _timeStepSeconds = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int NumberXCells
        {
            get { return _num_x_cells; }
            set { _num_x_cells = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int NumberYCells
        {
            get { return _num_y_cells; }
            set { _num_y_cells = value; }
        }
        
        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public int NumberZCells
        {
            get { return _num_z_cells; }
            set { _num_z_cells = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public float DX
        {
            get { return _dx; }
            set { _dx = value; }
        }

        [CategoryAttribute("Command Settings"), ReadOnlyAttribute(false)]
        public float DY
        {
            get { return _dy; }
            set { _dy = value; }
        }
    }

    public class TransectPoint : INotifyPropertyChanged
    {
        private double _x;
        private double _y;

        public event PropertyChangedEventHandler PropertyChanged;

        public TransectPoint()
        {
            _x = 0;
            _y = 0;
        }

        public TransectPoint(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public double X
        {
            get { return _x; }
            set { _x = value;
            this.NotifyPropertyChanged("X");
            }
        }

        public double Y
        {
            get { return _y; }
            set { _y = value;
            this.NotifyPropertyChanged("Y");
            }
        }

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

    }
}
