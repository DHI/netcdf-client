using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DHI.Generic.NetCDF.MIKE.Commands;

namespace DHI.Generic.NetCDF.MIKE
{
    public class CommandEngine
    {
        private EngineSettings _engineSettings;

        public void InitEngine(EngineSettings engineSettings)
        {
            try
            {
                _engineSettings = engineSettings;
            }
            catch (Exception ex)
            {
                throw new Exception("Initiate engine error: " + ex.Message);
            }
        }

        public void AutoRun()
        {
            try
            {
                if (_engineSettings.Commands != null)
                {
                    Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();

                    foreach (CommandSettings commandSet in _engineSettings.Commands)
                    {
                        Type type = assem.GetType("DHI.Generic.NetCDF.MIKE.Commands." + commandSet.CommandName);
                        iCommand autoRunCommand = (iCommand)assem.CreateInstance(type.Namespace + "." + type.Name);
                        autoRunCommand.Execute(commandSet);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("AutoRun error: " + ex.Message);
            }
        }

        public void RunCommand(iCommand newCommand, CommandSettings commandSet)
        {
            try
            {
                newCommand.Execute(commandSet);
            }
            catch (Exception ex)
            {
                throw new Exception("RunCommand error: " + ex.Message);
            }
        }
    }
}
