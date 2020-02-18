// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using RoseGarden;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Linq;

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
			var options = new ConvertOptions()
			{
				UsePortrait = true,
				UseLandscape = true
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
		public void TestConvertingGoat()
		{
			// SUT (UsePortrait or UseLandscape must be true to avoid file access)
			var convert = InitializeForConversions(new ConvertOptions() { LanguageName = "English", UsePortrait = true }, _goatOpfXml, _goatOpdsXml);
			Assert.That(convert._epubMetaData.Title, Is.EqualTo("Goat, The False King"));
			Assert.That(convert._opdsEntry, Is.Not.Null);
			Assert.That(convert._templatePages.Count, Is.GreaterThan(1));
			Assert.That(convert._bloomDoc, Is.Not.Null);
			var page0 = convert._bloomDoc.SelectSingleNode("/html/body/div[contains(@class,'bloom-page')]") as XmlElement;
			Assert.That(page0, Is.Null, "There should not be any pages in the empty initial book.");
			var dataDiv0 = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']");
			Assert.That(dataDiv0, Is.Not.Null, "The data div should exist in the empty initial book.");
			var titleNode0 = convert._bloomDoc.SelectSingleNode("/html/head/title") as XmlElement;
			Assert.That(titleNode0, Is.Not.Null, "The title in the header should be set even in the empty book.");
			Assert.That(titleNode0.InnerText, Is.EqualTo("Book"));

			// SUT
			convert.ConvertCoverPage(_goatPage1Xhtml);
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
			Assert.That(titleNode.InnerText, Is.EqualTo("Goat, The False King"));
			var titleData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='en']") as XmlElement;
			Assert.That(titleData, Is.Not.Null, "The bookTitle should be set in the data div.");
			Assert.That(titleData.InnerXml, Is.EqualTo("<p>Goat, The False King</p>"));
			var titleDiv = convert._bloomDoc.SelectSingleNode("//div[contains(@class, 'bloom-editable') and @data-book='bookTitle' and @lang='en']") as XmlElement;
			Assert.That(titleDiv, Is.Not.Null, "The title should be set on the front cover page.");
			Assert.That(titleDiv.InnerXml, Is.EqualTo("<p>Goat, The False King</p>"));
			var coverImageData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage' and @lang='*']") as XmlElement;
			Assert.That(coverImageData, Is.Not.Null, "The cover image is set in the data div.");
			Assert.That(coverImageData.InnerXml, Is.EqualTo("c7b42f14c72ad4a3b3488c4377b70d94.jpg"));
			Assert.That(coverImageData.GetAttribute("data-copyright"), Is.Empty, "Copyrights aren't set from the cover page.");
			Assert.That(coverImageData.GetAttribute("data-license"), Is.Empty, "Licenses aren't set from the cover page.");
			var coverImg = convert._bloomDoc.SelectSingleNode("//div[@class='bloom-imageContainer']/img[@data-book='coverImage']") as XmlElement;
			Assert.That(coverImg, Is.Not.Null, "The cover image should be set on the front cover page.");
			Assert.That(coverImg.GetAttribute("src"), Is.EqualTo("c7b42f14c72ad4a3b3488c4377b70d94.jpg"));
			var creditsData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='smallCoverCredits' and @lang='en']") as XmlElement;
			Assert.That(creditsData, Is.Not.Null, "Cover credits should be set in the data div.");
			Assert.That(creditsData.InnerXml, Does.StartWith("<p>Author: Alice Nakasango</p>"));
			Assert.That(creditsData.InnerXml, Does.EndWith("<p>Illustrator: Marleen Visser</p>"));
			var creditsDiv = convert._bloomDoc.SelectSingleNode("//div[contains(@class, 'bloom-editable') and @data-book='smallCoverCredits' and @lang='en']");
			Assert.That(creditsDiv, Is.Not.Null, "The credits should be inserted into the front cover page.");
			Assert.That(creditsDiv.InnerXml, Does.StartWith("<p>Author: Alice Nakasango</p>"));
			Assert.That(creditsDiv.InnerXml, Does.EndWith("<p>Illustrator: Marleen Visser</p>"));

			// SUT
			var result = convert.ConvertContentPage(1, _goatPage2Xhtml);
			Assert.That(result, Is.True, "converting Goat page 2 succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(2), "Two pages should exist after converting the cover page and one content page. (list has two pages)");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'numberedPage')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(1), "A numbered page should exist after converting a content page. (list has one page)");
			Assert.That(pages[0], Is.Not.Null, "A numbered page should exist after converting a content page. (page not null)");
			Assert.That(pages[0].GetAttribute("class"), Does.Contain("bloom-page"));
			Assert.That(pages[0].GetAttribute("data-page-number"), Is.EqualTo("1"), "The numbered page has the right page number (1).");
			var page1ImgList = pages[0].SelectNodes(".//div[contains(@class,'bloom-imageContainer')]/img").Cast<XmlElement>().ToList();
			Assert.That(page1ImgList.Count, Is.EqualTo(1), "Page 1 has one image (list has one item)");
			Assert.That(page1ImgList[0].GetAttribute("src"), Is.EqualTo("27e900b0dc523b77e981b601a779c6a0.jpg"));
			var page1TextList = pages[0].SelectNodes(".//div[contains(@class,'bloom-translationGroup')]/div[contains(@class,'bloom-editable') and @lang='en']").Cast<XmlElement>().ToList();
			Assert.That(page1TextList.Count, Is.EqualTo(1), "Page 1 has one text block (list has one item)");
			Assert.That(page1TextList[0].InnerXml, Is.EqualTo("<p>Once upon a time,  there was a goat called Igodhoobe. Igodhoobe the goat was the king of farm animals and birds. He lived a good life. One day,  Igodhoobe the goat called all the animals and birds to a meeting.</p>"));

			// SUT
			result = convert.ConvertContentPage(18, _goatPage19Xhtml);
			Assert.That(result, Is.True, "converting Goat page 19 succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page and two content pages. (list has three pages)");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'numberedPage')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(2), "Two numbered pages should exist after converting a content page. (list has two pages)");
			Assert.That(pages[1], Is.Not.Null, "A numbered page should exist after converting a content page. (page not null)");
			Assert.That(pages[1].GetAttribute("class"), Does.Contain("bloom-page"));
			Assert.That(pages[0].GetAttribute("data-page-number"), Is.EqualTo("1"), "The first numbered page has the right page number (18).");
			Assert.That(pages[1].GetAttribute("data-page-number"), Is.EqualTo("18"), "The second numbered page has the right page number (18 forced by test).");
			var page2ImgList = pages[1].SelectNodes(".//div[contains(@class,'bloom-imageContainer')]/img").Cast<XmlElement>().ToList();
			Assert.That(page2ImgList.Count, Is.EqualTo(1), "Page 18 has one image (list has one item)");
			Assert.That(page2ImgList[0].GetAttribute("src"), Is.EqualTo("fd3e3272e2bf6aefc67c827f529b3891.jpg"));
			var page2TextList = pages[1].SelectNodes(".//div[contains(@class,'bloom-translationGroup')]/div[contains(@class,'bloom-editable') and @lang='en']").Cast<XmlElement>().ToList();
			Assert.That(page2TextList.Count, Is.EqualTo(1), "Page 18 has one text block (list has one item)");
			Assert.That(page2TextList[0].InnerXml, Is.EqualTo("<p>From that time,  every goat refuses to move when it is pulled. It thinks that you are taking it to the king's court.</p>"));

			// SUT
			result = convert.ConvertContentPage(19, _goatPage20Xhtml);
			Assert.That(result, Is.True, "converting Goat page 20 (end page) succeeded");
			pages = convert._bloomDoc.SelectNodes("/html/body/div[contains(@class,'bloom-page')]").Cast<XmlElement>().ToList();
			Assert.That(pages.Count, Is.EqualTo(3), "Three pages should exist after converting the cover page, two content pages, and one end page. (list has three pages)");
			Assert.That(coverImageData.GetAttribute("data-copyright"), Is.EqualTo("© African Storybook Initiative 2015"), "End page sets cover image copyright in data div");
			Assert.That(coverImageData.GetAttribute("data-license"), Is.EqualTo("CC BY 4.0"), "End page sets cover image license in data div");
			Assert.That(coverImageData.GetAttribute("data-creator"), Is.EqualTo("Marleen Visser"), "End page sets cover image creator in data div");
			Assert.That(coverImg.GetAttribute("data-copyright"), Is.EqualTo("© African Storybook Initiative 2015"), "End page sets cover image copyright");
			Assert.That(coverImg.GetAttribute("data-license"), Is.EqualTo("CC BY 4.0"), "End page sets cover image license");
			Assert.That(coverImg.GetAttribute("data-creator"), Is.EqualTo("Marleen Visser"), "End page sets cover image creator");
			Assert.That(page1ImgList[0].GetAttribute("data-copyright"), Is.EqualTo("© African Storybook Initiative 2015"), "End page sets cover image copyright");
			Assert.That(page1ImgList[0].GetAttribute("data-license"), Is.EqualTo("CC BY 4.0"), "End page sets cover image license");
			Assert.That(page1ImgList[0].GetAttribute("data-creator"), Is.EqualTo("Marleen Visser"), "End page sets cover image creator");
			Assert.That(page2ImgList[0].GetAttribute("data-copyright"), Is.EqualTo("© African Storybook Initiative 2015"), "End page sets cover image copyright");
			Assert.That(page2ImgList[0].GetAttribute("data-license"), Is.EqualTo("CC BY 4.0"), "End page sets cover image license");
			Assert.That(page2ImgList[0].GetAttribute("data-creator"), Is.EqualTo("Marleen Visser"), "End page sets cover image creator");
			var licenseUrlData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyrightUrl' and @lang='*']") as XmlElement;
			Assert.That(licenseUrlData, Is.Not.Null, "End page sets copyrightUrl in data div");
			Assert.That(licenseUrlData.InnerXml, Is.EqualTo("http://creativecommons.org/licenses/by/4.0/"));
			var originalContribData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='en']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo("All illustrations by Marleen Visser.  Copyright © African Storybook Initiative 2015.  Some rights reserved.  Released under the CC BY 4.0 license."));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo("Copyright © Uganda Community Libraries Association (Ugcla) 2015"));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='en']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Not.Null, "End page sets the inside back cover in the data div");
			Assert.That(insideBackCoverData.InnerXml, Does.StartWith("You are free to download, copy, translate or adapt this story and use the illustrations as long as you attribute in the following way:"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain("Cornelius Wambi Gulere"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain("© Text: Uganda Community Libraries Association (Ugcla) Artwork: African Storybook Initiative 2015"));
			Assert.That(insideBackCoverData.InnerXml, Does.Contain("www.africanstorybook.org"));
			Assert.That(insideBackCoverData.InnerXml, Does.EndWith(">"));
		}

		// "Goat, The False King" is from African Storybook Initiative
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
	}
}
