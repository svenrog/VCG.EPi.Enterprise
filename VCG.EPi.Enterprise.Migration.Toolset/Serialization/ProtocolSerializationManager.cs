using VCG.EPi.Enterprise.Migration.Toolset.Data;
using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Xml;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Xml;

namespace VCG.EPi.Enterprise.Migration.Toolset.Serialization
{
	public static class ProtocolSerializationManager
	{
		public static void PrepareSerializer()
		{
			var metatype = RuntimeTypeModel.Default.Add(typeof(XmlElement), false);
			metatype.SetSurrogate(typeof(XmlSurrogate));

			Serializer.PrepareSerializer<XmlSurrogate>();
			Serializer.PrepareSerializer<PropertyTab>();
			Serializer.PrepareSerializer<PropertyType>();
			Serializer.PrepareSerializer<PropertyDefinition>();
			Serializer.PrepareSerializer<XmlTargetMapping>();
			Serializer.PrepareSerializer<XmlTarget>();
			Serializer.PrepareSerializer<Document>();
		}

	}
}
