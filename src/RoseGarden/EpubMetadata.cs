// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using SIL.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace RoseGarden
{
	public class EpubMetadata
	{
		public string Identifier;
		public string Title;
		public string LanguageCode;
		public string Description;
		public string Source;
		public string Publisher;
		public string BookProducer;
		public string RightsText;
		public DateTime Modified;
		public List<string> Authors = new List<string>();
		public List<string> Illustrators = new List<string>();
		public List<string> OtherCreators = new List<string>();
		public List<string> OtherContributors = new List<string>();
		public List<string> PageFiles = new List<string>();
		public List<string> ImageFiles = new List<string>();
		//public List<string> AudioFiles = new List<string>();
		public List<string> VideoFiles = new List<string>();
		public Dictionary<string,SmilFileData> SmilFiles = new Dictionary<string,SmilFileData>();
		public Dictionary<string,string> MediaOverlays = new Dictionary<string,string>();

		// These are provided in case there's more information to extract... (?)
		public XmlDocument _opfDocument;
		public XmlNamespaceManager _opfNsmgr;

		public EpubMetadata(string epubFolder, bool veryVerbose=false)
		{
			var opfPath = GetOpfPath(epubFolder);
			if (veryVerbose)
				Console.WriteLine("DEBUG: path to OPF file={0}", opfPath);
			if (String.IsNullOrEmpty(opfPath))
			{
				Console.WriteLine("WARNING: Could not read rootfile information from META-INF/container.xml!?");
				return;
			}
			var opfXml = File.ReadAllText(opfPath);
			InitializeMetadata(epubFolder, opfPath, opfXml);
		}

		/// <summary>
		/// Constructor for use by tests that avoids file reading.
		/// </summary>
		internal EpubMetadata(string epubFolder, string opfPath, string opfXml)
		{
			InitializeMetadata(epubFolder, opfPath, opfXml);
		}

		public string EpubContentFolder;

		internal void InitializeMetadata(string epubFolder, string opfPath, string opfXml)
		{
			EpubContentFolder = Path.GetDirectoryName(opfPath);
			_opfDocument = new XmlDocument();
			_opfDocument.PreserveWhitespace = true;
			_opfDocument.LoadXml(opfXml);
			_opfNsmgr = new XmlNamespaceManager(_opfDocument.NameTable);
			_opfNsmgr.AddNamespace("o", "http://www.idpf.org/2007/opf");
			_opfNsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
			var identifierItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:identifier", _opfNsmgr);
			Identifier = identifierItem.InnerText;
			var titleItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:title", _opfNsmgr);
			Title = titleItem.InnerText;
			var langItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:language", _opfNsmgr);
			LanguageCode = langItem.InnerText;
			var modifiedItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@property='dcterms:modified']", _opfNsmgr);
			if (modifiedItem != null)
				Modified = DateTime.Parse(modifiedItem.InnerText);
			else
			{
				var timestamp = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@name='calibre:timestamp']", _opfNsmgr);
				if (timestamp != null)
				{
					var dateTime = timestamp.GetOptionalStringAttribute("content", "");
					if (!String.IsNullOrEmpty(dateTime))
						Modified = DateTime.Parse(dateTime);
				}
			}
			var descriptionItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:description", _opfNsmgr);
			if (descriptionItem != null)
				Description = descriptionItem.InnerText;
			var creatorItems = _opfDocument.SelectNodes("/o:package/o:metadata/dc:creator", _opfNsmgr);
			foreach (var node in creatorItems)
			{
				var creator = node as XmlElement;
				var role = creator.GetOptionalStringAttribute("opf:role", null);
				if (role == "aut")
					Authors.Add(creator.InnerText);
				else if (role == "ill")
					Illustrators.Add(creator.InnerText);
				else if (!String.IsNullOrEmpty(role))
					OtherCreators.Add(creator.InnerText);
				else
				{
					var id = creator.GetAttribute("id");
					var refinementNode = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@refines='#" + id + "' and @property='role' and @scheme='marc:relators']", _opfNsmgr);
					if (refinementNode == null || refinementNode.InnerText == "aut")
						Authors.Add(creator.InnerText);
					else if (refinementNode.InnerText == "ill")
						Illustrators.Add(creator.InnerText);
					else
						OtherCreators.Add(creator.InnerText);
				}
			}
			var contributorItems = _opfDocument.SelectNodes("/o:package/o:metadata/dc:contributor", _opfNsmgr);
			foreach (var node in contributorItems)
			{
				var contributor = node as XmlElement;
				var role = contributor.GetOptionalStringAttribute("opf:role", null);
				if (role == "aut")
					Authors.Add(contributor.InnerText);
				else if (role == "ill")
					Illustrators.Add(contributor.InnerText);
				else if (role == "bkp")
					BookProducer = contributor.InnerText;
				else if (!String.IsNullOrEmpty(role))
					OtherContributors.Add(contributor.InnerText);
				else
				{
					var id = contributor.GetAttribute("id");
					var refinementNode = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@refines='#" + id + "' and @property='role' and @scheme='marc:relators']", _opfNsmgr);
					if (refinementNode == null || refinementNode.InnerText == "ill")
						Illustrators.Add(contributor.InnerText);
					else
						OtherContributors.Add(contributor.InnerText);
				}
			}
			var chapterItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[@media-type='application/xhtml+xml' and @id!='toc' and @id!='nav']", _opfNsmgr);
			foreach (var node in chapterItems)
			{
				var chapter = node as XmlElement;
				var href = chapter.GetAttribute("href");
				PageFiles.Add(Path.Combine(EpubContentFolder, href));
				var overlay = chapter.GetOptionalStringAttribute("media-overlay", null);
				if (!String.IsNullOrEmpty(overlay))
					MediaOverlays.Add(href, overlay);
			}
			var preArray = PageFiles.ToArray();
			PageFiles.Sort(new PageFilenameCompare());
			Debug.Assert(preArray.Length == PageFiles.Count);
			for (int i = 0; i < preArray.Length; ++i)
			{
				if (preArray[i] != PageFiles[i])
				{
					Console.WriteLine("INFO: the ordering of page files had to be fixed.");
					break;
				}
			}
			var imageItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[starts-with(@media-type,'image/')]", _opfNsmgr);
			foreach (var node in imageItems)
			{
				var image = node as XmlElement;
				var href = image.GetAttribute("href");
				ImageFiles.Add(GetSanitizedFilePath(href));
			}
			var videoItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[starts-with(@media-type,'video/')]", _opfNsmgr);
			foreach (var node in videoItems)
			{
				var video = node as XmlElement;
				var href = video.GetAttribute("href");
				VideoFiles.Add(GetSanitizedFilePath(href));
			}
			var smilItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[@media-type='application/smil+xml']", _opfNsmgr);
			foreach (var node in smilItems)
			{
				var smil = node as XmlElement;
				var href = smil.GetAttribute("href");
				SmilFiles.Add(smil.GetAttribute("id"), new SmilFileData(Path.Combine(EpubContentFolder, href)));
			}
			var sourceItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:source", _opfNsmgr);
			if (sourceItem != null)
				Source = sourceItem.InnerText;
			var publisherItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:publisher", _opfNsmgr);
			if (publisherItem != null)
			{
				Publisher = publisherItem.InnerText;
			}
			else if (Source != null && Source.StartsWith(Title))
			{
				var pub = Source.Substring(Title.Length);
				if (pub.StartsWith(",") || pub.StartsWith(":") || pub.StartsWith(";"))
					pub = pub.Substring(1);
				Publisher = pub.Trim();		// possibly better than nothing...
			}
			var rightsItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:rights", _opfNsmgr);
			if (rightsItem != null)
				RightsText = rightsItem.InnerText;
		}
		class PageFilenameCompare : IComparer<string>
		{
			public int Compare(string x, string y)
			{
				var base1 = Path.GetFileNameWithoutExtension(x);
				var base2 = Path.GetFileNameWithoutExtension(y);
				var ok1 = Int32.TryParse(base1, out int num1);
				var ok2 = Int32.TryParse(base2, out int num2);
				if (ok1 && ok2)
					return num1 - num2;
				if (base1.StartsWith("front"))
					num1 = 0;
				else if (base1.StartsWith("back"))
					num1 = 999999;
				else if (base1.StartsWith("questions"))
					num1 = 990000 + NumberFrom(base1.Substring(9));
				else if (base1.StartsWith("glossary"))
					num1 = 999000 + NumberFrom(base1.Substring(8));
				else if (base1.Trim(new char[] {'i','v'}) == "")
					num1 = 0;	// Roman numbers 1-8 sort alphabetically
				if (base2.StartsWith("front"))
					num2 = 0;
				else if (base2.StartsWith("back"))
					num2 = 999999;
				else if (base2.StartsWith("questions"))
					num2 = 990000 + NumberFrom(base2.Substring(9));
				else if (base2.StartsWith("glossary"))
					num2 = 999000 + NumberFrom(base2.Substring(8));
				else if (base2.Trim(new char[] { 'i', 'v' }) == "")
					num2 = 0;   // Roman numbers 1-8 sort alphabetically
				if (num1 == num2)
					return String.Compare(base1, base2, true, System.Globalization.CultureInfo.InvariantCulture);
				return num1 - num2;
			}

			private int NumberFrom(string tail)
			{
				if (String.IsNullOrEmpty(tail))
					return 0;
				if (Int32.TryParse(tail, out int num))
					return num;
				return 0;
			}
		}

		private string GetSanitizedFilePath(string href)
		{
			var path = Path.Combine(EpubContentFolder, href);
			if (!File.Exists(path))
			{
				
				var path1 = System.Net.WebUtility.UrlDecode(path);
				if (File.Exists(path1))
					return path1;
			}
			return path;
		}

		static internal string GetOpfPath(string epubFolder)
		{
			var metaPath = Path.Combine(epubFolder, "META-INF", "container.xml");
			var metaXml = File.ReadAllText(metaPath);
			return GetOpfPath(epubFolder, metaXml);
		}

		static internal string GetOpfPath(string epubFolder, string metaXml)
		{
			var metaInf = new XmlDocument();
			metaInf.LoadXml(metaXml);
			var nsmgr = new XmlNamespaceManager(metaInf.NameTable);
			nsmgr.AddNamespace("u", "urn:oasis:names:tc:opendocument:xmlns:container");
			var node = metaInf.SelectSingleNode("/u:container/u:rootfiles/u:rootfile", nsmgr) as XmlElement;
			if (node != null)
			{
				var relpath = node.GetAttribute("full-path");
				if (Path.DirectorySeparatorChar != '/')		// HTML/XML uses '/' exclusively.
					relpath = relpath.Replace('/', Path.DirectorySeparatorChar);
				if (!String.IsNullOrEmpty(relpath))
					return Path.Combine(epubFolder, relpath);
			}
			return null;
		}
	}
}

