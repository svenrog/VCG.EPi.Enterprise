using VCG.EPi.Enterprise.Migration.Toolset.Types.Base;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;
using VCG.EPi.Enterprise.Migration.Toolset.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ProtoBuf;

namespace VCG.EPi.Enterprise.Migration.Toolset.Types
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
	public class XmlTargetMapping : Notifier
	{
		[ProtoMember(1)]
		private XmlTarget _source;
		public XmlTarget Source
		{
			get { return _source; }
			set { SetField(ref _source, value, "Source"); }
		}

		[ProtoMember(2)]
		private XmlTarget _target;
		public XmlTarget Target
		{
			get { return _target; }
			set
			{
				SetField(ref _target, value, "Target");

				if (Source != null && Target != null)
					UpdateMatchingProperties();
			}
		}

		[ProtoMember(3)]
        private bool _exportAsBlock;
        public bool ExportAsBlock
        {
            get { return _exportAsBlock; }
            set
            {
                SetField(ref _exportAsBlock, value, "ExportAsBlock");
            }
        }

		[ProtoMember(4)]
		private List<PropertyDefinition> _sourceProperties;
		public List<PropertyDefinition> SourceProperties
		{
			get
			{
				if (_sourceProperties == null)
				{
					var sourceNode = Source.Data.SelectSingleNode("*[19]");
					_sourceProperties = sourceNode.Deserialize<List<PropertyDefinition>>();
				}

				return _sourceProperties;
			}
			set
			{
				_sourceProperties = value;
			}
		}

		[ProtoMember(5)]
		private List<PropertyDefinition> _targetProperties;

		public void UpdateMatchingProperties()
		{
            if (Target == null || Target.Data == null) return;

            var targetNode = Target.Data.SelectSingleNode("PropertyDefinitions[1]");
			_targetProperties = targetNode.Deserialize<List<PropertyDefinition>>();

			if (SourceProperties != null && _targetProperties != null)
				MatchingProperties = _targetProperties.Where(t => SourceProperties.Any(s => (s.Alias ?? s.Name) == t.Name && PropertyMatch(s.Type.DataType, t.Type.DataType))).ToList();

			OnPropertyChanged("MatchingProperties");
		}

		protected bool PropertyMatch(string source, string target)
		{
			if (source == target) return true;
			if (source == "String" && target == "LongString") return true;
			return false;
		}

		[ProtoMember(6)]
		public List<PropertyDefinition> MatchingProperties { get; protected set; }

		public XmlTargetMapping(XmlElement source)
		{
			Source = new XmlTarget(source);
		}
	}
}
