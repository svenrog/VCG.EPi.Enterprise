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
	public class XPathDataTransform : IXPathTransform
	{
		protected string m_expression = string.Empty;
		protected string m_value = string.Empty;

		public virtual string Expression
		{
			get { return m_expression; }
			set { m_expression = value; }
		}

		public virtual string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		public XPathDataTransform() {}
		public XPathDataTransform(string expression) 
		{
			m_expression = expression;
		}

		public XPathDataTransform(string expression, string targetValue) : this(expression)
		{
			m_value = targetValue;
		}

		public virtual XmlDocument Transform(XmlDocument source)
		{
			XmlNamespaceManager mgr = new XmlNamespaceManager(source.NameTable);
			mgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

			foreach (XmlNode node in source.SelectNodes(m_expression, mgr))
			{
				node.InnerText = m_value;
			}

			return source;
		}
	}
}
