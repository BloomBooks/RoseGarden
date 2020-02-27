// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Reflection;
using CommandLine;

namespace RoseGarden
{
#region Command line option classes
	public class OpdsOptions
	{
		[Option('c', "catalog", Required = false, HelpText = "Catalog file: output if either -u or -s is provided, input if neither is given.")]
		public string CatalogFile { get; set; }

		[Option('d', "dryrun", Required = false, HelpText = "Do not actually fetch a book or catalog file. Only report as directed by -v (verbose) or -V (veryverbose).")]
		public bool DryRun { get; set; }

		[Option('k', "key", Required = false, HelpText = "Key (token) for book download access if needed (may be in OPDSTOKEN environment variable")]
		public string AccessToken { get; set; }

		[Option('l', "language", Required = false, HelpText = "Name of desired language: limits catalog output and book title searches")]
		public string LanguageName { get; set; }

		[Option('s', "source", Required = false, HelpText = "Fetch from a known OPDS source (for example, gdl or sw)")]
		public string Source { get; set; }

		[Option('u', "url", Required = false, HelpText = "Url of the OPDS catalog file (or its root)")]
		public string Url { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}

	[Verb("fetch", HelpText = "Fetch a book or catalog from the OPDS source.")]
	public class FetchOptions : OpdsOptions
	{
		[Option('a', "author", Required = false, HelpText = "Author of desired book to download")]
		public string Author { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file path for downloaded book")]
		public string OutputFile { get; set; }

		[Option('p', "pdf", Required = false, HelpText = "Download the PDF file as well as the ePUB file.")]
		public bool DownloadPDF { get; set; }

		[Option('t', "title", Required = false, HelpText = "Title of desired book to download")]
		public string BookTitle { get; set; }

		[Option('T', "thumbnail", Required = false, HelpText = "Download the thumbnail image file as well as the ePUB file.")]
		public bool DownloadThumbnail { get; set; }

		[Option('I', "image", Required = false, HelpText = "Download the fullsize image file as well as the ePUB file.")]
		public bool DownloadImage { get; set; }
	}

	[Verb("convert", HelpText = "Convert a book from epub to Bloom source.")]
	public class ConvertOptions
	{
		[Option('a', "attribution", Required = false, HelpText = "Input attribution text file as provided by Story Weaver")]
		public string AttributionFile { get; set; }

		[Option('b', "bloomfolder", Required = true, HelpText = "Folder where Bloom is installed locally.")]
		public string BloomFolder { get; set; }

		[Option('e', "epub", Required = true, HelpText = "Path to the input epub file.  If available, the pdf file and jpg/png thumbnail file of the same name will also be used as well as the opds entry file.  If the filename ends in .epub.zip (as from StoryWeaver), then that file is unzipped to obtain the actual epub (and attribution) files.")]
		public string EpubFile { get; set; }

		[Option('f', "folder", Required = false, HelpText = "Folder for storing the Bloom book source.  (This may be an existing collection folder.)  $publisher$ is replaced by the publisher name and $language$ is replaced by the language name in this folder path.  For example, /home/steve/BloomImport/$publisher$/$language$ could be replaced by /home/steve/BloomImport/Pratham/English for storing one book, but by /home/steve/BloomImport/African StoryBook/French for another book.  For conciseness, Pratham Books is replaced by Pratham, and African Storybook Initiative/Project is replaced by African Storybook.  Other publishers are passed verbatim.")]
		public string CollectionFolder { get; set; }

		[Option('l', "language", Required = true, HelpText = "Name of the main language of the book from the catalog entry")]
		public string LanguageName { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file name to use instead of the book's title (name without .htm used for both directory and file names)")]
		public string FileName { get; set; }

		[Option('R', "replace", Required = false, HelpText = "Force replacing the Bloom book source if it already exists.")]
		public bool ReplaceExistingBook { get; set; }

		[Option( "rtl", Required = false, HelpText = "Flag that the language is written right-to-left.")]
		public bool IsRtl { get; set; }

		[Option( "landscape", Required = false, HelpText = "Lay out the book in landscape format.")]
		public bool UseLandscape { get; set; }

		[Option( "portrait", Required = false, HelpText = "Lay out the book in portrait format.  (This is the default behavior.)")]
		public bool UsePortrait { get; set; }

		[Option('U', "user", Required = false, HelpText = "Bloomlibrary user for the upload")]
		public string UploadUser { get; set; }

		[Option('P', "password", Required = false, HelpText = "Password for the given Bloomlibrary user")]
		public string UploadPassword { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}

	[Verb("upload", HelpText = "Upload one or more converted books to bloomlibrary.org")]
	public class UploadOptions
	{
		[Option('b', "bloomexe", Required = true, HelpText = "Path of the Bloom executable.  This is probably a shell script on Linux but the actual Bloom.exe file on Windows.")]
		public string BloomExe { get; set; }

		[Option('U', "user", Required = false, HelpText = "Bloomlibrary user for the upload")]
		public string UploadUser { get; set; }

		[Option('P', "password", Required = false, HelpText = "Password for the given Bloomlibrary user")]
		public string UploadPassword { get; set; }

		[Option('s', "singlelevel", HelpText = "Restrict bookshelf name to only the top level under the path.  (default limit is 2 levels)", Required = false)]
		public bool SingleBookshelfLevel { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }

		[Value(0, Required = true, HelpText = "Folder containing a bookshelf folder structure.  Collection folders are 2 or 3 levels beneath the given folder.")]
		public string BookShelfContainer { get; set; }
	}

	[Verb("batch", HelpText = "Batch process books from a catalog, checking which need to be updated, then fetching, converting, and uploading.")]
	public class BatchOptions : OpdsOptions
	{
		[Option('b', "bloomexe", Required = true, HelpText = "Path of the Bloom executable.  This is probably a shell script on Linux but the actual Bloom.exe file on Windows.")]
		public string BloomExe { get; set; }

		[Option('B', "bloomfolder", Required = false, HelpText = "Folder where Bloom is installed locally.  If not given, the folder portion of --bloomexe is used.  (This default probably works only on Windows.)")]
		public string BloomFolder { get; set; }

		[Option('I', "image", Required = false, HelpText = "Download the fullsize image file as well as the ePUB file.")]
		public bool DownloadImage { get; set; }

		[Option('R', "replace", Required = false, HelpText = "Force replacing the Bloom book source if it already exists in the local destination folder.")]
		public bool ReplaceExistingBook { get; set; }

		[Option('U', "user", Required = false, HelpText = "Bloomlibrary user for the upload")]
		public string UploadUser { get; set; }

		[Option('P', "password", Required = false, HelpText = "Password for the given Bloomlibrary user")]
		public string UploadPassword { get; set; }

		[Option('N', "noupload", Required = false, HelpText = "Do not upload after fetching and converting.")]
		public bool DoNotUpload { get; set; }

		[Value(0, Required = true, HelpText = "Folder containing a bookshelf folder structure.  The directory level containing the separate bookshelves (which are based on publisher) is marked $publisher$, and replaced by each book's publisher.  Under that directory may be an additional level marked $language$ which is replaced by each book's language.  For example, /home/steve/BloomImport/$publisher$/$language$ could be replaced by /home/steve/BloomImport/Pratham/English for storing one book, but by /home/steve/BloomImport/African StoryBook/French for another book.  For conciseness, Pratham Books is replaced by Pratham, and African Storybook Initiative/Project is replaced by African Storybook.  Other publishers are passed verbatim.")]
		public string BookShelfContainer { get; set; }
	}

	[Verb("fixtable", HelpText = "Fix the table fields for imported books to make up for mistakes in development.")]
	public class FixTableOptions
	{
		[Option('P', "password", Required = false, HelpText = "Password for the given Bloomlibrary user")]
		public string UploadPassword { get; set; }

		[Option('U', "user", Required = false, HelpText = "Bloomlibrary user for the upload")]
		public string UploadUser { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }
	}
#endregion

	class Program
	{
#region Main program methods
		static int Main(string[] args)
		{
			return Parser.Default.ParseArguments<BatchOptions, ConvertOptions, FetchOptions, FixTableOptions, UploadOptions>(args)
				.MapResult(
					(BatchOptions opts) => BatchAndReturnExitCode(opts),
					(ConvertOptions opts) => ConvertAndReturnExitCode(opts),
					(FetchOptions opts) => FetchAndReturnExitCode(opts),
					(FixTableOptions opts) => FixTableAndReturnExitCode(opts),
					(UploadOptions opts) => UploadAndReturnExitCode(opts),
					errs => 1);
		}

		private static int FetchAndReturnExitCode(FetchOptions opts)
		{
			return new FetchFromOPDS(opts).RunFetch();
		}
		private static int ConvertAndReturnExitCode(ConvertOptions opts)
		{
			return new ConvertFromEpub(opts).RunConvert();
		}
		private static int UploadAndReturnExitCode(UploadOptions opts)
		{
			return new UploadToBloomLibrary(opts).RunUpload();
		}
		private static int BatchAndReturnExitCode(BatchOptions opts)
		{
			return new BatchProcessBooks(opts).RunBatch();
		}

		private static int FixTableAndReturnExitCode(FixTableOptions opts)
		{
			return new FixTable(opts).RunFix();
		}
#endregion

#region Utility methods
		/// <summary>
		/// Utility method to sanitize a string for use in a filename or directory name.
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
		/// Replace invalid characters with spaces and trim off characters that shouldn't
		/// start or finish a file or directory name (space or period).
		/// </summary>
		/// <remarks>
		/// I don't particularly like the details of replacement here, but we need to match Bloom.
		/// </remarks>
		private static string RemoveDangerousCharacters(string name)
		{
			foreach (char c in GetInvalidOSIndependentFileNameChars())
			{
				name = name.Replace(c, ' ');
			}
			name = name.Trim('.', ' ');
			if (name.Length == 0)
				return "Book";
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

		/// <summary>
		/// Utility method to get the value of an environment variable.  It tries the all-uppercase
		/// version of the variable name if the original name doesn't provide a value.
		/// </summary>
		public static string GetEnvironmentVariable(string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			if (String.IsNullOrEmpty(value))	// Linux users tend to use all-caps for environment variables...
				value = Environment.GetEnvironmentVariable(variableName.ToUpperInvariant());
			return value;
		}

		/// <summary>
		/// Utility method to get the major and minor version numbers of the executing program.
		/// </summary>
		public static void GetVersionNumbers(out int majorVersion, out int minorVersion)
		{
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			majorVersion = version.Major;
			minorVersion = version.Minor;
		}
#endregion
	}
}
