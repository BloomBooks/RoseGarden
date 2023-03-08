using SIL.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RoseGarden
{
	/// <summary>
	/// smil is an acronym for Synchronized Multimedia Integration Language.
	/// I don't propose to use the full name for this class!
	/// This class contains the data from one smil file in the epub.
	/// </summary>
	public class SmilFileData
	{
		XmlDocument _smilDocument;
		XmlNamespaceManager _opsNsmgr;
		public Dictionary<string, SmilPar> SmilPars = new Dictionary<string,SmilPar>();

		public SmilFileData(string smilFilePath)
		{
			var smilXml = File.ReadAllText(smilFilePath);
			_smilDocument = new XmlDocument();
			_smilDocument.PreserveWhitespace = true;
			_smilDocument.LoadXml(smilXml);
			_opsNsmgr = new XmlNamespaceManager(_smilDocument.NameTable);
			_opsNsmgr.AddNamespace("epub", "http://www.idpf.org/2007/ops");
			var parNodes = _smilDocument.SafeSelectNodes("/smil/body/par").Cast<XmlElement>();
			foreach (var parNode in parNodes)
			{
				var textNode = parNode.SafeSelectNodes("text")[0] as XmlElement;
				var audioNode = parNode.SafeSelectNodes("audio")[0] as XmlElement;
				if (textNode != null && audioNode != null)
				{
					var par = new SmilPar();
					par.TextLink = Path.GetFileName(textNode.GetAttribute("src"));
					par.AudioFileName = Path.GetFileName(audioNode.GetAttribute("src"));
					par.AudioClipStart = audioNode.GetOptionalStringAttribute("clipBegin", null);
					par.AudioClipEnd = audioNode.GetOptionalStringAttribute("clipEnd", null);
					SmilPars.Add(par.TextLink, par);
				}
			}
		}
	}

	public struct SmilPar
	{
		public string TextLink;
		public string AudioFileName;
		public string AudioClipStart;
		public string AudioClipEnd;
	}
}
