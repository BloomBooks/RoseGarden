// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using CommandLine;

namespace RoseGarden
{
	[Verb("fetch", HelpText = "Fetch a book or catalog from the OPDS source.")]
	public class FetchOptions
	{
		[Option('u', "url", Required = false, HelpText = "Url of the OPDS catalog file (or its root)")]
		public string Url { get; set; }

		[Option('d', "digitallibrary", Required = false, HelpText = "Fetch from Global Digital Library")]
		public bool UseDigitalLibrary { get; set; }

		[Option('s', "storyweaver", Required = false, HelpText = "Fetch from Story Weaver")]
		public bool UseStoryWeaver { get; set; }

		[Option('l', "language", Required = false, HelpText = "Name of desired language: limits catalog output and book title searches")]
		public string LanguageName { get; set; }

		[Option('c', "catalog", Required = false, HelpText = "Download a catalog file to the given location.")]
		public string CatalogFile { get; set; }

		[Option('t', "title", Required = false, HelpText = "Title of desired book to download")]
		public string BookTitle { get; set; }

		[Option('a', "author", Required = false, HelpText = "Author of desired book to download")]
		public string Author { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file path for downloaded book")]
		public string OutputFile { get; set; }

		[Option('k', "key", Required = false, HelpText = "Key (token) for book download access if needed (may be in OPDSTOKEN environment variable")]
		public string AccessToken { get; set; }

		[Option('p', "pdf", Required = false, HelpText = "Download a PDF file instead of the ePUB file.")]
		public bool DownloadPDF { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}

	[Verb("convert", HelpText = "Convert a book from epub to Bloom source.")]
	public class ConvertOptions
	{
		[Option('e', "epub", Required = true, HelpText = "Path to the input epub file (required)")]
		public string EpubFile { get; set; }

		[Option('a', "attribution", Required = false, HelpText = "Input attribution text file as provided by Story Weaver (optional)")]
		public string AttributionFile { get; set; }

		[Option('f', "folder", Required = false, HelpText = "Folder for storing the Bloom book source.  (This may be an existing collection folder.)")]
		public string CollectionFolder { get; set; }

		[Option('F', "force", Required = false, HelpText = "Force overwriting the Bloom book source even if it already exists.")]
		public bool ForceOverwrite { get; set; }

		[Option('b', "bloomfolder", Required = true, HelpText = "Folder where Bloom is installed locally.  (This is used to find various css and other files.)")]
		public string BloomFolder { get; set; }

		[Option('l', "language", Required = false, HelpText = "Name of the main language of the book.")]
		public string LanguageName { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file name to use instead of the title (name without .htm used for both directory and file names)")]
		public string FileName { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}

	[Verb("check", HelpText = "Check whether the given book from the given source needs to be updated.")]
	public class CheckOptions
	{
		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}

	class Program
	{
		static int Main(string[] args)
		{
			return Parser.Default.ParseArguments<FetchOptions, ConvertOptions, CheckOptions>(args)
				.MapResult(
					(FetchOptions opts) => FetchAndReturnExitCode(opts),
					(ConvertOptions opts) => ConvertAndReturnExitCode(opts),
					(CheckOptions opts) => CheckAndReturnExitCode(opts),
					errs => 1);
		}

		private static int FetchAndReturnExitCode(FetchOptions opts)
		{
			return new FetchFromOPDS(opts).Run();
		}
		private static int ConvertAndReturnExitCode(ConvertOptions opts)
		{
			return new ConvertFromEpub(opts).Run();
		}
		private static int CheckAndReturnExitCode(CheckOptions opts)
		{
			return 99;
		}

		/// <summary>
		/// Utility function to sanitize a string for use in a filename or directory name.
		/// </summary>
		/// <remarks>
		/// This method and the following two methods are adapted from Bloom and libpalaso.
		/// </remarks>
		public static string SanitizeNameForFileSystem(string name)
		{
			const int MAX = 50;		// arbitrary length limit
			// Then replace invalid characters with spaces and trim off characters
			// that shouldn't start or finish a directory name.
			name = RemoveDangerousCharacters(name);
			if (name.Length == 0)
			{
				name = "Book";  // This should never be needed, but let's be paranoid.
			}
			if (name.Length > MAX)
				name = name.Substring(0, MAX);
			return name;
		}

		/// <summary>
		/// Replace invalid characters with hyphens and trim off characters that shouldn't
		/// start or finish a file or directory name.
		/// </summary>
		private static string RemoveDangerousCharacters(string name)
		{
			foreach (char c in GetInvalidOSIndependentFileNameChars())
			{
				name = name.Replace(c, '-');
			}
			int length;
			do
			{
				length = name.Length;
				name = name.Replace("--", "-");
				name = name.Replace("  ", " ");
				// Windows does not allow directory names ending in period.
				// If we give it a chance, it will make a directory without the dots,
				// but all our code that thinks the folder name has the dots will break.
				name = name.Trim('.', '-', ' ');
			}
			while (name.Length < length);
			return name;
		}

		private static char[] GetInvalidOSIndependentFileNameChars()
		{
			return new char[]
			{
				// everything with code less than a space character
				'\0','\u0001','\u0002','\u0003','\u0004','\u0005','\u0006','\a',
				'\b','\t','\n','\v','\f','\r','\u000e','\u000f',
				'\u0010','\u0011','\u0012','\u0013','\u0014','\u0015','\u0016','\u0017',
				'\u0018','\u0019','\u001a','\u001b','\u001c','\u001d','\u001e','\u001f',
				// various quotation and other punctuation marks illegal on Windows
				'"', '<', '>', '|', ':', '*', '?', '\\', '/',
				// these may be legal for the filesystem, but they're more trouble than they're worth
				'&', '\'', '{', '}', ',', ';', '(', ')', '$', '@',
				// this one is also trouble
				'\u00a0'
			};
		}

	}
}
