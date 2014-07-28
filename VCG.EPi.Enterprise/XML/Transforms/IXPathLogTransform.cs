using VCG.EPi.Enterprise.Logging;

namespace VCG.EPi.Enterprise.Xml.Transforms
{
	public interface IXPathLogTransform : IXPathTransform
	{
        event LogEventHandler OnLog;
	}
}
