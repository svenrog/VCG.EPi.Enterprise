using ProtoBuf;
using System;
using System.Xml.Serialization;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
	[Serializable]
	public class PropertyDefinition
	{
		[ProtoMember(1)]
		[XmlElement(ElementName = "ID")]
		public int Id { get; set; }

		[ProtoMember(2)]
		[XmlElement(ElementName = "PageTypeID")]
		public int PageTypeId { get; set; }

		[ProtoMember(3)]
		public string Name { get; set; }

		[ProtoMember(4)]
		public PropertyType Type { get; set; }

		[ProtoMember(5)]
		public bool Required { get; set; }

		[ProtoMember(6)]
		public PropertyTab Tab { get; set; }

		[ProtoMember(7)]
		public bool Searchable { get; set; }

		[ProtoMember(8)]
		public string DefaultValueType { get; set; }

		[ProtoMember(9)]
		public string DefaultValue { get; set; }

		[ProtoMember(10)]
		public string EditCaption { get; set; }
		
		[ProtoMember(11)]
		public string HelpText { get; set; }

		[ProtoMember(12)]
		public int FieldOrder { get; set; }

		[ProtoMember(13)]
		public bool DisplayEditUI { get; set; }

		[ProtoMember(14)]
		public bool LanguageSpecific { get; set; }

		[ProtoMember(15)]
		[XmlElement(ElementName = "SettingsID")]
		public Guid SettingsId { get; set; }

		[ProtoMember(16)]
		public bool ExistsOnModel { get; set; }

		[ProtoMember(17)]
		public string Alias { get; set; }
	}
}