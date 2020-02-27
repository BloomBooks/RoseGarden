using System;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class ConvertingBengaliGDL : ConversionTestBase
	{
		public ConvertingBengaliGDL()
		{
		}

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
				"<p>All illustrations by Mrigaja Bajracharya. Copyright © The Asia Foundation, 2018. Some rights reserved. Released under the CC BY-NC 4.0 license.</p>");
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
	}
}
