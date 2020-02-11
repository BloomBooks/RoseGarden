// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Net.Http;
using System.Xml;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RoseGarden
{
	public class FetchFromOPDS
	{
		public struct Source
		{
			public string Code;
			public string Url;
		}
		public readonly Source[] Sources= new Source[] {
			new Source { Code="gdl", Url="https://api.digitallibrary.io/book-api/opds/v1/root.xml" },
			new Source { Code="sw", Url="https://storage.googleapis.com/story-weaver-e2e-production/catalog/catalog.xml" }
		};

		private HttpClient _client = new HttpClient();
		private XmlDocument _rootCatalog = new XmlDocument();
		private XmlDocument _langCatalog = new XmlDocument();
		private XmlNamespaceManager _nsmgr;
		private FetchOptions _options;
		private string _feedTitle;
		private string _downloadPath;

		public FetchFromOPDS(FetchOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			_rootCatalog.PreserveWhitespace = true;
			_langCatalog.PreserveWhitespace = true;
			_downloadPath = Path.Combine(Path.GetTempPath(), "RoseGarden", "Downloads");
			Directory.CreateDirectory(_downloadPath);
		}

		public int RunFetch()
		{
			if (!VerifyOptions())
				return 1;

			if (_options.DryRun)
				Console.WriteLine("DRY RUN MESSAGES");
			GetRootPage();
			// Fill in the language specific catalog if the language name is given.
			if (!String.IsNullOrWhiteSpace(_options.LanguageName))
				GetCatalogForLanguage();
			// Don't overwrite an input catalog file.
			if (!String.IsNullOrWhiteSpace(_options.Url) || !String.IsNullOrWhiteSpace(_options.Source))
			{
				// Write the catalog file if the catalog filename is given.  If the language name is given,
				// write out the language specific catalog.  Otherwise write out whatever catalog was loaded
				// from the original root url.
				if (!String.IsNullOrEmpty(_options.CatalogFile))
				{
					if (String.IsNullOrEmpty(_options.LanguageName))
						File.WriteAllText(_options.CatalogFile, _rootCatalog.OuterXml);
					else
						File.WriteAllText(_options.CatalogFile, _langCatalog.OuterXml);
				}
			}
			// Retrieve and save a book if the title is provided.  The author and output filepath may optionally
			// be provided as well.  If the language name is given, search the language specific catalog.  Otherwise,
			// search the catalog loaded for the root url.
			if (!String.IsNullOrEmpty(_options.BookTitle))
			{
				return FetchAndSaveBook();
			}
			return 0;
		}

		private bool VerifyOptions()
		{
			bool allValid = true;

			var urlCount = 0;
			if (!String.IsNullOrWhiteSpace(_options.Url))
				++urlCount;
			if (!String.IsNullOrWhiteSpace(_options.Source))
			{
				if (!Sources.Any(s => s.Code == _options.Source.ToLowerInvariant()))
				{
					Console.Write("The -s (--source) option recognizes only these codes:");
					foreach (var src in Sources)
						Console.Write(" {0}", src.Code);
					Console.WriteLine();
					allValid = false;
				}
				++urlCount;
			}
			if (urlCount != 1)
			{
				if (urlCount == 0)
				{
					if (String.IsNullOrWhiteSpace(_options.CatalogFile) || !File.Exists(_options.CatalogFile))
					{
						Console.WriteLine("If neither -u (--url) nor -s (--source) is given, -c (--catalog) must provide an existing file");
						allValid = false;
					}
				}
				else
				{
					Console.WriteLine("Exactly one of -u (--url) or -s (--source) must be used unless -c (--catalog) is given as an input file.");
					allValid = false;
				}
			}

			var outputCount = 0;
			if (!String.IsNullOrWhiteSpace(_options.CatalogFile))
				++outputCount;
			if (!String.IsNullOrWhiteSpace(_options.BookTitle))
				++outputCount;
			if (outputCount == 0)
			{
				Console.WriteLine("At least one of -c (--catalog) and -t (--title) must be used.");
				allValid = false;
			}
			if (urlCount == 0 && !String.IsNullOrWhiteSpace(_options.CatalogFile) && String.IsNullOrEmpty(_options.BookTitle))
			{
				Console.WriteLine("If -c (--catalog) is an input file, then -t (--title) must be used.");
				allValid = false;
			}

			if (String.IsNullOrWhiteSpace(_options.BookTitle) && !String.IsNullOrWhiteSpace(_options.OutputFile))
			{
				Console.WriteLine("Using -o (--output) requires also using -t (--title).");
				allValid = false;
			}

			if (String.IsNullOrWhiteSpace(_options.BookTitle) && !String.IsNullOrWhiteSpace(_options.Author))
			{
				Console.WriteLine("Using -a (--author) requires also using -t (--title).");
				allValid = false;
			}

			if (String.IsNullOrWhiteSpace(_options.BookTitle) && !String.IsNullOrWhiteSpace(_options.AccessToken))
			{
				Console.WriteLine("Using -k (--key) requires also using -t (--title).");
				allValid = false;
			}

			if (String.IsNullOrWhiteSpace(_options.BookTitle) && _options.DownloadPDF)
			{
				Console.WriteLine("Using -p (--pdf) requires also using -t (--title).");
				allValid = false;
			}
			return allValid;
		}

		private int GetCatalogForLanguage()
		{
			if (_options.Verbose)
				Console.WriteLine("Extracting a catalog specifically for {0}", _options.LanguageName);
			_langCatalog.LoadXml("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>" + Environment.NewLine + "</feed>");
			if (!String.IsNullOrWhiteSpace(_feedTitle))
			{
				var title = _langCatalog.CreateElement("title", "http://www.w3.org/2005/Atom");
				title.InnerText = $"{_feedTitle} (extract for {_options.LanguageName})";
				_langCatalog.DocumentElement.AppendChild(title);
			}
			XmlElement selfLink;
			XmlElement nextLink;
			XmlElement lastLink;
			var xpath = $"//a:entry/dcterms:language[text()='{_options.LanguageName}']";  // actually gets child of entry...
			var entries = _rootCatalog.DocumentElement.SelectNodes(xpath, _nsmgr);
			int entryCount = 0;
			XmlDocument pageOfCatalog;
			string hrefLink = null;
			if (entries.Count == 0)
			{
				entries = null;
				// may need to pull a different root OPDS page: look for a link for the language
				var link = _rootCatalog.DocumentElement.SelectSingleNode($"/a:feed/a:link[@rel='http://opds-spec.org/facet' and @opds:facetGroup='Languages' and contains(@title,'{_options.LanguageName}')]", _nsmgr) as XmlElement;
				if (link == null)
				{
					if (_options.DryRun)
						return 0;
					Console.WriteLine("WARNING: Could not find any entries or a link for {0}", _options.LanguageName);
					return 2;
				}
				hrefLink = link.GetAttribute("href");
				if (link.GetAttribute("activeFacet", "opds") == "true")
				{
					pageOfCatalog = _rootCatalog;
				}
				else
				{
					var data = GetOpdsPage(hrefLink);
					pageOfCatalog = new XmlDocument();
					pageOfCatalog.PreserveWhitespace = true;
					pageOfCatalog.LoadXml(data);
				}
				// In this type of catalog, the entire file has entries for one (implied) language.
				xpath = "/a:feed/a:entry";
			}
			else
			{
				// In this type of catalog, each entry tells you its language.
				pageOfCatalog = _rootCatalog;
			}
			while (true)
			{
				if (_options.DryRun)
					break;
				selfLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='self']", _nsmgr) as XmlElement;
				nextLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='next']", _nsmgr) as XmlElement;
				lastLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='last']", _nsmgr) as XmlElement;
				if (entries == null)
				{
					entryCount += AddEntriesToLangCatalog(pageOfCatalog, xpath);
				}
				else
				{
					AddEntriesToLangCatalog(entries);
					entryCount += entries.Count;
					entries = null;
				}
				if (selfLink == null || nextLink == null || lastLink == null)
					break;
				var hrefNext = nextLink.GetAttribute("href");
				if (String.IsNullOrWhiteSpace(hrefNext))
					break;
				var data = GetOpdsPage(hrefNext);
				pageOfCatalog = new XmlDocument();
				pageOfCatalog.PreserveWhitespace = true;
				pageOfCatalog.LoadXml(data);
			}
			if (entryCount == 0)
			{
				if (_options.DryRun)
					return 0;
				Console.WriteLine("WARNING: Could not find any entries for {0} (href={1})", _options.LanguageName, hrefLink);
				return 2;
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: {0} entries found for {1}", entryCount, _options.LanguageName);

			// Clean up the extraneous, unnecessary xmlns attributes that InnerXml gratuitously inserts.
			var outerXml = _langCatalog.OuterXml;
			var idxFirst = outerXml.IndexOf("<entry", StringComparison.InvariantCulture);
			if (idxFirst > 0)
			{
				var head = outerXml.Substring(0, idxFirst);
				var tail = outerXml.Substring(idxFirst).Replace(" xmlns=\"\"", "")
					.Replace(" xmlns:dc=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:dcterms=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:lrmi=\"http://purl.org/dcx/lrmi-terms/\"", "")
					.Replace(" xmlns:opds=\"http://opds-spec.org/2010/catalog\"", "");
				_langCatalog = new XmlDocument();
				_langCatalog.PreserveWhitespace = true;
				_langCatalog.LoadXml(head + tail);
			}
			return 0;
		}

		private int AddEntriesToLangCatalog(XmlDocument catalogPage, string xpath)
		{
			var entries = catalogPage.DocumentElement.SelectNodes(xpath, _nsmgr);
			AddEntriesToLangCatalog(entries);
			return entries.Count;
		}

		private void AddEntriesToLangCatalog(XmlNodeList entries)
		{ 
			var newline = Environment.NewLine;
			foreach (var entry in entries.Cast<XmlElement>())
			{
				// If entry is actually the language element, use the parent element.  Otherwise, add a language element to the end of the inner XML.
				string innerXml;
				if (entry.LocalName == "language")
					innerXml = entry.ParentNode.InnerXml;
				else
					innerXml = $"{newline}{entry.InnerXml}{newline}<dcterms:language xmlns:dcterms='http://purl.org/dc/terms/'>{_options.LanguageName}</dcterms:language>{newline}";
				var newEntry = _langCatalog.CreateElement("entry", "http://www.w3.org/2005/Atom");
				innerXml = innerXml.Replace(" xmlns=\"http://www.w3.org/2005/Atom\"", "");
				innerXml = innerXml.Replace("><", ">" + Environment.NewLine + "<");
				newEntry.InnerXml = innerXml;
				_langCatalog.DocumentElement.AppendChild(newEntry);
				_langCatalog.DocumentElement.AppendChild(_langCatalog.CreateTextNode(Environment.NewLine));
			}
		}

		public void GetRootPage()
		{
			string catalogUrl;

			if (_options.Source != null)
				catalogUrl = (Sources.First(src => src.Code == _options.Source.ToLowerInvariant())).Url;
			else
				catalogUrl = _options.Url;
			if (!String.IsNullOrWhiteSpace(catalogUrl))
			{
				var data = GetOpdsPage(catalogUrl);
				_rootCatalog.LoadXml(data);
			}
			else
			{
				_rootCatalog.Load(_options.CatalogFile);
			}

			_nsmgr = new XmlNamespaceManager(_rootCatalog.NameTable);
			_nsmgr.AddNamespace("lrmi", "http://purl.org/dcx/lrmi-terms/");
			_nsmgr.AddNamespace("opds", "http://opds-spec.org/2010/catalog");
			_nsmgr.AddNamespace("dc", "http://purl.org/dc/terms/");
			_nsmgr.AddNamespace("dcterms", "http://purl.org/dc/terms/");
			_nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");

			var title = _rootCatalog.SelectSingleNode("/a:feed/a:title", _nsmgr);
			if (title != null)
				_feedTitle = title.InnerText;
			if (_options.VeryVerbose)
				Console.WriteLine("INFO: catalog title = {0}", _feedTitle);
		}

		public string GetOpdsPage(string urlPath)
		{
			if (_options.VeryVerbose)
				Console.WriteLine($"Retrieving OPDS page at {urlPath}");
			if (_options.DryRun)
				return "<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
					" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
					" xmlns:opds='http://opds-spec.org/2010/catalog'><title>Dry Run</title></feed>";
			try
			{
				return _client.GetStringAsync(urlPath).Result;
			}
			catch (Exception ex)
			{
				Console.WriteLine("WARNING: could not download OPDS page from {0} ({1})", urlPath, ex.Message);
				return "<feed></feed>";
			}
		}

		private int FetchAndSaveBook()
		{
			var catalog = String.IsNullOrWhiteSpace(_options.LanguageName) ? _rootCatalog : _langCatalog;
			var entries = catalog.DocumentElement.SelectNodes($"/a:feed/a:entry/a:title[text()='{_options.BookTitle}']", _nsmgr);
			if (entries.Count == 0)
			{
				Console.WriteLine("WARNING: Could not find a book with the title \"{0}\"", _options.BookTitle);
				return 2;
			}
			XmlElement bookEntry = null;
			if (entries.Count > 1 && !String.IsNullOrWhiteSpace(_options.Author))
			{
				foreach (var entry in entries.Cast<XmlElement>())
				{
					var parent = entry.ParentNode;
					var author = parent.SelectSingleNode($"./a:author/a:name[text()='{_options.Author}'");
					if (author != null)
					{
						bookEntry = parent as XmlElement;
						break;
					}
				}
				if (bookEntry == null)
				{
					Console.WriteLine("WARNING: Could not find a book written by {0} with the title \"{1}\"", _options.Author, _options.BookTitle);
					return 2;
				}
			}
			else
			{
				bookEntry = entries[0].ParentNode as XmlElement;
			}
			var ret = DownloadBook(bookEntry, "epub");
			if (ret != 0)
				return ret;
			if (_options.DownloadPDF)
			{
				ret = DownloadBook(bookEntry, "pdf");
				if (ret != 0)
					return ret;
			}
			if (_options.DownloadThumbnail)
			{
				ret = DownloadThumbnail(bookEntry);
				if (ret != 0)
					return ret;
			}
			if (_options.DownloadImage)
			{
				ret = DownloadImage(bookEntry);
				if (ret != 0)
					return ret;
			}
			WriteCatalogEntryFile(bookEntry);
			return ret;
		}

		private void WriteCatalogEntryFile(XmlElement bookEntry)
		{
			string path;
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				path = Path.Combine(_downloadPath, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".opds");
			}
			else
			{
				path = Path.ChangeExtension(_options.OutputFile, "opds");
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: writing catalog entry file {0}", path);
			var bldr = new StringBuilder();
			bldr.AppendLine("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>");
			var titleNode = _rootCatalog.SelectSingleNode("/a:feed/a:title", _nsmgr) as XmlElement;
			if (titleNode != null)
			{
				var title = Regex.Replace(titleNode.InnerText, " \\(extract for .*\\)$", "");
				bldr.AppendLine($"<title>{title} [extract]</title>");
			}
			var entry = Regex.Replace(bookEntry.OuterXml, " xmlns[:a-zA-Z]*=[\"'][^\"']+[\"']", "");
			bldr.AppendLine(entry);
			bldr.AppendLine("</feed>");
			File.WriteAllText(path, bldr.ToString());
		}

		private int DownloadThumbnail(XmlElement bookEntry)
		{
			var link = bookEntry.SelectSingleNode("./a:link[@rel='http://opds-spec.org/image/thumbnail']", _nsmgr) as XmlElement;
			if (link == null)
			{
				Console.WriteLine("WARNING: selected book does not have a link for a thumbnail image!?");
				if (_options.Verbose)
					Console.WriteLine(bookEntry.OuterXml);
				return 2;
			}
			var href = link.GetAttribute("href");
			var type = link.GetAttribute("type");
			if (type != "image/jpeg" && type != "image/png")
			{
				Console.WriteLine("WARNING: thumbnail link has unknown type: {0}", type);
				return 2;
			}
			// The actual returned file may well be jpeg even if advertised as image/png, so assume jpeg until we
			// see the data.
			string path;
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				path = Path.Combine(_downloadPath, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".thumb.jpg");
			}
			else
			{
				path = Path.ChangeExtension(_options.OutputFile, "thumb.jpg");
			}
			return DownloadImageFile(href, path);
		}

		private int DownloadImage(XmlElement bookEntry)
		{
			var link = bookEntry.SelectSingleNode("./a:link[@rel='http://opds-spec.org/image']", _nsmgr) as XmlElement;
			if (link == null)
			{
				Console.WriteLine("WARNING: selected book does not have a link for an image!?");
				if (_options.Verbose)
					Console.WriteLine(bookEntry.OuterXml);
				return 2;
			}
			var href = link.GetAttribute("href");
			var type = link.GetAttribute("type");
			if (type != "image/jpeg" && type != "image/png")
			{
				Console.WriteLine("WARNING: image link has unknown type: {0}", type);
				return 2;
			}
			// The actual returned file may well be jpeg even if advertised as image/png, so assume jpeg until we
			// see the data.
			string path;
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				path = Path.Combine(_downloadPath, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".jpg");
			}
			else
			{
				path = Path.ChangeExtension(_options.OutputFile, "jpg");
			}
			return DownloadImageFile(href, path);
		}

		private int DownloadImageFile(string href, string path)
		{
			byte[] bytes = null;
			if (!_options.DryRun)
			{
				try
				{
					bytes = _client.GetByteArrayAsync(href).Result;
					// check for PNG file data.
					if (bytes[0] == 0x89 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G')
						path = Path.ChangeExtension(path, "png");
				}
				catch (Exception ex)
				{
					Console.WriteLine("WARNING: could not download file from {0} ({1})", href, ex.Message);
					return 2;
				}
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: downloading {0} into {1}", href, path);
			if (!_options.DryRun)
				File.WriteAllBytes(path, bytes);
			return 0;
		}

		private int DownloadBook(XmlElement bookEntry, string type)
		{
			var apptype = (type == "epub") ? "epub+zip" : type;
			string path;
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				path = Path.Combine(_downloadPath, Program.SanitizeNameForFileSystem(_options.BookTitle) + "." + type);
			}
			else
			{
				path = _options.OutputFile;
				if (type == "pdf")
					path = Path.ChangeExtension(path, type);
			}
			if (_feedTitle != null && _feedTitle.ToLowerInvariant().Contains("storyweaver"))
			{
				if (_options.VeryVerbose)
					Console.WriteLine("INFO: adding .zip file extension for downloading from StoryWeaver");
				if (!path.EndsWith(".zip", StringComparison.InvariantCulture))
					path = path + ".zip";
				if (type == "pdf")
					apptype = "pdf+zip";
			}

			var link = bookEntry.SelectSingleNode($"./a:link[contains(@rel, 'http://opds-spec.org/acquisition') and @type='application/{apptype}']", _nsmgr) as XmlElement;
			if (link == null)
			{
				Console.WriteLine($"WARNING: selected book does not have a proper link for {type} download!?");
				if (_options.Verbose)
					Console.WriteLine(bookEntry.OuterXml);
				return 2;
			}
			var href = link.GetAttribute("href");
			var rel = link.GetAttribute("rel");
			var url = "";
			if (rel == "http://opds-spec.org/acquisition/open-access")
			{
				url = href;
			}
			else
			{
				// TODO: obtain token from a file in the user's AppData directory.  This would allow
				// storing tokens to multiple sources if that should become useful.
				var accessToken = _options.AccessToken;
				if (String.IsNullOrWhiteSpace(accessToken))
					accessToken = Environment.GetEnvironmentVariable("OPDSTOKEN");

				if (String.IsNullOrWhiteSpace(accessToken))
				{
					Console.WriteLine("WARNING: an access token appears to be needed to download this book.");
					Console.WriteLine(bookEntry.OuterXml);
					return 2;
				}
				url = href + "?token=" + accessToken;
			}
			byte[] bytes = null;
			if (!_options.DryRun)
			{
				try
				{
					bytes = _client.GetByteArrayAsync(url).Result;
				}
				catch (Exception ex)
				{
					Console.WriteLine("WARNING: could not download book from {0} ({1})", url, ex.Message);
					return 2;
				}
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: downloading {0} into {1}", url, path);
			if (!_options.DryRun)
				File.WriteAllBytes(path, bytes);
			return 0;
		}
	}
}
