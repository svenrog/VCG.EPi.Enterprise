using VCG.EPi.Enterprise.Migration.Toolset.Properties;
using VCG.EPi.Enterprise.Migration.Toolset.Types;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VCG.EPi.Enterprise.Migration.Toolset
{
	public partial class Preferences : MetroWindow
	{
        private MainWindow _application = (MainWindow)Application.Current.MainWindow;
        private CultureInfo _originalCulture;

        public Preferences()
		{
            _originalCulture = _application.CurrentCulture;

			InitializeComponent();
            DataBindComponents();
		}

        private void DataBindComponents()
        {
            ApplicationLanguage.SelectedItem = _application.CurrentCulture;
            ApplicationLanguage.ItemsSource = _application.AvailableCultures;
        }

        private void ApplicationLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _application.CurrentCulture = (CultureInfo)ApplicationLanguage.SelectedItem;
            ValidateButtons();
        }

        private void ValidateButtons()
        {
            btnRestart.IsEnabled = Changes;
            lblRestartNotice.Visibility = Changes ? Visibility.Visible : Visibility.Hidden;
        }

        private bool Changes
        {
            get
            {
                if (!_application.CurrentCulture.Equals(_originalCulture)) return true;
                return false;
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            System.Windows.Forms.Application.Restart();
            Application.Current.Shutdown();
        }
	}
}
