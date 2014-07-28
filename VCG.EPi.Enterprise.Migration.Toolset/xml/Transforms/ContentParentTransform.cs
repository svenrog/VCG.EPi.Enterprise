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
    public class ContentParentTransform : ContentTransform
    {
		protected readonly string _transferSelectionFormat = "/export[1]/pages[1]/TransferPageData/RawPageData[1]/Property/RawProperty/Name[.='PageTypeID'][1]/following-sibling::Value[.='{0}'][1]/../../../..";
        protected readonly string _pagePropertySelectionFormat = "./RawPageData[1]/Property/RawProperty/Name[.='{0}']/following-sibling::Value[1]";

		protected string _remapPageTypeId;
		protected Guid _guid;

		public ContentParentTransform(int pageTypeId, Guid guid, ILog log = null)
        {
			_remapPageTypeId = pageTypeId.ToString();
			_guid = guid;
			_log = log;
        }

        public override XmlDocument Transform(XmlDocument source)
        {
            XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);

            // Change parent of all pages with this pagetype
			string expression = string.Format(_transferSelectionFormat, _remapPageTypeId);
            XmlNodeList pageData = source.SelectNodes(expression, manager);

            if (pageData != null)
            {
				var count = 0;

                foreach (XmlNode page in pageData)
                {
                    lock (source)
                    {
						expression = string.Format(_pagePropertySelectionFormat, "PageParentLink");
                        XmlNode pageParentLinkProperty = page.SelectSingleNode(expression, manager);

						if (pageParentLinkProperty != null)
						{
							pageParentLinkProperty.InnerText = GetPageLink(_guid);
							count++;
						}
                    }
                }

				Log(string.Format("Remapped {0} entities to new parent '{1}'", count, _guid));
            }

            return source;
        }
    }
}
