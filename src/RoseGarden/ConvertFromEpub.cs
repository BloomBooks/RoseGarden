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

		EpubMetadata _metadata;
		string _epubFolder;
		string _bookFolder;
		string _htmFileName;
		XmlDocument _bloomDoc;
		XmlDocument _templateBook;  // provides templates for new content pages.
		List<XmlElement> _templatePages;
		XmlDocument _xmatterBook;   // provides template for the front cover page and other xmatter pages.
		List<XmlElement> _xmatterPages;

		// Files that are copied into a new Basic Book.
		readonly private string[] _copiedFiles = new string[]
		{
			"browser/bookLayout/basePage.css",
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
		// Files that provide templates for a new Basic Book.
		readonly private string _templateFile = "browser/templates/template books/Basic Book/Basic Book.html";
		readonly private string _xmatterFile = "browser/templates/xMatter/Traditional-XMatter/Traditional-XMatter.html";

		public ConvertFromEpub(ConvertOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
		}

		public int Run()
		{
			if (!VerifyOptions())
				return 1;
			try
			{
				InitializeData();

				_bloomDoc = new XmlDocument();
				_bloomDoc.PreserveWhitespace = true;
				// TODO: merge Book.htm with pages from Traditional-XMatter.html?
				_bloomDoc.Load(Path.Combine(_bookFolder, _htmFileName));

				_templateBook = new XmlDocument();
				_templateBook.PreserveWhitespace = true;
				// The head section of the template book may have unterminated meta elements, which are valid HTML but not XML.
				var htmlText = File.ReadAllText(Path.Combine(_options.BloomFolder, _templateFile));
				htmlText = RemoveHtmlHead(htmlText);
				_templateBook.LoadXml(htmlText);
				_templatePages = _templateBook.SelectNodes("//div[contains(@class,'numberedPage')]").Cast<XmlElement>().ToList();

				_xmatterBook = new XmlDocument();
				_xmatterBook.PreserveWhitespace = true;
				// The head section of the xmatter book may have unterminated meta elements, which are valid HTML but not XML.
				htmlText = File.ReadAllText(Path.Combine(_options.BloomFolder, _xmatterFile));
				htmlText = RemoveHtmlHead(htmlText);
				_xmatterBook.LoadXml(htmlText);
				_xmatterPages = _xmatterBook.SelectNodes("//div[@data-xmatter-page]").Cast<XmlElement>().ToList();

				ConvertBook();

				File.Delete(Path.Combine(_bookFolder, _htmFileName));
				_bloomDoc.Save(Path.Combine(_bookFolder, _htmFileName));
				if (!String.IsNullOrWhiteSpace(_options.CollectionFolder) && _options.CollectionFolder != _bookFolder)
					CopyBloomBookToOutputFolder();
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR: caught exception: {0}", e.Message);
				if (_options.VeryVerbose)
					Console.WriteLine(e.StackTrace);
				return 2;
			}
			return 0;
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
			// placeholder for if/when there's something we can validate at this point.
			return allValid;
		}

		private void CopyBloomBookToOutputFolder()
		{
			var newBookFolder = Path.Combine(_options.CollectionFolder, Path.GetFileNameWithoutExtension(_htmFileName));
			if (Directory.Exists(newBookFolder))
			{
				if (!_options.ForceOverwrite)
				{
					Console.WriteLine("WARNING: {0} already exists.", newBookFolder);
					Console.WriteLine("Use -F (--force) if you want to overwrite it.");
					return;
				}
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: deleting directory {0}", newBookFolder);
				Directory.Delete(newBookFolder, true);
			}
			CopyDirectory(_bookFolder, newBookFolder);
		}

		private void CopyDirectory(string sourceDir, string destDir)
		{
			if (_options.VeryVerbose)
				Console.WriteLine("INFO: copying directory {0} to {1}", sourceDir, destDir);
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

		private void InitializeData()
		{
			var workBase = Path.Combine(Path.GetTempPath(), "SIL", "RoseGarden");
			if (Directory.Exists(workBase))
				Directory.Delete(workBase, true);
			Directory.CreateDirectory(workBase);
			_epubFolder = Path.Combine(workBase, "EPUB");
			Directory.CreateDirectory(_epubFolder);
			ExtractZippedFiles(_options.EpubFile, _epubFolder);
			_metadata = new EpubMetadata(_epubFolder, _options.VeryVerbose);
			if (String.IsNullOrWhiteSpace(_options.FileName))
				_htmFileName = Program.SanitizeNameForFileSystem(_metadata.Title) + ".htm";
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
		}

		private void ConvertBook()
		{
			foreach (var imageFile in _metadata.ImageFiles)
			{
				var destPath = Path.Combine(_bookFolder, Path.GetFileName(imageFile));
				File.Copy(imageFile, destPath);
			}

			SetDataDivTextValue("contentLanguage1", _metadata.LanguageCode);
			SetDataDivTextValue("smallCoverCredits", "");

			for (int pageNumber = 0; pageNumber < _metadata.PageFiles.Count; ++pageNumber)
			{
				ConvertPage(pageNumber, _metadata.PageFiles[pageNumber]);
			}
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: processed {0} pages", _metadata.PageFiles.Count);
		}

		private void ConvertPage(int pageNumber, string pageFilePath)
		{
			if (_options.VeryVerbose)
				Console.WriteLine("INFO: converting {0}", pageFilePath);
			if (pageNumber == 0)
			{
				ConvertCoverPage(pageFilePath);
			}
			else
			{
				if (!ConvertContentPage(pageFilePath, pageNumber))
					Console.WriteLine("WARNING: {0} did not convert successfully.  (Navigation pages are not expected to convert.)", pageFilePath);
			}
		}

		private void ConvertCoverPage(string pageFilePath)
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
						SetTitle(title);
						titleSet = true;
					}
					else
					{
						AddCoverContributor(child.OuterXml);
						authorEtcSet = true;
					}
				}
			}
			if (!titleSet)
				SetTitle(_metadata.Title);
			if (!authorEtcSet)
			{
				// TODO: make & < and > safe for XML.
				// TODO: Localize "Author(s):", "Illustrator(s):", etc.
				var bldr = new StringBuilder();
				if (_metadata.Authors.Count > 0)
				{
					bldr.Append("<p>");
					if (_metadata.Authors.Count == 1)
						bldr.AppendFormat("Author: {0}", _metadata.Authors[0]);
					else
						bldr.AppendFormat("Authors: {0}", String.Join(", ", _metadata.Authors));
					bldr.Append("</p>");
				}
				if (_metadata.Illustrators.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_metadata.Illustrators.Count == 1)
						bldr.AppendFormat("Illustrator: {0}", _metadata.Illustrators[0]);
					else
						bldr.AppendFormat("Illustrators: {0}", String.Join(", ", _metadata.Illustrators));
					bldr.Append("</p>");
				}
				if (_metadata.OtherCreators.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_metadata.OtherCreators.Count == 1)
						bldr.AppendFormat("Creator: {0}", _metadata.OtherCreators[0]);
					else
						bldr.AppendFormat("Creators: {0}", String.Join(", ", _metadata.OtherCreators));
					bldr.Append("</p>");
				}
				if (_metadata.OtherContributors.Count > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine();
					bldr.Append("<p>");
					if (_metadata.OtherContributors.Count == 1)
						bldr.AppendFormat("Contributor: {0}", _metadata.OtherContributors[0]);
					else
						bldr.AppendFormat("Contributors: {0}", String.Join(", ", _metadata.OtherContributors));
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
			var coverPage = SelectXMatterPage("frontCover");
			if (coverPage == null)
			{
				Console.WriteLine("ERROR: cannot retrieve front cover page from xmatter file");
				return;
			}
			var newPageDiv = _bloomDoc.CreateElement("div");
			foreach (XmlAttribute attr in coverPage.Attributes.Cast<XmlAttribute>())
				newPageDiv.SetAttribute(attr.Name, attr.Value);
			newPageDiv.InnerXml = coverPage.InnerXml;
			// Find the first endmatter page and insert the new page before it.
			var docBody = _bloomDoc.SelectSingleNode("/html/body");
			docBody.AppendChild(newPageDiv);
		}

		private XmlElement SelectXMatterPage(string dataXmatterPage)
		{
			foreach (var page in _xmatterPages)
			{
				if (page.GetAttribute("data-xmatter-page") == dataXmatterPage)
					return page;
			}
			return null;
		}

		private XmlElement GetOrCreateDataDivElement(string key)
		{
			var dataDiv = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='" + key + "']") as XmlElement;
			if (dataDiv == null)
			{
				dataDiv = _bloomDoc.CreateElement("div");
				dataDiv.SetAttribute("data-book", key);
				var dataBook = _bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']");
				Debug.Assert(dataBook != null);
				dataBook.AppendChild(dataDiv);
			}
			return dataDiv;
		}

		private void SetDataDivTextValue(string key, string value)
		{
			var dataDiv = GetOrCreateDataDivElement(key);
			dataDiv.InnerText = value;
		}

		private void SetDataDivParaValue(string key, string value)
		{
			var dataDiv = GetOrCreateDataDivElement(key);
			dataDiv.InnerXml = "<p>" + value + "</p>";
		}

		private void SetTitle(string title)
		{
			// This should be called only once.
			var titleNode = _bloomDoc.SelectSingleNode("/html/head/title");
			titleNode.InnerText = title;
			SetDataDivParaValue("bookTitle", title);
			var zTitle = _bloomDoc.SelectSingleNode("//div[contains(@class, 'bloom-editable') and @data-book='bookTitle' and @lang='z']") as XmlElement;
			AddNewLanguageDiv(zTitle, "<p>" + title + "</p>");
		}

		private void AddCoverContributor(string paraXml)
		{
			// This may be called multiple times.
			var dataDiv = GetOrCreateDataDivElement("smallCoverCredits");
			var newXml = Regex.Replace(paraXml, " xmlns=[\"'][^\"']*[\"']", "", RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
			var credits = dataDiv.InnerXml + newXml;
			dataDiv.InnerXml = credits;
			var newContrib = _bloomDoc.SelectSingleNode($"//div[contains(@class, 'bloom-editable') and @data-book='smallCoverCredits' and @lang='{_metadata.LanguageCode}']");
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
			newDiv.SetAttribute("lang", _metadata.LanguageCode);
			newDiv.InnerXml = content;
			zTemplateDiv.ParentNode.PrependChild(newDiv);
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
			var navElement = pageDoc.SelectSingleNode("/x:html/x:body/x:nav", nsmgr);
			var body = pageDoc.SelectSingleNode("/x:html/x:body", nsmgr) as XmlElement;
			var elements = body.SelectNodes("//x:img[normalize-space(@src)!='']|//x:p[normalize-space()!='']", nsmgr).Cast<XmlElement>().ToList();
			if (navElement != null || elements.Count == 0)
				return false;
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
					++imageCount;	// each image counts separately.
				}
				else
				{
					Debug.Assert(child.Name == "p");
					if (prevChild != "p")	// paragraphs clump together when contiguous.
						++textCount;
				}
				prevChild = child.Name;
			}
			var templatePage = SelectTemplatePage(imageCount, textCount, firstChild, lastChild);
			if (templatePage == null)
			{
				Console.WriteLine("ERROR: cannot retrieve template page for {0} images and {1} text fields", imageCount, textCount);
				return false;
			}
			var newPageDiv = _bloomDoc.CreateElement("div");
			foreach (XmlAttribute attr in templatePage.Attributes.Cast<XmlAttribute>())
				newPageDiv.SetAttribute(attr.Name, attr.Value);
			newPageDiv.SetAttribute("data-page-number", pageNumber.ToString());
			newPageDiv.SetAttribute("lang", _metadata.LanguageCode);
			newPageDiv.InnerXml = templatePage.InnerXml;
			// Find the first endmatter page and insert the new page before it.
			var endMatter = _bloomDoc.SelectSingleNode("/html/body/div[@data-xmatter-page='insideBackCover']");
			var docBody = _bloomDoc.SelectSingleNode("/html/body");
			docBody.InsertBefore(newPageDiv, endMatter);

			var imageIdx = 0;
			var textIdx = 0;
			prevChild = "";
			var innerXmlBldr = new StringBuilder();
			var images = newPageDiv.SelectNodes(".//img").Cast<XmlElement>().ToList();
			var textGroupDivs = newPageDiv.SelectNodes(".//div[contains(@class,'bloom-translationGroup')]").Cast<XmlElement>().ToList();
			foreach (var child in elements)
			{
				if (child.Name == "img")
				{
					var src = child.GetAttribute("src");
					if (imageIdx < images.Count)
					{
						images[imageIdx].SetAttribute("src", src);
						var alt = child.GetAttribute("alt");
						if (String.IsNullOrWhiteSpace(alt))
							images[imageIdx].SetAttribute("alt", alt);
						else
							images[imageIdx].SetAttribute("alt", src);
					}
					else
					{
						Console.WriteLine("WARNING: no place on page to show image file {0}", src);
					}
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
						var xml = Regex.Replace(outer, " xmlns=[\"'][^\"']*[\"']", "", RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
						innerXmlBldr.Append(xml);
					}
				}
				prevChild = child.Name;
			}
			if (innerXmlBldr.Length > 0)
				StoreAccumulatedParagraphs(textIdx, innerXmlBldr, textGroupDivs);

			return true;
		}

		private void StoreAccumulatedParagraphs(int textIdx, StringBuilder innerXmlBldr, List<XmlElement> textGroupDivs)
		{
			Debug.Assert(innerXmlBldr != null && innerXmlBldr.Length > 0);
			Debug.Assert(textGroupDivs != null && textGroupDivs.Count > 0);
			if (textIdx < textGroupDivs.Count)
			{
				// Add new div with accumulated paragraphs
				var newTextDiv = _bloomDoc.CreateElement("div");
				textGroupDivs[textIdx].PrependChild(newTextDiv);
				newTextDiv.SetAttribute("lang", _metadata.LanguageCode);
				newTextDiv.SetAttribute("class", "bloom-editable");
				newTextDiv.SetAttribute("contenteditable", "true");
				newTextDiv.InnerXml = innerXmlBldr.ToString();
			}
			else
			{
				var div = textGroupDivs[textGroupDivs.Count - 1].FirstChild;
				var inner = div.InnerXml;
				var xml = Regex.Replace(inner, " xmlns=[\"'][^\"']*[\"']", "", RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
				innerXmlBldr.Insert(0, xml);
				div.InnerXml = innerXmlBldr.ToString();
			}
			innerXmlBldr.Clear();
		}

		private XmlElement SelectTemplatePage(int imageCount, int textCount, string firstChild, string lastChild)
		{
			if (imageCount == 0)
			{
				return SelectTemplatePage("TemplateBooks.PageLabel.Just Text");
			}
			if (imageCount == 1)
			{
				switch (textCount)
				{
					case 0:
						return SelectTemplatePage("TemplateBooks.PageLabel.Just a Picture");
					case 1:
						if (firstChild == "img")
							return SelectTemplatePage("TemplateBooks.PageLabel.Basic Text & Picture");
						else
							return SelectTemplatePage("TemplateBooks.PageLabel.Picture on Bottom");
					case 2:
						Debug.Assert(firstChild == "p" && lastChild == "p");
						return SelectTemplatePage("TemplateBooks.PageLabel.Picture in Middle");
				}
			}
			else
			{
				// We can't handle 2 or more images on the page automatically at this point.
				if (textCount == 0)
					return SelectTemplatePage("TemplateBooks.PageLabel.Just a Picture");
				else if (textCount == 1)
					return SelectTemplatePage("TemplateBooks.PageLabel.Basic Text & Picture");
				else
					return SelectTemplatePage("TemplateBooks.PageLabel.Picture in Middle");
			}
			return null;
		}

		private XmlElement SelectTemplatePage(string dataI18n)
		{
			foreach (var page in _templatePages)
			{
				var div = page.SelectSingleNode($"./div[@data-i18n='{dataI18n}']");
				if (div != null)
					return page;
			}
			return null;
		}

		private void ConvertEndCreditsPage(string pageFilePath)
		{
			Console.WriteLine("TODO: IMPLEMENT ConvertEndCreditsPage(\"{0}\")", pageFilePath);
		}

		public static void ExtractZippedFiles(string epubFile, string folder)
		{
			Directory.CreateDirectory(folder);
			var extractPath = folder + Path.DirectorySeparatorChar;     // safer for unknown zip sources
			using (var zipToOpen = new FileStream(epubFile, FileMode.Open))
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
	}
}
