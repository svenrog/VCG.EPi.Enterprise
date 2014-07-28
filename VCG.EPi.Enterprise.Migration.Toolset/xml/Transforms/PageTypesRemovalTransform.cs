using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using VCG.EPi.Enterprise.IO;
using VCG.EPi.Enterprise.Xml.Transforms;
using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;


namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
	public class PageTypesRemovalTransform : LogTransform
	{
		protected readonly string _pageTypeSelectionFormat = "/export[1]/pagetypes[1]";

		public PageTypesRemovalTransform(ILog log = null)
		{
            _log = log;
		}

		public override XmlDocument Transform(XmlDocument source)
		{			
			XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(source);
			XmlNode pageTypes = source.SelectSingleNode(_pageTypeSelectionFormat, manager);
            
            if (pageTypes != null)
            {
                lock (source)
                {
                    Log(string.Format("Removing all remaining PageTypes, not needed in import"));

					pageTypes.Remove();
                }
                
            }

			return source;
		}
	}
}
