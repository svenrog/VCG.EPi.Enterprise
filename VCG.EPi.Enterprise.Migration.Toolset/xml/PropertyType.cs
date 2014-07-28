using ProtoBuf;
using System;
using System.Xml.Serialization;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
	[Serializable]
	public class PropertyType
	{
		[ProtoMember(1)]
		[XmlElement(ElementName = "ID")]
		public int Id { get; set; }

		[ProtoMember(2)]
		public string DataType { get; set; }

		[ProtoMember(3)]
        public string TypeName { get; set; }

		[ProtoMember(4)]
		public string Name { get; set; }

		[ProtoMember(5)]
        public string AssemblyName { get; set; }
	}
}
