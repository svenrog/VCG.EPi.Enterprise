using VCG.EPi.Enterprise.Migration.Toolset.Data;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
	/// Interaction logic for NameSpaceEntry.xaml
	/// </summary>
    public partial class ClassExportOptions : MetroWindow
	{
		public ClassGenerationOptions Options { get; set; }

		public ClassExportOptions()
		{
			InitializeComponent();

			Options = new ClassGenerationOptions()
			{
				UseAttributeContentType = cbContentType.IsChecked ?? false,
				UseAttributeAvailablePageTypes = cbAvailablePageTypes.IsChecked ?? false,
				UseAttributeAccess = cbAccess.IsChecked ?? false,
				UseAttributeDisplay = cbDisplay.IsChecked ?? false,
				UseAttributeCultureSpecific = cbCultureSpecific.IsChecked ?? false,
				UseAttributeUIHint = cbUIHint.IsChecked ?? false,
                UseAttributeSearcheable = cbSearcheable.IsChecked ?? false,
                UseAttributeRequired = cbRequired.IsChecked ?? false,
                UseAttributeScaffoldColumn = cbScaffoldColumn.IsChecked ?? false,
                NameSpaceBase = string.Empty,
                TargetVersion = rbVersion75.IsChecked ?? true ? EPiServerVersion.Version_7_5 : EPiServerVersion.Version_7
			};
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
            Options.NameSpaceBase = tbBaseNamespace.Text;
			DialogResult = true;
		}

		private void tbBaseNamespace_TextChanged(object sender, TextChangedEventArgs e)
		{
			btnOk.IsEnabled = !string.IsNullOrEmpty(tbBaseNamespace.Text);
		}

		private void cbContentType_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeContentType = cbContentType.IsChecked ?? false;
		}

		private void cbDisplay_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeDisplay = cbDisplay.IsChecked ?? false;
		}

		private void cbCultureSpecific_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeCultureSpecific = cbCultureSpecific.IsChecked ?? false;
		}

		private void cbUIHint_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeUIHint = cbUIHint.IsChecked ?? false;
		}

		private void cbAvailablePageTypes_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeAvailablePageTypes = cbAvailablePageTypes.IsChecked ?? false;
		}

		private void cbAccess_Checked(object sender, RoutedEventArgs e)
		{
			if (Options == null) return;
			Options.UseAttributeAccess = cbAccess.IsChecked ?? false;
		}

        private void rbVersion_Checked(object sender, RoutedEventArgs e)
        {
            if (Options == null) return;
            Options.TargetVersion = rbVersion75.IsChecked ?? true ? EPiServerVersion.Version_7_5 : EPiServerVersion.Version_7;

            cbAvailablePageTypes.Content = Options.TargetVersion == EPiServerVersion.Version_7_5 ? "AvailableContentTypes" : "AvailablePageTypes";

        }

        private void cbRequired_Checked(object sender, RoutedEventArgs e)
        {
            if (Options == null) return;
            Options.UseAttributeRequired = cbRequired.IsChecked ?? false;
        }

        private void cbSearcheable_Checked(object sender, RoutedEventArgs e)
        {
            if (Options == null) return;
            Options.UseAttributeSearcheable = cbSearcheable.IsChecked ?? false;
        }

        private void cbScaffoldColumn_Checked(object sender, RoutedEventArgs e)
        {
            if (Options == null) return;
            Options.UseAttributeScaffoldColumn = cbScaffoldColumn.IsChecked ?? false;
        }

        
	}
}
