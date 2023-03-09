// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommandLine;
using RoseGarden.Parse;
using RoseGarden.Parse.Model;
using SIL.Windows.Forms.ClearShare;
using SIL.Xml;

namespace RoseGarden
{
	public class ConvertFromEpub
	{
		internal const int kMaxTextLengthForLandscape = 300;		// number is rather arbitrary...

		internal ConvertOptions _options;

		internal EpubMetadata _epubMetaData;
		internal string _epubFolder;
		internal string _epubFile;
		internal string _bookFolder;
		internal string _htmFileName;
		internal XmlDocument _bloomDoc;
		internal XmlDocument _templateBook;  // provides templates for new front cover and content pages.
		internal List<XmlElement> _templatePages;
		internal XmlDocument _opdsEntry;     // from catalog entry file (if it exists)
		internal XmlNamespaceManager _opdsNsmgr;
		internal readonly LanguageData _languageData = new LanguageData();
		internal BookMetaData _bookMetaData;
		internal string _publisher;
		internal string _creditsLang = "en";    // default assumption until proven otherwise (and true more often than for any other value)
		// Special case for publisher 3Asafeer since it doesn't do end credit pages.
		string _asafeerCopyright;
		string _asafeerLicense;

		private bool _coverImageOnlyInEpub;

		private int _endCreditsStart = Int32.MaxValue;  // Assume no end credits pages to begin with.
		private int _endCreditsPageCount = 0;
		private string _attributionFile;

		const string kStoryAttribution = "Story Attribution:";
		const string kOtherCredits = "Other Credits:";
		const string kIllustrationAttribs = "Illustration Attributions:";
		const string kImagesAttribs = "Images Attributions:";   // alternative to kIllustrationAttribs
		const string kDisclaimer = "Disclaimer:";
		const string kMatchPageString = "(Cover [Pp]age:|Page [0-9]+:)";
		const string kMatchPageNumber = "Page ([0-9]+):";
		const string kMatchRawPrathamIllustrationCredit = @"([^©]*)(©.*[12][09][0-9][0-9]\.?).*Released under[ a]* (CC[\sA-Z0-9.-]+) license";

		const string kFrenchStoryAttribution = "Attribution de l’histoire\u00a0:";
		const string kFrenchOtherCredits = "Autres crédits\u00a0:";
		const string kFrenchIllustrationAttribs = "Attributions de l’illustration\u00a0:";
		const string kFrenchDisclaimer = "Déni de responsabilité\u00a0:";
		const string kFrenchMatchPageString = @"(Page de couverture\s*:|Page\s[0-9]+\s*:)";
		const string kFrenchMatchPageNumber = @"Page\s([0-9]+)\s*:";
		const string kFrenchMatchRawPrathamIllustrationCredit = @"([^©]*)(©.*[12][09][0-9][0-9]\.?).*Publié sous licence\s(CC\s[\sA-Z0-9.-]+)\.";


		internal StringBuilder _contributionsXmlBldr = new StringBuilder();
		internal StringBuilder _supportedByXmlBldr = new StringBuilder();
		Dictionary<string, List<int>> _creditsAndPages;
		Dictionary<string, Book> _bloomlibraryBooks;

		// Files that are copied into a new Basic Book.
		readonly private string[] _copiedFiles = new string[]
		{
			"browser/bookLayout/basePage.css",
			"browser/bookLayout/langVisibility.css",
			"browser/collectionsTab/collectionsTabBookPane/previewMode.css",
			"browser/bookEdit/css/origami.css",
			"browser/branding/Default/branding.css",
			"browser/branding/Default/BloomWithTaglineAgainstLight.svg",
			"browser/templates/template books/Basic Book/Basic Book.css",
			"browser/templates/template books/Basic Book/license.png",
			"browser/templates/template books/Basic Book/meta.json",
			"browser/templates/template books/Basic Book/placeHolder.png",
			"browser/templates/template books/Basic Book/thumbnail.png",
			"browser/templates/xMatter/Traditional-XMatter/Traditional-XMatter.css",
		};

		public ConvertFromEpub(ConvertOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
		}

		public int RunConvert()
		{
			if (!VerifyOptions())
				return 1;
			try
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: converting \"{0}\"", Path.GetFileName(_options.EpubFile));
				InitializeData();

				ConvertBook();
				CreateThumbnails();

				File.Delete(Path.Combine(_bookFolder, _htmFileName));
				_bloomDoc.Save(Path.Combine(_bookFolder, _htmFileName));
				if (!String.IsNullOrWhiteSpace(_options.CollectionFolder) && _options.CollectionFolder != _bookFolder)
					CopyBloomBookToOutputFolder();
				if (NeedCopyrightInformation())
					Console.WriteLine("WARNING: could not find copyright information for {0}", _bookMetaData.Title);
				if (NeedLicenseInformation())
					Console.WriteLine("WARNING: could not find license information for {0}", _bookMetaData.Title);
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR: caught exception: {0}", e.Message);
				if (_options.Verbose)
					Console.WriteLine(e.StackTrace);
				return 2;
			}
			return 0;
		}

		internal void ChangePagesToLandscape()
		{
			foreach (var page in _templatePages)
			{
				var classes = page.GetAttribute("class");
				var newClasses = classes.Replace("A5Portrait", "A5Landscape");
				page.SetAttribute("class", newClasses);
			}
		}

		/// <summary>
		/// Remove the head element from the body of the HTML text.  It appears that the only elements created
		/// by pug that may not be valid XML are meta elements inside the head element.
		/// </summary>
		/// <remarks>
		/// We could use libtidy to convert the HTML to valid XML, but that's overkill at this point.
		/// </remarks>
		private static string RemoveHtmlHead(string htmlText)
		{
			var headStart = htmlText.IndexOf("<head>", StringComparison.InvariantCulture);
			var bodyStart = htmlText.IndexOf("<body>", StringComparison.InvariantCulture);
			htmlText = htmlText.Remove(headStart, bodyStart - headStart);
			return htmlText;
		}

		internal bool VerifyOptions()
		{
			var allValid = true;
			if (_options.UseLandscape && _options.UsePortrait)
			{
				Console.WriteLine("--portrait and --landscape cannot be used together.  --landscape is the default for books with pictures on most pages and limited text.  --portrait is the default otherwise.");
				allValid = false;
			}
			if (String.IsNullOrWhiteSpace(_options.UploadUser) || String.IsNullOrWhiteSpace(_options.UploadPassword))
			{
				if (String.IsNullOrWhiteSpace(_options.UploadUser))
					_options.UploadUser = Program.GetEnvironmentVariable("RoseGardenUserName");
				if (String.IsNullOrWhiteSpace(_options.UploadPassword))
					_options.UploadPassword = Program.GetEnvironmentVariable("RoseGardenUserPassword");
				if (String.IsNullOrWhiteSpace(_options.UploadUser) || String.IsNullOrWhiteSpace(_options.UploadPassword))
				{
					// lengthy warning message, but let the program proceed.
					Console.WriteLine("WARNING: without a user name (-U/--user) and password (-P/--password), RoseGarden cannot guarantee maintaining the same book instance id for books that have already been uploaded.  These values may be supplied by the RoseGardenUserName and RoseGardenUserPassword environment variables.");
				}
			}
			return allValid;
		}

		private void CopyBloomBookToOutputFolder()
		{
			string oldBookId = null;
			if (_bloomlibraryBooks != null)
			{
				var link = _opdsEntry.SelectSingleNode("/a:feed/a:entry/a:link[@type='application/epub+zip' and starts-with(@rel,'http://opds-spec.org/acquisition')]", _opdsNsmgr) as XmlElement;
				if (link != null)
				{
					var href = link.GetAttribute("href");
					if (!String.IsNullOrWhiteSpace(href))
					{
						Book book;
						if (_bloomlibraryBooks.TryGetValue(href.Trim(), out book))
						{
							oldBookId = book.BookInstanceId;
							if (_options.Verbose)
								Console.WriteLine("INFO: preserving book id {0} (from parse) for {1}", oldBookId, _bookMetaData.Title);
							_bookMetaData.Id = oldBookId;
						}
					}
				}
			}
			var folder = FixOutputBloomSourceFolderPath(_options.CollectionFolder, _publisher, _options.LanguageName);
			var newBookFolder = Path.Combine(folder, Path.GetFileNameWithoutExtension(_htmFileName));
			if (Directory.Exists(newBookFolder))
			{
				if (!_options.ReplaceExistingBook)
				{
					Console.WriteLine("WARNING: {0} already exists.", newBookFolder);
					Console.WriteLine("Use -R (--replace) if you want to overwrite it.");
					return;
				}
				if (oldBookId == null)
				{
					// Maintain the book id that was set before.
					var oldmeta = BookMetaData.FromFolder(newBookFolder);
					if (!String.IsNullOrWhiteSpace(oldmeta.Id) && oldmeta.Id != _bookMetaData.BookLineage)
					{
						if (_options.Verbose)
							Console.WriteLine("INFO: preserving book id {0} for {1}", oldmeta.Id, _bookMetaData.Title);
						_bookMetaData.Id = oldmeta.Id;
					}
				}
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: deleting directory {0}", newBookFolder);
				Directory.Delete(newBookFolder, true);
			}
			_bookMetaData.WriteToFolder(_bookFolder);
			CopyDirectory(_bookFolder, newBookFolder);
			EnsureBloomCollectionFile();
		}

		public static string FixOutputBloomSourceFolderPath(string folderPath, string publisherName, string languageName)
		{
			var folder = folderPath;
			if (folder.Contains("$publisher$"))
			{
				if (!String.IsNullOrWhiteSpace(publisherName))
					folder = folder.Replace("$publisher$", publisherName);
			}
			if (folder.Contains("$language$"))
			{
				if (!String.IsNullOrWhiteSpace(languageName))
					folder = folder.Replace("$language$", languageName);
			}
			return folder;
		}

		private void CopyDirectory(string sourceDir, string destDir)
		{
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: copying directory {0} to {1}", sourceDir, destDir);
			Directory.CreateDirectory(destDir);
			foreach (string file in Directory.EnumerateFiles(sourceDir))
			{
				var destFile = Path.Combine(destDir, Path.GetFileName(file));
				File.Copy(file, destFile);
			}
			foreach (string directory in Directory.EnumerateDirectories(sourceDir))
			{
				CopyDirectory(directory, Path.Combine(destDir, Path.GetFileName(directory)));
			}
		}

		private void EnsureBloomCollectionFile()
		{
			var collectionFile = Path.Combine(_options.CollectionFolder, Path.GetFileName(_options.CollectionFolder) + ".bloomCollection");
			if (File.Exists(collectionFile))
				return;
			var location = Assembly.GetExecutingAssembly().Location;
			var templateFile = Path.Combine(Path.GetDirectoryName(location), "Resources", "Blank.bloomCollection");
			var collectionText = File.ReadAllText(templateFile);
			collectionText = collectionText.Replace("<Language1Name>English</Language1Name>", $"<Language1Name>{_options.LanguageName}</Language1Name>");
			collectionText = collectionText.Replace("<Language1Iso639Code>en</Language1Iso639Code>", $"<Language1Iso639Code>{_epubMetaData.LanguageCode}</Language1Iso639Code>");
			//TODO default Font: collectionText.Replace("<DefaultLanguage1FontName>Andika New Basic</DefaultLanguage1FontName>", $"<DefaultLanguage1FontName>{font-name}</DefaultLanguage1FontName>");
			collectionText = collectionText.Replace("<IsLanguage1Rtl>false</IsLanguage1Rtl>", $"<IsLanguage1Rtl>{_options.IsRtl.ToString().ToLowerInvariant()}</IsLanguage1Rtl>");
			File.WriteAllText(collectionFile, collectionText);
			if (_options.Verbose)
				Console.WriteLine("INFO: created new {0}", Path.GetFileName(collectionFile));
		}

		private void InitializeData()
		{
			var workBase = Path.Combine(Path.GetTempPath(), "SIL", "RoseGarden");
			if (Directory.Exists(workBase))
				Directory.Delete(workBase, true);
			Directory.CreateDirectory(workBase);
			_epubFolder = Path.Combine(workBase, "EPUB");
			Directory.CreateDirectory(_epubFolder);
			if (_options.EpubFile.EndsWith(".epub.zip", StringComparison.InvariantCulture))
				_epubFile = UnzipZippedEpubFile();
			else
				_epubFile = _options.EpubFile;
			ExtractZippedFiles(_epubFile, _epubFolder);
			_epubMetaData = new EpubMetadata(_epubFolder, _options.VeryVerbose);
			string langCode = ForceGoodLanguageCode();
			if (String.IsNullOrWhiteSpace(_options.FileName))
				_htmFileName = Program.SanitizeNameForFileSystem(_epubMetaData.Title) + ".htm";
			else
				_htmFileName = _options.FileName + ".htm";

			_bookFolder = Path.Combine(workBase, "BLOOM");
			Directory.CreateDirectory(_bookFolder);
			foreach (var file in _copiedFiles)
			{
				var inputPath = Path.Combine(_options.BloomFolder, file);
				var outputPath = Path.Combine(_bookFolder, Path.GetFileName(file));
				File.Copy(inputPath, outputPath);
			}
			File.WriteAllText(Path.Combine(_bookFolder, "book.userPrefs"), "{\"mostRecentPage\":0,\"reducePdfMemory\":false}");
			var location = Assembly.GetExecutingAssembly().Location;
			var blankHtmPath = Path.Combine(Path.GetDirectoryName(location), "Resources", "Book.htm");
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: copying blank html file from {0}", blankHtmPath);
			File.Copy(blankHtmPath, Path.Combine(_bookFolder, _htmFileName));
			// defaultLangStyles.css and customCollectionStyles.css remain to be created
			_bookMetaData = BookMetaData.FromFolder(_bookFolder);
			_bookMetaData.BookLineage = _bookMetaData.Id;
			_bookMetaData.Id = Guid.NewGuid().ToString();   // This may be replaced if we're updating an existing book.

			DoubleCheckLanguageInformation(langCode);

			_bloomDoc = new XmlDocument();
			_bloomDoc.PreserveWhitespace = true;
			_bloomDoc.Load(Path.Combine(_bookFolder, _htmFileName));

			_templateBook = new XmlDocument();
			_templateBook.PreserveWhitespace = true;
			var pagesFile = Path.Combine(Path.GetDirectoryName(location), "Resources", "Pages.xml");
			_templateBook.Load(pagesFile);
			_templatePages = _templateBook.SelectNodes("//div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			if (_options.UseLandscape)
				ChangePagesToLandscape();
		}

		internal string ForceGoodLanguageCode()
		{
			var langCode = _languageData.GetCodeForName(_options.LanguageName, _options.VeryVerbose);
			if (_epubMetaData.LanguageCode != langCode)
			{
				if (_epubMetaData.LanguageCode == "en")
				{
					Console.WriteLine("WARNING: using '{0}' for {1} instead of 'en'", langCode, _options.LanguageName);
					_epubMetaData.LanguageCode = langCode;
				}
				else if (_epubMetaData.LanguageCode.ToLowerInvariant() == langCode.ToLowerInvariant())
				{
					Console.WriteLine("INFO: replacing language code '{0}' with '{1}'", _epubMetaData.LanguageCode, langCode);
					_epubMetaData.LanguageCode = langCode;
				}
				else if (_epubMetaData.LanguageCode == "bxk" && langCode == "luy")
				{
					Console.WriteLine("INFO: replacing obsolete language code '{0}' with '{1}'", _epubMetaData.LanguageCode, langCode);
					_epubMetaData.LanguageCode = langCode;
				}
				else if (_epubMetaData.LanguageCode.StartsWith(langCode + "-", StringComparison.InvariantCulture))
				{
					Console.WriteLine("INFO: replacing language code '{0}' with '{1}'", _epubMetaData.LanguageCode, langCode);
					_epubMetaData.LanguageCode = langCode;
				}
				else
				{
					Console.WriteLine("WARNING: language code '{0}' for {1} does not match expected '{2}'.", _epubMetaData.LanguageCode, _options.LanguageName, langCode);
				}
			}

			return langCode;
		}

		/// <summary>
		/// If the collection settings file exists, ensure that our language information matches with it.
		/// </summary>
		private void DoubleCheckLanguageInformation(string langCode)
		{
			var collectionFile = Path.Combine(_options.CollectionFolder, Path.GetFileName(_options.CollectionFolder) + ".bloomCollection");
			if (File.Exists(collectionFile))
			{
				// Check that the existing collection settings file matches with our data.
				var collectionSettings = new XmlDocument();
				collectionSettings.PreserveWhitespace = true;
				collectionSettings.Load(collectionFile);
				var langCodeCollection = collectionSettings.SelectSingleNode("/Collection/Language1Iso639Code")?.InnerText;
				if (langCodeCollection != langCode)
				{
					Console.WriteLine("ERROR: language code '{0}' does not match target collection language code '{1}'!", langCode, langCodeCollection);
					Environment.Exit(1);
				}
				var langNameCollection = collectionSettings.SelectSingleNode("/Collection/Language1Name")?.InnerText;
				if (langNameCollection != _options.LanguageName)
					Console.WriteLine("WARNING: language name '{0}' does not match target collection language name '{1}'", _options.LanguageName, langNameCollection);
				var rtlCollection = collectionSettings.SelectSingleNode("/Collection/IsLanguage1Rtl")?.InnerText;
				if (_options.IsRtl && rtlCollection.ToLowerInvariant() == "false")
				{
					Console.WriteLine("ERROR: collection settings indicate that '{0}' ('{1}') is not Right-to-Left.  Adjust collection settings (or command line) and try again.",
						langNameCollection, langCodeCollection);
					Environment.Exit(1);
				}
				else if (!_options.IsRtl && rtlCollection.ToLowerInvariant() == "true")
				{
					Console.WriteLine("WARNING: collection settings indicate that '{0}' ('{1}') is Right-to-Left.  RoseGarden is using the collection setting.",
						langNameCollection, langCodeCollection);
					_options.IsRtl = true;
				}
			}
		}

		private string UnzipZippedEpubFile()
		{
			if (_options.Verbose)
				Console.WriteLine("INFO: unzipping {0} to obtain actual epub file", _options.EpubFile);
			var unzipFolder = Path.Combine(Path.GetTempPath(), "RoseGarden", "EpubZip");
			if (Directory.Exists(unzipFolder))
				Directory.Delete(unzipFolder, true);
			ExtractZippedFiles(_options.EpubFile, unzipFolder);
			string epubName = null;
			foreach (var filepath in Directory.EnumerateFiles(unzipFolder))
			{
				if (filepath.EndsWith(".epub", StringComparison.InvariantCulture))
					epubName = filepath;
				else if (filepath.EndsWith(".txt", StringComparison.InvariantCulture) && filepath.Contains("StoryWeaverAttribution"))
					_attributionFile = filepath;
			}
			if (String.IsNullOrWhiteSpace(epubName))
				Console.Write("WARNING: could not find unzipped epub file from {0}!", _options.EpubFile);
			return epubName;
		}

		private string UnzipZippedPdfFile(string pdfZipFile)
		{
			if (_options.Verbose)
				Console.WriteLine("INFO: unzipping {0} to obtain actual pdf file", pdfZipFile);
			var unzipFolder = Path.Combine(Path.GetTempPath(), "RoseGarden", "PdfZip");
			if (Directory.Exists(unzipFolder))
				Directory.Delete(unzipFolder, true);
			ExtractZippedFiles(pdfZipFile, unzipFolder);
			foreach (var filepath in Directory.EnumerateFiles(unzipFolder))
			{
				if (filepath.EndsWith(".pdf", StringComparison.InvariantCulture))
					return filepath;
			}
			Console.Write("WARNING: could not find unzipped pdf file from {0}!", pdfZipFile);
			return null;
		}

		private void ConvertBook()
		{
			// Copy all the files needed for the book.
			foreach (var imageFile in _epubMetaData.ImageFiles)
			{
				var destPath = Path.Combine(_bookFolder, Path.GetFileName(imageFile));
				if (File.Exists(destPath))
					Console.WriteLine("WARNING: Overwriting {0}: image may be used more than once, or may be different!", destPath);
				File.Copy(imageFile, destPath, true);
			}
			//if (_epubMetaData.AudioFiles.Count > 0)
			//{
			//	var destFolder = Path.Combine(_bookFolder, "audio");
			//	Directory.CreateDirectory(destFolder);
			//	foreach (var audioFile in _epubMetaData.AudioFiles)
			//	{
			//		var destPath = Path.Combine(destFolder, Path.GetFileName(audioFile));
			//		if (File.Exists(destPath))
			//			Console.WriteLine("WARNING: Overwriting {0}: audio file may be used more than once, or may be different!", destPath);
			//		File.Copy(audioFile, destPath, true);
			//	}
			//}
			if (_epubMetaData.VideoFiles.Count > 0)
			{
				var destFolder = Path.Combine(_bookFolder, "video");
				Directory.CreateDirectory(destFolder);
				foreach (var videoFile in _epubMetaData.VideoFiles)
				{
					var destPath = Path.Combine(destFolder, Path.GetFileName(videoFile));
					if (File.Exists(destPath))
						Console.WriteLine("WARNING: Overwriting {0}: video file may be used more than once, or may be different!", destPath);
					File.Copy(videoFile, destPath, true);
				}
			}
			// Find related files that may have been downloaded or created for this book.
			var pathPDF = Path.ChangeExtension(_options.EpubFile, "pdf");
			var pathThumb = Path.ChangeExtension(_options.EpubFile, "thumb.jpg");
			var pathOPDS = Path.ChangeExtension(_options.EpubFile, "opds");
			if (_options.EpubFile.EndsWith(".epub.zip", StringComparison.InvariantCulture))
			{
				var pathPDFZip = _options.EpubFile.Replace(".epub.zip", ".pdf.zip");
				if (File.Exists(pathPDFZip))
					pathPDF = UnzipZippedPdfFile(pathPDFZip);
				pathThumb = _options.EpubFile.Replace(".epub.zip", "jpg");
				pathOPDS = _options.EpubFile.Replace(".epub.zip", "opds");
			}
			if (File.Exists(pathPDF))
				File.Copy(pathPDF, Path.Combine(_bookFolder, Path.ChangeExtension(_htmFileName, "pdf")));
			if (File.Exists(pathThumb))
			{
				File.Delete(Path.Combine(_bookFolder, "thumbnail.png"));    // blank image copied from Basic Book
				File.Copy(pathThumb, Path.Combine(_bookFolder, "thumbnail.jpg"));
			}
			else
			{
				pathThumb = Path.ChangeExtension(pathThumb, "png");
				if (File.Exists(pathThumb))
				{
					File.Delete(Path.Combine(_bookFolder, "thumbnail.png"));    // blank image copied from Basic Book
					File.Copy(pathThumb, Path.Combine(_bookFolder, "thumbnail.png"));
				}
			}
			if (File.Exists(pathOPDS))
			{
				File.Copy(pathOPDS, Path.Combine(_bookFolder, Path.GetFileName(pathOPDS).Replace(".opds", ".original.opds")));
				var opdsXml = File.ReadAllText(pathOPDS);
				// load the OPDS catalog information
				LoadOpdsDataAndSetPublisher(opdsXml);

				if (!String.IsNullOrWhiteSpace(_options.UploadUser) && !String.IsNullOrWhiteSpace(_options.UploadPassword))
					_bloomlibraryBooks = ParseClient.LoadBloomLibraryInfo(_options.UploadUser, _options.UploadPassword);
			}
			else
			{
				Console.WriteLine("WARNING: could not load OPDS file {0}: the import may lack important information.", pathOPDS);
				_publisher = _epubMetaData.Publisher;
				Console.WriteLine("INFO: Using publisher from ePUB metadata: \"{0}\"", _publisher);
			}

			SetHeadMetaAndBookLanguage();

			for (int pageNumber = 0; pageNumber < _epubMetaData.PageFiles.Count; ++pageNumber)
			{
				var pageFile = _epubMetaData.PageFiles[pageNumber];
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: converting {0}", pageFile);
				var pageXhtml = File.ReadAllText(pageFile);
				if (!ConvertPage(pageNumber, pageXhtml, Path.GetFileName(pageFile)))
					Console.WriteLine("WARNING: {0} did not convert successfully.", pageFile);
			}
			if (_publisher == "3Asafeer")
				SetAsafeerImageCredits();
			if ((_endCreditsPageCount > 3 && _endCreditsPageCount > _epubMetaData.PageFiles.Count / 7) || _endCreditsPageCount == 0)
				Console.WriteLine("WARNING: found {0} end credit pages in the book", _endCreditsPageCount);

			if (!String.IsNullOrWhiteSpace(_attributionFile) && File.Exists(_attributionFile))
			{
				var attributionText = File.ReadAllText(_attributionFile);
				attributionText = ProcessAttributionFileText(attributionText);
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: processed {0} pages from {1} ({2} pages of end credits)", _epubMetaData.PageFiles.Count, Path.GetFileName(_options.EpubFile), _endCreditsPageCount);
			FillInBookMetaData();
		}

		private string ProcessAttributionFileText(string attributionText)
		{
			const string AttributionTextHeader = "Attribution Text:";
			var idxStart = attributionText.IndexOf(AttributionTextHeader, StringComparison.InvariantCulture);
			if (idxStart >= 0)
			{
				idxStart = idxStart + AttributionTextHeader.Length;
				var idxEnd = attributionText.IndexOf('\n', idxStart);
				if (idxEnd < 0)
					idxEnd = attributionText.Length;
				attributionText = attributionText.Substring(idxStart, idxEnd - idxStart).Trim();
			}
			SetDataDivParaValue("originalAcknowledgments", _epubMetaData.LanguageCode, attributionText);
			ExtractCopyrightAndLicenseFromAttributionText(attributionText);
			return attributionText;
		}

		internal void SetHeadMetaAndBookLanguage()
		{
			SetHeadMetaValue("ConvertedBy", String.Format("RoseGarden {0} ({1})", GetVersion(), GetTimeStamp()));
			if (_opdsEntry != null)
			{
				var titleNode = _opdsEntry.SelectSingleNode("/a:feed/a:title", _opdsNsmgr) as XmlElement;
				if (titleNode != null)
				{
					var title = Regex.Replace(titleNode.InnerText, " \\[extract\\]$", "");
					SetHeadMetaValue("OpdsSource", title);
				}
				AdjustLayoutIfNeeded();
			}

			SetDataDivTextValue("contentLanguage1", _epubMetaData.LanguageCode);
			SetDataDivTextValue("languagesOfBook", _options.LanguageName);
			SetDataDivTextValue("smallCoverCredits", "");
		}

		internal void LoadOpdsDataAndSetPublisher(string opdsXml)
		{
			_opdsEntry = new XmlDocument();
			_opdsEntry.PreserveWhitespace = true;
			_opdsEntry.LoadXml(opdsXml);
			_opdsNsmgr = OpdsClient.CreateNameSpaceManagerForOpdsDocument(_opdsEntry);

			var divPublisher = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dc:publisher", _opdsNsmgr) as XmlElement;
			if (divPublisher == null)
				divPublisher = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dcterms:publisher", _opdsNsmgr) as XmlElement;
			if (divPublisher != null)
			{
				_publisher = divPublisher.InnerText.Trim();
				if (_publisher == "ew")
					_publisher = "EW Nigeria";
				else if (_publisher == "Pratham books")
					_publisher = "Pratham Books";   // consistent capitalization!
				_bookMetaData.Publisher = _publisher;
			}
		}

		private void AdjustLayoutIfNeeded()
		{
			if (_options.UsePortrait || _options.UseLandscape)
				return;     // user specifically demanded a particular layout
			for (int pageNumber = 1; pageNumber < _epubMetaData.PageFiles.Count; ++pageNumber)
				ScanPageForMetrics(pageNumber);
			_endCreditsStart = Int32.MaxValue;  // reset credits page marker
			ProcessImageSizes();
			if (_options.Verbose)
			{
				Console.WriteLine("DEBUG: scanning the book shows {0} content pages with {1} image-only and {2} text-only pages",
					_contentPageCount, _imageOnlyPageCount, _textOnlyPageCount);
				Console.WriteLine("DEBUG: {0} pages have a landscape picture, {1} pages have a portrait picture",
					_pagesWithLandscapeImage, _pagesWithPortraitImage);
			}
			if (_options.VeryVerbose)
			{
				Console.WriteLine("DEBUG: the maximum character count on a page is {0}, or {1} if the page has an image",
					_maxTextLength, _maxTextLengthWithImage);
				if (_imageSizes.Count > 0)
				{
					Console.WriteLine("DEBUG: images have an average size of {0} with an average aspect ratio of {1}",
						_averageImageSize, _averageAspect);
					Console.WriteLine("DEBUG: image sizes max = {0}, min = {1}; image aspect ratio max = {2}, min = {3}",
						_biggestImageSize, _smallestImageSize, _biggestAspect, _smallestAspect);
				}
			}
			var singularPageCount = _textOnlyPageCount + _imageOnlyPageCount;
			// Picture Books (those with only pictures except maybe for 1 page of instruction) are landscape if the pictures are mostly landscape.
			// Other books with both pictures and text on 2/3 or more of the pares are landscape if the pictures are mostly portrait.
			if ((_imageOnlyPageCount >= (_contentPageCount - 1) && _pagesWithLandscapeImage > _pagesWithPortraitImage) ||
				(_contentPageCount >= (singularPageCount * 3) && _pagesWithPortraitImage > _pagesWithLandscapeImage))
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: setting book layout to landscape for {0}", _epubMetaData.Title);
				_options.UseLandscape = true;
				ChangePagesToLandscape();
			}
			if (_options.VeryVerbose)
			{
				var oldLandscapeLayout = false;
				if (_maxTextLengthWithImage <= kMaxTextLengthForLandscape &&
					(singularPageCount * 3) < _contentPageCount)    //text-only and image-only are < 1/3 of all total pages
				{
					oldLandscapeLayout = true;
				}
				if (oldLandscapeLayout && !_options.UseLandscape)
					Console.WriteLine("DEBUG: Character counting would cause landscape layout, but not image size metrics.");
				else if (!oldLandscapeLayout && _options.UseLandscape)
					Console.WriteLine("DEBUG: Character counting would cause portrait layout, but not image size metrics.");
			}
		}

		int _maxTextLengthWithImage = 0;
		int _maxTextLength = 0;
		int _imageOnlyPageCount = 0;
		int _textOnlyPageCount = 0;
		int _contentPageCount = 0;
		List<Size> _imageSizes = new List<Size>();

		private void ScanPageForMetrics(int pageNumber)
		{
			if (_publisher == "3Asafeer" && pageNumber == 1)
				return;		// actually a disclaimer/credits page, not a content page.
			var pageDoc = new XmlDocument();
			pageDoc.Load(_epubMetaData.PageFiles[pageNumber]);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr) as XmlElement;
			if (IsEndCreditsPage(body, pageNumber))
				return;		// ignore credits pages
			++_contentPageCount;
			var img = body.SelectSingleNode("//x:img", nsmgr) as XmlElement;
			if (img != null)
			{
				var imgSize = GetImageSize(img);
				_imageSizes.Add(imgSize);
			}
			var text = body.InnerText.Trim();
			text = Regex.Replace(body.InnerText.Trim(), "[\\s\n]+", " ");
			if (img != null && text.Length == 0)
				++_imageOnlyPageCount;
			if (img == null && text.Length > 0)
				++_textOnlyPageCount;
			if (img == null && text.Length == 0)
				Console.WriteLine("WARNING: page {0} has no content!?");
			if (_maxTextLength < text.Length)
				_maxTextLength = text.Length;
			if (img != null && _maxTextLengthWithImage < text.Length)
				_maxTextLengthWithImage = text.Length;
		}

		private Size GetImageSize(XmlElement img)
		{
			var src = Path.Combine(_bookFolder, img.GetAttribute("src"));
			using (var image = Image.FromFile(src))
			{
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: image {0} has size {1}", src, image.Size);
				return image.Size;
			}
		}

		SizeF _averageImageSize;
		Size _widestImageSize;
		Size _tallestImageSize;
		Size _biggestImageSize;
		Size _smallestImageSize;
		double _biggestAspect;
		double _smallestAspect;
		double _averageAspect;
		int _pagesWithLandscapeImage;
		int _pagesWithPortraitImage;

		private void ProcessImageSizes()
		{
			if (_imageSizes.Count == 0)
				return;
			_widestImageSize = _imageSizes[0];
			_tallestImageSize = _imageSizes[0];
			_biggestImageSize = _imageSizes[0];
			_smallestImageSize = _imageSizes[0];
			_biggestAspect = (double)(_imageSizes[0].Width) / (double)(_imageSizes[0].Height);
			_smallestAspect = (double)(_imageSizes[0].Width) / (double)(_imageSizes[0].Height);
			var totalWidth = 0;
			var totalHeight = 0;
			foreach (var size in _imageSizes)
			{
				if (_widestImageSize.Width < size.Width)
					_widestImageSize = size;
				if (_tallestImageSize.Height < size.Height)
					_tallestImageSize = size;
				if (_biggestImageSize.Width * _biggestImageSize.Height < size.Width * size.Height)
					_biggestImageSize = size;
				if (_smallestImageSize.Width * _smallestImageSize.Height > size.Width * size.Height)
					_smallestImageSize = size;
				totalWidth += size.Width;
				totalHeight += size.Height;
				double aspect = (double)size.Width / (double)size.Height;
				if (_biggestAspect < aspect)
					_biggestAspect = aspect;
				if (_smallestAspect > aspect)
					_smallestAspect = aspect;
				if (size.Width > size.Height)
					++_pagesWithLandscapeImage;
				else
					++_pagesWithPortraitImage;		// square image counts as portrait
			}
			_averageImageSize = new SizeF((float)totalWidth / (float)_imageSizes.Count, (float)totalHeight / (float)_imageSizes.Count);
			_averageAspect = (double)totalWidth / (double)totalHeight;
		}

		internal void ExtractCopyrightAndLicenseFromAttributionText(string attributionText)
		{
			var copyright = Regex.Match(attributionText, "\\((©.*, [12][09][0-9][0-9])\\)", RegexOptions.CultureInvariant);
			if (copyright.Success)
			{
				SetBookCopyright(copyright.Groups[1].Value, "en");
			}
			var license = Regex.Match(attributionText, "under a* *(CC.*) license", RegexOptions.CultureInvariant);
			if (license.Success)
			{
				SetBookLicense(license.Groups[1].Value);
			}
		}

		internal void SetBookLicense(string licenseAbbreviation)
		{
			var url = "";
			licenseAbbreviation = licenseAbbreviation.Replace("\u00a0", " ").Trim('.', ' ');
			switch (licenseAbbreviation)
			{
				case "CC BY":
				case "CC BY 4.0":		url = "http://creativecommons.org/licenses/by/4.0/";		break;
				case "CC BY-SA":
				case "CC BY-SA 4.0":	url = "http://creativecommons.org/licenses/by-sa/4.0/";		break;
				case "CC BY-ND":
				case "CC BY-ND 4.0":	url = "http://creativecommons.org/licenses/by-nd/4.0/";		break;
				case "CC BY-NC":
				case "CC BY-NC 4.0":	url = "http://creativecommons.org/licenses/by-nc/4.0/";		break;
				case "CC BY-NC-SA":
				case "CC BY-NC-SA 4.0":	url = "https://creativecommons.org/licenses/by-nc-sa/4.0/";	break;
				case "CC BY-NC-ND":
				case "CC BY-NC-ND 4.0":	url = "http://creativecommons.org/licenses/by-nc-nd/4.0/";	break;
				case "CC BY 3.0":		url = "http://creativecommons.org/licenses/by/3.0/";		break;
				case "CC BY-SA 3.0":	url = "http://creativecommons.org/licenses/by-sa/3.0/";		break;
				case "CC BY-ND 3.0":	url = "http://creativecommons.org/licenses/by-nd/3.0/";		break;
				case "CC BY-NC 3.0":	url = "http://creativecommons.org/licenses/by-nc/3.0/";		break;
				case "CC BY-NC-SA 3.0":	url = "https://creativecommons.org/licenses/by-nc-sa/3.0/";	break;
				case "CC BY-NC-ND 3.0":	url = "http://creativecommons.org/licenses/by-nc-nd/3.0/";	break;
				case "CC BY 2.5":		url = "http://creativecommons.org/licenses/by/2.5/";		break;
				case "CC BY-SA 2.5":	url = "http://creativecommons.org/licenses/by-sa/2.5/";		break;
				case "CC BY-ND 2.5":	url = "http://creativecommons.org/licenses/by-nd/2.5/";		break;
				case "CC BY-NC 2.5":	url = "http://creativecommons.org/licenses/by-nc/2.5/";		break;
				case "CC BY-NC-SA 2.5":	url = "https://creativecommons.org/licenses/by-nc-sa/2.5/";	break;
				case "CC BY-NC-ND 2.5":	url = "http://creativecommons.org/licenses/by-nc-nd/2.5/";	break;
				case "CC BY 2.0":		url = "http://creativecommons.org/licenses/by/2.0/";		break;
				case "CC BY-SA 2.0":	url = "http://creativecommons.org/licenses/by-sa/2.0/";		break;
				case "CC BY-ND 2.0":	url = "http://creativecommons.org/licenses/by-nd/2.0/";		break;
				case "CC BY-NC 2.0":	url = "http://creativecommons.org/licenses/by-nc/2.0/";		break;
				case "CC BY-NC-SA 2.0":	url = "https://creativecommons.org/licenses/by-nc-sa/2.0/";	break;
				case "CC BY-NC-ND 2.0":	url = "http://creativecommons.org/licenses/by-nc-nd/2.0/";	break;
				case "CC0":
					url = "https://creativecommons.org/share-your-work/public-domain/cc0/";
					break;
				default:
					Console.WriteLine("WARNING: cannot decipher license abbreviation \"{0}\"", licenseAbbreviation);
					break;
			}
			if (!String.IsNullOrEmpty(url))
			{
				SetDataDivTextValue("licenseUrl", url);
				_bookMetaData.License = licenseAbbreviation.ToLowerInvariant().Replace("cc by", "cc-by");
			}
		}

		private void SetBookCopyright(string matchedCopyright, string lang)
		{
			string preface;
			switch (lang)
			{
				case "en":
					preface = "Copyright ";
					break;
				default:
					preface = "";
					break;
			}
			var text = preface + matchedCopyright.Trim();
			_bookMetaData.Copyright = text;
			SetDataDivTextValue("copyright", text);
		}

		private void SetOriginalAcknowledgements(string originalAck, string lang)
		{
			// QUESTION: Do we need to append to the current value (if any), or just set it?
			// Do we need to set the classes I can see on existing books?
			SetDataDivParaValue("originalAcknowledgments", lang, originalAck);
		}

		private void FillInBookMetaData()
		{
			if (_epubMetaData.Authors.Count > 0)
				_bookMetaData.Author = String.Join(", ", _epubMetaData.Authors);
			if (String.IsNullOrEmpty(_bookMetaData.Title))
				_bookMetaData.Title = _epubMetaData.Title;

			_bookMetaData.BrandingProjectName = "Default";
			_bookMetaData.AllTitles = $"{{\"{_epubMetaData.LanguageCode}\":\"{_bookMetaData.Title}\"}}";
			_bookMetaData.FormatVersion = "2.1";
			_bookMetaData.Summary = _epubMetaData.Description;
			if (_bookMetaData.DisplayNames == null)
				_bookMetaData.DisplayNames = new Dictionary<string, string>();
			_bookMetaData.DisplayNames.Add(_epubMetaData.LanguageCode, _options.LanguageName);
			if (_opdsEntry != null)
			{
				var link0 = _opdsEntry.SelectSingleNode("/a:feed/a:entry/a:link[@type='application/epub+zip' and contains(@rel, 'http://opds-spec.org/acquisition')]", _opdsNsmgr) as XmlElement;
				if (link0 != null)
					_bookMetaData.ImportedBookSourceUrl = link0.GetAttribute("href");
			}
			// Something that the Basic Books meta.json doesn't get right for our purposes.
			// We're aren't making "shells" from this book, but rather more vernacular books.
			// (I think the concept of IsSuitableForMakingShells is really unclear and confusing.)
			_bookMetaData.IsSuitableForMakingShells = false;
		}

		private void SetHeadMetaValue(string name, string value)
		{
			var meta = _bloomDoc.SelectSingleNode($"/html/head/meta[@name='{name}']") as XmlElement;
			if (meta == null)
			{
				var head = _bloomDoc.SelectSingleNode("/html/head") as XmlElement;
				meta = _bloomDoc.CreateElement("meta");
				meta.SetAttribute("name", name);
				var indent = _bloomDoc.CreateWhitespace("  ");
				head.AppendChild(indent);
				head.AppendChild(meta);
				var nl = _bloomDoc.CreateWhitespace(Environment.NewLine + "  ");
				head.AppendChild(nl);
			}
			meta.SetAttribute("content", value);
		}

		private string GetTimeStamp()
		{
			return DateTime.Now.ToString("u");
		}

		private string GetVersion()
		{
			var asVersion = Assembly.GetExecutingAssembly().GetName().Version;
			return String.Format("{0}.{1}.{2}", asVersion.Major, asVersion.Minor, asVersion.Revision);
		}

		internal bool ConvertPage(int pageNumber, string pageXhtml, string pageFileName)
		{
			// pass in XHTML and internal access to facilitate unit testing
			if (pageNumber == 0)
			{
				return ConvertCoverPage(pageXhtml);
			}
			else if (pageNumber == 1 && _publisher == "3Asafeer")
			{
				return ConvertAsafeerCreditPage(pageNumber, pageXhtml);
			}
			else
			{
				return ConvertContentPage(pageNumber, pageXhtml, pageFileName);
			}
		}

		private void ConvertFrontCoverPage(string pageFilePath)
		{
			var coverPageXhtml = File.ReadAllText(pageFilePath);
			ConvertCoverPage(coverPageXhtml);
		}

		internal bool ConvertCoverPage(string coverPageXhtml)
		{
			// The cover page created here will be overwritten by Bloom when it applies the user's chosen xmatter.
			// The important result is filling in values in the data div.
			AddEmptyCoverPage();
			var pageDoc = new XmlDocument();
			pageDoc.LoadXml(coverPageXhtml);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr);
			bool titleSet = false;
			bool authorEtcSet = false;
			int imageCount = 0;
			var titleBldr = new StringBuilder();
			var children = body.ChildNodes.Cast<XmlNode>();
			ProcessCoverContent(ref titleSet, ref authorEtcSet, ref imageCount, titleBldr, children);
			if (imageCount == 1 && !titleSet && titleBldr.Length == 0 && !authorEtcSet)
				_coverImageOnlyInEpub = true;
			if (!titleSet && titleBldr.Length > 0)
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: title does not match epub metadata.  Title=\"{0}\"; epub metadata=\"{1}\"", titleBldr, _epubMetaData.Title);
				SetTitle(titleBldr.ToString());
			}
			else if (!titleSet)
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: Using title from ePUB metadata: \"{0}\"", _epubMetaData.Title);
				SetTitle(_epubMetaData.Title);
			}
			if (!authorEtcSet)
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: Using contributor information from ePUB metadata for the front cover.");
				// TODO: make & < and > safe for XML.
				// TODO: Localize "Author(s):", "Illustrator(s):", etc.
				var bldr = new StringBuilder();
				if (_epubMetaData.Authors.Count > 0)
				{
					bldr.Append("<p>");
					if (_epubMetaData.Authors.Count == 1)
						bldr.AppendFormat("Author: {0}", _epubMetaData.Authors[0]);
					else
						bldr.AppendFormat("Authors: {0}", String.Join(", ", _epubMetaData.Authors));
					bldr.Append("</p>");
				}
				if (_epubMetaData.Illustrators.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_epubMetaData.Illustrators.Count == 1)
						bldr.AppendFormat("Illustrator: {0}", _epubMetaData.Illustrators[0]);
					else
						bldr.AppendFormat("Illustrators: {0}", String.Join(", ", _epubMetaData.Illustrators));
					bldr.Append("</p>");
				}
				if (_epubMetaData.OtherCreators.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_epubMetaData.OtherCreators.Count == 1)
						bldr.AppendFormat("Creator: {0}", _epubMetaData.OtherCreators[0]);
					else
						bldr.AppendFormat("Creators: {0}", String.Join(", ", _epubMetaData.OtherCreators));
					bldr.Append("</p>");
				}
				if (_epubMetaData.OtherContributors.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_epubMetaData.OtherContributors.Count == 1)
						bldr.AppendFormat("Contributor: {0}", _epubMetaData.OtherContributors[0]);
					else
						bldr.AppendFormat("Contributors: {0}", String.Join(", ", _epubMetaData.OtherContributors));
					bldr.Append("</p>");
				}
				if (bldr.Length > 0)
				{
					AddCoverContributor(bldr.ToString());
				}
			}
			return true;
		}

		private void ProcessCoverContent(ref bool titleSet, ref bool authorEtcSet, ref int imageCount, StringBuilder titleBldr, IEnumerable<XmlNode> children)
		{
			var skippingCircularFLO = false;
			foreach (var child in children)
			{
				if (child.NodeType == XmlNodeType.Comment && child.OuterXml == "<!--CircularFLO Body-->")
				{
					skippingCircularFLO = true;
					continue;
				}
				if (child.NodeType == XmlNodeType.Comment && child.OuterXml == "<!--END CircularFLO Body-->")
				{
					skippingCircularFLO = false;
					continue;
				}
				if (skippingCircularFLO)
					continue;
				if (child.Name == "img")
				{
					++imageCount;
					var imageFile = (child as XmlElement).GetAttribute("src");
					imageFile = Path.GetFileName(imageFile);	// don't want leading image/ or images/
					// cover image always comes first
					if (imageCount == 1)
						SetCoverImage(imageFile);
					else
						AddExtraCoverImage(imageFile, imageCount);
				}
				else if (child.Name == "p" || child.Name == "h1" || child.Name == "h2" || child.Name == "h3" || child is XmlText)
				{
					var text = child.InnerText.Trim();
					if (String.IsNullOrEmpty(text))
						continue;
					if (Regex.Replace(text, @"\s", "").ToLowerInvariant() == "3asafeer.com")
						continue;   // no free advertising on cover...
					if (IsAuthorOrIllustrator(text) || titleSet)
					{
						authorEtcSet |= SetCoverContributor(child);
					}
					else
					{
						titleSet = SetTitle(child, titleBldr);
					}
				}
				else if (child.Name == "div")
				{
					ProcessCoverContent(ref titleSet, ref authorEtcSet, ref imageCount, titleBldr, child.ChildNodes.Cast<XmlNode>());
				}
				else if (child is XmlComment)
				{
					// Ignore comments: we don't expect them but they don't hurt anything either.
					continue;
				}
				else
				{
					Console.WriteLine("WARNING: UNEXPECTED ITEM IN THE FIRST (COVER) PAGE: {0} / \"{1}\"", child.NodeType.ToString(), child.OuterXml);
				}
			}
		}

		private bool IsAuthorOrIllustrator(string text)
		{
			var normText = Program.NormalizeWhitespace(text).ToLowerInvariant();
			return Regex.IsMatch(normText, "^authors? ?:", RegexOptions.CultureInvariant) ||
					Regex.IsMatch(normText, "^illustrators? ?:", RegexOptions.CultureInvariant) ||
					Regex.IsMatch(normText, "^translation ?:", RegexOptions.CultureInvariant) ||
					Regex.IsMatch(normText, "^published by ?:", RegexOptions.CultureInvariant) ||
					Regex.IsMatch(normText, "^قصة *[:/]", RegexOptions.CultureInvariant) ||		// author: ?
					Regex.IsMatch(normText, "^رسوم *[:/]", RegexOptions.CultureInvariant) ||	// illustrator: ?
					Regex.IsMatch(normText, "^تأليف *[:/]", RegexOptions.CultureInvariant) ||		// written by: ?
					Regex.IsMatch(normText, "^كلمات *[:/]", RegexOptions.CultureInvariant);		// words: ?
		}

		private bool SetTitle(XmlNode child, StringBuilder titleBldr)
		{
			var title = child.InnerText.Trim();
			var normTitle = Program.NormalizeToCompare(title);
			var normEpub = Program.NormalizeToCompare(_epubMetaData.Title);
			if (normTitle == normEpub)
			{
				title = Program.NormalizeWhitespace(title);
				SetTitle(title);
				titleBldr.Clear();		// just in case...
				return true;
			}
			var titleNorm = Program.NormalizeWhitespace(title);
			if (titleBldr.Length > 0 && !titleNorm.StartsWith("’re", StringComparison.InvariantCulture))
				titleBldr.Append(" ");
			titleBldr.Append(titleNorm);
			if (normEpub.Contains(normTitle))
			{
				var newNormTitle = Program.NormalizeToCompare(titleBldr.ToString());
				if (normEpub == newNormTitle)
				{
					SetTitle(titleBldr.ToString());
					titleBldr.Clear();
					return true;
				}
			}
			return false;
		}

		private bool SetCoverContributor(XmlNode child)
		{
			var childXml = child.OuterXml;
			if (child.Name != "p")
			{
				if (child is XmlElement)
					childXml = $"<p>{child.InnerXml.Trim()}</p>";
				else
					childXml = $"<p>{child.InnerText.Trim()}</p>";
			}
			AddCoverContributor(childXml);
			return true;
		}

		private void AddEmptyCoverPage()
		{
			var coverPage = SelectTemplatePage("Front Cover");
			var newPageDiv = _bloomDoc.CreateElement("div");
			foreach (XmlAttribute attr in coverPage.Attributes.Cast<XmlAttribute>())
				newPageDiv.SetAttribute(attr.Name, attr.Value);
			newPageDiv.SetAttribute("id", Guid.NewGuid().ToString());
			newPageDiv.InnerXml = coverPage.InnerXml;
			// Find the first endmatter page and insert the new page before it.
			var docBody = _bloomDoc.SelectSingleNode("/html/body");
			docBody.AppendChild(newPageDiv);
			var nl = _bloomDoc.CreateWhitespace(Environment.NewLine);
			docBody.AppendChild(nl);
		}

		private XmlElement GetOrCreateDataDivElement(string key, string lang)
		{
			var dataDiv = _bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='{key}' and @lang='{lang}']") as XmlElement;
			if (dataDiv == null)
			{
				dataDiv = _bloomDoc.CreateElement("div");
				dataDiv.SetAttribute("data-book", key);
				dataDiv.SetAttribute("lang", lang);
				if (lang == _epubMetaData.LanguageCode && _options.IsRtl)
					dataDiv.SetAttribute("dir", "rtl");
				var dataBook = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']");
				Debug.Assert(dataBook != null);
				var indent = _bloomDoc.CreateWhitespace("  ");
				dataBook.AppendChild(indent);
				dataBook.AppendChild(dataDiv);
				var nl = _bloomDoc.CreateWhitespace(Environment.NewLine + "    ");
				dataBook.AppendChild(nl);
			}
			return dataDiv;
		}

		private void SetDataDivTextValue(string key, string value, string lang = "*")
		{
			var dataDiv = GetOrCreateDataDivElement(key, lang);
			dataDiv.InnerText = value;
		}

		private void SetDataDivParaValue(string key, string lang, string value)
		{
			var dataDiv = GetOrCreateDataDivElement(key, lang);
			dataDiv.InnerXml = "<p>" + value + "</p>";
		}

		private void SetTitle(string title)
		{
			// This should be called only once.
			var titleNode = _bloomDoc.SelectSingleNode("/html/head/title");
			titleNode.InnerText = title;
			SetDataDivParaValue("bookTitle", _epubMetaData.LanguageCode, title);
			var zTitle = _bloomDoc.SelectSingleNode("//div[contains(@class, 'bloom-editable') and @data-book='bookTitle' and @lang='z']") as XmlElement;
			AddNewLanguageDiv(zTitle, "<p>" + title + "</p>");
		}

		private void AddCoverContributor(string paraXml)
		{
			// This may be called multiple times.
			var dataDiv = GetOrCreateDataDivElement("smallCoverCredits", _epubMetaData.LanguageCode);
			var newXml = RemoveXmlnsAttribsFromXmlString(paraXml);
			var credits = dataDiv.InnerXml + newXml;
			dataDiv.InnerXml = credits;
			var newContrib = _bloomDoc.SelectSingleNode($"//div[contains(@class, 'bloom-editable') and @data-book='smallCoverCredits' and @lang='{_epubMetaData.LanguageCode}']");
			if (newContrib == null)
			{
				var zContrib = _bloomDoc.SelectSingleNode("//div[contains(@class, 'bloom-editable') and @data-book='smallCoverCredits' and @lang='z']") as XmlElement;
				AddNewLanguageDiv(zContrib, credits);
			}
			else
			{
				newContrib.InnerXml = credits;
			}
		}

		private XmlElement AddNewLanguageDiv(XmlElement zTemplateDiv, string content)
		{
			var newDiv = _bloomDoc.CreateElement("div");
			foreach (var attr in zTemplateDiv.Attributes.Cast<XmlAttribute>())
				newDiv.SetAttribute(attr.Name, attr.Value);
			var classes = newDiv.GetAttribute("class");
			if (!classes.Contains("normal-style"))
				classes = classes + " normal-style";
			if (!classes.Contains("bloom-content1"))
				classes = classes + " bloom-content1";
			if (!classes.Contains("bloom-visibility-code-on"))
				classes = classes + " bloom-visibility-code-on";
			newDiv.SetAttribute("class", classes.Trim());
			newDiv.SetAttribute("role", "textbox");
			newDiv.SetAttribute("spellcheck", "true");
			newDiv.SetAttribute("aria-label", "false");
			newDiv.SetAttribute("tabindex", "0");
			newDiv.SetAttribute("lang", _epubMetaData.LanguageCode);
			if (_options.IsRtl)
				newDiv.SetAttribute("dir", "rtl");
			newDiv.SetAttribute("data-languagetipcontent", _options.LanguageName);
			newDiv.InnerXml = content;
			var indent = zTemplateDiv.PreviousSibling;
			zTemplateDiv.ParentNode.InsertBefore(newDiv, zTemplateDiv); // keep after label element if any
			if (indent.NodeType == XmlNodeType.Whitespace && indent.InnerText.Length > 0)
			{
				var reindent = _bloomDoc.CreateWhitespace(indent.InnerText);
				zTemplateDiv.ParentNode.InsertBefore(reindent, zTemplateDiv);
			}
			else
			{
				var nl = _bloomDoc.CreateWhitespace(Environment.NewLine);
				zTemplateDiv.ParentNode.InsertAfter(nl, newDiv);
			}
			return newDiv;
		}

		private void SetCoverImage(string imageFile)
		{
			SetDataDivTextValue("coverImage", imageFile);
			var coverImg = _bloomDoc.SelectSingleNode("//div[@class='bloom-imageContainer']/img[@data-book='coverImage']") as XmlElement;
			if (coverImg != null)
				coverImg.SetAttribute("src", imageFile);
		}


		private void AddExtraCoverImage(string imageFile, int count)
		{
			// This may not be sufficient, but maybe it's better than nothing.
			SetDataDivTextValue("coverImage" + count.ToString(), imageFile);
		}

		private bool ConvertAsafeerCreditPage(int pageNumber, string pageXhtml)
		{
			var pageDoc = new XmlDocument();
			pageDoc.PreserveWhitespace = true;
			pageDoc.LoadXml(pageXhtml);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr) as XmlElement;
			// English books from Asafeerhave a paragraph of disclaimer/credit text on page 2.
			//  Arabic books embed that text in an image (at least in the sample I looked at).
			var text = body.InnerText.Trim();
			if (!String.IsNullOrEmpty(text))
				SetDataDivParaValue("originalAcknowledgments", _epubMetaData.LanguageCode, text);
			var year = GetYearPublished();
			_asafeerCopyright = $"© Asafeer Education Technologies FZ LLC, {year}";
			_asafeerLicense = "CC BY-NC-SA 4.0";
			SetBookCopyright(_asafeerCopyright, "en");
			SetBookLicense(_asafeerLicense);
			_endCreditsPageCount = 1;   // not really an "end" credits page, but a credits page
			return true;
		}

		private int GetYearPublished()
		{
			if (_opdsEntry != null)
			{
				var publishedNode = _opdsEntry.SelectSingleNode("/a:feed/a:entry/a:published", _opdsNsmgr) as XmlElement;
				if (DateTime.TryParse(publishedNode.InnerText, out DateTime published))
					return published.Year;
			}
			return _epubMetaData.Modified.Year;
		}

		internal void SetAsafeerImageCredits()
		{
			var illustrator = String.Join(", ", _epubMetaData.Illustrators).Trim(' ', ',');
			if (String.IsNullOrEmpty(illustrator) && _opdsEntry != null)
			{
				var contribNodes = _opdsEntry.SelectNodes("/a:feed/a:entry/a:contributor[@type='Illustrator']/a:name", _opdsNsmgr).Cast<XmlElement>().ToList();
				if (contribNodes.Count == 1)
				{
					illustrator = contribNodes[0].InnerText.Trim();
				}
				else if (contribNodes.Count > 1)
				{
					foreach (var contrib in contribNodes)
					{
						if (!String.IsNullOrEmpty(illustrator))
							illustrator = illustrator + ", " + contrib.InnerText.Trim();
						else
							illustrator = contrib.InnerText.Trim();
					}
				}
			}
			SetAllImageMetadata(illustrator, _asafeerCopyright, _asafeerLicense);
			AddIllustratorContributionCredit(illustrator, _asafeerCopyright, _asafeerLicense);
			WriteAccumulatedImageAndOtherCredits();
		}

		internal bool ConvertContentPage(int pageNumber, string pageXhtml, string pageFileName)
		{
			var pageDoc = new XmlDocument();
			pageDoc.PreserveWhitespace = true;
			pageDoc.LoadXml(pageXhtml);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr) as XmlElement;
			if (IsEndCreditsPage(body, pageNumber))
				return ConvertEndCreditsPage(body, nsmgr, pageNumber);

			var imageCount = 0;
			var textCount = 0;
			var videoCount = 0;
			var firstChild = "";
			var lastChild = "";
			var prevChild = "";
			var rawNodes = new List<XmlNode>();
			// Summarize the page content to find an appropriate template page.
			ExtractTextAndImageNodes(body, rawNodes, ref imageCount, ref textCount, ref videoCount, ref firstChild, ref prevChild);
			var templatePage = SelectTemplatePage(imageCount, textCount, videoCount, firstChild, lastChild);
			if (templatePage == null)
			{
				Console.WriteLine("ERROR: cannot retrieve template page for {0} images and {1} text fields", imageCount, textCount);
				return false;
			}
			var newPageDiv = _bloomDoc.CreateElement("div");
			foreach (XmlAttribute attr in templatePage.Attributes.Cast<XmlAttribute>())
				newPageDiv.SetAttribute(attr.Name, attr.Value);
			newPageDiv.SetAttribute("id", Guid.NewGuid().ToString());
			if (_publisher == "3Asafeer")
				newPageDiv.SetAttribute("data-page-number", (pageNumber - 1).ToString());
			else
				newPageDiv.SetAttribute("data-page-number", pageNumber.ToString());
			newPageDiv.SetAttribute("lang", _epubMetaData.LanguageCode);
			if (_options.IsRtl)
				newPageDiv.SetAttribute("dir", "rtl");
			newPageDiv.InnerXml = templatePage.InnerXml;
			// Find the first endmatter page and insert the new page before it.
			var endMatter = _bloomDoc.SelectSingleNode("/html/body/div[@data-xmatter-page='insideBackCover']");
			var docBody = _bloomDoc.SelectSingleNode("/html/body");
			docBody.InsertBefore(newPageDiv, endMatter);
			var nl = _bloomDoc.CreateWhitespace(Environment.NewLine);
			docBody.InsertAfter(nl, newPageDiv);

			var imageIdx = 0;
			var videoIdx = 0;
			var textIdx = 0;
			prevChild = "";
			var innerXmlBldr = new StringBuilder();
			var images = newPageDiv.SelectNodes(".//img").Cast<XmlElement>().ToList();
			var videos = newPageDiv.SelectNodes(".//video").Cast<XmlElement>().ToList();
			var textGroupDivs = newPageDiv.SelectNodes(".//div[contains(@class,'bloom-translationGroup')]").Cast<XmlElement>().ToList();
			if (rawNodes.Count > 0)
			{
				foreach (var node in rawNodes)
				{
					if (node.Name == "img")
					{
						StoreImage(imageIdx, images, node as XmlElement, pageFileName);
						++imageIdx;
					}
					else if (node.Name == "p")
					{
						innerXmlBldr.Append("<p>");
						innerXmlBldr.Append(FixInnerXml(node.InnerXml.Trim()));
						innerXmlBldr.AppendLine("</p>");
						var div = StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);
						ApplyAnyAudio(div, pageFileName);
						++textIdx;
					}
					else if (node.Name == "video")
					{
						StoreVideo(videoIdx, videos, node as XmlElement, nsmgr);
						++videoIdx;
					}
					else
					{
						Debug.Assert(node is XmlText, "The XmlNode has to be text if it's neither <img> nor <p>!");
						innerXmlBldr.Append("<p>");
						innerXmlBldr.Append(node.InnerText.Trim());
						innerXmlBldr.AppendLine("</p>");
						StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);
						++textIdx;
					}
				}
			}
			return true;
		}

		private void ApplyAnyAudio(XmlElement div, string pageFileName)
		{
			if (!_epubMetaData.MediaOverlays.TryGetValue(pageFileName, out string smilId))
				return;
			if (!_epubMetaData.SmilFiles.TryGetValue(smilId, out SmilFileData smilFile))
				return;
			var spans = div.SafeSelectNodes(".//span[@id]").Cast<XmlElement>();
			if (spans.Count() > 0)
			{
				var parsUsed = new List<SmilPar>();
				var soundFiles = new HashSet<string>();
				foreach (var span in spans)
				{
					var id = span.GetAttribute("id");
					var compositeId = $"{pageFileName}#{id}";
					if (smilFile.SmilPars.TryGetValue(compositeId, out SmilPar smil))
					{
						span.SetAttribute("class", "bloom-highlightSegment");
						parsUsed.Add(smil);
						soundFiles.Add(smil.AudioFileName);
					}
					if (!String.IsNullOrEmpty(id))
						span.SetAttribute("id", NewGuidBasedId());
				}
				if (parsUsed.Count > 0)
				{
					if (soundFiles.Count == 1)
					{
						var haveClipBounds = smilFile.FileClipBounds.TryGetValue(parsUsed[0].AudioFileName, out ClipBounds clipBounds);
						div.SetAttribute("data-audiorecordingmode", "TextBox");
						var clipBegin = parsUsed[0].AudioClipStart;
						var clipEnd = parsUsed[parsUsed.Count - 1].AudioClipEnd;
						string duration = clipEnd ?? "";
						double begin = 0.0;
						if (!String.IsNullOrEmpty(clipBegin) && !String.IsNullOrEmpty(clipEnd) &&
							Double.TryParse(clipBegin, out begin) && Double.TryParse(clipEnd, out double end) &&
							end > begin && begin > 0.0)
						{
							var delta = end - begin;
							duration = String.Format("{0:0.000}", delta);
						}
						var divId = NewGuidBasedId();
						div.SetAttribute("id", divId);
						var source = Path.Combine(_epubMetaData.EpubContentFolder, "audio", parsUsed[0].AudioFileName);
						var dest = Path.Combine(_bookFolder, "audio", divId + Path.GetExtension(source));

						var destDirectory = Path.GetDirectoryName(dest);
						Directory.CreateDirectory(destDirectory);   // only needed first time, but doesn't hurt.

						var shortenedFile = false;
						if (duration == clipEnd && (!haveClipBounds || duration == clipBounds.FinalClipEnd))
							File.Copy(source, dest);
						else
							shortenedFile = ExtractAudioCopy(source, dest, clipBegin, clipEnd);
						var endsBldr = new StringBuilder();
						if (shortenedFile)
						{
							div.SetAttribute("data-duration", duration);
							foreach (var par in parsUsed)
							{
								if (endsBldr.Length > 0)
									endsBldr.Append(" ");
								var audioClipEnd = par.AudioClipEnd ?? "";
								if (Double.TryParse(audioClipEnd, out double endClip))
									audioClipEnd = String.Format("{0:0.000}", endClip - begin);
								endsBldr.Append(audioClipEnd);
							}
						}
						else
						{
							div.SetAttribute("data-duration", clipEnd);
							foreach (var par in parsUsed)
							{
								if (endsBldr.Length > 0)
									endsBldr.Append(" ");
								endsBldr.Append(par.AudioClipEnd ?? "");
							}
						}
						div.SetAttribute("data-audiorecordingendtimes", endsBldr.ToString().Trim());
						var md5 = ComputeMd5ForFile(dest);
						div.SetAttribute("recordingmd5", md5);
						var classes = div.GetAttribute("class");
						div.SetAttribute("class", classes + " audio-sentence bloom-postAudioSplit");
					}
				}
			}
		}

		private bool ExtractAudioCopy(string source, string dest, string clipBegin, string clipEnd)
		{
			var ffmpeg = "ffmpeg";
			if (File.Exists(Path.Combine(_options.BloomFolder, "ffmpeg.exe")))
				ffmpeg = Path.Combine(_options.BloomFolder, "ffmpeg.exe");
			else if (File.Exists(Path.Combine(_options.BloomFolder, "Debug", "ffmpeg.exe")))
				ffmpeg = Path.Combine(_options.BloomFolder, "Debug", "ffmpeg.exe");
			else if (File.Exists(Path.Combine(_options.BloomFolder, "Release", "ffmpeg.exe")))
				ffmpeg = Path.Combine(_options.BloomFolder, "Release", "ffmpeg.exe");
			string arguments = $"-i \"{source}\" -acodec copy -ss {clipBegin} -to {clipEnd} \"{dest}\"";
			var proc = new Process
			{
				StartInfo =
					{
						FileName = ffmpeg,
						Arguments = arguments,
						UseShellExecute = false, // enables CreateNoWindow
						CreateNoWindow = true, // don't need a DOS box
						RedirectStandardOutput = true,
						RedirectStandardError = true,
					}
			};
			proc.Start();
			proc.WaitForExit();
			if (proc.ExitCode != 0)
			{
				Console.WriteLine("WARNING: {0} {1} failed", ffmpeg, arguments);
				Console.WriteLine("##########################################################");
				Console.WriteLine("STDOUT={0}", proc.StandardOutput.ReadToEnd());
				Console.WriteLine("##########################################################");
				Console.WriteLine("STDERR={0}", proc.StandardError.ReadToEnd());
				Console.WriteLine("##########################################################");
				File.Copy(source, dest, true);
				return false;
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: {0} {1} succeeded", ffmpeg, arguments);
			return true;
		}

		private string ComputeMd5ForFile(string dest)
		{
			// We don't have access to the javascript function used by Bloom.
			// I hope this is compatible!
			using (var md5 = MD5.Create())
			{
				var inputBytes = File.ReadAllBytes(dest);
				var hashBytes = md5.ComputeHash(inputBytes);
				// Convert the byte array to hexadecimal string
				var sb = new StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("x2"));
				}
				return sb.ToString();
			}
		}
		
		private string NewGuidBasedId()
		{
			var id = Guid.NewGuid().ToString();
			var firstChar = id[0];
			if (firstChar >= '0' && firstChar <= '9')
				return "i" + id;
			else
				return id;
		}

		/// <summary>
		/// One epub I've seen has multiple layers of &lt;b&gt; element enclosing a &lt;p&gt; element.  This seems rather
		/// wierd, but let's try to cope with such situations without propagating *all* of the badness.
		/// </summary>
		private static void ExtractTextAndImageNodes(XmlElement body, List<XmlNode> rawNodes, ref int imageCount, ref int textCount,
			ref int videoCount, ref string firstChild, ref string prevChild)
		{
			var skippingCircularFLO = false;
			foreach (var child in body.ChildNodes.Cast<XmlNode>())
			{
				if (child.NodeType == XmlNodeType.Comment && child.OuterXml == "<!--CircularFLO Body-->")
				{
					skippingCircularFLO = true;
					continue;
				}
				if (child.NodeType == XmlNodeType.Comment && child.OuterXml == "<!--END CircularFLO Body-->")
				{
					skippingCircularFLO = false;
					continue;
				}
				if (skippingCircularFLO)
					continue;
				if (child is XmlWhitespace)
					continue;
				if (child.Name == "img")
				{
					++imageCount;
					if (String.IsNullOrEmpty(firstChild))
						firstChild = child.Name;
					prevChild = child.Name;
				}
				else if (child.Name == "video")
				{
					++videoCount;
					if (String.IsNullOrEmpty(firstChild))
						firstChild = child.Name;
					prevChild = child.Name;
				}
				else if (child.Name == "p" || child is XmlText)
				{
					if (String.IsNullOrWhiteSpace(child.InnerText))
						continue;   // ignore empty paragraphs
					if (String.IsNullOrEmpty(firstChild))
						firstChild = "p";
					if (prevChild != "p")
						++textCount;
					prevChild = "p";
				}
				else if (child.Name == "b" || child.Name == "i" || child.Name == "strong" || child.Name == "em" || child.Name == "div")
				{
					// recurse!
					ExtractTextAndImageNodes(child as XmlElement, rawNodes, ref imageCount, ref textCount, ref videoCount,
						ref firstChild, ref prevChild);
					continue;
				}
				else
				{
					// Should we pay attention to <br> to create paragraphs instead of every text node becoming a paragraph?
					// I think the <br> element breaks up the text nodes anyway so its effect is implicit.
					if (child.Name != "br")
						Console.WriteLine("WARNING: UNEXPECTED ELEMENT IN EPUB PAGE: {0} / \"{1}\"", child.NodeType.ToString(), child.OuterXml);
					continue;
				}
				rawNodes.Add(child);
			}
		}

		/// <summary>
		/// Start by removing the xmlns attribute added gratuitously by the C# library code.  Then
		/// remove remove unwanted <br/> markers and empty format markers and try to minimize
		/// markers (like &lt;b&gt; or &lt;/b&gt;) to minimize extra spaces.
		/// </summary>
		/// <remarks>
		/// using multiple variables is to aid in debugging...
		/// Each step should have a comment describing its intended effect.
		/// </remarks>
		static internal string FixInnerXml(string innerXml)
		{
			var plain00 = RemoveXmlnsAttribsFromXmlString(innerXml);
			//Console.WriteLine("DEBUG FixInnerXml: 00=\"{0}\"", plain00);
			// remove any empty format marker pairs, handling any possible nesting
			var plain01 = RegexReplaceAsNeeded(plain00, @"<(b|i|strong|em)>(\s*)</\1>", "$2");
			//Console.WriteLine("DEBUG FixInnerXml: 01=\"{0}\"", plain01);
			// remove any format close marker followed by space and the same format open marker
			var plain02 = Regex.Replace(plain01, @"</(b|i|strong|em)>(\s*)<\1>", "$2");
			//Console.WriteLine("DEBUG FixInnerXml: 02=\"{0}\"", plain02);
			// move <br/> past any number ofclosing format markers
			var plain03 = Regex.Replace(plain02, @"\s*<br />\s*((</(b|i|strong|em)>)*\s*)$", "$1<br />");
			//Console.WriteLine("DEBUG FixInnerXml: 03=\"{0}\"", plain03);
			// remove any trailing <br/> (with surrounding whitespace)
			var plain04 = Regex.Replace(plain03, @"(\s*<br />\s*)+$", "");
			//Console.WriteLine("DEBUG FixInnerXml: 04=\"{0}\"", plain04);
			// move <br/> before any number of opening format markers
			var plain05 = Regex.Replace(plain04, @"((<(b|i|strong|em)>)*\s*)\s*<br />\s*", "<br />$1");
			//Console.WriteLine("DEBUG FixInnerXml: 05=\"{0}\"", plain05);
			// remove leading <br/>
			var plain06 = Regex.Replace(plain05, @"^(\s*<br />\s*)+", "");
			//Console.WriteLine("DEBUG FixInnerXml: 06=\"{0}\"", plain06);
			// move whitespace before opening format marker (3x for nesting)
			var plain07 = RegexReplaceAsNeeded(plain06, @"<(b|i|strong|em)>(\s+)", "$2<$1>");
			//Console.WriteLine("DEBUG FixInnerXml: 07=\"{0}\"", plain07);
			// move whitespace after closing format marker (3x for nesting)
			var plain08 = RegexReplaceAsNeeded(plain07, @"(\s+)</(b|i|strong|em)>", "</$2>$1");
			//Console.WriteLine("DEBUG FixInnerXml: 08=\"{0}\"", plain08);
			// remove whitespace around any remaining <br/>
			var plain09 = Regex.Replace(plain08, @"\s*<br />\s*", "<br />");
			///Console.WriteLine("DEBUG FixInnerXml: 09=\"{0}\"", plain09);
			// collapse multiple spaces into one space
			var plain10 = Regex.Replace(plain09, @"  +", " ");
			//Console.WriteLine("DEBUG FixInnerXml: 10=\"{0}\"", plain10);
			//Console.WriteLine("DEBUG FixInnerXml: Final=\"{0}\"", plain10.Trim());
			var plain11 = Regex.Replace(plain10, @" class=""[^""]*"" style=""[^""]*"">",">");
			return plain11.Trim();
		}

		private static string RegexReplaceAsNeeded(string input, string match, string replace)
		{
			var inValue = input;
			var outValue = input;
			int count = 0;
			do
			{
				if (++count > 10)
				{
					Console.WriteLine("WARNING: RegexReplaceAsNeeded has looped 10 times without terminating!");
					Console.WriteLine("  RegexReplaceAsNeeded(\"{0}\", \"{1}\", \"{2}\") => \"{3}\"", input, match, replace, outValue);
					return outValue;
				}
				inValue = outValue;
				outValue = Regex.Replace(inValue, match, replace);
			} while (outValue != inValue);
			return outValue;
		}

		private void StoreImage(int imageIdx, List<XmlElement> images, XmlElement img, string pageFileName)
		{
			var parentId = img.ParentNode?.GetOptionalStringAttribute("id", null);
			var alt = img.GetAttribute("alt");
			var src = img.GetAttribute("src");
			if (imageIdx < images.Count)
			{
				src = Path.GetFileName(src);	// don't want leading image/ or images/
				images[imageIdx].SetAttribute("src", src);
				if (String.IsNullOrWhiteSpace(alt))
					images[imageIdx].SetAttribute("alt", alt);
				else
					images[imageIdx].SetAttribute("alt", src);
				if (!String.IsNullOrWhiteSpace(alt) && !String.IsNullOrEmpty(parentId))
				{
					AddImageDescriptionAndAudio(images[imageIdx], alt, parentId, pageFileName);
				}
				return;
			}
			if (images.Count == 1)
			{
				src = Path.GetFileName(src);    // don't want leading image/ or images/
				var newSrcName = Path.GetFileNameWithoutExtension(src);
				var oldSrc = images[0].GetAttribute("src");
				var oldAlt = images[0].GetAttribute("alt");
				var oldSrcName = Path.GetFileNameWithoutExtension(oldSrc);
				if ((Int32.TryParse(newSrcName, out int newNumber) && !Int32.TryParse(oldSrcName, out int oldNumber)) ||
					(oldAlt??"").Length > (alt??"").Length)
				{
					// keep image with an alt value in preference to one without (or longest alt value anyway)
					// keep more complex image filename if this one is purely numeric
					if (_options.VeryVerbose)
						Console.WriteLine("INFO: retaining image file {0} in preference to {1}", oldSrc, src);
					return;
				}
				Console.WriteLine("INFO: replacing image file ({0}) with {1}", oldSrc, src);
				images[0].SetAttribute("src", src);
				if (String.IsNullOrWhiteSpace(alt))
					images[0].SetAttribute("alt", alt);
				else
					images[0].SetAttribute("alt", src);
				if (!String.IsNullOrWhiteSpace(alt) && !String.IsNullOrEmpty(parentId))
				{
					AddImageDescriptionAndAudio(images[0], alt, parentId, pageFileName);
				}
				return;
			}
			else
			{
				Console.WriteLine("WARNING: no place on page to show image file {0}", src);
			}
		}

		private void AddImageDescriptionAndAudio(XmlElement newImg, string alt, string parentId, string pageFileName)
		{
			// Convert alt string into an image description.  This may or may not be valid...
			var groupDiv = newImg.OwnerDocument.CreateElement("div");
			newImg.ParentNode.AppendChild(groupDiv);
			groupDiv.SetAttribute("class", "bloom-translationGroup bloom-imageDescription bloom-trailingElement");
			groupDiv.SetAttribute("data-default-languages", "auto");
			var editDiv = groupDiv.OwnerDocument.CreateElement("div");
			groupDiv.AppendChild(editDiv);
			editDiv.SetAttribute("class", "bloom-editable ImageDescriptionEdit-style bloom-content1 bloom-visibility-code-on");
			editDiv.SetAttribute("role", "textbox");
			editDiv.SetAttribute("aria-label", "false");
			editDiv.SetAttribute("lang", _epubMetaData.LanguageCode);
			editDiv.SetAttribute("contenteditable", "true");
			editDiv.SetAttribute("tabindex", "0");
			editDiv.SetAttribute("spellcheck", "true");
			var p = groupDiv.OwnerDocument.CreateElement("p");
			editDiv.AppendChild(p);
			var span = groupDiv.OwnerDocument.CreateElement("span");
			p.AppendChild(span);
			span.SetAttribute("id", NewGuidBasedId());
			span.InnerText = alt;
			var zDiv = groupDiv.OwnerDocument.CreateElement("div");
			groupDiv.AppendChild(zDiv);
			zDiv.SetAttribute("class", "bloom-editable ImageDescriptionEdit-style");
			zDiv.SetAttribute("lang", "z");
			zDiv.SetAttribute("contenteditable", "true");
			// After establishing the text description, look for any linked audio.
			var compositeId = $"{pageFileName}#{parentId}";
			if (!_epubMetaData.MediaOverlays.TryGetValue(pageFileName, out string smilId))
				return;
			if (!_epubMetaData.SmilFiles.TryGetValue(smilId, out SmilFileData smilFile))
				return;
			if (!smilFile.SmilPars.TryGetValue(compositeId, out SmilPar smilPar))
			{
				// Pages without actual text seem to not align the smil quite right, but
				// use a fixed reference to #blank.
				if (!smilFile.SmilPars.TryGetValue($"{pageFileName}#blank", out smilPar))
					return;
			}
			// We apparently have a sound file to link up.
			var classes = editDiv.GetAttribute("class");
			editDiv.SetAttribute("class", classes + " audio-sentence bloom-postAudioSplit");
			editDiv.SetAttribute("data-audiorecordingmode", "TextBox");
			span.SetAttribute("class", "bloom-highlightSegment");
			var duration = smilPar.AudioClipEnd ?? "";
			var haveClipBounds = smilFile.FileClipBounds.TryGetValue(smilPar.AudioFileName, out ClipBounds clipBounds);
			if (!String.IsNullOrEmpty(smilPar.AudioClipStart) && !String.IsNullOrEmpty(smilPar.AudioClipEnd) &&
				Double.TryParse(smilPar.AudioClipStart, out double start) && Double.TryParse(smilPar.AudioClipEnd, out double end))
			{
				if (smilPar.AudioClipStart != clipBounds.InitialClipStart)
					duration = String.Format("{0:0.000}", end - start);
			}
			var divId = NewGuidBasedId();
			editDiv.SetAttribute("id", divId);
			var source = Path.Combine(_epubMetaData.EpubContentFolder, "audio", smilPar.AudioFileName);
			var dest = Path.Combine(_bookFolder, "audio", divId + Path.GetExtension(source));

			var destDirectory = Path.GetDirectoryName(dest);
			Directory.CreateDirectory(destDirectory);   // only needed first time, but doesn't hurt.

			var shortenedFile = false;
			if (duration == (smilPar.AudioClipEnd ?? "") && (!haveClipBounds || duration == clipBounds.FinalClipEnd))
				File.Copy(source, dest);
			else
				shortenedFile = ExtractAudioCopy(source, dest, smilPar.AudioClipStart, smilPar.AudioClipEnd);
			if (shortenedFile)
			{
				editDiv.SetAttribute("data-duration", duration);
				editDiv.SetAttribute("data-audiorecordingendtimes", duration);
			}
			else
			{
				editDiv.SetAttribute("data-duration", smilPar.AudioClipEnd ?? "");
				editDiv.SetAttribute("data-audiorecordingendtimes", smilPar.AudioClipEnd ?? "");
			}
			var md5 = ComputeMd5ForFile(dest);
			editDiv.SetAttribute("recordingmd5", md5);
		}

		private void StoreVideo(int videoIdx, List<XmlElement> videos, XmlElement video, XmlNamespaceManager nsmgr)
		{
			var epubSource = video.SelectSingleNode("./x:source", nsmgr);
			var epubSrc = epubSource?.GetOptionalStringAttribute("src", null);
			var videoType = epubSource?.GetOptionalStringAttribute("type", null);
			if (epubSrc == null || videoType != "video/mp4")
			{
				Console.WriteLine("WARNING: invalid video element in epub: {0}", video.OuterXml);
				return;
			}
			if (videoIdx < videos.Count)
			{
				var bloomSource = videos[videoIdx].SelectSingleNode("./source") as XmlElement;
				bloomSource?.SetAttribute("src", epubSrc);
			}
			else
			{
				Console.WriteLine("WARNING: no place on page to show video {0}", video.OuterXml);
			}
		}

		private XmlElement StoreAccumulatedParagraphs(int textIdx, StringBuilder innerXmlBldr, List<XmlElement> textGroupDivs)
		{
			Debug.Assert(innerXmlBldr != null && innerXmlBldr.Length > 0);
			Debug.Assert(textGroupDivs != null && textGroupDivs.Count > 0);
			XmlElement div = null;
			if (textIdx < textGroupDivs.Count)
			{
				var zTemplateDiv = textGroupDivs[textIdx].SelectSingleNode("./div[contains(@class, 'bloom-editable') and @lang='z' and @contenteditable='true']") as XmlElement;
				// Add new div with accumulated paragraphs
				div = AddNewLanguageDiv(zTemplateDiv, innerXmlBldr.ToString().Trim());
			}
			else
			{
				// Cram new accumulation into last text group.
				var groupDiv = textGroupDivs[textGroupDivs.Count - 1];
				div = groupDiv.SelectSingleNode($"./div[@lang='{_epubMetaData.LanguageCode}']") as XmlElement;
				if (div != null)
				{
					var inner = div.InnerXml;
					var xml = RemoveXmlnsAttribsFromXmlString(inner);
					innerXmlBldr.Insert(0, Environment.NewLine);
					innerXmlBldr.Insert(0, xml);
					div.InnerXml = innerXmlBldr.ToString().Trim();
				}
				else
				{
					Debug.Assert(div != null);
				}
			}
			innerXmlBldr.Clear();
			return div;
		}

		private XmlElement SelectTemplatePage(int imageCount, int textCount, int videoCount, string firstChild, string lastChild)
		{
			if (imageCount > 1)
			{
				// We can't handle 2 or more images on the page automatically at this point.
				Console.WriteLine("Encountered page with {0} pictures: only one is stored", imageCount);
			}
			if (videoCount > 1)
			{
				// We can't handle 2 or more videos on the page automatically at this point.
				Console.WriteLine("Encountered page with {0} videos: only the first is stored", videoCount);
			}
			if (videoCount == 0)
			{
				if (imageCount == 0 && textCount == 0)
				{
					Console.WriteLine("Encountered empty page!?");
					return null;
				}
				if (imageCount == 0)
				{
					return SelectTemplatePage("Just Text");
				}
				if (imageCount > 0)
				{
					switch (textCount)
					{
						case 0:
							return SelectTemplatePage("Just a Picture");
						case 1:
							if (firstChild == "img")
								return SelectTemplatePage("Basic Text & Picture");
							else
								return SelectTemplatePage("Picture on Bottom");
						case 2:
							//Debug.Assert(firstChild == "p" && lastChild == "p");
							return SelectTemplatePage("Picture in Middle");
					}
				}
			}
			else if (videoCount > 0)
			{
				if (imageCount == 0)
				{
					if (textCount == 0)
					{
						return SelectTemplatePage("Just Video");
					}
					else
					{
						return SelectTemplatePage("Video Over Text");
					}
				}
				else if (imageCount == 1)
				{
					if (textCount == 0)
					{
						return SelectTemplatePage("Picture and Video");
					}
					else
					{
						return SelectTemplatePage("Picture, Video, Text");
					}
				}
				else if (imageCount == 2)
				{
					return SelectTemplatePage("Video, 2 Pictures and Text");
				}
			}
			Console.WriteLine("Could not determine template page type for {0} text blocks, {1} images, and {2} videos", textCount, imageCount, videoCount);
			return null;
		}

		private XmlElement SelectTemplatePage(string id)
		{
			if (_options.UseLandscape && id == "Basic Text & Picture" || id == "Picture on Bottom")
				id = "Picture on Left";
			foreach (var page in _templatePages)
			{
				if (page.GetAttribute("id") == id)
					return page;
			}
			return null;
		}

		private bool IsEndCreditsPage(XmlElement body, int pageNumber)
		{
			if (pageNumber < _epubMetaData.PageFiles.Count / 2)
				return false;   // Books should never be more than half end credits!
			if (pageNumber > _endCreditsStart)
				return true;    // Once we hit the end credits, we assume other pages go on inside/output back cover.
			var divs = body.SelectNodes(".//div[@class='attrb-full' or @class='attribution-text' or @class='license_container']");
			if (divs.Count > 0)
			{
				_endCreditsStart = pageNumber;
				return true;    // StoryWeaver output apparently...
			}
			var text = body.InnerText;
			// Not all books contain an explicit copyright.  But I think all the books we want use Creative Commons licensing,
			// and they appear to use the English phrase.
			if (text.Contains("Creative Commons") || text.Contains("http://creativecommons.org/licenses/"))
			{
				_endCreditsStart = pageNumber;
				return true;
			}
			return false;
		}

		static public string RemoveXmlnsAttribsFromXmlString(string xml)
		{
			return Regex.Replace(xml, " xmlns[:a-z]*=[\"'][^\"']*[\"']", "", RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
		}

		private bool ConvertEndCreditsPage(XmlElement body, XmlNamespaceManager nsmgr, int pageNumber)
		{
			if (_endCreditsPageCount == 0)
				_creditsAndPages = new Dictionary<string, List<int>>();
			++_endCreditsPageCount;
			if (pageNumber == _epubMetaData.PageFiles.Count - 1)
			{
				var divs = body.SelectNodes(".//div[@class='back-cover-top' or @class='back-cover-bottom']").Cast<XmlElement>().ToList();
				if (divs.Count == 0)
				{
					if (NeedCopyrightInformation())
						ProcessRawCreditsPageForCopyrights(body, pageNumber);
					WriteAccumulatedImageAndOtherCredits();
					return true;
				}
			}
			else
			{
				var divs = body.SelectNodes(".//div[@class='attrb-full' or @class='attribution-text' or @class='license_container']").Cast<XmlElement>().ToList();
				if (divs.Count == 0)
				{
					ProcessRawCreditsPageForCopyrights(body, pageNumber);
					return true;
				}
			}
			return false;
		}

		private void SetBookAuthorInContributions()
		{
			var author = String.Join(", ", _epubMetaData.Authors).Trim(',', ' ');
			if (String.IsNullOrEmpty(author) && _opdsEntry != null)
			{
				var authorNames = _opdsEntry.SelectNodes("/a:feed/a:entry/a:author/a:name", _opdsNsmgr).Cast<XmlElement>().ToList();
				foreach (var name in authorNames)
				{
					if (String.IsNullOrWhiteSpace(name.InnerText))
						continue;
					if (!String.IsNullOrEmpty(author))
						author = author + ", " + name.InnerText.Trim();
					else
						author = name.InnerText.Trim();
				}
			}
			if (!String.IsNullOrEmpty(author))
			{
				string writtenByFmt;
				switch (_creditsLang)
				{
					case "fr":
						writtenByFmt = "<p>Écrit par {0}.</p>{1}";
						break;
					default:
						writtenByFmt = "<p>Written by {0}.</p>{1}";
						break;
				}
				var writtenBy = String.Format(writtenByFmt, author, Environment.NewLine);
				_contributionsXmlBldr.Insert(0, writtenBy);
			}
		}

		private void WriteAccumulatedImageAndOtherCredits()
		{
			SetBookAuthorInContributions();
			if (_creditsAndPages != null)
			{
				if (_creditsAndPages.Count == 1)
				{
					var creditText = FormatIllustrationCredit(_creditsAndPages.Keys.First());
					_contributionsXmlBldr.AppendLine($"<p>Images {creditText}</p>");
				}
				else if (_creditsAndPages.Count > 1)
				{
					foreach (var credit in _creditsAndPages.Keys)
					{
						var pagesText = ConvertIntListToPageString(_creditsAndPages[credit]);
						var creditText = FormatIllustrationCredit(credit);
						_contributionsXmlBldr.AppendLine($"<p>{pagesText} {creditText}</p>");
					}
				}
			}
			AddToDataDivParaValue("originalContributions", _contributionsXmlBldr);
			if (_supportedByXmlBldr.Length > 0)
				AddToDataDivParaValue("versionAcknowledgments", _supportedByXmlBldr);
		}

		private void AddToDataDivParaValue(string name, StringBuilder newData)
		{
			var dataDiv = GetOrCreateDataDivElement(name, _creditsLang);
			if (!String.IsNullOrWhiteSpace(dataDiv.InnerText))
			{
				newData.Insert(0, Environment.NewLine);
				var oldXml = RemoveXmlnsAttribsFromXmlString(dataDiv.InnerXml);
				newData.Insert(0, oldXml);
			}
			dataDiv.InnerXml = newData.ToString().Trim();
			newData.Clear();
		}

		private string FormatIllustrationCredit(string credit)
		{
			var matchCredits = kMatchRawPrathamIllustrationCredit;
			if (_creditsLang == "fr")
				matchCredits = kFrenchMatchRawPrathamIllustrationCredit;
			var match = Regex.Match(credit, matchCredits);
			if (match.Success)
			{
				var creator = MakeSafeForXhtml(match.Groups[1].Value.Trim(' ', '.'));
				var copyright = MakeSafeForXhtml(match.Groups[2].Value.Trim(' ', '.'));
				var license = match.Groups[3].Value.Trim();
				if (copyright.StartsWith("Copyright", StringComparison.InvariantCulture))
					copyright = copyright.Substring(9).Trim();
				if (_creditsLang == "fr")
					return string.Format("de {0}. {1}. {2}.", creator, copyright, license);
				else
					return String.Format("by {0}. {1}. {2}.", creator, copyright, license);
			}
			return credit;	// stick with what we have...
		}

		private string MakeSafeForXhtml(string raw)
		{
			if (raw.IndexOfAny(new[] { '<', '>', '&' }) < 0 ||
				raw.IndexOf("&amp;", StringComparison.InvariantCulture) >= 0 ||
				raw.IndexOf("&lt;", StringComparison.InvariantCulture) >= 0 ||
				raw.IndexOf("&gt;", StringComparison.InvariantCulture) >= 0)
			{
				return raw;
			}
			return raw.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		private string ConvertIntListToPageString(List<int> pageList)
		{
			// Image on page 37
			// Images on front cover, pages 1-2
			// Images on pages 1–2 by 
			// Images on pages 4, 7 by 
			if (pageList.Count == 1)
			{
				if (_creditsLang == "fr")
				{
					if (pageList[0] == 0)
						return "Image sur la couverture avant";
					else
						return $"Image à la page {pageList[0]}";
				}
				else
				{
					if (pageList[0] == 0)
						return "Image on front cover";
					else
						return $"Image on page {pageList[0]}";
				}
			}
			var bldr = new StringBuilder();
			int firstPageIndex = 0;
			if (pageList[0] == 0)
			{
				if (_creditsLang == "fr")
				{
					bldr.Append("Images sur la couverture avant, ");
					if (pageList.Count == 2)
						bldr.Append("page ");
					else
						bldr.Append("pages ");
				}
				else
				{
					bldr.Append("Images on Front Cover, ");
					if (pageList.Count == 2)
						bldr.Append("page ");
					else
						bldr.Append("pages ");
				}
				++firstPageIndex;
			}
			else
			{
				if (_creditsLang == "fr")
					bldr.Append("Images aux pages ");
				else
					bldr.Append("Images on pages ");
			}
			bldr.AppendFormat("{0}", pageList[firstPageIndex]);
			int lastPageShowing = pageList[firstPageIndex];
			for (int i = firstPageIndex + 1; i < pageList.Count; ++i)
			{
				var currentPage = pageList[i];
				if (currentPage == lastPageShowing)
				{
					Console.WriteLine("WARNING: processing credits for more than one image on page {0}", currentPage);
					continue;       // same page (first page image or multiple images on page)
				}
				var previousPage = pageList[i - 1];
				if (currentPage == previousPage + 1)
					continue;		// contiguous range
				// Now it's time to add to the builder.
				if (previousPage == lastPageShowing)
				{
					bldr.AppendFormat(", {0}", currentPage);
				}
				else
				{
					bldr.AppendFormat("-{0}, {1}", previousPage, currentPage);
				}
				lastPageShowing = currentPage;
			}
			var lastPage = pageList[pageList.Count - 1];
			if (lastPage != lastPageShowing)
				bldr.AppendFormat("-{0}", lastPage);	// had to be contiguous range to finish
			return bldr.ToString();
		}

		private bool NeedCopyrightInformation()
		{
			var copyright = GetOrCreateDataDivElement("copyright", "*");
			return String.IsNullOrWhiteSpace(copyright.InnerText);
		}

		private bool NeedLicenseInformation()
		{
			var license = GetOrCreateDataDivElement("licenseUrl", "*");
			return String.IsNullOrWhiteSpace(license.InnerText);
		}

		private void ProcessRawCreditsPageForCopyrights(XmlElement body, int pageNumber)
		{
			var bodyText = body.InnerText;
			if (bodyText.Contains("Pratham Books") && bodyText.Contains("©") &&
				(bodyText.Contains(kStoryAttribution) || bodyText.Contains(kIllustrationAttribs) || bodyText.Contains(kImagesAttribs)))
			{
				ProcessRawPrathamCreditsPage(bodyText, pageNumber, "en");
				return;
			}
			if (bodyText.Contains("Pratham Books") && bodyText.Contains("Creative Commons") &&
				(bodyText.Contains(kFrenchStoryAttribution) || bodyText.Contains(kFrenchIllustrationAttribs)))
			{
				_creditsLang = "fr";
				ProcessRawPrathamCreditsPage(bodyText, pageNumber, "fr");
				return;
			}
			var artCopyright = "";
			var bookCopyright = "";
			var copyright = GetOrCreateDataDivElement("copyright", "*");
			var matches = Regex.Matches(bodyText, "(©[^0-9©]* ([12][09][0-9][0-9]))", RegexOptions.CultureInvariant|RegexOptions.Singleline);
			if (matches.Count > 1)
				Console.WriteLine("WARNING: MULTIPLE COPYRIGHTS FOUND ON CREDIT PAGE!  THIS NEEDS TO BE CHECKED OUT!");
			if (String.IsNullOrWhiteSpace(copyright.InnerText))
			{
				//var match = Regex.Match(bodyText, "(©[^0-9]* ([12][09][0-9][0-9]))", RegexOptions.CultureInvariant);
				//if (match.Success)
				if (matches.Count > 0)
				{
					var match = matches[0];
					var copyrightMatch = match.Groups[1].Value;
					if (copyrightMatch.StartsWith("© Text:", StringComparison.InvariantCulture) && copyrightMatch.Contains("Artwork:"))
					{
						var beginArtwork = copyrightMatch.IndexOf("Artwork:", StringComparison.InvariantCulture);
						bookCopyright = "© " + copyrightMatch.Substring(7, beginArtwork - 7).Trim() + " " + match.Groups[2].Value;
						artCopyright = $"© { copyrightMatch.Substring(beginArtwork + 8).Trim()}";
					}
					else
					{
						bookCopyright = copyrightMatch;
					}
					SetBookCopyright(bookCopyright, "en");
				}
				else if (_publisher != null && _publisher.ToLowerInvariant() == "book dash")
				{
					// Book Dash books are shy about admitting it, but they're effectively copyright by Book Dash
					// since they're all released under the CC BY 4.0 license.
					string year = null;
					var dateDiv = _opdsEntry.SelectSingleNode("/a:feed/a:entry/a:published", _opdsNsmgr) as XmlElement;
					if (dateDiv != null)
					{
						var year0 = dateDiv.InnerText.Trim().Substring(0, 4);
						if (Regex.IsMatch(year0, "[12][90][0-9][0-9]"))
							year = year0;
					}
					if (year == null)
					{
						var updateDiv = _opdsEntry.SelectSingleNode("/a:feed/a:entry/a:updated", _opdsNsmgr) as XmlElement;
						if (updateDiv != null)
						{
							var year0 = updateDiv.InnerText.Trim().Substring(0, 4);
							if (Regex.IsMatch(year0, "[12][90][0-9][0-9]"))
								year = year0;
						}
					}
					if (year == null)
					{
						var year0 = _epubMetaData.Modified.Year.ToString();
						if (Regex.IsMatch(year0, "[12][90][0-9][0-9]"))
							year = year0;
					}
					if (year == null)
						year = DateTime.Now.Year.ToString();
					bookCopyright = String.Format("© Book Dash, {0}", year);
					SetBookCopyright(bookCopyright, "en");
				}
				else
				{
					matches = Regex.Matches(bodyText, "(©[^0-9©\r\t]*)", RegexOptions.CultureInvariant | RegexOptions.Singleline);
					if (matches.Count > 0)
					{
						var match = matches[0];
						var copyrightMatch = match.Groups[1].Value;
						if (!String.IsNullOrEmpty(copyrightMatch))
						{
							var year = _epubMetaData.Modified.Year.ToString();
							if (!Regex.IsMatch(year, "[12][90][0-9][0-9]"))
								year = DateTime.Now.Year.ToString();
							bookCopyright = String.Format("{0}, {1}", copyrightMatch, year);
							SetBookCopyright(bookCopyright, _epubMetaData.LanguageCode);
						}
					}
				}
			}
			var licenseUrl = GetOrCreateDataDivElement("licenseUrl", "*");
			var licenseAbbrev = "";
			if (String.IsNullOrWhiteSpace(licenseUrl.InnerText))
			{
				licenseAbbrev = FindAndProcessCreativeCommonsForBook(bodyText);
				if (String.IsNullOrWhiteSpace(licenseAbbrev))
					Console.WriteLine("WARNING: No license found for book {0}", _bookMetaData.Title);
			}
			if (String.IsNullOrWhiteSpace(artCopyright) && !String.IsNullOrWhiteSpace(bookCopyright))
			{
				// Artwork is presumably the same copyright and license as the text.
				if (!String.IsNullOrWhiteSpace(bookCopyright))
					artCopyright = bookCopyright.Trim();
			}
			if (!String.IsNullOrWhiteSpace(artCopyright))
			{
				// Assume art has the same license as the text.
				var artCreator = "";
				if (_epubMetaData.Illustrators.Count > 0)
					artCreator = String.Join(", ", _epubMetaData.Illustrators).Trim(' ', ',');
				SetAllImageMetadata(artCreator, artCopyright, licenseAbbrev);
				AddIllustratorContributionCredit(artCreator, artCopyright, licenseAbbrev);
			}
		}

		private void AddIllustratorContributionCredit(string artCreator, string artCopyright, string licenseAbbrev)
		{
			var artCopyrightAndLicense = artCopyright;
			if (licenseAbbrev == "CC0")
				artCopyrightAndLicense = "no rights reserved. (public domain)";
			if (licenseAbbrev.StartsWith("CC BY", StringComparison.InvariantCulture))
				artCopyrightAndLicense = $"{artCopyright}. {licenseAbbrev}.";
			if (String.IsNullOrEmpty(artCreator))
				artCopyrightAndLicense = $"Images {artCopyrightAndLicense}";
			else
				artCopyrightAndLicense = $"Images by {artCreator}. {artCopyrightAndLicense}";
			_contributionsXmlBldr.AppendLine($"<p>{artCopyrightAndLicense}</p>");
			//var contributions = GetOrCreateDataDivElement("originalContributions", _creditsLang);
			//if (!String.IsNullOrWhiteSpace(contributions.InnerText))
			//{
			//	_contributionsXmlBldr.Insert(0, Environment.NewLine);
			//	var oldXml = RemoveXmlnsAttribsFromXmlString(contributions.InnerXml);
			//	_contributionsXmlBldr.Insert(0, oldXml);
			//}
			//contributions.InnerXml = _contributionsXmlBldr.ToString().Trim();
			//_contributionsXmlBldr.Clear();
		}

		private string FindAndProcessCreativeCommonsForBook(string bodyText)
		{
			var match = Regex.Match(bodyText, "(http://creativecommons.org/licenses/([a-z-][/0-9.]*)/)", RegexOptions.CultureInvariant);
			if (match.Success)
			{
				var url = match.Groups[1].Value;
				SetDataDivTextValue("licenseUrl", url);
				var license = "CC " + match.Groups[2].Value.ToUpperInvariant().Replace("/", " ").Trim();
				_bookMetaData.License = license.ToLowerInvariant().Replace("cc by", "cc-by");
				return license;
			}
			// regular expressions don't handle non-breaking space very well
			bodyText = bodyText.Replace("\u00a0", " ");
			match = Regex.Match(bodyText, "(Creative\\s+Commons:?\\s+Attribution.*)\n", RegexOptions.CultureInvariant);
			if (!match.Success)
				match = Regex.Match(bodyText, "(Creative\\s+Commons:?\\s+Attribution.*[1-9]\\.[0-9])", RegexOptions.CultureInvariant);
			if (match.Success)
			{
				var licenseText = match.Groups[1].Value;
				string abbrev = GetLicenseAbbrevFromEnglishText(licenseText);
				SetBookLicense(abbrev);
				return abbrev;
			}
			match = Regex.Match(bodyText, "(CC BY(-[A-Z][A-Z])*( [1-9]\\.[0-9])?)");
			if (match.Success)
			{
				var abbrev = match.Groups[1].Value.Trim();
				SetBookLicense(abbrev);
				return abbrev;
			}
			return GetLicenseFromOpdsEntryIfPossible();
		}

		private static string GetLicenseAbbrevFromEnglishText(string licenseText)
		{
			const int CCBY = 0;
			const int CCNC = 1;
			const int CCND = 2;
			const int CCSA = 4;
			string abbrev = "";
			int license = CCBY;
			if (Regex.IsMatch(licenseText, ".*Non\\s*Commercial.*"))
				license += CCNC;
			if (Regex.IsMatch(licenseText, ".*No\\s+Derivatives.*"))
				license += CCND;
			if (Regex.IsMatch(licenseText, ".*Share\\s+Alike.*"))
				license += CCSA;
			switch (license)
			{
				case CCBY:
					abbrev = "CC BY";
					break;
				case CCNC:
					abbrev = "CC BY-NC";
					break;
				case CCND:
					abbrev = "CC BY-ND";
					break;
				case CCNC + CCND:
					abbrev = "CC BY-NC-ND";
					break;
				case CCSA:
					abbrev = "CC BY-SA";
					break;
				case CCNC + CCSA:
					abbrev = "CC BY-NC-SA";
					break;
			}
			// 4.0 and 3.0 are the most likely to be found, but include the others for completeness.
			if (licenseText.Contains("4.0"))
				abbrev = abbrev + " 4.0";
			else if (licenseText.Contains("3.0"))
				abbrev = abbrev + " 3.0";
			else if (licenseText.Contains("2.5"))
				abbrev = abbrev + " 2.5";
			else if (licenseText.Contains("2.0"))
				abbrev = abbrev + " 2.0";
			else if (licenseText.Contains("1.0"))
				abbrev = abbrev + " 1.0";
			return abbrev;
		}

		private string GetLicenseFromOpdsEntryIfPossible()
		{
			if (_opdsEntry == null)
				return "";
			var licenseDiv = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dc:license", _opdsNsmgr) as XmlElement;
			if (licenseDiv == null)
				licenseDiv = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dcterms:license", _opdsNsmgr) as XmlElement;
			if (licenseDiv == null)
				return "";
			var licenseText = licenseDiv.InnerText.Trim();
			if (!licenseText.Contains("Creative Commons Attribution"))
				return "";
			var abbrev = GetLicenseAbbrevFromEnglishText(licenseDiv.InnerText.Trim());
			SetBookLicense(abbrev);
			return abbrev;
		}

		private void ProcessRawPrathamCreditsPage(string bodyText, int pageNumber, string lang)
		{
			string storyAttributionHeader;
			string otherCreditsHeader;
			string illustrationAttribsHeader;
			string disclaimerHeader;
			string matchPageString;
			switch (lang)
			{
				case "fr":
					storyAttributionHeader = kFrenchStoryAttribution;//.Replace("\u00a0", "");
					otherCreditsHeader = kFrenchOtherCredits;//.Replace("\u00a0", "");
					illustrationAttribsHeader = kFrenchIllustrationAttribs;//.Replace("\u00a0", "");
					disclaimerHeader = kFrenchDisclaimer;//.Replace("\u00a0", "");
					matchPageString = kFrenchMatchPageString;
					break;
				default:
					storyAttributionHeader = kStoryAttribution;
					otherCreditsHeader = kOtherCredits;
					illustrationAttribsHeader = kIllustrationAttribs;
					disclaimerHeader = kDisclaimer;
					matchPageString = kMatchPageString;
					break;
			}
			bodyText = bodyText.Replace("\u00a0", "&#xa0;");
			bodyText = Regex.Replace(bodyText, "\\s+", " ");
			bodyText = bodyText.Replace("&#xa0;", "\u00a0");
			bodyText = Regex.Replace(bodyText, " ([,;:!?.])", "$1").Replace(":'", ": '");
			var beginStoryAttrib = bodyText.IndexOf(storyAttributionHeader, StringComparison.InvariantCulture);
			var beginOtherCredits = bodyText.IndexOf(otherCreditsHeader, StringComparison.InvariantCulture);
			var beginIllustration = bodyText.IndexOf(illustrationAttribsHeader, StringComparison.InvariantCulture);
			if (beginIllustration < 0)
				beginIllustration = bodyText.IndexOf(kImagesAttribs, StringComparison.InvariantCulture);
			var beginDisclaimer = bodyText.IndexOf(disclaimerHeader, StringComparison.InvariantCulture);
			var copyright = GetOrCreateDataDivElement("copyright", "*");
			if (String.IsNullOrWhiteSpace(copyright.InnerText) && beginStoryAttrib >= 0)
			{
				var endStoryAttrib = bodyText.Length;
				if (beginOtherCredits > beginStoryAttrib)
					endStoryAttrib = beginOtherCredits;
				else if (beginIllustration > beginStoryAttrib)
					endStoryAttrib = beginIllustration;
				else if (beginDisclaimer > beginStoryAttrib)
					endStoryAttrib = beginDisclaimer;
				var storyAttrib = bodyText.Substring(beginStoryAttrib, endStoryAttrib - beginStoryAttrib).Trim();
				if (storyAttrib.Length > 0)
					ProcessPrathamStoryAttribution(storyAttrib, lang);
			}
			if (beginOtherCredits > 0)
			{
				var endOtherCredits = bodyText.Length;
				if (beginIllustration > 0)
					endOtherCredits = beginIllustration;
				else if (beginDisclaimer > 0)
					endOtherCredits = beginDisclaimer;
				var begin = beginOtherCredits + otherCreditsHeader.Length;
				var otherCreditsText = bodyText.Substring(begin, endOtherCredits - begin).Trim();
				if (otherCreditsText.Length > 0)
					ProcessOtherCredits(otherCreditsText, lang);
			}
			if (beginIllustration > 0)
			{
				var endIllustration = bodyText.Length;
				if (beginDisclaimer > 0)
					endIllustration = beginDisclaimer;
				var illustrationAttributions = bodyText.Substring(beginIllustration, endIllustration - beginIllustration);
				var matches = Regex.Matches(illustrationAttributions, matchPageString, RegexOptions.CultureInvariant);
				for (int i = 0; i < matches.Count; ++i)
				{
					int idxBegin = matches[i].Index;
					int idxEnd = illustrationAttributions.Length;
					if (i < matches.Count - 1)
						idxEnd = matches[i + 1].Index;
					var credit = illustrationAttributions.Substring(idxBegin, idxEnd - idxBegin).Trim();
					var pageText = matches[i].Groups[1].Value;
					ProcessIllustrationAttribution(pageText, credit, lang);
				}
			}
		}

		private void ProcessPrathamStoryAttribution(string storyAttrib, string lang)
		{
			/*
			 * Story Attribution: This story: Too Many Bananas is written by Rohini Nilekani. © Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
			 * Story Attribution: This story: The Generous Crow is translated by Divaspathy Hegde. The © for this translation lies with Pratham Books, 2004. Some rights reserved. Released under CC BY 4.0 license. Based on Original story: ' ಕಾಗೆ ಬಳಗವ ಕರೆಯಿತು ', by Venkatramana Gowda. © Pratham Books, 2004. Some rights reserved. Released under CC BY 4.0 license.
			 * Attribution de l’histoire : Cette histoire, « Les fourmis affairées » est écrite par Kanchan Bannerjee. © Pratham Books, 2015. Certains droits réservés. Publié sous licence CC BY 4.0.
			 * Attribution de l’histoire : Cette histoire, « Voler haut » est traduite par Rohini Nilekani. Le © de cette traduction appartient à Pratham Books, 2004. Certains droits réservés. Publié sous licence CC BY 4.0. Basée sur l’histoire originale : «  तरंगत तरंगत  », de Vidya Tiware. © Pratham Books, 2004. Certains droits réservés. Publié sous licence CC BY 4.0.
			 */
			var success = ExtractInfoFromPrathamStoryAttribution(storyAttrib, lang,
				out string author, out string copyright, out string license, out string originalAttrib);
			if (!success || String.IsNullOrWhiteSpace(copyright) || String.IsNullOrWhiteSpace(license))
			{
				Console.WriteLine("WARNING: Cannot extract copyright and license information in ProcessPrathamStoryAttribution()!");
				Console.WriteLine("Story Attribution=\"{0}\"", storyAttrib);
				return;
			}
			SetBookCopyright(copyright, lang);
			SetBookLicense(license);
			if (!String.IsNullOrWhiteSpace(originalAttrib))
				SetOriginalAcknowledgements(originalAttrib, lang);
		}

		// This method is static internal to facilitate unit testing.
		static internal bool ExtractInfoFromPrathamStoryAttribution(string storyAttrib, string lang,
			out string author, out string copyright, out string license, out string originalAttrib)
		{
			author = null;
			copyright = null;
			license = null;
			originalAttrib = null;
			string matchAuthorCredits;
			string matchTranslatorCredits;
			switch (lang)
			{
				case "fr":
					matchAuthorCredits = @" est écrite par (.*)\. (©[^0-9]*, [12][09][0-9][0-9]).* licence\s(CC\sBY[A-Z0-9-.\s]*)";
					matchTranslatorCredits = @" est traduite par (.*)\. Le © de cette traduction appartient à (.*, [12][09][0-9][0-9]).* licence (CC\sBY[A-Z0-9-.\s]*)\. ([BaséeInspir]+ [surde]+ l’histoire originale\s: .* licence .*\.)";
					break;
				default:
					matchAuthorCredits = @"is written by ?(.*)\. (©[^0-9]*, [12][09][0-9][0-9]).* (CC\sBY[A-Z0-9-.\s]*) license";
					matchTranslatorCredits = @"is translated by ?(.*)\. The © for this translation lies with ([^0-9]*, [12][09][0-9][0-9]).* (CC\sBY[A-Z0-9-.\s]*) license\. ?(Based on Original story: .* license\.)";
					break;
			}
			var match = Regex.Match(storyAttrib, matchAuthorCredits, RegexOptions.CultureInvariant);
			if (!match.Success)
				match = Regex.Match(storyAttrib, matchTranslatorCredits, RegexOptions.CultureInvariant);
			if (match.Success)
			{
				author = match.Groups[1].Value.Trim();
				copyright = match.Groups[2].Value.Trim();
				if (!copyright.StartsWith("© ", StringComparison.InvariantCulture))
					copyright = "© " + copyright;
				license = match.Groups[3].Value.Trim(' ', '.');
				if (license.Contains("\u00a0"))
					license = license.Replace("\u00a0", " ");	// change non-breaking space to plain space inside license
				if (match.Groups.Count > 4)
					originalAttrib = match.Groups[4].Value;
			}
			return match.Success;
		}

		private void ProcessOtherCredits(string otherCredits, string lang)
		{
			var credits = RemovePrathamCreditBoilerplate(otherCredits, lang);
			if (!String.IsNullOrWhiteSpace(credits))
			{
				_supportedByXmlBldr.Append("<p>");
				_supportedByXmlBldr.Append(credits);
				_supportedByXmlBldr.AppendLine("</p>");
			}
		}

		// This method is static internal to facilitate unit testing.
		static internal string RemovePrathamCreditBoilerplate(string otherCreditsText, string lang)
		{
			string[] boilerPlatesEnglish = {
				@"This book [a-z ]+ published on StoryWeaver[,a-z ]+ Pratham Books\.",
				@"^'[^']+' has been published on StoryWeaver by Pratham Books\.",
				@"Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children\.",
			};
			string[] boilerPlatesFrench =
			{
				@"Ce livre a été publié sur StoryWeaver par Pratham Books\.",
				@"^«[^»]+» a été publié sur StoryWeaver par Pratham Books\.",
				@"Pratham Books est une? organis[meation]+ à but non lucratif qui publie des livres dans plusieurs langues indiennes afin de promouvoir la lecture chez les enfants\.",
			};
			var credits = otherCreditsText;
			string[] boilerPlates;
			switch (lang)
			{
				case "fr":	boilerPlates = boilerPlatesFrench;	break;
				case "en":	boilerPlates = boilerPlatesEnglish;	break;
				default:	boilerPlates = new string[0];		break;
			}
			foreach (var match in boilerPlates)
				credits = Regex.Replace(credits, match, "");
			credits = Regex.Replace(credits, @" *www.prathambooks.org *$", "");
			credits = Regex.Replace(credits, @"  +", " ");
			credits = credits.Trim();
			switch (lang)
			{
				case "en":
					if (credits.StartsWith("It ", StringComparison.InvariantCulture))
						credits = "This book" + credits.Substring(2);
					break;
				case "fr":
					break;
			}
			return credits;
		}

		private void ProcessIllustrationAttribution(string pageText, string credit, string lang)
		{
			string byWithSpaces;
			string byWithoutSpaceAfter;
			switch (lang)
			{
				case "fr":
					byWithSpaces = " de ";
					byWithoutSpaceAfter = ", de";
					break;
				default:
					byWithSpaces = " by ";
					byWithoutSpaceAfter = ", by";
					break;
			}
			var creditText = credit.Substring(pageText.Length).Trim();
			var beginCredit = creditText.LastIndexOf(byWithSpaces, StringComparison.InvariantCulture);
			if (beginCredit < 0)
			{
				// Some books omit the space between "by" and the author's name.
				beginCredit = creditText.LastIndexOf(byWithoutSpaceAfter, StringComparison.InvariantCulture);
				if (beginCredit > 0)
					beginCredit += byWithoutSpaceAfter.Length;	// move past the ", by"
			}
			else
			{
				beginCredit += byWithSpaces.Length;	// move past the " by "
			}
			if (beginCredit > 0)
				creditText = creditText.Substring(beginCredit).Trim();
			if (!_creditsAndPages.TryGetValue(creditText, out List<int> pages))
			{
				pages = new List<int>();
				_creditsAndPages.Add(creditText, pages);
			}
			int pageNumber;
			if (pageText.ToLowerInvariant() == "cover page:" || Regex.IsMatch(pageText.ToLowerInvariant(), "page de couverture\u00a0?:"))
				pageNumber = 0;
			else
				pageNumber = GetPageNumber(pageText, lang) - 1;   // Content starts at page 2 for Pratham, but page 1 for Bloom.
			pages.Add(pageNumber);

			var beginDesc = pageText.Length;
			var endDesc = beginDesc + beginCredit - 1;
			string description = "";
			if (beginDesc < endDesc)
				description = credit.Substring(beginDesc, endDesc - beginDesc).Trim();
			SetImageMetaData(pageNumber, description, creditText, lang);
		}

		private int GetPageNumber(string pageText, string lang)
		{
			string matchNumber;
			switch (lang)
			{
				case "fr":
					matchNumber = kFrenchMatchPageNumber;
					break;
				default:
					matchNumber = kMatchPageNumber;
					break;
			}
			var match = Regex.Match(pageText, matchNumber);
			if (match.Success)
			{
				int result;
				if (int.TryParse(match.Groups[1].Value, out result))
					return result;
			}
			return -1;
		}

		public static void ExtractZippedFiles(string zipFile, string unzipFolder)
		{
			Directory.CreateDirectory(unzipFolder);
			var extractPath = unzipFolder + Path.DirectorySeparatorChar;     // safer for unknown zip sources
			using (var zipToOpen = new FileStream(zipFile, FileMode.Open))
			{
				using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
				{
					foreach (var entry in archive.Entries)
					{
						// Gets the full path to ensure that relative segments are removed.
						string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
						// Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
						// are case-insensitive.
						if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
						{
							Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
							var length = (int)entry.Length; // entry.Open() apparently clears this value, at least for Mono
							using (var reader = new BinaryReader(entry.Open()))
							{
								using (var writer = new BinaryWriter(new FileStream(destinationPath, FileMode.Create)))
								{
									var data = reader.ReadBytes(length);
									writer.Write(data);
									writer.Close();
								}
							}
						}
					}
				}
			}
		}

		private void SetAllImageMetadata(string artCreator, string artCopyright, string artLicense)
		{
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: all images metadata: creator={0}; copyright={1}; license={2}", artCreator, artCopyright, artLicense);
			var imgCover = _bloomDoc.SelectSingleNode($"//div[@id='bloomDataDiv']/div[@data-book='coverImage']") as XmlElement;
			if (artCopyright.StartsWith("©", StringComparison.InvariantCulture) && !artCopyright.ToLowerInvariant().Contains("copyright"))
				artCopyright = "Copyright " + artCopyright;
			if (imgCover != null)
			{
				imgCover.SetAttribute("data-copyright", artCopyright);
				imgCover.SetAttribute("data-license", artLicense);
				if (!String.IsNullOrEmpty(artCreator))
					imgCover.SetAttribute("data-creator", artCreator);
				SetMetadataInImageFile(imgCover.InnerText.Trim(), artCreator, artCopyright, artLicense);
			}
			foreach (var img in _bloomDoc.SelectNodes("//div[contains(@class,'bloom-page')]//div[contains(@class,'bloom-imageContainer')]/img[@src]").Cast<XmlElement>().ToList())
			{
				if (!String.IsNullOrWhiteSpace(artCreator))
					img.SetAttribute("data-creator", artCreator);
				img.SetAttribute("data-copyright", artCopyright);
				img.SetAttribute("data-license", artLicense);
				SetMetadataInImageFile(img.GetAttribute("src"), artCreator, artCopyright, artLicense);
			}
		}

		private void SetImageMetaData(int pageNumber, string description, string creditText, string lang)
		{
			string matchCredit = kMatchRawPrathamIllustrationCredit;
			if (lang == "fr")
				matchCredit = kFrenchMatchRawPrathamIllustrationCredit;
			string copyrightPreface = "";
			if (lang == "en")
				copyrightPreface = "Copyright ";
			XmlElement img = null;
			string creator = null;
			string copyright = null;
			string license = null;
			var match = Regex.Match(creditText, matchCredit);
			if (match.Success)
			{
				creator = match.Groups[1].Value.Trim(' ', '.');
				copyright = match.Groups[2].Value.Trim(' ', '.');
				if (copyright.StartsWith("©", StringComparison.InvariantCulture) && !copyright.ToLowerInvariant().Contains("copyright"))
					copyright = copyrightPreface + copyright;
				license = match.Groups[3].Value.Trim();
			}
			if (pageNumber == 0)
			{
				if (!String.IsNullOrWhiteSpace(description))
				{
					var divDesc = GetOrCreateDataDivElement("coverImageDescription", "en");
					divDesc.SetAttribute("class", "bloom-editable");
					divDesc.SetAttribute("contenteditable", "true");
					divDesc.InnerXml = $"<p>{description}</p>";
				}
				var div = GetOrCreateDataDivElement("coverImage", "*");
				if (!String.IsNullOrWhiteSpace(description))
					div.SetAttribute("alt", description);
				if (!String.IsNullOrWhiteSpace(creator))
					div.SetAttribute("data-creator", creator);
				if (!String.IsNullOrWhiteSpace(copyright))
					div.SetAttribute("data-copyright", copyright);
				if (!String.IsNullOrWhiteSpace(license))
					div.SetAttribute("data-license", license);
				img = _bloomDoc.SelectSingleNode($"//div[@data-xmatter-page='frontCover']//div[contains(@class,'bloom-imageContainer')]/img[@src]") as XmlElement;
			}
			else
			{
				img = _bloomDoc.SelectSingleNode($"//div[@data-page-number='{pageNumber}']//div[contains(@class,'bloom-imageContainer')]/img[@src]") as XmlElement;
			}
			if (img != null)
			{
				if (!String.IsNullOrWhiteSpace(description))
					img.SetAttribute("alt", description);
				if (!String.IsNullOrWhiteSpace(creator))
					img.SetAttribute("data-creator", creator);
				if (!String.IsNullOrWhiteSpace(copyright))
					img.SetAttribute("data-copyright", copyright);
				if (!String.IsNullOrWhiteSpace(license))
					img.SetAttribute("data-license", license);
				// Set internal image description values as well as the alt attribute?
				// Set copyright/license information inside the image files.
				SetMetadataInImageFile(img.GetAttribute("src"), creator, copyright, license);
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: page {0} image metadata: creator={1}; copyright={2}; license={3}", pageNumber == 0 ? "Front Cover" : pageNumber.ToString(), creator, copyright, license);
			}
			else
			{
				Console.WriteLine("WARNING: Could not find expected image on page {0}", pageNumber == 0 ? "Front Cover" : pageNumber.ToString());
			}
		}

		private void SetMetadataInImageFile(string filename, string creator, string copyright, string licenseAbbrev)
		{
			if (String.IsNullOrEmpty(_bookFolder))
				return;		// This can happen in tests.
			var path = Path.Combine(_bookFolder, filename);
			if (File.Exists(path))
			{
				if (!String.IsNullOrEmpty(licenseAbbrev))
					licenseAbbrev = Regex.Replace(licenseAbbrev, @"\s", " ");	// ensure actual space characters, not non-breaking space or the like
				var metadata = ImageUtility.GetImageMetadata(path);
				if (!String.IsNullOrWhiteSpace(creator) && String.IsNullOrWhiteSpace(metadata.Creator))
					metadata.Creator = creator;
				if (!String.IsNullOrWhiteSpace(copyright) && String.IsNullOrWhiteSpace(metadata.CopyrightNotice))
					metadata.CopyrightNotice = copyright;
				if (!String.IsNullOrWhiteSpace(licenseAbbrev) && licenseAbbrev.StartsWith("CC BY", StringComparison.InvariantCulture) &&
					(metadata.License is NullLicense || metadata.License == null))
				{
					// parse license to set right values here.
					var commercialUseOK = !licenseAbbrev.Contains("NC");
					var derivativeRule = CreativeCommonsLicense.DerivativeRules.Derivatives;
					if (licenseAbbrev.Contains("SA"))
						derivativeRule = CreativeCommonsLicense.DerivativeRules.DerivativesWithShareAndShareAlike;
					else if (licenseAbbrev.Contains("ND"))
						derivativeRule = CreativeCommonsLicense.DerivativeRules.NoDerivatives;
					var version = "";
					if (licenseAbbrev.Contains("4.0"))
						version = "4.0";
					else if (licenseAbbrev.Contains("3.0"))
						version = "3.0";
					else if (licenseAbbrev.Contains("2.5"))
						version = "2.5";
					else if (licenseAbbrev.Contains("2.0"))
						version = "2.0";
					else if (licenseAbbrev.Contains("1.0"))
						version = "1.0";
					metadata.License = new CreativeCommonsLicense(true, commercialUseOK, derivativeRule, version);
				}
				// Setting "collection" information should prevent Bloom users from changing the copyright.
				SetImageCollectionMetadata(metadata);
				ImageUtility.SetImageMetadata(path, metadata);
			}
		}

		private void SetImageCollectionMetadata(Metadata metadata)
		{
			metadata.CollectionName = $"{_publisher} / {_epubMetaData.Title}";
			// An alternative for the CollectionUri would be the link to the epub from the opds catalog entry.
			switch (_publisher.ToLowerInvariant())
			{
				case "3asafeer":	// not in StoryWeaver
					metadata.CollectionUri = "https://3asafeer.com/";
					break;
				case "african storybook initiative":
				case "african storybook project":
					metadata.CollectionUri = "https://www.africanstorybook.org/";
					break;
				case "biblionef":   // not in StoryWeaver
					metadata.CollectionUri = "http://www.biblionefsa.org.za/";
					break;
				case "bookbox": // not in StoryWeaver
					metadata.CollectionUri = "https://bookbox.com/";
					break;
				case "book dash":
					metadata.CollectionUri = "https://bookdash.org/";
					break;
				case "pratham books":
					metadata.CollectionUri = "https://prathambooks.org/";
					break;
				case "room to read":
					metadata.CollectionUri = "https://www.roomtoread.org/";
					break;
				case "seru setiap saat":    // not in StoryWeaver
					metadata.CollectionUri = "https://serusetiapsaat.com/";
					break;
				case "the asia foundation":
					metadata.CollectionUri = "https://asiafoundation.org/";
					break;
				case "usaid":	// not in StoryWeaver
					metadata.CollectionUri = "https://www.usaid.gov/";
					break;
				// I don't know how to interpret these publisher codes from GDL.
				case "canvas":	// not in StoryWeaver
				case "ew nigeria":		// not in StoryWeaver
					metadata.CollectionUri = "https://unknown url/";
					break;
/*  using StoryWeaver as a source would add these additional publishers:
	AfLIA
	Azad India Foundation
	CGnet Swara
	Darakht-e Danesh Library
	Dastkari Haat Samiti
	Jala
	Konkani Bhasha Mandal
	Little Readers' Nook
	Manjushri Educational Services
	Ms Moochie
	North East Educational Trust
	REHMA
	Right To Play
	Sns Foundation
	StoryWeaver
	Sub-Saharan Publishers
	Suchana Uttorchandipur Community Society
	The District Administration, Ranchi District
	The Rosetta Foundation
	Uganda Christian University
	Unnati ISEC
	World Konkani Centre
 */
			}
		}

		private void CreateThumbnails()
		{
			ReplaceCoverImageIfNeeded();
			var imageFile = GetCoverImagePath();
			if (String.IsNullOrWhiteSpace(imageFile) || !File.Exists(imageFile))
				return;
			if (_options.Verbose)
				Console.WriteLine("INFO: creating thumbnail images from {0}", imageFile);
			using (var util = new ImageUtility(imageFile))
			{
				util.CreateThumbnail(70, Path.Combine(_bookFolder, "thumbnail-70.png"));
				File.Copy(Path.Combine(_bookFolder, "thumbnail-70.png"), Path.Combine(_bookFolder, "thumbnail.png"), true);
				util.CreateThumbnail(256, Path.Combine(_bookFolder, "thumbnail-256.png"));
				util.CreateThumbnail(200, Path.Combine(_bookFolder, "coverImage200.jpg"));
			}
			if (File.Exists(Path.Combine(_bookFolder, "thumbnail.jpg")))
				File.Delete(Path.Combine(_bookFolder, "thumbnail.jpg"));
		}

		private string GetCoverImagePath()
		{
			var coverImageDiv = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage' and @lang='*']") as XmlElement;
			if (coverImageDiv != null)
				return Path.Combine(_bookFolder, coverImageDiv.InnerText.Trim());
			return "";
		}

		/// <summary>
		/// Some publishers use only an image for the front cover with the title and author embedded in the image.
		/// This is obviously bad for translated books, so try using the image from the first content page instead.
		/// </summary>
		internal void ReplaceCoverImageIfNeeded()
		{
			if (String.IsNullOrEmpty(_publisher))
			{
				Console.WriteLine("WARNING: Unknown publisher");
				return;
			}
			if (_publisher.ToLowerInvariant().StartsWith("african storybook", StringComparison.InvariantCulture))
			{
				var firstPageImageDiv = _bloomDoc.SelectSingleNode("/html/body/div[@data-page-number='1']//div[contains(@class,'bloom-imageContainer')]/img") as XmlElement;
				var coverImageDiv = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage' and @lang='*']") as XmlElement;
				if (firstPageImageDiv != null && coverImageDiv != null)
				{
					// Replace the cover image with the first page image since we're using it as the thumbnail.
					var src = firstPageImageDiv.GetAttribute("src");
					coverImageDiv.InnerText = src;
					coverImageDiv.SetAttribute("data-creator", firstPageImageDiv.GetAttribute("data-creator"));
					coverImageDiv.SetAttribute("data-copyright", firstPageImageDiv.GetAttribute("data-copyright"));
					coverImageDiv.SetAttribute("data-license", firstPageImageDiv.GetAttribute("data-license"));
				}
			}
		}
	}
}
