using VCG.EPi.Enterprise.Xml.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VCG.EPi.Enterprise.Xml.Transforms
{
	public class PageTypePropertyTransform : IXPathTransform
	{
		protected readonly string m_selectionFormat = "//RawProperty/Name[.='PageTypeID']/../Value[.='{0}']/../PageDefinitionID[.='{1}']/../";

		public string PageTypeId { get; set; }
		public string DefinitionId { get; set; }
		public string Value { get; set; }
		public string Name { get; set; }

		public PageTypePropertyTransform(string pageTypeId, string definitionId, string newValue, string newName)
		{
			PageTypeId = pageTypeId;
			DefinitionId = definitionId;
			Value = newValue;
			Name = newName;
		}

		public XmlDocument Transform(XmlDocument source)
		{
			string expression = string.Format(m_selectionFormat, PageTypeId, DefinitionId);

			XmlNodeList affectedPages = source.SelectNodes(expression);

			foreach (XmlNode node in source)
			{
				XmlNode definitionNode = node.SelectSingleNode("//PageDefinitionID");

				if (definitionNode != null)
					definitionNode.InnerText = Value.ToString();

				XmlNode nameNode = node.SelectSingleNode("//Name");

				if (nameNode != null)
					nameNode.InnerText = Name;
			}

			return source;
		}
	}
}
