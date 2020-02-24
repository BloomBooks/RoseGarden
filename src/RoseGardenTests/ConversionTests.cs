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
	public class ConversionTests
	{
		private string _blankBookHtml;
		private string _pagesFileXhtml;

		public ConversionTests()
		{
			// Reading the blank XHTML book and page files once is our only violation of no file access.
			// I suppose I could duplicate the XHTML in the code here, but reading what the program
			// actually uses is safer.
			var location = Assembly.GetExecutingAssembly().Location;
			var blankHtmPath = Path.Combine(Path.GetDirectoryName(location), "Resources", "Book.htm");
			_blankBookHtml = File.ReadAllText(blankHtmPath);
			var pagesFile = Path.Combine(Path.GetDirectoryName(location), "Resources", "Pages.xml");
			_pagesFileXhtml = File.ReadAllText(pagesFile);
		}

		/// <summary>
		/// This method should reflect the ConvertFromEpub.Initialize() method except for the file copying
		/// and other file I/O operations.  (plus some content from ConvertBook()
		/// </summary>
		private ConvertFromEpub InitializeForConversions(ConvertOptions opts, string opfXml, string opdsXml)
		{
			var convert = new ConvertFromEpub(opts);
			convert._epubFolder = "/home/steve/test/epub";
			convert._epubMetaData = new EpubMetadata(convert._epubFolder, "/home/steve/test/epub/content/book.opf", opfXml);
			string langCode = convert.ForceGoodLanguageCode();
			if (String.IsNullOrWhiteSpace(opts.FileName))
				convert._htmFileName = Program.SanitizeNameForFileSystem(convert._epubMetaData.Title) + ".htm";
			else
				convert._htmFileName = opts.FileName + ".htm";

			convert._bookMetaData = new BookMetaData();
			convert._bookMetaData.BookLineage = "056B6F11-4A6C-4942-B2BC-8861E62B03B3";
			convert._bookMetaData.Id = Guid.NewGuid().ToString();   // This may be replaced if we're updating an existing book.

			convert._bloomDoc = new XmlDocument();
			convert._bloomDoc.PreserveWhitespace = true;
			convert._bloomDoc.LoadXml(_blankBookHtml);

			convert._templateBook = new XmlDocument();
			convert._templateBook.PreserveWhitespace = true;
			convert._templateBook.LoadXml(_pagesFileXhtml);
			convert._templatePages = convert._templateBook.SelectNodes("//div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			if (opts.UseLandscape)
				convert.ChangePagesToLandscape();

			if (!String.IsNullOrWhiteSpace(opdsXml))
				convert.LoadOpdsDataAndSetPublisher(opdsXml);

			convert.SetHeadMetaAndBookLanguage();

			return convert;
		}

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
			var epubMeta = new EpubMetadata(epubPath1, opfPath1, _goatOpfXml);
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

		/// <summary>
		/// This tests converting the Global Digital Library version of "Goat, The False King", a book published by the African Storybook Initiative.
		/// It has these distinctive features:
		/// * a cover page with no text, just a picture
		/// * content pages that have no paragraph markup, just raw text nodes under the body element
		/// * a copyright line on the end page split between text and artwork
		/// </summary>
		/// <remarks>
		/// The end page provides information we don't try to glean such as the translator.
		/// </remarks>
		[Test]
		public void TestConvertingGoat_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _goatOpfXml, _goatOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Goat, The False King");

			// SUT
			convert.ConvertCoverPage(_goatPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Goat, The False King", "c7b42f14c72ad4a3b3488c4377b70d94.jpg", @"<p>Author: Alice Nakasango</p>
<p>Illustrator: Marleen Visser</p>", out XmlElement coverImageData);

			// SUT
			var result = convert.ConvertContentPage(1, _goatPage2Xhtml);
			Assert.That(result, Is.True, "converting Goat chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "27e900b0dc523b77e981b601a779c6a0.jpg",
				"<p>Once upon a time,  there was a goat called Igodhoobe. Igodhoobe the goat was the king of farm animals and birds. He lived a good life. One day,  Igodhoobe the goat called all the animals and birds to a meeting.</p>");

			// SUT
			result = convert.ConvertContentPage(18, _goatPage19Xhtml);
			Assert.That(result, Is.True, "converting Goat chapter 19 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "18", 3, "fd3e3272e2bf6aefc67c827f529b3891.jpg",
				"<p>From that time,  every goat refuses to move when it is pulled. It thinks that you are taking it to the king's court.</p>");

			// SUT
			result = convert.ConvertContentPage(19, _goatPage20Xhtml);
			Assert.That(result, Is.True, "converting Goat chapter 20 (end page) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © African Storybook Initiative 2015", "CC BY 4.0", "Marleen Visser",
				"Copyright © Uganda Community Libraries Association (Ugcla) 2015", "http://creativecommons.org/licenses/by/4.0/",
				"<p>All illustrations by Marleen Visser. Copyright © African Storybook Initiative 2015. Some rights reserved. Released under the CC BY 4.0 license.</p>",
				new[] { "You are free to download, copy, translate or adapt this story and use the illustrations as long as you attribute in the following way:",
					"/>",
					"Cornelius Wambi Gulere",
					"© Text: Uganda Community Libraries Association (Ugcla) Artwork: African Storybook Initiative 2015",
					"www.africanstorybook.org" });
		}

		const string _goatOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">4f513a80-8f36-46c5-a73f-3169420c5c24</dc:identifier>
		<dc:title>Goat, The False King</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-01-31T08:43:02Z</meta>
		<dc:description>Now you know why goats are so stubborn!</dc:description>
		<dc:creator id=""contributor_1"">Alice Nakasango</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Marleen Visser</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""c7b42f14c72ad4a3b3488c4377b70d94.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""27e900b0dc523b77e981b601a779c6a0.jpg"" id=""image-1797-1"" media-type=""image/jpeg"" />
		<item href=""113620301be83807851256da253854ec.jpg"" id=""image-1798-2"" media-type=""image/jpeg"" />
		<item href=""fc4988bbc31120db06542eb426a073e8.jpg"" id=""image-1799-3"" media-type=""image/jpeg"" />
		<item href=""4a714c8c8598031504f0ec74b4d3c672.jpg"" id=""image-1800-4"" media-type=""image/jpeg"" />
		<item href=""75ada9f934a7a76def05d00f3dfdce47.jpg"" id=""image-1801-5"" media-type=""image/jpeg"" />
		<item href=""84a4d8b7161da4f56093aeb41aec3fb8.jpg"" id=""image-1802-6"" media-type=""image/jpeg"" />
		<item href=""09826080c43ae1bed161829fc9b4b2d7.jpg"" id=""image-1803-7"" media-type=""image/jpeg"" />
		<item href=""710655c5d3d4fc7d58f8364e3fca1c1f.jpg"" id=""image-1804-8"" media-type=""image/jpeg"" />
		<item href=""e3957499bd3676e02abfd50f3e657dc8.jpg"" id=""image-1805-9"" media-type=""image/jpeg"" />
		<item href=""165dd188609dd0c27dd5e5bd6ec229de.jpg"" id=""image-1806-10"" media-type=""image/jpeg"" />
		<item href=""d0c6d93209ffa56df091c7eac8df269b.jpg"" id=""image-1807-11"" media-type=""image/jpeg"" />
		<item href=""c0ae2e896d6d57145ca445b1a82d8a97.jpg"" id=""image-1808-12"" media-type=""image/jpeg"" />
		<item href=""e85d1bcff49f59e0235eb392674c7eaf.jpg"" id=""image-1809-13"" media-type=""image/jpeg"" />
		<item href=""caf35933c4ed05679aab174f883380ea.jpg"" id=""image-1810-14"" media-type=""image/jpeg"" />
		<item href=""f6523f10a36bb6c18b51c9e6ccae5a69.jpg"" id=""image-1811-15"" media-type=""image/jpeg"" />
		<item href=""70105fcf818fd5ae99eec2d748e84273.jpg"" id=""image-1812-16"" media-type=""image/jpeg"" />
		<item href=""a0b6f6f8534d1cdec13df494ff30ca32.jpg"" id=""image-1813-17"" media-type=""image/jpeg"" />
		<item href=""fd3e3272e2bf6aefc67c827f529b3891.jpg"" id=""image-1814-18"" media-type=""image/jpeg"" />
		<item href=""b05ecc1c18eb82dd065f09f48f02e76d.png"" id=""image-1815-19"" media-type=""image/png"" />
		<item href=""0e3092caa488d94d313660efea502c08.png"" id=""image-1816-20"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-13.xhtml"" id=""chapter-13"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-14.xhtml"" id=""chapter-14"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-15.xhtml"" id=""chapter-15"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-16.xhtml"" id=""chapter-16"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-17.xhtml"" id=""chapter-17"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-18.xhtml"" id=""chapter-18"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-19.xhtml"" id=""chapter-19"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-20.xhtml"" id=""chapter-20"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _goatOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:4f513a80-8f36-46c5-a73f-3169420c5c24</id>
<title>Goat, The False King</title>
<author>
<name>Alice Nakasango</name>
</author>
<contributor type=""Illustrator"">
<name>Marleen Visser</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>African Storybook Initiative</dc:publisher>
<updated>2017-11-24T00:00:00Z</updated>
<dc:created>2015-10-20T00:00:00Z</dc:created>
<published>2017-11-24T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 4"" />
<summary>Now you know why goats are so stubborn!</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/c7b42f14c72ad4a3b3488c4377b70d94"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/c7b42f14c72ad4a3b3488c4377b70d94?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/4f513a80-8f36-46c5-a73f-3169420c5c24.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/4f513a80-8f36-46c5-a73f-3169420c5c24.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _goatPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""c7b42f14c72ad4a3b3488c4377b70d94.jpg"" />
</body>
</html>";
		const string _goatPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""27e900b0dc523b77e981b601a779c6a0.jpg"" />
<br />
Once upon a time,  there was a goat called Igodhoobe. Igodhoobe the goat was the king of farm animals and birds. He lived a good life. One day,  Igodhoobe the goat called all the animals and birds to a meeting.
</body>
</html>";
		const string _goatPage19Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 19</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""fd3e3272e2bf6aefc67c827f529b3891.jpg"" />
<br />
From that time,  every goat refuses to move when it is pulled. It thinks that you are taking it to the king's court.
</body>
</html>";
		const string _goatPage20Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 20</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body>You are free to download, copy, translate or adapt this story and use the illustrations as long as you attribute in the following way:
Alice Nakasango
<b>
 Author -
</b>
Alice Nakasango
<br />
<b>
 Translation
</b>
-
Cornelius Wambi Gulere
<br />
<b>
 Illustration -
</b>
Marleen Visser
<br />
<b>
 Language -
</b>
English
<br />
<b>
 Level -
</b>
Longer paragraphs
© Text: Uganda Community Libraries Association (Ugcla) Artwork: African Storybook Initiative 2015
<br />
Creative Commons: Attribution 4.0
<br />
Source
www.africanstorybook.org
<br />
Original source
http://ugcla.org
<br />
<br />
<img src=""b05ecc1c18eb82dd065f09f48f02e76d.png"" />
<br />
<img src=""0e3092caa488d94d313660efea502c08.png"" />
</body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "Dogs versus Cats", a book published by Room to Read.
		/// It has these distinctive features:
		/// * a cover page with 3 pictures and text marked up by paragraph elements
		/// * content pages that have paragraph markup for the text
		/// * multiple end pages that apparently are based on those produced by StoryWeaver, but with most of the StoryWeaver
		///   markup removed.
		/// </summary>
		[Test]
		public void TestConvertingDogsVsCats_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _dogsOpfXml, _dogsOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Dogs versus Cats");

			// SUT
			convert.ConvertCoverPage(_dogsPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Dogs versus Cats", "ac99ee2a331aa285d8c828cdb2ee0b29.jpg", @"<p>
 Author: Nomkhosi Cynthia Thabethe
</p><p>
 Illustrator: Vusi  Malindi
</p><p>
 Translator: Alisha Berger
</p>", out XmlElement coverImageData);
			// This book has two extra images on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "1088b6d732161819888481bc20863e7c.png", "61fdf7a3fe76891db5a123fe68b73434.png");

			// SUT
			var result = convert.ConvertContentPage(1, _dogsPage2Xhtml);
			Assert.That(result, Is.True, "converting Dogs chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "472ec5f74412d1021a8fba06e832364d.jpg", @"<p>A dog and a cat were best friends.</p>
<p>Everyone knew that if they saw the cat, the dog wouldn't be far behind.</p>");

			// SUT
			result = convert.ConvertContentPage(10, _dogsPage11Xhtml);
			Assert.That(result, Is.True, "converting Dogs chapter 11 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "10", 3, "1ef2966e90505f7cebbff05e35856362.jpg", @"<p>Since that day, cats and dogs stopped getting along... most of the time.</p>");

			// SUT
			result = convert.ConvertContentPage(11, _dogsPage12Xhtml);
			Assert.That(result, Is.True, "converting Dogs chapter 12 (end page 1/3) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(12, _dogsPage13Xhtml);
			Assert.That(result, Is.True, "converting Dogs chapter 13 (end page 2/3) succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and two end pages.");

			// SUT
			result = convert.ConvertContentPage(13, _dogsPage14Xhtml);
			Assert.That(result, Is.True, "converting Dogs chapter 14 (end page 3/3) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © Room to Read, 2013", "CC BY 4.0", "Vusi Malindi",
				"Copyright © for this translation lies with Room to Read, 2013", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>This story 'Dogs versus Cats' has been published on StoryWeaver by Room to Read.</p>
<p>All images by Vusi Malindi. Copyright © Room to Read, 2013. Some rights reserved. Released under the CC BY 4.0 license.</p>",
				new string[] { @"<p>
 Dogs versus Cats (English)
</p>",
					"Come, start weaving today, and help us get a book in every child's hand!",
					"Dogs Versus Cats is the story of an uncommon friendship between a dog and a cat.",
					@"<p>
 This is a Level 2 book for children who recognize familiar words and can read new words with help.
</p>",
					"<img src=\"1ce0e999d2f96c6d254c2de8d763318a.png\"" });
		}

		const string _dogsOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">ccbc2cf5-b073-42b0-b1e9-d823f238a73d</dc:identifier>
		<dc:title>Dogs versus Cats</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:02:59Z</meta>
		<dc:description>Dogs Versus Cats is the story of an uncommon friendship between a dog and a cat. But what happens when the cat is invited to live inside a nice, warm house--and the dog must still live outside?</dc:description>
		<dc:creator id=""contributor_1"">Alisha Berger</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Vusi  Malindi</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""ac99ee2a331aa285d8c828cdb2ee0b29.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""1088b6d732161819888481bc20863e7c.png"" id=""image-47215-1"" media-type=""image/png"" />
		<item href=""61fdf7a3fe76891db5a123fe68b73434.png"" id=""image-47216-2"" media-type=""image/png"" />
		<item href=""472ec5f74412d1021a8fba06e832364d.jpg"" id=""image-47217-3"" media-type=""image/jpeg"" />
		<item href=""514a716d9611120b75bdb9d581ac7279.jpg"" id=""image-47218-4"" media-type=""image/jpeg"" />
		<item href=""1f5d585d9c1efa81ccd9ea84a243abd7.jpg"" id=""image-47219-5"" media-type=""image/jpeg"" />
		<item href=""03ded927675b12aa94c90a7677153027.jpg"" id=""image-47220-6"" media-type=""image/jpeg"" />
		<item href=""1b4fae9c3bc50728460241ee9af90843.jpg"" id=""image-47221-7"" media-type=""image/jpeg"" />
		<item href=""9869ac391122762ef3f02119e64979da.jpg"" id=""image-47222-8"" media-type=""image/jpeg"" />
		<item href=""c9b2c0df2cdc06eae7aecb3549787d5c.jpg"" id=""image-47223-9"" media-type=""image/jpeg"" />
		<item href=""0048e086772eaebbd1e2bb91865b5659.jpg"" id=""image-47224-10"" media-type=""image/jpeg"" />
		<item href=""b00ac216bb6bb726b399cadd8216bffa.jpg"" id=""image-47225-11"" media-type=""image/jpeg"" />
		<item href=""1ef2966e90505f7cebbff05e35856362.jpg"" id=""image-47226-12"" media-type=""image/jpeg"" />
		<item href=""1ce0e999d2f96c6d254c2de8d763318a.png"" id=""image-47227-13"" media-type=""image/png"" />
		<item href=""1dd8bab4fb44379a9e7e38ff1ec1a2e3.png"" id=""image-47228-14"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-13.xhtml"" id=""chapter-13"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-14.xhtml"" id=""chapter-14"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _dogsOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:ccbc2cf5-b073-42b0-b1e9-d823f238a73d</id>
<title>Dogs versus Cats</title>
<author>
<name>Alisha Berger</name>
</author>
<contributor type=""Illustrator"">
<name>Vusi  Malindi</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Room to Read</dc:publisher>
<updated>2019-06-05T00:00:00Z</updated>
<dc:created>2019-06-05T00:00:00Z</dc:created>
<published>2019-06-05T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 2"" />
<summary>Dogs Versus Cats is the story of an uncommon friendship between a dog and a cat. But what happens when the cat is invited to live inside a nice, warm house--and the dog must still live outside?</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/ac99ee2a331aa285d8c828cdb2ee0b29"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/ac99ee2a331aa285d8c828cdb2ee0b29?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/ccbc2cf5-b073-42b0-b1e9-d823f238a73d.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/ccbc2cf5-b073-42b0-b1e9-d823f238a73d.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _dogsPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""ac99ee2a331aa285d8c828cdb2ee0b29.jpg"" />
<img src=""1088b6d732161819888481bc20863e7c.png"" />
<img src=""61fdf7a3fe76891db5a123fe68b73434.png"" />
<p>
 <b>
  Dogs versus Cats
 </b>
</p>
<p>
 Author: Nomkhosi Cynthia Thabethe
</p>
<p>
 Illustrator: Vusi  Malindi
</p>
<p>
 Translator: Alisha Berger
</p></body>
</html>";
		const string _dogsPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""472ec5f74412d1021a8fba06e832364d.jpg"" />
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
 A dog and a cat were best friends.
</p>
<p>
</p>
<p>
 Everyone knew that if they saw the cat, the dog wouldn't be far behind.
</p></body>
</html>";
		const string _dogsPage11Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 11</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""1ef2966e90505f7cebbff05e35856362.jpg"" />
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
</p>
<p>
 Since that day, cats and dogs stopped getting along... most of the time.
</p>
<p>
</p></body>
</html>";
		const string _dogsPage12Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 12</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""1ce0e999d2f96c6d254c2de8d763318a.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following link .
</p>
<p>
 Story Attribution:
</p>
This story:Dogs versus Catsis translated byAlisha Berger.            The © for this translation lies with Room to Read, 2013. Some rights reserved. Released under CC BY 4.0 license.Based on Original story:'Akwatiwa lokwacabanisa inja nelikati', byNomkhosi Cynthia Thabethe.            © Room to Read  , 2013. Some rights reserved. Released under CC BY 4.0 license.
<p>
 Other Credits:
</p>
This story 'Dogs versus Cats' has been published on StoryWeaver by Room to Read.
<p>
 Images Attributions:
</p>
Cover page:A dog and a cat, byVusi  Malindi© Room to Read,             2013. Some rights reserved. Released under CC BY 4.0 license.Page 2:A cat and a dog, byVusi  Malindi© Room to Read,             2013. Some rights reserved. Released under CC BY 4.0 license.Disclaimer:https://www.storyweaver.org.in/terms_and_conditions
<img src=""1dd8bab4fb44379a9e7e38ff1ec1a2e3.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution, http://creativecommons.org/licenses/by/4.0/
</p></body>
</html>";
		const string _dogsPage13Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 13</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""1ce0e999d2f96c6d254c2de8d763318a.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following link .
</p>
<p>
 Images Attributions:
</p>
Page 11:Cat on a barrel, and a dog nearby, byVusi  Malindi© Room to Read,             2013. Some rights reserved. Released under CC BY 4.0 license.Disclaimer:https://www.storyweaver.org.in/terms_and_conditions
<img src=""1dd8bab4fb44379a9e7e38ff1ec1a2e3.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution, http://creativecommons.org/licenses/by/4.0/
</p></body>
</html>";
		const string _dogsPage14Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 14</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Dogs versus Cats (English)
</p>
<p>
 Dogs Versus Cats is the story of an uncommon friendship between a dog and a cat. But what happens when the cat is invited to live inside a nice, warm house--and the dog must still live outside?
</p>
<p>
 This is a Level 2 book for children who recognize familiar words and can read new words with help.
</p>
<img src=""1ce0e999d2f96c6d254c2de8d763318a.png"" />
Pratham Books goes digital to weave a whole new chapter in the realm of multilingual children's stories. Knitting together children, authors, illustrators and publishers. Folding in teachers, and translators. To create a rich fabric of openly licensed multilingual stories for the children of India ­­ and the world. Our unique online platform, StoryWeaver, is a playground where children, parents, teachers and librarians can get creative. Come, start weaving today, and help us get a book in every child's hand!
</body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "Bagaimana Jika?", a book created by The Asia Foundation
		/// as a translation of a book published by Pratham Books, and claiming itself to be published by Pratham Books.  (I think
		/// the GDL catalog entry should say the publisher is The Asia Foundation, but maybe that's just me...)
		/// It has these distinctive features:
		/// * book in Indonesian, not English
		/// * the front cover page uses header (&lt;h1&gt; &lt;h2&gt;) markup instead of paragraph (&lt;p&gt; markup
		/// * content pages use paragraph markup for the text
		/// * end page still in English, not Indonesian
		/// </summary>
		/// <remarks>
		/// If we can figure out anything better to do to handle the end page, this test will need to be updated.
		/// </remarks>
		[Test]
		public void TestConvertingBagaimanaJika_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "Indonesian", UsePortrait = true }, _bagaimanaOpfXml, _bagaimanaOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Bagaimana Jika?");

			// SUT
			convert.ConvertCoverPage(_bagaimanaPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Bagaimana Jika?", "cfe98a0f73b77370b3807c22493dd508.jpg", @"<p>Hari Kumar Nair</p>", out XmlElement coverImageData, "id");

			// SUT
			var result = convert.ConvertContentPage(1, _bagaimanaPage2Xhtml);
			Assert.That(result, Is.True, "converting Bagaimana Jika chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "d0b65a163a77bbe4caf7a8c5664e0e7d.jpg", @"<p>Namaku Shyam. Usiaku 10 tahun.
 Badanku kurus, tapi aku pemberani!
 Aku tidak suka bangun pagi untuk bersekolah.
 Aku ngantuk sekali,  sampai rasanya mau jatuh.</p>", "id");

			// SUT
			result = convert.ConvertContentPage(10, _bagaimanaPage11Xhtml);
			Assert.That(result, Is.True, "converting Bagaimana Jika chapter 11 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "10", 3, "218e51bccf02100958873a40e1e19e23.jpg", @"<p>Ternyata aku masih berdiri dengan sikat gigi di tanganku.
 Aku tersenyum memikirkan dunia rahasiaku.</p>", "id");

			// SUT
			result = convert.ConvertContentPage(11, _bagaimanaPage12Xhtml);
			Assert.That(result, Is.True, "converting Bagaimana Jika chapter 12 (end page) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © The Asia Foundation, 2018", "CC BY 4.0", "Hari Kumar Nair",
				"Copyright © The Asia Foundation, 2018", "http://creativecommons.org/licenses/by/4.0/",
				"<p>All illustrations by Hari Kumar Nair. Copyright © The Asia Foundation, 2018. Some rights reserved. Released under the CC BY 4.0 license.</p>",
				new[] { @"<p>
 Brought to you by
</p>",
					"</p>",
					"Let's Read! is an initiative of The Asia Foundation's Books for Asia program",
					@"© Pratham Books.
 Released under CC BY 4.0.",
					"This work is a modified version of the original story. © The Asia Foundation, 2018."});
		}

		const string _bagaimanaOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">fb78f606-d23d-4c70-b63a-512ce03d2f90</dc:identifier>
		<dc:title>Bagaimana Jika?</dc:title>
		<dc:language>id</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:04:08Z</meta>
		<dc:description>Ketika Shyam mengantuk, dia kesulitan untuk menggosok gigi. Namun bermimpi besar sama sekali bukan masalah.</dc:description>
		<dc:creator id=""contributor_1"">Hari Kumar Nair</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Hari Kumar Nair</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""cfe98a0f73b77370b3807c22493dd508.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""d0b65a163a77bbe4caf7a8c5664e0e7d.jpg"" id=""image-18716-1"" media-type=""image/jpeg"" />
		<item href=""8790942ca0b4f1be0fdcf2af2205004a.jpg"" id=""image-18717-2"" media-type=""image/jpeg"" />
		<item href=""882af60696ad0c5daf47e3949e29894c.jpg"" id=""image-18724-3"" media-type=""image/jpeg"" />
		<item href=""95f00a64d54ccf04c876ac37c17a7b36.jpg"" id=""image-18718-4"" media-type=""image/jpeg"" />
		<item href=""79401a67dfe89777d3053531e2e70d4c.jpg"" id=""image-18725-5"" media-type=""image/jpeg"" />
		<item href=""aab2d9e17805f555edda0caefafcc825.jpg"" id=""image-18726-6"" media-type=""image/jpeg"" />
		<item href=""7d3a4b367788d8247847237e60e9e6d8.jpg"" id=""image-18719-7"" media-type=""image/jpeg"" />
		<item href=""547b339813f01eb4b9cbd6f0f0ed7937.jpg"" id=""image-18720-8"" media-type=""image/jpeg"" />
		<item href=""977a0860f01f00ae287140d04d48bb1b.jpg"" id=""image-18721-9"" media-type=""image/jpeg"" />
		<item href=""218e51bccf02100958873a40e1e19e23.jpg"" id=""image-18727-10"" media-type=""image/jpeg"" />
		<item href=""2d85083a3544781ab3cab25d5c38b443.png"" id=""image-18722-11"" media-type=""image/png"" />
		<item href=""3fb7682f683350564d62e624ffbfcac3.jpg"" id=""image-18723-12"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _bagaimanaOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:fb78f606-d23d-4c70-b63a-512ce03d2f90</id>
<title>Bagaimana Jika?</title>
<author>
<name>Hari Kumar Nair</name>
</author>
<contributor type=""Illustrator"">
<name>Hari Kumar Nair</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Pratham books</dc:publisher>
<updated>2018-06-18T00:00:00Z</updated>
<dc:created>2018-06-18T00:00:00Z</dc:created>
<published>2018-06-18T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 1"" />
<summary>Ketika Shyam mengantuk, dia kesulitan untuk menggosok gigi. Namun bermimpi besar sama sekali bukan masalah.</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/cfe98a0f73b77370b3807c22493dd508"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/cfe98a0f73b77370b3807c22493dd508?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/id/fb78f606-d23d-4c70-b63a-512ce03d2f90.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/id/fb78f606-d23d-4c70-b63a-512ce03d2f90.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>bahasa Indonesia</dcterms:language>
</entry>
</feed>";
		const string _bagaimanaPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""cfe98a0f73b77370b3807c22493dd508.jpg"" />
<h1>
 Bagaimana Jika?
</h1>
<h2>
 Hari Kumar Nair
</h2>
</body>
</html>";
		const string _bagaimanaPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d0b65a163a77bbe4caf7a8c5664e0e7d.jpg"" />
<p>
 Namaku Shyam. Usiaku 10 tahun.
 Badanku kurus, tapi aku pemberani!
 Aku tidak suka bangun pagi untuk bersekolah.
 Aku ngantuk sekali,  sampai rasanya mau jatuh.
</p>
</body>
</html>";
		const string _bagaimanaPage11Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 11</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""218e51bccf02100958873a40e1e19e23.jpg"" />
<p>
 Ternyata aku masih berdiri dengan sikat gigi di tanganku.
 Aku tersenyum memikirkan dunia rahasiaku.
</p>
</body>
</html>";
		const string _bagaimanaPage12Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 12</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Brought to you by
</p>
<p>
 <img src=""2d85083a3544781ab3cab25d5c38b443.png"" />
</p>
<p>
 Let's Read! is an initiative of The Asia Foundation's Books for Asia program that fosters young readers in Asia.
 booksforasia.org
 To read more books like this and get further information, visit
 letsreadasia.org
 .
</p>
<p>
 Original Story
 What If?,
 author: Hari Kumar Nair
 .
 illustrator: Hari Kumar Nair.
 Published by Pratham Books,
 https://storyweaver.org.in/stories/880-what-if
 © Pratham Books.
 Released under CC BY 4.0.
</p>
<p>
 This work is a modified version of the original story. © The Asia Foundation, 2018. Some rights reserved. Released under CC BY 4.0.
</p>
<p>
 <img src=""3fb7682f683350564d62e624ffbfcac3.jpg"" />
 For full terms of use and attribution,
 http://creativecommons.org/licenses/by/4.0/
</p>
</body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "What If?" published by Pratham Books,
		/// the English original of "Bagaimana Jika?".
		/// It has these distinctive features:
		/// * the front cover page has 3 images
		/// * content pages use paragraph markup for the text
		/// * 2 end pages, the first with (minimal) Pratham markup for book and illustration credits
		/// </summary>
		[Test]
		public void TestConvertingWhatIf_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _whatIfOpfXml, _whatIfOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "What If?");

			// SUT
			convert.ConvertCoverPage(_whatIfPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "What If?", "badd7122aa68d2a339e359f03c03cc51.jpg", @"<p>
 Author:
 Hari Kumar Nair
</p><p>
 Illustrator:
 Hari Kumar Nair
</p>", out XmlElement coverImageData);
			// This book has two extra images on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "61fd7e1fd7a0b699c82eb4f089a455f7.png", "8716a9ccecd3c9b8a45e823d244f7647.png");

			// SUT
			var result = convert.ConvertContentPage(1, _whatIfPage2Xhtml);
			Assert.That(result, Is.True, "converting What If? chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "7afe226493701626fcda5e9d38deb172.jpg", @"<p>My name is Shyam, I am ten years old.</p>
<p>I am a little skinny, but very bold!</p>
<p>Waking up for school is no fun at all</p>
<p>I am so sleepy as I get up, I think I will fall.</p>");

			// SUT
			result = convert.ConvertContentPage(10, _whatIfPage11Xhtml);
			Assert.That(result, Is.True, "converting What If? chapter 11 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "10", 3, "6acdb659741aed3435e3f9b1c36bc64a.jpg", @"<p>I find myself still standing with my brush in my hand,</p>
<p>And I smile and think of my secret little land.</p>");

			// SUT
			result = convert.ConvertContentPage(11, _whatIfPage12Xhtml);
			Assert.That(result, Is.True, "converting What If? chapter 12 (end page 1/2) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(12, _whatIfPage13Xhtml);
			Assert.That(result, Is.True, "converting What If? chapter 13 (end page 2/2) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © Pratham Books, 2015", "CC BY 4.0", "Hari Kumar Nair",
				"Copyright © Pratham Books, 2015", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>This book has been published on StoryWeaver by Pratham Books. The development of this book has been supported by HDFC Asset Management Company Limited (A joint venture with Standard Life Investments).This book was part of the Pratham Books lab conducted in collaboration with Srishti School of Art, Design and Technology, Bangalore. www.prathambooks.org</p>
<p>All images by Hari Kumar Nair. Copyright © Pratham Books, 2015. Some rights reserved. Released under the CC BY 4.0 license.</p>",
				new [] { @"<p>
 What If?
 (English)
</p>",
					"Come, start weaving today, and help us get a book in every child's hand!",
					@"<p>
 When Shyam is sleepy he has trouble brushing his teeth. But dreaming big is no trouble at all.
</p>",
					@"<p>
 This is a Level 1 book for children who are eager to begin reading.
</p>",
					"<img src=\"d710444fa4fa11e970eed00fa1977069.png\" />" });
		}

		const string _whatIfOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">0ffa0b70-4b21-4097-bc39-f22d80842d6f</dc:identifier>
		<dc:title>What If?</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-02-19T07:01:26Z</meta>
		<dc:description>When Shyam is sleepy he has trouble brushing his teeth. But dreaming big is no trouble at all.</dc:description>
		<dc:creator id=""contributor_1"">Hari Kumar Nair</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Hari Kumar Nair</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""badd7122aa68d2a339e359f03c03cc51.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""61fd7e1fd7a0b699c82eb4f089a455f7.png"" id=""image-8009-1"" media-type=""image/png"" />
		<item href=""8716a9ccecd3c9b8a45e823d244f7647.png"" id=""image-8010-2"" media-type=""image/png"" />
		<item href=""7afe226493701626fcda5e9d38deb172.jpg"" id=""image-8011-3"" media-type=""image/jpeg"" />
		<item href=""49682ce9686f1e26ed9c24fc03b729f6.jpg"" id=""image-8012-4"" media-type=""image/jpeg"" />
		<item href=""14c1e7dbff1e0291a0fcb46ddeb82f5f.jpg"" id=""image-8013-5"" media-type=""image/jpeg"" />
		<item href=""f3dd4661625fe36e2b3db68703d7b768.jpg"" id=""image-8014-6"" media-type=""image/jpeg"" />
		<item href=""f6009889dcfc2f1eefedf329449996c4.jpg"" id=""image-8015-7"" media-type=""image/jpeg"" />
		<item href=""7e152c08f46ab89aeadff8739edfea08.jpg"" id=""image-8016-8"" media-type=""image/jpeg"" />
		<item href=""9fbb17c82872b8a375c727c5dea6e93b.jpg"" id=""image-8017-9"" media-type=""image/jpeg"" />
		<item href=""dbef2b7bf9a326e2f48629cb73170ed4.jpg"" id=""image-8018-10"" media-type=""image/jpeg"" />
		<item href=""66fc5028293e61e3e059fe5e5cbfa1ef.jpg"" id=""image-8019-11"" media-type=""image/jpeg"" />
		<item href=""6acdb659741aed3435e3f9b1c36bc64a.jpg"" id=""image-8020-12"" media-type=""image/jpeg"" />
		<item href=""d710444fa4fa11e970eed00fa1977069.png"" id=""image-8021-13"" media-type=""image/png"" />
		<item href=""a5c66ea0438e97ee66266fcc2890dcfd.png"" id=""image-8022-14"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-13.xhtml"" id=""chapter-13"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _whatIfOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:0ffa0b70-4b21-4097-bc39-f22d80842d6f</id>
<title>What If?</title>
<author>
<name>Hari Kumar Nair</name>
</author>
<contributor type=""Illustrator"">
<name>Hari Kumar Nair</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Pratham books</dc:publisher>
<updated>2017-11-10T00:00:00Z</updated>
<dc:created>2017-11-10T00:00:00Z</dc:created>
<published>2017-11-10T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 1"" />
<summary>When Shyam is sleepy he has trouble brushing his teeth. But dreaming big is no trouble at all.</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/badd7122aa68d2a339e359f03c03cc51"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/badd7122aa68d2a339e359f03c03cc51?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/0ffa0b70-4b21-4097-bc39-f22d80842d6f.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/0ffa0b70-4b21-4097-bc39-f22d80842d6f.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _whatIfPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""badd7122aa68d2a339e359f03c03cc51.jpg"" />
<img src=""61fd7e1fd7a0b699c82eb4f089a455f7.png"" />
<img src=""8716a9ccecd3c9b8a45e823d244f7647.png"" />
<p>
 <b>
  What If?
 </b>
</p>
<p>
 Author:
 Hari Kumar Nair
</p>
<p>
 Illustrator:
 Hari Kumar Nair
</p>
</body>
</html>";
		const string _whatIfPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7afe226493701626fcda5e9d38deb172.jpg"" />
<p>
 My name is Shyam, I am ten years old.
</p>
<p>
 I am a little skinny, but very bold!
</p>
<p>
 Waking up for school is no fun at all
</p>
<p>
 I am so sleepy as I get up, I think I will fall.
</p>
</body>
</html>";
		const string _whatIfPage11Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 11</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""6acdb659741aed3435e3f9b1c36bc64a.jpg"" />
<p>
 I find myself still standing with my brush in my hand,
</p>
<p>
 And I smile and think of my secret little land.
</p>
</body>
</html>";
		const string _whatIfPage12Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 12</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d710444fa4fa11e970eed00fa1977069.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following
 <a>
  link
 </a>
 .
</p>
<p>
 Story Attribution:
</p>
This story:
What If?
is written by
<a>
 Hari Kumar Nair
</a>
.
            © Pratham Books
  , 2015. Some rights reserved. Released under CC BY 4.0 license.
<p>
 Other Credits:
</p>
This book has been published on StoryWeaver by Pratham Books. The development of this book has been supported by HDFC Asset Management Company Limited (A joint venture with Standard Life Investments).This book was part of the Pratham Books lab conducted in collaboration with Srishti School of Art, Design and Technology, Bangalore. www.prathambooks.org
<p>
 Illustration Attributions:
</p>
Cover page:
<a>
 Boy's face looking upside down, long legs, pair of hands holding a toothbrush
</a>
, by
<a>
 Hari Kumar Nair
</a>
© Pratham Books, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 2:
<a>
 Sleepy boy sitting on bed
</a>
, by
<a>
 Hari Kumar Nair
</a>
© Pratham Books, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 11:
<a>
 Boy brushing his teeth
</a>
, by
<a>
 Hari Kumar Nair
</a>
© Pratham Books, 2015. Some rights reserved. Released under CC BY 4.0 license.
Disclaimer:
<a>
 https://www.storyweaver.org.in/terms_and_conditions
</a>
<img src=""a5c66ea0438e97ee66266fcc2890dcfd.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution,
 <a>
  http://creativecommons.org/licenses/by/4.0/
 </a>
</p>
</body>
</html>";
		const string _whatIfPage13Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 13</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 What If?
 (English)
</p>
<p>
 When Shyam is sleepy he has trouble brushing his teeth. But dreaming big is no trouble at all.
</p>
<p>
 This is a Level 1 book for children who are eager to begin reading.
</p>
<img src=""d710444fa4fa11e970eed00fa1977069.png"" />
Pratham Books goes digital to weave a whole new chapter in the realm of multilingual children's stories. Knitting together children, authors, illustrators and publishers. Folding in teachers, and translators. To create a rich fabric of openly licensed multilingual stories for the children of India ­­ and the world. Our unique online platform, StoryWeaver, is a playground where children, parents, teachers and librarians can get creative. Come, start weaving today, and help us get a book in every child's hand!
</body>
</html>";


		/// <summary>
		/// This tests converting the Global Digital Library version of "Une maison pour la souris" published by Book Dash.
		/// It has these distinctive features:
		/// * implicit copyright by Book Dash (no explicit copyright mention anywhere in the book)
		/// * French, not English
		/// * no text on title page, just a single image
		/// * one page has a mix of paragraph (&lt;p&gt;) and raw text nodes
		/// * single end page (all in French apart from "Creative Commons Attribution"
		/// </summary>
		[Test]
		public void TestConvertingUneMaison_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "French", UsePortrait = true }, _uneMaisonOpfXml, _uneMaisonOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Une maison pour la souris");

			// SUT
			convert.ConvertCoverPage(_uneMaisonPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Une maison pour la souris", "42fd93119291a1e074074b72938c36f5.jpg", @"<p>Authors: Michele Fry, Amy Uzzell, Jennifer Jacobs</p>", out XmlElement coverImageData, "fr");

			// SUT
			var result = convert.ConvertContentPage(1, _uneMaisonPage2Xhtml);
			Assert.That(result, Is.True, "converting Une maison pour la souris chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "6bde3f7a5ee007f9335bc2fcc059d438.jpg", @"<p>Une souris était à la recherche d’une nouvelle maison.</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(12, _uneMaisonPage13Xhtml);
			Assert.That(result, Is.True, "converting Une maison pour la souris chapter 13 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "12", 3, "c201f3d8f9796de558ce74428efe35cf.jpg", @"<p>Cette nuit-là, la souris fit des rêves chaleureux et douillets.</p>
<p>Réponds aux questions après avoir lu le livre.</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(13, _uneMaisonPage14Xhtml);
			Assert.That(result, Is.True, "converting Une maison pour la souris chapter 14 (end page) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © Book Dash, 2018", "CC BY 4.0", "",
				"Copyright © Book Dash, 2018", "http://creativecommons.org/licenses/by/4.0/",
				"<p>All illustrations copyright © Book Dash, 2018. Some rights reserved. Released under the CC BY 4.0 license.</p>",
				new[] { @"<blockquote> 
 <img src=""787fb029b87454ff3ba0549f59f0ad0d.png"" />",
					"</blockquote>",
					"<p> <em> Une maison pour la souris </em> </p>",
					"<p> Créé par Michele Fry, Amy Uzzell, Jennifer Jacobs </p>",
					"<p> Cette publication est distribuée sous licence internationale Creative Commons Attribution\u00a04.0."});
		}

		const string _uneMaisonOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">17bb2580-3a10-47c2-9c0b-e0c3596b13d8</dc:identifier>
		<dc:title>Une maison pour la souris</dc:title>
		<dc:language>fr</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:03:52Z</meta>
		<dc:description>Une souris est à la recherche d’une nouvelle maison. Des animaux amicaux lui offrent de partager leurs maisons. Mais rien ne lui convient jusqu’à ce qu’elle trouve son petit coin chaud et douillet.</dc:description>
		<dc:creator id=""contributor_1"">Michele Fry</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:creator id=""contributor_2"">Amy Uzzell</dc:creator>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:creator id=""contributor_3"">Jennifer Jacobs</dc:creator>
		<meta refines=""#contributor_3"" property=""role"" scheme=""marc:relators"">aut</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""42fd93119291a1e074074b72938c36f5.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""6bde3f7a5ee007f9335bc2fcc059d438.jpg"" id=""image-2039-1"" media-type=""image/jpeg"" />
		<item href=""ef038bbd6bfefdaa69bb7d27b3f354ea.jpg"" id=""image-2040-2"" media-type=""image/jpeg"" />
		<item href=""2dd9ec09502449de7729b7065a79b14e.jpg"" id=""image-2041-3"" media-type=""image/jpeg"" />
		<item href=""3e321aac7100b9bae1fdc2af7f070f8f.jpg"" id=""image-2042-4"" media-type=""image/jpeg"" />
		<item href=""a9ee8bb36ef2d2e0b233ab6ea75b7d73.jpg"" id=""image-2043-5"" media-type=""image/jpeg"" />
		<item href=""6ad1330f8558b84a1f6daabdb7bdf6ee.jpg"" id=""image-2044-6"" media-type=""image/jpeg"" />
		<item href=""fd3be3c53b78383f14693f626c184fed.jpg"" id=""image-2045-7"" media-type=""image/jpeg"" />
		<item href=""3fa17e0803c7fdde9f8f3929a8642d68.jpg"" id=""image-2046-8"" media-type=""image/jpeg"" />
		<item href=""4583c8dfc401f979eafc433340538958.jpg"" id=""image-2047-9"" media-type=""image/jpeg"" />
		<item href=""b3d865a667105431274e674c5c36ae3e.jpg"" id=""image-2048-10"" media-type=""image/jpeg"" />
		<item href=""27e4b4b41e85f1507240c62f54ae1b4b.jpg"" id=""image-2049-11"" media-type=""image/jpeg"" />
		<item href=""c201f3d8f9796de558ce74428efe35cf.jpg"" id=""image-2051-12"" media-type=""image/jpeg"" />
		<item href=""787fb029b87454ff3ba0549f59f0ad0d.png"" id=""image-2050-13"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-13.xhtml"" id=""chapter-13"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-14.xhtml"" id=""chapter-14"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _uneMaisonOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:17bb2580-3a10-47c2-9c0b-e0c3596b13d8</id>
<title>Une maison pour la souris</title>
<author>
<name>Michele Fry</name>
</author>
<author>
<name>Amy Uzzell</name>
</author>
<author>
<name>Jennifer Jacobs</name>
</author>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Book Dash</dc:publisher>
<updated>2018-03-08T00:00:00Z</updated>
<dc:created>2018-03-08T00:00:00Z</dc:created>
<published>2018-03-08T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Read aloud"" />
<summary>Une souris est à la recherche d’une nouvelle maison. Des animaux amicaux lui offrent de partager leurs maisons. Mais rien ne lui convient jusqu’à ce qu’elle trouve son petit coin chaud et douillet. </summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/42fd93119291a1e074074b72938c36f5"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/42fd93119291a1e074074b72938c36f5?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/fr/17bb2580-3a10-47c2-9c0b-e0c3596b13d8.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/fr/17bb2580-3a10-47c2-9c0b-e0c3596b13d8.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>French</dcterms:language>
</entry>
</feed>";
		const string _uneMaisonPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""42fd93119291a1e074074b72938c36f5.jpg"" /></body>
</html>";
		const string _uneMaisonPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""6bde3f7a5ee007f9335bc2fcc059d438.jpg"" /> 
<p> Une souris était à la recherche d’une nouvelle maison. </p></body>
</html>";
		const string _uneMaisonPage13Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 13</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""c201f3d8f9796de558ce74428efe35cf.jpg"" /> 
<p> Cette nuit-là, la souris fit des rêves chaleureux et douillets. </p> Réponds aux questions après avoir lu le livre.</body>
</html>";
		const string _uneMaisonPage14Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 14</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><blockquote> 
 <img src=""787fb029b87454ff3ba0549f59f0ad0d.png"" /> 
 <p> <em> Une maison pour la souris </em> </p> 
 <p> Créé par Michele Fry, Amy Uzzell, Jennifer Jacobs </p> 
 <p> </p> 
 <p> Cette publication est distribuée sous licence internationale Creative Commons Attribution&#xa0;4.0. Cela signifie que vous êtes libre de partager (de copier et de redistribuer le matériel sur n’importe quel support ou sous n’importe quel format) et de l’adapter (de le remixer, de le transformer et de vous en inspirer) à n’importe quelle fin, même commerciale, à condition d’indiquer la provenance avec un lien vers votre source et d'indiquer que des modifications ont été apportées. Vous pouvez indiquer ces informations de n’importe quelle façon raisonnable, sans pourtant suggérer que le concédant de licence vous soutient ou soutient la façon dont vous avez utilisé son œuvre. </p> 
 <p> </p> 
</blockquote></body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "সমীর ক্ষুধার্ত" published by The Asia Foundation.
		/// It has these distinctive features:
		/// * Bengali, not English, in a non-Roman script
		/// * front cover uses &lt;h1&gt; and &lt;h2&gt; instead of &lt;p&gt; for title and author markup.
		/// * single end page in English with Bengali title and names
		/// * licensed under CC BY-NC 4.0 instead of CC BY 4.0
		/// </summary>
		/// <remarks>
		/// If we can figure out anything better to do to handle the end page, this test will need to be updated.
		/// </remarks>
		[Test]
		public void TestConvertingBengaliBook_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "Bengali", UsePortrait = true }, _bengaliOpfXml, _bengaliOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, 
				"সমীর ক্ষুধার্ত"
				);

			// SUT
			convert.ConvertCoverPage(_bengaliPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, 
				"সমীর ক্ষুধার্ত",
				"5d0a7f714e5fdcd4bbf6429c8b0d691a.jpg",
				 @"<p>Rochak Dahal</p>",
				 out XmlElement coverImageData, "bn");

			// SUT
			var result = convert.ConvertContentPage(1, _bengaliPage2Xhtml);
			Assert.That(result, Is.True, "converting Bengali book chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "33746d4a84ed4d9d40154ccced7db932.jpg",
				@"<p>“বাবা, বাবা! আমার খিদে পেয়েছে। নাস্তা কী আছে?"" সমীর জিজ্ঞেস করে।</p>",
				"bn");

			// SUT
			result = convert.ConvertContentPage(11, _bengaliPage12Xhtml);
			Assert.That(result, Is.True, "converting Bengali book chapter 12 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "11", 3, "8011dc950bc3cbe5abbb21de3a389f15.jpg",
				@"<p>“ওহ দারুণ! আমি সব ধরনের খাবার খাবো — শুধু গোল খাবার নয়!” সমীর বলে। “তিনকোণা সমোসা, চারকোনা মিষ্টি, লম্বাটে গাজর এবং রঙিন আইসক্রিম খাওয়ার জন্য মন অস্থির হয়ে আছে।”</p>",
				"bn");

			// SUT
			result = convert.ConvertContentPage(12, _bengaliPage13Xhtml);
			Assert.That(result, Is.True, "converting Bengali book chapter 13 (end page) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © The Asia Foundation, 2018", "CC BY-NC 4.0", "Mrigaja Bajracharya",
				"Copyright © The Asia Foundation, 2018", "http://creativecommons.org/licenses/by-nc/4.0/",
				"<p>All illustrations by Mrigaja Bajracharya. Copyright © The Asia Foundation, 2018. Some rights reserved. Released under the CC BY-NC 4.0 license.</p>",
				new[] { @"<p>
 Brought to you by
</p>",
					@"http://creativecommons.org/licenses/by-nc/4.0/
</p>",
					"Let's Read! is an initiative of The Asia Foundation's Books for Asia program that fosters young readers in Asia.",
					@"<p>
 Original Story
 आज के खाने?,
 author: रोचक दाहाल
 .
 illustrator: मृगजा बज्राचार्य.
 Published by The Asia Foundation,
 https://www.letsreadasia.org
 © The Asia Foundation.
 Released under CC BY-NC 4.0.
</p>",
					"This work is a modified version of the original story. © The Asia Foundation, 2018. Some rights reserved. Released under CC BY-NC 4.0."});
		}

		const string _bengaliOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">b7628365-15a3-4dd7-af57-c9ba9350fb6b</dc:identifier>
		<dc:title>সমীর ক্ষুধার্ত</dc:title>
		<dc:language>bn</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:03:42Z</meta>
		<dc:description>সমীর ক্ষুধার্ত কিন্তু কী খাবার আছে? তার পরিবার যে খাবারের কথা বলবে তা কি সে অনুমান করতে পারে?</dc:description>
		<dc:creator id=""contributor_1"">Rochak Dahal</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Mrigaja Bajracharya</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""5d0a7f714e5fdcd4bbf6429c8b0d691a.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""33746d4a84ed4d9d40154ccced7db932.jpg"" id=""image-5130-1"" media-type=""image/jpeg"" />
		<item href=""a155afebcdbfb01147301e2050e239f3.jpg"" id=""image-5131-2"" media-type=""image/jpeg"" />
		<item href=""7e8f9550dc595a432cfe2d9597ad6072.jpg"" id=""image-5132-3"" media-type=""image/jpeg"" />
		<item href=""2d57252d0be37c4db628f6a9da812929.jpg"" id=""image-5133-4"" media-type=""image/jpeg"" />
		<item href=""1ce5c502b2bacc73372ca8370aff2498.jpg"" id=""image-5134-5"" media-type=""image/jpeg"" />
		<item href=""dfd0835ca1e03d8e014b05b80d6f37c4.jpg"" id=""image-5135-6"" media-type=""image/jpeg"" />
		<item href=""8a080d7301d768a52ee1374155570cfd.jpg"" id=""image-5136-7"" media-type=""image/jpeg"" />
		<item href=""d89da78c0d632331ee6888e3551d3b2b.jpg"" id=""image-5137-8"" media-type=""image/jpeg"" />
		<item href=""940f097993f7ae966cf9fac03a99db7e.jpg"" id=""image-5138-9"" media-type=""image/jpeg"" />
		<item href=""23fc4f310010b0c4577717803ee0e9de.jpg"" id=""image-5139-10"" media-type=""image/jpeg"" />
		<item href=""8011dc950bc3cbe5abbb21de3a389f15.jpg"" id=""image-5140-11"" media-type=""image/jpeg"" />
		<item href=""4905d48250cb70ebe23ff7639fc48d24.png"" id=""image-5141-12"" media-type=""image/png"" />
		<item href=""7768d2b166124036ae0d84f2f75f731f.png"" id=""image-5142-13"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-11.xhtml"" id=""chapter-11"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-13.xhtml"" id=""chapter-13"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _bengaliOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:b7628365-15a3-4dd7-af57-c9ba9350fb6b</id>
<title>সমীর ক্ষুধার্ত</title>
<author>
<name>Rochak Dahal</name>
</author>
<contributor type=""Illustrator"">
<name>Mrigaja Bajracharya</name>
</contributor>
<dc:license>Creative Commons Attribution Non Commercial 4.0 International</dc:license>
<dc:publisher>The Asia Foundation</dc:publisher>
<updated>2018-04-19T00:00:00Z</updated>
<dc:created>2018-04-19T00:00:00Z</dc:created>
<published>2018-04-19T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 1"" />
<summary>সমীর ক্ষুধার্ত কিন্তু কী খাবার আছে? তার পরিবার যে খাবারের কথা বলবে তা কি সে অনুমান করতে পারে?</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/5d0a7f714e5fdcd4bbf6429c8b0d691a"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/5d0a7f714e5fdcd4bbf6429c8b0d691a?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/bn/b7628365-15a3-4dd7-af57-c9ba9350fb6b.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/bn/b7628365-15a3-4dd7-af57-c9ba9350fb6b.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>বাঙালি</dcterms:language>
</entry>
</feed>";
		const string _bengaliPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""5d0a7f714e5fdcd4bbf6429c8b0d691a.jpg"" />
<h1>
 সমীর ক্ষুধার্ত
</h1>
<h2>
 Rochak Dahal
</h2>
</body>
</html>";
		const string _bengaliPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""33746d4a84ed4d9d40154ccced7db932.jpg"" />
<p>
 “বাবা, বাবা! আমার খিদে পেয়েছে। নাস্তা কী আছে?"" সমীর জিজ্ঞেস করে।
</p>
</body>
</html>";
		const string _bengaliPage12Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 12</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""8011dc950bc3cbe5abbb21de3a389f15.jpg"" />
<p>
 “ওহ দারুণ! আমি সব ধরনের খাবার খাবো — শুধু গোল খাবার নয়!” সমীর বলে। “তিনকোণা সমোসা, চারকোনা মিষ্টি, লম্বাটে গাজর এবং রঙিন আইসক্রিম খাওয়ার জন্য মন অস্থির হয়ে আছে।”
</p>
</body>
</html>";
		const string _bengaliPage13Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 13</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Brought to you by
</p>
<p>
 <img src=""4905d48250cb70ebe23ff7639fc48d24.png"" />
</p>
<p>
 Let's Read! is an initiative of The Asia Foundation's Books for Asia program that fosters young readers in Asia.
 booksforasia.org
 To read more books like this and get further information, visit
 letsreadasia.org
 .
</p>
<p>
 Original Story
 आज के खाने?,
 author: रोचक दाहाल
 .
 illustrator: मृगजा बज्राचार्य.
 Published by The Asia Foundation,
 https://www.letsreadasia.org
 © The Asia Foundation.
 Released under CC BY-NC 4.0.
</p>
<p>
 This work is a modified version of the original story. © The Asia Foundation, 2018. Some rights reserved. Released under CC BY-NC 4.0.
</p>
<p>
 <img src=""7768d2b166124036ae0d84f2f75f731f.png"" />
 For full terms of use and attribution,
 http://creativecommons.org/licenses/by-nc/4.0/
</p>
</body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "The Birthday Party" published by Pratham Books.
		/// It has these distinctive features:
		/// * the front cover page has 3 images
		/// * wordless story: most pages have no text
		/// * 2 end pages, the first with (minimal) Pratham markup for book and illustration credits.  The copyright
		///   for each illustration varies between the artist and Pratham Books.
		/// </summary>
		[Test]
		public void TestConvertingBirthday_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _birthdayOpfXml, _birthdayOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "The Birthday Party");

			// SUT
			convert.ConvertCoverPage(_birthdayPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "The Birthday Party", "01d36947d2cd48e29d9239fc22d2f2dd.jpg", @"<p>
 Author:
 Storyweaver, Pratham Books
</p><p>
 Illustrator:
 Megha Vishwanath
</p>", out XmlElement coverImageData);
			// This book has two extra images on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "327960bfcbc83b0500bbb87e89866273.png", "8716a9ccecd3c9b8a45e823d244f7647.png");

			// SUT
			var result = convert.ConvertContentPage(1, _birthdayPage2Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 2 succeeded");
			var page1Img = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "296dc94e65540e945f21665a7c44a801.jpg", null);

			// SUT
			result = convert.ConvertContentPage(2, _birthdayPage3Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 3 succeeded");
			var page2Img = CheckTrueContentPageImport(convert._bloomDoc, "2", 3, "7935930bd3ead65ff8abbb51c14fd303.jpg", null);

			// SUT
			result = convert.ConvertContentPage(3, _birthdayPage4Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 4 succeeded");
			var page3Img = CheckTrueContentPageImport(convert._bloomDoc, "3", 4, "2aa8778355a0f25384d96e8421402c6a.jpg", null);

			// SUT
			result = convert.ConvertContentPage(4, _birthdayPage5Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 5 succeeded");
			var page4Img = CheckTrueContentPageImport(convert._bloomDoc, "4", 5, "df22465a96ac6839a15a9de54dd5d417.jpg", null);

			// SUT
			result = convert.ConvertContentPage(5, _birthdayPage6Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 6 succeeded");
			var page5Img = CheckTrueContentPageImport(convert._bloomDoc, "5", 6, "c81aac9617591f5591d3bf8e500e8c89.jpg", null);

			// SUT
			result = convert.ConvertContentPage(6, _birthdayPage7Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 7 succeeded");
			var page6Img = CheckTrueContentPageImport(convert._bloomDoc, "6", 7, "81fc38f93c2b8c8d53bddd7ec9eb8ec6.jpg", null);

			// SUT
			result = convert.ConvertContentPage(7, _birthdayPage8Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 8 succeeded");
			var page7Img = CheckTrueContentPageImport(convert._bloomDoc, "7", 8, "e723496d42cb09497980634d6ba9d9a7.jpg", @"<p><b>Wondering what to do with wordless stories?</b></p>
<p>Wordless stories are wonderful because they contain infinite possibilities. Here are a few ideas for engaging with children using visual stories:</p>
<p>- Explore the story in a leisurely manner. Draw attention to the details - the expressions of the characters, setting, colours, etc. The idea is for each child to build her own story. If the story is being shown to a group of children, you could ask each of them to contribute a sentence or two for each illustration. Take joy in exploring each illustration and build the story as you go along.</p>
<p>- Use themes explored in the story to start a discussion. For instance, in this story, you could ask children about what they do for their birthday, or even how they help out at home.</p>
<p>- Encourage children to create 2-3 different stories using the same set of visuals. This will help push their imagination.</p>");

			// SUT
			result = convert.ConvertContentPage(8, _birthdayPage9Xhtml);
			Assert.That(result, Is.True, "converting The Birthday Party chapter 9 (end page 1/2) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(8), "Eight pages should exist after converting the cover page, seven content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(9, _birthdayPage10Xhtml);
			Assert.That(result, Is.True, "converting What If? chapter 10 (end page 2/2) succeeded");
			// We can't use the normal checking method because it assumes only 2 content pages and we have 7.
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(8), "Eight pages should exist after converting the cover page, seven content pages, and two end pages.");
			var imageCreator = "Megha Vishwanath";
			var imageCopyright = "Copyright © Megha Vishwanath, 2015";
			var imageLicense = "CC BY 4.0";
			CheckImageMetaData(coverImageData, imageCreator, "Copyright © Pratham Books, 2015", imageLicense);
			CheckImageMetaData(coverImg, imageCreator, "Copyright © Pratham Books, 2015", imageLicense);
			CheckImageMetaData(page1Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page2Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page3Img, imageCreator, "Copyright © Pratham Books, 2015", imageLicense);
			CheckImageMetaData(page4Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page5Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page6Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page7Img, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyrightUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets copyrightUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Images on Front Cover, page 3 by Megha Vishwanath. Copyright © Pratham Books, 2015. Some rights reserved. Released under the CC BY 4.0 license.</p>
<p>Images on pages 1-2, 4-7 by Megha Vishwanath. Copyright © Megha Vishwanath, 2015. Some rights reserved. Released under the CC BY 4.0 license.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © Pratham Books, 2015"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Not.Null, "End page sets the inside back cover in the data div");
			Assert.That(insideBackCoverData.InnerXml, Does.StartWith(@"<p>
 The Birthday Party
 (English)
</p>"));
			Assert.That(insideBackCoverData.InnerXml.Trim(), Does.EndWith(@"Come, start weaving today, and help us get a book in every child's hand!"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"After his birthday party, the boy in the story opens his gifts and is thrilled to find a camera."));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"<p>
 This is a Level 1 book for children who are eager to begin reading.
</p>"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"<img src=""d710444fa4fa11e970eed00fa1977069.png"" />"));
		}

		const string _birthdayOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">25d40f31-5c64-427a-922d-fb5d6902b711</dc:identifier>
		<dc:title>The Birthday Party</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:03:14Z</meta>
		<dc:description>After his birthday party, the boy in the story opens his gifts and is thrilled to find a camera. But as he&apos;s playing with his new gift, he notices his mother crying in the kitchen. Find out what he does next!</dc:description>
		<dc:creator id=""contributor_1"">Storyweaver, Pratham Books</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Megha Vishwanath</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""01d36947d2cd48e29d9239fc22d2f2dd.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""327960bfcbc83b0500bbb87e89866273.png"" id=""image-7814-1"" media-type=""image/png"" />
		<item href=""8716a9ccecd3c9b8a45e823d244f7647.png"" id=""image-7815-2"" media-type=""image/png"" />
		<item href=""296dc94e65540e945f21665a7c44a801.jpg"" id=""image-7816-3"" media-type=""image/jpeg"" />
		<item href=""7935930bd3ead65ff8abbb51c14fd303.jpg"" id=""image-7817-4"" media-type=""image/jpeg"" />
		<item href=""2aa8778355a0f25384d96e8421402c6a.jpg"" id=""image-7818-5"" media-type=""image/jpeg"" />
		<item href=""df22465a96ac6839a15a9de54dd5d417.jpg"" id=""image-7819-6"" media-type=""image/jpeg"" />
		<item href=""c81aac9617591f5591d3bf8e500e8c89.jpg"" id=""image-7820-7"" media-type=""image/jpeg"" />
		<item href=""81fc38f93c2b8c8d53bddd7ec9eb8ec6.jpg"" id=""image-7821-8"" media-type=""image/jpeg"" />
		<item href=""e723496d42cb09497980634d6ba9d9a7.jpg"" id=""image-7822-9"" media-type=""image/jpeg"" />
		<item href=""d710444fa4fa11e970eed00fa1977069.png"" id=""image-7823-10"" media-type=""image/png"" />
		<item href=""a5c66ea0438e97ee66266fcc2890dcfd.png"" id=""image-7824-11"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-3.xhtml"" id=""chapter-3"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-4.xhtml"" id=""chapter-4"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-5.xhtml"" id=""chapter-5"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-6.xhtml"" id=""chapter-6"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-7.xhtml"" id=""chapter-7"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-8.xhtml"" id=""chapter-8"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-9.xhtml"" id=""chapter-9"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-10.xhtml"" id=""chapter-10"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _birthdayOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:25d40f31-5c64-427a-922d-fb5d6902b711</id>
<title>The Birthday Party</title>
<author>
<name>Storyweaver, Pratham Books</name>
</author>
<contributor type=""Illustrator"">
<name>Megha Vishwanath</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Pratham books</dc:publisher>
<updated>2017-11-10T00:00:00Z</updated>
<dc:created>2017-11-10T00:00:00Z</dc:created>
<published>2017-11-10T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 1"" />
<summary>After his birthday party, the boy in the story opens his gifts and is thrilled to find a camera. But as he's playing with his new gift, he notices his mother crying in the kitchen. Find out what he does next!</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/01d36947d2cd48e29d9239fc22d2f2dd"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/01d36947d2cd48e29d9239fc22d2f2dd?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/25d40f31-5c64-427a-922d-fb5d6902b711.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/25d40f31-5c64-427a-922d-fb5d6902b711.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _birthdayPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""01d36947d2cd48e29d9239fc22d2f2dd.jpg"" />
<img src=""327960bfcbc83b0500bbb87e89866273.png"" />
<img src=""8716a9ccecd3c9b8a45e823d244f7647.png"" />
<p>
 <b>
  The Birthday Party
 </b>
</p>
<p>
 Author:
 Storyweaver, Pratham Books
</p>
<p>
 Illustrator:
 Megha Vishwanath
</p>
</body>
</html>";
		const string _birthdayPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""296dc94e65540e945f21665a7c44a801.jpg"" />
</body>
</html>";
		const string _birthdayPage3Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 3</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7935930bd3ead65ff8abbb51c14fd303.jpg"" />
</body>
</html>";
		const string _birthdayPage4Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 4</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""2aa8778355a0f25384d96e8421402c6a.jpg"" />
</body>
</html>";
		const string _birthdayPage5Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 5</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""df22465a96ac6839a15a9de54dd5d417.jpg"" />
</body>
</html>";
		const string _birthdayPage6Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 6</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""c81aac9617591f5591d3bf8e500e8c89.jpg"" />
</body>
</html>";
		const string _birthdayPage7Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 7</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""81fc38f93c2b8c8d53bddd7ec9eb8ec6.jpg"" />
</body>
</html>";
		const string _birthdayPage8Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 8</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""e723496d42cb09497980634d6ba9d9a7.jpg"" />
<p>
 <b>
  Wondering what to do with wordless stories?
  <br />
 </b>
</p>
<p>
 <br />
</p>
<p>
 Wordless stories are wonderful because they contain infinite possibilities. Here are a few ideas for engaging with children using visual stories:
 <br />
</p>
<p>
 <br />
 - Explore the story in a leisurely manner. Draw attention to the details - the expressions of the characters, setting, colours, etc. The idea is for each child to build her own story. If the story is being shown to a group of children, you could ask each of them to contribute a sentence or two for each illustration. Take joy in exploring each illustration and build the story as you go along.
 <br />
</p>
<p>
 <br />
</p>
<p>
 - Use themes explored in the story to start a discussion. For instance, in this story, you could ask children about what they do for their birthday, or even how they help out at home.
</p>
<p>
 <br />
</p>
<p>
 - Encourage children to create 2-3 different stories using the same set of visuals. This will help push their imagination.
</p>
</body>
</html>";
		const string _birthdayPage9Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 9</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d710444fa4fa11e970eed00fa1977069.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following
 <a>
  link
 </a>
 .
</p>
<p>
 Story Attribution:
</p>
This story:
The Birthday Party
is written by
<a>
 Storyweaver, Pratham Books
</a>
.
            © Pratham Books
  , 2015. Some rights reserved. Released under CC BY 4.0 license.
<p>
 Illustration Attributions:
</p>
Cover page:
<a>
 Boy taking photographs, mother crying in the kitchen
</a>
, by
<a>
 Megha Vishwanath
</a>
© Pratham Books, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 2:
<a>
 Birthday party
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 3:
<a>
 A boy opening gifts after a party
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 4:
<a>
 Boy taking photographs, mother crying in the kitchen
</a>
, by
<a>
 Megha Vishwanath
</a>
© Pratham Books, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 5:
<a>
 Crying mother and son
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 6:
<a>
 Boy and mother in the kitchen
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 7:
<a>
 Camera Frame
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Page 8:
<a>
 Crying mother and son
</a>
, by
<a>
 Megha Vishwanath
</a>
© Megha Vishwanath, 2015. Some rights reserved. Released under CC BY 4.0 license.
Disclaimer:
<a>
 https://www.storyweaver.org.in/terms_and_conditions
</a>
<img src=""a5c66ea0438e97ee66266fcc2890dcfd.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution,
 <a>
  http://creativecommons.org/licenses/by/4.0/
 </a>
</p>
</body>
</html>";
		const string _birthdayPage10Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 10</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 The Birthday Party
 (English)
</p>
<p>
 After his birthday party, the boy in the story opens his gifts and is thrilled to find a camera. But as he's playing with his new gift, he notices his mother crying in the kitchen. Find out what he does next!
</p>
<p>
 This is a Level 1 book for children who are eager to begin reading.
</p>
<img src=""d710444fa4fa11e970eed00fa1977069.png"" />
Pratham Books goes digital to weave a whole new chapter in the realm of multilingual children's stories. Knitting together children, authors, illustrators and publishers. Folding in teachers, and translators. To create a rich fabric of openly licensed multilingual stories for the children of India ­­ and the world. Our unique online platform, StoryWeaver, is a playground where children, parents, teachers and librarians can get creative. Come, start weaving today, and help us get a book in every child's hand!
</body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "Trop de Bananes" published by Pratham Books.
		/// It has these distinctive features:
		/// * artist list has & character
		/// * French, not English
		/// * 4 end pages of credits
		/// * 3 images on front cover page
		/// </summary>
		[Test]
		public void TestConvertingTropDeBananes_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "French", UsePortrait = true }, _bananasOpfXml, _bananasOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Trop de bananes");

			// SUT
			convert.ConvertCoverPage(_bananasPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Trop de bananes", "c40b4194867c7f8f4dbd5ff633653996.jpg", "<p> Auteur\u00a0: Rohini Nilekani </p>" +
				"<p> Illustrateurs\u00a0: Angie et Upesh </p>", out XmlElement coverImageData, "fr");
			// This book has two extra images on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "61fd7e1fd7a0b699c82eb4f089a455f7.png", "95e805cc9f03ab235937124ab44755fa.png");

			// SUT
			var result = convert.ConvertContentPage(1, _bananasPage2Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 2 succeeded");
			var page1Img = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "d1ef3e2832dfc9b6deff6f80fd23dbfc.jpg", @"<p>Sringeri Srinivas passait une très mauvaise journée.</p>
<p>Personne ne voulait des bananes mûres et sucrées qu’il cultivait dans sa ferme.</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(2, _bananasPage12Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 12 succeeded");
			var page2Img = CheckTrueContentPageImport(convert._bloomDoc, "2", 3, "7e447cd435fc7fbd0e1eba3826e7f8b7.jpg",
				"<p>Pas avec les commerçants qui pouvaient vendre les bananes sur des marchés lointains. Et même pas avec ses vaches\u00a0!</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(3, _bananasPage20Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 20 succeeded");
			var page3Img = CheckTrueContentPageImport(convert._bloomDoc, "3", 4, "74f123aa6e096acbb941dab70a3dca99.jpg",
				"<p>Au même moment, Sringeri Srinivas entra en portant un gros sac.</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(4, _bananasPage24Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 24 succeeded");
			var page4Img = CheckTrueContentPageImport(convert._bloomDoc, "4", 5, "9ba4b52e528a51c2d632545904e3a3e9.jpg",
				"<p>Le prêtre était tellement surpris qu’il en oublia de psalmodier. Dans le silence, un enfant commença à rire.</p>", "fr");

			// SUT
			result = convert.ConvertContentPage(5, _bananasPage27Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 27 succeeded");
			var page5Img = CheckTrueContentPageImport(convert._bloomDoc, "5", 6, "7493b48ae67b8b766a7826728a5c8cd5.jpg",
				@"<p><b>LE <b>SAVIEZ-VOUS" + "\u00a0" + @"?</b></b></p>
<p><b>Faits <b>à propos <b>des bananes</b></b></b></p>
<p>Le mot banane est dérivé du mot arabe signifiant «"+"\u00a0"+@"doigt"+"\u00a0"+@"». L’Inde est le plus grand producteur de bananes au monde. Plus de 120"+"\u00a0"+ @"variétés de bananes comestibles y sont cultivées. Le Centre national de la recherche sur les bananes à Trichy possède une collection de 1"+"\u00a0120\u00a0"+@"variétés de bananes"+"\u00a0"+@"!</p>
<p>Les bananes sont riches en minéraux qui aident à stimuler le cerveau. Les bananes rendent les étudiants plus actifs et alertes. De nombreuses friandises indiennes sont confectionnées à l’aide de bananes"+"\u00a0"+@": le payasam de banane dans le Kerala, le rasayana de banane dans le Karnataka, le halva de banane Halwa, le Rawa Kela-Gur Mithai. En savez-vous davantage"+"\u00a0"+@"?</p>",
				"fr");

			// SUT
			result = convert.ConvertContentPage(6, _bananasPage28Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 28 (end page 1/4) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(6), "Six pages should exist after converting the cover page, five content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(7, _bananasPage29Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 29 (end page 2/4) succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(6), "Six pages should exist after converting the cover page, five content pages, and two end pages.");

			// SUT
			result = convert.ConvertContentPage(8, _bananasPage30Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 30 (end page 3/4) succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(6), "Six pages should exist after converting the cover page, five content pages, and three end pages.");

			// SUT
			result = convert.ConvertContentPage(9, _bananasPage31Xhtml);
			Assert.That(result, Is.True, "converting Trop de bananes chapter 31 (end page 4/4) succeeded");
			// We can't use the normal checking method because it assumes only 2 content pages and we have 5.
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(6), "Six pages should exist after converting the cover page, five content pages, and four end pages.");
			var imageCreator = "Angie & Upesh";
			var imageCopyright = "Copyright © Pratham Books, 2010";
			var imageLicense = "CC BY 4.0";
			CheckImageMetaData(coverImageData, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(coverImg, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page1Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page2Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page3Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page4Img, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(page5Img, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyrightUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets copyrightUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(
				@"<p>This book has been published on StoryWeaver by Pratham Books. The author of this book, Rohini Nilekani used to earlier write under the pseudonym 'Noni'. The print version of 'Too Many Bananas' has been published by Pratham Books with the support by Nikki Gulati. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org</p>
<p>All images by Angie &amp; Upesh. Copyright © Pratham Books, 2010. Some rights reserved. Released under the CC BY 4.0 license.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © Pratham Books, 2010"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Not.Null, "End page sets the inside back cover in the data div");
			Assert.That(insideBackCoverData.InnerXml, Does.StartWith(@"<p>
 Too Many Bananas
 (English)
</p>"));
			Assert.That(insideBackCoverData.InnerXml.Trim(), Does.EndWith(@"Come, start weaving today, and help us get a book in every child's hand!"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"<p>
 No one wanted to buy the sweet bananas that Sringeri Srinivas grew on his farm. Find out what he did with them in this cute story.
</p>"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"<p>
 This is a Level 2 book for children who recognize familiar words and can read new words with help.
</p>"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain(@"<img src=""d710444fa4fa11e970eed00fa1977069.png"" />"));
		}

		const string _bananasOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">5ccb52cc-66c6-481c-92b2-f7f2fc90126e</dc:identifier>
		<dc:title>Trop de bananes</dc:title>
		<dc:language>fr</dc:language>
		<meta property=""dcterms:modified"">2020-02-21T10:41:28Z</meta>
		<dc:description>Personne ne voulait acheter les bananes sucrées que Sringeri Srinivas cultivait dans sa ferme. Découvrez ce qu’il en a fait dans cette jolie histoire.</dc:description>
		<dc:creator id=""contributor_1"">Rohini Nilekani</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Angie &amp; Upesh</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
		<dc:contributor id=""contributor_3"">Global Digital Library</dc:contributor>
		<meta refines=""#contributor_3"" property=""role"" scheme=""marc:relators"">trl</meta>
		<dc:contributor id=""contributor_4"">INSÈRE TON NOM ICI</dc:contributor>
		<meta refines=""#contributor_4"" property=""role"" scheme=""marc:relators"">trl</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""c40b4194867c7f8f4dbd5ff633653996.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""61fd7e1fd7a0b699c82eb4f089a455f7.png"" id=""image-9790-1"" media-type=""image/png"" />
		<item href=""95e805cc9f03ab235937124ab44755fa.png"" id=""image-9791-2"" media-type=""image/png"" />
		<item href=""d1ef3e2832dfc9b6deff6f80fd23dbfc.jpg"" id=""image-9792-3"" media-type=""image/jpeg"" />
		<item href=""57d0bb1ff550ac967b38eb6378d4645e.jpg"" id=""image-9793-4"" media-type=""image/jpeg"" />
		<item href=""e1cac78ff611abb8ffaf87941af1013f.jpg"" id=""image-9794-5"" media-type=""image/jpeg"" />
		<item href=""7fa2c3a16f1b74834fdbd8833fc46727.jpg"" id=""image-9795-6"" media-type=""image/jpeg"" />
		<item href=""72b6efb334032b8a1f1db9cc910676b7.jpg"" id=""image-9796-7"" media-type=""image/jpeg"" />
		<item href=""a18fde940fb6c35958a8071d66058714.jpg"" id=""image-9797-8"" media-type=""image/jpeg"" />
		<item href=""c329a22bbc9c749f5650e596937807d0.jpg"" id=""image-9798-9"" media-type=""image/jpeg"" />
		<item href=""0aa054acb34df5a02b6d2866f5c06850.jpg"" id=""image-9799-10"" media-type=""image/jpeg"" />
		<item href=""86b7465b34d7d8900c7d11de1e4c0781.jpg"" id=""image-9800-11"" media-type=""image/jpeg"" />
		<item href=""95bf1c06bf5010e68c04664607c57336.jpg"" id=""image-9780-12"" media-type=""image/jpeg"" />
		<item href=""7e447cd435fc7fbd0e1eba3826e7f8b7.jpg"" id=""image-9781-13"" media-type=""image/jpeg"" />
		<item href=""d0e780b6de81768a8f09c56e79d5389a.jpg"" id=""image-9783-14"" media-type=""image/jpeg"" />
		<item href=""61c4dd9a4f5c7eaf6b181178f128ddba.jpg"" id=""image-9784-15"" media-type=""image/jpeg"" />
		<item href=""2b5618c14074c72d971defdbc7908643.jpg"" id=""image-9785-16"" media-type=""image/jpeg"" />
		<item href=""e1cd967e8ce91d3a05dd2280c3e92d62.jpg"" id=""image-9787-17"" media-type=""image/jpeg"" />
		<item href=""35ce818b7ccab3896502366cad03f2a5.jpg"" id=""image-9788-18"" media-type=""image/jpeg"" />
		<item href=""eaf320d1da93d234f1ffb4d63a569e50.jpg"" id=""image-9786-19"" media-type=""image/jpeg"" />
		<item href=""5af48de345d79d56cc0fa0e0ff0f851a.jpg"" id=""image-9782-20"" media-type=""image/jpeg"" />
		<item href=""74f123aa6e096acbb941dab70a3dca99.jpg"" id=""image-9801-21"" media-type=""image/jpeg"" />
		<item href=""4ad22dbee0765619458b2c93a0c1a3c8.jpg"" id=""image-9802-22"" media-type=""image/jpeg"" />
		<item href=""1e2293e5bea2d72019a544b12d1bee99.jpg"" id=""image-9803-23"" media-type=""image/jpeg"" />
		<item href=""f3aedcdfb0c65b279448268f70bfeca4.jpg"" id=""image-9804-24"" media-type=""image/jpeg"" />
		<item href=""9ba4b52e528a51c2d632545904e3a3e9.jpg"" id=""image-9805-25"" media-type=""image/jpeg"" />
		<item href=""e74638f18902204087172952d098266c.jpg"" id=""image-9806-26"" media-type=""image/jpeg"" />
		<item href=""180070f10d1665fafdde98fb285c5fed.jpg"" id=""image-9807-27"" media-type=""image/jpeg"" />
		<item href=""7493b48ae67b8b766a7826728a5c8cd5.jpg"" id=""image-9808-28"" media-type=""image/jpeg"" />
		<item href=""d710444fa4fa11e970eed00fa1977069.png"" id=""image-9809-29"" media-type=""image/png"" />
		<item href=""a5c66ea0438e97ee66266fcc2890dcfd.png"" id=""image-9810-30"" media-type=""image/png"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-2.xhtml"" id=""chapter-2"" media-type=""application/xhtml+xml"" />
		<!-- we need to exclude a number of pages so that the test for end pages not being more than half
			the total number of pages will succeed properly. -->
		<item href=""chapter-12.xhtml"" id=""chapter-12"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-20.xhtml"" id=""chapter-20"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-24.xhtml"" id=""chapter-24"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-27.xhtml"" id=""chapter-27"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-28.xhtml"" id=""chapter-28"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-29.xhtml"" id=""chapter-29"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-30.xhtml"" id=""chapter-30"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-31.xhtml"" id=""chapter-31"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _bananasOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:5ccb52cc-66c6-481c-92b2-f7f2fc90126e</id>
<title>Trop de bananes</title>
<author>
<name>Rohini Nilekani</name>
</author>
<contributor type=""Illustrator"">
<name>Angie &amp; Upesh</name>
</contributor>
<contributor type=""Translator"">
<name>Global Digital Library</name>
</contributor>
<contributor type=""Translator"">
<name>INSÈRE TON NOM ICI</name>
</contributor>
<dc:license>Creative Commons Attribution 4.0 International</dc:license>
<dc:publisher>Pratham books</dc:publisher>
<updated>2017-11-10T00:00:00Z</updated>
<dc:created>2017-11-10T00:00:00Z</dc:created>
<published>2017-11-10T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 2"" />
<summary>Personne ne voulait acheter les bananes sucrées que Sringeri Srinivas cultivait dans sa ferme. Découvrez ce qu’il en a fait dans cette jolie histoire.</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/c40b4194867c7f8f4dbd5ff633653996"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/c40b4194867c7f8f4dbd5ff633653996?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/fr/5ccb52cc-66c6-481c-92b2-f7f2fc90126e.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/fr/5ccb52cc-66c6-481c-92b2-f7f2fc90126e.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>French</dcterms:language>
</entry>
</feed>";
		const string _bananasPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""c40b4194867c7f8f4dbd5ff633653996.jpg"" /> 
<img src=""61fd7e1fd7a0b699c82eb4f089a455f7.png"" /> 
<img src=""95e805cc9f03ab235937124ab44755fa.png"" /> 
<p> <b> Trop de bananes </b> </p> 
<p> Auteur&#xa0;: Rohini Nilekani </p> 
<p> Illustrateurs&#xa0;: Angie et Upesh </p></body>
</html>";
		const string _bananasPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d1ef3e2832dfc9b6deff6f80fd23dbfc.jpg"" /> 
<p> Sringeri Srinivas passait une très mauvaise journée. </p> 
<p> Personne ne voulait des bananes mûres et sucrées qu’il cultivait dans sa ferme. </p></body>
</html>";
		const string _bananasPage12Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 12</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7e447cd435fc7fbd0e1eba3826e7f8b7.jpg"" /> 
<p> Pas avec les commerçants qui pouvaient vendre les bananes sur des marchés lointains. Et même pas avec ses vaches&#xa0;! </p></body>
</html>";
		const string _bananasPage20Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 20</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""74f123aa6e096acbb941dab70a3dca99.jpg"" /> 
<p> Au même moment, Sringeri Srinivas entra en portant un gros sac. </p></body>
</html>";
		const string _bananasPage24Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 24</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""9ba4b52e528a51c2d632545904e3a3e9.jpg"" /> 
<p> Le prêtre était tellement surpris qu’il en oublia de psalmodier. Dans le silence, un enfant commença à rire. </p></body>
</html>";
		const string _bananasPage27Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 27</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7493b48ae67b8b766a7826728a5c8cd5.jpg"" /> 
<p> <b> LE <b> SAVIEZ-VOUS&#xa0;? </b> </b></p> 
<b> <p> <b> Faits <b> à propos <b> des bananes </b></b></b></p><b><b><b> <p> </p> <p> Le mot banane est dérivé du mot arabe signifiant «&#xa0;doigt&#xa0;». L’Inde est le plus grand producteur de bananes au monde. Plus de 120&#xa0;variétés de bananes comestibles y sont cultivées. Le Centre national de la recherche sur les bananes à Trichy possède une collection de 1&#xa0;120&#xa0;variétés de bananes&#xa0;! </p> <p> </p> <p> Les bananes sont riches en minéraux qui aident à stimuler le cerveau. Les bananes rendent les étudiants plus actifs et alertes. De nombreuses friandises indiennes sont confectionnées à l’aide de bananes&#xa0;: le payasam de banane dans le Kerala, le rasayana de banane dans le Karnataka, le halva de banane Halwa, le Rawa Kela-Gur Mithai. En savez-vous davantage&#xa0;? </p> </b></b></b></b></body>
</html>";
		const string _bananasPage28Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 28</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d710444fa4fa11e970eed00fa1977069.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following
 <a>
  link
 </a>
 .
</p>
<p>
 Story Attribution:
</p>
This story:
Too Many Bananas
is written by
<a>
 Rohini Nilekani
</a>
.
            © Pratham Books
  , 2010. Some rights reserved. Released under CC BY 4.0 license.
<p>
 Other Credits:
</p>
This book has been published on StoryWeaver by Pratham Books. The author of this book, Rohini Nilekani used to earlier write under the pseudonym 'Noni'. The print version of 'Too Many Bananas' has been published by Pratham Books with the support by Nikki Gulati. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org
<p>
 Illustration Attributions:
</p>
Cover page:
<a>
 Man and bananas
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Page 2:
<a>
 Man carrying pile of bananas
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Disclaimer:
<a>
 https://www.storyweaver.org.in/terms_and_conditions
</a>
<img src=""a5c66ea0438e97ee66266fcc2890dcfd.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution,
 <a>
  http://creativecommons.org/licenses/by/4.0/
 </a>
</p>
</body>
</html>";
		const string _bananasPage29Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 29</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d710444fa4fa11e970eed00fa1977069.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following
 <a>
  link
 </a>
 .
</p>
<p>
 Illustration Attributions:
</p>
Page 3:
<a>
 Cows looking at man with bananas
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Page 4:
<a>
 People staring at man with bag
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Disclaimer:
<a>
 https://www.storyweaver.org.in/terms_and_conditions
</a>
<img src=""a5c66ea0438e97ee66266fcc2890dcfd.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution,
 <a>
  http://creativecommons.org/licenses/by/4.0/
 </a>
</p>
</body>
</html>";
		const string _bananasPage30Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 30</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d710444fa4fa11e970eed00fa1977069.png"" />
<p>
 This book was made possible by Pratham Books' StoryWeaver platform. Content under Creative Commons licenses can be downloaded, translated and can even be used to create new stories ­ provided you give appropriate credit, and indicate if changes were made. To know more about this, and the full terms of use and attribution, please visit the following
 <a>
  link
 </a>
 .
</p>
<p>
 Illustration Attributions:
</p>
Page 5:
<a>
 Surprised priest and laughing child
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Page 6:
<a>
 Man wheeling a pile of bananas
</a>
, by
<a>
 Angie &amp; Upesh
</a>
© Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.
Disclaimer:
<a>
 https://www.storyweaver.org.in/terms_and_conditions
</a>
<img src=""a5c66ea0438e97ee66266fcc2890dcfd.png"" />
<p>
 Some rights reserved. This book is CC­-BY­-4.0 licensed. You can copy, modify, distribute and perform the work, even for commercial purposes, all without asking permission. For full terms of use and attribution,
 <a>
  http://creativecommons.org/licenses/by/4.0/
 </a>
</p>
</body>
</html>";
		const string _bananasPage31Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 31</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Too Many Bananas
 (English)
</p>
<p>
 No one wanted to buy the sweet bananas that Sringeri Srinivas grew on his farm. Find out what he did with them in this cute story.
</p>
<p>
 This is a Level 2 book for children who recognize familiar words and can read new words with help.
</p>
<img src=""d710444fa4fa11e970eed00fa1977069.png"" />
Pratham Books goes digital to weave a whole new chapter in the realm of multilingual children's stories. Knitting together children, authors, illustrators and publishers. Folding in teachers, and translators. To create a rich fabric of openly licensed multilingual stories for the children of India ­­ and the world. Our unique online platform, StoryWeaver, is a playground where children, parents, teachers and librarians can get creative. Come, start weaving today, and help us get a book in every child's hand!
</body>
</html>";

		/// <summary>
		/// Checks the initial book setup to verify that the epub's opf file and the opds file were read
		/// and the book XHTML initialized properly.
		/// </summary>
		/// <returns>The book's data div from the initial setup.</returns>
		private XmlElement CheckInitialBookSetup(ConvertFromEpub convert, string title)
		{
			Assert.That(convert._epubMetaData.Title, Is.EqualTo(title));
			Assert.That(convert._opdsEntry, Is.Not.Null);
			Assert.That(convert._templatePages.Count, Is.GreaterThan(1));
			Assert.That(convert._bloomDoc, Is.Not.Null);
			var page0 = convert._bloomDoc.SelectSingleNode("/html/body/div[contains(@class,'bloom-page')]") as XmlElement;
			Assert.That(page0, Is.Null, "There should not be any pages in the empty initial book.");
			var dataDiv0 = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']") as XmlElement;
			Assert.That(dataDiv0, Is.Not.Null, "The data div should exist in the empty initial book.");
			var titleNode0 = convert._bloomDoc.SelectSingleNode("/html/head/title") as XmlElement;
			Assert.That(titleNode0, Is.Not.Null, "The title in the header should be set even in the empty book.");
			Assert.That(titleNode0.InnerText, Is.EqualTo("Book"));
			return dataDiv0;
		}

		/// <summary>
		/// Check the data imported from the front cover page.
		/// </summary>
		/// <returns>The cover image element from the front cover page.</returns>
		private XmlElement CheckCoverPageImport(ConvertFromEpub convert, XmlElement dataDiv0, string title, string imageSrc, string creditsInnerXml, out XmlElement coverImageData, string lang = "en")
		{
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages, Is.Not.Null, "A page should exist after converting a cover page.  (list not null)");
			Assert.That(pages.Count, Is.EqualTo(1), "A page should exist after converting a cover page. (list has one page)");
			var dataDiv = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']");
			Assert.That(dataDiv, Is.Not.Null, "The data div should still exist after converting the cover page.");
			Assert.That(dataDiv, Is.EqualTo(dataDiv0));
			Assert.That(pages[0], Is.Not.Null, "A page should exist after converting a cover page. (page not null)");
			Assert.That(pages[0].GetAttribute("class"), Does.Contain("outsideFrontCover"));
			Assert.That(pages[0].GetAttribute("data-xmatter-page"), Is.EqualTo("frontCover"));
			var titleNode = convert._bloomDoc.SelectSingleNode("/html/head/title") as XmlElement;
			Assert.That(titleNode, Is.Not.Null, "The title in the header should be set.");
			Assert.That(titleNode.InnerText, Is.EqualTo(title));
			var titleData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='{lang}']") as XmlElement;
			Assert.That(titleData, Is.Not.Null, "The bookTitle should be set in the data div.");
			Assert.That(titleData.InnerXml, Is.EqualTo($"<p>{title}</p>"));
			var titleDiv = convert._bloomDoc.SelectSingleNode($"//div[contains(@class, 'bloom-editable') and @data-book='bookTitle' and @lang='{lang}']") as XmlElement;
			Assert.That(titleDiv, Is.Not.Null, "The title should be set on the front cover page.");
			Assert.That(titleDiv.InnerXml, Is.EqualTo($"<p>{title}</p>"));
			coverImageData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage' and @lang='*']") as XmlElement;
			Assert.That(coverImageData, Is.Not.Null, "The cover image is set in the data div.");
			Assert.That(coverImageData.InnerXml, Is.EqualTo(imageSrc));
			Assert.That(coverImageData.GetAttribute("data-copyright"), Is.Empty, "Copyrights aren't set from the cover page.");
			Assert.That(coverImageData.GetAttribute("data-license"), Is.Empty, "Licenses aren't set from the cover page.");
			var coverImg = convert._bloomDoc.SelectSingleNode("//div[@class='bloom-imageContainer']/img[@data-book='coverImage']") as XmlElement;
			Assert.That(coverImg, Is.Not.Null, "The cover image should be set on the front cover page.");
			Assert.That(coverImg.GetAttribute("src"), Is.EqualTo(imageSrc));
			var creditsData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='smallCoverCredits' and @lang='{lang}']") as XmlElement;
			Assert.That(creditsData, Is.Not.Null, "Cover credits should be set in the data div.");
			Assert.That(creditsData.InnerXml, Is.EqualTo(creditsInnerXml));
			var creditsDiv = convert._bloomDoc.SelectSingleNode($"//div[contains(@class, 'bloom-editable') and @data-book='smallCoverCredits' and @lang='{lang}']");
			Assert.That(creditsDiv, Is.Not.Null, "The credits should be inserted into the front cover page.");
			Assert.That(creditsDiv.InnerXml, Is.EqualTo(creditsInnerXml));
			return coverImg;
		}

		/// <summary>
		/// Check the image src and text content imported for a true content page.
		/// Also verify the number of pages that we expect at this point.
		/// </summary>
		/// <returns>The img element for the page, to be checked later for copyright/license information.</returns>
		private XmlElement CheckTrueContentPageImport(XmlDocument bookDoc, string pageNumber, int pageCount, string imageSrc, string textInnerXml, string lang = "en")
		{
			var pages = bookDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(pageCount), $"{pageCount} pages should exist after converting page {pageNumber}");
			pages = bookDoc.SelectNodes("/html/body/div[contains(@class,'numberedPage')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(pageCount - 1), "A numbered page should exist after converting a content page.");
			Assert.That(pages[pageCount - 2].GetAttribute("class"), Does.Contain("bloom-page"));
			Assert.That(pages[pageCount - 2].GetAttribute("data-page-number"), Is.EqualTo(pageNumber), $"The numbered page has the right page number ({pageNumber}).");
			var imgList = pages[pageCount - 2].SelectNodes(".//div[contains(@class,'bloom-imageContainer')]/img").Cast<XmlElement>().ToList();
			Assert.That(imgList.Count, Is.EqualTo(1), $"Page {pageNumber} has one image (list has one item)");
			var pageImage = imgList[0];
			Assert.That(pageImage.GetAttribute("src"), Is.EqualTo(imageSrc));
			var textDivList = pages[pageCount - 2].SelectNodes($".//div[contains(@class,'bloom-translationGroup')]/div[contains(@class,'bloom-editable') and @lang='{lang}']").Cast<XmlElement>().ToList();
			if (String.IsNullOrEmpty(textInnerXml))
			{
				Assert.That(textDivList.Count, Is.EqualTo(0), $"Page {pageNumber} has no text block (list has zero items)");
			}
			else
			{
				Assert.That(textDivList.Count, Is.EqualTo(1), $"Page {pageNumber} has one text block (list has one item)");
				Assert.That(textDivList[0].InnerXml, Is.EqualTo(textInnerXml));
			}
			return pageImage;
		}

		/// <summary>
		/// Checks the book after any end pages have been imported.
		/// This assumes that all images have the same copyright information and that the book has a cover page and two content pages
		/// before any end pages.
		/// </summary>
		private static void CheckTwoPageBookAfterEndPages(ConvertFromEpub convert, XmlElement coverImg, XmlElement coverImageData, XmlElement firstPageImage, XmlElement secondPageImage,
			string imageCopyright, string imageLicense, string imageCreator, string bookCopyright, string bookLicense, string contribInnerXml, string[] insideCoverFragments)
		{
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and any end pages. (list has three pages)");
			CheckImageMetaData(coverImageData, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(coverImg, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(firstPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(secondPageImage, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyrightUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets copyrightUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo(bookLicense));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(contribInnerXml));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo(bookCopyright));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Not.Null, "End page sets the inside back cover in the data div");
			Assert.That(insideBackCoverData.InnerXml, Does.StartWith(insideCoverFragments[0]));
			Assert.That(insideBackCoverData.InnerXml.Trim(), Does.EndWith(insideCoverFragments[1]));
			for (int i = 2; i < insideCoverFragments.Length; ++i)
				Assert.That(insideBackCoverData.InnerXml, Does.Contain(insideCoverFragments[i]));
		}

		private static void CheckImageMetaData(XmlElement img, string imageCreator, string imageCopyright, string imageLicense)
		{
			Assert.That(img.GetAttribute("data-copyright"), Is.EqualTo(imageCopyright), "End page sets image copyright");
			Assert.That(img.GetAttribute("data-license"), Is.EqualTo(imageLicense), "End page sets image license");
			Assert.That(img.GetAttribute("data-creator"), Is.EqualTo(imageCreator), "End page sets image creator");
		}

		private static void CheckExtraCoverImages(XmlDocument bookDoc, string image2Src, string image3Src)
		{
			var coverImage2Data = bookDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage2' and @lang='*']") as XmlElement;
			Assert.That(coverImage2Data, Is.Not.Null, "The second cover image is set in the data div.");
			Assert.That(coverImage2Data.InnerXml, Is.EqualTo(image2Src));
			var coverImage3Data = bookDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage3' and @lang='*']") as XmlElement;
			Assert.That(coverImage3Data, Is.Not.Null, "The third cover image is set in the data div.");
			Assert.That(coverImage3Data.InnerXml, Is.EqualTo(image3Src));
		}
	}
}
