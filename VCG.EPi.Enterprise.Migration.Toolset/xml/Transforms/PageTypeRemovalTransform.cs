using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using VCG.EPi.Enterprise.IO;
using VCG.EPi.Enterprise.Xml.Transforms;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;
using VCG.EPi.Enterprise.Logging;


namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
	public class PageTypeRemovalTransform : LogTransform
	{
		//protected readonly string _transferSelectionFormat = "//pages/TransferPageData/RawPageData/Property/RawProperty/Name[.='PageTypeID']/../Value[.='{0}']/../../../..";
        // Optimizing slow performance (needed)
        protected readonly string _transferSelectionFormat = "/export[1]/pages[1]/TransferPageData/RawPageData[1]/Property/RawProperty/Name[.='PageTypeID'][1]/following-sibling::Value[.='{0}'][1]/../../../..";

		//protected readonly string _transferParentSelectionFormat = "//pages/TransferPageData/RawPageData/Property/RawProperty/Name[.='PageParentLink']/../Value[contains(., '{0}')]/../../../..";
        // Optimizing slow performance (needed)
        protected readonly string _transferParentSelectionFormat = "/export[1]/pages[1]/TransferPageData/RawPageData[1]/Property/RawProperty/Name[.='PageParentLink'][1]/following-sibling::Value[contains(., '{0}')][1]/../../../..";

        //protected readonly string _transferFileSelectionFormat = "//files/FileTransferObject/PageGuid[.='{0}']/..";
        // Optimizing slow performance (needed)
        protected readonly string _transferFileSelectionFormat = "./export[1]/files[1]/FileTransferObject/*[3 and .='{0}'][1]/..";

		protected readonly string _pageGuidSelectionFormat = "./RawPageData[1]/Property/RawProperty/Name[.='PageGUID']/following-sibling::Value[1]";
		protected readonly string _pageTypeSelectionFormat = "/export[1]/pagetypes[1]/ArrayOfPageType[1]/PageType/ID[.='{0}'][1]/..";
		
		protected string _removePageTypeId;

		public PageTypeRemovalTransform(int pageTypeId, ILog log = null)
		{
			_removePageTypeId = pageTypeId.ToString();
            _log = log;
		}

		public override XmlDocument Transform(XmlDocument source)
		{
			string expression = string.Format(_pageTypeSelectionFormat, _removePageTypeId);
			
			XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);
			XmlNode pageType = source.SelectSingleNode(expression, manager);
            
            if (pageType != null)
            {
                XmlNode pageTypeName = pageType.SelectSingleNode("./Name");
                
                lock (source)
                {
                    Log(string.Format("Removing PageType {0} - '{1}', not mapped by user", _removePageTypeId, pageTypeName.InnerText));
                    pageType.Remove();
                }
                
            }

			expression = string.Format(_transferSelectionFormat, _removePageTypeId);

			XmlNodeList pageData = source.SelectNodes(expression, manager);

            lock (pageData)
            {
                foreach (XmlNode page in pageData)
                {
                    Guid guid = GetPageGuid(page, manager);

                    if (guid.Equals(Guid.Empty)) continue;

                    Log(string.Format("Removing PageData {0}, is of PageType {1}", guid, _removePageTypeId));

                    RemovePageWithParentRecursive(page, guid);

                    lock (source)
                    {
                        page.Remove();
                    }
                }
            }

			return source;
		}

		public Guid GetPageGuid(XmlNode page, XmlNamespaceManager manager)
		{
			XmlNode pageGuid = page.SelectSingleNode(_pageGuidSelectionFormat, manager);

			if (pageGuid == null) return Guid.Empty;
			return Guid.Parse(pageGuid.InnerText);
		}

		public void RemovePageWithParentRecursive(XmlNode element, Guid guid)
		{
			XmlDocument source = element.OwnerDocument;
			XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);

			string selectionExpression = string.Format(_transferParentSelectionFormat, guid.ToString("N"));
			XmlNodeList nodes = source.SelectNodes(selectionExpression, manager);

			if (nodes == null) return;

			foreach (XmlNode node in nodes)
			{
				Guid pageguid = GetPageGuid(node, manager);

                Log(string.Format("Removing PageData {0}, parent {1} will be unreacheable", pageguid, guid));

                if (!pageguid.Equals(Guid.Empty))
                {
                    RemoveFilesFromPageWithGuid(source, manager, pageguid);
                    RemovePageWithParentRecursive(node, pageguid);
                }

                node.Remove();
			}
		}

        public void RemoveFilesFromPageWithGuid(XmlDocument document, XmlNamespaceManager manager, Guid guid)
        {
            string expression = string.Format(_transferFileSelectionFormat, guid.ToString());
            XmlNodeList files = document.SelectNodes(expression, manager);

            if (files != null)
            {
                lock (files)
                {
                    foreach (XmlNode file in files)
                    {
                        string fileName = file.SelectSingleNode("*[2]").InnerText;

                        Log(string.Format("Removing File {0} in PageFiles, owner {1} has been removed", fileName, guid));
                        file.Remove();
                    }
                }
            }
        }
	}
}
