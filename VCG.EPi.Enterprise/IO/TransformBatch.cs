using System.Collections.Generic;
using System.IO;
using System.Xml;
using VCG.EPi.Enterprise.Xml.Transforms;
using ICSharpCode.SharpZipLib.Zip;

namespace VCG.EPi.Enterprise.IO
{
	public class TransformBatch
	{
		protected int m_bufferSize = 4096;

		protected List<StaticDataSource> m_references = new List<StaticDataSource>();
		protected List<IXPathTransform> m_transforms = new List<IXPathTransform>();
		protected List<string> m_affectedFiles = new List<string>() { "epix.xml" };

		public List<IXPathTransform> Transforms
		{
			get { return m_transforms; }
			set { m_transforms = value; }
		}

		public TransformBatch()
		{

		}

		public TransformBatch(IEnumerable<IXPathTransform> transforms)
		{
			foreach (IXPathTransform transform in transforms)
				m_transforms.Add(transform);
		}

		public void Run(string path)
		{
			using (FileStream file = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
			{
				Batch(file);
			}
		}

		public void Run(Stream stream)
		{
			Batch(stream);
		}

		private void Batch(Stream stream)
		{
			using (ZipFile zip = new ZipFile(stream))
			{
				zip.IsStreamOwner = false;
				zip.BeginUpdate();

				foreach (ZipEntry entry in zip)
				{
					if (!entry.IsFile) continue;

					foreach (string affectedFile in m_affectedFiles)
					{
						if (!entry.Name.Contains(affectedFile)) continue;

						StaticDataSource source = ProcessZipEntry(zip, entry, affectedFile);

						zip.Add(source, affectedFile);
						m_references.Add(source);
					}
				}

				zip.CommitUpdate();
				zip.Close();

				foreach (StaticDataSource source in m_references)
					source.Dispose();

				m_references.Clear();
			}
			
		}

		protected StaticDataSource ProcessZipEntry(ZipFile zip, ZipEntry entry, string fileName) 
		{
			byte[] buffer = new byte[m_bufferSize];
			Stream stream = zip.GetInputStream(entry);

			return ProcessTransforms(stream);
		}

		protected StaticDataSource ProcessTransforms(Stream stream)
		{
			XmlDocument xmldoc = new XmlDocument();
			xmldoc.Load(stream);

			foreach (IXPathTransform transform in m_transforms)
				xmldoc = transform.Transform(xmldoc);

			Stream result = new MemoryStream();
			xmldoc.Save(result);

			StaticDataSource dataSource = new StaticDataSource();
			dataSource.SetStream(result);

			return dataSource;
		}

	}
}
