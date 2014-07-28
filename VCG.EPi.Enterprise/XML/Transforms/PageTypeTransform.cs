using VCG.EPi.Enterprise.Xml.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VCG.EPi.Enterprise.Xml.Transforms
{
	public class PageTypeTransform : IXPathTransform
	{
		protected readonly string m_selectionFormat = "//RawProperty/Name[.='PageTypeID']/../Value[.='{0}']/../";
		
		public string OldValue { get; set; }
		public string Value { get; set; }
		public string Name { get; set; }

		public PageTypeTransform(string oldValue, string newValue, string newName)
		{
			OldValue = oldValue;
			Value = newValue;
			Name = newName;
		}

		public XmlDocument Transform(XmlDocument source)
		{
			string expression = string.Format(m_selectionFormat, OldValue);
			
			XmlNodeList affectedPages = source.SelectNodes(expression);

			foreach (XmlNode node in source)
			{
				XmlNode propertyNode = node.SelectSingleNode("//Value");

				if (propertyNode != null)
					propertyNode.InnerText = Value.ToString();

				XmlNode metaData = node.SelectSingleNode("//CustomData/RawNameAndXml/Xml");
					
				if (metaData != null)
					metaData.InnerText = "<string>" + Name + "</string>";
			}

			return source;
		}
	}
}
