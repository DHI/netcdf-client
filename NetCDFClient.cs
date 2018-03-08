using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ucar.nc2;
using ucar.nc2.dataset;
using ucar.nc2.ncml;
using ucar.util;
using ucar;
using java;
using java.io;
using DHI.Generic.NetCDF.MIKE.Commands;
using DHI.Generic.MikeZero;
using System.Globalization;

namespace DHI.Generic.NetCDF.MIKE
{
    public partial class NetCDFClient : Form
    {

        private EngineSettings _saveSettings = null;
        private NetCdfUtilities _netcdfUtil = null;
        private DfsUtilities _dfsUtil = null;

        private List<string> _variableNames;
        private List<bool> _isVariableSelected;
        private List<int> _selectedEUMIndex;
        private List<bool> _hasSelectedEUM;
        private List<List<DHICFEntry>> _variableMappings;
        private bool _isNCFile;
        private bool _canPlot;
        private bool _hasImportSettings = false;

        public NetCDFClient()
        {
            InitializeComponent();
            _createCommands();
        }

        #region Step 1 Select Command

        private void _createCommands()
        {
            listBoxCommand.Items.Clear();
            List<Type> commandList = _getClasses(typeof(iCommand));
            foreach (Type commandType in commandList)
            {
                if (!commandType.IsInterface)
                {
                    object command = Activator.CreateInstance(commandType);
                    listBoxCommand.Items.Add(commandType.Name);
                }
            }
            listBoxCommand.Sorted = true;
        }

        private void buttonS1Next_Click(object sender, EventArgs e)
        {
            _gotoStep2(true);
        }

        private void listBoxCommand_DoubleClick(object sender, EventArgs e)
        {
            _gotoStep2(true);
        }

        private void listBoxCommand_SelectedIndexChanged(object sender, EventArgs e)
        {     
            List<Type> commandList = _getClasses(typeof(iCommand));
            bool hasDisplayed = false;
            foreach (Type commandType in commandList)
            {
                if (!commandType.IsInterface)
                {
                    if (listBoxCommand.SelectedItem.ToString() == commandType.Name)
                    {
                        object command = Activator.CreateInstance(commandType);
                        object commandDescription = commandType.InvokeMember("CommandDescription", System.Reflection.BindingFlags.InvokeMethod,
                            null, command, null);
                        richTextBoxCommDesc.Text = commandDescription.ToString();
                        hasDisplayed = true;
                    }
                    else if (hasDisplayed == false)
                        richTextBoxCommDesc.Text = "";
                }
            }
        }

        private void listBoxCommand_MouseDown(object sender, MouseEventArgs e)
        {
            if (_hasImportSettings && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                DialogResult result = MessageBox.Show("Do you want to discard saved changes in the settings file?", "Confirmation", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                {
                    _hasImportSettings = true;
                    for (int i = 0; i < listBoxCommand.Items.Count; i++)
                    {
                        if (listBoxCommand.Items[i].ToString() == _saveSettings.Commands[0].CommandName)
                        {
                            listBoxCommand.SelectedIndex = i;
                        }
                    }
                }
                else
                    _hasImportSettings = false;
            }
        }

        private void _gotoStep2(bool goToNextStep)
        {
            try
            {
                if (listBoxCommand.SelectedItem == null)
                {
                    throw new Exception("No command chosen.");
                }

                if (!_hasImportSettings)
                {

                    _saveSettings = new EngineSettings();
                    _saveSettings.Commands = new List<CommandSettings>();
                    CommandSettings newCommand = new CommandSettings();
                    newCommand.CommandName = listBoxCommand.SelectedItem.ToString().Split('-')[0].Trim();

                    List<Type> commandList = _getClasses(typeof(iCommand));
                    foreach (Type commandType in commandList)
                    {
                        if (!commandType.IsInterface)
                        {
                            if (commandType.Name == newCommand.CommandName)
                            {
                                object command = Activator.CreateInstance(commandType);
                                object commandInputFileExtension = commandType.InvokeMember("CommandInputFileExtension", System.Reflection.BindingFlags.InvokeMethod,
                                    null, command, null);
                                object commandOutputFileExtension = commandType.InvokeMember("CommandOutputFileExtension", System.Reflection.BindingFlags.InvokeMethod,
                                    null, command, null);
                                newCommand.InputFileExtension = commandInputFileExtension.ToString();
                                newCommand.OutputFileExtension = commandOutputFileExtension.ToString();
                                _canPlot = (bool)commandType.InvokeMember("CanPlot", System.Reflection.BindingFlags.InvokeMethod,
                                    null, command, null);
                            }
                        }
                    }

                    _saveSettings.Commands.Add(newCommand);
                    openFileDialog1.Filter = newCommand.InputFileExtension + " files|*" + newCommand.InputFileExtension;
                    saveFileDialog1.Filter = newCommand.OutputFileExtension + " files|*" + newCommand.OutputFileExtension;
                }

                if (_saveSettings.Commands[0].CommandName.EndsWith("ToDfs2") || _saveSettings.Commands[0].CommandName.EndsWith("ToDfs3"))
                {
                    groupBoxOverwriteOri.Enabled = true;
                    groupBoxGrid.Enabled = true;
                }
                else
                {
                    groupBoxOverwriteOri.Enabled = false;
                    groupBoxGrid.Enabled = false;
                }

                if (goToNextStep) _nextStep();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _enableDXDY()
        {
            textBoxDX.Enabled = true;
            textBoxDX.Text = "Auto";
            textBoxDY.Enabled = true;
            textBoxDY.Text = "Auto";
            textBoxNumXCells.Enabled = true;
            textBoxNumXCells.Text = "Auto";
            textBoxNumYCells.Enabled = true;
            textBoxNumYCells.Text = "Auto";
            textBoxNumZCells.Enabled = true;
            textBoxNumZCells.Text = "Auto";
            textBoxDZ.Enabled = true;
            textBoxMemBlock.Enabled = true;
            //groupBoxSubset.Enabled = false;
        }

        private void _disableDXDY()
        {
            textBoxDX.Enabled = false;
            textBoxDX.Text = "Auto";
            textBoxDY.Enabled = false;
            textBoxDY.Text = "Auto";
            textBoxNumXCells.Enabled = false;
            textBoxNumXCells.Text = "Auto";
            textBoxNumYCells.Enabled = false;
            textBoxNumYCells.Text = "Auto";
            textBoxNumZCells.Enabled = false;
            textBoxNumZCells.Text = "Auto";
            textBoxDZ.Enabled = false;
            textBoxMemBlock.Enabled = false;
            //groupBoxSubset.Enabled = true;
        }

        /// <summary>
        /// Get list of types in the calling assembly that inherits
        /// from a certain base type.
        /// </summary>
        /// <param name="baseType">The base type to check for.</param>
        /// <returns>A list of types that inherits from baseType.</returns>
        private static List<Type> _getClasses(Type baseType)
        {
            return System.Reflection.Assembly.GetCallingAssembly().GetTypes().Where(type => baseType.IsAssignableFrom(type)).ToList();
        }

        #endregion //Step 1

        #region Step 2 Select File

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            textBoxInputFile.Text = openFileDialog1.FileName;
            _hasImportSettings = false;
            _readInputFile(_hasImportSettings);
        }

        private void _readInputFile(bool hasImportSettings)
        {
            if (!String.IsNullOrEmpty(textBoxInputFile.Text))
            {
                if (!hasImportSettings) _initSaveSettings();

                if (System.IO.Path.GetExtension(textBoxInputFile.Text).StartsWith(".dfs"))
                {
                    _isNCFile = false;
                    bool readOk = _readDfsFile(textBoxInputFile.Text);
                    _isFile(readOk);
                    if (readOk)
                    {
                        _createDFSTreeView();
                        //_createVariablesCheckBoxList();
                        if (!hasImportSettings)
                        {
                            _saveSettings.Commands[0].TimeAxisName = "time";
                            _saveSettings.Commands[0].XAxisName = "lon";
                            _saveSettings.Commands[0].YAxisName = "lat";
                            _saveSettings.Commands[0].ZAxisName = "depth";
                        }
                    }
                }
                else //try to read as netcdf datasets (should work with thredds, opendab, etc)
                {
                    _isNCFile = true;
                    bool readOk = _readNCFile(textBoxInputFile.Text);
                    _isFile(readOk);
                    if (readOk)
                    {
                        _createNCTreeView();
                    }
                }
            }
        }

        private bool _readNCFile(string fileName)
        {
            try
            {
                _netcdfUtil = new NetCdfUtilities(textBoxInputFile.Text, checkBoxNotFile.Checked);
                if (_saveSettings.Commands[0].CommandName.Contains("Grib"))
                    _saveSettings.Commands[0].UseDataSet = true;
                else
                    _saveSettings.Commands[0].UseDataSet = checkBoxNotFile.Checked;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Read NetCDF file error. " + ex.Message);
                return false;
            }
        }

        private bool _readDfsFile(string fileName)
        {
            try
            {
                _dfsUtil = new DfsUtilities();
                _dfsUtil.ReadDfsFile(fileName);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Read Dfs file error. " + ex.Message);
                return false;
            }
        }

        private void _isFile(bool yesOrNo)
        {
            label8.Visible = yesOrNo;
            treeViewNCFileInfo.Visible = yesOrNo;
        }

        private void _createNCTreeView()
        {
            treeViewNCFileInfo.Nodes.Clear();

            object[] ncDimensions = _netcdfUtil.GetDimensions();
            TreeNode[] dimTreeArr = new TreeNode[ncDimensions.Length];
            for (int i = 0; i < ncDimensions.Length; i++)
            {
                TreeNode newDimTreeNode = new TreeNode(((Dimension)ncDimensions[i]).toString());
                dimTreeArr[i] = newDimTreeNode;
            }
            TreeNode treeNode = new TreeNode("Dimensions [" + ncDimensions.Length.ToString() + "]", dimTreeArr);
            treeViewNCFileInfo.Nodes.Add(treeNode);

            object[] ncVariables = _netcdfUtil.GetVariables();
            TreeNode[] varTreeArr = new TreeNode[ncVariables.Length];
            for (int i = 0; i < ncVariables.Length; i++)
            {
                TreeNode newVarTreeNode = new TreeNode(((Variable)ncVariables[i]).toString());
                varTreeArr[i] = newVarTreeNode;
            }
            treeNode = new TreeNode("Variables [" + ncVariables.Length.ToString() + "]", varTreeArr);
            treeViewNCFileInfo.Nodes.Add(treeNode);

            object[] ncGlobalAtt = _netcdfUtil.GetGlobalAttributes();
            TreeNode[] gAttTreeArr = new TreeNode[ncGlobalAtt.Length];
            for (int i = 0; i < ncGlobalAtt.Length; i++)
            {
                TreeNode newgAttTreeNode = new TreeNode(((ucar.nc2.Attribute)ncGlobalAtt[i]).toString());
                gAttTreeArr[i] = newgAttTreeNode;
            }
            treeNode = new TreeNode("Global Attributes [" + ncGlobalAtt.Length.ToString() + "]", gAttTreeArr);
            treeViewNCFileInfo.Nodes.Add(treeNode);
        }

        private void _createDFSTreeView()
        {
            treeViewNCFileInfo.Nodes.Clear();
            TreeNode treeNode;
            List<TreeNode> gAttTreeArr = new List<TreeNode>();
            gAttTreeArr.Add(new TreeNode("File Name = " + _dfsUtil.FileName));
            gAttTreeArr.Add(new TreeNode("DfsFileType = " + _dfsUtil.dfsFileType));
            gAttTreeArr.Add(new TreeNode("Compressed = " + _dfsUtil.compressed));
            gAttTreeArr.Add(new TreeNode("Delete Value = " + _dfsUtil.delVal));
            gAttTreeArr.Add(new TreeNode("File Title = " + _dfsUtil.FileTitle));
            gAttTreeArr.Add(new TreeNode("Stat Type = " + _dfsUtil.statType));
            gAttTreeArr.Add(new TreeNode("Projection Type = " + _dfsUtil.Projection_type));
            gAttTreeArr.Add(new TreeNode("Projection = " + _dfsUtil.Projection));
            gAttTreeArr.Add(new TreeNode("Orientation = " + _dfsUtil.Orientation));
            gAttTreeArr.Add(new TreeNode("Start DateTime = " + _dfsUtil.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
            gAttTreeArr.Add(new TreeNode("Time Steps Count = " + _dfsUtil.tAxis_nTSteps));

            treeNode = new TreeNode("Headers [" + gAttTreeArr.Count() + "]", gAttTreeArr.ToArray());
            treeViewNCFileInfo.Nodes.Add(treeNode);

            TreeNode[] varTreeArr = new TreeNode[_dfsUtil.Items.Count()];
            for (int i = 0; i < _dfsUtil.Items.Count(); i++)
            {
                TreeNode newVarTreeNode = new TreeNode(_dfsUtil.Items[i].Name);
                varTreeArr[i] = newVarTreeNode;
            }
            treeNode = new TreeNode("Items [" + _dfsUtil.Items.Count() + "]", varTreeArr);
            treeViewNCFileInfo.Nodes.Add(treeNode);
          
        }

        private void buttonS2Next_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(textBoxInputFile.Text))
            {
                if (_isNCFile)
                {
                    if (!_hasImportSettings)
                    {
                        _populateComboBoxDimensions();
                        _populateSubsets();
                    }
                    _setTab3ToValid();
                    _nextStep();
                }
                else
                {
                    _setTab3ToInValid();
                    if (!_hasImportSettings)
                    {
                        _createVariablesCheckBoxList();
                    }
                    tabControl1.SelectedTab = tabPageS4;
                }
            }
            else
                MessageBox.Show("Input file does not exist.");

        }

        private void _setTab3ToInValid()
        {
            groupBoxDimensions.Enabled = false;
            groupBoxSubset.Enabled = false;
        }

        private void _setTab3ToValid()
        {
            groupBoxDimensions.Enabled = true;
            groupBoxSubset.Enabled = true;
        }

        private void buttonS2Back_Click(object sender, EventArgs e)
        {
            _previousStep();
        }

        private void textBoxInputFile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (int)Keys.Return)
            {
                if (!String.IsNullOrEmpty(textBoxInputFile.Text))
                {
                    _initSaveSettings();

                    if (System.IO.Path.GetExtension(textBoxInputFile.Text).StartsWith(".dfs"))
                    {
                        _isNCFile = false;
                        bool readOk = _readDfsFile(textBoxInputFile.Text);
                        _isFile(readOk);
                        if (readOk)
                        {
                            _createDFSTreeView();
                            //_createVariablesCheckBoxList();

                            _saveSettings.Commands[0].TimeAxisName = "time";
                            _saveSettings.Commands[0].XAxisName = "lon";
                            _saveSettings.Commands[0].YAxisName = "lat";
                            _saveSettings.Commands[0].ZAxisName = "depth";
                        }
                    }
                    else //try to read as netcdf datasets
                    {
                        _isNCFile = true;
                        bool readOk = _readNCFile(textBoxInputFile.Text);
                        _isFile(readOk);
                        if (readOk)
                        {
                            _createNCTreeView();
                        }
                    }
                }
            }
        }

        #endregion //Step 2

        #region Step 3 Set Dimensions

        private void _populateComboBoxDimensions()
        {

            object[] ncDims = _netcdfUtil.GetDimensions();
            object[] ncVars = _netcdfUtil.GetVariables();
            comboBoxX.Items.Clear();
            comboBoxY.Items.Clear();
            comboBoxZ.Items.Clear();
            comboBoxTime.Items.Clear();

            comboBoxX.Items.Add("None");
            comboBoxY.Items.Add("None");
            comboBoxZ.Items.Add("None");
            comboBoxTime.Items.Add("None");

            //remove dimensions and use only variables. dimensions will be automatically added
            /*foreach (object ncDim in ncDims)
            {
                ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                comboBoxX.Items.Add(dim.getName());
                comboBoxY.Items.Add(dim.getName());
                comboBoxZ.Items.Add(dim.getName());
                comboBoxTime.Items.Add(dim.getName());
            }*/

            foreach (object ncVar in ncVars)
            {
                ucar.nc2.Variable var = (ucar.nc2.Variable)ncVar;
                foreach (object ncDim in ncDims)
                {
                    ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                    if (dim.getName() != var.getFullName())
                    {
                        bool doesExist = false;
                        foreach (string existingName in comboBoxX.Items)
                        {
                            if (var.getFullName() == existingName) doesExist = true;
                        }
                        if (!doesExist) comboBoxX.Items.Add(var.getFullName());
                    }
                }
                foreach (object ncDim in ncDims)
                {
                    ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                    if (dim.getName() != var.getFullName())
                    {
                        bool doesExist = false;
                        foreach (string existingName in comboBoxY.Items)
                        {
                            if (var.getFullName() == existingName) doesExist = true;
                        }
                        if (!doesExist) comboBoxY.Items.Add(var.getFullName());
                    }
                }
                foreach (object ncDim in ncDims)
                {
                    ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                    if (dim.getName() != var.getFullName())
                    {
                        bool doesExist = false;
                        foreach (string existingName in comboBoxZ.Items)
                        {
                            if (var.getFullName() == existingName) doesExist = true;
                        }
                        if (!doesExist) comboBoxZ.Items.Add(var.getFullName());
                    }
                }
                foreach (object ncDim in ncDims)
                {
                    ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                    if (dim.getName() != var.getFullName())
                    {
                        bool doesExist = false;
                        foreach (string existingName in comboBoxTime.Items)
                        {
                            if (var.getFullName() == existingName) doesExist = true;
                        }
                        if (!doesExist) comboBoxTime.Items.Add(var.getFullName());
                    }
                }
            }

            //find lon
            CFMapping cfmap = new CFMapping();
            List<string> searchWords = new List<string>();
            string foundWord;
            for (int i = 0; i < comboBoxX.Items.Count; i++)
            {
                searchWords.Add(comboBoxX.Items[i].ToString());
            }
            int index = cfmap.Search("longitude", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("lon", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("x", searchWords, 0.7, out foundWord);
            for (int i = 0; i < comboBoxX.Items.Count; i++)
            {
                if (foundWord == comboBoxX.Items[i].ToString())
                    comboBoxX.SelectedIndex = i;
            }

            //find lat
            searchWords = new List<string>();
            for (int i = 0; i < comboBoxY.Items.Count; i++)
            {
                searchWords.Add(comboBoxY.Items[i].ToString());
            }
            index = cfmap.Search("latitude", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("lat", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("y", searchWords, 0.7, out foundWord);
            for (int i = 0; i < comboBoxY.Items.Count; i++)
            {
                if (foundWord == comboBoxY.Items[i].ToString())
                    comboBoxY.SelectedIndex = i;
            }

            //find depth 
            searchWords = new List<string>();
            for (int i = 0; i < comboBoxZ.Items.Count; i++)
            {
                searchWords.Add(comboBoxZ.Items[i].ToString());
            }
            index = cfmap.Search("depth", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("dep", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("z", searchWords, 0.7, out foundWord);

            //added: 20-09-2016 filter for dfs1 option to set depth to "none"
            if (_saveSettings.Commands[0].CommandName.ToLower().Contains("dfs1"))
                index = cfmap.Search("None", searchWords, 0.7, out foundWord);

            for (int i = 0; i < comboBoxZ.Items.Count; i++)
            {
                if (foundWord == comboBoxZ.Items[i].ToString())
                    comboBoxZ.SelectedIndex = i;
            }
            

            //find time
            searchWords = new List<string>();
            for (int i = 0; i < comboBoxTime.Items.Count; i++)
            {
                searchWords.Add(comboBoxTime.Items[i].ToString());
            }
            index = cfmap.Search("time", searchWords, 0.7, out foundWord);
            if (foundWord == "None") index = cfmap.Search("dt", searchWords, 0.7, out foundWord);
            for (int i = 0; i < comboBoxTime.Items.Count; i++)
            {
                if (foundWord == comboBoxTime.Items[i].ToString())
                    comboBoxTime.SelectedIndex = i;
            }
        }

        private void _populateSubsets()
        {
            object[] ncDims = _netcdfUtil.GetDimensions();
            bool hasFoundDataSet = false;
            foreach (object ncDim in ncDims)
            {
                ucar.nc2.Dimension dim = (ucar.nc2.Dimension)ncDim;
                if (dim.getName() == comboBoxX.Text)
                {
                    textBoxSubX.Text = "0:" + (dim.getLength() - 1).ToString();
                    hasFoundDataSet = true;
                }
                else if (!hasFoundDataSet)
                    textBoxSubX.Text = "Not an axis dataset";
                if (dim.getName() == comboBoxY.Text)
                {
                    textBoxSubY.Text = "0:" + (dim.getLength()-1).ToString();
                    hasFoundDataSet = true;
                }
                else if (!hasFoundDataSet)
                    textBoxSubY.Text = "Not an axis dataset";
            }
        }

        private void _setDimensions()
        {
            try
            {
                if (comboBoxX.Text == "None" || comboBoxX.Text == "")
                    _saveSettings.Commands[0].XAxisName = null;
                else
                {
                    _saveSettings.Commands[0].XAxisName = comboBoxX.Text;
                    _saveSettings.Commands[0].XAxisDimensionName = comboBoxX.Text;
                }

                if (comboBoxY.Text == "None" || comboBoxY.Text == "")
                    _saveSettings.Commands[0].YAxisName = null;
                else
                {
                    _saveSettings.Commands[0].YAxisName = comboBoxY.Text;
                    _saveSettings.Commands[0].YAxisDimensionName = comboBoxY.Text;
                }

                if (comboBoxZ.Text == "None" || comboBoxZ.Text == "")
                    _saveSettings.Commands[0].ZAxisName = null;
                else
                {
                    _saveSettings.Commands[0].ZAxisName = comboBoxZ.Text;
                    _saveSettings.Commands[0].ZAxisDimensionName = comboBoxZ.Text;
                }

                if (comboBoxTime.Text == "None" || comboBoxTime.Text == "")
                    _saveSettings.Commands[0].TimeAxisName = null;
                else
                {
                    _saveSettings.Commands[0].TimeAxisName = comboBoxTime.Text;
                    _saveSettings.Commands[0].TimeAxisDimensionName = comboBoxTime.Text;
                }

                _saveSettings.Commands[0].MZMapProjectionString = textBoxProj.Text;
                _saveSettings.Commands[0].ProjectionEastNorthMultiplier = textBoxENMultiplier.Text;
                _saveSettings.Commands[0].XLayer = textBoxSubX.Text;
                _saveSettings.Commands[0].YLayer = textBoxSubY.Text;

                if (textBoxOriX.Text != "" && textBoxOriY.Text != "" && textBoxRotation.Text != "")
                {
                    _saveSettings.Commands[0].OverwriteOriginX = Convert.ToDouble(textBoxOriX.Text, CultureInfo.InvariantCulture);
                    _saveSettings.Commands[0].OverwriteOriginY = Convert.ToDouble(textBoxOriY.Text, CultureInfo.InvariantCulture);
                    _saveSettings.Commands[0].OverwriteRotation = Convert.ToDouble(textBoxRotation.Text, CultureInfo.InvariantCulture);
                }
                else
                {
                    _saveSettings.Commands[0].OverwriteOriginX = -999;
                    _saveSettings.Commands[0].OverwriteOriginY = -999;
                    _saveSettings.Commands[0].OverwriteRotation = -999;
                }
                _saveSettings.Commands[0].MaxBlockSizeMB = Convert.ToInt32(textBoxMemBlock.Text, CultureInfo.InvariantCulture);
                _saveSettings.Commands[0].TimeStepSeconds = Convert.ToInt32(textBoxTimeStepSec.Text, CultureInfo.InvariantCulture);
                if (textBoxDZ.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].DZ = (float)Convert.ToDouble(textBoxDZ.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].DZ = 0;

                //adding DX and DY to settings
                if (textBoxDX.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].DX = (float)Convert.ToDouble(textBoxDX.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].DX = 0;

                if (textBoxDY.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].DY = (float)Convert.ToDouble(textBoxDY.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].DY = 0;

                if (textBoxNumXCells.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].NumberXCells = Convert.ToInt32(textBoxNumXCells.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].NumberXCells = 0;

                if (textBoxNumYCells.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].NumberYCells = Convert.ToInt32(textBoxNumYCells.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].NumberYCells = 0;

                if (textBoxNumZCells.Text.ToLower() != "auto")
                    _saveSettings.Commands[0].NumberZCells = Convert.ToInt32(textBoxNumZCells.Text, CultureInfo.InvariantCulture);
                else
                    _saveSettings.Commands[0].NumberZCells = 0;
            }
            catch (Exception ex)
            {
                throw new Exception ("_setDimensions Error: " + ex.Message);
            }
        }

        private void _createVariablesCheckBoxList()
        {
            checkedListBoxFileItems.Items.Clear();
            _variableMappings = new List<List<DHICFEntry>>();
            _variableNames = new List<string>();
            _isVariableSelected = new List<bool>();
            _selectedEUMIndex = new List<int>();
            _hasSelectedEUM = new List<bool>();

            if (!_hasImportSettings)
            {
                //if (_saveSettings.Commands[0].VariablesMappings == null)
                    _initSaveSettings();
            }

            if (_isNCFile)
            {
                
                object[] ncVariables = _netcdfUtil.GetVariables();

                //check how many variables are selected as axes
                int varFilterCount = 0;
                if (_saveSettings.Commands[0].XAxisDimensionName != null) varFilterCount++;
                if (_saveSettings.Commands[0].YAxisDimensionName != null) varFilterCount++;
                if (_saveSettings.Commands[0].ZAxisDimensionName != null) varFilterCount++;
                if (_saveSettings.Commands[0].TimeAxisDimensionName != null) varFilterCount++;

                
                for (int i = 0; i < ncVariables.Length; i++)
                {
                    java.util.List ncDimensions = ((Variable)ncVariables[i]).getDimensions();
                    
                    int dimCount = 0;
                    for (int j = 0; j < ncDimensions.size(); j++)
                    {
                        if (((Dimension)ncDimensions.get(j)).getName()== _saveSettings.Commands[0].XAxisDimensionName) dimCount++;
                        if (((Dimension)ncDimensions.get(j)).getName() == _saveSettings.Commands[0].YAxisDimensionName) dimCount++;
                        if (((Dimension)ncDimensions.get(j)).getName() == _saveSettings.Commands[0].ZAxisDimensionName) dimCount++;
                        if (((Dimension)ncDimensions.get(j)).getName() == _saveSettings.Commands[0].TimeAxisDimensionName) dimCount++;
                    }

                    if (dimCount == varFilterCount && dimCount == ncDimensions.size())
                    {
                        checkedListBoxFileItems.Items.Add(((Variable)ncVariables[i]).getFullName());

                        
                            _variableNames.Add(((Variable)ncVariables[i]).getFullName());
                            if (!_hasImportSettings)
                            {
                                _saveSettings.Commands[0].VariablesMappings.Add(new DHICFEntry());
                            }

                            CFMapping cfMap = new CFMapping();
                            List<DHICFEntry> closestDHIEUMs = cfMap.FindDHIEums(((Variable)ncVariables[i]).getFullName(), 0.7);
                            if (closestDHIEUMs.Count > 0)
                                _variableMappings.Add(closestDHIEUMs);
                            else
                            {
                                _variableMappings.Add(new List<DHICFEntry>());
                            }
                            _isVariableSelected.Add(false);
                            _selectedEUMIndex.Add(0);
                            _hasSelectedEUM.Add(false);
                        
                    }
                }                
            }
            else
            {
                CFMapping cfMap = new CFMapping();
                for (int i = 0; i < _dfsUtil.Items.Count(); i++)
                {
                    checkedListBoxFileItems.Items.Add(_dfsUtil.Items[i].Name);


                    _variableNames.Add(_dfsUtil.Items[i].Name);
                    if (!_hasImportSettings)
                    {
                        _saveSettings.Commands[0].VariablesMappings.Add(new DHICFEntry());
                    }

                    List<DHICFEntry> closestDHIEUMs = new List<DHICFEntry>();
                    closestDHIEUMs = cfMap.FindDHIEums(_dfsUtil.Items[i].Name, 0.3);

                    if (closestDHIEUMs.Count > 0)
                    {
                        List<DHICFEntry> foundEntries = new List<DHICFEntry>();
                        for (int j = 0; j < closestDHIEUMs.Count; j++)
                        {
                            List<DHICFEntry> currentfoundEntries = cfMap.FindClosestCFStandards(closestDHIEUMs[j].EUMItemDesc, 0.3);
                            for (int k = 0; k < currentfoundEntries.Count; k++)
                            {
                                if (checkExists(currentfoundEntries[k], foundEntries))
                                    foundEntries.Add(currentfoundEntries[k]);
                            }
                        }
                        _variableMappings.Add(foundEntries);
                    }
                    else
                    { _variableMappings.Add(new List<DHICFEntry>()); }
                    _isVariableSelected.Add(false);
                    _selectedEUMIndex.Add(0);
                    _hasSelectedEUM.Add(false);

                }
            }

            
            
        }

        private bool checkExists(DHICFEntry currentFoundEntry, List<DHICFEntry> foundEntries)
        {
            for (int i = 0; i < foundEntries.Count; i++)
            {
                if (foundEntries[i].CFStandardName == currentFoundEntry.CFStandardName)
                    return true;
            }

            return false;
        }

        private void _initSaveSettings()
        {
            try
            {
                _saveSettings.WorkingDirectory = System.IO.Path.GetDirectoryName(textBoxInputFile.Text);
                _saveSettings.InputFileExtension = System.IO.Path.GetExtension(textBoxInputFile.Text);
                _saveSettings.InputFilePrefix = System.IO.Path.GetFileNameWithoutExtension(textBoxInputFile.Text);

                if (_saveSettings.InputFileExtension.StartsWith(".dfs") || _saveSettings.Commands[0].CommandName.Contains("Trans") || _saveSettings.Commands[0].CommandName.StartsWith("Mercator_ConvertNc"))
                    groupBoxSubset.Enabled = false;
                else
                    groupBoxSubset.Enabled = true;

                _saveSettings.Commands[0].InputFileName = textBoxInputFile.Text;
                _saveSettings.Commands[0].VariablesMappings = new List<DHICFEntry>();
                _saveSettings.Commands[0].Variables = new List<string>();
                _saveSettings.Commands[0].IsVariablesSelected = new List<bool>();
            }
            catch (Exception ex)
            {
                //throw new Exception("Init. save settings error. " + ex.Message);
            }
        }

        private void buttonS3Next_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(textBoxInputFile.Text))
            {
                if (_isNCFile)
                {
                    if (!_hasImportSettings)
                    {
                        _createVariablesCheckBoxList();
                    }
                    _setDimensions();
                    _nextStep();

                    if (_saveSettings.Commands[0].CommandName.Contains("ConvertNcToDfs2Trans"))// || _saveSettings.Commands[0].CommandName == "ARPAL_ConvertNcToDfs2Trans" || _saveSettings.Commands[0].CommandName == "Hycom_ConvertNcToDfs2Trans")
                        _createTransectTable();
                    else
                    {
                        groupBoxTransect.Enabled = false;
                        dataGridViewTP.DataSource = null;
                    }

                    if (checkedListBoxFileItems.Items.Count > 0)
                    {
                        if (!_hasImportSettings)
                        {
                            checkedListBoxFileItems.SetItemChecked(0, true);
                            checkedListBoxFileItems.SelectedIndex = 0;
                        }
                    }
                    else
                        MessageBox.Show("No variables are found with the user defined axes (Step 3).");
                }
            }
            else
                MessageBox.Show("Input file does not exist.");
        }

        private void buttonS3Back_Click(object sender, EventArgs e)
        {
            if (_isNCFile)
            {
                _previousStep();
            }
        }

        private void checkBoxProj_CheckedChanged(object sender, EventArgs e)
        {
            textBoxProj.Enabled = !checkBoxProj.Checked;
            textBoxENMultiplier.Enabled = !checkBoxProj.Checked;
            textBoxOriX.Enabled = !checkBoxProj.Checked;
            textBoxOriY.Enabled = !checkBoxProj.Checked;
            textBoxRotation.Enabled = !checkBoxProj.Checked;

            if (checkBoxProj.Checked)
            {
                textBoxProj.Text = "LONG/LAT";
                textBoxENMultiplier.Text = "";
                textBoxOriX.Text = "";
                textBoxOriY.Text = "";
                textBoxRotation.Text = "";
            }
        }

        private void checkBoxTimeStep_CheckedChanged(object sender, EventArgs e)
        {
            textBoxTimeStepSec.Enabled = !checkBoxTimeStep.Checked;
            if (checkBoxTimeStep.Checked)
                textBoxTimeStepSec.Text = "86400";
        }

        private void comboBoxX_TextChanged(object sender, EventArgs e)
        {
            _populateSubsets();
        }

        private void comboBoxY_TextChanged(object sender, EventArgs e)
        {
            _populateSubsets();
        }

        #endregion //Step 3

        #region Step 4 Select Variables

        private void _setVariableMappings(int varIndex, int eumIndex, int eumUnitIndex)
        {
            try
            {
                _saveSettings.Commands[0].Variables = _variableNames;
                _saveSettings.Commands[0].IsVariablesSelected = _isVariableSelected;
                if (_isNCFile)
                {
                    _variableMappings[varIndex][eumIndex].EUMMappedItemUnitKey = _variableMappings[varIndex][eumIndex].EUMItemUnitKeys[eumUnitIndex];
                    _variableMappings[varIndex][eumIndex].EUMMappedItemUnitDesc = _variableMappings[varIndex][eumIndex].EUMItemUnitDesc[eumUnitIndex];
                    _saveSettings.Commands[0].VariablesMappings[varIndex] = _variableMappings[varIndex][eumIndex];
                }
                else
                {
                    _saveSettings.Commands[0].VariablesMappings[varIndex] = _variableMappings[varIndex][eumIndex];
                }
                
                try
                {
                    _saveSettings.Commands[0].TransectSpaceStepsNumber = Convert.ToInt32(textBoxTSSize.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot convert " + textBoxTSSize.Text + " to an integer." + ex.Message);
                }
            }
            catch
            {
 
            }
        }

        private void _createEums(int varIndex)
        {
            try
            {
                _checkMike();
                CFMapping cfMap = new CFMapping();

                if (_variableMappings[varIndex].Count > 0) //found mapped entry
                {
                    comboBoxEUM.DataSource = null;
                    comboBoxEUM.Items.Clear();

                    if (_isNCFile)
                    {
                        for (int i = 0; i < _variableMappings[varIndex].Count; i++)
                        {
                            comboBoxEUM.Items.Add(_variableMappings[varIndex][i].EUMItemDesc);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _variableMappings[varIndex].Count; i++)
                        {
                            comboBoxEUM.Items.Add(_variableMappings[varIndex][i].CFStandardName);
                        }
                    }
                    comboBoxEUM.SelectedIndex = _selectedEUMIndex[varIndex];
                }
                else
                {
                    if (_isNCFile)
                    {
                        List<string> wordList = new List<string>();
                        DHICFMapping newMap = new DHICFMapping();

                        List<DHICFEntry> allEntries = cfMap.GetAllDHIEum();
                        foreach (DHICFEntry newEntry in allEntries)
                        {
                            _variableMappings[varIndex].Add(newEntry);
                            _variableMappings[varIndex].Sort((x, y) => string.Compare(x.EUMItemDesc, y.EUMItemDesc));
                            wordList.Add(newEntry.EUMItemDesc);
                            wordList.Sort((x, y) => string.Compare(x, y));
                        }

                        comboBoxEUM.DataSource = wordList;
                        comboBoxEUM.SelectedIndex = _selectedEUMIndex[varIndex];
                        string foundWord;
                        int index = cfMap.Search(checkedListBoxFileItems.Items[varIndex].ToString(), wordList, 0.6, out foundWord);
                        for (int i = 0; i < comboBoxX.Items.Count; i++)
                        {
                            if (foundWord == comboBoxEUM.Items[i].ToString())
                            {
                                if (!_hasSelectedEUM[varIndex])
                                    comboBoxEUM.SelectedIndex = i;
                            }
                        }

                    }
                    else
                    {
                        List<DHICFEntry> foundEntries = cfMap.GetAllCFNames();
                        /*List<string> wordList = new List<string>();

                        foreach (DHICFEntry newEntry in foundEntries)
                        {
                            _variableMappings[varIndex].Add(newEntry);
                            _variableMappings[varIndex].Sort((x, y) => string.Compare(x.CFStandardName, y.CFStandardName));
                            wordList.Add(newEntry.CFStandardName);
                            wordList.Sort((x, y) => string.Compare(x, y));
                        }
                        comboBoxEUM.DataSource = wordList;*/
                        
                        if (foundEntries != null && foundEntries.Count > 0)
                        {
                            for (int j = 0; j < foundEntries.Count; j++)
                                comboBoxEUM.Items.Add(foundEntries[j].CFStandardName);
                        }
                        _variableMappings[varIndex] = foundEntries;

                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Variable mapping error: create EUM error. " + ex.Message);
            }
        }

        private void _createEumUnits(int varIndex, int eumIndex)
        {
            _checkMike();
            CFMapping cfMap = new CFMapping();
            try
            {
                comboBoxEUMUnit.Items.Clear();
                comboBoxEUMUnit.SelectedText = "";

                if (_isNCFile)
                {
                    for (int i = 0; i < _variableMappings[varIndex][eumIndex].EUMItemUnitDesc.Count; i++)
                    {
                        comboBoxEUMUnit.Items.Add(_variableMappings[varIndex][eumIndex].EUMItemUnitDesc[i]);
                    }
                }
                else
                {

                    comboBoxEUMUnit.Items.Add(_variableMappings[varIndex][eumIndex].CFStandardUnit);
                }
                comboBoxEUMUnit.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Variable mapping error: _creatEumUnits error. " + ex.Message);
            }
        }

        private void checkedListBoxFileItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            _createEums(checkedListBoxFileItems.SelectedIndex);
            if (!_hasImportSettings)
            {
                _isVariableSelected[checkedListBoxFileItems.SelectedIndex] = checkedListBoxFileItems.GetItemChecked(checkedListBoxFileItems.SelectedIndex);
                _setVariableMappings(checkedListBoxFileItems.SelectedIndex
                    , comboBoxEUM.SelectedIndex, comboBoxEUMUnit.SelectedIndex);
            }
        }

        private void comboBoxEUM_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                _createEumUnits(checkedListBoxFileItems.SelectedIndex, comboBoxEUM.SelectedIndex);
                _selectedEUMIndex[checkedListBoxFileItems.SelectedIndex] = comboBoxEUM.SelectedIndex;
                _hasSelectedEUM[checkedListBoxFileItems.SelectedIndex] = true;
                if (!_hasImportSettings)
                {
                    _isVariableSelected[checkedListBoxFileItems.SelectedIndex] = checkedListBoxFileItems.GetItemChecked(checkedListBoxFileItems.SelectedIndex);
                    _setVariableMappings(checkedListBoxFileItems.SelectedIndex
                        , comboBoxEUM.SelectedIndex, comboBoxEUMUnit.SelectedIndex);
                }
            }
            catch { }
        }

        private void buttonS4Next_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(textBoxInputFile.Text))
            {
                try
                {
                    _isVariableSelected[checkedListBoxFileItems.SelectedIndex] = checkedListBoxFileItems.GetItemChecked(checkedListBoxFileItems.SelectedIndex);
                    _setVariableMappings(checkedListBoxFileItems.SelectedIndex
                        , comboBoxEUM.SelectedIndex, comboBoxEUMUnit.SelectedIndex);
                    if (groupBoxTransect.Enabled)
                        _saveSettings.Commands[0].TransectPoints = ((BindingList<TransectPoint>)dataGridViewTP.DataSource).ToList();
                    _nextStep();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Step 4 Error. " + ex.Message);
                }
            }
            else
                MessageBox.Show("Input file does not exist.");
        }

        private void buttonS4Back_Click(object sender, EventArgs e)
        {
            if (_isNCFile)
            {
                _previousStep();
            }
        }

        private void _createTransectTable()
        {
            try
            {
                BindingList<TransectPoint> transectPoints = new BindingList<TransectPoint>();
                
                //for non mercator non structured grid nc files
                if (_saveSettings.Commands[0].NumberXCells == 0 && _saveSettings.Commands[0].NumberYCells == 0)
                {
                    object[] ncDims = _netcdfUtil.GetDimensions();
                    object xVar = null, yVar = null;

                    //cf standard
                    foreach (object ncDim in ncDims)
                    {
                        string dimName = ((ucar.nc2.Dimension)ncDim).getName();
                        if (_saveSettings.Commands[0].XAxisName == dimName)
                        {
                            xVar = (ucar.nc2.Variable)_netcdfUtil.GetVariable(dimName);
                        }
                        if (_saveSettings.Commands[0].YAxisName == dimName)
                        {
                            yVar = (ucar.nc2.Variable)_netcdfUtil.GetVariable(dimName);
                        }
                    }

                    //non cf standard
                    if (xVar == null && yVar == null)
                    {
                        object[] ncVars = _netcdfUtil.GetVariables();
                        foreach (object ncVar in ncVars)
                        {
                            string varName = ((ucar.nc2.Variable)ncVar).getFullName();
                            if (_saveSettings.Commands[0].XAxisName == varName)
                            {
                                xVar = (ucar.nc2.Variable)_netcdfUtil.GetVariable(varName);
                            }
                            if (_saveSettings.Commands[0].YAxisName == varName)
                            {
                                yVar = (ucar.nc2.Variable)_netcdfUtil.GetVariable(varName);
                            }
                        }
                    }
                    
                    ucar.ma2.Array xData = _netcdfUtil.GetAllVariableData(xVar);
                    ucar.ma2.Array yData = _netcdfUtil.GetAllVariableData(yVar);
                    int xCount = (int)xData.getSize();
                    int yCount = (int)yData.getSize();
                    transectPoints.Add(new TransectPoint(xData.getDouble(0), yData.getDouble(0)));
                    transectPoints.Add(new TransectPoint(xData.getDouble(xCount - 1), yData.getDouble(yCount - 1)));
                    textBoxTSSize.Enabled = true;
                }
                else //for mercator non structured grid files
                {
                    if (_saveSettings.Commands[0].DX == 0) throw new Exception("DX (Step 3) is empty or null");
                    if (_saveSettings.Commands[0].DY == 0) throw new Exception("DY (Step 3) is empty or null");

                    double lat0 = _saveSettings.Commands[0].OverwriteOriginY;
                    double lon0 = _saveSettings.Commands[0].OverwriteOriginX;

                    double dx, dy, j, k;

                    if (_saveSettings.Commands[0].DX > 0) dx = _saveSettings.Commands[0].DX;
                    else throw new Exception("Cannot convert " + _saveSettings.Commands[0].DX + " to double");
                    if (_saveSettings.Commands[0].DY > 0) dy = _saveSettings.Commands[0].DY; 
                    else throw new Exception("Cannot convert " + _saveSettings.Commands[0].DY + " to double");
                    if (_saveSettings.Commands[0].NumberXCells > 0) j = _saveSettings.Commands[0].NumberXCells;
                    else throw new Exception("Cannot convert " + _saveSettings.Commands[0].NumberXCells + " to double");
                    if (_saveSettings.Commands[0].NumberYCells > 0) k = _saveSettings.Commands[0].NumberYCells;
                    else throw new Exception("Cannot convert " + _saveSettings.Commands[0].NumberYCells + " to double");

                    double maxLon = lon0 + j * dx;
                    double maxLat = lat0 + k * dy;

                    transectPoints.Add(new TransectPoint(lon0, lat0));
                    transectPoints.Add(new TransectPoint(maxLon, maxLat));
                    textBoxTSSize.Text = (_saveSettings.Commands[0].NumberXCells-1).ToString();
                    textBoxTSSize.Enabled = false;
                }

                dataGridViewTP.DataSource = transectPoints;
                groupBoxTransect.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Create Transect table error. " + ex.Message);
            }

        }

        private void buttonPreBack_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPageS4;
        }

        #endregion //Step 4

        #region Step 5 Finish

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveSettingsFile = new SaveFileDialog();
                saveSettingsFile.InitialDirectory = saveFileDialog1.InitialDirectory;
                saveSettingsFile.Filter = "Settings File |*.xml";
                saveSettingsFile.ShowDialog();

                if (!System.String.IsNullOrEmpty(saveSettingsFile.FileName))
                {
                    _saveSettings.OutputFilePrefix = "_" + DateTime.Now.ToString("yyyyMMdd");
                    _saveSettings.Commands[0].OutputFileName = textBoxOutput.Text;

                    XmlSerialiser xmlSerialiser = new XmlSerialiser();
                    string xmlData = xmlSerialiser.SerializeObject(_saveSettings, typeof(EngineSettings));
                    _WaitReady(saveSettingsFile.FileName);
                    xmlSerialiser.WriteXMLFile(xmlData, saveSettingsFile.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Settings Error: " + ex.Message);
            }

        }

        private void buttonExecute_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(textBoxInputFile.Text))
            {
            try
            {
                _saveSettings.OutputFilePrefix = "_" + DateTime.Now.ToString("yyyyMMdd");
                _saveSettings.Commands[0].OutputFileName = textBoxOutput.Text;

                CommandEngine newEngine = new CommandEngine();
                newEngine.InitEngine(_saveSettings);
                newEngine.AutoRun();
                MessageBox.Show(_saveSettings.Commands[0].CommandName + " completed. " + _saveSettings.Commands[0].OutputFileName + " is ready.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Execute error. " + ex.Message);
            }
            }
            else
                MessageBox.Show("Input file does not exist.");
        }

        private void buttonBrowseOutput_Click(object sender, EventArgs e)
        {
            _hasImportSettings = false;
            if (String.IsNullOrEmpty(openFileDialog1.FileName))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;
            else
                saveFileDialog1.InitialDirectory = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
            saveFileDialog1.ShowDialog();
            textBoxOutput.Text = saveFileDialog1.FileName;
        }

        private void buttonS5Back_Click(object sender, EventArgs e)
        {
            _previousStep();
        }

        #endregion //Step 5

        private void buttonImportSettings_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openSettingsFile = new OpenFileDialog();
                openSettingsFile.InitialDirectory = saveFileDialog1.InitialDirectory;
                openSettingsFile.Filter = "Settings File |*.xml";
                openSettingsFile.ShowDialog();

                XmlSerialiser xmlSerialiser = new XmlSerialiser();
                string xmlData = xmlSerialiser.ReadXMLFile(openSettingsFile.FileName);
                _saveSettings = (EngineSettings)xmlSerialiser.DeserializeObject(xmlData, typeof(EngineSettings));
                this._hasImportSettings = true;

                //set up different steps
                #region Import settings Step 1
                //step 1
                for (int i = 0; i < listBoxCommand.Items.Count; i++)
                {
                    if (listBoxCommand.Items[i].ToString() == _saveSettings.Commands[0].CommandName)
                    {
                        listBoxCommand.SelectedIndex = i;
                    }
                }
                if (_saveSettings.Commands[0].CommandName.EndsWith("ToDfs2") || _saveSettings.Commands[0].CommandName.EndsWith("ToDfs3"))
                {
                    groupBoxOverwriteOri.Enabled = true;
                    groupBoxGrid.Enabled = true;
                    checkBoxMemoryBlockDefault.Checked = true;
                }
                else
                {
                    groupBoxOverwriteOri.Enabled = false;
                    groupBoxGrid.Enabled = false;
                }

                #endregion //step 1

                #region Import settings Step 2
                //step 2
                textBoxInputFile.Text = _saveSettings.Commands[0].InputFileName;
                _readInputFile(_hasImportSettings);
                #endregion //step 2

                #region Import settings step 3
                //step 3
                if (System.IO.File.Exists(textBoxInputFile.Text))
                {
                    if (_isNCFile)
                    {
                        _populateComboBoxDimensions();
                        _populateSubsets();
                        _setTab3ToValid();
                    }
                    else
                    {
                        _createVariablesCheckBoxList();
                        _setTab3ToInValid();
                    }
                }
                else
                    MessageBox.Show("Input file does not exist.");
                if (_saveSettings.Commands[0].XAxisName == null)
                    comboBoxX.Text = "None";
                else
                {
                    for (int i = 0; i < comboBoxX.Items.Count; i++)
                    {
                        if (comboBoxX.Items[i].ToString() == _saveSettings.Commands[0].XAxisName)
                            comboBoxX.SelectedIndex = i;
                    }
                    //comboBoxX.Text = _saveSettings.Commands[0].XAxisName;
                }

                if (_saveSettings.Commands[0].YAxisName == null)
                    comboBoxY.Text = "None";
                else
                {
                    for (int i = 0; i < comboBoxY.Items.Count; i++)
                    {
                        if (comboBoxY.Items[i].ToString() == _saveSettings.Commands[0].YAxisName)
                            comboBoxY.SelectedIndex = i;
                    }
                    //comboBoxY.Text = _saveSettings.Commands[0].YAxisName;
                }

                if (_saveSettings.Commands[0].ZAxisName == null)
                    comboBoxZ.Text = "None";
                else
                {
                    for (int i = 0; i < comboBoxZ.Items.Count; i++)
                    {
                        if (comboBoxZ.Items[i].ToString() == _saveSettings.Commands[0].ZAxisName)
                            comboBoxZ.SelectedIndex = i;
                    }
                    //comboBoxZ.Text = _saveSettings.Commands[0].ZAxisName;
                }

                if (_saveSettings.Commands[0].TimeAxisName == null)
                    comboBoxTime.Text = "None";
                else
                {
                    for (int i = 0; i < comboBoxTime.Items.Count; i++)
                    {
                        if (comboBoxTime.Items[i].ToString() == _saveSettings.Commands[0].TimeAxisName)
                            comboBoxTime.SelectedIndex = i;
                    }
                    //comboBoxTime.Text = _saveSettings.Commands[0].TimeAxisName;
                }

                textBoxProj.Text = _saveSettings.Commands[0].MZMapProjectionString;
                textBoxENMultiplier.Text = _saveSettings.Commands[0].ProjectionEastNorthMultiplier;
                textBoxSubX.Text = _saveSettings.Commands[0].XLayer;
                textBoxSubY.Text = _saveSettings.Commands[0].YLayer;

                if (_saveSettings.Commands[0].OverwriteOriginX != -999 && _saveSettings.Commands[0].OverwriteOriginY != -999 && _saveSettings.Commands[0].OverwriteRotation != -999)
                {
                    textBoxOriX.Text = _saveSettings.Commands[0].OverwriteOriginX.ToString(CultureInfo.InvariantCulture);
                    textBoxOriY.Text = _saveSettings.Commands[0].OverwriteOriginY.ToString(CultureInfo.InvariantCulture);
                    textBoxRotation.Text = _saveSettings.Commands[0].OverwriteRotation.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    textBoxOriX.Text = "";
                    textBoxOriY.Text = "";
                    textBoxRotation.Text = "";
                }

                textBoxMemBlock.Text = _saveSettings.Commands[0].MaxBlockSizeMB.ToString();
                textBoxTimeStepSec.Text = _saveSettings.Commands[0].TimeStepSeconds.ToString();

                if (_saveSettings.Commands[0].DZ == 0)
                    textBoxDZ.Text = "Auto";
                else 
                    textBoxDZ.Text = _saveSettings.Commands[0].DZ.ToString(CultureInfo.InvariantCulture);

                if (_saveSettings.Commands[0].DX == 0)
                    textBoxDX.Text = "Auto";
                else
                    textBoxDX.Text = _saveSettings.Commands[0].DX.ToString(CultureInfo.InvariantCulture);

                if (_saveSettings.Commands[0].DY == 0)
                    textBoxDY.Text = "Auto";
                else
                    textBoxDY.Text = _saveSettings.Commands[0].DY.ToString(CultureInfo.InvariantCulture);

                if (_saveSettings.Commands[0].NumberXCells == 0)
                    textBoxNumXCells.Text = "Auto";
                else
                    textBoxNumXCells.Text = _saveSettings.Commands[0].NumberXCells.ToString(CultureInfo.InvariantCulture);

                if (_saveSettings.Commands[0].NumberYCells == 0)
                    textBoxNumYCells.Text = "Auto";
                else
                    textBoxNumYCells.Text = _saveSettings.Commands[0].NumberYCells.ToString(CultureInfo.InvariantCulture);

                if (_saveSettings.Commands[0].NumberZCells == 0)
                    textBoxNumZCells.Text = "Auto";
                else
                    textBoxNumZCells.Text = _saveSettings.Commands[0].NumberZCells.ToString(CultureInfo.InvariantCulture);


                #endregion //step 3

                #region Import settings step 4
                //step 4
                _createVariablesCheckBoxList();
                _variableNames = _saveSettings.Commands[0].Variables;
                _isVariableSelected = _saveSettings.Commands[0].IsVariablesSelected;
                if (_isNCFile)
                {
                    for (int i = 0; i < _saveSettings.Commands[0].IsVariablesSelected.Count; i++)
                    {
                        if (_saveSettings.Commands[0].IsVariablesSelected[i]) 
                        {
                            checkedListBoxFileItems.SetItemChecked(i, true);
                            checkedListBoxFileItems.SelectedIndex = i;
                            for (int j = 0; j < comboBoxEUM.Items.Count; j++)
                            {
                                if (comboBoxEUM.Items[j].ToString() == _saveSettings.Commands[0].VariablesMappings[i].EUMItemDesc)
                                    comboBoxEUM.SelectedIndex = j;
                            }
                            for (int j = 0; j < comboBoxEUMUnit.Items.Count; j++)
                            {
                                if (comboBoxEUMUnit.Items[j].ToString() == _saveSettings.Commands[0].VariablesMappings[i].EUMMappedItemUnitDesc)
                                    comboBoxEUMUnit.SelectedIndex = j;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _saveSettings.Commands[0].IsVariablesSelected.Count; i++)
                    {
                        if (_saveSettings.Commands[0].IsVariablesSelected[i])
                        {
                            checkedListBoxFileItems.SetItemChecked(i, true);
                            checkedListBoxFileItems.SelectedIndex = i;
                            for (int j = 0; j < comboBoxEUM.Items.Count; j++)
                            {
                                if (comboBoxEUM.Items[j].ToString() == _saveSettings.Commands[0].VariablesMappings[i].CFStandardName)
                                    comboBoxEUM.SelectedIndex = j;
                            }
                            for (int j = 0; j < comboBoxEUMUnit.Items.Count; j++)
                            {
                                if (comboBoxEUMUnit.Items[j].ToString() == _saveSettings.Commands[0].VariablesMappings[i].CFStandardUnit)
                                    comboBoxEUMUnit.SelectedIndex = j;
                            }
                        }
                    }
                }

                if (_saveSettings.Commands[0].TransectPoints != null && _saveSettings.Commands[0].CommandName.Contains("ConvertNcToDfs2Trans"))
                {
                    dataGridViewTP.DataSource = _saveSettings.Commands[0].TransectPoints;
                    groupBoxTransect.Enabled = true;
                }
                else
                {
                    groupBoxTransect.Enabled = false;
                    dataGridViewTP.DataSource = null;
                }
                #endregion //step 4

                #region Import settings step 5
                //step 5
                textBoxOutput.Text = _saveSettings.Commands[0].OutputFileName;
                #endregion //step 5

            }
            catch (Exception ex)
            {
                MessageBox.Show("Import settings error. " + ex.Message);
                _hasImportSettings = false;
            }

        }

        private void _nextStep()
        {
            int selectedIndex = tabControl1.TabPages.IndexOf(tabControl1.SelectedTab);
            tabControl1.SelectedIndex = ++selectedIndex;
        }

        private void _previousStep()
        {
            int selectedIndex = tabControl1.TabPages.IndexOf(tabControl1.SelectedTab);
            tabControl1.SelectedIndex = --selectedIndex;
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

        private static void _WaitReady(string fileName)
        {
            while (true)
            {
                try
                {
                    using (Stream stream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        if (stream != null)
                        { System.Diagnostics.Trace.WriteLine(string.Format("Output file {0} ready.", fileName)); break; }
                    }
                }
                catch (System.IO.FileNotFoundException ex) { break; }
                catch (System.IO.IOException ex) { System.Diagnostics.Trace.WriteLine(string.Format("Output file {0} not yet ready ({1})", fileName, ex.Message)); }
                catch (UnauthorizedAccessException ex) { System.Diagnostics.Trace.WriteLine(string.Format("Output file {0} not yet ready ({1})", fileName, ex.Message)); }
                System.Threading.Thread.Sleep(500);
            }
        }

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.F1)
                Help.ShowHelp(this, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Quick Help.chm");
        }

        private void checkBoxMemoryBlockDefault_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMemoryBlockDefault.Checked)
            {
                textBoxDZ.Text = "Auto";
                textBoxMemBlock.Text = "10";
                _disableDXDY();
            }
            else
                _enableDXDY();
        }

        private void NetCDFClient_Load(object sender, EventArgs e)
        {

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = tabControl1.TabPages.IndexOf(tabControl1.SelectedTab);
            switch (selectedIndex)
            {
                case 0: //step 1
                    break;
                case 1: //step 2
                    if (!_hasImportSettings) _gotoStep2(false);
                    break;
                case 2: //step 3
                    if (System.IO.File.Exists(textBoxInputFile.Text))
                    {
                        if (_isNCFile)
                        {
                            if (!_hasImportSettings)
                            {
                                _populateComboBoxDimensions();
                                _populateSubsets();
                            }
                            _setTab3ToValid();
                        }
                        else
                        {
                            _setTab3ToInValid();
                            if (!_hasImportSettings)
                            {
                                _createVariablesCheckBoxList();
                            }
                        }
                    }
                    else
                        MessageBox.Show("Input file does not exist.");
                    break;
                case 3: //step 4
                    if (System.IO.File.Exists(textBoxInputFile.Text))
                    {
                        if (_isNCFile)
                        {
                            if (!_hasImportSettings)
                            {
                                _createVariablesCheckBoxList();
                            }
                            _setDimensions();
 
                            if (_saveSettings.Commands[0].CommandName.Contains("ConvertNcToDfs2Trans"))// || _saveSettings.Commands[0].CommandName == "ARPAL_ConvertNcToDfs2Trans" || _saveSettings.Commands[0].CommandName == "Hycom_ConvertNcToDfs2Trans")
                                _createTransectTable();
                            else
                            {
                                groupBoxTransect.Enabled = false;
                                dataGridViewTP.DataSource = null;
                            }
                        }
                    }
                    else
                        MessageBox.Show("Input file does not exist.");
                    break;
                case 4: //step 5
                    if (!_hasImportSettings)
                    {
                        if (System.IO.File.Exists(textBoxInputFile.Text))
                        {
                            try
                            {
                                _isVariableSelected[checkedListBoxFileItems.SelectedIndex] = checkedListBoxFileItems.GetItemChecked(checkedListBoxFileItems.SelectedIndex);
                                _setVariableMappings(checkedListBoxFileItems.SelectedIndex
                                    , comboBoxEUM.SelectedIndex, comboBoxEUMUnit.SelectedIndex);
                                if (groupBoxTransect.Enabled)
                                    _saveSettings.Commands[0].TransectPoints = ((BindingList<TransectPoint>)dataGridViewTP.DataSource).ToList();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Step 4 Error. " + ex.Message);
                            }
                        }
                        else
                            MessageBox.Show("Input file does not exist.");
                    }
                    break;

            }
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Quick Help.chm");
        }


    }

}


