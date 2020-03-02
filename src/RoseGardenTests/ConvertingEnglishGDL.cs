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
				"<p>Images by Marleen Visser. © African Storybook Initiative 2015. CC BY 4.0.</p>");
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
				"Copyright © Room to Read, 2013", "http://creativecommons.org/licenses/by/4.0/",
				@"<p>This story 'Dogs versus Cats' has been published on StoryWeaver by Room to Read.</p>
<p>Images by Vusi Malindi. © Room to Read, 2013. CC BY 4.0.</p>");
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
				@"<p>The development of this book has been supported by HDFC Asset Management Company Limited (A joint venture with Standard Life Investments).This book was part of the Pratham Books lab conducted in collaboration with Srishti School of Art, Design and Technology, Bangalore.</p>
<p>Images by Hari Kumar Nair. © Pratham Books, 2015. CC BY 4.0.</p>");
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
			Assert.That(originalContribData.InnerXml, Is.EqualTo(@"<p>Images on Front Cover, page 3 by Megha Vishwanath. © Pratham Books, 2015. CC BY 4.0.</p>
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
	}
}
