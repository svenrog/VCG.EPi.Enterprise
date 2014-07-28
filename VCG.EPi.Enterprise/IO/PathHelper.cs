using System;
using System.IO;

namespace VCG.EPi.Enterprise.IO
{
	public static class PathHelper
	{
		public static string ConvertPath(string oldBasePath, string newBasePath, string path)
		{
			if (string.IsNullOrEmpty(newBasePath)) return path;

			if (Path.IsPathRooted(path))
				return GetRelativePath(newBasePath, path);

			path = GetAbsolutePath(oldBasePath, path);
			return GetRelativePath(newBasePath, path);
		}

		public static string GetAbsolutePath(string basePath, string path)
		{
			if (string.IsNullOrEmpty(basePath)) return path;

			var root = new Uri(Path.GetDirectoryName(basePath));
			var absolute = new Uri(root, path);

			return UriToPath(absolute).Replace("file:\\\\\\", string.Empty);
		}

		public static string GetRelativePath(string basePath, string path)
		{
			if (string.IsNullOrEmpty(basePath)) return path;

			var folder = new Uri(Path.GetDirectoryName(basePath));
			var file = new Uri(path);
			var relative = folder.MakeRelativeUri(file);

			return UriToPath(relative);
		}

		public static string UriToPath(Uri uri)
		{
			string result = uri.ToString();
			result = result.Replace('/', Path.DirectorySeparatorChar);
			result = Uri.UnescapeDataString(result);

			return result;
		}
	}
}
