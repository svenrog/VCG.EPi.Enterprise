using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml;


namespace VCG.EPi.Enterprise.Migration.Toolset.Serialization
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class XmlSurrogate
    {
		[ProtoMember(1)]
		public string Xml;

		public static implicit operator XmlSurrogate(XmlElement element)
		{
			return element != null ? new XmlSurrogate { Xml = element.OuterXml } : null;
		}

		public static implicit operator XmlElement(XmlSurrogate surrogate)
		{
			XmlDocument doc = new XmlDocument();

			if (string.IsNullOrWhiteSpace(surrogate.Xml)) 
				return null;

			doc.LoadXml(surrogate.Xml);

			return doc.DocumentElement;
		}
    }
}
