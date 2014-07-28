using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Migration.Toolset.Data
{
	public class ClassGenerationOptions
	{
        public string NameSpaceBase { get; set; }
        public EPiServerVersion TargetVersion { get; set; }
		public bool UseAttributeContentType { get; set; }
		public bool UseAttributeAvailablePageTypes { get; set; }
		public bool UseAttributeAccess { get; set; }
		public bool UseAttributeDisplay { get; set; }
		public bool UseAttributeCultureSpecific { get; set; }
        public bool UseAttributeRequired { get; set; }
        public bool UseAttributeSearcheable { get; set; }
		public bool UseAttributeUIHint { get; set; }
        public bool UseAttributeScaffoldColumn { get; set; }
	}
}
