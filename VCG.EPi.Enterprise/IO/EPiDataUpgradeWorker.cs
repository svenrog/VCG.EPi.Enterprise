using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Xml.Transforms;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace VCG.EPi.Enterprise.IO
{

    public class EPiDataUpgradeWorker
    {
        #region Members

        private object m_lock = new object();

        private int m_lastprogress = 0;

		private XmlDocument m_dataContainer;
		private XmlNamespaceManager m_dataManager;
		private XmlElement m_dataRoot;

        private XmlDocument m_handlerMappings;

		private XmlDocument m_definitionContainer;
        private XmlElement m_definitionRoot;
        private XmlDocument m_postDataContainer;
        private XmlElement m_postDataRoot;
        private AsyncOperation m_operation;

        public event ProgressEventHandler OnUpgradeProgress;
        public event EventHandler OnUpgradeComplete;

        private readonly HashSet<string> m_allowedDynamicStores = new HashSet<string>()
        {
            "VisitorGroup",
            "VisitorGroupCriterion",
            "EPiServer.Core.PropertySettings.PropertySettingsContainer",
            "EPiServer.Core.PropertySettings.PropertySettingsWrapper",
            "EPiServer.Web.PropertyControls.PropertySettings.MultipleOptionsListSettings",
            "EPiServer.Editor.TinyMCE.TinyMCESettings",
            "EPiServer.Editor.TinyMCE.ToolbarRow",
            "EPiServer.Editor.HtmlEditorSettings"
        };

        private readonly HashSet<string> m_allowedDataTypes = new HashSet<string>()
        {
            "EPiServer.SpecializedProperties.PropertyImageUrl",
            "EPiServer.SpecializedProperties.PropertyDocumentUrl",
			"EPiServer.SpecializedProperties.PropertyUrl",
            "EPiServer.SpecializedProperties.PropertyXhtmlString",
			"EPiServer.SpecializedProperties.PropertyLinkCollection",
			"EPiServer.SpecializedProperties.PropertyLanguageBranchList",
            "EPiServer.Core.PropertyXForm",
            "LongString",
            "PageType",
            "PageReference",
            "Number",
            "Decimal",
            "String",
            "Boolean"
        };

        // Todo: Test filter
		private readonly Regex m_cultureInfo = new Regex("([a-z]{2}-{0,1}[a-z]{0,2})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex m_permanentLink = new Regex("(\\~\\/link\\/[a-z0-9]{32}\\.[a-z]{1,5}).*?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex m_breaks = new Regex("<br\\s{0,1}\\/{0,1}>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //"~/link/b43d8067c4c444fbad72fc4bb0c0f8c1.aspx?epslanguage=sv,sv,sv,sv&amp;amp;epslanguage=sv,sv,sv,sv"

        private readonly string m_filterAllowedDynamicCriteria = "EPiServer.Personalization.VisitorGroups.Criteria";
        private readonly string m_filterSystemAllowedType = "System";
        #endregion

        
        public EPiDataUpgradeWorker(AsyncOperation operation) 
        {
            m_operation = operation;
        }

        // Seems to need destruction
        ~EPiDataUpgradeWorker()
        {
            m_lock = null;
		    m_dataContainer = null;
		    m_dataManager = null;
		    m_dataRoot = null;
            m_handlerMappings = null;
		    m_definitionContainer = null;
            m_definitionRoot = null;
            m_postDataContainer = null;
            m_postDataRoot = null;
            m_operation = null;
            OnUpgradeProgress = null;
            OnUpgradeComplete = null;
        }

        public void UpgradeDataPackage(string path, IEnumerable<IXPathTransform> transforms = null, ILog log = null)
		{
            int steps = 12;
            int current = 0;

            if (transforms != null)
                steps += transforms.Count();

			var loader = new EPiResourceLoader();

			m_dataContainer = loader.LoadResource(path, "epix.xml");
			m_dataRoot = (XmlElement)m_dataContainer.FirstChild;

            Log(log, "Version checking data container");
			VersionCheck(m_dataContainer);

            Log(log, "Creating new data structure");

            // Create empty structure and apply namespaces for definitions and post content
            Dictionary<string, XmlDocument> newData = new Dictionary<string, XmlDocument>();

			m_definitionContainer = GetEmptyExportStructure("Definition", 3);
            m_definitionRoot = (XmlElement)m_definitionContainer.FirstChild;
            GetNameSpaceManager(m_definitionContainer);

            m_postDataContainer = GetEmptyExportStructure("PostContent", 3);
            m_postDataRoot = (XmlElement)m_postDataContainer.FirstChild;
            GetNameSpaceManager(m_postDataContainer);

			m_dataManager = GetNameSpaceManager(m_dataContainer);

            Progress((double)++current / (double)steps, ref log);

            // Apply transforms before upgrade
            if (transforms != null)
            {
                foreach(var transform in transforms)
                {
                    if (transform is IXPathLogTransform)
                        ((IXPathLogTransform)transform).OnLog += (x, y) => { Log(log, y.Entry.Message, y.Entry.Type); };

                    transform.Transform(m_dataContainer);
                    Progress((double)++current / (double)steps, ref log);
                }
            }

            // Move & upgrade dynamic properties
            XmlNode dynamicProperties = GetDynamicProperties(m_dataContainer);
            if (dynamicProperties != null)
            {
                Log(log, "Adjusting dynamic properties to new format");

                lock (m_dataRoot)
                {
                    m_dataRoot.RemoveChild(dynamicProperties);
                    UpgradeDynamicProperties(dynamicProperties, log);
                }

                m_definitionRoot.AppendChild(ConvertContext(dynamicProperties, m_definitionContainer));

                Progress((double)++current / (double)steps, ref log);
            }

            // Move tabs
            XmlNode tabs = GetTabs(m_dataContainer);
            if (tabs != null)
            {
                Log(log, "Adjusting tabs to new format");

                lock (m_dataRoot)
                {
                    m_dataRoot.RemoveChild(tabs);
                    m_definitionRoot.AppendChild(ConvertContext(tabs, m_definitionContainer));
                }

                Progress((double)++current / (double)steps, ref log);
            }

            // Move categories
            XmlNode categories = GetCategories(m_dataContainer);
            if (categories != null)
            {
                Log(log, "Adjusting categories to new format");

                lock (m_dataRoot)
                {
                    m_dataRoot.RemoveChild(categories);
                    m_definitionRoot.AppendChild(ConvertContext(categories, m_definitionContainer));
                }

                Progress((double)++current / (double)steps, ref log);
            }

            // Move frames
            XmlNode frames = GetFrames(m_dataContainer);
            if (frames != null)
            {
                Log(log, "Adjusting frames to new format");


                lock (m_dataRoot)
                {
                    m_dataRoot.RemoveChild(frames);
                    m_definitionRoot.AppendChild(ConvertContext(frames, m_definitionContainer));
                }

                
                Progress((double)++current / (double)steps, ref log);
            }

            // Upgrade dds
            m_handlerMappings = loader.LoadResource(path, "handleddata/handlermap.xml");
            XmlNodeList handlers = GetDataHandlers(m_handlerMappings);
            if (handlers != null)
            {
                Log(log, "Upgrading dynamic data store, custom types will be removed");

                foreach(XmlNode handler in handlers)
                {
                    if (handler.Attributes["path"] == null) continue;

                    string handlerPath = handler.Attributes["path"].Value.TrimStart('/');
                    XmlDocument ddsDefinition = loader.LoadResource(path, handlerPath);

                    if (ddsDefinition == null) continue;

                    UpgradeHandler(ddsDefinition, log);

                    newData.Add(handlerPath, ddsDefinition);
                }

            }

            // Move visitor groups
            XmlNode visitorGroups = GetVisitorGroups(m_dataContainer);
            if (visitorGroups != null)
            {
                Log(log, "Upgrading visitor groups");

                lock (m_dataRoot)
                {
                    m_dataRoot.RemoveChild(visitorGroups);
                }               

                m_postDataRoot.AppendChild(ConvertContext(visitorGroups, m_postDataContainer));
                Progress((double)++current / (double)steps, ref log);
            }

            // Move & upgrade page types
            XmlNode pageTypes = GetPageTypes(m_dataContainer);
            if (pageTypes != null)
            {
                Log(log, "Upgrading page types");

                lock (m_dataRoot)
                {
                    pageTypes = RenameNode(pageTypes, "contenttypes");
                    m_dataRoot.RemoveChild(pageTypes);
                    UpgradePageTypes(pageTypes);
                }

                m_definitionRoot.AppendChild(ConvertContext(pageTypes, m_definitionContainer));
                Progress((double)++current / (double)steps, ref log);
            }

            Log(log, "Descoping scope protected PageTypeIds and verifying data integrity");

            // Repair page data if previously modified for coherence
            XmlNodeList modifiedPages = GetModifiedPageData(m_dataContainer);
            if (modifiedPages != null)
                RepairPageData(modifiedPages, m_dataContainer, log);

            Progress((double)++current / (double)steps, ref log);
            

            // Upgrade page data, dont work on page data in parallel with repair
            Log(log, "Upgrading page data");

			XmlNodeList pageDataNodes = GetPageData(m_dataContainer);

			if (pageDataNodes != null)
            {
                foreach (XmlElement pageDataNode in pageDataNodes)
                    UpgradePageData(pageDataNode);
            }
				

            Progress((double)++current / (double)steps, ref log);

			// Update file version of data container
			m_dataRoot.SetAttribute("version", "3");

            // Construct data package to be injected into zip file
            newData.Add("epix.xml", m_dataContainer);

            if (m_definitionRoot.HasChildNodes)
                newData.Add("epiDefinition.xml", m_definitionContainer);

            if (m_postDataRoot.HasChildNodes)
                newData.Add("epiPostContent.xml", m_postDataContainer);

            Log(log, "Committing to file");

            Progress((double)++current / (double)steps, ref log);    

			RepackageData(path, newData);

            Progress((double)++current / (double)steps, ref log);

            Log(log, "Operation over");

            Complete();
            m_operation.OperationCompleted();            
		}

		
		public XmlDocument GetEmptyExportStructure(string suffix, int version)
		{
			XmlDocument result = new XmlDocument();

			XmlElement root = result.CreateElement("export" + suffix);
			root.SetAttribute("culture", "en-US");
			root.SetAttribute("version", version.ToString());

			result.AppendChild(root);

			return result;
		}

		public XmlNamespaceManager GetNameSpaceManager(XmlDocument document)
		{
			XmlNamespaceManager result = new XmlNamespaceManager(document.NameTable);
			result.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			result.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");

			return result;
		}

		private void RepackageData(string path, Dictionary<string, XmlDocument> data)
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

		private StaticDataSource ConvertXmlToDataSource(XmlDocument doc)
		{
			Stream result = new MemoryStream();
			doc.Save(result);

			StaticDataSource dataSource = new StaticDataSource();
			dataSource.SetStream(result);

			return dataSource;
		}

        private XmlNodeList GetModifiedPageData(XmlDocument doc)
        {
            return doc.SelectNodes("/export[1]/pages/TransferPageData/RawPageData/Property/RawProperty/Name[.='PageTypeID'][1]/following-sibling::Value[starts-with(.,'*')][1]/../../../..");
        }
		private XmlNode GetDynamicProperties(XmlDocument doc)
		{
            return doc.SelectSingleNode("/export/dynamicpropertydefinitions");
		}

        private XmlNodeList GetDataHandlers(XmlDocument doc)
        {
            return doc.SelectNodes("/handlersinfo/handler");
        }

        private XmlNode GetFrames(XmlDocument doc)
        {
            return doc.SelectSingleNode("/export/frames");
        }

        private XmlNode GetVisitorGroups(XmlDocument doc)
        {
            return doc.SelectSingleNode("/export/visitorGroups");
        }

        private XmlNode GetCategories(XmlDocument doc)
        {
            return doc.SelectSingleNode("/export/categories");
        }

        private XmlNode GetTabs(XmlDocument doc)
        {
            return doc.SelectSingleNode("/export/tabdefinitions");
        }

		private XmlNode GetPageTypes(XmlDocument doc)
		{
            return doc.SelectSingleNode("/export/pagetypes");
		}

		private XmlNodeList GetPageData(XmlDocument doc)
		{
            return doc.SelectNodes("/export/pages");
		}

        private void UpgradeHandler(XmlNode node, ILog log = null)
        {
            List<string> invalidReferences = new List<string>();

            XmlNodeList ddsTypes = node.SelectNodes("/objects/object");

            foreach(XmlNode ddsType in ddsTypes)
            {
                string ddsStore = ddsType.Attributes["storename"].Value;

                if (m_allowedDynamicStores.Contains(ddsStore) || ddsStore.StartsWith(m_filterAllowedDynamicCriteria) || ddsStore.StartsWith(m_filterSystemAllowedType))
                {
                    if (ddsStore == "VisitorGroupCriterion")
                    {
                        string criterionType = ddsType.SelectSingleNode("*[6]").InnerText;
                        if (!criterionType.StartsWith(m_filterAllowedDynamicCriteria))
                        {
                            Log(log, string.Format("Found unknown custom VisitorGroupCriterion '{0}', removing it and referring criteria values from VisitorGroups", criterionType), MessageType.Warning);

                            invalidReferences.Add(ddsType.Attributes["id"].Value);
                            ddsType.ParentNode.RemoveChild(ddsType);
                        }
                    }
                }
                else
                {
                    Log(log, string.Format("Removed unknown custom Type '{0}' from dds", ddsType.Attributes["storename"].Value), MessageType.Warning);
                    ddsType.ParentNode.RemoveChild(ddsType);
                }
            }

            foreach(string id in invalidReferences)
            {
                XmlNodeList referringNodes = node.SelectNodes(string.Format("/objects/object/*[1]/ref[.='{0}']/../..", id));

                if (referringNodes != null)
                {
                    foreach(XmlNode referringNode in referringNodes)
                    {
                        XmlNode reference = referringNode.SelectSingleNode(string.Format("*[1]/ref[.='{0}']", id));
                        XmlNodeList criteriaValues = referringNode.SelectNodes(string.Format("property[@name='Criteria']//value[@refId='{0}']", id));

                        if (criteriaValues != null)
                        {
                            foreach(XmlNode criteriaValue in criteriaValues)
                            {
                                criteriaValue.ParentNode.RemoveChild(criteriaValue);
                            }
                        }
                    }
                }
            }
        }

		private void UpgradeDynamicProperties(XmlNode node, ILog log = null)
		{
            XmlDocument document = node.OwnerDocument;
            XmlNodeList properties = node.SelectNodes("./ArrayOfPageDefinition/PageDefinition");

			foreach (XmlNode property in properties)
			{
                XmlNode dataType = property.SelectSingleNode("./Type/TypeName") ?? property.SelectSingleNode("./Type/DataType");
                XmlNode name = property.SelectSingleNode("Name");

                if (!m_allowedDataTypes.Contains(dataType.InnerText))
                {
                    Log(log, string.Format("Found incompatible or obsolete dynamic property '{0}' of type {1}, removing", name.InnerText, dataType.InnerText), MessageType.Warning);
                    property.ParentNode.RemoveChild(property);
                }                   

                XmlNode attribute = property.SelectSingleNode("./LongStringSettings");

				if (attribute != null)
                {
                    lock(document)
                    {
                        property.RemoveChild(attribute);
                    }
                }
					
                lock (document)
                {
                    attribute = CreateTextNode("ExistsOnModel", "false");
                    property.AppendChild(attribute);
                }
			}
		}

		private void UpgradePageData(XmlNode node)
		{
            XmlDocument document = node.OwnerDocument;
			XmlNodeList pages = node.SelectNodes("./TransferPageData");

			foreach (XmlNode page in pages)
			{
                lock (document)
                {
                    XmlNode newPage = RenameNode(page, "TransferContentData");

                    XmlElement rawPage = (XmlElement)newPage.SelectSingleNode("./RawPageData");
                    rawPage = (XmlElement)RenameNode(rawPage, "RawContentData");

                    if (rawPage.HasAttribute("type", "http://www.w3.org/2001/XMLSchema-instance"))
                    {
                        rawPage.RemoveAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
                    }
                    else
                    {
                        //rawPage.SetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", "RawPage");
                    }

                    rawPage.AppendChild(CreateEmptyNode("BlockProperties"));
                }
			}
		}

		private void UpgradePageTypes(XmlNode node)
		{
			if (node == null) return;

            XmlDocument document = node.OwnerDocument;
            XmlNode array = node.SelectSingleNode("./ArrayOfPageType");

			if (array == null) return;

            lock (document)
            {
                array = RenameNode(array, "ArrayOfContentTypeTransferObject");
            }
            
            XmlNodeList pageTypes = array.SelectNodes("./PageType");

            foreach (XmlNode pageType in pageTypes)
            {
                XmlElement newType = (XmlElement)RenameNode(pageType, "ContentTypeTransferObject");

                if (newType.Attributes["xsi:type"] == null)
                {
                    lock (document)
                    {
                        newType.SetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", "PageTypeTransferObject");
                    }
                }		

                XmlNode fileName = newType.SelectSingleNode("./FileName");
                if (fileName != null)
                {
                    lock (document)
                    {
                        newType.RemoveChild(fileName);
                    }
                }

                XmlNode exportFileName = newType.SelectSingleNode("./ExportableFileName");
                if (exportFileName != null)
                {
                    lock (document)
                    {
                        newType.RemoveChild(exportFileName);
                    }
                }

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

        private void RepairPageData(XmlNodeList data, XmlDocument document, ILog log)
        {            
            foreach (XmlNode page in data)
            {
                // PageTypeIds
                //XmlNode pageTypeIdProperty = page.SelectSingleNode("./RawPageData/Property/RawProperty/Name[.='PageTypeID']/../Value");
                // Optimizing, a bit premature
                XmlNode pageTypeIdProperty = page.SelectSingleNode("./RawPageData[1]/Property[1]/RawProperty/Name[.='PageTypeID'][1]/following-sibling::Value[1]");
                
                if (pageTypeIdProperty != null)
                {
                    if (!pageTypeIdProperty.InnerText.StartsWith("*")) continue;

                    lock (document)
                    {
                        pageTypeIdProperty.InnerText = pageTypeIdProperty.InnerText.Substring(1);
                    }
                }

                // Properties that point to pagetypes
                //XmlNodeList properties = page.SelectNodes("./RawPageData/Property/RawProperty/Type[.='PageType']/../Value");
                // Optimizing, a bit premature
                XmlNodeList properties = page.SelectNodes("./RawPageData[1]/Property[1]/RawProperty/Type[.='PageType'][1]/following-sibling::Value[1]");

                foreach(XmlNode property in properties)
                {
                    if (!property.InnerText.StartsWith("*")) continue;

                    lock (document)
                    {
                        property.InnerText = property.InnerText.Substring(1);
                    }
                }

                // PropertyDefinitionIDs, always first, easily optimized selector
                XmlNodeList definitions = page.SelectNodes("./RawPageData[1]/Property[1]/RawProperty/*[1 and .!='0'][1]/..");

                foreach(XmlNode property in definitions)
                {
                    XmlNode type = property.SelectSingleNode("Type");

                    // Opportunity to repair broken link collections
                    if (type.InnerText == "LinkCollection")
                    {
                        XmlNode value = property.SelectSingleNode("Value");

                        if (value != null)
                        {
                            value.InnerText = CheckRepairLinkCollection(pageTypeIdProperty.InnerText, value.InnerText, log);
                        }
                            
                    }

                    XmlNode definition = property.SelectSingleNode("./*[1]");

                    if (!definition.InnerText.StartsWith("*")) continue;

                    lock (document)
                    {
                        definition.InnerText = definition.InnerText.Substring(1);
                    }
                }
            }
        }

        private string CheckRepairLinkCollection(string pageTypeId, string content, ILog log)
        {
            if (string.IsNullOrEmpty(content)) return content;

			// Remove inserted <br /> tags, possible to use editor this way...
			// Impossible to import...
            content = m_breaks.Replace(content, string.Empty);

            XmlDocument list = new XmlDocument();
            list.LoadXml(content);

            XmlNodeList links = list.SelectNodes("/ul[1]/li/a");

            if (links != null)
            {
                foreach (XmlNode link in links)
                {
                    XmlNode listItem = link.ParentNode;

					// Remove nonreferring nodes. Just plain stupid to keep.
                    if (link.Attributes["href"] == null)
                    {
                        listItem.ParentNode.RemoveChild(listItem);
                        continue;
                    }                        

                    string href = link.Attributes["href"].Value;

					// If PermanentLink, sanitize it.
					// Seen some examples that have ?epslanguage=sv,sv,sv,sv
					// Should be ?epslanguage=sv
                    Match match = m_permanentLink.Match(href);

                    if (match.Success)
                    {
						if (href.IndexOf("?") > -1)
						{
                            link.Attributes["href"].Value = match.Groups[0].Value;

							var collection = GetQueryString(href);

							if (collection.ContainsKey("epslanguage")) 
							{
								var query = "?epslanguage=" + GetCulturalCorrectParameter(collection["epslanguage"]);
								link.Attributes["href"].Value += query;
							}

							Log(log, string.Format("Restructured query parameters from PermanentLink in LinkCollection, PageTypeId {0} -> {1}", pageTypeId, href), MessageType.Warning);
						}       
                    }
                    else
                    {
						// If an external, user definied url, make sure that it is a valid URI,
						// The importer will not buy broken urls.
                        Uri result;
                        bool validuri = Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out result);

						if (!validuri)
						{
							listItem.ParentNode.RemoveChild(listItem);
							Log(log, string.Format("Removing non-conforming url from LinkCollection, PageTypeId {0} -> {1}", pageTypeId, href), MessageType.Warning);
						}
                            
                    }
                }
            }

			return list.OuterXml;
        }

		private string GetCulturalCorrectParameter(string value)
		{
			MatchCollection matches = m_cultureInfo.Matches(value);

			foreach(Match match in matches)
			{
				if (match.Success)
					return match.Groups[1].Value;
			}
			
			return null;
		}

		private Dictionary<string, string> GetQueryString(string href)
		{
			var result = new Dictionary<string, string>();
			var index = href.IndexOf("?");

			if (index < 0) return result;

			href = href.Substring(index + 1);
			string[][] data = href.Split('&').Select(s => s.Split('=')).ToArray();

			for (int i = 0; i < data.Length; i++ )
			{
				var key = data[i][0];
				var value = data[i][1];

				if (!string.IsNullOrEmpty(key))
					key = key.ToLower();

				if (!result.ContainsKey(key) && !string.IsNullOrEmpty(value))
					result.Add(key, value);
			}

			return result;
		}

		private XmlNode RenameNode(XmlNode node, string name)
		{
			XmlDocument doc = node.OwnerDocument;
			XmlNodeList children = node.ChildNodes;

			XmlElement newNode = (XmlElement)CreateEmptyNode(name);

			foreach (XmlAttribute attribute in node.Attributes)
				newNode.SetAttribute(attribute.Name, attribute.Value);
			
			foreach (XmlNode child in children)
				newNode.AppendChild(child);

            lock (doc)
            {
                node.ParentNode.ReplaceChild(newNode, node);
            }

			return newNode;
		}

		private XmlNode ConvertContext(XmlNode node, XmlDocument newContext)
		{
            if (node == null) return null;

			XmlElement result = (XmlElement)newContext.CreateElement(node.LocalName);

			foreach (XmlAttribute attribute in node.Attributes)
				result.SetAttribute(attribute.Name, attribute.Value);

			result.InnerXml = node.InnerXml;

			return result;
		}

        

		private XmlNode CreateTextNode(string name, string value)
		{
			XmlNode result = m_dataContainer.CreateElement(name);
			result.AppendChild(m_dataContainer.CreateTextNode(value));
			return result;
		}

		private XmlNode CreateEmptyNode(string name)
		{
			return m_dataContainer.CreateElement(name);
		}

        public int GetFileVersion(XmlDocument doc)
        {
            XmlNode root = doc.FirstChild;

            if (root == null || root.Attributes["version"] == null)
                return -1;

            int version = -1;
            int.TryParse(root.Attributes["version"].Value, out version);

            return version;
        }

		private void VersionCheck(XmlDocument doc)
		{
			XmlNode root = doc.FirstChild;

			if (root == null || root.Attributes["version"] == null)
				throw new ApplicationException("Incompatible data");

			if (root.Attributes["version"].Value == "3")
				throw new ApplicationException("Data already upgraded");

			else if (root.Attributes["version"].Value != "1")
				throw new ApplicationException("Incompatible data");
		}

        #region Events

        private void OnTriggerProgress(ProgressLogEventArgs args)
        {
            if (OnUpgradeProgress != null)
                OnUpgradeProgress.Invoke(this, args);
        }

        private void OnTriggerComplete(EventArgs args)
        {
            if (OnUpgradeComplete != null)
                OnUpgradeComplete.Invoke(this, args);
        }

        private void Progress(double progress, ref ILog log)
        {
            if (progress > -1)
                m_lastprogress = (int)(progress * 100);
            else
                progress = m_lastprogress;

            var args = new ProgressLogEventArgs(m_lastprogress, log.GetLogChanges());

            Post((a) => { OnTriggerProgress(a); }, args);
        }

        private void Complete()
        {
            Post((a) => { OnTriggerComplete(a); }, new EventArgs());
        }

        private void Post<T>(Action<T> action, T arg)
        {
            m_operation.Post((p) => action((T)p), arg);
        }

        #endregion

        #region Logging

        private void Log(ILog log, string message, MessageType type = MessageType.Message)
        {
            if (log == null) return;

            lock (m_lock)
            {
                log.Log(message, type);
            }

            Progress(-1, ref log);
        }

        #endregion
    }

    
}
