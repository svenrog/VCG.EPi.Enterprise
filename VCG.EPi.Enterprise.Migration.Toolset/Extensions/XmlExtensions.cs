using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace VCG.EPi.Enterprise.Migration.Toolset.Extensions
{
    public static class XmlExtensions
    {
		public static XmlNode Remove(this XmlNode node)
		{
			if (node == null) return null;
			if (node.ParentNode == null) return null;
			return node.ParentNode.RemoveChild(node);
		}

		public static XmlDocument Duplicate(this XmlDocument document)
		{
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(document.InnerXml);
			return xml;
		}

        public static T Deserialize<T>(this XmlNode node) where T : class, new()
        {
            if (node == null) return null;
            if (node.ChildNodes.Count < 1) return default(T);

            string name = node.FirstChild.Name;
            Regex pattern = new Regex("<(\\/{0,1})" + name + ">");

            Type type = typeof(T);
            if (type.IsGenericType) 
                type = type.GetGenericArguments()[0];

            if (type.Name != name)
                node.InnerXml = pattern.Replace(node.InnerXml, "<$1" + type.Name + ">");
            
            XmlSerializer serializer = new XmlSerializer(typeof(T), new XmlRootAttribute() { ElementName = node.Name });
            XmlReaderSettings settings = new XmlReaderSettings() { CheckCharacters = false, IgnoreProcessingInstructions = true };

			try
			{
				using (var sreader = new StringReader(node.OuterXml))
				using (var reader = XmlReader.Create(sreader, settings))
				{
					return (T)serializer.Deserialize(reader);
				}
			}
			catch(Exception)
			{
				return null;
			}
        }

    }
}
