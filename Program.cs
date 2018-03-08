using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace DHI.Generic.NetCDF.MIKE
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args == null | args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new NetCDFClient());
                Form newForm = new NetCDFClient();

                const int SW_HIDE = 0;
                const int SW_SHOW = 5;
                var handle = GetConsoleWindow();
                
                // Hide console application
                ShowWindow(handle, SW_HIDE);
                newForm.ShowDialog();

            }
            else if (args[0] == "-auto" & args.Length == 2)
            {
                try
                {
                    XmlSerialiser xmlSerialiser = new XmlSerialiser();
                    if (System.IO.File.Exists(args[1]))
                    {
                        string xmlData = xmlSerialiser.ReadXMLFile(args[1]);
                        EngineSettings savedSettings = (EngineSettings)xmlSerialiser.DeserializeObject(xmlData, typeof(EngineSettings));
                        Console.WriteLine("Saved settings read successful... Running engine");
                        CommandEngine newEngine = new CommandEngine();
                        newEngine.InitEngine(savedSettings);
                        newEngine.AutoRun();
                        Console.WriteLine("Auto run successful!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Netcdf client autorun error: " + ex.Message);
                }
            }
            else if (args[0] == "-autoprefix" & args.Length == 2)
            {
                try
                {
                    XmlSerialiser xmlSerialiser = new XmlSerialiser();
                    if (System.IO.File.Exists(args[1]))
                    {
                        string xmlData = xmlSerialiser.ReadXMLFile(args[1]);
                        EngineSettings savedSettings = (EngineSettings)xmlSerialiser.DeserializeObject(xmlData, typeof(EngineSettings));
                        Console.WriteLine("Saved settings read successful... Running engine");
                        CommandEngine newEngine = new CommandEngine();
                        string[] files = System.IO.Directory.GetFiles(savedSettings.WorkingDirectory);
                        for (int fileCount = 0; fileCount < files.Length; fileCount++)
                        {
                            if (files[fileCount].StartsWith(savedSettings.WorkingDirectory + "\\" + savedSettings.InputFilePrefix))
                            {
                                for (int commandCount = 0; commandCount < savedSettings.Commands.Count; commandCount++)
                                {
                                    savedSettings.Commands[commandCount].InputFileName = files[fileCount];
                                    savedSettings.Commands[commandCount].OutputFileName = savedSettings.WorkingDirectory + "\\" + System.IO.Path.GetFileNameWithoutExtension(files[fileCount])
                                        + savedSettings.OutputFilePrefix + savedSettings.Commands[commandCount].OutputFileExtension;
                                    newEngine.InitEngine(savedSettings);
                                    newEngine.AutoRun();
                                }
                            }
                        }
                        Console.WriteLine("Auto run successful!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Netcdf client autorun with prefix error: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Option 1) Type -auto settingsfile.xml to read a specific NC/DFS file.");
                Console.WriteLine("Option 2) Type -autoprefix settingsfile.xml to use the file prefix within the settings file.");
            }
        }
    }
}
