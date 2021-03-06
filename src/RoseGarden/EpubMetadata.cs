﻿// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
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
		public DateTime Modified;
		public List<string> Authors = new List<string>();
		public List<string> Illustrators = new List<string>();
		public List<string> OtherCreators = new List<string>();
		public List<string> OtherContributors = new List<string>();
		public List<string> PageFiles = new List<string>();
		public List<string> ImageFiles = new List<string>();

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

		internal void InitializeMetadata(string epubFolder, string opfPath, string opfXml)
		{
			var contentFolder = Path.GetFileName(Path.GetDirectoryName(opfPath));
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
			Modified = DateTime.Parse(modifiedItem.InnerText);
			var descriptionItem = _opfDocument.SelectSingleNode("/o:package/o:metadata/dc:description", _opfNsmgr);
			if (descriptionItem != null)
				Description = descriptionItem.InnerText;
			var creatorItems = _opfDocument.SelectNodes("/o:package/o:metadata/dc:creator", _opfNsmgr);
			foreach (var node in creatorItems)
			{
				var creator = node as XmlElement;
				var id = creator.GetAttribute("id");
				var refinementNode = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@refines='#" + id + "' and @property='role' and @scheme='marc:relators']", _opfNsmgr);
				if (refinementNode == null || refinementNode.InnerText == "aut")
					Authors.Add(creator.InnerText);
				else
					OtherCreators.Add(creator.InnerText);
			}
			var contributorItems = _opfDocument.SelectNodes("/o:package/o:metadata/dc:contributor", _opfNsmgr);
			foreach (var node in contributorItems)
			{
				var contributor = node as XmlElement;
				var id = contributor.GetAttribute("id");
				var refinementNode = _opfDocument.SelectSingleNode("/o:package/o:metadata/o:meta[@refines='#" + id + "' and @property='role' and @scheme='marc:relators']", _opfNsmgr);
				if (refinementNode == null || refinementNode.InnerText == "ill")
					Illustrators.Add(contributor.InnerText);
				else
					OtherContributors.Add(contributor.InnerText);
			}
			var chapterItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[@media-type='application/xhtml+xml' and @id!='toc' and @id!='nav']", _opfNsmgr);
			foreach (var node in chapterItems)
			{
				var chapter = node as XmlElement;
				var href = chapter.GetAttribute("href");
				PageFiles.Add(Path.Combine(epubFolder, contentFolder, href));
			}
			var imageItems = _opfDocument.SelectNodes("/o:package/o:manifest/o:item[starts-with(@media-type,'image/')]", _opfNsmgr);
			foreach (var node in imageItems)
			{
				var image = node as XmlElement;
				var href = image.GetAttribute("href");
				ImageFiles.Add(Path.Combine(epubFolder, contentFolder, href));
			}
		}

		private string GetOpfPath(string epubFolder)
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

