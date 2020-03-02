using System;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class ConvertingIndonesianGDL : ConversionTestBase
	{
		public ConvertingIndonesianGDL()
		{
		}

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
 Aku ngantuk sekali, sampai rasanya mau jatuh.</p>", "id");

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
				"<p>Images by Hari Kumar Nair. © The Asia Foundation, 2018. CC BY 4.0.</p>");
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
	}
}
