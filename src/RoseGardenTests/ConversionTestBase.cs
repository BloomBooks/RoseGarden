using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	public class ConversionTestBase
	{
		protected string _blankBookHtml;
		protected string _pagesFileXhtml;

		public ConversionTestBase()
		{
			// Reading the blank XHTML book and page files is our only violation of no file access.
			// I suppose I could duplicate the XHTML in the code here, but reading what the program
			// actually uses is safer for testing the program.
			var location = Assembly.GetExecutingAssembly().Location;
			var blankHtmPath = Path.Combine(Path.GetDirectoryName(location), "Resources", "Book.htm");
			_blankBookHtml = File.ReadAllText(blankHtmPath);
			var pagesFile = Path.Combine(Path.GetDirectoryName(location), "Resources", "Pages.xml");
			_pagesFileXhtml = File.ReadAllText(pagesFile);
		}

#region Utility methods
		/// <summary>
		/// This method should reflect the ConvertFromEpub.Initialize() method except for the file copying
		/// and other file I/O operations.  (plus some content from ConvertBook()
		/// </summary>
		protected ConvertFromEpub InitializeForConversions(ConvertOptions opts, string opfXml, string opdsXml)
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

		/// <summary>
		/// Checks the initial book setup to verify that the epub's opf file and the opds file were read
		/// and the book XHTML initialized properly.
		/// </summary>
		/// <returns>The book's data div from the initial setup.</returns>
		protected XmlElement CheckInitialBookSetup(ConvertFromEpub convert, string title)
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
		protected XmlElement CheckCoverPageImport(ConvertFromEpub convert, XmlElement dataDiv0, string title, string imageSrc, string creditsInnerXml, out XmlElement coverImageData, string lang = "en")
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
		protected XmlElement CheckTrueContentPageImport(XmlDocument bookDoc, string pageNumber, int pageCount, string imageSrc, string textInnerXml, string lang = "en")
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
		protected static void CheckTwoPageBookAfterEndPages(ConvertFromEpub convert, XmlElement coverImg, XmlElement coverImageData, XmlElement firstPageImage, XmlElement secondPageImage,
			string imageCopyright, string imageLicense, string imageCreator, string bookCopyright, string bookLicense, string contribInnerXml, string[] insideCoverFragments,
			string lang="en")
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
			var originalContribData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='originalContributions' and @lang='{lang}']") as XmlElement;
			Assert.That(originalContribData, Is.Not.Null, "End page sets originalContributions in data div");
			Assert.That(originalContribData.InnerXml, Is.EqualTo(contribInnerXml));
			var copyrightData = convert._bloomDoc.SelectSingleNode("/html/body/div[@id='bloomDataDiv']/div[@data-book='copyright' and @lang='*']") as XmlElement;
			Assert.That(copyrightData, Is.Not.Null, "End page sets copyright in data div");
			Assert.That(copyrightData.InnerXml, Is.EqualTo(bookCopyright));
			var insideBackCoverData = convert._bloomDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='insideBackCover' and @lang='{lang}']") as XmlElement;
			Assert.That(insideBackCoverData, Is.Not.Null, "End page sets the inside back cover in the data div");
			Assert.That(insideBackCoverData.InnerXml, Does.StartWith(insideCoverFragments[0]));
			Assert.That(insideBackCoverData.InnerXml.Trim(), Does.EndWith(insideCoverFragments[1]));
			for (int i = 2; i < insideCoverFragments.Length; ++i)
				Assert.That(insideBackCoverData.InnerXml, Does.Contain(insideCoverFragments[i]));
		}

		protected static void CheckImageMetaData(XmlElement img, string imageCreator, string imageCopyright, string imageLicense)
		{
			Assert.That(img.GetAttribute("data-copyright"), Is.EqualTo(imageCopyright), "End page sets image copyright");
			Assert.That(img.GetAttribute("data-license"), Is.EqualTo(imageLicense), "End page sets image license");
			Assert.That(img.GetAttribute("data-creator"), Is.EqualTo(imageCreator), "End page sets image creator");
		}

		protected static void CheckExtraCoverImages(XmlDocument bookDoc, string image2Src, string image3Src)
		{
			var coverImage2Data = bookDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage2' and @lang='*']") as XmlElement;
			Assert.That(coverImage2Data, Is.Not.Null, "The second cover image is set in the data div.");
			Assert.That(coverImage2Data.InnerXml, Is.EqualTo(image2Src));
			var coverImage3Data = bookDoc.SelectSingleNode($"/html/body/div[@id='bloomDataDiv']/div[@data-book='coverImage3' and @lang='*']") as XmlElement;
			Assert.That(coverImage3Data, Is.Not.Null, "The third cover image is set in the data div.");
			Assert.That(coverImage3Data.InnerXml, Is.EqualTo(image3Src));
		}
#endregion
	}
}
