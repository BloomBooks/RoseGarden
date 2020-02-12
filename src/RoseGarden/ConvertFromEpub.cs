// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RoseGarden
{
	public class ConvertFromEpub
	{
		ConvertOptions _options;

		EpubMetadata _epubMetaData;
		string _epubFolder;
		string _epubFile;
		string _bookFolder;
		string _htmFileName;
		XmlDocument _bloomDoc;
		XmlDocument _templateBook;  // provides templates for new front cover and content pages.
		List<XmlElement> _templatePages;
		XmlDocument _opdsEntry;     // from catalog entry file (if it exists)
		XmlNamespaceManager _opdsNsmgr;
		private readonly LanguageData _languageData = new LanguageData();
		private BookMetaData _bookMetaData;
		string _publisher;

		private int _endCreditsStart = Int32.MaxValue;  // Assume no end credits pages to begin with.
		private int _endCreditsPageCount = 0;
		StringBuilder _contributionsXmlBldr = new StringBuilder();
		Dictionary<string, List<int>> _creditsAndPages;

		// Files that are copied into a new Basic Book.
		readonly private string[] _copiedFiles = new string[]
		{
			"browser/bookLayout/basePage.css",
			"browser/bookLayout/langVisibility.css",
			"browser/bookPreview/previewMode.css",
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
				InitializeData();

				_bloomDoc = new XmlDocument();
				_bloomDoc.PreserveWhitespace = true;
				_bloomDoc.Load(Path.Combine(_bookFolder, _htmFileName));

				_templateBook = new XmlDocument();
				_templateBook.PreserveWhitespace = true;
				var location = Assembly.GetExecutingAssembly().Location;
				var pagesFile = Path.Combine(Path.GetDirectoryName(location), "Resources", "Pages.xml");
				_templateBook.Load(pagesFile);
				_templatePages = _templateBook.SelectNodes("//div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
				if (_options.UseLandscape)
					ChangePagesToLandscape();

				ConvertBook();

				File.Delete(Path.Combine(_bookFolder, _htmFileName));
				_bloomDoc.Save(Path.Combine(_bookFolder, _htmFileName));
				CreateThumbnails();
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

		private void ChangePagesToLandscape()
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

		private bool VerifyOptions()
		{
			var allValid = true;
			if (_options.UseLandscape && _options.UsePortrait)
			{
				Console.WriteLine("--portrait and --landscape cannot be used together.  --portrait is the default behavior for most books.  --landscape may become the default for some publishers.");
				allValid = false;
			}
			return allValid;
		}

		private void CopyBloomBookToOutputFolder()
		{
			var newBookFolder = Path.Combine(_options.CollectionFolder, Path.GetFileNameWithoutExtension(_htmFileName));
			if (Directory.Exists(newBookFolder))
			{
				if (!_options.ReplaceExistingBook)
				{
					Console.WriteLine("WARNING: {0} already exists.", newBookFolder);
					Console.WriteLine("Use -F (--force) if you want to overwrite it.");
					return;
				}
				// Maintain the book id that was set before.
				var oldmeta = BookMetaData.FromFolder(newBookFolder);
				if (!String.IsNullOrWhiteSpace(oldmeta.Id) && oldmeta.Id != _bookMetaData.BookLineage)
				{
					if (_options.Verbose)
						Console.WriteLine("INFO: preserving book id {0} for {1}", oldmeta.Id, _bookMetaData.Title);
					_bookMetaData.Id = oldmeta.Id;
				}
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: deleting directory {0}", newBookFolder);
				Directory.Delete(newBookFolder, true);
			}
			_bookMetaData.WriteToFolder(_bookFolder);
			CopyDirectory(_bookFolder, newBookFolder);
			EnsureBloomCollectionFile();
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
			var langCode = _languageData.GetCodeForName(_options.LanguageName);
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
				else
				{
					Console.WriteLine("WARNING: language code '{0}' for {1} does not match expected '{2}'.", _epubMetaData.LanguageCode, _options.LanguageName, langCode);
				}
			}
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
				else if (filepath.EndsWith(".txt", StringComparison.InvariantCulture) && filepath.Contains("StoryWeaverAttribution") && String.IsNullOrWhiteSpace(_options.AttributionFile))
					_options.AttributionFile = filepath;
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
				File.Copy(imageFile, destPath);
			}
			// Find related files that may have been downloaded or created for this book.
			var pathPDF = Path.ChangeExtension(_options.EpubFile, "pdf");
			var pathThumb = Path.ChangeExtension(_options.EpubFile, "jpg");
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
				File.Copy(pathOPDS, Path.Combine(_bookFolder, Path.GetFileName(pathOPDS).Replace(".opds",".original.opds")));
				// load the OPDS catalog information
				_opdsEntry = new XmlDocument();
				_opdsEntry.PreserveWhitespace = true;
				_opdsEntry.Load(pathOPDS);
				_opdsNsmgr = new XmlNamespaceManager(_opdsEntry.NameTable);
				_opdsNsmgr.AddNamespace("lrmi", "http://purl.org/dcx/lrmi-terms/");
				_opdsNsmgr.AddNamespace("opds", "http://opds-spec.org/2010/catalog");
				_opdsNsmgr.AddNamespace("dc", "http://purl.org/dc/terms/");
				_opdsNsmgr.AddNamespace("dcterms", "http://purl.org/dc/terms/");
				_opdsNsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");

				var divPublisher = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dc:publisher", _opdsNsmgr) as XmlElement;
				if (divPublisher == null)
					divPublisher = _opdsEntry.SelectSingleNode("/a:feed/a:entry/dcterms:publisher", _opdsNsmgr) as XmlElement;
				if (divPublisher != null)
					_publisher = divPublisher.InnerText.Trim();
			}

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

			for (int pageNumber = 0; pageNumber < _epubMetaData.PageFiles.Count; ++pageNumber)
			{
				ConvertPage(pageNumber, _epubMetaData.PageFiles[pageNumber]);
			}
			if (_endCreditsPageCount > 3 || _endCreditsPageCount == 0)
				Console.WriteLine("WARNING: found {0} end credit pages in the book", _endCreditsPageCount);

			if (!String.IsNullOrWhiteSpace(_options.AttributionFile) && File.Exists(_options.AttributionFile))
			{
				const string AttributionTextHeader = "Attribution Text:";
				var attributionText = File.ReadAllText(_options.AttributionFile);
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
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: processed {0} pages from {1} ({2} pages of end credits)", _epubMetaData.PageFiles.Count, Path.GetFileName(_options.EpubFile), _endCreditsPageCount);
			FillInBookMetaData();
		}

		private void AdjustLayoutIfNeeded()
		{
			if (_options.UsePortrait || _options.UseLandscape)
				return;     // user specifically demanded a particular layout
			// Africa Storybook Project books should be landscape by default instead of portrait.
			if (_publisher != null && _publisher.ToLowerInvariant().StartsWith("african storybook", StringComparison.InvariantCulture))
			{
				if (_options.Verbose)
					Console.WriteLine("INFO: setting book layout to landscape for {0}", _epubMetaData.Title);
				_options.UseLandscape = true;
				ChangePagesToLandscape();
			}
		}

		private void ExtractCopyrightAndLicenseFromAttributionText(string attributionText)
		{
			var copyright = Regex.Match(attributionText, "\\((©.*, [12][09][0-9][0-9])\\)", RegexOptions.CultureInvariant);
			if (copyright.Success)
			{
				SetBookCopyright(copyright.Groups[1].Value);
			}
			var license = Regex.Match(attributionText, "under a (CC.*) license", RegexOptions.CultureInvariant);
			if (license.Success)
			{
				SetBookLicense(license.Groups[1].Value);
			}
		}

		private void SetBookLicense(string licenseAbbreviation)
		{
			var url = "";
			switch (licenseAbbreviation)
			{
				case "CC BY":
				case "CC BY 4.0":
					url = "http://creativecommons.org/licenses/by/4.0/";
					break;
				case "CC BY-SA":
				case "CC BY-SA 4.0":
					url = "http://creativecommons.org/licenses/by-sa/4.0/";
					break;
				case "CC BY-ND":
				case "CC BY-ND 4.0":
					url = "http://creativecommons.org/licenses/by-nd/4.0/";
					break;
				case "CC BY-NC":
				case "CC BY-NC 4.0":
					url = "http://creativecommons.org/licenses/by-nc/4.0/";
					break;
				case "CC BY-NC-SA":
				case "CC BY-NC-SA 4.0":
					url = "https://creativecommons.org/licenses/by-nc-sa/4.0/";
					break;
				case "CC BY-NC-ND":
				case "CC BY-NC-ND 4.0":
					url = "http://creativecommons.org/licenses/by-nc-nd/4.0/";
					break;
				case "CC0":
					url = "https://creativecommons.org/share-your-work/public-domain/cc0/";
					break;
				default:
					Console.WriteLine("WARNING: cannot decipher license abbreviation \"{0}\"", licenseAbbreviation);
					break;
			}
			if (!String.IsNullOrEmpty(url))
			{
				SetDataDivTextValue("copyrightUrl", url);
				_bookMetaData.License = licenseAbbreviation.ToLowerInvariant().Replace("cc by", "cc-by");
			}
		}

		private void SetBookCopyright(string matchedCopyright)
		{
			var text = "Copyright " + matchedCopyright;
			_bookMetaData.Copyright = text;
			SetDataDivTextValue("copyright", text);
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

		private void ConvertPage(int pageNumber, string pageFilePath)
		{
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: converting {0}", pageFilePath);
			if (pageNumber == 0)
			{
				ConvertFrontCoverPage(pageFilePath);
			}
			else if (!ConvertContentPage(pageFilePath, pageNumber))
			{
				Console.WriteLine("WARNING: {0} did not convert successfully.", pageFilePath);
			}
		}

		private void ConvertFrontCoverPage(string pageFilePath)
		{
			// The cover page created here will be overwritten by Bloom when it applies the user's chosen xmatter.
			// The important result is filling in values in the data div.
			AddEmptyCoverPage();
			var pageDoc = new XmlDocument();
			pageDoc.Load(pageFilePath);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr);
			bool titleSet = false;
			bool authorEtcSet = false;
			int imageCount = 0;
			foreach (var child in body.SelectNodes("//x:img|//x:p[normalize-space()!='']", nsmgr).Cast<XmlElement>())
			{
				if (child.Name == "img")
				{
					++imageCount;
					var imageFile = child.GetAttribute("src");
					// cover image always comes first
					if (imageCount == 1)
						SetCoverImage(imageFile);
					else
						AddExtraCoverImage(imageFile, imageCount);
				}
				else
				{
					Debug.Assert(child.Name == "p");
					if (!titleSet)
					{
						var title = child.InnerText.Trim();
						if (title != _epubMetaData.Title)
						{
							Console.WriteLine("WARNING: using title from ePUB metadata ({0}) instead of data from title page({1})", _epubMetaData.Title, title);
							SetTitle(_epubMetaData.Title);
							titleSet = true;
							AddCoverContributor(child.OuterXml);
							// Don't claim to have set the author etc.  We don't know what we have here!
						}
						else
						{
							SetTitle(title);
							titleSet = true;
						}
					}
					else
					{
						AddCoverContributor(child.OuterXml);
						authorEtcSet = true;
					}
				}
			}
			if (!titleSet)
				SetTitle(_epubMetaData.Title);
			if (!authorEtcSet)
			{
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

		private void AddNewLanguageDiv(XmlElement zTemplateDiv, string content)
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

		private bool ConvertContentPage(string pageFilePath, int pageNumber)
		{
			var pageDoc = new XmlDocument();
			pageDoc.Load(pageFilePath);
			var nsmgr = new XmlNamespaceManager(pageDoc.NameTable);
			nsmgr.AddNamespace("x", "http://www.w3.org/1999/xhtml");
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr) as XmlElement;
			if (IsEndCreditsPage(body, pageNumber))
				return ConvertEndCreditsPage(body, nsmgr, pageNumber);

			var elements = body.SelectNodes(".//x:img[normalize-space(@src)!='']|//x:p[normalize-space()!='']", nsmgr).Cast<XmlElement>().ToList();
			var imageCount = 0;
			var textCount = 0;
			var firstChild = "";
			var lastChild = "";
			var prevChild = "";
			// Summarize the page content to find an appropriate template page.
			foreach (var child in elements)
			{
				if (String.IsNullOrEmpty(firstChild))
					firstChild = child.Name;
				lastChild = child.Name;
				if (child.Name == "img")
				{
					++imageCount;   // each image counts separately.
				}
				else
				{
					Debug.Assert(child.Name == "p");
					if (prevChild != "p")   // paragraphs clump together when contiguous.
						++textCount;
				}
				prevChild = child.Name;
			}
			var rawNodes = new List<XmlNode>();
			if (textCount == 0 && !String.IsNullOrWhiteSpace(body.InnerText))
			{
				// Some Global Digital Library epubs have pages with text directly in the HTML body with no div or p elements.
				// Such pages may still have img (and br) elements.
				firstChild = lastChild = "";
				imageCount = 0;
				foreach (var node in body.ChildNodes.Cast<XmlNode>())
				{
					if (String.IsNullOrEmpty(firstChild))
						firstChild = node.Name;
					if (node.NodeType == XmlNodeType.Element && node.Name == "img")
					{
						++imageCount;
						rawNodes.Add(node);
					}
					else if (node.NodeType == XmlNodeType.Text && !String.IsNullOrWhiteSpace(node.InnerText))
					{
						++textCount;
						rawNodes.Add(node);
					}
					lastChild = node.Name;
				}
			}
			//if (elements.Count == 0 && body.ChildNodes.Count == 1 && !body.FirstChild.HasChildNodes && !String.IsNullOrWhiteSpace(body.FirstChild.InnerText))
			//{
			//	textCount = 1;
			//	firstChild = lastChild = "p";
			//}
			var templatePage = SelectTemplatePage(imageCount, textCount, firstChild, lastChild);
			if (templatePage == null)
			{
				Console.WriteLine("ERROR: cannot retrieve template page for {0} images and {1} text fields", imageCount, textCount);
				return false;
			}
			var newPageDiv = _bloomDoc.CreateElement("div");
			foreach (XmlAttribute attr in templatePage.Attributes.Cast<XmlAttribute>())
				newPageDiv.SetAttribute(attr.Name, attr.Value);
			newPageDiv.SetAttribute("id", Guid.NewGuid().ToString());
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
			var textIdx = 0;
			prevChild = "";
			var innerXmlBldr = new StringBuilder();
			var images = newPageDiv.SelectNodes(".//img").Cast<XmlElement>().ToList();
			var textGroupDivs = newPageDiv.SelectNodes(".//div[contains(@class,'bloom-translationGroup')]").Cast<XmlElement>().ToList();
			if (rawNodes.Count > 0)
			{
				foreach (var node in rawNodes)
				{
					if (node.Name == "img")
					{
						StoreImage(imageIdx, images, node as XmlElement);
						++imageIdx;
					}
					else
					{
						innerXmlBldr.Append("<p>");
						innerXmlBldr.Append(node.InnerText);
						innerXmlBldr.Append("</p>");
						StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);
						++textIdx;
					}
				}
			}
			else
			{
				foreach (var child in elements)
				{
					if (child.Name == "img")
					{
						StoreImage(imageIdx, images, child);
						++imageIdx;
					}
					else
					{
						if (innerXmlBldr.Length > 0 && prevChild != "p")
						{
							StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);
							++textIdx;
						}
						if (child.InnerText.Trim().Length > 0)
						{
							var outer = child.OuterXml;
							var xml = RemoveXmlnsAttribsFromXmlString(outer);
							innerXmlBldr.Append(xml);
						}
					}
					prevChild = child.Name;
				}
				if (innerXmlBldr.Length > 0)
					StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);
			}
			return true;
		}

		private void StoreImage(int imageIdx, List<XmlElement> images, XmlElement img)
		{
			var src = img.GetAttribute("src");
			if (imageIdx < images.Count)
			{
				images[imageIdx].SetAttribute("src", src);
				var alt = img.GetAttribute("alt");
				if (String.IsNullOrWhiteSpace(alt))
					images[imageIdx].SetAttribute("alt", alt);
				else
					images[imageIdx].SetAttribute("alt", src);
			}
			else
			{
				Console.WriteLine("WARNING: no place on page to show image file {0}", src);
			}
		}

		private void StoreAccumulatedParagraphs(int textIdx, StringBuilder innerXmlBldr, List<XmlElement> textGroupDivs)
		{
			Debug.Assert(innerXmlBldr != null && innerXmlBldr.Length > 0);
			Debug.Assert(textGroupDivs != null && textGroupDivs.Count > 0);
			if (textIdx < textGroupDivs.Count)
			{
				var zTemplateDiv = textGroupDivs[textIdx].SelectSingleNode("./div[contains(@class, 'bloom-editable') and @lang='z' and @contenteditable='true']") as XmlElement;
				// Add new div with accumulated paragraphs
				AddNewLanguageDiv(zTemplateDiv, innerXmlBldr.ToString());
			}
			else
			{
				// Cram new accumulation into last text group.
				var groupDiv = textGroupDivs[textGroupDivs.Count - 1];
				var div = groupDiv.SelectSingleNode($"./div[@lang='{_epubMetaData.LanguageCode}']");
				if (div != null)
				{
					var inner = div.InnerXml;
					var xml = RemoveXmlnsAttribsFromXmlString(inner);
					innerXmlBldr.Insert(0, xml);
					div.InnerXml = innerXmlBldr.ToString();
				}
				else
				{
					Debug.Assert(div != null);
				}
			}
			innerXmlBldr.Clear();
		}

		private XmlElement SelectTemplatePage(int imageCount, int textCount, string firstChild, string lastChild)
		{
			if (imageCount == 0)
			{
				return SelectTemplatePage("Just Text");
			}
			if (imageCount == 1)
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
						Debug.Assert(firstChild == "p" && lastChild == "p");
						return SelectTemplatePage("Picture in Middle");
				}
			}
			else
			{
				// We can't handle 2 or more images on the page automatically at this point.
				if (textCount == 0)
					return SelectTemplatePage("Just a Picture");
				else if (textCount == 1)
					return SelectTemplatePage("Basic Text & Picture");
				else
					return SelectTemplatePage("Picture in Middle");
			}
			return null;
		}

		private XmlElement SelectTemplatePage(string id)
		{
			if (_options.UseLandscape && id == "Basic Text & Picture")
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
				return true;	// StoryWeaver output apparently...
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

		public string RemoveXmlnsAttribsFromXmlString(string xml)
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
					// The inside back cover is better because it usually allows more space than the outside back cover, which
					// may have some branding taking up space.  It also isn't enforcing centering of lines, which distorts the
					// original appearance.
					var insideBackCoverDiv = GetOrCreateDataDivElement("insideBackCover", "en");
					var backCoverXml = RemoveXmlnsAttribsFromXmlString(body.InnerXml);
					insideBackCoverDiv.InnerXml = backCoverXml;
					if (NeedCopyrightInformation())
					{
						ProcessRawCreditsPageForCopyrights(body, pageNumber);
					}
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

		private void WriteAccumulatedImageAndOtherCredits()
		{
			if (_creditsAndPages.Count > 0 || _contributionsXmlBldr.Length > 0)
			{
				var contributions = GetOrCreateDataDivElement("originalContributions", "en");
				if (!String.IsNullOrWhiteSpace(contributions.InnerText))
				{
					_contributionsXmlBldr.Insert(0, Environment.NewLine);
					var oldXml = RemoveXmlnsAttribsFromXmlString(contributions.InnerXml);
					_contributionsXmlBldr.Insert(0, oldXml);
				}
				if (_creditsAndPages.Count == 1)
				{
					_contributionsXmlBldr.AppendLine($"All images {_creditsAndPages.Keys.First()}");
				}
				else if (_creditsAndPages.Count > 1)
				{
					foreach (var credit in _creditsAndPages.Keys)
					{
						var pagesText = ConvertIntListToPageString(_creditsAndPages[credit]);
						_contributionsXmlBldr.AppendLine($"<p>{pagesText}{credit}</p>");
					}
				}
				contributions.InnerXml = _contributionsXmlBldr.ToString();
			}
		}

		private string ConvertIntListToPageString(List<int> pageList)
		{
			// Images on pages Front Cover, 1–2 by 
			// Image on page 4, 7 by 
			if (pageList.Count == 1)
			{
				if (pageList[0] == 0)
					return "Image on Front Cover by ";
				else
					return $"Image on page {pageList[0]} by ";
			}
			var bldr = new StringBuilder();
			int firstPageIndex = 0;
			if (pageList[0] == 0)
			{
				bldr.Append("Images on Front Cover, ");
				if (pageList.Count == 2)
					bldr.Append("page ");
				else
					bldr.Append("pages ");
				++firstPageIndex;
			}
			else
			{
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
			bldr.Append(" by ");
			return bldr.ToString();
		}

		private bool NeedCopyrightInformation()
		{
			var copyright = GetOrCreateDataDivElement("copyright", "*");
			return String.IsNullOrWhiteSpace(copyright.InnerText);
		}

		private bool NeedLicenseInformation()
		{
			var license = GetOrCreateDataDivElement("copyrightUrl", "*");
			return String.IsNullOrWhiteSpace(license.InnerText);
		}

		private void ProcessRawCreditsPageForCopyrights(XmlElement body, int pageNumber)
		{
			var bodyText = body.InnerText;
			if (bodyText.Contains("Pratham Books") && bodyText.Contains("©") && (bodyText.Contains(kStoryAttribution) || bodyText.Contains(kIllustrationAttribs)))
			{
				ProcessRawPrathamCreditsPage(bodyText, pageNumber);
			}
			var artCopyright = "";
			var copyright = GetOrCreateDataDivElement("copyright", "*");
			if (String.IsNullOrWhiteSpace(copyright.InnerText))
			{
				var match = Regex.Match(bodyText, "(©[^0-9]* ([12][09][0-9][0-9]))", RegexOptions.CultureInvariant);
				if (match.Success)
				{
					var copyrightMatch = match.Groups[1].Value;
					if (copyrightMatch.StartsWith("© Text:", StringComparison.InvariantCulture) && copyrightMatch.Contains("Artwork:"))
					{
						var beginArtwork = copyrightMatch.IndexOf("Artwork:", StringComparison.InvariantCulture);
						var bookCopyright = "© " + copyrightMatch.Substring(7, beginArtwork - 7).Trim() + " " + match.Groups[2].Value;
						SetBookCopyright(bookCopyright);
						artCopyright = $"Artwork © { copyrightMatch.Substring(beginArtwork + 8).Trim()}";
					}
					else
					{
						SetBookCopyright(copyrightMatch);
					}
				}
				else
				{
					// Book Dash books are shy about admitting it, but they're effectively copyright by Book Dash
					// since they're all released under the CC BY 4.0 license.
					if (_publisher != null && _publisher.ToLowerInvariant() == "book dash")
					{
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
						SetBookCopyright(String.Format("Copyright © by Book Dash, {0}", year));
					}
				}
			}
			var copyrightUrl = GetOrCreateDataDivElement("copyrightUrl", "*");
			var licenseAbbrev = "";
			if (String.IsNullOrWhiteSpace(copyrightUrl.InnerText))
			{
				licenseAbbrev = FindAndProcessCreativeCommonsForBook(bodyText);
			}
			if (!String.IsNullOrWhiteSpace(artCopyright))
			{
				// Assume art has the same license as the text.
				var artCopyrightAndLicense = artCopyright;
				if (licenseAbbrev == "CC0")
					artCopyrightAndLicense = "Artwork: no rights reserved. (public domain)";
				if (licenseAbbrev.StartsWith("CC BY", StringComparison.InvariantCulture))
					artCopyrightAndLicense = $"{artCopyright}.  Some rights reserved.  Released under the {licenseAbbrev} license.";
				_contributionsXmlBldr.AppendLine(artCopyrightAndLicense);
				SetAllImageCopyrights(artCopyright, licenseAbbrev);
				var contributions = GetOrCreateDataDivElement("originalContributions", "en");
				if (!String.IsNullOrWhiteSpace(contributions.InnerText))
				{
					_contributionsXmlBldr.Insert(0, Environment.NewLine);
					var oldXml = RemoveXmlnsAttribsFromXmlString(contributions.InnerXml);
					_contributionsXmlBldr.Insert(0, oldXml);
				}
				contributions.InnerXml = _contributionsXmlBldr.ToString();
				_contributionsXmlBldr.Clear();
			}
		}

		private string FindAndProcessCreativeCommonsForBook(string bodyText)
		{
			var match = Regex.Match(bodyText, "(http://creativecommons.org/licenses/([a-z-][/0-9.]*)/)", RegexOptions.CultureInvariant);
			if (match.Success)
			{
				var url = match.Groups[1].Value;
				SetDataDivTextValue("copyrightUrl", url);
				var license = "CC " + match.Groups[2].Value.ToUpperInvariant().Replace("/", " ").Trim();
				_bookMetaData.License = license.ToLowerInvariant().Replace("cc by", "cc-by");
				return license;
			}
			// regular expressions don't handle non-breaking space very well
			bodyText = bodyText.Replace("\u00a0", " ");
			match = Regex.Match(bodyText, "(Creative\\s+Commons:?\\s+Attribution.*)\n", RegexOptions.CultureInvariant);
			if (!match.Success)
				match = Regex.Match(bodyText, "(Creative\\s+Commons:?\\s+Attribution.*4\\.0)", RegexOptions.CultureInvariant);
			if (match.Success)
			{
				var licenseText = match.Groups[1].Value;
				string abbrev = GetLicenseAbbrevFromEnglishText(licenseText);
				SetBookLicense(abbrev);
				return abbrev;
			}
			match = Regex.Match(bodyText, "(CC BY(-[A-Z][A-Z])*( 4.0)?)");
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
			if (licenseText.Contains("4.0"))
				abbrev = abbrev + " 4.0";
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

		const string kStoryAttribution = "Story Attribution:";
		const string kOtherCredits = "Other Credits:";
		const string kIllustrationAttribs = "Illustration Attributions:";
		const string kDisclaimer = "Disclaimer:";

		private void ProcessRawPrathamCreditsPage(string bodyText, int pageNumber)
		{
			bodyText = Regex.Replace(bodyText, "\\s+", " ", RegexOptions.Singleline);
			bodyText = bodyText.Replace(" ,",",").Replace(" .",".").Replace(" '","'").Replace("' ","'").Replace(":'",": '");
			var beginStoryAttrib = bodyText.IndexOf(kStoryAttribution, StringComparison.InvariantCulture);
			var beginOtherCredits = bodyText.IndexOf(kOtherCredits, StringComparison.InvariantCulture);
			var beginIllustration = bodyText.IndexOf(kIllustrationAttribs, StringComparison.InvariantCulture); // already checked by Contains
			var beginDisclaimer = bodyText.IndexOf(kDisclaimer, StringComparison.InvariantCulture);
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
				var storyAttrib = bodyText.Substring(beginStoryAttrib, endStoryAttrib - beginStoryAttrib);
				var match = Regex.Match(storyAttrib, "(©[^0-9]*, [12][09][0-9][0-9]).* (CC BY[A-Z0-9-. ]*) license", RegexOptions.CultureInvariant);
				if (match.Success)
				{
					SetBookCopyright(match.Groups[1].Value);
					SetBookLicense(match.Groups[2].Value.Trim());
				}
			}
			if (beginOtherCredits > 0)
			{
				var endOtherCredits = bodyText.Length;
				if (beginIllustration > 0)
					endOtherCredits = beginIllustration;
				else if (beginDisclaimer > 0)
					endOtherCredits = beginDisclaimer;
				var begin = beginOtherCredits + kOtherCredits.Length;
				var otherCreditsText = bodyText.Substring(begin, endOtherCredits - begin).Trim();
				if (otherCreditsText.Length > 0)
				{
					_contributionsXmlBldr.Append("<p>");
					_contributionsXmlBldr.Append(otherCreditsText);
					_contributionsXmlBldr.AppendLine("</p>");
				}
			}
			if (beginIllustration > 0)
			{
				var endIllustration = bodyText.Length;
				if (beginDisclaimer > 0)
					endIllustration = beginDisclaimer;
				var illustrationAttributions = bodyText.Substring(beginIllustration, endIllustration - beginIllustration);
				var matches = Regex.Matches(illustrationAttributions, "(Cover [Pp]age:|Page [0-9]+:)", RegexOptions.CultureInvariant);
				for (int i = 0; i < matches.Count; ++i)
				{
					int idxBegin = matches[i].Index;
					int idxEnd = illustrationAttributions.Length;
					if (i < matches.Count - 1)
						idxEnd = matches[i + 1].Index;
					var credit = illustrationAttributions.Substring(idxBegin, idxEnd - idxBegin).Trim();
					var pageText = matches[i].Groups[1].Value;
					ProcessIllustrationAttribution(pageText, credit);
				}
			}
		}

		private void ProcessIllustrationAttribution(string pageText, string credit)
		{
			var creditText = credit.Substring(pageText.Length).Trim();
			var beginCredit = creditText.IndexOf(" by ", StringComparison.InvariantCulture);
			if (beginCredit < 0)
			{
				// Some books omit the space between "by" and the author's name.
				beginCredit = creditText.IndexOf(", by", StringComparison.InvariantCulture);
				if (beginCredit > 0)
					++beginCredit;	// move past comma
			}
			if (beginCredit > 0)
				creditText = creditText.Substring(beginCredit).Trim();
			if (!_creditsAndPages.TryGetValue(creditText, out List<int> pages))
			{
				pages = new List<int>();
				_creditsAndPages.Add(creditText, pages);
			}
			int pageNumber;
			if (pageText.ToLowerInvariant() == "cover page:")
				pageNumber = 0;
			else
				pageNumber = GetPageNumber(pageText) - 1;   // Content starts at page 2 for Pratham, but page 1 for Bloom.
			pages.Add(pageNumber);

			var beginDesc = pageText.Length + 1;
			var endDesc = beginDesc + beginCredit - 1;
			string description = "";
			if (beginDesc < endDesc)
				description = credit.Substring(beginDesc, endDesc - beginDesc).Trim();
			SetImageMetaData(pageNumber, description, creditText);
		}

		private int GetPageNumber(string pageText)
		{
			int result;
			if (int.TryParse(pageText.Substring(5, pageText.Length - 6), out result))
				return result;
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

		private void SetAllImageCopyrights(string artCopyright, string artLicense)
		{
			var imgCover = _bloomDoc.SelectSingleNode($"//div[@id='bloomDataDiv']/div[@data-book='coverImage']") as XmlElement;
			if (imgCover != null)
			{
				imgCover.SetAttribute("data-copyright", artCopyright);
				imgCover.SetAttribute("data-license", artLicense);
			}
			foreach (var img in _bloomDoc.SelectNodes("//div[contains(@class,'numberedPage')]//div[contains(@class,'bloom-imageContainer')]/img[@src]").Cast<XmlElement>().ToList())
			{
				img.SetAttribute("data-copyright", artCopyright);
				img.SetAttribute("data-license", artLicense);
			}
		}

		private void SetImageMetaData(int pageNumber, string description, string creditText)
		{
			XmlElement img = null;
			string creator = null;
			string copyright = null;
			string license = null;
			var match = Regex.Match(creditText, "by *(.*) *(©.*[12][09][0-9][0-9]\\.?).*Released under[ a]* (CC[ A-Z0-9.-]+) license");
			if (match.Success)
			{
				creator = match.Groups[1].Value.Trim();
				copyright = match.Groups[2].Value.Trim();
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
			}
			else
			{
				Console.WriteLine("WARNING: Could not find expected image on page {0}", pageNumber == 0 ? "Front Cover" : pageNumber.ToString());
			}
		}

		private void CreateThumbnails()
		{
			var imageFile = Path.ChangeExtension(_options.EpubFile, "jpg");
			if (_options.EpubFile.EndsWith(".epub.zip", StringComparison.InvariantCulture))
				imageFile = _options.EpubFile.Replace(".epub.zip", ".jpg");
			if (_publisher.ToLowerInvariant().StartsWith("african storybook", StringComparison.InvariantCulture))
				imageFile = GetFirstPageOrCoverImage();
			if (String.IsNullOrWhiteSpace(imageFile) || !File.Exists(imageFile))
				return;
			if (_options.Verbose)
				Console.WriteLine("INFO: creating thumbnail images from {0}", imageFile);
			using (var util = new ImageUtility(imageFile))
			{
				util.CreateThumbnail(70, Path.Combine(_bookFolder, "thumbnail-70.png"));
				File.Copy(Path.Combine(_bookFolder, "thumbnail-70.png"), Path.Combine(_bookFolder, "thumbnail.png"));
				util.CreateThumbnail(256, Path.Combine(_bookFolder, "thumbnail-256.png"));
				util.CreateThumbnail(200, Path.Combine(_bookFolder, "coverImage200.jpg"));
			}
			if (File.Exists(Path.Combine(_bookFolder, "thumbnail.jpg")))
				File.Delete(Path.Combine(_bookFolder, "thumbnail.jpg"));
		}

		private string GetFirstPageOrCoverImage()
		{
			var firstPageImageDiv = _bloomDoc.SelectSingleNode("/html/body/div[@data-page-number='1']//div[contains(@class,'bloom-imageContainer')]/img") as XmlElement;
			if (firstPageImageDiv != null)
				return Path.Combine(_bookFolder, firstPageImageDiv.GetAttribute("src"));
			var coverImageDiv = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage' and @lang='*']") as XmlElement;
			if (coverImageDiv != null)
				return Path.Combine(_bookFolder, coverImageDiv.InnerText.Trim());
			return "";
		}
	}
}
