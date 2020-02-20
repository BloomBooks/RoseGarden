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

		public int GetCatalogForLanguage(XmlDocument rootCatalog, out XmlDocument langCatalog)
		{
			if (_options.Verbose)
				Console.WriteLine("Extracting a catalog specifically for {0}", _options.LanguageName);
			langCatalog = new XmlDocument();
			langCatalog.PreserveWhitespace = true;
			langCatalog.LoadXml("<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/'" +
				" xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/'" +
				" xmlns:opds='http://opds-spec.org/2010/catalog'>" + Environment.NewLine + "</feed>");
			if (!String.IsNullOrWhiteSpace(FeedTitle))
			{
				var title = langCatalog.CreateElement("title", "http://www.w3.org/2005/Atom");
				title.InnerText = $"{FeedTitle} (extract for {_options.LanguageName})";
				langCatalog.DocumentElement.AppendChild(title);
			}
			XmlElement selfLink;
			XmlElement nextLink;
			XmlElement lastLink;
			var xpath = $"//a:entry/dcterms:language[text()='{_options.LanguageName}']";  // actually gets child of entry...
			var entries = rootCatalog.DocumentElement.SelectNodes(xpath, _nsmgr);
			int entryCount = 0;
			XmlDocument pageOfCatalog;
			string hrefLink = null;
			if (entries.Count == 0)
			{
				entries = null;
				// may need to pull a different root OPDS page: look for a link for the language
				var link = rootCatalog.DocumentElement.SelectSingleNode($"/a:feed/a:link[@rel='http://opds-spec.org/facet' and @opds:facetGroup='Languages' and contains(@title,'{_options.LanguageName}')]", _nsmgr) as XmlElement;
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
					pageOfCatalog = rootCatalog;
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
				pageOfCatalog = rootCatalog;
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
					entryCount += AddEntriesToLangCatalog(pageOfCatalog, xpath, _nsmgr, langCatalog);
				}
				else
				{
					AddEntriesToLangCatalog(entries, langCatalog);
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
			var outerXml = langCatalog.OuterXml;
			var idxFirst = outerXml.IndexOf("<entry", StringComparison.InvariantCulture);
			if (idxFirst > 0)
			{
				var head = outerXml.Substring(0, idxFirst);
				var tail = outerXml.Substring(idxFirst).Replace(" xmlns=\"\"", "")
					.Replace(" xmlns:dc=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:dcterms=\"http://purl.org/dc/terms/\"", "")
					.Replace(" xmlns:lrmi=\"http://purl.org/dcx/lrmi-terms/\"", "")
					.Replace(" xmlns:opds=\"http://opds-spec.org/2010/catalog\"", "");
				langCatalog = new XmlDocument();
				langCatalog.PreserveWhitespace = true;
				langCatalog.LoadXml(head + tail);
			}
			return 0;
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

			nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("lrmi", "http://purl.org/dcx/lrmi-terms/");
			nsmgr.AddNamespace("opds", "http://opds-spec.org/2010/catalog");
			nsmgr.AddNamespace("dc", "http://purl.org/dc/terms/");
			nsmgr.AddNamespace("dcterms", "http://purl.org/dc/terms/");
			nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");
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

		private int AddEntriesToLangCatalog(XmlDocument catalogPage, string xpath, XmlNamespaceManager nsmgr, XmlDocument langCatalog)
		{
			var entries = catalogPage.DocumentElement.SelectNodes(xpath, nsmgr);
			AddEntriesToLangCatalog(entries, langCatalog);
			return entries.Count;
		}

		private void AddEntriesToLangCatalog(XmlNodeList entries, XmlDocument langCatalog)
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
				var newEntry = langCatalog.CreateElement("entry", "http://www.w3.org/2005/Atom");
				innerXml = innerXml.Replace(" xmlns=\"http://www.w3.org/2005/Atom\"", "");
				innerXml = innerXml.Replace("><", ">" + Environment.NewLine + "<");
				newEntry.InnerXml = innerXml;
				langCatalog.DocumentElement.AppendChild(newEntry);
				langCatalog.DocumentElement.AppendChild(langCatalog.CreateTextNode(Environment.NewLine));
			}
		}

		public void WriteCatalogEntryFile(XmlDocument rootCatalog, XmlElement bookEntry, string path)
		{
			if (_options.Verbose)
				Console.WriteLine("INFO: writing catalog entry file {0}", path);
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
			File.WriteAllText(path, bldr.ToString());
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
				File.WriteAllBytes(path, bytes);
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
	}
}
