using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ExtractAudioFilenames
{
	class Program
	{
		static void Main(string[] args)
		{
			var helps = new HashSet<string>
			{
				"/h", "/help", "/?", "-h", "--help", "-?"
			};
			if (args.Length == 0 || args.Length == 1 && helps.Contains(args[0].ToLowerInvariant()))
			{
				Console.WriteLine("ExtractAudioFilenames bookFolder1 bookFolder2 ...");
				return;
			}
			foreach (var arg in args)
			{
				var folder = arg;
				var htmlFile = Path.Combine(folder, Path.GetFileName(folder)+".htm");
				if (Directory.Exists(folder) && File.Exists(htmlFile))
				{
					var xdoc = new XmlDocument();
					xdoc.PreserveWhitespace = true;
					var htmlContent = File.ReadAllText(htmlFile);
					htmlContent = htmlContent.Replace("&nbsp;", "&#160;");
					xdoc.LoadXml(htmlContent);
					Console.WriteLine("================================");
					Console.WriteLine("{0}", htmlFile);
					Console.WriteLine("========");
					foreach (XmlNode page in xdoc.SelectNodes("//div[contains(@class,'bloom-page')]"))
					{
						var pageNumber = page.Attributes["data-page-number"]?.Value;
						if (String.IsNullOrEmpty(pageNumber))
							pageNumber = page.Attributes["data-xmatter-page"]?.Value;
						var pageNumberPrinted = false;
						foreach (XmlNode div in page.SelectNodes(".//div[@data-audiorecordingmode]"))
						{
							var id = div.Attributes["id"]?.Value;
							var text = div.InnerText.Trim();
							if (!pageNumberPrinted)
							{
								pageNumberPrinted = true;
								Console.WriteLine("Page {0}", pageNumber);
							}
							Console.WriteLine("--------");
							Console.WriteLine("Audio file: {0}.mp3", id);
							Console.WriteLine("Text: {0}", text);
						}
						if (pageNumberPrinted)
							Console.WriteLine("========");
					}
				}
			}
		}
	}
}
