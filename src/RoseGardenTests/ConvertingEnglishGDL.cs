using System;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class ConvertingEnglishGDL : ConversionTestBase
	{
		public ConvertingEnglishGDL()
		{
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
			var result = convert.ConvertContentPage(1, _goatPage2Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting Goat chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "27e900b0dc523b77e981b601a779c6a0.jpg",
				"<p>Once upon a time,  there was a goat called Igodhoobe. Igodhoobe the goat was the king of farm animals and birds. He lived a good life. One day,  Igodhoobe the goat called all the animals and birds to a meeting.</p>");

			// SUT
			result = convert.ConvertContentPage(18, _goatPage19Xhtml, "19.xhtml");
			Assert.That(result, Is.True, "converting Goat chapter 19 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "18", 3, "fd3e3272e2bf6aefc67c827f529b3891.jpg",
				"<p>From that time,  every goat refuses to move when it is pulled. It thinks that you are taking it to the king's court.</p>");

			// SUT
			result = convert.ConvertContentPage(19, _goatPage20Xhtml, "20.xhtml");
			Assert.That(result, Is.True, "converting Goat chapter 20 (end page) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © African Storybook Initiative 2015", "CC BY 4.0", "Marleen Visser",
				"Copyright © Uganda Community Libraries Association (Ugcla) 2015", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>Written by Alice Nakasango.</p>
<p>Images by Marleen Visser. © African Storybook Initiative 2015. CC BY 4.0.</p>", null);

			// SUT
			convert.ReplaceCoverImageIfNeeded();
			// Check the changed image filename.
			Assert.That(coverImageData.InnerXml, Is.EqualTo("27e900b0dc523b77e981b601a779c6a0.jpg"));
			CheckImageMetaData(coverImageData, "Marleen Visser", "Copyright © African Storybook Initiative 2015", "CC BY 4.0");
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
			var result = convert.ConvertContentPage(1, _dogsPage2Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting Dogs chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "472ec5f74412d1021a8fba06e832364d.jpg", @"<p>A dog and a cat were best friends.</p>
<p>Everyone knew that if they saw the cat, the dog wouldn't be far behind.</p>");

			// SUT
			result = convert.ConvertContentPage(10, _dogsPage11Xhtml, "10.xhtml");
			Assert.That(result, Is.True, "converting Dogs chapter 11 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "10", 3, "1ef2966e90505f7cebbff05e35856362.jpg", @"<p>Since that day, cats and dogs stopped getting along... most of the time.</p>");

			// SUT
			result = convert.ConvertContentPage(11, _dogsPage12Xhtml, "11.xhtml");
			Assert.That(result, Is.True, "converting Dogs chapter 12 (end page 1/3) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(12, _dogsPage13Xhtml, "12.xhtml");
			Assert.That(result, Is.True, "converting Dogs chapter 13 (end page 2/3) succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and two end pages.");

			// SUT
			result = convert.ConvertContentPage(13, _dogsPage14Xhtml, "13.xhtml");
			Assert.That(result, Is.True, "converting Dogs chapter 14 (end page 3/3) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © Room to Read, 2013", "CC BY 4.0", "Vusi Malindi",
				"Copyright © Room to Read, 2013", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>Written by Alisha Berger.</p>
<p>Images by Vusi Malindi. © Room to Read, 2013. CC BY 4.0.</p>", "<p>This story 'Dogs versus Cats' has been published on StoryWeaver by Room to Read.</p>");

			// SUT
			convert.ReplaceCoverImageIfNeeded();
			// Check the unchanged image filename.
			Assert.That(coverImageData.InnerXml, Is.EqualTo("ac99ee2a331aa285d8c828cdb2ee0b29.jpg"));
			CheckImageMetaData(coverImageData, "Vusi Malindi", "Copyright © Room to Read, 2013", "CC BY 4.0");
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
			var result = convert.ConvertContentPage(1, _whatIfPage2Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting What If? chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "7afe226493701626fcda5e9d38deb172.jpg", @"<p>My name is Shyam, I am ten years old.</p>
<p>I am a little skinny, but very bold!</p>
<p>Waking up for school is no fun at all</p>
<p>I am so sleepy as I get up, I think I will fall.</p>");

			// SUT
			result = convert.ConvertContentPage(10, _whatIfPage11Xhtml, "11.xhtml");
			Assert.That(result, Is.True, "converting What If? chapter 11 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "10", 3, "6acdb659741aed3435e3f9b1c36bc64a.jpg", @"<p>I find myself still standing with my brush in my hand,</p>
<p>And I smile and think of my secret little land.</p>");

			// SUT
			result = convert.ConvertContentPage(11, _whatIfPage12Xhtml, "12.xhtml");
			Assert.That(result, Is.True, "converting What If? chapter 12 (end page 1/2) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(12, _whatIfPage13Xhtml, "13.xhtml");
			Assert.That(result, Is.True, "converting What If? chapter 13 (end page 2/2) succeeded");
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
				"Copyright © Pratham Books, 2015", "CC BY 4.0", "Hari Kumar Nair",
				"Copyright © Pratham Books, 2015", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>Written by Hari Kumar Nair.</p>
<p>Images by Hari Kumar Nair. © Pratham Books, 2015. CC BY 4.0.</p>",
				"<p>The development of this book has been supported by HDFC Asset Management Company Limited (A joint venture with Standard Life Investments).This book was part of the Pratham Books lab conducted in collaboration with Srishti School of Art, Design and Technology, Bangalore.</p>");
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
			var result = convert.ConvertContentPage(1, _birthdayPage2Xhtml, "1.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 2 succeeded");
			var page1Img = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "296dc94e65540e945f21665a7c44a801.jpg", null);

			// SUT
			result = convert.ConvertContentPage(2, _birthdayPage3Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 3 succeeded");
			var page2Img = CheckTrueContentPageImport(convert._bloomDoc, "2", 3, "7935930bd3ead65ff8abbb51c14fd303.jpg", null);

			// SUT
			result = convert.ConvertContentPage(3, _birthdayPage4Xhtml, "3.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 4 succeeded");
			var page3Img = CheckTrueContentPageImport(convert._bloomDoc, "3", 4, "2aa8778355a0f25384d96e8421402c6a.jpg", null);

			// SUT
			result = convert.ConvertContentPage(4, _birthdayPage5Xhtml, "4.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 5 succeeded");
			var page4Img = CheckTrueContentPageImport(convert._bloomDoc, "4", 5, "df22465a96ac6839a15a9de54dd5d417.jpg", null);

			// SUT
			result = convert.ConvertContentPage(5, _birthdayPage6Xhtml, "5.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 6 succeeded");
			var page5Img = CheckTrueContentPageImport(convert._bloomDoc, "5", 6, "c81aac9617591f5591d3bf8e500e8c89.jpg", null);

			// SUT
			result = convert.ConvertContentPage(6, _birthdayPage7Xhtml, "6.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 7 succeeded");
			var page6Img = CheckTrueContentPageImport(convert._bloomDoc, "6", 7, "81fc38f93c2b8c8d53bddd7ec9eb8ec6.jpg", null);

			// SUT
			result = convert.ConvertContentPage(7, _birthdayPage8Xhtml, "7.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 8 succeeded");
			var page7Img = CheckTrueContentPageImport(convert._bloomDoc, "7", 8, "e723496d42cb09497980634d6ba9d9a7.jpg", @"<p><b>Wondering what to do with wordless stories?</b></p>
<p>Wordless stories are wonderful because they contain infinite possibilities. Here are a few ideas for engaging with children using visual stories:</p>
<p>- Explore the story in a leisurely manner. Draw attention to the details - the expressions of the characters, setting, colours, etc. The idea is for each child to build her own story. If the story is being shown to a group of children, you could ask each of them to contribute a sentence or two for each illustration. Take joy in exploring each illustration and build the story as you go along.</p>
<p>- Use themes explored in the story to start a discussion. For instance, in this story, you could ask children about what they do for their birthday, or even how they help out at home.</p>
<p>- Encourage children to create 2-3 different stories using the same set of visuals. This will help push their imagination.</p>");

			// SUT
			result = convert.ConvertContentPage(8, _birthdayPage9Xhtml, "8.xhtml");
			Assert.That(result, Is.True, "converting The Birthday Party chapter 9 (end page 1/2) succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(8), "Eight pages should exist after converting the cover page, seven content pages, and one end page.");

			// SUT
			result = convert.ConvertContentPage(9, _birthdayPage10Xhtml, "9.xhtml");
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
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='licenseUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets licenseUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Written by Storyweaver, Pratham Books.</p>
<p>Images on Front Cover, page 3 by Megha Vishwanath. © Pratham Books, 2015. CC BY 4.0.</p>
<p>Images on pages 1-2, 4-7 by Megha Vishwanath. © Megha Vishwanath, 2015. CC BY 4.0.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © Pratham Books, 2015"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Null, "The inside back cover in the data div should not be set.");
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
		/// This tests converting the Global Digital Library version of "The Great Hairy Khyaa" published by The Asia Foundation.
		/// It has these distinctive features:
		/// * the front cover page has a mixture of paragraph and raw text for the author and illustrator names
		/// * content pages use paragraph markup for the text
		/// * page 2 is really another credit page
		/// * 2 end pages, the first talking about the author and illustrator (and no copyright or license information), and the
		///   second a messy copyright/license page.
		/// </summary>
		[Test]
		public void TestConvertingGreatHairyKhyaa_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _hairyKhyaaOpfXml, _hairyKhyaaOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "The Great Hairy Khyaa");

			// SUT
			convert.ConvertCoverPage(_hairyKhyaaPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "The Great Hairy Khyaa", "36c0b3f194cfaee044a627a9ad4d5fc0.jpg",
				@"<p>Durga Lal Shrestha</p><p>Suman Maharjan</p>", out XmlElement coverImageData);

			// SUT
			var result = convert.ConvertContentPage(1, _hairyKhyaaPage2Xhtml, "1.xhtml");
			Assert.That(result, Is.True, "converting The Great Hairy Khyaa chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "a99b2b2d4284bde63d16c7b4abffdf41.jpg",
				@"<p>Srijanalaya produced this book with the support of The Asia Foundation’s Books for Asia program. Srijanalaya is an NGO based in Nepal that creates safe spaces of learning through the arts. For more information, visit: srijanalaya.org. Title: ‘Khyaa’ (2018), originally sung in Chulichiya Chan Chan (1991) Writer: Durga Lal Shrestha Illustrator: Suman Maharjan Editors: Muna Gurung, Sharareh Bajracharya and Niranjan Kunwar</p>");

			// SUT
			result = convert.ConvertContentPage(2, _hairyKhyaaPage3Xhtml, "3.xhtml");
			Assert.That(result, Is.True, "converting The Great Hairy Khyaa chapter 3 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "2", 3, "e664fd3ac4e1600bcb4c0743a7552b7c.jpg", @"<p>Who’s down there? The Great Hairy Khyaa !</p>");

			// SUT
			result = convert.ConvertContentPage(12, _hairyKhyaaPage13Xhtml, "13.xhtml");
			Assert.That(result, Is.True, "converting The Great Hairy Khyaa chapter 13 succeeded");
			var thirdPageImage = CheckTrueContentPageImport(convert._bloomDoc, "12", 4, "50d7c9ca4e58137eb2e37a403666dc8d.jpg", @"<p>Ma is this the khyaa that scares me so?</p>");

			// SUT
			result = convert.ConvertContentPage(13, _hairyKhyaaPage14Xhtml, "14.xhtml");
			Assert.That(result, Is.True, "converting The Great Hairy Khyaa chapter 14 succeeded");
			var fourthPageImage = CheckTrueContentPageImport(convert._bloomDoc, "13", 5, "1f8866c433bbc82ef57f54cc0233009d.jpg",
				@"<p>About the Author Durga Lal Shrestha is a famous poet of Nepal Bhasa and Nepali. As a teacher of Nepal Bhasa at Kanya Mandir Higher Secondary School in the 1950s to 70s, he created songs to inspire children to express themselves in their mother tongue. His songs became widely known throughout the Kathmandu Valley and collections of his children’s songs have been through over reprints and are still circulated today. About the Illustrator Suman Maharjan is a passionate visual artist, animator, and freelance illustrator from Nepal. He has loved illustrating and children’s picture books since a young age, with a passion for 2D character animation. He enjoys working in different medium - in addition to DIY solutions, printmaking and sculpture. Acknowledgments First and foremost we would like to thank the openness with which Durga Lal Shrestha has embraced this project. He is an inspiration for the next generation of creative thinkers. A warm thank you to the illustrator Amber Delahaye from Stichting Thang who held illustration workshops. And finally, this book would not have been possible without Suman and Suchita Shrestha, who are Durga Lal Shrestha’s children, and his wife, Purnadevi Shrestha, who is always by his side.</p>");

			// SUT
			result = convert.ConvertContentPage(14, _hairyKhyaaPage15Xhtml, "15.xhtml");
			Assert.That(result, Is.True, "converting The Great Hairy Khyaa chapter 15 (end page) succeeded");
			// We can't use the normal checking method because it assumes only 2 content pages and we have 4.
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(5), "Five pages should exist after converting the cover page, four content pages, and an end page.");
			var imageCreator = "Suman Maharjan";
			var imageCopyright = "Copyright © The Asia Foundation, 2019";
			var imageLicense = "CC BY-NC 4.0";
			CheckImageMetaData(coverImageData, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(coverImg, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(firstPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(secondPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(thirdPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(fourthPageImage, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='licenseUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets licenseUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by-nc/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Written by Durga Lal Shrestha.</p>
<p>Images by Suman Maharjan. © The Asia Foundation, 2019. CC BY-NC 4.0.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © The Asia Foundation, 2019"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Null, "The inside back cover in the data div should not be set.");
		}
		const string _hairyKhyaaOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">1c13c21b-8a29-41f0-9633-ed3653aa2eaf</dc:identifier>
		<dc:title>The Great Hairy Khyaa</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:52:06Z</meta>
		<dc:description>What lurks under the stairs yet disappears when you turn on the lights? A group of friends encounter the Great Hairy Khyaa. Will they overcome their fear of the darkness?</dc:description>
		<dc:creator id=""contributor_1"">Durga Lal Shrestha</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Suman Maharjan</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""36c0b3f194cfaee044a627a9ad4d5fc0.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""2afebc0bb8df793d350593e7e1859da5.jpg"" id=""image-50946-1"" media-type=""image/jpeg"" />
		<item href=""a99b2b2d4284bde63d16c7b4abffdf41.jpg"" id=""image-50947-2"" media-type=""image/jpeg"" />
		<item href=""e664fd3ac4e1600bcb4c0743a7552b7c.jpg"" id=""image-50948-3"" media-type=""image/jpeg"" />
		<item href=""b1a965d6bbad678cac98c5c815d7e784.jpg"" id=""image-50949-4"" media-type=""image/jpeg"" />
		<item href=""0e001757a769d2711c4152fe9dac8853.jpg"" id=""image-50950-5"" media-type=""image/jpeg"" />
		<item href=""5ad37ac1a7e04ae838a3e769f2129df7.jpg"" id=""image-50951-6"" media-type=""image/jpeg"" />
		<item href=""7555ebd64d50d6c5689d987ef529dfbc.jpg"" id=""image-50952-7"" media-type=""image/jpeg"" />
		<item href=""7210f46dc24d1c9c32e073bf6dd2b6b3.jpg"" id=""image-50953-8"" media-type=""image/jpeg"" />
		<item href=""2ede96dd77c6b1c0e1af9b255bf1affe.jpg"" id=""image-50954-9"" media-type=""image/jpeg"" />
		<item href=""573dfdaaa3149c14020cce24651b9090.jpg"" id=""image-50955-10"" media-type=""image/jpeg"" />
		<item href=""6ff0c357d78357b470270f45839123e6.jpg"" id=""image-50956-11"" media-type=""image/jpeg"" />
		<item href=""f3ea5a34517e7db78015e6ae684221fc.jpg"" id=""image-50957-12"" media-type=""image/jpeg"" />
		<item href=""50d7c9ca4e58137eb2e37a403666dc8d.jpg"" id=""image-50958-13"" media-type=""image/jpeg"" />
		<item href=""1f8866c433bbc82ef57f54cc0233009d.jpg"" id=""image-50959-14"" media-type=""image/jpeg"" />
		<item href=""2d85083a3544781ab3cab25d5c38b443.png"" id=""image-50960-15"" media-type=""image/png"" />
		<item href=""13652b55fe3beeb1954c87d0f4c9bbc4.png"" id=""image-50961-16"" media-type=""image/png"" />
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
	</manifest>
</package>";
		const string _hairyKhyaaOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:1c13c21b-8a29-41f0-9633-ed3653aa2eaf</id>
<title>The Great Hairy Khyaa</title>
<author>
<name>Durga Lal Shrestha</name>
</author>
<contributor type=""Illustrator"">
<name>Suman Maharjan</name>
</contributor>
<dc:license>Creative Commons Attribution Non Commercial 4.0 International</dc:license>
<dc:publisher>The Asia Foundation</dc:publisher>
<updated>2019-12-03T00:00:00Z</updated>
<dc:created>2019-12-03T00:00:00Z</dc:created>
<published>2019-12-03T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 2"" />
<summary>What lurks under the stairs yet disappears when you turn on the lights? A group of friends encounter the Great Hairy Khyaa. Will they overcome their fear of the darkness?</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/36c0b3f194cfaee044a627a9ad4d5fc0"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/36c0b3f194cfaee044a627a9ad4d5fc0?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/1c13c21b-8a29-41f0-9633-ed3653aa2eaf.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/1c13c21b-8a29-41f0-9633-ed3653aa2eaf.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _hairyKhyaaPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""36c0b3f194cfaee044a627a9ad4d5fc0.jpg"" />
<h1>
 The Great Hairy Khyaa
</h1>
<h2>
 Durga Lal Shrestha
</h2>
Suman Maharjan
<img src=""2afebc0bb8df793d350593e7e1859da5.jpg"" />
</body>
</html>";
		const string _hairyKhyaaPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""a99b2b2d4284bde63d16c7b4abffdf41.jpg"" />
<p>
 Srijanalaya produced this book with the support of The Asia Foundation’s Books for Asia program. Srijanalaya is an NGO based in Nepal that creates safe spaces of learning through the arts. For more information, visit: srijanalaya.org. Title: ‘Khyaa’ (2018), originally sung in Chulichiya Chan Chan (1991) Writer: Durga Lal Shrestha Illustrator: Suman Maharjan Editors: Muna Gurung, Sharareh Bajracharya and Niranjan Kunwar
</p></body>
</html>";
		const string _hairyKhyaaPage3Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 3</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""e664fd3ac4e1600bcb4c0743a7552b7c.jpg"" />
<p>
 Who’s down there? The Great Hairy Khyaa !
</p></body>
</html>";
		const string _hairyKhyaaPage13Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 13</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""50d7c9ca4e58137eb2e37a403666dc8d.jpg"" />
<p>
 Ma is this the khyaa that scares me so?
</p></body>
</html>";
		const string _hairyKhyaaPage14Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 14</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""1f8866c433bbc82ef57f54cc0233009d.jpg"" />
<p>
 About the Author Durga Lal Shrestha is a famous poet of Nepal Bhasa and Nepali. As a teacher of Nepal Bhasa at Kanya Mandir Higher Secondary School in the 1950s to 70s, he created songs to inspire children to express themselves in their mother tongue. His songs became widely known throughout the Kathmandu Valley and collections of his children’s songs have been through over reprints and are still circulated today. About the Illustrator Suman Maharjan is a passionate visual artist, animator, and freelance illustrator from Nepal. He has loved illustrating and children’s picture books since a young age, with a passion for 2D character animation. He enjoys working in different medium - in addition to DIY solutions, printmaking and sculpture. Acknowledgments First and foremost we would like to thank the openness with which Durga Lal Shrestha has embraced this project. He is an inspiration for the next generation of creative thinkers. A warm thank you to the illustrator Amber Delahaye from Stichting Thang who held illustration workshops. And finally, this book would not have been possible without Suman and Suchita Shrestha, who are Durga Lal Shrestha’s children, and his wife, Purnadevi Shrestha, who is always by his side.
</p></body>
</html>";
		const string _hairyKhyaaPage15Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 15</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Brought to you by
</p>
<p>
 <img src=""2d85083a3544781ab3cab25d5c38b443.png"" />
</p>
<p>
 Let's Read! is an initiative of The Asia Foundation's Books for Asia program that fosters young readers in Asia. booksforasia.org To read more books like this and get further information, visit letsreadasia.org .
</p>
<p>
 Original Story ख्याक्, author: Durga Lal Shrestha. . illustrator: Suman Maharjan. Published by , https://www.letsreadasia.org © . Released under CC BY-NC 4.0.
</p>
<p>
 This work is a modified version of the original story. © The Asia Foundation, 2019. Some rights reserved. Released under CC BY-NC 4.0.
</p>
<p>
 <img src=""13652b55fe3beeb1954c87d0f4c9bbc4.png"" />
 For full terms of use and attribution, http://creativecommons.org/licenses/by-nc/4.0/
</p>
<p>
 #_contributions_#
</p></body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "The Elephant in My House" published by The Asia Foundation.
		/// It has these distinctive features:
		/// * the front cover page gives the author's name in a non-Roman script (I assume it's the author's name...)
		/// * content pages use paragraph markup for the text
		/// * 1 content page with only a picture without any text
		/// * 3 end pages, the first with a blurb about saving the elephants, the second thanking a donor (neither with any
		///   copyright or license information), and the final page a messy copyright/license page.
		/// </summary>
		[Test]
		public void TestConvertingElephantInMyHouse_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _elephantOpfXml, _elephantOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "The Elephant in My House");

			// SUT
			convert.ConvertCoverPage(_elephantPage1Xhtml);
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "The Elephant in My House", "cc1c496186e7bd12151a5c39b522058d.jpg",
				@"<p>ព្រុំ គន្ធារ៉ូ</p>"
				, out XmlElement coverImageData);

			// SUT
			var result = convert.ConvertContentPage(1, _elephantPage2Xhtml, "1.xhtml");
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 2 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "858af36e7f07543929931d2002d7fd2c.jpg",
				@"<p>One morning, Botom and her mother were tending their fields. Suddenly, they saw a young elephant running towards them!</p>");

			// SUT
			result = convert.ConvertContentPage(16, _elephantPage17Xhtml, "17.xhtml");
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 17 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "16", 3, "bb4e18bba720607c88276b8aff191169.jpg",
				@"<p>Sakor was sad to leave too, now that Botom treated him kindly. But his mother reminded him he could come back to visit now that Botom understood how to be a friend to elephants.</p>");

			// SUT
			result = convert.ConvertContentPage(17, _elephantPage18Xhtml, "18.xhtml");
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 18 succeeded");
			var thirdPageImage = CheckTrueContentPageImport(convert._bloomDoc, "17", 4, "33b5b1c2c03c6bbfcc438c7c0d16de0b.jpg", null);

			// SUT
			result = convert.ConvertContentPage(18, _elephantPage19Xhtml, "19.xhtml");
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 19 succeeded");
			var fourthPageImage = CheckTrueContentPageImport(convert._bloomDoc, "18", 5, "7bd541afd7e292faa70d1492a4d45f45.jpg",
				@"<p>More About the Environment Conservation International (CI) has been working in Cambodia since 2001 to conserve the rich biodiversity of Cambodia. From the Cardamom Mountains in the southwest, home of some of the few remaining Asian elephants in the country, to Tonle Sap Lake, the largest inland fishery in Southeast Asia, to Veun Sai Siem Park National Park, home of the yellow-cheeked gibbons. For more information: https://www.conservation.org/where/Pages/Greater-Mekong-region.aspx https://www.youtube.com/watch?v=XGlTHR8aD-o https://www.youtube.com/watch?v=xgqsniNBhgs Additional environmental information provided by Conservation International in collaboration with The Asia Foundation</p>");

			// SUT
			result = convert.ConvertContentPage(19, _elephantPage20Xhtml, "20.xhtml");
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 20 succeeded");
			var fifthPageImage = CheckTrueContentPageImport(convert._bloomDoc, "19", 6, "7b6cd336c62f1c6b2098f9b65811403a.jpg",
				@"<p>Generously supported by SMART</p>");

			// SUT
			result = convert.ConvertContentPage(20, _elephantPage21Xhtml, "21.xhtml");  // page number must match what is in opf file below.
			Assert.That(result, Is.True, "converting The Elephant in My House chapter 21 (end page) succeeded");
			// We can't use the normal checking method because it assumes only 2 content pages and we have 4.
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(6), "Six pages should exist after converting the cover page, five content pages, and one end page.");
			var imageCreator = "Sin Thuokna";
			var imageCopyright = "Copyright © The Asia Foundation, 2019";
			var imageLicense = "CC BY-NC 4.0";
			CheckImageMetaData(coverImageData, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(coverImg, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(firstPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(secondPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(thirdPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(fourthPageImage, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='licenseUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets licenseUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by-nc/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			// Check that the English author name gets put into the xmatter credits data, not just the illustrator's name.
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Written by Prum Kunthearo.</p>
<p>Images by Sin Thuokna. © The Asia Foundation, 2019. CC BY-NC 4.0.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © The Asia Foundation, 2019"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Null, "The inside back cover in the data div should not be set.");
		}
		const string _elephantOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">34b4d1e4-cee5-4ad3-b747-be5e6358df85</dc:identifier>
		<dc:title>The Elephant in My House</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:52:08Z</meta>
		<dc:description>When a baby elephant runs into their house, Botom’s parents care for it but she becomes jealous. Can Botom get rid of the elephant or will she become friends with the lovable creature as well?</dc:description>
		<dc:creator id=""contributor_1"">Prum Kunthearo</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Sin Thuokna</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""cc1c496186e7bd12151a5c39b522058d.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""858af36e7f07543929931d2002d7fd2c.jpg"" id=""image-50858-1"" media-type=""image/jpeg"" />
		<item href=""e1024cc63b7235c73364e53c8d08fb99.jpg"" id=""image-50859-2"" media-type=""image/jpeg"" />
		<item href=""3b3810a3aed98fa51b4375515e429c9a.jpg"" id=""image-50860-3"" media-type=""image/jpeg"" />
		<item href=""a9ccb615210a86ac952d50bb02831aab.jpg"" id=""image-50861-4"" media-type=""image/jpeg"" />
		<item href=""7de9b97f286d9e9cd5708828189971ae.jpg"" id=""image-50862-5"" media-type=""image/jpeg"" />
		<item href=""bb4e18bba720607c88276b8aff191169.jpg"" id=""image-50873-16"" media-type=""image/jpeg"" />
		<item href=""33b5b1c2c03c6bbfcc438c7c0d16de0b.jpg"" id=""image-50874-17"" media-type=""image/jpeg"" />
		<item href=""7bd541afd7e292faa70d1492a4d45f45.jpg"" id=""image-50875-18"" media-type=""image/jpeg"" />
		<item href=""7b6cd336c62f1c6b2098f9b65811403a.jpg"" id=""image-50876-19"" media-type=""image/jpeg"" />
		<item href=""2d85083a3544781ab3cab25d5c38b443.png"" id=""image-50877-20"" media-type=""image/png"" />
		<item href=""13652b55fe3beeb1954c87d0f4c9bbc4.png"" id=""image-50878-21"" media-type=""image/png"" />
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
		<item href=""chapter-21.xhtml"" id=""chapter-21"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _elephantOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:34b4d1e4-cee5-4ad3-b747-be5e6358df85</id>
<title>The Elephant in My House</title>
<author>
<name>Prum Kunthearo</name>
</author>
<contributor type=""Illustrator"">
<name>Sin Thuokna</name>
</contributor>
<dc:license>Creative Commons Attribution Non Commercial 4.0 International</dc:license>
<dc:publisher>The Asia Foundation</dc:publisher>
<updated>2019-12-02T00:00:00Z</updated>
<dc:created>2019-12-02T00:00:00Z</dc:created>
<published>2019-12-02T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 4"" />
<summary>When a baby elephant runs into their house, Botom’s parents care for it but she becomes jealous. Can Botom get rid of the elephant or will she become friends with the lovable creature as well?</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/cc1c496186e7bd12151a5c39b522058d"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/cc1c496186e7bd12151a5c39b522058d?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/34b4d1e4-cee5-4ad3-b747-be5e6358df85.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/34b4d1e4-cee5-4ad3-b747-be5e6358df85.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _elephantPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""cc1c496186e7bd12151a5c39b522058d.jpg"" />
<h1>
 The Elephant in My House
</h1>
<h2>
 ព្រុំ គន្ធារ៉ូ
</h2></body>
</html>";
		const string _elephantPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""858af36e7f07543929931d2002d7fd2c.jpg"" />
<p>
 One morning, Botom and her mother were tending their fields. Suddenly, they saw a young elephant running towards them!
</p></body>
</html>";
		const string _elephantPage17Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 17</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""bb4e18bba720607c88276b8aff191169.jpg"" />
<p>
 Sakor was sad to leave too, now that Botom treated him kindly. But his mother reminded him he could come back to visit now that Botom understood how to be a friend to elephants.
</p></body>
</html>";
		const string _elephantPage18Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 18</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""33b5b1c2c03c6bbfcc438c7c0d16de0b.jpg"" />
<p>
</p></body>
</html>";
		const string _elephantPage19Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 19</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7bd541afd7e292faa70d1492a4d45f45.jpg"" />
<p>
 More About the Environment Conservation International (CI) has been working in Cambodia since 2001 to conserve the rich biodiversity of Cambodia. From the Cardamom Mountains in the southwest, home of some of the few remaining Asian elephants in the country, to Tonle Sap Lake, the largest inland fishery in Southeast Asia, to Veun Sai Siem Park National Park, home of the yellow-cheeked gibbons. For more information: https://www.conservation.org/where/Pages/Greater-Mekong-region.aspx https://www.youtube.com/watch?v=XGlTHR8aD-o https://www.youtube.com/watch?v=xgqsniNBhgs Additional environmental information provided by Conservation International in collaboration with The Asia Foundation
</p></body>
</html>";
		const string _elephantPage20Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 20</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""7b6cd336c62f1c6b2098f9b65811403a.jpg"" />
<p>
 Generously supported by SMART
</p></body>
</html>";
		const string _elephantPage21Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 21</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 Brought to you by
</p>
<p>
 <img src=""2d85083a3544781ab3cab25d5c38b443.png"" />
</p>
<p>
 Let's Read! is an initiative of The Asia Foundation's Books for Asia program that fosters young readers in Asia. booksforasia.org To read more books like this and get further information, visit letsreadasia.org .
</p>
<p>
 Original Story បុទម និងសាគរ, author: ព្រុំ គន្ធារ៉ូ . . illustrator: ស៊ិន ធួកណា. Published by The Asia Foundation, https://www.letsreadasia.org © The Asia Foundation. Released under CC BY-NC 4.0.
</p>
<p>
 This work is a modified version of the original story. © The Asia Foundation, 2019. Some rights reserved. Released under CC BY-NC 4.0.
</p>
<p>
 <img src=""13652b55fe3beeb1954c87d0f4c9bbc4.png"" />
 For full terms of use and attribution, http://creativecommons.org/licenses/by-nc/4.0/
</p>
<p>
 Contributing translators: Kyle Barker
</p></body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "Mini Num" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * content pages use paragraph markup for the text
		/// * chapter 2 (page 1) is an acknowledgements page, not a content page.
		/// * there are no end credit pages: the copyright is implied and the license given only by an image page 1 (chapter 2)
		/// </summary>
		[Test]
		public void TestConvertingMiniNum_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _miniNumOpfXml, _miniNumOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "Mini Num");

			// SUT
			convert.ConvertPage(0, _miniNumPage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Mini Num", "069dd655ca62b0c5f476446ce84d8b72.jpg", @"<p>
 Author: Al-Sayed Ibrahim
</p><p>
 Illustrator: Mostafa Al-Barshoom
</p>", out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "c264a8fa3bce4416fdd903cfdcef27cc.png", null);

			// SUT
			var result = convert.ConvertPage(1, _miniNumPage2Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting Mini Num chapter 2 succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(1), "Only one page (the cover page) exists after converting the cover page and the second (acknowledgements) page.");

			// SUT
			result = convert.ConvertPage(2, _miniNumPage3Xhtml, "3.xhtml");
			Assert.That(result, Is.True, "converting Mini Num chapter 3 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "11eee87111091e1fe005745714df306f.jpg",
				@"<p>Amid the noise in the forest, Mini Num the bear hatched in the depths of the quiet river. He drifted far away in the current and he found himself quite alone.</p>");

			// SUT
			result = convert.ConvertPage(3, _miniNumPage4Xhtml, "4.xhtml");
			Assert.That(result, Is.True, "converting Mini Num chapter 4 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "2", 3, "2e9d898f5ae82b2a731206fee76a5354.jpg",
				@"<p>None of the animals ever knew he was there, so small he was like a particle in the air. How would they ever see him if he’s that small? They probably won’t notice him at all.</p>");

			// SUT
			result = convert.ConvertPage(17, _miniNumPage18Xhtml, "18.xhtml");
			Assert.That(result, Is.True, "converting Mini Num chapter 18 succeeded");
			var thirdPageImage = CheckTrueContentPageImport(convert._bloomDoc, "16", 4, "1af6d646ab3a0d97ec8ff797825a6f18.jpg",
				@"<p>Tiny laughed as he swam and said, “We can’t get burned! Did you know I survived an atomic bomb in World War II? Mini Num hadn’t known that but now he was convinced. So he jumped in, shouting, “We ARE amazing, no matter how small we are!”</p>");

			// SUT
			result = convert.ConvertPage(18, _miniNumPage19Xhtml, "19.xhtml");
			Assert.That(result, Is.True, "converting Mini Num chapter 19 succeeded");
			var fourthPageImage = CheckTrueContentPageImport(convert._bloomDoc, "17", 5, "0599389b1be03ae06bc492ed7af54c8f.jpg",
				@"<p>The End</p>");

			// SUT
			convert.SetAsafeerImageCredits();
			// We can't use the normal checking method because it assumes only 2 content pages and we have 4.
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(5), "Five pages should exist after converting the cover page, one acknowledgments page, and four content pages.");
			var imageCreator = "Mostapha Al-Barshomi";
			var imageCopyright = "Copyright © Asafeer Education Technologies FZ LLC, 2018";
			var imageLicense = "CC BY-NC-SA 4.0";
			CheckImageMetaData(coverImageData, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(coverImg, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(firstPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(secondPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(thirdPageImage, imageCreator, imageCopyright, imageLicense);
			CheckImageMetaData(fourthPageImage, imageCreator, imageCopyright, imageLicense);
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='licenseUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets licenseUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("https://creativecommons.org/licenses/by-nc-sa/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Written by El-Sayyed Ibraheem.</p>
<p>Images by Mostapha Al-Barshomi. © Asafeer Education Technologies FZ LLC, 2018. CC BY-NC-SA 4.0.</p>"));
			var originalAckData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalAcknowledgments' and @lang='en']") as XmlElement;
			Assert.That(originalAckData, Is.Not.Null, "Acknowledgements page sets originalAcknowledgments in data div");
			Assert.That(originalAckData.InnerXml, Is.EqualTo("<p>The original work of this book was made possible through the generous support of the All Children Reading: A Grand Challenge for Development (ACR GCD) Partners (the United States Agency for International Development (USAID), World Vision, and the Australian Government). It was prepared by Asafeer Education Technologies FZ LLC and does not necessarily reflect the views of the ACR GCD Partners. Any adaptation or translation of this work should not be considered an official ACR GCD translation and ACR GCD shall not be liable for any content or error in this translation.</p>"));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © Asafeer Education Technologies FZ LLC, 2018"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Null, "The inside back cover in the data div should not be set.");
		}
		const string _miniNumOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">3538852d-2ec3-49b0-a548-d385848b1423</dc:identifier>
		<dc:title>Mini Num</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-02-25T11:17:58Z</meta>
		<dc:description>Mini Num, the tardigrade (water bear), learns the value of being different.</dc:description>
		<dc:creator id=""contributor_1"">El-Sayyed Ibraheem</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Mostapha Al-Barshomi</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""41a9c1d8188fecef429e48521a8fe200.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""069dd655ca62b0c5f476446ce84d8b72.jpg"" id=""image-21324-0"" media-type=""image/jpeg"" />
		<item href=""c264a8fa3bce4416fdd903cfdcef27cc.png"" id=""image-20538-1"" media-type=""image/png"" />
		<item href=""1af6d646ab3a0d97ec8ff797825a6f18.jpg"" id=""image-21340-22"" media-type=""image/jpeg"" />
		<item href=""0599389b1be03ae06bc492ed7af54c8f.jpg"" id=""image-21341-23"" media-type=""image/jpeg"" />
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
	</manifest>
</package>";
		const string _miniNumOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:3538852d-2ec3-49b0-a548-d385848b1423</id>
<title>Mini Num</title>
<author>
<name>El-Sayyed Ibraheem</name>
</author>
<contributor type=""Illustrator"">
<name>Mostapha Al-Barshomi</name>
</contributor>
<dc:license>Creative Commons Attribution Non Commercial Share Alike 4.0 International</dc:license>
<dc:publisher>3Asafeer</dc:publisher>
<updated>2018-10-02T00:00:00Z</updated>
<dc:created>2019-04-29T00:00:00Z</dc:created>
<published>2018-10-02T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Level 2"" />
<summary>Mini Num, the tardigrade (water bear), learns the value of being different.</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/41a9c1d8188fecef429e48521a8fe200"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/41a9c1d8188fecef429e48521a8fe200?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/3538852d-2ec3-49b0-a548-d385848b1423.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/3538852d-2ec3-49b0-a548-d385848b1423.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _miniNumPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""069dd655ca62b0c5f476446ce84d8b72.jpg"" />
<p>
 Mini
</p>
<p>
 Num
</p>
<p>
 Author: Al-Sayed Ibrahim
</p>
<p>
 Illustrator: Mostafa Al-Barshoom
</p>
<img data-resource_size=""150"" width=""150"" src=""c264a8fa3bce4416fdd903cfdcef27cc.png"" />
<p>
 3 a s a f e e r . c o m
</p></body>
</html>";
		const string _miniNumPage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img data-resource_size=""150"" width=""150"" src=""875144e42ad79c7fbcfa025d9f664943.png"" />
<p>
 The original work of this book was made possible through the generous support of the All Children Reading: A Grand Challenge for Development (ACR GCD) Partners (the United States Agency for International Development (USAID), World Vision, and the Australian Government). It was prepared by Asafeer Education Technologies FZ LLC and does not necessarily reflect the views of the ACR GCD Partners. Any adaptation or translation of this work should not be considered an official ACR GCD translation and ACR GCD shall not be liable for any content or error in this translation.
</p>
<img data-resource_size=""200"" width=""200"" src=""e5a4391fb765b113a36c7212d62f171e.jpg"" />
<img data-resource_size=""200"" width=""200"" src=""fab5309f47c06b8a76c1b9f0cf38e959.png"" />
<img data-resource_size=""200"" width=""200"" src=""fec090169da14c8b198eaff508d9fbb0.png"" />
<img data-resource_size=""100"" width=""100"" src=""671491aaacc6cfe7012f9e62b5b1ccdb.png"" />
</body>
</html>";
		const string _miniNumPage3Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 3</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""11eee87111091e1fe005745714df306f.jpg"" />
<p>
 Amid the noise in the forest, Mini Num the bear hatched in the depths of the quiet river. He drifted far away in the current and he found himself quite alone.
</p></body>
</html>";
		const string _miniNumPage4Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 4</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""2e9d898f5ae82b2a731206fee76a5354.jpg"" />
<p>
 None of the animals ever knew he was there, so small he was like a particle in the air. How would they ever see him if he’s that small? They probably won’t notice him at all.
</p></body>
</html>";
		const string _miniNumPage18Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 18</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""1af6d646ab3a0d97ec8ff797825a6f18.jpg"" />
<p>
 Tiny laughed as he swam and said, “We can’t get burned! Did you know I survived an atomic bomb in World War II?  Mini Num hadn’t known that but now he was convinced. So he jumped in, shouting, “We ARE amazing, no matter how small we are!”
</p></body>
</html>";
		const string _miniNumPage19Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 19</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""0599389b1be03ae06bc492ed7af54c8f.jpg"" />
<p>
 The End
</p></body>
</html>";

		/// <summary>
		/// This tests converting the Global Digital Library version of "The Garbage Monster" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * content pages use paragraph markup for the text
		/// * chapter 2 (page 1) is an acknowledgements page, not a content page.
		/// * there are no end credit pages: the copyright is implied and the license given only by an image page 1 (chapter 2)
		/// * the cover page places the title following the author and illustrator, and splits the words of the title across lines.
		/// </summary>
		[Test]
		public void TestConvertingTheGarbageMonster_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _garbageOpfXml, _garbageOpdsXml);
			var dataDiv0 = CheckInitialBookSetup(convert, "The Garbage Monster");

			// SUT
			convert.ConvertPage(0, _garbagePage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "The Garbage Monster", "c098c8faab74b36f4f7f47740abce964.jpg",
				@"<p>
 Author:
 Layla
 Audi
</p><p>
 Illustrator:
 Hanan
 Al-Karargy
</p>",
				out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "39187b6109946a555d0aa2590ad7dfc3.png", null);

			// SUT
			var result = convert.ConvertPage(1, _garbagePage2Xhtml, "2.xhtml");
			Assert.That(result, Is.True, "converting The Garbage Monster chapter 2 succeeded");
			var pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(1), "Only one page (the cover page) exists after converting the cover page and the second (acknowledgements) page.");

			// SUT
			result = convert.ConvertPage(2, _garbagePage3Xhtml, "3.xhtml");
			Assert.That(result, Is.True, "converting The Garbage Monster chapter 3 succeeded");
			var firstPageImage = CheckTrueContentPageImport(convert._bloomDoc, "1", 2, "03fed45a79182e03f23ef567d95cb554.jpg",
				@"<p>Bishu
 lived
 happily
 in
 Bubbly
 Bill
 Village
 near
 Lemonade
 River.
 He
 loved
 collecting
 tin
 cans,
 bottles
 and
 paper
 and
 wouldn’t
 throw
 them
 away,
 but
 why?</p>");

			// SUT
			result = convert.ConvertPage(21, _garbagePage22Xhtml, "22.xhtml");
			Assert.That(result, Is.True, "converting The Garbage Monster chapter 22 succeeded");
			var secondPageImage = CheckTrueContentPageImport(convert._bloomDoc, "20", 3, "a6040707f0f75a1a141b5ebe6e93655a.jpg",
				@"<p>THE
 END</p>");

			// SUT
			convert.SetAsafeerImageCredits();
			// TODO CHECK THIS OUT!
			CheckTwoPageBookAfterEndPages(convert, coverImg, coverImageData, firstPageImage, secondPageImage,
	"Copyright © Asafeer Education Technologies FZ LLC, 2018", "CC BY-NC-SA 4.0", "Hanan Al-Karargy",
	"Copyright © Asafeer Education Technologies FZ LLC, 2018", "https://creativecommons.org/licenses/by-nc-sa/4.0/",
	@"<p>Written by Layla Audi.</p>
<p>Images by Hanan Al-Karargy. © Asafeer Education Technologies FZ LLC, 2018. CC BY-NC-SA 4.0.</p>",
	null);
			var originalAckData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalAcknowledgments' and @lang='en']") as XmlElement;
			Assert.That(originalAckData, Is.Not.Null, "Acknowledgements page sets originalAcknowledgments in data div");
			Assert.That(originalAckData.InnerXml, Is.EqualTo("<p>The original work of this book was made possible through the generous support of the All Children Reading: A Grand Challenge for Development (ACR GCD) Partners (the United States Agency for International Development (USAID), World Vision, and the Australian Government). It was prepared by Asafeer Education Technologies FZ LLC and does not necessarily reflect the views of the ACR GCD Partners. Any adaptation or translation of this work should not be considered an official ACR GCD translation and ACR GCD shall not be liable for any content or error in this translation.</p>"));
		}
		const string _garbageOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">dfd8b7cb-dfbc-4d16-b490-5cdbb1776730</dc:identifier>
		<dc:title>The Garbage Monster</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:11:28Z</meta>
		<dc:description>The Garbage Monster</dc:description>
		<dc:creator id=""contributor_1"">Layla Audi</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Hanan Al-Karargy</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""fa21492bf8fb8800028bb03226e10e7e.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""c098c8faab74b36f4f7f47740abce964.jpg"" id=""image-13612-0"" media-type=""image/jpeg"" />
		<item href=""39187b6109946a555d0aa2590ad7dfc3.png"" id=""image-13613-1"" media-type=""image/png"" />
		<item href=""4a4f7676089264ea017d6864c3fc69a8.png"" id=""image-14211-2"" media-type=""image/png"" />
		<item href=""9a668c34be6e6fd9a4a3db65ef00825a.png"" id=""image-14212-3"" media-type=""image/png"" />
		<item href=""8365a4264f99914742806bf950a4df07.png"" id=""image-14213-4"" media-type=""image/png"" />
		<item href=""19d5730405dc3ef4f9ab77113b7aaf0f.png"" id=""image-14214-5"" media-type=""image/png"" />
		<item href=""03fed45a79182e03f23ef567d95cb554.jpg"" id=""image-13620-6"" media-type=""image/jpeg"" />
		<item href=""477f47cc28cf3f40a368787b556844d7.jpg"" id=""image-13621-7"" media-type=""image/jpeg"" />
		<item href=""bf09775e3c05ced024e1413b3fed4fac.jpg"" id=""image-13622-8"" media-type=""image/jpeg"" />
		<item href=""6a67e8594677b53b67686a279c01e071.jpg"" id=""image-13623-9"" media-type=""image/jpeg"" />
		<item href=""3264b38983b480b0d25db636c151f4e2.jpg"" id=""image-13624-10"" media-type=""image/jpeg"" />
		<item href=""50f50eeeb093e2d67c9dad8c10939add.jpg"" id=""image-13625-11"" media-type=""image/jpeg"" />
		<item href=""468ef548d10f95e3b9691dff382a0809.jpg"" id=""image-13626-12"" media-type=""image/jpeg"" />
		<item href=""abbc8370ace4e46d24b45a1856ee0085.jpg"" id=""image-13627-13"" media-type=""image/jpeg"" />
		<item href=""f8f73360d9868e2781cfbd8e96bbc8f2.jpg"" id=""image-13628-14"" media-type=""image/jpeg"" />
		<item href=""aa78499e96695df6d94ea2b1eb309d1d.jpg"" id=""image-13629-15"" media-type=""image/jpeg"" />
		<item href=""1bc4a0964e5ca519a5b15577c58a6a89.jpg"" id=""image-13630-16"" media-type=""image/jpeg"" />
		<item href=""880df9e9c4fc8beb73b69059f389215c.jpg"" id=""image-13631-17"" media-type=""image/jpeg"" />
		<item href=""a2db193b02c07c566a4e1c2d736453e2.jpg"" id=""image-13632-18"" media-type=""image/jpeg"" />
		<item href=""04c27b6bdd546f1c12517aeb08a8c98d.jpg"" id=""image-13614-19"" media-type=""image/jpeg"" />
		<item href=""3d93d691a93bb3ec342da67794dc7167.jpg"" id=""image-13633-20"" media-type=""image/jpeg"" />
		<item href=""46a2cc2176632998e53cf43e36cd01ca.jpg"" id=""image-13634-21"" media-type=""image/jpeg"" />
		<item href=""9977d1494c47813d8be6b9aadc24539f.jpg"" id=""image-13635-22"" media-type=""image/jpeg"" />
		<item href=""c8584cd4408ba4eb4c93e15c5a999d71.jpg"" id=""image-13636-23"" media-type=""image/jpeg"" />
		<item href=""88bd698c1f40dc6502ee200a85c1fcd0.jpg"" id=""image-13637-24"" media-type=""image/jpeg"" />
		<item href=""a6040707f0f75a1a141b5ebe6e93655a.jpg"" id=""image-13615-25"" media-type=""image/jpeg"" />
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
		<item href=""chapter-21.xhtml"" id=""chapter-21"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-22.xhtml"" id=""chapter-22"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _garbageOpdsXml = @"<feed xmlns='http://www.w3.org/2005/Atom' xmlns:lrmi='http://purl.org/dcx/lrmi-terms/' xmlns:dc='http://purl.org/dc/terms/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:opds='http://opds-spec.org/2010/catalog'>
<title>Global Digital Library - Book Catalog [extract]</title>
<entry>
<id>urn:uuid:dfd8b7cb-dfbc-4d16-b490-5cdbb1776730</id>
<title>The Garbage Monster</title>
<author>
<name>Layla Audi</name>
</author>
<contributor type=""Illustrator"">
<name>Hanan Al-Karargy</name>
</contributor>
<dc:license>Creative Commons Attribution Non Commercial Share Alike 4.0 International</dc:license>
<dc:publisher>3Asafeer</dc:publisher>
<updated>2018-09-20T00:00:00Z</updated>
<dc:created>2018-09-20T00:00:00Z</dc:created>
<published>2018-09-20T00:00:00Z</published>
<lrmi:educationalAlignment alignmentType=""readingLevel"" targetName=""Read aloud"" />
<summary>The Garbage Monster</summary>
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/fa21492bf8fb8800028bb03226e10e7e"" type=""image/jpeg"" rel=""http://opds-spec.org/image"" />
<link href=""https://res.cloudinary.com/dwqxoowxi/f_auto,q_auto/fa21492bf8fb8800028bb03226e10e7e?width=200"" type=""image/png"" rel=""http://opds-spec.org/image/thumbnail"" />
<link href=""https://books.digitallibrary.io/epub/en/dfd8b7cb-dfbc-4d16-b490-5cdbb1776730.epub"" type=""application/epub+zip"" rel=""http://opds-spec.org/acquisition/open-access"" />
<link href=""https://books.digitallibrary.io/pdf/en/dfd8b7cb-dfbc-4d16-b490-5cdbb1776730.pdf"" type=""application/pdf"" rel=""http://opds-spec.org/acquisition/open-access"" />
<dcterms:language>English</dcterms:language>
</entry>
</feed>";
		const string _garbagePage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""c098c8faab74b36f4f7f47740abce964.jpg"" />
<p>
 Author:
 Layla
 Audi
</p>
<p>
 Illustrator:
 Hanan
 Al-Karargy
</p>
<p>
 The
 Garbage
 Monster
</p>
<img data-resource_size=""150"" width=""150"" src=""39187b6109946a555d0aa2590ad7dfc3.png"" />
<p>
 3asafeer.com
</p>
</body>
</html>";
		const string _garbagePage2Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 2</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><p>
 The original work of this book was made possible through the generous support of the All Children Reading: A Grand Challenge for Development (ACR GCD) Partners (the United States Agency for International Development (USAID), World Vision, and the Australian Government). It was prepared by Asafeer Education Technologies FZ LLC and does not necessarily reflect the views of the ACR GCD Partners. Any adaptation or translation of this work should not be considered an official ACR GCD translation and ACR GCD shall not be liable for any content or error in this translation.
</p>
<img data-resource_size=""200"" width=""200"" src=""4a4f7676089264ea017d6864c3fc69a8.png"" />
<img data-resource_size=""200"" width=""200"" src=""9a668c34be6e6fd9a4a3db65ef00825a.png"" />
<img data-resource_size=""200"" width=""200"" src=""8365a4264f99914742806bf950a4df07.png"" />
<img data-resource_size=""100"" width=""100"" src=""19d5730405dc3ef4f9ab77113b7aaf0f.png"" /></body>
</html>";
		const string _garbagePage3Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 3</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""03fed45a79182e03f23ef567d95cb554.jpg"" />
<p>
 Bishu
 lived
 happily
 in
 Bubbly
 Bill
 Village
 near
 Lemonade
 River.
 He
 loved
 collecting
 tin
 cans,
 bottles
 and
 paper
 and
 wouldn’t
 throw
 them
 away,
 but
 why?
</p>
</body>
</html>";
		const string _garbagePage22Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 22</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""a6040707f0f75a1a141b5ebe6e93655a.jpg"" />
<p>
 THE
 END
</p>
</body>
</html>";

		/// <summary>
		/// This tests converting the cover page of Global Digital Library version of "The Centipede’s Problem" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * title split into 3 paragraphs on the front cover
		/// </summary>
		[Test]
		public void TestConvertingTheCentipedesProblemCover_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _centipedeOpfXml, null);
			var dataDiv0 = CheckInitialBookSetup(convert, "The Centipede's Problem", false);    // title from epub metadata

			// SUT
			convert.ConvertPage(0, _centipedePage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "The Centipede’s Problem",   // title from cover page
				"2e36be50ae9e12d26ff140bb3f44a3c6.jpg",
				@"<p>
 Author: Asmaa Emara
</p><p>
 Illustrator: Noreen Khan
</p>",
				out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "c264a8fa3bce4416fdd903cfdcef27cc.png", null);
		}
		const string _centipedeOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">bf3c9678-a012-4b98-898d-d4cd41dbd831</dc:identifier>
		<dc:title>The Centipede&apos;s Problem</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:08:01Z</meta>
		<dc:description>Mother centipede needs to go shoe shopping for her little ones. Will she ever get what she needs?</dc:description>
		<dc:creator id=""contributor_1"">Asmaa Emara</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Nooren Khan</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""229571ba8b8244d4750e2b1b563e1879.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""2e36be50ae9e12d26ff140bb3f44a3c6.jpg"" id=""image-21584-0"" media-type=""image/jpeg"" />
		<item href=""bcad2b0a6c493c6eb23c1c3f6cd969c0.jpg"" id=""image-21583-29"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-25.xhtml"" id=""chapter-25"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _centipedePage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""2e36be50ae9e12d26ff140bb3f44a3c6.jpg"" />
<p>
 The
</p>
<p>
 Centipede’s
</p>
<p>
 Problem
</p>
<img data-resource_size=""150"" width=""150"" src=""c264a8fa3bce4416fdd903cfdcef27cc.png"" />
<p>
 3asafeer.com
</p>
<p>
 Author: Asmaa Emara
</p>
<p>
 Illustrator: Noreen Khan
</p></body>
</html>";

		/// <summary>
		/// This tests converting the cover page of Global Digital Library version of "Jack and the Magic Grape Seeds" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * title split into 2 paragraphs on the front cover, and the words in the paragraphs separated by newlines
		/// * extra words on the cover that aren't really title, but not author or illustrator
		/// * author and illustrator in the same paragraph
		/// * title differs between epub metadata and cover page in capitalization
		/// </summary>
		[Test]
		public void TestConvertingJackAndTheEtcCover_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _jackOpfXml, null);
			var dataDiv0 = CheckInitialBookSetup(convert, "Jack and the magic grape seeds", false); // title from epub metadata

			// SUT
			convert.ConvertPage(0, _jackPage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Jack and the Magic Grape Seeds",    // title from cover page
				"d40cf1083253ea28e0d1534d31ca5a5a.jpg",
				@"<p>
 2092
 Version
</p><p>
 Illustrator:
 Nawras
 Douqa
 Author:
 Maria
 Daadouch
</p>",
				out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "652930789d77e9df9da69074760707d9.png", null);
		}
		const string _jackOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">a3f840c7-1176-4134-a9b3-a834acb4aa90</dc:identifier>
		<dc:title>Jack and the magic grape seeds</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:07:27Z</meta>
		<dc:description>Jack and the magic grape seeds</dc:description>
		<dc:creator id=""contributor_1"">Maria Daadouch</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Nawras Douqa</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""484979e0fe163e78557db46b3e217720.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""d40cf1083253ea28e0d1534d31ca5a5a.jpg"" id=""image-19551-0"" media-type=""image/jpeg"" />
		<item href=""71588addaade5467d6291b71f21574cf.jpg"" id=""image-19578-27"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-24.xhtml"" id=""chapter-24"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _jackPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""d40cf1083253ea28e0d1534d31ca5a5a.jpg"" />
<p>
 Jack
 and
 the
</p>
<p>
 Magic
 Grape
 Seeds
</p>
<p>
 2092
 Version
</p>
<p>
 Illustrator:
 Nawras
 Douqa
 Author:
 Maria
 Daadouch
</p>
<img data-resource_size=""150"" width=""150"" src=""652930789d77e9df9da69074760707d9.png"" />
<p>
 3asafeer.com
</p>
</body>
</html>";

		/// <summary>
		/// This tests converting the cover page of Global Digital Library version of "Don’t Open this Book" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * title split into 2 paragraphs on the front cover
		/// * title follows the author and illustrator
		/// * title differs between epub metadata and cover page in punctuation and capitalization
		/// </summary>
		[Test]
		public void TestConvertingDontOpenThisBookCover_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _dontOpenOpfXml, null);
			var dataDiv0 = CheckInitialBookSetup(convert, "Don't open this book…", false);  // title from epub metadata

			// SUT
			convert.ConvertPage(0, _dontOpenPage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Don’t Open this Book",  // title from cover page
				"a79fdc2db9718139b8de32870b32aeb2.jpg",
				@"<p>
 Author:Lamees Asali
</p><p>
 Illustrator:Youmna Ibraheem
</p>",
				out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "c264a8fa3bce4416fdd903cfdcef27cc.png", null);
		}
		const string _dontOpenOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">7f287318-ed90-43b8-b450-9338ff9652f8</dc:identifier>
		<dc:title>Don&apos;t open this book…</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:13:18Z</meta>
		<dc:description>An illustrators jumps into his own story and learns all about owls .</dc:description>
		<dc:creator id=""contributor_1"">Lamis Asali</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Yomna Ibraheem</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""ac20ba2e2844d2bbb1a6dcb06f15a58a.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""a79fdc2db9718139b8de32870b32aeb2.jpg"" id=""image-20873-0"" media-type=""image/jpeg"" />
		<item href=""ebec8d40d6aa9409d9d03681e2451ac1.jpg"" id=""image-20891-27"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-23.xhtml"" id=""chapter-23"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _dontOpenPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""a79fdc2db9718139b8de32870b32aeb2.jpg"" />
<p>
</p>
<p>
 Author:Lamees Asali
</p>
<p>
 Illustrator:Youmna Ibraheem
</p>
<p>
 Don’t Open this
</p>
<p>
 Book
</p>
<img data-resource_size=""150"" width=""150"" src=""c264a8fa3bce4416fdd903cfdcef27cc.png"" />
<p>
 3asafeer.com
</p></body>
</html>";

		/// <summary>
		/// This tests converting the cover page of Global Digital Library version of "We’re Not Alone" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 2 images on the front cover page
		/// * title split into 4 paragraphs on the front cover, with "We’re" split into 2 paragraphs and 3 lines
		/// * title follows the author and illustrator
		/// * title differs between epub metadata and cover page in punctuation and capitalization
		/// </summary>
		[Test]
		public void TestConvertingWereNotAloneCover_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _notAloneOpfXml, null);
			var dataDiv0 = CheckInitialBookSetup(convert, "We are not Alone", false);  // title from epub metadata

			// SUT
			convert.ConvertPage(0, _notAlonePage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "We’re Not Alone",  // title from cover page
				"a5898af755806d2177dbb129da871d5c.jpg",
				@"<p>
 Author
 :
 Asma’a
 Amara
</p><p>
 Illustrator
 :
 Rasha
 Sami
</p>",
				out XmlElement coverImageData);
			// This book has one extra image on the front cover page.  We save this information even though it doesn't do any good.
			CheckExtraCoverImages(convert._bloomDoc, "652930789d77e9df9da69074760707d9.png", null);
		}
		const string _notAloneOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">2566a7d5-dc9c-43aa-abdb-7f060f488bad</dc:identifier>
		<dc:title>We are not Alone</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:08:37Z</meta>
		<dc:description>We are not Alone</dc:description>
		<dc:creator id=""contributor_1"">Asmaa Emara</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Rasha Sami</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""acb56dd339abc0ec67ed02f5f47994c8.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""a5898af755806d2177dbb129da871d5c.jpg"" id=""image-20128-0"" media-type=""image/jpeg"" />
		<item href=""652930789d77e9df9da69074760707d9.png"" id=""image-20129-1"" media-type=""image/png"" />
		<item href=""6efd71bdd6b642f64392802fc215436f.jpg"" id=""image-20161-33"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-30.xhtml"" id=""chapter-30"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _notAlonePage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""a5898af755806d2177dbb129da871d5c.jpg"" />
<img data-resource_size=""150"" width=""150"" src=""652930789d77e9df9da69074760707d9.png"" />
<p>
 3asafeer.com
</p>
<p>
 Author
 :
 Asma’a
 Amara
</p>
<p>
 Illustrator
 :
 Rasha
 Sami
</p>
<p>
 We
</p>
<p>
 ’
 re
</p>
<p>
 Not
</p>
<p>
 Alone
</p>
</body>
</html>";

		/// <summary>
		/// This tests converting the cover page of Global Digital Library version of "Mrs. Witty and the Coconut Tree" published by 3Asafeer.
		/// It has these distinctive features:
		/// * 1 image on the front cover page
		/// * title split into 2 paragraphs on the front cover
		/// * "Translation:" and "Published by:" instead of "Author:" and "Illustrator:"
		/// * title differs between epub metadata and cover page in punctuation and capitalization
		/// </summary>
		[Test]
		public void TestConvertingMrsWittyCover_GDL()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid invalid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _mrsWittyOpfXml, null);
			var dataDiv0 = CheckInitialBookSetup(convert, "Ms Witty and the Coconut Tree", false);  // title from epub metadata

			// SUT
			convert.ConvertPage(0, _mrsWittyPage1Xhtml, "1.xhtml");
			var coverImg = CheckCoverPageImport(convert, dataDiv0, "Mrs. Witty and the Coconut Tree",  // title from cover page
				"edd007783f43083f16236073271c7825.jpg",
				@"<p>
 Translation:
 Weaam
 Ahmed
</p><p>
 Published
 by:
 Asafeer
</p>",
				out XmlElement coverImageData);
		}
		const string _mrsWittyOpfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">4b059526-421a-426f-b8ca-fc2aaaa6ad1a</dc:identifier>
		<dc:title>Ms Witty and the Coconut Tree</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-03-02T09:14:37Z</meta>
		<dc:description>Ms Witty and the Coconut Tree</dc:description>
		<dc:creator id=""contributor_1"">Layla Audi</dc:creator>
		<meta refines=""#contributor_1"" property=""role"" scheme=""marc:relators"">aut</meta>
		<dc:contributor id=""contributor_2"">Ayah Khamees</dc:contributor>
		<meta refines=""#contributor_2"" property=""role"" scheme=""marc:relators"">ill</meta>
	</metadata>
	<manifest>
		<item href=""toc.xhtml"" id=""toc"" media-type=""application/xhtml+xml"" properties=""nav"" />
		<item href=""epub.css"" id=""css"" media-type=""text/css"" />
		<item href=""a0cbb6ad7f87aa41f51896c24f454d92.jpg"" id=""cover"" media-type=""image/jpeg"" properties=""cover-image"" />
		<item href=""edd007783f43083f16236073271c7825.jpg"" id=""image-13869-0"" media-type=""image/jpeg"" />
		<item href=""4a4f7676089264ea017d6864c3fc69a8.png"" id=""image-14211-1"" media-type=""image/png"" />
		<item href=""deb33ca402db839cbcefc36c298aef31.jpg"" id=""image-13870-24"" media-type=""image/jpeg"" />
		<item href=""chapter-1.xhtml"" id=""chapter-1"" media-type=""application/xhtml+xml"" />
		<item href=""chapter-22.xhtml"" id=""chapter-22"" media-type=""application/xhtml+xml"" />
	</manifest>
</package>";
		const string _mrsWittyPage1Xhtml = @"<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Chapter 1</title>
    <link href=""epub.css"" rel=""stylesheet"" type=""text/css""/>
</head>
<body><img src=""edd007783f43083f16236073271c7825.jpg"" />
<p>
 Mrs.
 Witty
</p>
<p>
 and
 the
 Coconut
 Tree
</p>
<p>
 Translation:
 Weaam
 Ahmed
</p>
<p>
 Published
 by:
 Asafeer
</p>
</body>
</html>";
	}
}
