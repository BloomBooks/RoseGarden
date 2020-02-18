// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml;
using RoseGarden.Parse;
using RoseGarden.Parse.Model;

namespace RoseGarden
{
	/// <summary>
	/// Class for batch processing books from an OPDS catalog, either an online catalog or a local
	/// catalog file.  The basic approach is as follows:
	/// 1) Find which books need to be converted, either updated or for the first time.
	/// 2) Fetch and convert those books (epubs and image files).
	/// 3) Upload the converted books.
	/// </summary>
	/// <remarks>
	/// Possible enhancements:
	/// Limit number of books to process at a time, with some sort of record of how many/which
	/// books were processed so that the program can start there the next time.
	/// </remarks>
	public class BatchProcessBooks
	{
		private BatchOptions _options;

		private Dictionary<string, Book> _bloomlibraryBooks = new Dictionary<string, Book>();
		private OpdsClient _opdsClient;
		private XmlDocument _rootCatalog;
		private XmlDocument _langCatalog;
		private XmlNamespaceManager _nsmgr;
		private string _feedTitle;

		public BatchProcessBooks(BatchOptions opts)
		{
			this._options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			_opdsClient = new OpdsClient(opts);
		}

		/// <summary>
		/// Runs the batch process.
		/// 1) Get the list of imported books with their import data from bloomlibrary's book table.
		/// 2) If the catalog is online, load it, possibly filtered by language.
		/// 3) For each book in the catalog, check to see if it has been updated since it was uploaded
		///    to bloomlibrary.  Also check to see if RoseGarden has been updated significantly since
		///    the book was uploaded and whether it has never been uploaded.  Any one of these three
		///    conditions is sufficient to include the book in the batch process.
		/// 4) Download the epub and image files for all of the batched books.  (Image files can be
		///    ignored for African Storybook Project books since we use page 1 images for those.)
		/// 5) Convert each of the books, cleverly sorting them into bookshelves and language folders.
		/// 6) Upload the newly converted books.
		/// </summary>
		/// <returns>The batch.</returns>
		public int RunBatch()
		{
			if (!VerifyOptions())
				return 1;
			_bloomlibraryBooks = ParseClient.LoadBloomLibraryInfo(_options.UploadUser, _options.UploadPassword);
			var dryrun = _options.DryRun;
			_options.DryRun = false;    // we really want the catalog even though we may not fetch, convert, or upload any books.
			LoadCatalog();
			_options.DryRun = dryrun;
			var entries = GetEntriesToProcess();
			if (entries.Count == 0)
			{
				Console.WriteLine("INFO: No books need to be updated or converted.");
				return 0;
			}
			DownloadBooks(entries);
			ConvertBooks(entries);
			UploadBooks();
			return 0;
		}

		private void DownloadBooks(List<XmlElement> entries)
		{
			if (_options.DryRun)
			{
				foreach (var entry in entries)
					Console.WriteLine(CreateFetchCommandLine(entry));
				return;
			}
			foreach (var entry in entries)
				FetchBook(entry);
		}

		/// <summary>
		/// Fetch the epub and main image file for the given book and write them to the standard RoseGarden/Downloads folder.
		/// Also write the catalog entry as a single-entry OPDS catalog file.
		/// </summary>
		private void FetchBook(XmlElement entry)
		{
			var titleXml = entry.SelectSingleNode("./a:title", _nsmgr) as XmlElement;
			if (titleXml == null)
			{
				Console.WriteLine("WARNING: cannot find the title in the OPDS entry?! {0}", entry.OuterXml);
				Environment.Exit(2);
			}
			var title = titleXml.InnerText.Trim();
			var pathEpub = Path.Combine(OpdsClient.DownloadFolder, title, ".epub");
			_opdsClient.DownloadBook(entry, "epub", _feedTitle, pathEpub);
			var pathImage = Path.ChangeExtension(pathEpub, "jpg");
			_opdsClient.DownloadImage(entry, pathImage);
			var pathOpds = Path.ChangeExtension(pathEpub, "opds");
			_opdsClient.WriteCatalogEntryFile(_rootCatalog, entry, pathOpds);
		}

		private string CreateFetchCommandLine(XmlElement entry)
		{
			var command = new StringBuilder();
			command.Append("RoseGarden fetch -v --image");
			if (!String.IsNullOrWhiteSpace(_options.CatalogFile))
			{
				command.AppendFormat(" -c \"{0}\"", _options.CatalogFile);
			}
			else if (!String.IsNullOrWhiteSpace(_options.Source))
			{
				command.AppendFormat(" -s {0}", _options.Source);
			}
			else if (!String.IsNullOrWhiteSpace(_options.Url))
			{
				command.AppendFormat(" -u \"{0}\"", _options.Url);
			}
			else
			{
				Console.WriteLine("ERROR: RoseGarden should have detected invalid input arguments before now: need one of -c, -s, or -u!");
				Environment.Exit(2);
			}
			if (!String.IsNullOrWhiteSpace(_options.AccessToken))
				command.AppendFormat(" -k \"{0}\"", _options.AccessToken);
			if (!String.IsNullOrWhiteSpace(_options.LanguageName))
				command.AppendFormat(" -l \"{0}\"", _options.LanguageName);
			var titleXml = entry.SelectSingleNode("./a:title", _nsmgr);
			// There may be multiple authors, but we assume that finding one is good enough to match/disambiguate the title.
			var authorXml = entry.SelectSingleNode("./a:author/a:name", _nsmgr) as XmlElement;
			if (titleXml == null)
			{
				Console.WriteLine("ERROR: catalog entry does not have a title field?!  {0}", entry.OuterXml);
				Environment.Exit(2);
			}
			command.AppendFormat(" -t \"{0}\"", titleXml.InnerText.Trim());
			if (authorXml != null && !String.IsNullOrWhiteSpace(authorXml.InnerText))
			{
				command.AppendFormat(" -a \"{0}\"", authorXml.InnerText.Trim());
			}
			return command.ToString();
		}

		private void ConvertBooks(List<XmlElement> entries)
		{
			throw new NotImplementedException();
		}

		private void UploadBooks()
		{
			throw new NotImplementedException();
		}

		private List<XmlElement> GetEntriesToProcess()
		{
			var catalog = String.IsNullOrWhiteSpace(_options.LanguageName) ? _rootCatalog : _langCatalog;
			var allEntries = catalog.DocumentElement.SelectNodes($"/a:feed/a:entry", _nsmgr).Cast<XmlElement>().ToList();
			int majorVersion;
			int minorVersion;
			Program.GetVersionNumbers(out majorVersion, out minorVersion);
			var entriesToProcess = new List<XmlElement>();
			foreach (var entry in allEntries)
			{
				var link = entry.SelectSingleNode("./a:link[contains(@rel, 'http://opds-spec.org/acquisition') and @type='application/epub+zip']", _nsmgr) as XmlElement;
				if (link == null)
					continue;
				var epubPath = link.GetAttribute("href");
				if (String.IsNullOrWhiteSpace(epubPath))
					continue;
				Book book;
				if (_bloomlibraryBooks.TryGetValue(epubPath, out book))
				{
					// We have this book already, but is either the book or RoseGarden newer than before?
					// The updated element should exist: it does in both the GDL and StoryWeaver catalog entries.
					// But we'll check a couple of other possible fields in the entry just in case.
					var timestampXml = entry.SelectSingleNode("./a:updated", _nsmgr) as XmlElement;
					if (timestampXml == null)
						timestampXml = entry.SelectSingleNode("./a:published", _nsmgr) as XmlElement;
					if (timestampXml == null)
						timestampXml = entry.SelectSingleNode("./dc:created", _nsmgr) as XmlElement;
					DateTime catalogUpdated = DateTime.MinValue;
					if (timestampXml != null)
						catalogUpdated = DateTime.Parse(timestampXml.InnerText);
					if (book.LastUploaded == null ||
						book.LastUploaded.UtcTime < catalogUpdated ||
						book.ImporterMajorVersion < majorVersion ||
						(book.ImporterMajorVersion == majorVersion && book.ImporterMinorVersion < minorVersion))
					{
						entriesToProcess.Add(entry);
						if (_options.VeryVerbose)
							Console.WriteLine("{0} is already imported, but needs to be updated.", book.Title);
					}
					else
					{
						if (_options.VeryVerbose)
							Console.WriteLine("{0} is already converted and still up to date with RoseGarden and the catalog entry.", book.Title);
					}
				}
				else
				{
					// We don't have this book yet: add it to the collection for processing.
					entriesToProcess.Add(entry);
				}
			}
			return entriesToProcess;
		}

		private void LoadCatalog()
		{
			_rootCatalog = _opdsClient.GetRootPage(out _nsmgr, out _feedTitle);
			if (!String.IsNullOrWhiteSpace(_options.LanguageName))
			{
				int res = _opdsClient.GetCatalogForLanguage(_rootCatalog, out _langCatalog);
				if (res != 0)
				{
					Console.WriteLine("ERROR: creating the language specific catalog failed!");
					Environment.Exit(res);
				}
			}
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
			if (!String.IsNullOrWhiteSpace(_options.CatalogFile) && File.Exists(_options.CatalogFile))
				++urlCount;
			if (urlCount != 1)
			{
				Console.WriteLine("Exactly one of -u (--url), -s (--source), or -c (--catalog) must be given.");
				allValid = false;
			}
			if (!File.Exists(_options.BloomExe))
			{
				Console.WriteLine("WARNING: {0} does not exist!", _options.BloomExe);
				allValid = false;
			}
			if (!Directory.Exists(_options.BookShelfContainer))
			{
				Console.WriteLine("WARNING: {0} does not exist!", _options.BookShelfContainer);
				allValid = false;
			}
			if (String.IsNullOrWhiteSpace(_options.UploadUser))
			{
				_options.UploadUser = Program.GetEnvironmentVariable("RoseGardenUserName");
				if (String.IsNullOrWhiteSpace(_options.UploadUser))
				{
					Console.WriteLine("WARNING: the user name must be provided by -U (--user) or by the RoseGardenUserName environment variable.");
					allValid = false;
				}
			}
			if (String.IsNullOrWhiteSpace(_options.UploadPassword))
			{
				_options.UploadPassword = Program.GetEnvironmentVariable("RoseGardenUserPassword");
				if (String.IsNullOrWhiteSpace(_options.UploadPassword))
				{
					Console.WriteLine("WARNING: the user name must be provided by -P (--password) or by the RoseGardenUserPassword environment variable.");
					allValid = false;
				}
			}
			return allValid;
		}
	}
}