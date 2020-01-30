using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RoseGarden
{
	/// <summary>
	/// This class is essentially lifted from Bloom with some simplifications.
	/// At this point, copying the code is much simpler than trying to work up a shared library.
	/// </summary>
	public class BookMetaData
	{
		public BookMetaData()
		{
			IsExperimental = false;
			AllowUploadingToBloomLibrary = true;
			BookletMakingIsAppropriate = true;
			IsSuitableForVernacularLibrary = true;
			Id = Guid.NewGuid().ToString();
		}
		public static BookMetaData FromString(string input)
		{
			var result = JsonConvert.DeserializeObject<BookMetaData>(input);
			if (result == null)
			{
				throw new ApplicationException("meta.json of this book may be corrupt");
			}
			return result;
		}

		/// <summary>
		/// Make a metadata, usually by just reading the meta.json file in the book folder.
		/// If some exception is thrown while trying to do that, or if it doesn't exist,
		/// Try reading a backup (and restore it if successful).
		/// If that also fails, return null.
		/// </summary>
		public static BookMetaData FromFolder(string bookFolderPath)
		{
			var metaDataPath = MetaDataPath(bookFolderPath);
			BookMetaData result;
			if (TryReadMetaData(metaDataPath, out result))
				return result;
			return null;
		}

		private static bool TryReadMetaData(string path, out BookMetaData result)
		{
			result = null;
			if (!File.Exists(path))
				return false;
			try
			{
				result = FromString(File.ReadAllText(path));
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("DEBUG: failed to read meta.json file - {0}", e.Message);
				return false;
			}
		}

		public static string MetaDataPath(string bookFolderPath)
		{
			return Path.Combine(bookFolderPath, "meta.json");
		}

		public void WriteToFolder(string bookFolderPath)
		{
			var metaDataPath = MetaDataPath(bookFolderPath);
			File.WriteAllText(metaDataPath, Json);
		}

		[JsonIgnore]
		public string Json
		{
			get
			{
				return JsonConvert.SerializeObject(this);
			}
		}

		[JsonProperty("bookInstanceId")]
		public string Id { get; set; }

		[JsonProperty("suitableForMakingShells")]
		public bool IsSuitableForMakingShells { get; set; }

		// Special property for Template Starter template.
		[JsonProperty("suitableForMakingTemplates")]
		public bool IsSuitableForMakingTemplates { get; set; }

		[JsonProperty("suitableForVernacularLibrary")]
		public bool IsSuitableForVernacularLibrary { get; set; }

		/// <summary>
		/// This version number is set when making a meta.json to embed in a bloomd file.
		/// We increment it whenever something changes that bloom-player or some other
		/// client might need to know about. It is NOT intended that the player would
		/// refuse to open a book with a higher number than it knows about; we may one day
		/// implement another mechanism for that. Rather, this is intended to allow a
		/// newer player which accommodates older books to know which of those accommodations
		/// are needed.
		/// See the one place where it is set for a history of the versions and what each
		/// indicates about the bloomd content.
		/// </summary>
		[JsonProperty("bloomdVersion")]
		public int BloomdVersion { get; set; }

		//SeeAlso: commented IsExperimental on Book
		[JsonProperty("experimental")]
		public bool IsExperimental { get; set; }

		[JsonProperty("brandingProjectName")]
		public String BrandingProjectName { get; set; }

		/// <summary>
		/// A "Folio" document is one that acts as a wrapper for a number of other books
		/// </summary>
		[JsonProperty("folio")]
		public bool IsFolio { get; set; }

		// A book is considerted RTL if its first content language is.
		[JsonProperty("isRtl")]
		public bool IsRtl { get; set; }

		// Enhance: multilingual?
		// BL-3774 was caused by a book with a meta.json value for Title of null.
		// So here let's just ensure we have store strings in that situation.
		private string _title = string.Empty;
		[JsonProperty("title")]
		public string Title
		{
			get { return _title; }
			set { _title = value == null ? string.Empty : value; }
		}

		[JsonProperty("allTitles")]
		public string AllTitles { get; set; }

		// This is filled in when we upload the json. It is not used locally, but becomes a field on parse.com
		// containing the actual url where we can grab the thumbnails, pdfs, etc.
		[JsonProperty("baseUrl")]
		public string BaseUrl { get; set; }

		// This is filled in when we upload the json. It is not used locally, but becomes a field on parse.com
		// containing the actual url where we can grab the book order file which when opened by Bloom causes it
		// to download the book.
		[JsonProperty("bookOrder")]
		public string BookOrder { get; set; }

		[JsonProperty("isbn")]
		public string Isbn { get; set; }

		[JsonProperty("bookLineage")]
		public string BookLineage { get; set; }

		// This tells Bloom where the data files can be found.
		// Strictly it is the first argument that needs to be passed to BookTransfer.DownloadBook in order to get the entire book data.
		[JsonProperty("downloadSource")]
		public string DownloadSource { get; set; }

		// This indicates the kind of license in use. For Creative Commons licenses, it is the Abbreviation of the CreativeCommonsLicense
		// object, the second-last (before version number) element of the licenseUrl. Other known values are 'ask' (no license granted,
		// ask the copyright holder for permission to use) 'custom' (rights presumably specified in licenseNotes)
		// Review: would it help with filtering if this field contained some indication of whether licenseNotes contains anything
		// (e.g., so we can search for CC licenses with no non-standard encumbrance)?
		[JsonProperty("license")]
		public string License { get; set; }

		[JsonProperty("formatVersion")]
		public string FormatVersion { get; set; }

		// When license is 'custom' this contains the license information. For other types in may contain additional permissions
		// (or possibly restrictions).
		// Review: do we need this, or just a field indicating whether there ARE additional notes, or just some modifier in license indicating that?
		[JsonProperty("licenseNotes")]
		public string LicenseNotes { get; set; }

		[JsonProperty("copyright")]
		public string Copyright { get; set; }

		[JsonProperty("credits")]
		public string Credits { get; set; }

		/// <summary>
		/// This is intended to be a list of strings, possibly from a restricted domain, indicating kinds of content
		/// the book contains. Currently it contains one member of the Topics list and possibly a bookshelf for the website.
		/// </summary>
		[JsonProperty("tags")]
		public string[] Tags { get; set; }

		[JsonProperty("pageCount")]
		public int PageCount { get; set; }

		// This is obsolete but loading old Json files fails if we don't have a setter for it.
		[JsonProperty("languages")]
		public string[] Languages { get { return new string[0]; } set { } }

		[JsonProperty("langPointers")]
		public ParseDotComObjectPointer[] LanguageTableReferences { get; set; }

		[JsonProperty("summary")]
		public string Summary { get; set; }

		// This is set to true in situations where the materials that are not permissively licensed and the creator doesn't want derivative works being uploaded.
		// Currently we don't need this property in Parse.com, so we don't upload it.
		[JsonProperty("allowUploadingToBloomLibrary", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(true)]
		public bool AllowUploadingToBloomLibrary { get; set; }

		[JsonProperty("bookletMakingIsAppropriate", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(true)]
		public bool BookletMakingIsAppropriate { get; set; }

		/// <summary>
		/// This is an item the user checks-off as part of claiming that the book is fully accessible
		/// </summary>
		[JsonProperty("a11y_NoEssentialInfoByColor")]
		public bool A11y_NoEssentialInfoByColor;

		/// <summary>
		/// This is an item the user checks-off as part of claiming that the book is fully accessible
		/// </summary>
		[JsonProperty("a11y_NoTextIncludedInAnyImages")]
		public bool A11y_NoTextIncludedInAnyImages;

		/// <summary>
		/// This corresponds to a checkbox indicating that the user wants to use the eReader's native font styles.
		/// </summary>
		[JsonProperty("epub_RemoveFontStyles")]
		public bool Epub_RemoveFontSizes;

		[JsonProperty("country")]
		public string CountryName { get; set; }

		[JsonProperty("province")]
		public string ProvinceName { get; set; }

		[JsonProperty("district")]
		public string DistrictName { get; set; }

		/// <summary>
		/// Normally, we get the xmatter from our collection. But this can be overridden here
		/// </summary>
		[JsonProperty("xmatterName")]
		public string XMatterNameOverride { get; set; }

		public void SetUploader(string id)
		{
			// The uploader is stored in a way that makes the json that parse.com requires for a 'pointer'
			// to an object in another table: in this case the special table of users.
			if (Uploader == null)
				Uploader = new ParseDotComObjectPointer() { ClassName = "_User" };
			Uploader.ObjectId = id;
		}

		/// <summary>
		/// The Parse.com ID of the person who uploaded the book.
		/// This is stored in a special way that parse.com requires for cross-table pointers.
		/// </summary>
		[JsonProperty("uploader")]
		public ParseDotComObjectPointer Uploader { get; set; }

		[JsonProperty("currentTool", NullValueHandling = NullValueHandling.Ignore)]
		public string CurrentTool { get; set; }

		[JsonProperty("toolboxIsOpen")]
		[DefaultValue(false)]
		public bool ToolboxIsOpen { get; set; }

		[JsonProperty("author")]
		public string Author { get; set; }

		// tags from Thema (https://www.editeur.org/151/Thema/)
		[JsonProperty("subjects")]
		public SubjectObject[] Subjects { get; set; }

		//https://www.w3.org/wiki/WebSchemas/Accessibility#Features_for_augmentation
		[JsonProperty("hazards")]
		public string Hazards { get; set; }

		//https://www.w3.org/wiki/WebSchemas/Accessibility#Features_for_augmentation
		[JsonProperty("a11yFeatures")]
		public string A11yFeatures { get; set; }

		//http://www.idpf.org/epub/a11y/accessibility.html#sec-conf-reporting
		[JsonProperty("a11yLevel")]
		public string A11yLevel { get; set; }

		//http://www.idpf.org/epub/a11y/accessibility.html#sec-conf-reporting
		[JsonProperty("a11yCertifier")]
		public string A11yCertifier { get; set; }

		// Global Digital Library: Indicates reading level
		// NB: this is just "level" in the Global Digital Library
		// e.g. "Pratham Level 1"
		[JsonProperty("readingLevelDescription")]
		public string ReadingLevelDescription { get; set; }

		// Global Digital Library: The typical range of ages the content’s intended end user.
		[JsonProperty("typicalAgeRange")]
		public string TypicalAgeRange { get; set; }

		[JsonProperty("features")]
		public string[] Features
		{
			get
			{
				var features = new List<string>(5);
				if (Feature_Blind) features.Add("blind");
				if (Feature_SignLanguage) features.Add("signLanguage");
				if (Feature_TalkingBook) features.Add("talkingBook");
				if (Feature_Motion) features.Add("motion");
				if (Feature_Quiz) features.Add("quiz");
				return features.ToArray();
			}
			set
			{
				Feature_Blind = value.Contains("blind");
				Feature_SignLanguage = value.Contains("signLanguage");
				Feature_TalkingBook = value.Contains("talkingBook");
				Feature_Motion = value.Contains("motion");
				Feature_Quiz = value.Contains("quiz");
			}
		}

		[JsonIgnore]
		public bool Feature_Blind { get; set; }
		[JsonIgnore]
		public bool Feature_SignLanguage { get; set; }
		[JsonIgnore]
		public bool Feature_TalkingBook { get; set; }
		[JsonIgnore]
		public bool Feature_Motion { get; set; }
		[JsonIgnore]
		public bool Feature_Quiz { get; set; }

		[JsonProperty("page-number-style")]
		public string PageNumberStyle { get; set; }

		[JsonProperty("language-display-names")]
		public Dictionary<string, string> DisplayNames { get; set; }

		// A json string used to limit what the user has access to (such as based on their location)
		// example:
		// {"downloadShell":{"countryCode":"PG"}}
		// which would mean only users in Papua New Guinea can download this book for use as a shell.
		// Currently, there is no UI for this. So, whatever the user enters in manually in meta.json gets passed to parse.
		[JsonProperty("internetLimits")]
		public dynamic InternetLimits { get; set; }

		/// <summary>
		/// Flag whether the user has used the original copyright and license for a derived/translated book.
		/// </summary>
		[JsonProperty("use-original-copyright")]
		public bool UseOriginalCopyright { get; set; }
	}

	/// <summary>
	/// Holds Code-Description pairs for Thema subjects.
	/// https://www.editeur.org/files/Thema/1.3/Thema_v1.3.0_en.json
	/// </summary>
	public class SubjectObject
	{
		[JsonProperty("value")]
		public string value { get; set; }

		[JsonProperty("label")]
		public string label { get; set; }
	}

	/// <summary>
	/// This is the required structure for a parse.com pointer to an object in another table.
	/// </summary>
	public class ParseDotComObjectPointer
	{
		public ParseDotComObjectPointer()
		{
			Type = "Pointer"; // Required for all parse.com pointers.
		}

		[JsonProperty("__type")]
		public string Type { get; set; }

		[JsonProperty("className")]
		public string ClassName { get; set; }

		[JsonProperty("objectId")]
		public string ObjectId { get; set; }
	}

	/// <summary>
	/// This class represents the parse.com Language class (for purposes of generating json)
	/// </summary>
	public class LanguageDescriptor
	{
		[JsonIgnore]
		public string Json
		{
			get
			{
				return JsonConvert.SerializeObject(this);
			}
		}

		[JsonProperty("isoCode")]
		public string IsoCode { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("ethnologueCode")]
		public string EthnologueCode { get; set; }
	}
}
