// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using RoseGarden.Parse;
using RoseGarden.Parse.Model;

namespace RoseGarden
{
	public class UploadToBloomLibrary
	{
		UploadOptions _options;
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
			var process = new Process
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = _options.BloomExe,
					Arguments = $"upload {(_options.SingleBookshelfLevel?"-s":"")} -u \"{_options.UploadUser}\" -p \"{_options.UploadPassword}\" \"{_options.BookShelfContainer}\"",
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
					Console.WriteLine("WARNING: the user name must be provided by -u (--user) or by the RoseGardenUserName environment variable.");
					allValid = false;
				}
			}
			if (String.IsNullOrWhiteSpace(_options.UploadPassword))
			{
				_options.UploadPassword = Program.GetEnvironmentVariable("RoseGardenUserPassword");
				if (String.IsNullOrWhiteSpace(_options.UploadPassword))
				{
					Console.WriteLine("WARNING: the user name must be provided by -p (--password) or by the RoseGardenUserPassword environment variable.");
					allValid = false;
				}
			}
			return allValid;
		}

		private void UpdateParseTables()
		{
			var logfile = Path.Combine(_options.BookShelfContainer, "BloomBulkUploadLog.txt");
			var bookDirs = File.ReadAllLines(logfile);
			var instanceIdToTitle = new Dictionary<string, string>();
			foreach (var dir in bookDirs)
			{
				if (String.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
					break;
				BookMetaData meta = BookMetaData.FromFolder(dir);
				instanceIdToTitle.Add(meta.Id, meta.Title);
			}
			ParseClient parseClient = new ParseClient(_options.UploadUser, _options.UploadPassword);
			string importedFilter = "{\"importedBookSourceUrl\": {\"$regex\": \".\"}}";
			IEnumerable<Book> bookList = parseClient.GetBooks(importedFilter, new [] { "uploader" });
			int majorVersion;
			int minorVersion;
			GetVersionNumbers(out majorVersion, out minorVersion);
			foreach (var book in bookList)
			{
				string localTitle;
				if (instanceIdToTitle.TryGetValue(book.BookInstanceId, out localTitle))
				{
					if (localTitle != book.Title)
						Console.WriteLine("WARNING: mismatch in titles from local to parse server: \"{0}\" vs \"{1}\"", localTitle, book.Title);
					if (book.ImporterName != "RoseGarden" || book.ImporterMajorVersion != majorVersion || book.ImporterMinorVersion != minorVersion)
					{
						if (_options.Verbose)
							Console.WriteLine("INFO: updating bloomlibrary books table with RoseGarden importer values for {0}", book.Title);
						var response = parseClient.UpdateObject("books", book.ObjectId,
							$"{{ \"{Book.kImporterNameField}\":\"RoseGarden\", \"{Book.kImporterMajorVersionField}\":{majorVersion}, \"{Book.kImporterMinorVersionField}\":{minorVersion} }}");
					}
				}
			}
		}

		private void GetVersionNumbers(out int majorVersion, out int minorVersion)
		{
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			majorVersion = version.Major;
			minorVersion = version.Minor;
		}
	}
}
