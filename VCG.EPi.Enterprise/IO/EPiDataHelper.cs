using VCG.EPi.Enterprise.Xml.Transforms;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VCG.EPi.Enterprise.IO
{
    // Just for reference, keeping it as original implementation

	public class EPiDataHelper
	{
		private static XmlDocument m_dataContainer;
		private static XmlNamespaceManager m_dataManager;
		private static XmlElement m_dataRoot;

		private static XmlDocument m_definitionContainer;
        private static XmlElement m_definitionRoot;
        private static XmlDocument m_postDataContainer;
        private static XmlElement m_postDataRoot;
		
        private static void Log(TextWriter writer, string message)
        {
            if (writer == null) return;

            writer.WriteLine(message);
        }

		public static void UpgradeCMSDataPackage(string path, IEnumerable<IXPathTransform> transforms = null, TextWriter log = null)
		{
            // progress reporting, in future..
            int steps = 10;
            int current = 0;

            if (transforms != null)
                steps += transforms.Count();

			m_dataContainer = LoadResource(path, "epix.xml");
			m_dataRoot = (XmlElement)m_dataContainer.FirstChild;

			VersionCheck(m_dataContainer);

            // Create empty structure and apply namespaces for definitions and post content
			m_definitionContainer = GetEmptyExportStructure("Definition", 3);
            m_definitionRoot = (XmlElement)m_definitionContainer.FirstChild;
            GetNameSpaceManager(m_definitionContainer);

            m_postDataContainer = GetEmptyExportStructure("PostContent", 3);
            m_postDataRoot = (XmlElement)m_postDataContainer.FirstChild;
            GetNameSpaceManager(m_postDataContainer);

			m_dataManager = GetNameSpaceManager(m_dataContainer);

            // Apply transforms before upgrade
            if (transforms != null)
                foreach (IXPathTransform transform in transforms)
                    transform.Transform(m_dataContainer);                   

			// Move & upgrade dynamic properties
			XmlNode dynamicProperties = GetDynamicProperties(m_dataContainer);
			if (dynamicProperties != null)
			{
				m_dataRoot.RemoveChild(dynamicProperties);
				UpgradeDynamicProperties(dynamicProperties);
				m_definitionRoot.AppendChild(ConvertContext(dynamicProperties, m_definitionContainer));
			}

            // Move tabs
            XmlNode tabs = GetTabs(m_dataContainer);
            if (tabs != null)
            {
                m_dataRoot.RemoveChild(tabs);
                m_definitionRoot.AppendChild(ConvertContext(tabs, m_definitionContainer));
            }

            // Move categories
            XmlNode categories = GetCategories(m_dataContainer);
            if (categories != null)
            {
                m_dataRoot.RemoveChild(categories);
                m_definitionRoot.AppendChild(ConvertContext(categories, m_definitionContainer));
            }

            // Move frames
            XmlNode frames = GetFrames(m_dataContainer);
            if (frames != null)
            {
                m_dataRoot.RemoveChild(frames);
                m_definitionRoot.AppendChild(ConvertContext(frames, m_definitionContainer));
            }

            // Move visitor groups
            XmlNode visitorGroups = GetVisitorGroups(m_dataContainer);
            if (visitorGroups != null)
            {
                m_dataRoot.RemoveChild(visitorGroups);
                // Removed for Demo purpose / m_postDataRoot.AppendChild(ConvertContext(visitorGroups, m_postDataContainer));
            }

            // Move & upgrade page types
			XmlNode pageTypes = GetPageTypes(m_dataContainer);
			if (pageTypes != null)
			{
                pageTypes = RenameNode(pageTypes, "contenttypes");
				m_dataRoot.RemoveChild(pageTypes);
				UpgradePageTypes(pageTypes);
				m_definitionRoot.AppendChild(ConvertContext(pageTypes, m_definitionContainer));
			}

            // Repair page data if previously modified for coherence
            XmlNodeList modifiedPages = GetModifiedPageData(m_dataContainer);
            if (modifiedPages != null)
                RepairPageData(modifiedPages, m_dataContainer);

			// Upgrade page data
			XmlNode pageData = GetPageData(m_dataContainer);

			if (pageData != null)
				UpgradePageData(pageData);

			// Update file version of data container
			m_dataRoot.SetAttribute("version", "3");

            // Construct data package to be injected into zip file
            Dictionary<string, XmlDocument> newData = new Dictionary<string, XmlDocument>();
            newData.Add("epix.xml", m_dataContainer);

            if (m_definitionRoot.HasChildNodes)
                newData.Add("epiDefinition.xml", m_definitionContainer);

            if (m_postDataRoot.HasChildNodes)
                newData.Add("epiPostContent.xml", m_postDataContainer);

			RepackageData(path, newData);
		}

		public static XmlDocument LoadResource(string path, string name)
		{
			using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
			{
				return LoadResource(file, name);
			}
		}

		public static XmlDocument LoadResource(Stream stream, string name)
		{
			using (ZipFile zip = new ZipFile(stream))
			{
				foreach (ZipEntry entry in zip)
				{
					if (!entry.IsFile) continue;
					if (!entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

                    using (Stream outStream = zip.GetInputStream(entry))
                    {
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(outStream);

                        return xmldoc;
                    }
				}
			}

			return null;
		}

		public static XmlDocument GetEmptyExportStructure(string suffix, int version)
		{
			XmlDocument result = new XmlDocument();

			XmlElement root = result.CreateElement("export" + suffix);
			root.SetAttribute("culture", "en-US");
			root.SetAttribute("version", version.ToString());

			result.AppendChild(root);

			return result;
		}

		public static XmlNamespaceManager GetNameSpaceManager(XmlDocument document)
		{
			XmlNamespaceManager result = new XmlNamespaceManager(document.NameTable);
			result.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			result.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");

			return result;
		}

		private static void RepackageData(string path, Dictionary<string, XmlDocument> data)
		{
			using (FileStream file = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
			{
				using (ZipFile zip = new ZipFile(file))
				{
					zip.BeginUpdate();

                    var sources = data.ToDictionary(d => d.Key, d => ConvertXmlToDataSource(d.Value));

                    foreach (string key in sources.Keys)
                        zip.Add(sources[key], key);                    
					
					zip.CommitUpdate();
					zip.Close();

                    foreach (string key in sources.Keys)
                        sources[key].Dispose();
				}
			}
		}

		private static StaticDataSource ConvertXmlToDataSource(XmlDocument doc)
		{
			Stream result = new MemoryStream();
			doc.Save(result);

			StaticDataSource dataSource = new StaticDataSource();
			dataSource.SetStream(result);

			return dataSource;
		}

        private static XmlNodeList GetModifiedPageData(XmlDocument doc)
        {
            return doc.SelectNodes("//pages/TransferPageData/RawPageData/Property/RawProperty/Name[.='PageTypeID']/../Value[starts-with(.,'*')]/../../../..");

        }
		private static XmlNode GetDynamicProperties(XmlDocument doc)
		{
			return doc.SelectSingleNode("//dynamicpropertydefinitions");
		}

        private static XmlNode GetFrames(XmlDocument doc)
        {
            return doc.SelectSingleNode("//frames");
        }

        private static XmlNode GetVisitorGroups(XmlDocument doc)
        {
            return doc.SelectSingleNode("//visitorGroups");
        }

        private static XmlNode GetCategories(XmlDocument doc)
        {
            return doc.SelectSingleNode("//categories");
        }

        private static XmlNode GetTabs(XmlDocument doc)
        {
            return doc.SelectSingleNode("//tabdefinitions");
        }

		private static XmlNode GetPageTypes(XmlDocument doc)
		{
			return doc.SelectSingleNode("//pagetypes");
		}

		private static XmlNode GetPageData(XmlDocument doc)
		{
			return doc.SelectSingleNode("//pages");
		}

		private static void UpgradeDynamicProperties(XmlNode node)
		{
			XmlNodeList properties = node.SelectNodes("//PageDefinition");

			XmlNodeList longStringSettings = node.SelectNodes(".//LongStringSettings");

			foreach (XmlNode property in properties)
			{
				XmlNode attribute = property.SelectSingleNode("/LongStringSettings");

				if (attribute != null)
					property.RemoveChild(attribute);

				attribute = CreateTextNode("ExistsOnModel", "false");
				property.AppendChild(attribute);
			}
		}

		private static void UpgradePageData(XmlNode node)
		{
			XmlNodeList pages = node.SelectNodes("//TransferPageData");

			foreach (XmlNode page in pages)
			{
				XmlNode newPage = RenameNode(page, "TransferContentData");

				XmlElement rawPage = (XmlElement)newPage.SelectSingleNode("./RawPageData");

				rawPage = (XmlElement)RenameNode(rawPage, "RawContentData");

				rawPage.SetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", "RawPage");
				rawPage.AppendChild(CreateEmptyNode("BlockProperties"));
			}
		}

		private static void UpgradePageTypes(XmlNode node)
		{
            XmlNode array = node.SelectSingleNode("./ArrayOfPageType");
            RenameNode(array, "ArrayOfContentTypeTransferObject");

            XmlNodeList pageTypes = node.SelectNodes(".//PageType");

            foreach (XmlNode pageType in pageTypes)
            {
                XmlElement newType = (XmlElement)RenameNode(pageType, "ContentTypeTransferObject");
                newType.SetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", "PageTypeTransferObject");

                XmlNode fileName = newType.SelectSingleNode("./FileName");
                if (fileName != null) newType.RemoveChild(fileName);

                XmlNode exportFileName = newType.SelectSingleNode("./ExportableFileName");
                if (exportFileName != null) newType.RemoveChild(exportFileName);

                XmlNode properties = newType.SelectSingleNode("./Definitions");
                
                if (properties != null)
                {
                    properties = RenameNode(properties, "PropertyDefinitions");

                    XmlNodeList definitions = properties.SelectNodes("./PageDefinition");

                    if (definitions != null)
                    {
                        foreach(XmlNode definition in definitions)
                        {
                            RenameNode(definition, "PropertyDefinition");
                        }
                    }
                }
            }
		}

        private static void RepairPageData(XmlNodeList data, XmlDocument document)
        {
            XmlNode pageDataRoot = GetPageData(document);
            pageDataRoot.RemoveAll();

            foreach (XmlNode page in data)
            {
                XmlNode pageTypeIdProperty = page.SelectSingleNode("./RawPageData/Property/RawProperty/Name[.='PageTypeID']/../Value");
                
                if (pageTypeIdProperty != null)
                    pageTypeIdProperty.InnerText = pageTypeIdProperty.InnerText.Substring(1);

                XmlNodeList properties = page.SelectNodes("./RawPageData/Property/RawProperty/Type[.='PageType']/../Value");

                foreach(XmlNode property in properties)
                {
                    if (!property.InnerText.StartsWith("*")) continue;
                    property.InnerText = property.InnerText.Substring(1);
                }

                pageDataRoot.AppendChild(page);
            }
        }

		private static XmlNode RenameNode(XmlNode node, string name)
		{
			XmlDocument doc = node.OwnerDocument;
			XmlNodeList children = node.ChildNodes;

			XmlElement newNode = (XmlElement)CreateEmptyNode(name);

			foreach (XmlAttribute attribute in node.Attributes)
				newNode.SetAttribute(attribute.Name, attribute.Value);
			
			foreach (XmlNode child in children)
				newNode.AppendChild(child);

			node.ParentNode.ReplaceChild(newNode, node);

			return newNode;
		}

		private static XmlNode ConvertContext(XmlNode node, XmlDocument newContext)
		{
            if (node == null) return null;

			XmlElement result = (XmlElement)newContext.CreateElement(node.LocalName);

			foreach (XmlAttribute attribute in node.Attributes)
				result.SetAttribute(attribute.Name, attribute.Value);

			result.InnerXml = node.InnerXml;

			return result;
		}
        

		private static XmlNode CreateTextNode(string name, string value)
		{
			XmlNode result = m_dataContainer.CreateElement(name);
			result.AppendChild(m_dataContainer.CreateTextNode(value));
			return result;
		}

		private static XmlNode CreateEmptyNode(string name)
		{
			return m_dataContainer.CreateElement(name);
		}

        public static int GetFileVersion(XmlDocument doc)
        {
			if (doc == null) return -1;

            XmlNode root = doc.FirstChild;

            if (root == null || root.Attributes["version"] == null)
                return -1;

            int version = -1;
            int.TryParse(root.Attributes["version"].Value, out version);

            return version;
        }

		private static void VersionCheck(XmlDocument doc)
		{
			XmlNode root = doc.FirstChild;

			if (root == null || root.Attributes["version"] == null)
				throw new ApplicationException("Incompatible data");

			if (root.Attributes["version"].Value == "3")
				throw new ApplicationException("Data already upgraded");

			if (root.Attributes["version"].Value != "1")
				throw new ApplicationException("Incompatible data");
		}

	}
}
