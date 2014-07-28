using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Xml;
using VCG.EPi.Enterprise.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace VCG.EPi.Enterprise.Migration.Toolset.Data
{
    public static class EPiServerClassGenerator
	{
        private static readonly string _templateIncludes = "using {0};\r\n";
		private static readonly string _templateAttribute = "[{0}{1}]";
		private static readonly string _templateAttributeDefaultMembers = "({0})";
		private static readonly string _templateAttributeMemberList = "{0} = {1}";
		private static readonly string _templateAttributeMemberListJoin = ", ";
        private static readonly string _templateIndent = "\t";
		private static readonly string _templateMemberFormat = "public virtual {0} {1} {{ get; set; }}";
		private static readonly string _pageTypeNameSelectionFormat = "./export[1]/pagetypes[1]/ArrayOfPageType[1]/PageType/ID[.='{0}'][1]/following-sibling::Name[1]";
		
		private static readonly Regex _filterBraces = new Regex("({)[^{]|(})[^}]", RegexOptions.Compiled);
		private static readonly Regex _filterQuotes = new Regex("\"", RegexOptions.Compiled);
        private static readonly Regex _filterEndsInNumber = new Regex("(\\d+)$", RegexOptions.Compiled);
		private static readonly Regex _filterAllowedClassChars = new Regex("[^a-zåäöü0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static readonly string[] _classCapitalizationSeparators = new string[] { "] ", "]", ".", " " };

		private static readonly List<string> _classDependencies = new List<string>()
		{
			"System",
			"EPiServer.Core",
			"EPiServer.Framework.DataAnnotations",
			"EPiServer.DataAnnotations",
			"EPiServer.DataAbstraction",
			"EPiServer.SpecializedProperties",
			"EPiServer.Web",
			"EPiServer"
		};

		private static readonly Dictionary<string, string> _dataTypeMappings = new Dictionary<string, string>()
        {
            { "EPiServer.SpecializedProperties.PropertyImageUrl", "Url|UIHint(UIHint.Image)" },
            { "EPiServer.SpecializedProperties.PropertyDocumentUrl", "Url|UIHint(UIHint.Document)" },
			{ "EPiServer.SpecializedProperties.PropertyUrl", "Url" },
            { "EPiServer.SpecializedProperties.PropertyXhtmlString", "XhtmlString" },
			{ "EPiServer.SpecializedProperties.PropertyLinkCollection", "LinkItemCollection" },
            { "EPiServer.Core.PropertyXForm", "EPiServer.XForms.XForm" },
            { "LongString", "string|UIHint(UIHint.Textarea)" },
            { "PageType", "PageType" },
            { "PageReference", "PageReference" },
			{ "Date", "DateTime?" },
            { "Number", "int?" },
            { "FloatNumber", "double?" },
            { "Decimal", "double?"},
            { "String", "string" },
            { "Boolean", "bool?" }
        };

		private static readonly Dictionary<string, string> _systemTabNames = new Dictionary<string, string>()
        {
            { "Categories", "Categories" },
            { "Information", "Content" },
            { "Scheduling", "Scheduling" },
            { "Advanced", "Settings" },
            { "Shortcut", "Shortcut" }
        };

		public static string ClassFromTargetMapping(XmlTargetMapping mapping, XmlDocument document, ClassGenerationOptions options, ILog log = null)
		{
			XmlNode node = mapping.Source.Data;
			
			StringBuilder builder = new StringBuilder();
			StringWriter baseWriter = new StringWriter(builder);
			IndentedTextWriter writer = new IndentedTextWriter(baseWriter, _templateIndent);

			// Includes
			var includes = new List<string>(_classDependencies);

			if (options.UseAttributeDisplay || options.UseAttributeRequired || options.UseAttributeUIHint || options.UseAttributeScaffoldColumn)
				includes.Add("System.ComponentModel.DataAnnotations");

			if (options.TargetVersion == EPiServerVersion.Version_7)
			{
				if (options.UseAttributeAvailablePageTypes)
					includes.Add("EPiServer.DataAbstraction.PageTypeAvailability");
			}

			IOrderedEnumerable<string> sortedIncludes = includes.OrderByDescending(i => i);
			ContentType type = mapping.ExportAsBlock ? ContentType.Block : ContentType.Page;

			writer.WriteLine(string.Join("", sortedIncludes.Select(i => string.Format(_templateIncludes, i))));
			
			writer.WriteLine(string.Format("namespace {0}.Models.{1}s", options.NameSpaceBase.Trim().TrimEnd('.'), type));
			writer.WriteLine("{");
			writer.Indent++;

			string guid = node.SelectSingleNode("./GUID").InnerText;
			string displayName = node.SelectSingleNode("./Name").InnerText;
			string description = node.SelectSingleNode("./Description").InnerText;
			string isAvailable = node.SelectSingleNode("./IsAvailable").InnerText;
			string order = node.SelectSingleNode("./SortOrder").InnerText;
            string contentTypeAvailabilityAttribute = options.TargetVersion > EPiServerVersion.Version_7 ? "AvailableContentTypes" : "AvailablePageTypes";

			if (options.UseAttributeContentType)
			{
                Dictionary<string, string> attributes = new Dictionary<string, string>() {};

                if (!Guid.Parse(guid).Equals(Guid.Empty))
                    attributes.Add("GUID", "\"{" + guid.ToUpper() + "}\"");

                if (!string.IsNullOrEmpty(displayName))
                    attributes.Add("DisplayName", "\"" + FilterUserInput(displayName) + "\"");

                if (!string.IsNullOrEmpty(description))
                    attributes.Add("Description", "\"" + FilterUserInput(description) + "\"");

                if (int.Parse(order) != 100)
                    attributes.Add("Order", order);

                if (!bool.Parse(isAvailable))
                    attributes.Add("AvailableInEditMode", isAvailable);

				WriteAttribute(ref writer, "ContentType", attributes);
			}

			if (options.UseAttributeAvailablePageTypes)
			{
				XmlNodeList allowedPageTypes = node.SelectNodes("./AllowedPageTypes/*");

				if (allowedPageTypes != null)
				{
					List<string> classes = new List<string>();

					foreach (XmlNode allowedPageType in allowedPageTypes)
					{
						string pageTypeClassName = GetPageTypeClassName(allowedPageType, document);

						if (!string.IsNullOrEmpty(pageTypeClassName))
							classes.Add(pageTypeClassName);
					}

					if (classes.Count > 0)
					{
						WriteAttribute(ref writer, contentTypeAvailabilityAttribute, new Dictionary<string, string>() 
						{ 
							{ "Availability.Specific", null },
							{ "Include", "new[] { " + string.Join(", ", classes.Select(c => string.Format("typeof({0})", c))) + " }" }
						});
					}
				}
			}

			if (options.UseAttributeAccess)
			{
				XmlNodeList accessPermissions = node.SelectNodes("./ACL/entries/entry");

				Dictionary<string, List<string>> accessDictionary = new Dictionary<string, List<string>>()
				{
					{ "User", new List<string>() },
					{ "Role", new List<string>() },
					{ "VisitorGroup", new List<string>() }
				};

				if (accessPermissions != null)
				{
					foreach (XmlNode accessPermission in accessPermissions)
					{
						string mode = accessPermission.Attributes["access"].Value;
						string entityType = accessPermission.Attributes["entityType"].Value;
						string entityName = accessPermission.Attributes["name"].Value;

						if (mode == "Create" && entityType == "Role" && entityName == "Everyone")
						{
							accessDictionary["User"] =
							accessDictionary["Role"] =
							accessDictionary["VisitorGroup"] = new List<string>();
							break;
						}
						else if (mode == "Create")
						{
							accessDictionary[entityType].Add(entityName);
						}
					}
				}

				if (accessDictionary.Any(p => p.Value != null && p.Value.Count > 0))
				{
					Dictionary<string, string> attributes = new Dictionary<string, string>();

					if (accessDictionary["User"].Count > 0)
						attributes.Add("Users", "\"" + string.Join(",", accessDictionary["User"]) + "\"");

					if (accessDictionary["Role"].Count > 0)
						attributes.Add("Roles", "\"" + string.Join(",", accessDictionary["Role"]) + "\"");

					if (accessDictionary["VisitorGroup"].Count > 0)
						attributes.Add("VisitorGroups", "\"" + string.Join(",", accessDictionary["VisitorGroup"]) + "\"");

					WriteAttribute(ref writer, "Access", attributes);
				}
			}

            writer.WriteLine(string.Format("public class {0} : {1}Data", ClassNameFromTargetMapping(mapping), type));
			writer.WriteLine("{");
			writer.Indent++;
			writer.WriteLine(string.Empty);

			IEnumerable<PropertyDefinition> properties = mapping.SourceProperties;

			if (properties != null)
			{
				foreach (PropertyDefinition property in properties)
				{
					string propertyName = property.Alias ?? property.Name;

                    if (propertyName.IndexOf("-") > -1)
                    {
                        propertyName = propertyName.Replace("-", "_");
                        log.Log(string.Format("Found occurrence of illegal character '-' in property '{0}'", propertyName), MessageType.Warning);
                    }

                    string propertyType = property.Type.TypeName ?? property.Type.DataType;
					bool isCustomProperty = !_dataTypeMappings.ContainsKey(propertyType);
					string propertyDataType = isCustomProperty ? propertyType : _dataTypeMappings[propertyType];
                    string className = ClassNameFromTargetMapping(mapping);

					if (isCustomProperty)
					{
                        Log(log, string.Format("{0} - {1}: Incompatible property type {2}, commented out.", className, propertyName, propertyType), MessageType.Warning);
						writer.WriteLine(string.Format("// Todo: Convert: {0} - {1} to EPiServer 7 custom property", className, propertyName));
						writer.WriteLine("/*");
					}

					if (options.UseAttributeDisplay)
					{
                        Dictionary<string, string> attributes = new Dictionary<string, string>() { };

						if (!string.IsNullOrEmpty(property.Tab.Name))
                            attributes.Add("GroupName", GetGroupName(property.Tab.Name));

						if (!string.IsNullOrEmpty(property.EditCaption))
                            attributes.Add("Name", "\"" + FilterUserInput(property.EditCaption) + "\"");

						if (!string.IsNullOrEmpty(property.HelpText))
                            attributes.Add("Description", "\"" + FilterUserInput(property.HelpText) + "\"");

                        attributes.Add("Order", property.FieldOrder.ToString());

						WriteAttribute(ref writer, "Display", attributes);
					}

					if (options.UseAttributeCultureSpecific)
					{
						if (property.LanguageSpecific)
                            WriteAttribute(ref writer, "CultureSpecific");
					}

                    if (options.UseAttributeRequired)
                    {
                        if (property.Required)
                            WriteAttribute(ref writer, "Required");
                    }

                    if (options.UseAttributeSearcheable)
                    {                        
                        if (property.Searchable)
                            WriteAttribute(ref writer, "Searchable");
                    }

                    if (options.UseAttributeScaffoldColumn)
                    {                        
                        if (!property.DisplayEditUI)
                            WriteAttribute(ref writer, "ScaffoldColumn(false)");
                    }

					if (propertyDataType.Contains('|'))
					{
						int index = propertyDataType.IndexOf('|');
						string hint = propertyDataType.Substring(index + 1);
						propertyDataType = propertyDataType.Substring(0, index);

						if (options.UseAttributeUIHint)
							WriteAttribute(ref writer, hint);
					}

					writer.WriteLine(string.Format(_templateMemberFormat, propertyDataType, propertyName));

					if (isCustomProperty)
						writer.WriteLine("*/");

					writer.WriteLine(string.Empty);
				}
			}

			writer.Indent--;
			writer.WriteLine("}");
			writer.Indent--;
			writer.WriteLine("}");

			return builder.ToString();
		}

		private static string GetGroupName(string groupName)
		{
			if (_systemTabNames.ContainsKey(groupName))
				return "SystemTabNames." + _systemTabNames[groupName];

			return "\"" + groupName + "\"";
		}

		private static void WriteAttribute(ref IndentedTextWriter writer, string name, Dictionary<string, string> properties = null)
		{
			if (properties == null || properties.Keys.Count == 0)
			{
				writer.WriteLine(string.Format(_templateAttribute, name, string.Empty));
				return;
			}

			string attributeMembers = string.Join(_templateAttributeMemberListJoin,
												  properties.Select(p => string.IsNullOrEmpty(p.Value) ? p.Key : string.Format(_templateAttributeMemberList, p.Key, p.Value)));

			writer.WriteLine(string.Format(_templateAttribute, name,
							 string.Format(_templateAttributeDefaultMembers, attributeMembers)));
		}

		public static string ClassNameFromTargetMapping(XmlTargetMapping mapping)
		{
			string name = mapping.Source.Name;

			return ClassNameFromPageTypeName(name, mapping.ExportAsBlock ? "Block" : "Page");
		}

		private static string GetPageTypeClassName(XmlNode pageTypeId, XmlDocument document, string suffix = "Page")
		{
			XmlNode pageTypeNameNode = document.SelectSingleNode(string.Format(_pageTypeNameSelectionFormat, pageTypeId.InnerText));

			if (pageTypeNameNode == null) return null;

			string pageTypeName = pageTypeNameNode.InnerText;

            return ClassNameFromPageTypeName(pageTypeName, suffix);
		}

        public static string ClassNameFromPageTypeName(string pageTypeName, string suffix = "Page")
        {
            if (pageTypeName == null) return null;

            string name = string.Empty;

            string[] data = pageTypeName.Split(_classCapitalizationSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in data)
            {
                string cleanentry = _filterAllowedClassChars.Replace(entry, string.Empty);

                if (cleanentry.Length > 1)
                    name += cleanentry.Substring(0, 1).ToUpper() + cleanentry.Substring(1).ToLower();
                else
                    name += cleanentry;
            }

            if (name.Equals("page", StringComparison.OrdinalIgnoreCase))
                name = "Standard";

            if (name.EndsWith("page", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            if (name.IndexOf(suffix, StringComparison.OrdinalIgnoreCase) < 0)
                name = name + suffix;

            return name;
        }
        public static string FilterUserInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = _filterBraces.Replace(input, "$1$1");
            input = _filterQuotes.Replace(input, "\\\"");

            return input;
        }

        public static void Log(ILog log, string message, MessageType type = MessageType.Message)
        {
            if (log == null) return;
            else
                log.Log(message, type);
        }
	}
}
