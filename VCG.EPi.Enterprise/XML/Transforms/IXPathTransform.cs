﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace VCG.EPi.Enterprise.Xml.Transforms
{
	public interface IXPathTransform
	{
		XmlDocument Transform(XmlDocument source);
	}
}
