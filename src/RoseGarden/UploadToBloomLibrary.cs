// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using RoseGarden.Parse;
using RoseGarden.Parse.Model;

namespace RoseGarden
{
	public class UploadToBloomLibrary
	{
		private UploadOptions _options;
		private readonly HashSet<string> _previouslyLoadedBooks = new HashSet<string>();
		private int _majorVersion;
		private int _minorVersion;

		public UploadToBloomLibrary(UploadOptions opts)
		{
			_options = opts;
			if (_options.VeryVerbose)
				_options.Verbose = true;
		}

		internal int RunUpload()
		{
			if (!VerifyOptions())
				return 1;
			PrepareForUpload();
			var process = new Process
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = _options.BloomExe,
					Arguments = $"upload {(_options.SingleBookshelfLevel? "--singleBookshelfLevel" : "")} --preserveThumbnails -u \"{_options.UploadUser}\" -p \"{_options.UploadPassword}\" \"{_options.BookShelfContainer}\"",
					UseShellExecute = false,
					CreateNoWindow = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				},
			};
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: upload arguments={0}", process.StartInfo.Arguments);
			process.Start();
			process.WaitForExit();

			var standardOutput = process.StandardOutput.ReadToEnd();
			var standardError = process.StandardError.ReadToEnd();
			if (!String.IsNullOrWhiteSpace(standardError))
			{
				Console.WriteLine("WARNING: uploading {0} produced the following error messages (with exit code {1}):",
					_options.BookShelfContainer, process.ExitCode);
				Console.WriteLine(standardError);
			}
			if (_options.VeryVerbose && !String.IsNullOrWhiteSpace(standardOutput))
			{
				Console.WriteLine("DEBUG: uploading {0} produced this output (with exit code {1}):", _options.BookShelfContainer, process.ExitCode);
				Console.WriteLine(standardOutput);
			}
			else if (_options.Verbose)
			{
				Console.WriteLine("INFO: uploading {0} has finished with exit code {1}", _options.BookShelfContainer, process.ExitCode);
			}

			UpdateParseTables();

			return process.ExitCode;
		}

		private bool VerifyOptions()
		{
			var allValid = true;
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

		/// <summary>
		/// Check the books beneath the upload folder against those in BloomBulkUploadLog.txt in that folder,
		/// checking RoseGarden version numbers and upload timestamps against OPDS update timestamps.  If
		/// any books that have already been uploaded actually do need to be updated, remove them from the
		/// BloomBulkUploadLog.txt file.
		/// Keep a record of which books remain in BloomBulkUploadLog.txt so that we don't try to update
		/// their parse records after the upload finishes.
		/// </summary>
		private void PrepareForUpload()
		{
			var bookDirs = ReadBloomBulkUploadLogFile();
			foreach (var dir in bookDirs)
			{
				if (IsValidFolder(dir))
					_previouslyLoadedBooks.Add(dir);
			}
			GetBookInformationFromBookFolders(_previouslyLoadedBooks, out Dictionary<string, string> instanceIdToTitle, out Dictionary<string, XmlDocument> instanceIdToOpds,
				out Dictionary<string, string> instanceIdToFolder);
			Program.GetVersionNumbers(out _majorVersion, out _minorVersion);
			ParseClient parseClient = new ParseClient(_options.UploadUser, _options.UploadPassword);
			string importedFilter = "{\"importedBookSourceUrl\": {\"$regex\": \".\"}}";
			IEnumerable<Book> bookList = parseClient.GetBooks(importedFilter, new[] { "uploader" });
			int updatedBooks = 0;
			var uploadedInstances = new HashSet<string>();
			// Check for books that are newer and thus need to be re-uploaded.
			foreach (var book in bookList)
			{
				uploadedInstances.Add(book.BookInstanceId);
				if (instanceIdToTitle.TryGetValue(book.BookInstanceId, out string localTitle) &&
					instanceIdToOpds.TryGetValue(book.BookInstanceId, out XmlDocument opdsEntry) &&
					instanceIdToFolder.TryGetValue(book.BookInstanceId, out string dir))
				{
					// First check if RoseGarden is a newer version: if so, updating the book is indicated.
					var needUpdate = book.ImporterName == "RoseGarden" &&
						(book.ImporterMajorVersion < _majorVersion ||
						(book.ImporterMajorVersion == _majorVersion && book.ImporterMinorVersion < _minorVersion));
					if (!needUpdate)
					{
						// Check if the book source is newer than the uploaded book: if so, updating the book is indicated.
						var nsmgrOpds = OpdsClient.CreateNameSpaceManagerForOpdsDocument(opdsEntry);
						var updateNode = opdsEntry.SelectSingleNode("/a:feed/a:entry/a:updated", nsmgrOpds) as XmlElement;
						if (updateNode != null)
						{
							var bookUpdated = updateNode.InnerText.Trim();
							needUpdate = string.Compare(book.LastUploaded.Iso, bookUpdated, StringComparison.InvariantCulture) < 0;
						}
					}
					if (needUpdate)
					{
						_previouslyLoadedBooks.Remove(dir);
						++updatedBooks;
						if (_options.VeryVerbose)
							Console.WriteLine("DEBUG: will re-upload \"{0}\" because it appears to be newer.", Path.GetFileName(dir));
					}
				}
			}
			// Check for books that have been deleted and thus need to be re-uploaded.
			foreach (var instance in instanceIdToFolder.Keys)
			{
				if (!uploadedInstances.Contains(instance))
				{
					// Book no longer exists in the library: we need to reload it!
					var dir = instanceIdToFolder[instance];
					_previouslyLoadedBooks.Remove(dir);
					++updatedBooks;
					if (_options.VeryVerbose)
						Console.WriteLine("DEBUG: will re-upload \"{0}\" because it does not exist in bloomlibrary.", Path.GetFileName(dir));
				}
			}
			if (updatedBooks > 0)
			{
				var logfile = GetBloomBulkUploadLogFilePath();
				File.Delete(logfile);
				if (_previouslyLoadedBooks.Count > 0)
				{
					var lines = new List<string>();
					lines.AddRange(_previouslyLoadedBooks);
					lines.Add("");
					lines.Add("");
					lines.Add("All finished!");
					lines.Add("In order to repeat the uploading, this file will need to be deleted.");
					File.WriteAllLines(logfile, lines);
				}
			}
		}

		private bool IsValidFolder(string directoryPath)
		{
			return !String.IsNullOrWhiteSpace(directoryPath) &&
				!directoryPath.StartsWith("All finished", StringComparison.InvariantCulture) &&
				!directoryPath.StartsWith("In order to repeat", StringComparison.InvariantCulture) &&
				Directory.Exists(directoryPath);
		}

		public Dictionary<string, List<Book>> _allBooks = new Dictionary<string, List<Book>>();
		private void LoadAllBooks(ParseClient client)
		{
			var startTime = DateTime.Now;
			IEnumerable<Book> bookList = client.GetBooks("", new[] { "uploader" });
			var countAllBooks = 0;
			var countTitles = 0;
			foreach (var book in bookList)
			{
				++countAllBooks;
				var normTitle = Program.NormalizeTitle(book.Title);
				if (!_allBooks.TryGetValue(normTitle, out List<Book> books))
				{
					++countTitles;
					books = new List<Book>();
					_allBooks.Add(normTitle, books);
				}
				books.Add(book);
			}
			var endTime = DateTime.Now;
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: {0} books with {1} distinct titles processed in {2}", countAllBooks, countTitles, (endTime - startTime));
		}

		private void UpdateParseTables()
		{
			var bookDirs = ReadBloomBulkUploadLogFile();
			GetBookInformationFromBookFolders(bookDirs, out Dictionary<string, string> instanceIdToTitle, out Dictionary<string, XmlDocument> instanceIdToOpds,
				out Dictionary<string, string> instanceIdToFolder);
			ParseClient parseClient = new ParseClient(_options.UploadUser, _options.UploadPassword);
			LoadAllBooks(parseClient);
			string importedFilter = "{\"importedBookSourceUrl\": {\"$regex\": \".\"}}";
			IEnumerable<Book> bookList = parseClient.GetBooks(importedFilter, new[] { "uploader" });
			var uploadDate = new Date(DateTime.Now.ToUniversalTime());
			var updateJsonBase = String.Format("{{ \"{0}\":\"{1}\", \"{2}\":{3}, \"{4}\":{5}, \"{6}\":\"{7}\", \"{8}\": {9}",
				Book.kImporterNameField, "RoseGarden",              // possibly unneeded, but loudly claim RoseGarden did the import
				Book.kImporterMajorVersionField, _majorVersion,      // version stamp so we can update for new versions of RoseGarden
				Book.kImporterMinorVersionField, _minorVersion,
				"updateSource", "importerbot@bloomlibrary.org",     // very important so we don't add system:Incoming tag
				"lastUploaded", uploadDate.ToJson()                 // timestamp so we can check later for books modified on ODPS source
				);
			foreach (var book in bookList)
			{
				if (instanceIdToTitle.TryGetValue(book.BookInstanceId, out string localTitle) &&
					instanceIdToOpds.TryGetValue(book.BookInstanceId, out XmlDocument opdsEntry) &&
					instanceIdToFolder.TryGetValue(book.BookInstanceId, out string folder) &&
					!_previouslyLoadedBooks.Contains(folder))       // If we didn't reupload, don't try to update table.
				{
					var needUpdate = book.ImporterName != "RoseGarden" || book.ImporterMajorVersion != _majorVersion || book.ImporterMinorVersion != _minorVersion;
					if (localTitle != book.Title)
						Console.WriteLine("WARNING: mismatch in titles from local to parse server: \"{0}\" vs \"{1}\"", localTitle, book.Title);
					var updateJsonBldr = new StringBuilder(updateJsonBase);
					var updateTags = false;
					// Matching titles that vary in case or whitespace doesn't seem feasible using
					// parse queries.  So we preload everything locally and index by a normalized
					// title to find matching titles.
					var matchingBooks = FindBooksWithMatchingTitle(Program.NormalizeTitle(book.Title));
					var related = new HashSet<Book>();
					foreach (var oldBook in matchingBooks)
					{
						if (oldBook.ObjectId == book.ObjectId)
							continue;   // a better query would avoid this check
						if (SameAuthor(book, oldBook) && SameBookshelf(book, oldBook))
						{
							if (_options.VeryVerbose)
								Console.WriteLine("DEBUG: found matching book for \"{0}\"", book.Title);
							if (book.InCirculation != false && oldBook.InCirculation != false)
							{
								book.InCirculation = false;
								updateJsonBldr.Append(", \"inCirculation\":false");
								needUpdate = true;
							}
							// add tag for librarian?
							if (!book.Tags.Contains("todo:check duplicate import"))
							{
								book.Tags.Add("todo:check duplicate import");
								updateTags = true;
							}
							related.Add(oldBook);
						}
					}
					if (book.Tags != null)
					{
						if (_options.VeryVerbose)
						{
							foreach (var tag in book.Tags)
								Console.WriteLine("DEBUG: initial parse books table tags: tag=\"{0}\"", tag);
						}
						if (book.Tags.Contains("system:Incoming"))
						{
							updateTags = true;
							book.Tags.Remove("system:Incoming");
						}
						var nsmgrOpds = OpdsClient.CreateNameSpaceManagerForOpdsDocument(opdsEntry);
						var levelNode = opdsEntry.SelectSingleNode("/a:feed/a:entry/lrmi:educationalAlignment[@alignmentType='readingLevel']", nsmgrOpds) as XmlElement;
						var level = levelNode?.GetAttribute("targetName");
						if (!String.IsNullOrWhiteSpace(level))
						{
							var levelTag = GetTagForLrmiReadingLevel(level);
							if (!book.Tags.Contains(levelTag))
							{
								// This removal step may just be paranoid.  But I think there should always be at most one level tag!
								foreach (var tag in book.Tags)
								{
									if (tag.StartsWith("level:", StringComparison.InvariantCulture))
									{
										book.Tags.Remove(tag);
										break;
									}
								}
								updateTags = true;
								book.Tags.Add(levelTag);
							}
						}
						if (updateTags)
						{
							updateJsonBldr.Append(", \"tags\":[");
							var sep = "";
							foreach (var tag in book.Tags)
							{
								updateJsonBldr.AppendFormat("{0}\"{1}\"", sep, tag);
								sep = ",";
							}
							updateJsonBldr.Append("]");
							needUpdate = true;
						}
					}
					if (needUpdate)
					{
						if (_options.Verbose)
							Console.WriteLine("INFO: updating bloomlibrary books table with RoseGarden importer values for {0}", book.Title);
						updateJsonBldr.Append(" }");
						if (_options.VeryVerbose)
							Console.WriteLine("DEBUG: updateJson={0}", updateJsonBldr);
						var response = parseClient.UpdateObject("books", book.ObjectId, updateJsonBldr.ToString());
						if (response.StatusCode != System.Net.HttpStatusCode.OK)
							Console.WriteLine("WARNING: updating the book table for \"{0}\" failed: {1}", book.Title, response.Content);
					}

					if (related.Count > 0)
					{
						FixRelatedBooksTable(book, related, parseClient);
					}
				}
			}
		}

		private List<Book> FindBooksWithMatchingTitle(string normTitle)
		{
			if (_allBooks.TryGetValue(normTitle, out List<Book> books))
				return books;
			return new List<Book>();
		}

		private void FixRelatedBooksTable(Book book, HashSet<Book> related, ParseClient parseClient)
		{
			related.Add(book);
			if (related.Count == 1)
				return;
			var currentRelatedList = parseClient.GetRelatedBooks(book.ObjectId);
			var objectsToRemove = new HashSet<string>();
			foreach (var currentRelated in currentRelatedList)
			{
				if (SameBookList(related, currentRelated.Books))
					return;
				foreach (var obj in currentRelated.Books)
					objectsToRemove.Add(obj.ObjectId);
			}
			foreach (var objId in objectsToRemove)
				RemoveObjectFromRelatedBooksEntry(objId, parseClient);
			CreateRelatedBooksEntry(related, parseClient);
		}

		private void RemoveObjectFromRelatedBooksEntry(string objId, ParseClient parseClient)
		{
			var currentRelatedList = parseClient.GetRelatedBooks(objId);
			var bookSep = "";
			foreach (var currentRelated in currentRelatedList)
			{
				var bldr = new StringBuilder("{");
				bldr.AppendFormat("\"objectId\":\"{0}\", \"books\":[", currentRelated.ObjectId);
				foreach (var book in currentRelated.Books)
				{
					if (book.ObjectId == objId)
						continue;
					bldr.AppendFormat("{0}{{\"__type\":\"Pointer\", \"className\":\"books\", \"objectId\":\"{1}\"}}", bookSep, book.ObjectId);
					bookSep = ", ";
				}
				bldr.Append("]}");
				Console.WriteLine("DEBUG RemoveObjectFromRelatedBooksEntry(): updateJson = {0}", bldr);
				parseClient.UpdateObject("relatedBooks", currentRelated.ObjectId, bldr.ToString());
			}
		}

		private void CreateRelatedBooksEntry(HashSet<Book> related, ParseClient parseClient)
		{
			var bldr = new StringBuilder("{\"books\":[");
			var bookSep = "";
			foreach (var book in related)
			{
				bldr.AppendFormat("{0}{{\"__type\":\"Pointer\", \"className\":\"books\", \"objectId\":\"{1}\"}}", bookSep, book.ObjectId);
				bookSep = ", ";
			}
			bldr.Append("]}");

			Console.WriteLine("DEBUG Create: json = {0}", bldr);
			parseClient.CreateObject("relatedBooks", bldr.ToString());
		}

		private bool SameBookList(HashSet<Book> related, List<Book> currentRelated)
		{
			if (related.Count != currentRelated.Count)
				return false;
			foreach (var oldBook in currentRelated)
			{
				var match = false;
				foreach (var newBook in related)
				{
					if (newBook.ObjectId == oldBook.ObjectId)
					{
						match = true;
						break;
					}
				}
				if (!match)
					return false;
			}
			return true;
		}

		private bool SameAuthor(Book newBook, Book oldBook)
		{
			if (newBook.Authors == oldBook.Authors)
				return true;    // they may both be null
			if (newBook.Authors == null || oldBook.Authors == null)
				return false;
			if (newBook.Authors.Count != oldBook.Authors.Count)
				return false;
			foreach (var author in newBook.Authors)
			{
				if (!oldBook.Authors.Contains(author))
					return false;
			}
			return true;
		}

		private bool SameBookshelf(Book newBook, Book oldBook)
		{
			if (newBook.Tags == oldBook.Tags)
				return true;
			if (newBook.Tags == null || oldBook.Tags == null)
				return false;
			var oldShelf = "";
			var newShelf = "";
			foreach (var tag in oldBook.Tags)
			{
				if (tag.StartsWith("Bookshelf:", StringComparison.InvariantCulture))
				{
					oldShelf = tag.Substring(10);
					break;
				}
			}
			foreach (var tag in newBook.Tags)
			{
				if (tag.StartsWith("Bookshelf:", StringComparison.InvariantCulture))
				{
					newShelf = tag.Substring(10);
					break;
				}
			}
			return oldShelf == newShelf;
		}

		public static string GetTagForLrmiReadingLevel(string level)
		{
			if (level.StartsWith("Level ", StringComparison.InvariantCulture))
			{
				return "level:" + level.Replace("Level ","");
			}
			else if (level.StartsWith("ደረጃ ", StringComparison.InvariantCulture))
			{
				return "level:" + level.Replace("ደረጃ ", "");
			}
			else if (level == "Read aloud" || level == "ጮክ ብለህ አንብብ")
			{
				return "marked:Read aloud";
			}
			else if (level == "Decodable" || level == "መፍታት የሚችል")
			{
				return "marked:Decodable";
			}
			else
			{
				return "marked:" + level;	// just in case...
			}
		}

		private void GetBookInformationFromBookFolders(IEnumerable<string> bookDirs, out Dictionary<string, string> instanceIdToTitle, out Dictionary<string, XmlDocument> instanceIdToOpds,
			out Dictionary<string, string> instanceIdToFolder)
		{
			instanceIdToTitle = new Dictionary<string, string>();
			instanceIdToOpds = new Dictionary<string, XmlDocument>();
			instanceIdToFolder = new Dictionary<string, string>();
			foreach (var dir in bookDirs)
			{
				if (!IsValidFolder(dir))
					continue;
				var meta = BookMetaData.FromFolder(dir);
				instanceIdToTitle.Add(meta.Id, meta.Title);
				instanceIdToFolder.Add(meta.Id, dir);
				var opdsEntry = GetOpdsEntryFromFolder(dir);
				if (opdsEntry != null)
					instanceIdToOpds.Add(meta.Id, opdsEntry);
			}
		}

		private string[] ReadBloomBulkUploadLogFile()
		{
			string logfile = GetBloomBulkUploadLogFilePath();
			if (File.Exists(logfile))
				return File.ReadAllLines(logfile);
			else
				return new string[] { };
		}

		private string GetBloomBulkUploadLogFilePath()
		{
			return Path.Combine(_options.BookShelfContainer, "BloomBulkUploadLog.txt");
		}

		private XmlDocument GetOpdsEntryFromFolder(string dir)
		{
			if (!Directory.Exists(dir))
				return null;
			foreach (var file in Directory.EnumerateFiles(dir, "*.opds"))
			{
				XmlDocument entry = new XmlDocument
				{
					PreserveWhitespace = true
				};
				entry.Load(file);
				return entry;       // should be only one, so quit after first.
			}
			return null;
		}
	}
}
