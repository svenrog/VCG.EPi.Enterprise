using VCG.EPi.Enterprise.Migration.Toolset.Data;
using VCG.EPi.Enterprise.Migration.Toolset.Types.Base;
using ProtoBuf;
using System;
using System.Xml;

namespace VCG.EPi.Enterprise.Migration.Toolset.Types
{
    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class XmlTarget : Notifier
    {
		[ProtoMember(1)]
        public XmlElement Data { get; set; }

		[ProtoMember(2)]
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value, "Name"); }
        }

		[ProtoMember(3)]
        private int _id;
        public int Id 
        { 
            get { return _id; }
            set { SetField(ref _id, value, "Id"); } 
        }

		[ProtoMember(4)]
        private ContentType _type = ContentType.Page;
        public ContentType Type
        {
            get { return _type; }
            set { SetField(ref _type, value, "Type"); } 
        }

        public XmlTarget(XmlElement node)
        {
            Data = node;
            Name = node.SelectSingleNode("*[3]").InnerText;
            Id = int.Parse(node.SelectSingleNode("*[1]").InnerText);

            var type = node.Attributes["xsi:type"];
            if (type != null && type.Value == "BlockTypeTransferObject")
                Type = ContentType.Block;
        }
    }
}
