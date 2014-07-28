using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using VCG.EPi.Enterprise.IO;
using VCG.EPi.Enterprise.Migration.Toolset.Types;
using ProtoBuf;
using ICSharpCode.SharpZipLib.Zip;


namespace VCG.EPi.Enterprise.Migration.Toolset.Data
{
	[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class Document
    {
		[ProtoMember(1)]
        public string DocumentPath;
        
        public XmlDocument SourceFile;
		public XmlDocument TargetFile;

		[ProtoMember(2)]
        public string SourceFilePath;
		
		[ProtoMember(3)]
        public string TargetFilePath;

		public string SourceAbsolutePath { get { return PathHelper.GetAbsolutePath(DocumentPath, SourceFilePath); } }
		public string TargetAbsolutePath { get { return PathHelper.GetAbsolutePath(DocumentPath, TargetFilePath); } }

		[ProtoMember(4)]
        public List<XmlTargetMapping> SourceMappings;

		[ProtoMember(5)]
        public List<XmlTarget> Targets;

		public static event FileNotFoundEventHandler OnDocumentNotFound;
		public static event DocumentLoadedEventHandler OnDocumentLoaded;
		public static event FileImportedEventHandler OnFileImported;
		public static event ErrorEventHandler OnError;
		public static event ProgressStreamReportEventHandler OnDocumentLoadingProgress;

		public static AsyncOperation Operation;

        public void SaveAs(string path)
        {
            SaveInternal(this, path);
        }

        public void Save()
        {
            SaveInternal(this, DocumentPath);
        }

        public void Open(string path)
        {
            Document opened = OpenInternal(path);

            DocumentPath = opened.DocumentPath;
            SourceFilePath = opened.SourceFilePath;
            TargetFilePath = opened.TargetFilePath;
            SourceFile = opened.SourceFile;
            TargetFile = opened.TargetFile;
            SourceMappings = opened.SourceMappings;
            Targets = opened.Targets;
        }

		#region Loading

		public bool LoadSource(string path)
		{
			return LoadSource(path, false);
		}

        public bool LoadSource(string path, bool throwError)
        {
			bool absolute = Path.IsPathRooted(path);

			if (!absolute)
				path = PathHelper.GetAbsolutePath(DocumentPath, path);

			var loader = new EPiResourceLoader();
			loader.OnProgress += Loader_OnResourceProgress;

			XmlDocument document = loader.LoadResource(path, "epix.xml");
            int version = EPiDataHelper.GetFileVersion(document);

            if (version == 1)
            {
				SourceFilePath = PathHelper.GetRelativePath(DocumentPath, path);
                SourceFile = document;

				FileImported(Operation, new FileImportedEventArgs(document));

                return true;
            }

			if (throwError)
				Error(Operation, new ErrorEventArgs(new ApplicationException("Wrong EPiServer version on data. Must be 5 or 6")));

            return false;
        }


		public bool LoadTarget(string path)
		{
			return LoadTarget(path, false);
		}
		public bool LoadTarget(string path, bool throwError)
        {
			bool absolute = !Path.IsPathRooted(path);

			if (!absolute)
				path = PathHelper.GetAbsolutePath(DocumentPath, path);

			var loader = new EPiResourceLoader();
			loader.OnProgress += Loader_OnResourceProgress;

            XmlDocument document = loader.LoadResource(path, "epiDefinition.xml");
            int version = EPiDataHelper.GetFileVersion(document);

            if (version == 3)
            {
				TargetFilePath = PathHelper.GetRelativePath(DocumentPath, path);
                TargetFile = document;

				FileImported(Operation, new FileImportedEventArgs(document));
                return true;
            }
			
			if (throwError)
				Error(Operation, new ErrorEventArgs(new ApplicationException("Wrong EPiServer version on data. Must be 7")));

            return false;
        }

		public static void LoadOrPrompt(AsyncOperation operation, string path, Func<string, bool> reference)
		{
			try
			{
				reference(path);
			}
			catch (FileNotFoundException ex)
			{
				FileNotFound(operation, ex, reference);
			}
			catch (IOException)
			{
				FileNotFound(operation, new FileNotFoundException("Directory doesn't even exist", path), reference);
			}
		}

        private static Document OpenInternal(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            Document result = null;

            using (var inputFile = new FileStream(path, FileMode.Open))
			using (var compressionStream = new ZipInputStream(inputFile))
            {
				var entry = compressionStream.GetNextEntry();
				while (entry != null)
				{
					if (entry.Name == "data.bin")
						result = Serializer.Deserialize<Document>(compressionStream);

					entry = compressionStream.GetNextEntry();
				}
            }

            result.DocumentPath = path;

            if (!string.IsNullOrEmpty(result.SourceFilePath))
				LoadOrPrompt(Operation, result.SourceFilePath, result.LoadSource);

            if (!string.IsNullOrEmpty(result.TargetFilePath))
				LoadOrPrompt(Operation, result.TargetFilePath, result.LoadTarget);

			Loaded(Operation, result);

			return result;
        }

		

		#endregion

		#region Events & triggers

		private void Loader_OnResourceProgress(object sender, ProgressStreamReportEventArgs args)
		{
			DocumentLoading(Operation, args);
		}
		
		private static void Loaded(AsyncOperation operation, Document document)
		{
			Post(operation, (a) => { TriggerOnDocumentLoaded(a); }, new DocumentLoadedEventArgs(document));
		}

		private static void FileNotFound(AsyncOperation operation, FileNotFoundException ex, Func<string, bool> reference)
		{
			Post(operation, (a) => { TriggerOnFileNotFound(a); }, new FileNotFoundRetryEventArgs(ex, reference));
		}

		private static void DocumentLoading(AsyncOperation operation, ProgressStreamReportEventArgs args)
		{
			Post(operation, (a) => { TriggerOnDocumentLoadingProgress(a); }, args);
		}

		private static void FileImported(AsyncOperation operation, FileImportedEventArgs args)
		{
			Post(operation, (a) => { TriggerOnFileImported(a); }, args);
		}

		private static void Error(AsyncOperation operation, ErrorEventArgs args)
		{
			Post(operation, (a) => { TriggerOnError(a); }, args);
		}

		private static void Post<T>(AsyncOperation operation, Action<T> action, T arg)
		{
			operation.Post((p) => action((T)p), arg);
		}

		private static void TriggerOnFileNotFound(FileNotFoundRetryEventArgs args)
		{
			if (OnDocumentNotFound != null)
				OnDocumentNotFound.Invoke(null, args);
		}

		private static void TriggerOnDocumentLoaded(DocumentLoadedEventArgs args)
		{
			if (OnDocumentLoaded != null)
				OnDocumentLoaded.Invoke(null, args);
		}

		private static void TriggerOnDocumentLoadingProgress(ProgressStreamReportEventArgs args)
		{
			if (OnDocumentLoadingProgress != null)
				OnDocumentLoadingProgress.Invoke(null, args);
		}

		private static void TriggerOnFileImported(FileImportedEventArgs args)
		{
			if (OnFileImported != null)
				OnFileImported.Invoke(null, args);
		}

		private static void TriggerOnError(ErrorEventArgs args)
		{
			if (OnError != null)
				OnError.Invoke(null, args);
		}

		#endregion

		#region Parsing

		public void ReadTargetPageTypes()
        {
            XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(TargetFile);
            XmlNodeList pageTypes = TargetFile.SelectNodes("./exportDefinition[1]/contenttypes[1]/ArrayOfContentTypeTransferObject[1]/ContentTypeTransferObject", manager);

            var result = new List<XmlTarget>();

			foreach (XmlElement pageType in pageTypes)
            {
                var mapping = new XmlTarget((XmlElement)pageType);

                result.Add(mapping);
            }

            result = result.OrderBy(m => m.Name).ToList();

            Targets = result;
        }

        public void ReadSourcePageTypes()
        {
            XmlNamespaceManager manager = EPiDataHelper.GetNameSpaceManager(SourceFile);
            XmlNodeList pageTypes = SourceFile.SelectNodes("./export[1]/pagetypes[1]/ArrayOfPageType[1]/PageType");

            var result = new List<XmlTargetMapping>();

            foreach (XmlElement pageType in pageTypes)
            {
                var mapping = new XmlTargetMapping(pageType);
                result.Add(mapping);
            }

            result = result.OrderBy(m => m.Source.Name).ToList();

            SourceMappings = result;
        }

		#endregion

		#region Saving

		private static void SaveInternal(Document document, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

			if (!string.IsNullOrEmpty(document.SourceFilePath))
				document.SourceFilePath = PathHelper.ConvertPath(document.DocumentPath, path, document.SourceFilePath);

			if (!string.IsNullOrEmpty(document.TargetFilePath))
				document.TargetFilePath = PathHelper.ConvertPath(document.DocumentPath, path, document.TargetFilePath);

            document.DocumentPath = path;

            using (var outputFile = new FileStream(path, FileMode.Create))
            using (var compressionStream = new ZipOutputStream(outputFile))
            {
				compressionStream.SetLevel(3);
				compressionStream.PutNextEntry(new ZipEntry("data.bin"));

                Serializer.Serialize(compressionStream, document);

				compressionStream.CloseEntry();
				compressionStream.IsStreamOwner = true;
				compressionStream.Close();
            }
        }

		#endregion

		#region Support methods

	

		#endregion
	}
}
