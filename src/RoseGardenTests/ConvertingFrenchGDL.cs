using System;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class ConvertingFrenchGDL : ConversionTestBase
	{
		public ConvertingFrenchGDL()
		{
		}

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
<p>Le mot banane est dérivé du mot arabe signifiant «" + "\u00a0" + @"doigt" + "\u00a0" + @"». L’Inde est le plus grand producteur de bananes au monde. Plus de 120" + "\u00a0" + @"variétés de bananes comestibles y sont cultivées. Le Centre national de la recherche sur les bananes à Trichy possède une collection de 1" + "\u00a0120\u00a0" + @"variétés de bananes" + "\u00a0" + @"!</p>
<p>Les bananes sont riches en minéraux qui aident à stimuler le cerveau. Les bananes rendent les étudiants plus actifs et alertes. De nombreuses friandises indiennes sont confectionnées à l’aide de bananes" + "\u00a0" + @": le payasam de banane dans le Kerala, le rasayana de banane dans le Karnataka, le halva de banane Halwa, le Rawa Kela-Gur Mithai. En savez-vous davantage" + "\u00a0" + @"?</p>",
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
		<item href=""95bf1c06bf5010e68c04664607c57336.jpg"" id=""image-9780-12"" media-type=""image/jpeg"" />
		<item href=""5af48de345d79d56cc0fa0e0ff0f851a.jpg"" id=""image-9782-20"" media-type=""image/jpeg"" />
		<item href=""f3aedcdfb0c65b279448268f70bfeca4.jpg"" id=""image-9804-24"" media-type=""image/jpeg"" />
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
	}
}
