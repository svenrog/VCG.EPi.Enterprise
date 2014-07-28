using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using VCG.EPi.Enterprise.IO;
using VCG.EPi.Enterprise.Xml;
using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Optimization;
using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;
using VCG.EPi.Enterprise.Xml.Transforms;
using VCG.EPi.Enterprise.Migration.Toolset.Data;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
    public class PageTypeUpgradeTransform : LogTransform
    {
        //protected readonly string _transferSelectionFormat = "//pages/TransferPageData/RawPageData/Property/RawProperty/Name[.='PageTypeID']/../Value[.='{0}']/../../../..";
        // Optimizing slow performance (needed)
        protected readonly string _transferPropertySelectionFormat = "/export[1]/pages[1]/TransferPageData/RawPageData[1]/Property/RawProperty/Name[.='PageTypeID'][1]/following-sibling::Value[.='{0}'][1]/../../../..";

        //protected readonly string _transferReferringPropertiesFormat = "//pages/TransferPageData/RawPageData/Property/RawProperty/Type[.='PageType']/../Value[.='{0}']";
        // Optimizing slow performance (needed, needs more)
        protected readonly string _transferReferringPropertiesFormat = "/export[1]/pages[1]/TransferPageData/RawPageData[1]/Property/RawProperty/Type[.='PageType'][1]/following-sibling::Value[.='{0}'][1]";

        protected readonly string _pagePropertySelectionFormat = "./RawPageData[1]/Property/RawProperty/Name[.='{0}']/following-sibling::Value[1]";
        protected readonly string _pageAllPropertiesSelectionFormat = "./RawPageData[1]/Property/RawProperty";
        protected readonly string _pageIndexedPropertiesSelectionFormat = "./RawPageData[1]/Property/RawProperty[{0}]";
        protected readonly string _pageTypeSelectionFormat = "/export[1]/pagetypes[1]/ArrayOfPageType[1]/PageType/ID[.='{0}'][1]/..";
        
        protected readonly string _pageSourceSelectionFormat = "/export[1]/pages[1]";
        protected readonly string _blockTargetSelectionFormat = "/export[1]/pages[2]";
        

        XmlTargetMapping _mapping;
        Dictionary<string, PropertyDefinition> _definitions = new Dictionary<string, PropertyDefinition>();

        public PageTypeUpgradeTransform(XmlTargetMapping mapping, ILog log = null)
        {
            _mapping = mapping;

            if (mapping.MatchingProperties != null)
                _definitions = mapping.MatchingProperties.ToDictionary(p => p.Name, p => p);

            _log = log;
        }

        public override XmlDocument Transform(XmlDocument source)
        {
            XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);

            string expression = string.Format(_pageTypeSelectionFormat, _mapping.Source.Id);
            XmlNode pageType = source.SelectSingleNode(expression, manager);

            if (pageType != null)
            {
                XmlNode pageTypeName = pageType.SelectSingleNode("./Name");

                lock (source)
                {
                    Log(string.Format("Removing PageType '{0}', already present in target", pageTypeName.InnerText));
                    pageType.Remove();
                }
            }

            // Change pageTypeId of all pages with this pagetype
            expression = string.Format(_transferPropertySelectionFormat, _mapping.Source.Id);
            XmlNodeList pageData = source.SelectNodes(expression, manager);
            XmlNode pageDataSource = source.SelectSingleNode(_pageSourceSelectionFormat);
            XmlNode blockDataTarget = source.SelectSingleNode(_blockTargetSelectionFormat);
            bool isBlock = _mapping.Target.Type == ContentType.Block;

            if (isBlock && blockDataTarget == null) 
                blockDataTarget = source.FirstChild.InsertAfter(source.CreateElement("pages"), pageDataSource);

            if (pageData != null)
            {
                //Log(string.Format("Changing PageData for {0} - PageTypeId: {1} -> {2} (scope protected)", _mapping.Source.Name, _mapping.Source.Id, _mapping.Target.Id));

                foreach (XmlNode page in pageData)
                {
                    lock (source)
                    {
                        XmlAttribute attribute = null;

                        if (!isBlock && page.FirstChild.Attributes["xsi:type"] == null)
                        {
                            attribute = source.CreateAttribute("xsi:type");
                            attribute.Value = "RawPage";
                        }

                        if (attribute != null)
                            page.FirstChild.Attributes.Append(attribute);

                        if (isBlock)
                        {
                            pageDataSource.RemoveChild(page);
                            blockDataTarget.AppendChild(page);
                        }

                        expression = string.Format(_pagePropertySelectionFormat, "PageTypeID");
                        XmlNode pageTypeIdProperty = page.SelectSingleNode(expression, manager);

                        expression = string.Format(_pagePropertySelectionFormat, "PageGUID");
                        XmlNode pageGuidProperty = page.SelectSingleNode(expression, manager);

                        if (pageTypeIdProperty != null)
                        {
                            // Protect the target id from further modification down the pipe
                            pageTypeIdProperty.InnerText = "*" + _mapping.Target.Id.ToString();

                            // Upgrade custom data, reason: it's the devil.
                            XmlNode customData = pageTypeIdProperty.SelectSingleNode("./../CustomData");
                            UpgradeCustomData(customData, source);
                        }

                        expression = string.Format(_pagePropertySelectionFormat, "PageTypeName");
                        XmlNode pageTypeNameProperty = page.SelectSingleNode(expression, manager);

                        if (pageTypeNameProperty != null)
                        {
                            lock (source)
                            {
                                pageTypeNameProperty.InnerText = _mapping.Target.Name;
                            }
                        }

                        XmlNodeList properties = page.SelectNodes(_pageAllPropertiesSelectionFormat, manager);

                        if (properties != null)
                        {
                            //Log(string.Format("Remapping PropertyIds for {0} - {1}", _mapping.Source.Name, pageGuidProperty.InnerText));

                            foreach (XmlNode property in properties)
                            {
                                XmlNode definitionId = property.SelectSingleNode("./PageDefinitionID");

                                // Continue if internal EPiServer property
                                if (definitionId.InnerText == "0")
                                    continue;

                                XmlNode propertyName = property.SelectSingleNode("./Name");

                                // If this isn't a matching property, remove it
                                if (!_definitions.ContainsKey(propertyName.InnerText))
                                {
                                    Log(string.Format("Removing property from {0} - {2} - '{1}': No match in target", _mapping.Source.Name, propertyName.InnerText, pageGuidProperty.InnerText));
                                    property.ParentNode.RemoveChild(property);
                                    continue;
                                }
                                else if (_definitions[propertyName.InnerText].Name != propertyName.InnerText)
                                {
                                    // Rename if aliased
                                    propertyName.InnerText = _definitions[propertyName.InnerText].Alias;
                                }

                                //Log(string.Format("Remapping property from {0} - {1} to target, {2} -> {3}", _mapping.Source.Name, propertyName, definitionId.InnerText, _definitions[propertyName].Id));

                                // Remap property Id to target (scope protect)
                                definitionId.InnerText = "*" + _definitions[propertyName.InnerText].Id.ToString();
                            }
                        }
                    }
                }

                XmlNodeList referredProperties = source.SelectNodes(string.Format(_transferReferringPropertiesFormat, _mapping.Source.Id));

                if (referredProperties != null)
                {
                    if (referredProperties.Count > 0)
                    {
                        lock (_log)
                        {
                            Log(string.Format("Remapping referring properties for {0} (scope protected)", _mapping.Source.Name));
                        }
                    }

                    foreach (XmlNode referredProperty in referredProperties)
                    {
                        referredProperty.InnerText = "*" + _mapping.Target.Id.ToString();

                        // Upgrade custom data, reason: it's the devil.
                        XmlNode customData = referredProperty.SelectSingleNode("./../CustomData");
                        UpgradeCustomData(customData, source);
                    }
                }
            }

            return source;
        }

        protected void UpgradeCustomData(XmlNode customDataNode, XmlDocument source)
        {
            if (customDataNode == null) return;

            foreach (XmlNode child in customDataNode)
                child.Remove();

            XmlNode metaData = source.CreateElement("RawNameAndXml");
            XmlNode name = source.CreateElement("Name");
            XmlNode xml = source.CreateElement("Xml");

            name.InnerText = "PageTypeName";
            xml.InnerText = string.Format("<string>{0}</string>", _mapping.Target.Name);

            metaData.AppendChild(name);
            metaData.AppendChild(xml);

            lock (source)
            {
                customDataNode.AppendChild(metaData);
            }
        }
    }
}
