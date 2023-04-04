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
		public Dictionary<string, ClipBounds> FileClipBounds = new Dictionary<string, ClipBounds>();

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
					if (SmilPars.TryGetValue(par.TextLink, out SmilPar prevPar))
					{
						// Smil data can sometimes split a segment into two contiguous pieces.
						// Whether or not this is according to spec, it's been known to happen.
						// Fuse those two contiguous pieces into one piece.
						if (prevPar.Equals(par))
							continue;
						if (prevPar.AudioClipEnd == par.AudioClipStart)
						{
							prevPar.AudioClipEnd = par.AudioClipEnd;
							par.AudioClipStart = prevPar.AudioClipStart;
						}
						else if (prevPar.AudioClipStart == par.AudioClipEnd)
						{
							prevPar.AudioClipStart = par.AudioClipStart;
							par.AudioClipEnd = prevPar.AudioClipEnd;
						}
						else
						{
							throw new Exception($"Unexpected smil {parNode.OuterXml} in {smilFilePath}");
						}
					}
					else
					{
						SmilPars.Add(par.TextLink, par);
					}
					if (!String.IsNullOrEmpty(par.AudioFileName) &&
						!String.IsNullOrEmpty(par.AudioClipStart) && Double.TryParse(par.AudioClipStart, out double start) &&
						!String.IsNullOrEmpty(par.AudioClipEnd) && Double.TryParse(par.AudioClipStart, out double end))
					{
						if (FileClipBounds.TryGetValue(par.AudioFileName, out var bounds))
						{
							if (start < bounds.Start)
							{
								bounds.Start = start;
								bounds.InitialClipStart = par.AudioClipStart;
							}
							if (end > bounds.End)
							{
								bounds.End = end;
								bounds.FinalClipEnd = par.AudioClipEnd;
							}
						}
						else
						{
							FileClipBounds[par.AudioFileName] =
								new ClipBounds {InitialClipStart=par.AudioClipStart, FinalClipEnd=par.AudioClipEnd, Start=start, End=end};
						}

					}
				}
			}
		}
	}

	public class ClipBounds
	{
		public string InitialClipStart;
		public string FinalClipEnd;
		public double Start;
		public double End;
	}

	public class SmilPar
	{
		public string TextLink;
		public string AudioFileName;
		public string AudioClipStart;
		public string AudioClipEnd;

		public override bool Equals(object obj)
		{
			var that = obj as SmilPar;
			if (obj == null)
				return false;
			return this.TextLink == that.TextLink &&
				this.AudioFileName == that.AudioFileName &&
				this.AudioClipStart == that.AudioClipStart &&
				this.AudioClipEnd == that.AudioClipEnd;
		}

		public override int GetHashCode()
		{
			var hash1 = this.TextLink != null ? this.TextLink.GetHashCode() : 1;
			var hash2 = this.AudioFileName != null ? this.AudioFileName.GetHashCode() : 2;
			var hash3 = this.AudioClipStart != null ? this.AudioClipStart.GetHashCode() : -1;
			var hash4 = this.AudioClipEnd != null ? this.AudioClipEnd.GetHashCode() : -2;
			return (hash1 + hash2) ^ (hash3 + hash4);
		}
	}
}
