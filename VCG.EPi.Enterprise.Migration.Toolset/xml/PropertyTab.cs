using ProtoBuf;
using System;
using System.Xml.Serialization;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
	[Serializable]
	public class PropertyTab
	{
		[ProtoMember(1)]
		[XmlElement(ElementName = "ID")]
		public int Id { get; set; }

		[ProtoMember(2)]
		public string Name { get; set; }

		[ProtoMember(3)]
		public string RequiredAccess { get; set; }

		[ProtoMember(4)]
		public int SortIndex { get; set; }

		[ProtoMember(5)]
		public bool IsSystemTab { get; set; }
	}
}
