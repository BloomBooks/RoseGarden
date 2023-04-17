using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RoseGarden;

namespace RoseGardenTests
{
	[TestFixture]
	public class EpubMetadataTests
	{
		[Test]
		public void CheckSortPagesToSame()
		{
			var opfXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<package xmlns=""http://www.idpf.org/2007/opf"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" unique-identifier=""bookid"" version=""3.0"" prefix=""rendition: http://www.idpf.org/vocab/rendition/# ibooks: http://vocabulary.itunes.apple.com/rdf/ibooks/vocabulary-extensions-1.0/"">
    <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
        <dc:date>2022</dc:date>
        <dc:language>rw</dc:language>
        <dc:title>Amazi_Soma</dc:title>
        <dc:creator>MUKANYANDWI Béatrice</dc:creator>
        <dc:source>Amazi_Soma, Espace Littéraire Soma Ltd</dc:source>
        <dc:publisher>Espace Littéraire Soma Ltd</dc:publisher>
        <dc:identifier id=""bookid"">urn:uuid:3B07DC19-F28B-477D-89E5-940C5A2CB3EB</dc:identifier>
        <meta property=""dcterms:modified"">2022-06-09T15:38:46Z</meta>
    </metadata>
    <manifest>
        <item id=""xi"" href=""i.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""xii"" href=""ii.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""xiii"" href=""iii.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""xiv"" href=""iv.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x001"" href=""1.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a1""/>
        <item id=""x002"" href=""2.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a2""/>
        <item id=""x003"" href=""3.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a3""/>
        <item id=""x004"" href=""4.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a4""/>
        <item id=""x005"" href=""5.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a5""/>
        <item id=""x006"" href=""6.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a6""/>
        <item id=""x007"" href=""7.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a7""/>
        <item id=""x008"" href=""8.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a8""/>
        <item id=""x009"" href=""9.xhtml"" media-type=""application/xhtml+xml""   media-overlay=""a9""/>
        <item id=""x010"" href=""10.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x011"" href=""11.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x012"" href=""12.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x013"" href=""13.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x014"" href=""14.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x015"" href=""15.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x016"" href=""16.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x017"" href=""17.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x018"" href=""18.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x019"" href=""19.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x020"" href=""20.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x021"" href=""21.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x022"" href=""22.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x023"" href=""23.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x024"" href=""24.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x025"" href=""25.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x026"" href=""26.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x027"" href=""27.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x028"" href=""28.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""x029"" href=""29.xhtml"" media-type=""application/xhtml+xml"" />
        <item id=""toc"" href=""toc.xhtml"" media-type=""application/xhtml+xml"" properties=""nav"" />
        <item id=""idGeneratedStyles.css"" href=""css/idGeneratedStyles.css"" media-type=""text/css"" />
        <item id=""OpenDyslexicThree-Regular.ttf"" href=""font/OpenDyslexicThree-Regular.ttf"" media-type=""font/ttf"" />
        <item id=""x1.png"" href=""image/1.png"" media-type=""image/png"" />
    </manifest>
</package>
";
			var meta = new EpubMetadata("C:/work/epub", "C:/work/epub/x/OEBPS/content.opf", opfXml);
            Assert.That(meta != null);
            Assert.That(meta.PageFiles.Count == 33);
			Assert.That(meta.PageFiles[0].EndsWith("\\i.xhtml"));
			Assert.That(meta.PageFiles[1].EndsWith("\\ii.xhtml"));
			Assert.That(meta.PageFiles[2].EndsWith("\\iii.xhtml"));
			Assert.That(meta.PageFiles[3].EndsWith("\\iv.xhtml"));
			Assert.That(meta.PageFiles[4].EndsWith("\\1.xhtml"));
			Assert.That(meta.PageFiles[5].EndsWith("\\2.xhtml"));
			Assert.That(meta.PageFiles[31].EndsWith("\\28.xhtml"));
			Assert.That(meta.PageFiles[32].EndsWith("\\29.xhtml"));
		}

        [Test]
        public void CheckSortOutOfOrderPages()
        {
            var opfXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package version=""3.0"" unique-identifier=""uuid_id"" prefix=""ibooks: http://vocabulary.itunes.apple.com/rdf/ibooks/vocabulary-extensions-1.0/"" xmlns=""http://www.idpf.org/2007/opf"">
  <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:dcterms=""http://purl.org/dc/terms/"" xmlns:opf=""http://www.idpf.org/2007/opf"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <dc:title>Kariza akunda kubaza</dc:title>
    <dc:creator id=""creator01"">Jean de Dieu Bavugempore</dc:creator>
    <meta property=""dcterms:modified"">2021-08-06T09:57:59Z</meta>
    <dc:date>2017</dc:date>
    <dc:publisher id=""en_publisher"" xml:lang=""en-us"">African Storybook</dc:publisher>
    <dc:language>rw</dc:language>
    <dc:identifier id=""uuid_id"">N/A</dc:identifier>
    <dc:source>Title: Kariza akunda kubaza, Publisher: African Storybook, Author: Jean de Dieu Bavugempore, Contributor: Rob Owen, Year: 2017, Africa</dc:source>
  </metadata>
  <manifest>
    <item id=""font00"" href=""Fonts/OpenDyslexic3-Regular.ttf"" media-type=""application/vnd.ms-opentype""/>
    <item id=""stylesheet.css"" href=""Styles/stylesheet.css"" media-type=""text/css""/>
    <item id=""item_image_1"" href=""Images/asblogo.png"" media-type=""image/png""/>
    <item id=""front_cover.xhtml"" href=""Text/front_cover.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_1"" href=""Text/1.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_3"" href=""Text/3.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_4"" href=""Text/4.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_5"" href=""Text/5.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_6"" href=""Text/6.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_7"" href=""Text/7.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_8"" href=""Text/8.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_9"" href=""Text/9.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_10"" href=""Text/10.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_11"" href=""Text/11.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""chapter_12"" href=""Text/12.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""back_cover.xhtml"" href=""Text/back_cover.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""ncx"" href=""toc.ncx"" media-type=""application/x-dtbncx+xml""/>
    <item id=""nav"" href=""Text/nav.xhtml"" media-type=""application/xhtml+xml"" properties=""nav""/>
    <item id=""cover-image"" href=""Images/cover.png"" media-type=""image/png"" properties=""cover-image""/>
    <item id=""front_cover.mp4"" href=""Video/front_cover.mp4"" media-type=""video/mp4""/>
    <item id=""page1.mp4"" href=""Video/page1.mp4"" media-type=""video/mp4""/>
    <item id=""page12.mp4"" href=""Video/page12.mp4"" media-type=""video/mp4""/>
    <item id=""x2.xhtml"" href=""Text/2.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""questions2.xhtml"" href=""Text/questions2.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""questions1.xhtml"" href=""Text/questions1.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""questions1.mp4"" href=""Video/questions1.mp4"" media-type=""video/mp4""/>
    <item id=""questions2.mp4"" href=""Video/questions2.mp4"" media-type=""video/mp4""/>
    <item id=""glossary1.xhtml"" href=""Text/glossary1.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""Akabati.mp4"" href=""Video/Akabati.mp4"" media-type=""video/mp4""/>
    <item id=""Burigihe.mp4"" href=""Video/Burigihe.mp4"" media-type=""video/mp4""/>
    <item id=""go-back-icon.jpg"" href=""Images/go-back-icon.jpg"" media-type=""image/jpeg""/>
    <item id=""glossary2.xhtml"" href=""Text/glossary2.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary3.xhtml"" href=""Text/glossary3.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary4.xhtml"" href=""Text/glossary4.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary5.xhtml"" href=""Text/glossary5.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary6.xhtml"" href=""Text/glossary6.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary7.xhtml"" href=""Text/glossary7.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary8.xhtml"" href=""Text/glossary8.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary9.xhtml"" href=""Text/glossary9.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary10.xhtml"" href=""Text/glossary10.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary11.xhtml"" href=""Text/glossary11.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary12.xhtml"" href=""Text/glossary12.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary13.xhtml"" href=""Text/glossary13.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""glossary14.xhtml"" href=""Text/glossary14.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""Gufasha.mp4"" href=""Video/Gufasha.mp4"" media-type=""video/mp4""/>
    <item id=""Zisaneza.mp4"" href=""Video/Zisaneza.mp4"" media-type=""video/mp4""/>
    <item id=""back_cover_attribution.xhtml"" href=""Text/back_cover_attribution.xhtml"" media-type=""application/xhtml+xml""/>
    <item id=""Injangwe_za_Sano.epub"" href=""Misc/Injangwe%20za%20Sano.epub"" media-type=""application/epub+zip""/>
    <item id=""acr_logo.png"" href=""Images/acr_logo.png"" media-type=""image/png""/>
  </manifest>
</package>
";
			var meta = new EpubMetadata("C:/work/epub", "C:/work/epub/x/OEBPS/content.opf", opfXml);
			Assert.That(meta != null);
			Assert.That(meta.PageFiles.Count == 31);
			Assert.That(meta.PageFiles[0].EndsWith("\\Text/front_cover.xhtml"));
			Assert.That(meta.PageFiles[1].EndsWith("\\Text/1.xhtml"));
			Assert.That(meta.PageFiles[2].EndsWith("\\Text/2.xhtml"));
			Assert.That(meta.PageFiles[3].EndsWith("\\Text/3.xhtml"));
			Assert.That(meta.PageFiles[9].EndsWith("\\Text/9.xhtml"));
			Assert.That(meta.PageFiles[10].EndsWith("\\Text/10.xhtml"));
			Assert.That(meta.PageFiles[11].EndsWith("\\Text/11.xhtml"));
			Assert.That(meta.PageFiles[12].EndsWith("\\Text/12.xhtml"));
			Assert.That(meta.PageFiles[13].EndsWith("\\Text/questions1.xhtml"));
			Assert.That(meta.PageFiles[14].EndsWith("\\Text/questions2.xhtml"));
			Assert.That(meta.PageFiles[15].EndsWith("\\Text/glossary1.xhtml"));
			Assert.That(meta.PageFiles[16].EndsWith("\\Text/glossary2.xhtml"));
			Assert.That(meta.PageFiles[17].EndsWith("\\Text/glossary3.xhtml"));
			Assert.That(meta.PageFiles[23].EndsWith("\\Text/glossary9.xhtml"));
			Assert.That(meta.PageFiles[24].EndsWith("\\Text/glossary10.xhtml"));
			Assert.That(meta.PageFiles[28].EndsWith("\\Text/glossary14.xhtml"));
			Assert.That(meta.PageFiles[29].EndsWith("\\Text/back_cover.xhtml"));
			Assert.That(meta.PageFiles[30].EndsWith("\\Text/back_cover_attribution.xhtml"));
		}
	}
}
