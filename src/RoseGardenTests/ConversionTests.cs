// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class ConversionTests : ConversionTestBase
	{
		[Test]
		public void TestVerifyConvertOptions()
		{
			Console.WriteLine("TESTING: A message about not using --portrait and --landscape together is expected.");
			var options = new ConvertOptions()
			{
				UsePortrait = true,
				UseLandscape = true,
				// The next two merely prevent a spurious warning if the enviroment variables are not set.
				UploadUser = "user",
				UploadPassword = "password"
			};
			var convert = new ConvertFromEpub(options);
			//SUT
			var result = convert.VerifyOptions();
			Assert.That(result, Is.False);

			convert._options.UsePortrait = false;
			convert._options.UseLandscape = false;
			result = convert.VerifyOptions();
			Assert.That(result, Is.True);

			convert._options.UsePortrait = true;
			convert._options.UseLandscape = false;
			result = convert.VerifyOptions();
			Assert.That(result, Is.True);

			convert._options.UsePortrait = false;
			convert._options.UseLandscape = true;
			result = convert.VerifyOptions();
			Assert.That(result, Is.True);
		}

		[Test]
		public void TestEpubMetadataGetOpfPath()
		{
			var epubPath1 = "/home/steve/test/epubs/This is a test";
			var metaXml1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"" version=""1.0"">
   <rootfiles>
      <rootfile full-path=""content/book.opf"" media-type=""application/oebps-package+xml""/>
   </rootfiles>
</container>";

			// SUT
			var result1 = EpubMetadata.GetOpfPath(epubPath1, metaXml1);
			Assert.That(result1, Is.EqualTo(Path.Combine(epubPath1, "content", "book.opf")));

			var epubPath2 = "C:\\Users\\steve\\Documents\\epubs\\Testing Away";
			var metaXml2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/package.opf"" media-type=""application/oebps-package+xml""/>
  </rootfiles>
</container>";

			// SUT
			var result2 = EpubMetadata.GetOpfPath(epubPath2, metaXml2);
			Assert.That(result2, Is.EqualTo(Path.Combine(epubPath2, "OEBPS", "package.opf")));
		}

		[Test]
		public void TestEpubMetadataLoading()
		{
			var epubPath1 = "/home/steve/test/epubs/Test";
			var opfPath1 = "/home/steve/test/epubs/Test/content/book.opf";

			// SUT
			var epubMeta = new EpubMetadata(epubPath1, opfPath1, ConvertingEnglishGDL._goatOpfXml);
			Assert.That(epubMeta.Authors.Count, Is.EqualTo(1));
			Assert.That(epubMeta.Authors[0], Is.EqualTo("Alice Nakasango"));
			Assert.That(epubMeta.Illustrators.Count, Is.EqualTo(1));
			Assert.That(epubMeta.Illustrators[0], Is.EqualTo("Marleen Visser"));
			Assert.That(epubMeta.OtherCreators.Count, Is.EqualTo(0));
			Assert.That(epubMeta.OtherContributors.Count, Is.EqualTo(0));
			Assert.That(epubMeta.Description, Is.EqualTo("Now you know why goats are so stubborn!"));
			Assert.That(epubMeta.Title, Is.EqualTo("Goat, The False King"));
			Assert.That(epubMeta.LanguageCode, Is.EqualTo("en"));
			Assert.That(epubMeta.Identifier, Is.EqualTo("4f513a80-8f36-46c5-a73f-3169420c5c24"));
			Assert.That(epubMeta.Modified, Is.EqualTo(DateTime.Parse("2020-01-31T08:43:02Z")));
			Assert.That(epubMeta.ImageFiles.Count, Is.EqualTo(21));
			Assert.That(epubMeta.ImageFiles[0], Is.EqualTo(Path.Combine(epubPath1, "content", "c7b42f14c72ad4a3b3488c4377b70d94.jpg")));
			Assert.That(epubMeta.PageFiles.Count, Is.EqualTo(20));
			Assert.That(epubMeta.PageFiles[6], Is.EqualTo(Path.Combine(epubPath1, "content", "chapter-7.xhtml")));
		}





	}
}
