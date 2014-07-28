using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace VCG.EPi.Enterprise.Xml.Transforms
{
	public class PropertyDataTransform : XPathDataTransform
	{
		protected string m_selectionFormat = "//RawProperty/Name[.='{0}']/../Value";

		protected HashSet<string> m_propertySet = new HashSet<string>();

		public virtual HashSet<string> Properties
		{
			get { return m_propertySet; }
			set { m_propertySet = value; }
		}

		protected void UpdateExpression()
		{
			m_expression = string.Join("|", m_propertySet.Select(p => string.Format(m_selectionFormat, p)));
		}

		public PropertyDataTransform() {}
		public PropertyDataTransform(IEnumerable<string> properties) 
		{
			foreach (string property in properties)
			{
				m_propertySet.Add(property);
			}
		}

		public PropertyDataTransform(IEnumerable<string> properties, string targetValue) : this(properties)
		{
			m_value = targetValue;
		}

		public override XmlDocument Transform(XmlDocument source)
		{
			UpdateExpression();
			return base.Transform(source);
		}
	}
}
