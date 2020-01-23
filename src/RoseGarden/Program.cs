using System;
using CommandLine;

namespace RoseGarden
{
	[Verb("fetch", HelpText = "Fetch a book or catalog from the OPDS source.")]
	public class FetchOptions
	{
		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }

		[Option('V', "veryverbose", Required = false, HelpText = "Write very verbose progress messages to the console.")]
		public bool VeryVerbose { get; set; }

		[Option('u', "url", Required = false, HelpText = "Url of the OPDS catalog file (or its root)")]
		public string Url { get; set; }

		[Option('d', "digitallibrary", Required = false, HelpText = "Fetch from Global Digital Library")]
		public bool UseDigitalLibrary { get; set; }

		[Option('s', "storyweaver", Required = false, HelpText = "Fetch from Story Weaver")]
		public bool UseStoryWeaver { get; set; }

		[Option('l', "language", Required = false, HelpText = "Name of desired language")]
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
	}

	[Verb("convert", HelpText = "Convert one or more books.")]
	public class ConvertOptions
	{
		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }
	}

	[Verb("check", HelpText = "Check whether the given book from the given source needs to be updated.")]
	public class CheckOptions
	{
		[Option('v', "verbose", Required = false, HelpText = "Write verbose progress messages to the console.")]
		public bool Verbose { get; set; }
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
			return 99;
		}
		private static int CheckAndReturnExitCode(CheckOptions opts)
		{
			return 99;
		}

	}
}
