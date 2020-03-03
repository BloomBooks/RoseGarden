// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RoseGarden
{
	public class OpdsClient
	{
		public struct Source
		{
			public string Code;
			public string Url;
		}
		public static readonly Source[] Sources = new Source[] {
			new Source { Code="gdl", Url="https://api.digitallibrary.io/book-api/opds/v1/root.xml" },
			new Source { Code="sw", Url="https://storage.googleapis.com/story-weaver-e2e-production/catalog/catalog.xml" }
		};

		private HttpClient _client = new HttpClient();
		private OpdsOptions _options;
		private XmlNamespaceManager _nsmgr;

		public static readonly string DownloadFolder = Path.Combine(Path.GetTempPath(), "RoseGarden", "Downloads");

		public string FeedTitle;

		public OpdsClient(OpdsOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			Directory.CreateDirectory(DownloadFolder);
		}

		public XmlDocument GetFilteredCatalog(XmlDocument rootCatalog)
		{
			if (_options.Verbose)
			{
				if (String.IsNullOrEmpty(_options.Publisher))
					Console.WriteLine("INFO: Extracting a catalog for all {0} books", _options.LanguageName);
				else if (String.IsNullOrEmpty(_options.LanguageName))
					Console.WriteLine("INFO: Extracting a catalog for all books published by {0}", _options.Publisher);
				else
					Console.WriteLine("INFO: Extracting a catalog for {0} books published by {1}", _options.LanguageName, _options.Publisher);
			}
			var filteredCatalog = CreateNewCatalogDocument();
			int entryCount = 0;
			int filteredEntryCount = 0;
			// Some OPDS catalogs are monolithic (StoryWeaver).  Others are split into multiple pages and by language (Global Digital Library).
			// The wonderful thing about some standards are that they allow you so many different options for implementing them!
			var catLinks = rootCatalog.DocumentElement.SelectNodes($"/a:feed/a:link[@rel='http://opds-spec.org/facet' and @opds:facetGroup='Languages']", _nsmgr).Cast<XmlElement>().ToList();
			if (catLinks.Count > 0)
			{
				// This must be a catalog split by language, possibly with multiple pages per language.
				foreach (var link in catLinks)
				{
					var language = link.GetAttribute("title");
					if (!String.IsNullOrWhiteSpace(_options.LanguageName) && !language.ToLowerInvariant().Contains(_options.LanguageName.ToLowerInvariant()))
						continue;
					XmlDocument firstPageOfCatalog;
					var hrefLink = link.GetAttribute("href");
					if (link.GetAttribute("activeFacet", "opds") == "true")
					{
						firstPageOfCatalog = rootCatalog;
					}
					else
					{
						var data = GetOpdsPage(hrefLink);
						firstPageOfCatalog = new XmlDocument();
						firstPageOfCatalog.PreserveWhitespace = true;
						firstPageOfCatalog.LoadXml(data);
					}
					LoadCatalogFromRootPage(firstPageOfCatalog, filteredCatalog, "/a:feed/a:entry", ref entryCount, ref filteredEntryCount);
				}
			}
			else
			{
				// This must be a linguistically monolithic catalog: all languages bundled together.
				// We still allow for the catalog to be split into multiple pages, however.
				var pageOfCatalog = rootCatalog;
				var xpath = $"/a:feed/a:entry/dcterms:language[text()='{_options.LanguageName}']";
				if (String.IsNullOrEmpty(_options.LanguageName))
					xpath = "/a:feed/a:entry";
				LoadCatalogFromRootPage(rootCatalog, filteredCatalog, xpath, ref entryCount, ref filteredEntryCount);
			}
			if (entryCount == 0)
			{
				if (_options.DryRun)
					return filteredCatalog;
				Console.WriteLine("WARNING: Could not find any entries for {0}", _options.LanguageName);
				return null;
			}
			if (_options.Verbose)
			{

				if (filteredEntryCount == entryCount)
					Console.WriteLine("INFO: {0} entries found for {1}", entryCount, _options.LanguageName);
				else
					Console.WriteLine("INFO: {0} entries found for {1} published by {2}", filteredEntryCount, _options.LanguageName, _options.Publisher);
			}

			return RemoveXmnlsAttributesFromCatalogEntries(filteredCatalog);
		}

		private XmlDocument CreateNewCatalogDocument()
		{
			XmlDocument catalog = new XmlDocument();
			catalog.PreserveWhitespace = true;
			catalog.LoadXml("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>" + Environment.NewLine + "</feed>");
			if (!String.IsNullOrWhiteSpace(FeedTitle))
			{
				var title = catalog.CreateElement("title", "http://www.w3.org/2005/Atom");
				title.InnerText = $"{FeedTitle} (extract for {_options.LanguageName})";
				catalog.DocumentElement.AppendChild(title);
			}
			return catalog;
		}

		/// <summary>
		/// Clean up the extraneous, unnecessary xmlns attributes that InnerXml gratuitously inserts.
		/// This may or may not result in a new XmlDocument.
		/// </summary>
		private static XmlDocument RemoveXmnlsAttributesFromCatalogEntries(XmlDocument catalog)
		{
			// 
			var outerXml = catalog.OuterXml;
			var idxFirst = outerXml.IndexOf("<entry", StringComparison.InvariantCulture);
			if (idxFirst > 0)
			{
				var head = outerXml.Substring(0, idxFirst);
				var tail = outerXml.Substring(idxFirst).Replace(" xmlns=\"\"", "")
					.Replace(" xmlns:dc=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:dcterms=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:lrmi=\"http://purl.org/dcx/lrmi-terms/\"", "")
					.Replace(" xmlns:opds=\"http://opds-spec.org/2010/catalog\"", "");
				catalog = new XmlDocument();
				catalog.PreserveWhitespace = true;
				catalog.LoadXml(head + tail);
			}
			return catalog;
		}

		private void LoadCatalogFromRootPage(XmlDocument firstPageOfCatalog, XmlDocument filteredCatalog, string xpath, ref int entryCount, ref int filteredEntryCount)
		{
			var pageOfCatalog = firstPageOfCatalog;
			while (!_options.DryRun)
			{
				var selfLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='self']", _nsmgr) as XmlElement;
				var nextLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='next']", _nsmgr) as XmlElement;
				var lastLink = pageOfCatalog.DocumentElement.SelectSingleNode("/a:feed/a:link[@rel='last']", _nsmgr) as XmlElement;
				entryCount += AddEntriesToFilteredCatalog(pageOfCatalog, xpath, _nsmgr, filteredCatalog, ref filteredEntryCount);
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

		public XmlDocument GetRootPage(out XmlNamespaceManager nsmgr, out string feedTitle)
		{
			var doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			string catalogUrl;

			if (_options.Source != null)
				catalogUrl = (Sources.First(src => src.Code == _options.Source.ToLowerInvariant())).Url;
			else
				catalogUrl = _options.Url;
			if (!String.IsNullOrWhiteSpace(catalogUrl))
			{
				var data = GetOpdsPage(catalogUrl);
				doc.LoadXml(data);
			}
			else
			{
				doc.Load(_options.CatalogFile);
			}

			nsmgr = CreateNameSpaceManagerForOpdsDocument(doc);
			_nsmgr = nsmgr;

			var title = doc.SelectSingleNode("/a:feed/a:title", nsmgr);
			if (title != null)
				feedTitle = title.InnerText.Trim();
			else
				feedTitle = null;
			FeedTitle = feedTitle;
			if (_options.VeryVerbose)
				Console.WriteLine("INFO: catalog title = {0}", feedTitle);

			return doc;
		}

		private int AddEntriesToFilteredCatalog(XmlDocument catalogPage, string xpath, XmlNamespaceManager nsmgr, XmlDocument langCatalog,
			ref int filteredCount)
		{
			var entries = catalogPage.DocumentElement.SelectNodes(xpath, nsmgr);
			filteredCount += AddEntriesToFilteredCatalog(entries, langCatalog);
			return entries.Count;
		}

		private int AddEntriesToFilteredCatalog(XmlNodeList entries, XmlDocument filteredCatalog)
		{
			var newline = Environment.NewLine;
			int addedEntries = 0;
			foreach (var entry in entries.Cast<XmlElement>())
			{
				if (FilterForPublisher(entry))
					continue;
				// If entry is actually the language element, use the parent element.  Otherwise, add a language element to the end of the inner XML.
				string innerXml;
				if (entry.LocalName == "language")
					innerXml = entry.ParentNode.InnerXml;
				else
					innerXml = $"{newline}{entry.InnerXml}{newline}<dcterms:language xmlns:dcterms='http://purl.org/dc/terms/'>{_options.LanguageName}</dcterms:language>{newline}";
				var newEntry = filteredCatalog.CreateElement("entry", "http://www.w3.org/2005/Atom");
				innerXml = innerXml.Replace(" xmlns=\"http://www.w3.org/2005/Atom\"", "");
				innerXml = innerXml.Replace("><", ">" + Environment.NewLine + "<");
				newEntry.InnerXml = innerXml;
				filteredCatalog.DocumentElement.AppendChild(newEntry);
				filteredCatalog.DocumentElement.AppendChild(filteredCatalog.CreateTextNode(Environment.NewLine));
				++addedEntries;
			}
			if (_options.VeryVerbose && !String.IsNullOrWhiteSpace(_options.Publisher))
				Console.WriteLine("DEBUG: added {0} of {1} entries for {2}", addedEntries, entries.Count, _options.Publisher);
			return addedEntries;
		}

		private bool FilterForPublisher(XmlElement entry)
		{
			if (String.IsNullOrWhiteSpace(_options.Publisher))
				return false;
			if (entry.LocalName == "language")
				entry = entry.ParentNode as XmlElement;
			var publisherNode = entry.SelectSingleNode("./dc:publisher", _nsmgr);
			if (publisherNode == null)
				publisherNode = entry.SelectSingleNode("./dcterms:publisher", _nsmgr);
			if (publisherNode == null)
			{
				Console.WriteLine("WARNING: cannot find publisher for catalog entry!");
				Console.WriteLine(entry.OuterXml);
				return true;
			}
			return publisherNode.InnerText.ToLowerInvariant() != _options.Publisher.ToLowerInvariant();
		}

		public void WriteCatalogEntryFile(XmlDocument rootCatalog, XmlElement bookEntry, string path)
		{
			if (_options.Verbose)
				Console.WriteLine("INFO: writing catalog entry file {0}", path);
			string content = CreateCatalogEntryFileAsString(rootCatalog, bookEntry);
			File.WriteAllText(path, content);
		}

		public string CreateCatalogEntryFileAsString(XmlDocument rootCatalog, XmlElement bookEntry)
		{
			var bldr = new StringBuilder();
			bldr.AppendLine("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>");
			var titleNode = rootCatalog.SelectSingleNode("/a:feed/a:title", _nsmgr) as XmlElement;
			if (titleNode != null)
			{
				var title = Regex.Replace(titleNode.InnerText, " \\(extract for .*\\)$", "");
				bldr.AppendLine($"<title>{title} [extract]</title>");
			}
			var entry = Regex.Replace(bookEntry.OuterXml, " xmlns[:a-zA-Z]*=[\"'][^\"']+[\"']", "");
			bldr.AppendLine(entry);
			bldr.AppendLine("</feed>");
			var content = bldr.ToString();
			return content;
		}

		public int DownloadBook(XmlElement bookEntry, string type, string feedTitle, string path)
		{
			var apptype = (type == "epub") ? "epub+zip" : type;
			if (feedTitle != null && feedTitle.ToLowerInvariant().Contains("storyweaver"))
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
			{
				var folder = Path.GetDirectoryName(path);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				File.WriteAllBytes(path, bytes);
			}
			return 0;
		}

		public int DownloadThumbnail(XmlElement bookEntry, string path)
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
			return DownloadImageFile(href, path);
		}

		public int DownloadImage(XmlElement bookEntry, string path)
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

		/// <summary>
		/// Utility method for creating a namespace manager that can handle either GDL or StoryWeaver catalog
		/// files.
		/// </summary>
		public static XmlNamespaceManager CreateNameSpaceManagerForOpdsDocument(XmlDocument opdsDoc)
		{
			var nsmgr = new XmlNamespaceManager(opdsDoc.NameTable);
			nsmgr.AddNamespace("lrmi", "http://purl.org/dcx/lrmi-terms/");
			nsmgr.AddNamespace("opds", "http://opds-spec.org/2010/catalog");
			nsmgr.AddNamespace("dc", "http://purl.org/dc/terms/");
			nsmgr.AddNamespace("dcterms", "http://purl.org/dc/terms/");
			nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");
			return nsmgr;
		}
	}
}
