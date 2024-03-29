﻿// Copyright (c) 2020 SIL International
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
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				epubPath1 = epubPath1.Replace("/", "\\");

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
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				epubPath1 = epubPath1.Replace("/", "\\");
				opfPath1 = opfPath1.Replace("/", "\\");
			}

			// SUT
			var epubMeta = new EpubMetadata(epubPath1, opfPath1, _opfXml);
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
			Assert.That(epubMeta.Modified, Is.EqualTo(DateTime.Parse("2020-02-11T11:03:04Z")));
			Assert.That(epubMeta.ImageFiles.Count, Is.EqualTo(21));
			Assert.That(epubMeta.ImageFiles[0], Is.EqualTo(Path.Combine(epubPath1, "content", "c7b42f14c72ad4a3b3488c4377b70d94.jpg")));
			Assert.That(epubMeta.PageFiles.Count, Is.EqualTo(20));
			Assert.That(epubMeta.PageFiles[6], Is.EqualTo(Path.Combine(epubPath1, "content", "chapter-7.xhtml")));
		}

		const string _opfXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""uid"">
	<metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
		<dc:identifier id=""uid"">4f513a80-8f36-46c5-a73f-3169420c5c24</dc:identifier>
		<dc:title>Goat, The False King</dc:title>
		<dc:language>en</dc:language>
		<meta property=""dcterms:modified"">2020-02-11T11:03:04Z</meta>
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
	<spine>
		<itemref idref=""toc"" linear=""no"" />
		<itemref idref=""chapter-1"" />
		<itemref idref=""chapter-2"" />
		<itemref idref=""chapter-3"" />
		<itemref idref=""chapter-4"" />
		<itemref idref=""chapter-5"" />
		<itemref idref=""chapter-6"" />
		<itemref idref=""chapter-7"" />
		<itemref idref=""chapter-8"" />
		<itemref idref=""chapter-9"" />
		<itemref idref=""chapter-10"" />
		<itemref idref=""chapter-11"" />
		<itemref idref=""chapter-12"" />
		<itemref idref=""chapter-13"" />
		<itemref idref=""chapter-14"" />
		<itemref idref=""chapter-15"" />
		<itemref idref=""chapter-16"" />
		<itemref idref=""chapter-17"" />
		<itemref idref=""chapter-18"" />
		<itemref idref=""chapter-19"" />
		<itemref idref=""chapter-20"" />
	</spine>
</package>
";

		/// <summary>
		/// Test the FixInnerXml method with examples from actual books.
		/// Each of the test strings was the content of a paragraph in the book.
		/// </summary>
		[Test]
		public void TestFixInnerXml()
		{
			//SUT - "Une souris dans la maison"
			var fixedXml = ConvertFromEpub.FixInnerXml("<b> «\u00a0Où\u00a0?\u00a0» , demanda papa montant sur la fenêtre. Il baissa les rideaux. <b> «\u00a0Là\u00a0!\u00a0» , s’écria maman en sautant sur la table. Les assiettes tombèrent avec fracas. <b> </b> <br /> </b></b>");
			Assert.That(fixedXml, Is.EqualTo("<b>«\u00a0Où\u00a0?\u00a0» , demanda papa montant sur la fenêtre. Il baissa les rideaux. <b>«\u00a0Là\u00a0!\u00a0» , s’écria maman en sautant sur la table. Les assiettes tombèrent avec fracas.</b></b>"));

			//SUT - "À l’intérieur du World Wide Web"
			fixedXml = ConvertFromEpub.FixInnerXml("<b> <i> </i> </b> <i> <b> </b> </i> <i> <b> </b> </i> <b> Donc, Nettikutti, voici ce que tout le monde veut savoir\u00a0: qu’est-ce qu’Internet\u00a0? </b> <i> </i> <i> </i> <b> </b>");
			Assert.That(fixedXml, Is.EqualTo("<b>Donc, Nettikutti, voici ce que tout le monde veut savoir\u00a0: qu’est-ce qu’Internet\u00a0?</b>"));

			//SUT - "Le garçon et le tambour"
			fixedXml = ConvertFromEpub.FixInnerXml("<i>Les</i> <i>membres du chœur</i> <i>peuvent</i> <i>bouger</i> <i>leurs</i> <i>mains</i> <i>tous</i> <i> ensemble, </i> <i>pour symboliser</i> <i>la </i> <i>rivière</i> <i>qui coule. </i> <i>Le </i> <i>blanchisseur</i> <i>et</i> <i>sa</i> <i>femme</i> <i>se dégagent</i> <i>du</i> <i></i> <i>chœur, <i>en mimant</i> <i>une</i> <i>dispute. </i> <i>Leurs </i> <i>actions</i> <i>sont</i> <i>exagérées,</i> <i>mais</i> <i>on</i> <i>n’entend <i>pas</i> <i> leurs</i> <i>voix. </i> <br /> </i></i>");
			Assert.That(fixedXml, Is.EqualTo("<i>Les membres du chœur peuvent bouger leurs mains tous ensemble, pour symboliser la rivière qui coule. Le blanchisseur et sa femme se dégagent du chœur, <i>en mimant une dispute. Leurs actions sont exagérées, mais on n’entend <i>pas leurs voix.</i></i></i>"));

			//SUT - "PLONGÉE!"
			fixedXml = ConvertFromEpub.FixInnerXml("Les <b>poissons-perroquets ont des dents robustes qui forment un bec semblable à celui des perroquets, qu’ils utilisent pour racler les algues du corail dur. Certaines espèces ne se gênent pas pour manger des morceaux de corail également, et elles défèquent ensuite un sable fin qui finit sur la terre pour former de magnifiques plages de sable blanc. <br /> </b>");
			Assert.That(fixedXml, Is.EqualTo("Les <b>poissons-perroquets ont des dents robustes qui forment un bec semblable à celui des perroquets, qu’ils utilisent pour racler les algues du corail dur. Certaines espèces ne se gênent pas pour manger des morceaux de corail également, et elles défèquent ensuite un sable fin qui finit sur la terre pour former de magnifiques plages de sable blanc.</b>"));
			fixedXml = ConvertFromEpub.FixInnerXml("<b>Les poissons-clowns et les <b>anémones de mer vivent ensemble et s’entraident. Les poissons-clowns aident les anémones en nettoyant leurs tentacules et en attirant d’autres poissons pour que l’anémone les mange. Les anémones, à leur tour, permettent aux poissons-clowns de se cacher parmi leurs tentacules venimeux sans les piquer. <br /> </b></b>");
			Assert.That(fixedXml, Is.EqualTo("<b>Les poissons-clowns et les <b>anémones de mer vivent ensemble et s’entraident. Les poissons-clowns aident les anémones en nettoyant leurs tentacules et en attirant d’autres poissons pour que l’anémone les mange. Les anémones, à leur tour, permettent aux poissons-clowns de se cacher parmi leurs tentacules venimeux sans les piquer.</b></b>"));
		}

		[Test]
		public void TestRemovePrathamCreditBoilerplate()
		{
			// SUT - "À l’intérieur du World Wide Web"
			var credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("This book was first published on StoryWeaver, Pratham Books. The development of this book has been supported by Oracle Giving Initiative.", "en");
			Assert.That(credits, Is.EqualTo("The development of this book has been supported by Oracle Giving Initiative."));

			// SUT - "Aller acheter un livre"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("This book has been published on StoryWeaver by Pratham Books. The development of the print version of this book was supported by Dubai Creek Round Table, Dubai .U.A.E. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org", "en");
			Assert.That(credits, Is.EqualTo("The development of the print version of this book was supported by Dubai Creek Round Table, Dubai .U.A.E."));

			// SUT - "M. Anand a une aventure"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("'Mr. Anand has an Adventure' has been published on StoryWeaver by Pratham Books. It was created for #6FrameStoryChallenge, an illustration campaign organized by Pratham Books. www.prathambooks.org", "en");
			Assert.That(credits, Is.EqualTo("This book was created for #6FrameStoryChallenge, an illustration campaign organized by Pratham Books."));

			// SUT - "Trop de bananes"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("This book has been published on StoryWeaver by Pratham Books. The author of this book, Rohini Nilekani used to earlier write under the pseudonym 'Noni'. The print version of 'Too Many Bananas' has been published by Pratham Books with the support by Nikki Gulati. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org", "en");
			Assert.That(credits, Is.EqualTo("The author of this book, Rohini Nilekani used to earlier write under the pseudonym 'Noni'. The print version of 'Too Many Bananas' has been published by Pratham Books with the support by Nikki Gulati."));

			// SUT - "Le corbeau généreux"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("This book has been published on StoryWeaver by Pratham Books. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org", "en");
			Assert.That(credits, Is.EqualTo(""));

			// SUT - "ससा आणि कासव"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("This book has been published on StoryWeaver by Pratham Books.'The Hare and The Tortoise (Again!)' has been published by Pratham Books in partnership with the Rajiv Gandhi Foundation. Pratham Books is a not-for-profit organization that publishes books in multiple Indian languages to promote reading among children. www.prathambooks.org", "en");
			Assert.That(credits, Is.EqualTo("'The Hare and The Tortoise (Again!)' has been published by Pratham Books in partnership with the Rajiv Gandhi Foundation."));

			// SUT - "Fourmis affairées"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("Ce livre a été publié sur StoryWeaver par Pratham Books. Le développement de la version imprimée de ce livre a reçu le soutien de HDFC Asset Management Company Limited (une coentreprise avec Standard Life Investments). www.prathambooks.org ", "fr");
			Assert.That(credits, Is.EqualTo("Le développement de la version imprimée de ce livre a reçu le soutien de HDFC Asset Management Company Limited (une coentreprise avec Standard Life Investments)."));

			// SUT - "Les petits peintres"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("Ce livre a été publié sur StoryWeaver par Pratham Books. Pratham Books est un organisme à but non lucratif qui publie des livres dans plusieurs langues indiennes afin de promouvoir la lecture chez les enfants. www.prathambooks.org ", "fr");
			Assert.That(credits, Is.EqualTo(""));

			// SUT - "Se brosser n’est pas amusant !"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("«&#xa0;Se brosser n’est pas amusant&#xa0;!&#xa0;» a été publié sur StoryWeaver par Pratham Books. Le développement de ce livre a été soutenu par Fortis Charitable Foundation. www.prathambooks.org ", "fr");
			Assert.That(credits, Is.EqualTo("Le développement de ce livre a été soutenu par Fortis Charitable Foundation."));

			// SUT - "Voler haut"
			credits = ConvertFromEpub.RemovePrathamCreditBoilerplate("Ce livre a été publié sur StoryWeaver par Pratham Books. Le développement de la version imprimée de ce livre a été soutenu par Dubai Creek Round Table, Dubaï, EAU Pratham Books Pratham Books est une organisation à but non lucratif qui publie des livres dans plusieurs langues indiennes afin de promouvoir la lecture chez les enfants. www.prathambooks.org ", "fr");
			Assert.That(credits, Is.EqualTo("Le développement de la version imprimée de ce livre a été soutenu par Dubai Creek Round Table, Dubaï, EAU Pratham Books"));
		}

		[Test]
		public void TestExtractInfoFromPrathamStoryAttribution()
		{
			string author, copyright, license, originalAck;
			// SUT - "Trop de bananes"
			var okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Story Attribution: This story: Too Many Bananas is written by Rohini Nilekani. © Pratham Books, 2010. Some rights reserved. Released under CC BY 4.0 license.",
				"en", out author, out copyright, out license, out originalAck);
			Assert.That(okay, Is.True, "Extracting from story attribution for \"Trop de bananes\" succeeded");
			Assert.That(author, Is.EqualTo("Rohini Nilekani"));
			Assert.That(copyright, Is.EqualTo("© Pratham Books, 2010"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.Null, "\"Trop de bananes\" is not a translation!");

			// SUT - "Le corbeau généreux"
			okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Story Attribution: This story: The Generous Crow is translated by Divaspathy Hegde. The © for this translation lies with Pratham Books, 2004. Some rights reserved. Released under CC BY 4.0 license. Based on Original story: ' ಕಾಗೆ ಬಳಗವ ಕರೆಯಿತು ', by Venkatramana Gowda. © Pratham Books, 2004. Some rights reserved. Released under CC BY 4.0 license.",
				"en", out author, out copyright, out license, out originalAck);
			Assert.That(okay, Is.True, "Extracting from story attribution for \"Le corbeau généreux\" succeeded");
			Assert.That(author, Is.EqualTo("Divaspathy Hegde"));
			Assert.That(copyright, Is.EqualTo("© Pratham Books, 2004"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.EqualTo("Based on Original story: ' ಕಾಗೆ ಬಳಗವ ಕರೆಯಿತು ', by Venkatramana Gowda. © Pratham Books, 2004. Some rights reserved. Released under CC BY 4.0 license."));

			// SUT - "Fourmis affairées"
			okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Attribution de l’histoire : Cette histoire, « Les fourmis affairées » est écrite par Kanchan Bannerjee. © Pratham Books, 2015. Certains droits réservés. Publié sous licence CC BY 4.0.",
				"fr", out author, out copyright, out license, out originalAck);

			Assert.That(okay, Is.True, "Extracting from story attribution for \"Fourmis affairées\" succeeded");
			Assert.That(author, Is.EqualTo("Kanchan Bannerjee"));
			Assert.That(copyright, Is.EqualTo("© Pratham Books, 2015"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.Null, "\"Fourmis affairées\" is not a translation!");

			// SUT - "Voler haut"
			okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Attribution de l’histoire : Cette histoire, « Voler haut » est traduite par Rohini Nilekani. Le © de cette traduction appartient à Pratham Books, 2004. Certains droits réservés. Publié sous licence CC BY 4.0. Basée sur l’histoire originale : «  तरंगत तरंगत  », de Vidya Tiware. © Pratham Books, 2004. Certains droits réservés. Publié sous licence CC BY 4.0.",
				"fr", out author, out copyright, out license, out originalAck);
			Assert.That(okay, Is.True, "Extracting from story attribution for \"Voler haut\" succeeded");
			Assert.That(author, Is.EqualTo("Rohini Nilekani"));
			Assert.That(copyright, Is.EqualTo("© Pratham Books, 2004"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.EqualTo("Basée sur l’histoire originale : «  तरंगत तरंगत  », de Vidya Tiware. © Pratham Books, 2004. Certains droits réservés. Publié sous licence CC BY 4.0."));

			// SUT - "L’heure du bain pour Chunnu et Munnu"
			okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Attribution de l’histoire\u00a0: Cette histoire, «\u00a0L’heure du bain pour Chunnu et Munnu\u00a0» est traduite par Rohini Nilekani. Le © de cette traduction appartient à Pratham Books, 2015. Certains droits réservés. Publié sous licence CC\u00a0BY\u00a04.0. Inspiré de l’histoire originale\u00a0: «\u00a0 चुन्नु-मुन्नु का नहाना \u00a0», de Rohini Nilekani. © Storyweaver, Pratham Books, 2015. Certains droits réservés. Publié sous licence CC\u00a0BY\u00a04.0.",
				"fr", out author, out copyright, out license, out originalAck);
			Assert.That(okay, Is.True, "Extracting from story attribution for \"Voler haut\" succeeded");
			Assert.That(author, Is.EqualTo("Rohini Nilekani"));
			Assert.That(copyright, Is.EqualTo("© Pratham Books, 2015"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.EqualTo("Inspiré de l’histoire originale\u00a0: «\u00a0 चुन्नु-मुन्नु का नहाना \u00a0», de Rohini Nilekani. © Storyweaver, Pratham Books, 2015. Certains droits réservés. Publié sous licence CC\u00a0BY\u00a04.0."));

			// SUT - "Dogs versus Cats" (this mess really is verbatim from the epub)
			okay = ConvertFromEpub.ExtractInfoFromPrathamStoryAttribution("Story Attribution: This story:Dogs versus Catsis translated byAlisha Berger. The © for this translation lies with Room to Read, 2013. Some rights reserved. Released under CC BY 4.0 license.Based on Original story: 'Akwatiwa lokwacabanisa inja nelikati', byNomkhosi Cynthia Thabethe. © Room to Read, 2013. Some rights reserved. Released under CC BY 4.0 license.",
				"en", out author, out copyright, out license, out originalAck);
			Assert.That(okay, Is.True, "Extracting from story attribution for \"Dogs versus Cats\" succeeded");
			Assert.That(author, Is.EqualTo("Alisha Berger"));
			Assert.That(copyright, Is.EqualTo("© Room to Read, 2013"));
			Assert.That(license, Is.EqualTo("CC BY 4.0"));
			Assert.That(originalAck, Is.EqualTo("Based on Original story: 'Akwatiwa lokwacabanisa inja nelikati', byNomkhosi Cynthia Thabethe. © Room to Read, 2013. Some rights reserved. Released under CC BY 4.0 license."));
		}
	}
}
