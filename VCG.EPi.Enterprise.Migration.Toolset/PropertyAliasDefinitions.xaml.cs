using VCG.EPi.Enterprise.Migration.Toolset.Types;
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
	/// Interaction logic for PropertyAliasDefinitions.xaml
	/// </summary>
	public partial class PropertyAliasDefinitions : MetroWindow
	{
		public XmlTargetMapping Mapping { get; private set; }


		public PropertyAliasDefinitions(XmlTargetMapping mapping)
		{
			InitializeComponent();

			if (mapping == null)
			{
				Items.ItemsSource = null;
				return;
			}

			Mapping = mapping;
			Items.ItemsSource = Mapping.SourceProperties;
		}

		private void MetroWindow_Closed(object sender, EventArgs e)
		{
			Mapping.UpdateMatchingProperties();
		}
	}
}
