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

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
    public class AddBlockFolderTransform : ContentTransform
	{
        protected Guid _guid;

		public AddBlockFolderTransform(Guid guid, ILog log = null)
		{
            _guid = guid;
            _log = log;
        }

		public override XmlDocument Transform(XmlDocument source)
		{
            XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);

			try
			{
				XmlNode blockRoot = source.SelectSingleNode(_transferBlockRootSelectionFormat, manager);

                if (blockRoot == null)
                    blockRoot = source.FirstChild.InsertAfter(source.CreateElement("pages"), source.SelectSingleNode(_transferPageRootSelectionFormat, manager));

				blockRoot.PrependChild(CreateImportFolder(source, _guid));

				Log(string.Format("Added a content folder '{0}' to blocks", _guid));
			}
			catch (Exception)
			{
				Log(string.Format("Root node for content folder not found", _guid));
			}			

			return source;
		}

        protected XmlElement CreateImportFolder(XmlDocument context, Guid guid)
        {
            return CreateContentDataNode(context, CreateContentFolderProperties(context, guid));
        }

        protected IEnumerable<XmlElement> CreateContentFolderProperties(XmlDocument context, Guid guid)
        {
			yield return CreatePropertyDataNode(context, "-1", "String", "PageGUID", guid.ToString());
			yield return CreatePropertyDataNode(context, "-1", "PageReference", "PageLink", GetPageLink(guid));
			yield return CreatePropertyDataNode(context, "-1", "PageReference", "PageParentLink", GetPageLink(_contentFolderGuid));
			yield return CreatePropertyDataNode(context, "0", "String", "PageName", "Converted blocks", null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "PageType", "PageTypeID", "3", null, false, false, CreateCustomDataNode(context, "PageTypeName", "SysContentFolder"));
			yield return CreatePropertyDataNode(context, "-1", "Boolean", "PageDeleted", "False");
			yield return CreatePropertyDataNode(context, "-1", "String", "PageLanguageBranch", "sv", "EPiServer.SpecializedProperties.PropertyLanguage", false, true);
			yield return CreatePropertyDataNode(context, "-1", "String", "PageMasterLanguageBranch", "sv");
			yield return CreatePropertyDataNode(context, "-1", "Date", "PageChanged", GetDate(DateTime.Now), null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "String", "PageChangedBy", "DataUpgradeTool", null, false, true);
			yield return CreatePropertyDataNode(context, "4", "Date", "PageCreated", GetDate(DateTime.Now), null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "String", "PageCreatedBy", "DataUpgradeTool", null, false, true);
			yield return CreatePropertyDataNode(context, "1", "Boolean", "PageChangedOnPublish", "False", null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "Date", "PageSaved", null, null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "String", "PageDeletedBy", null, null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "Date", "PageDeletedDate");
			yield return CreatePropertyDataNode(context, "1", "String", "PageURLSegment", "converted-blocks", null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "String", "PageTypeName", "SysContentFolder");
			//yield return CreatePropertyDataNode(context, "-1", "Boolean", "EPi:ImplicitlyAdded", _contentFolderGuid.ToString());
			
			/*
			yield return CreatePropertyDataNode(context, "2", "Category", "PageCategory");
			yield return CreatePropertyDataNode(context, "-1", "Boolean", "PagePendingPublish", null, null, false, true);
			yield return CreatePropertyDataNode(context, "-1", "Number", "PageWorkStatus", "4", "EPiServer.SpecializedProperties.PropertyVersionStatus", true, true);
			yield return CreatePropertyDataNode(context, "-1", "Number", "PageFolderID", int.MaxValue.ToString(), null, true);
			
			yield return CreatePropertyDataNode(context, "-1", "String", "PageDeletedBy");
			yield return CreatePropertyDataNode(context, "-1", "Date", "PageDeletedDate");
			yield return CreatePropertyDataNode(context, "-1", "String", "PageContentAssetsID");
			yield return CreatePropertyDataNode(context, "-1", "String", "PageContentOwnerID");
			
			yield return CreatePropertyDataNode(context, "1", "Boolean", "PageVisibleInMenu", "True");
			yield return CreatePropertyDataNode(context, "1", "Number", "PageChildOrderRule", "1", "EPiServer.SpecializedProperties.PropertySortOrder");
			yield return CreatePropertyDataNode(context, "1", "Number", "PagePeerOrder", "100");
			yield return CreatePropertyDataNode(context, "1", "String", "PageExternalURL", null, "EPiServer.SpecializedProperties.PropertyVirtualLink", false, true);
			
			yield return CreatePropertyDataNode(context, "4", "Date", "PageStartPublish", GetDate(DateTime.Now), null, false, true);
			yield return CreatePropertyDataNode(context, "4", "Date", "PageStopPublish", null, null, false, true);
            
			yield return CreatePropertyDataNode(context, "4", "PageReference", "PageArchiveLink", null, null, false, true);
			yield return CreatePropertyDataNode(context, "3", "Number", "PageShortcutType", "0", "EPiServer.SpecializedProperties.PropertyLinkType", false, true);
			yield return CreatePropertyDataNode(context, "3", "PageReference", "PageShortcutLink", null, null, false, true);
			yield return CreatePropertyDataNode(context, "3", "Number", "PageTargetFrame", null, "EPiServer.SpecializedProperties.PropertyFrame", false, true);
            */
			
			//yield return CreatePropertyDataNode(context, "", "", "");			
        }
    }
}
