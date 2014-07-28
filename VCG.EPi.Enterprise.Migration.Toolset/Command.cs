using System;
using System.Windows.Input;

namespace VCG.EPi.Enterprise.Migration.Toolset
{
    public static class Command
    {
        public static readonly RoutedUICommand ImportEPi6 = new RoutedUICommand("Imports EPiServer 5 or 6 data", "ImportSource", typeof(MainWindow));
        public static readonly RoutedUICommand ImportEPi7 = new RoutedUICommand("Imports EPiServer 7 data", "ImportTarget", typeof(MainWindow));
        public static readonly RoutedUICommand ExportClass = new RoutedUICommand("Exports to classes", "ExportClass", typeof(MainWindow));
        public static readonly RoutedUICommand ExportData = new RoutedUICommand("Exports to data file", "ExportData", typeof(MainWindow));
    }
}
