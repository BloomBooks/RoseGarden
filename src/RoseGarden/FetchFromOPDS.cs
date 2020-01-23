using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RoseGarden
{
	public class FetchFromOPDS
	{
		private const string DigitalLibraryUrl = "https://api.digitallibrary.io/book-api/opds/v1/root.xml";
		private const string StoryWeaverUrl = "https://storage.googleapis.com/story-weaver-e2e-production/catalog/catalog.xml";

		private HttpClient _client = new HttpClient();
		private XmlDocument _rootCatalog = new XmlDocument();
		private XmlDocument _langCatalog = new XmlDocument();
		private XmlNamespaceManager _nsmgr;
		private FetchOptions _options;

		public FetchFromOPDS(FetchOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			_rootCatalog.PreserveWhitespace = true;
			_langCatalog.PreserveWhitespace = true;
		}

		public int Run()
		{
			if (!VerifyOptions())
				return 1;
	
			GetRootPage();
			// Fill in the language specific catalog if the language name is given.
			if (!String.IsNullOrWhiteSpace(_options.LanguageName))
				GetCatalogForLanguage();
			// Write the catalog file if the catalog filename is given.  If the langauge name is given,
			// write out the language specific catalog.  Otherwise write out whatever catalog was loaded
			// from the original root url.
			if (!String.IsNullOrEmpty(_options.CatalogFile))
			{
				if (String.IsNullOrEmpty(_options.LanguageName))
					File.WriteAllText(_options.CatalogFile, _rootCatalog.OuterXml);
				else
					File.WriteAllText(_options.CatalogFile, _langCatalog.OuterXml);
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
			if (_options.UseStoryWeaver)
				++urlCount;
			if (_options.UseDigitalLibrary)
				++urlCount;
			if (!String.IsNullOrWhiteSpace(_options.Url))
				++urlCount;
			if (urlCount != 1)
			{
				Console.WriteLine("Exactly one of -u (--url), -d (--digitallibrary), and -s (--storyweaver) must be used.");
				allValid = false;
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
			_langCatalog.LoadXml("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>" + Environment.NewLine + "</feed>");
			XmlElement selfLink;
			XmlElement nextLink;
			XmlElement lastLink;
			var xpath = $"//a:entry/dcterms:language[contains(text(),'{_options.LanguageName}')]";  // actually gets child of entry...
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

			if (_options.UseStoryWeaver)
				catalogUrl = StoryWeaverUrl;
			else if (_options.UseDigitalLibrary)
				catalogUrl = DigitalLibraryUrl;
			else
				catalogUrl = _options.Url;

			var data = GetOpdsPage(catalogUrl);
			_rootCatalog.LoadXml(data);
			_nsmgr = new XmlNamespaceManager(_rootCatalog.NameTable);
			_nsmgr.AddNamespace("lrmi", "http://purl.org/dcx/lrmi-terms/");
			_nsmgr.AddNamespace("opds", "http://opds-spec.org/2010/catalog");
			_nsmgr.AddNamespace("dc", "http://purl.org/dc/terms/");
			_nsmgr.AddNamespace("dcterms", "http://purl.org/dc/terms/");
			_nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");
		}

		public string GetOpdsPage(string urlPath)
		{
			if (_options.VeryVerbose)
				Console.WriteLine($"Retrieving OPDS page at {urlPath}");
			return _client.GetStringAsync(urlPath).Result;
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
			if (_options.DownloadPDF)
				return DownloadBook(bookEntry, "pdf");
			else
				return DownloadBook(bookEntry, "epub");
		}

		private int DownloadBook(XmlElement bookEntry, string type)
		{
			var apptype = (type == "epub") ? "epub+zip" : type;
			// TODO: sanitize filename if based on title.
			var path = (String.IsNullOrWhiteSpace(_options.OutputFile)) ? Path.Combine(Path.GetTempPath(), _options.BookTitle + "." + type) : _options.OutputFile;
			if (_options.UseStoryWeaver)
			{
				if (!path.EndsWith(".zip", StringComparison.InvariantCulture))
					path = path + ".zip";
				if (type == "pdf")
					apptype = "pdf+zip";
			}

			var link = bookEntry.SelectSingleNode($"./a:link[contains(@rel, 'http://opds-spec.org/acquisition') and @type='application/{apptype}']", _nsmgr) as XmlElement;
			if (link == null)
			{
				Console.WriteLine($"WARNING: selected book does not have a proper link for {type} download!?");
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
			var bytes = _client.GetByteArrayAsync(url).Result;

			if (_options.Verbose)
				Console.WriteLine("INFO: downloading {0} into {1}", url, path);
			File.WriteAllBytes(path, bytes);
			return 0;
		}
	}
}
