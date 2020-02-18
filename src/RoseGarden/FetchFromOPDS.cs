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
	public class FetchFromOPDS
	{
		private HttpClient _client = new HttpClient();
		private OpdsClient _opdsClient;
		private XmlDocument _rootCatalog;
		private XmlDocument _langCatalog = new XmlDocument();
		private XmlNamespaceManager _nsmgr;
		private FetchOptions _options;
		private string _feedTitle;

		public FetchFromOPDS(FetchOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			_langCatalog.PreserveWhitespace = true;
			_opdsClient = new OpdsClient(opts);
		}

		public int RunFetch()
		{
			if (!VerifyOptions())
				return 1;

			if (_options.DryRun)
				Console.WriteLine("DRY RUN MESSAGES");
			_rootCatalog = _opdsClient.GetRootPage(out _nsmgr, out _feedTitle);
			// Fill in the language specific catalog if the language name is given.
			if (!String.IsNullOrWhiteSpace(_options.LanguageName))
				_opdsClient.GetCatalogForLanguage(_rootCatalog, out _langCatalog);
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
				if (!OpdsClient.Sources.Any(s => s.Code == _options.Source.ToLowerInvariant()))
				{
					Console.Write("The -s (--source) option recognizes only these codes:");
					foreach (var src in OpdsClient.Sources)
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
				// The actual returned file may well be jpeg even if advertised as image/png, so assume jpeg until we
				// see the data.
				string path;
				if (String.IsNullOrEmpty(_options.OutputFile))
					path = Path.Combine(OpdsClient.DownloadFolder, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".thumb.jpg");
				else
					path = Path.ChangeExtension(_options.OutputFile, "thumb.jpg");
				ret = _opdsClient.DownloadThumbnail(bookEntry, path);
				if (ret != 0)
					return ret;
			}
			if (_options.DownloadImage)
			{
				// The actual returned file may well be jpeg even if advertised as image/png, so assume jpeg until we
				// see the data.
				string path;
				if (String.IsNullOrEmpty(_options.OutputFile))
					path = Path.Combine(OpdsClient.DownloadFolder, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".jpg");
				else
					path = Path.ChangeExtension(_options.OutputFile, "jpg");
				ret = _opdsClient.DownloadImage(bookEntry, path);
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
				path = Path.Combine(OpdsClient.DownloadFolder, Program.SanitizeNameForFileSystem(_options.BookTitle) + ".opds");
			else
				path = Path.ChangeExtension(_options.OutputFile, "opds");
			_opdsClient.WriteCatalogEntryFile(_rootCatalog, bookEntry, path);
		}


		public int DownloadBook(XmlElement bookEntry, string type)
		{
			string path;
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				path = Path.Combine(OpdsClient.DownloadFolder, Program.SanitizeNameForFileSystem(_options.BookTitle) + "." + type);
			}
			else
			{
				path = _options.OutputFile;
				if (type == "pdf")
					path = Path.ChangeExtension(path, type);
			}
			return _opdsClient.DownloadBook(bookEntry, type, _feedTitle, path);
		}
	}
}
