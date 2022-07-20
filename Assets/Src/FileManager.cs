using System;
using System.Xml;
using AnotherFileBrowser.Windows;

namespace Src
{
    public class FileManager
    {
        #region FILE MANAGEMENT
        
        public XmlDocument OpenFileBrowserForLoad()
        {
            BrowserProperties browserProps = new BrowserProperties();
            browserProps.filter = "Config Files (*.xml)|*.xml";
            browserProps.filterIndex = 0;
            browserProps.initialDir = @"C:\Development\University\NeatGame\NeatSimulation Game\Assets"; // TODO: Once I build this is this path still valid? TODO:: This will cause a bug when running anywhere other than my computer.
            
            XmlDocument xmlConfig = new XmlDocument();

            new FileBrowser().OpenFileBrowser(browserProps, path  =>
            {
                // TODO: THere is an error here if the user doesnt select a file.
                xmlConfig = new XmlDocument();

                xmlConfig.Load(path);
            });
            
            return xmlConfig;
        }
        
        public string OpenFileBrowserForSave()
        {
            BrowserProperties browserProps = new BrowserProperties();
            browserProps.filter = "Config Files (*.xml)|*.xml";
            browserProps.filterIndex = 0;
            browserProps.initialDir = @"C:\Development\University\NeatGame\NeatSimulation Game\Assets"; // TODO: Once I build this is this path still valid? TODO:: This will cause a bug when running anywhere other than my computer.

            string pathToReturn = String.Empty;
            
            new FileBrowser().SaveFileBrowser(browserProps, path  =>
            {
                pathToReturn = path;
            });
            
            return pathToReturn;
        }
        
        #endregion
    }
}