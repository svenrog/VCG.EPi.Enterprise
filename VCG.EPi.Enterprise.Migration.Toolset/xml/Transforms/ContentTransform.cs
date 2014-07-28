using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;
using VCG.EPi.Enterprise.Xml.Transforms;
using System.Xml;
using System.Linq;
using VCG.EPi.Enterprise.IO;
using System;
using System.Collections.Generic;
using VCG.EPi.Enterprise.Xml;
using System.IO;
using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Optimization;
using System.Xml.XPath;
using System.Globalization;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
	public abstract class ContentTransform : LogTransform
	{
        protected readonly string _transferPageRootSelectionFormat = "/export[1]/pages[1]";
        protected readonly string _transferBlockRootSelectionFormat = "/export[1]/pages[2]";
        protected readonly Guid _contentFolderGuid = Guid.Parse("E56F85D0-E833-4E02-976A-2D11FE4D598C");

        protected string GetPageLink(Guid guid)
        {
            return string.Format("[{0}][][]", guid.ToString("N"));
        }

        protected string GetDate(DateTime date)
        {
            return date.ToString("MM/dd/yy hh:mm:ss tt", CultureInfo.InvariantCulture);
        }

		protected virtual XmlElement CreateCustomDataNode(XmlDocument context, string name, string xml)
        {
            XmlElement result = context.CreateElement("RawNameAndXml");
            XmlElement node = null;

            node = context.CreateElement("Name");
            node.InnerText = name;
            result.AppendChild(node);

            node = context.CreateElement("Xml");
            node.InnerText = string.Format("<string>{0}</string>", xml);
            result.AppendChild(node);

            return result;
        }

		protected virtual XmlElement CreateContentDataNode(XmlDocument context, IEnumerable<XmlElement> properties)
        {
            XmlElement result = context.CreateElement("TransferContentData");
            result.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
            result.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");

            XmlElement node, subnode = null;

            node = context.CreateElement("RawContentData");
			subnode = CreateDefaultACLNode(context);
            node.AppendChild(subnode);
			subnode = context.CreateElement("Property");

            foreach (var property in properties)
                subnode.AppendChild(property);

			node.AppendChild(subnode);
            result.AppendChild(node);

            node = context.CreateElement("RawLanguageData");
            result.AppendChild(node);

            node = context.CreateElement("LanguageSettings");
            result.AppendChild(node);

            node = context.CreateElement("ContentLanguageSettings");
            result.AppendChild(node);

            node = context.CreateElement("DynamicProperties");
            result.AppendChild(node);

            return result;
        }

		protected virtual XmlElement CreatePropertyDataNode(XmlDocument context, string tab, string type, string name, string value = null, string typeName = null, bool isRequired = false, bool isLanguageSpecific = false, XmlNode customData = null)
        {
            XmlElement result = context.CreateElement("RawProperty");
            XmlElement node = null;

            bool isNull = value == null;

			node = context.CreateElement("PageDefinitionID");
            node.InnerText = "0";
            result.AppendChild(node);

            node = context.CreateElement("OwnerTab");
            node.InnerText = tab;
            result.AppendChild(node);

            node = context.CreateElement("IsModified");
            node.InnerText = "false";
            result.AppendChild(node);

            node = context.CreateElement("IsNull");
			node.InnerText = isNull.ToString().ToLower();
            result.AppendChild(node);

            node = context.CreateElement("IsRequired");
			node.InnerText = isRequired.ToString().ToLower();
            result.AppendChild(node);

            node = context.CreateElement("IsPropertyData");
            node.InnerText = "false";
            result.AppendChild(node);

            node = context.CreateElement("IsDynamicProperty");
            node.InnerText = "false";
            result.AppendChild(node);

            node = context.CreateElement("IsLanguageSpecific");
            node.InnerText = isLanguageSpecific.ToString().ToLower();
            result.AppendChild(node);

            node = context.CreateElement("Type");
            node.InnerText = type;
            result.AppendChild(node);

            if (!string.IsNullOrEmpty(typeName))
            {
                node = context.CreateElement("TypeName");
                node.InnerText = typeName;
                result.AppendChild(node);

                node = context.CreateElement("AssemblyName");
                node.InnerText = "EPiServer";
                result.AppendChild(node);
            }

            node = context.CreateElement("Name");
            node.InnerText = name;
            result.AppendChild(node);

            node = context.CreateElement("Value");

            if (!string.IsNullOrEmpty(value))
                node.InnerText = value;

            result.AppendChild(node);

            node = context.CreateElement("BlockProperties");
            result.AppendChild(node);

            node = context.CreateElement("CustomData");
            if (customData != null)
                node.AppendChild(customData);

            result.AppendChild(node);

            return result;
        }

        protected virtual XmlElement CreateDefaultACLNode(XmlDocument context)
        {
            XmlElement result = context.CreateElement("ACL");
            XmlElement node = null;

            node = CreateACENode(context, "WebEditors", "Read Create Edit Delete Publish");
            result.AppendChild(node);

            node = CreateACENode(context, "WebAdmins", "FullAccess");
            result.AppendChild(node);

            node = CreateACENode(context, "Everyone", "Read");
            result.AppendChild(node);

            return result;
        }

		protected virtual XmlElement CreateACENode(XmlDocument context, string name, string access)
        {
            XmlElement result = context.CreateElement("RawACE");
            XmlElement node = null;

            node = context.CreateElement("EntityType");
            node.InnerText = "Role";
            result.AppendChild(node);

            node = context.CreateElement("SID");
            node.InnerText = "0";
            result.AppendChild(node);

            node = context.CreateElement("Access");
            node.InnerText = access;
            result.AppendChild(node);

            node = context.CreateElement("Name");
            node.InnerText = name;
            result.AppendChild(node);

            return result;
        }
    }
}
