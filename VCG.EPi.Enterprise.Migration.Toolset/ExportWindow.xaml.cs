using VCG.EPi.Enterprise.Migration.Toolset.Additions.Avalon;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VCG.EPi.Enterprise.Migration.Toolset
{
	/// <summary>
	/// Interaction logic for CustomPropertiesDetected.xaml
	/// </summary>
    public partial class ExportResultWindow : MetroWindow
	{
        public ExportResultWindow(string resultLog)
		{
			InitializeComponent();

            tbLog.TextArea.TextView.LineTransformers.Add(new LogLineColorizer());

            tbLog.Text = resultLog;

            if (!string.IsNullOrEmpty(tbLog.Text))
            {
                HideProgress();
            }                
            else
            {
                ShowCloseButton = false;
                btnOk.IsEnabled = false;
            }              
		}
		
        private void HideProgress()
        {
            pbProgress.Visibility = Visibility.Hidden;
            tbLog.Margin = new Thickness(tbLog.Margin.Left, tbLog.Margin.Top, tbLog.Margin.Right, 50);
        }

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
            // Reset taskbar icon if in progress
            if (Application.Current.MainWindow.TaskbarItemInfo != null)
                Application.Current.MainWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

			DialogResult = true;
		}
	}
}
