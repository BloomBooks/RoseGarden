using System;
using System.Collections.Generic;
using SIL.WritingSystems;

namespace RoseGarden
{
	public class Language
	{
		Dictionary<string, string> _nameToCode = new Dictionary<string, string>();
		LanguageLookup _languageLookup;	// throws FileNotFound / invalid image exception

		public Language()
		{
			Sldr.Initialize();
			_languageLookup = new LanguageLookup();
			// These are from the GDL catalog of January 2020.
			_nameToCode.Add("አማርኛ", "am");
			_nameToCode.Add("Arabic", "ar");
			_nameToCode.Add("Awadhi", "awa");
			_nameToCode.Add("Bhojpuri", "bho");
			_nameToCode.Add("বাঙালি", "bn");
			_nameToCode.Add("Bukusu", "luy");	// bxk in GDL
			_nameToCode.Add("Cebuano", "ceb");
			_nameToCode.Add("Dotyali", "dty");
			_nameToCode.Add("English", "en");
			_nameToCode.Add("Spanish (Spain)", "es-ES");
			_nameToCode.Add("French", "fr");
			_nameToCode.Add("Gusii", "guz");
			_nameToCode.Add("Hausa (Nigeria)", "ha-NG");
			_nameToCode.Add("Hadiyya", "hdy");
			_nameToCode.Add("हिंदी", "hi");
			_nameToCode.Add("bahasa Indonesia", "id");
			_nameToCode.Add("Kalanguya", "kak");
			_nameToCode.Add("Kamba (Kenya)", "kam");
			_nameToCode.Add("ភាសាខ្មែរ", "km");
			_nameToCode.Add("Lingala", "ln");
			_nameToCode.Add("Wanga", "lwg");
			_nameToCode.Add("Maithili", "mai");
			_nameToCode.Add("मराठी", "mr");
			_nameToCode.Add("नेपाली (Nepal)", "ne-NP");
			_nameToCode.Add("Newari", "new");
			_nameToCode.Add("isiNdebele seSewula", "nr");
			_nameToCode.Add("Pedi", "nso");
			_nameToCode.Add("Afaan Oromoo", "om");
			_nameToCode.Add("Portuguese (Brazil)", "pt-BR");
			_nameToCode.Add("Kinyarwanda", "rw");
			_nameToCode.Add("Sidamo", "sid");
			_nameToCode.Add("Shona", "sn");
			_nameToCode.Add("Somali (Ethiopia)", "so-ET");
			_nameToCode.Add("siSwati", "ss");
			_nameToCode.Add("Sesotho", "st");
			_nameToCode.Add("Kiswahili (Kenya)", "sw-KE");
			_nameToCode.Add("Dangaura Tharu", "thl");
			_nameToCode.Add("Tigrinya (Ethiopia)", "ti-ET");
			_nameToCode.Add("Setswana", "tn");
			_nameToCode.Add("Tshivenḓa", "ve");
			_nameToCode.Add("Wolaytta", "wal");
			_nameToCode.Add("isiZulu", "zu");
			// These are the best I could figure out from the StoryWeaver catalog of January 2020.
			_nameToCode.Add("Acholi", "ach");
			_nameToCode.Add("Afaan Oromo", "om");   // gax hae
			_nameToCode.Add("Afrikaans", "af");
			_nameToCode.Add("Akuapem Twi", "ak-x-Akuapem");
			_nameToCode.Add("Amharic", "am");
			_nameToCode.Add("Aringati", "luc");
			_nameToCode.Add("Asante Twi", "ak-x-Asante");
			_nameToCode.Add("Assamese", "as");
			_nameToCode.Add("Ateso", "teo");
			_nameToCode.Add("Bahasa Indonesia", "id");
			_nameToCode.Add("Balinese", "ban");
			_nameToCode.Add("Bangla (Bangladesh)", "bn-BD");
			_nameToCode.Add("Basa Jawa", "jv");
			_nameToCode.Add("Bengali", "bn");
			_nameToCode.Add("Bengali-Santali (Bengali Script)", "sat-Beng");
			_nameToCode.Add("Bhoti", "sbu");    // ??
			_nameToCode.Add("Chinese (Simplified)", "zh-CN");
			_nameToCode.Add("Chinese (Traditional)", "zh-TW");
			_nameToCode.Add("Chinyanja", "ny");
			_nameToCode.Add("ChiShona", "sn");
			_nameToCode.Add("Chitonga", "tog");
			_nameToCode.Add("Dagaare", "dga");  // dgi
			_nameToCode.Add("Dagbani", "dag");
			_nameToCode.Add("Dangme", "ada");
			_nameToCode.Add("Dhopadhola", "adh");
			_nameToCode.Add("Ekegusii", "guz");
			//_nameToCode.Add("English", "en"); - already have
			_nameToCode.Add("Ewe", "ee");
			_nameToCode.Add("Fante", "ak-x-Fante");
			_nameToCode.Add("Farsi (Samim)", "prs-x-Samim");
			_nameToCode.Add("Filipino", "fil");
			//_nameToCode.Add("French", "fr"); - already have
			_nameToCode.Add("Fulfulde Mbororoore", "fuv");
			_nameToCode.Add("Ga", "gaa");
			_nameToCode.Add("Gondi", "gon");
			_nameToCode.Add("Gonja", "gjn");
			_nameToCode.Add("Gujarati", "gu");
			_nameToCode.Add("Gurene", "gur");
			_nameToCode.Add("Hausa", "ha");
			_nameToCode.Add("Hindi", "hi");
			_nameToCode.Add("IciBemba", "bem");
			_nameToCode.Add("Igbo", "ig");
			_nameToCode.Add("isiNdebele", "nd");    // nr
			_nameToCode.Add("isiXhosa", "xh");
			//_nameToCode.Add("isiZulu", "zu"); - already have
			_nameToCode.Add("Kakwa", "keo");
			_nameToCode.Add("Kannada", "kn");
			_nameToCode.Add("Kasem", "xsm");
			_nameToCode.Add("Khmer", "km");
			_nameToCode.Add("Khoekhoegowab", "naq");
			_nameToCode.Add("K'iche", "quc");
			_nameToCode.Add("Kidawida", "dav");
			_nameToCode.Add("Kikamba", "kam");
			_nameToCode.Add("Kikuyu", "ki");
			//_nameToCode.Add("Kinyarwanda", "rw"); - already have
			_nameToCode.Add("Kiswahili", "sw");
			_nameToCode.Add("Konkani", "knn");
			_nameToCode.Add("Kora (Bengali Script)", "cdz-Beng");
			_nameToCode.Add("Korku", "kfq");
			_nameToCode.Add("Kumam", "kdi");
			_nameToCode.Add("Kurmali", "kyw");
			_nameToCode.Add("Kurukh", "kru");
			_nameToCode.Add("Lámnsoʼ", "lns");
			_nameToCode.Add("Lao", "lo");
			_nameToCode.Add("Likpakpaanl", "xon");
			//_nameToCode.Add("Lingala", "ln"); - already have
			_nameToCode.Add("Lubukusu", "luy");
			_nameToCode.Add("Luganda", "lg");
			_nameToCode.Add("Lugbarati", "lgg");
			_nameToCode.Add("Lugwere", "gwr");
			_nameToCode.Add("Lumasaaba", "myx");
			_nameToCode.Add("Lunyole", "nuj");  // nyd
			_nameToCode.Add("Lusoga", "xog");
			_nameToCode.Add("Maa", "cma");  // mas mev
			_nameToCode.Add("Malayalam", "ml");
			_nameToCode.Add("Mampruli", "maw");
			_nameToCode.Add("Marathi", "mr");
			_nameToCode.Add("Marwari", "mwr");  // mve rwr
			_nameToCode.Add("Minangkabau", "min");
			_nameToCode.Add("Mundari", "unr");  // mqu
			_nameToCode.Add("Nepali", "ne");
			_nameToCode.Add("Ng’aturkana", "tuv");
			_nameToCode.Add("Nzema", "nzi");
			_nameToCode.Add("Odia", "or");  // (macrolanguage)
			_nameToCode.Add("Olukhayo", "lko");
			_nameToCode.Add("Oluwanga", "lwg");
			_nameToCode.Add("Oshikwanyama", "kj");
			_nameToCode.Add("Oshindonga", "ng");
			_nameToCode.Add("Otjiherero", "hz");
			_nameToCode.Add("Pashto", "ps");    // pst pbu
			_nameToCode.Add("Portuguese", "pt");
			_nameToCode.Add("Punjabi", "pa");
			_nameToCode.Add("Rufumbira", "rw");
			_nameToCode.Add("Rukwangali", "kwn");
			_nameToCode.Add("Runyankore", "nyn");
			_nameToCode.Add("Rutooro", "ttj");
			_nameToCode.Add("Sadri", "sck");
			_nameToCode.Add("Santali (Bengali Script)", "sat-Beng");
			_nameToCode.Add("Sepedi", "nso");
			//_nameToCode.Add("Sesotho", "st"); - already have
			//_nameToCode.Add("Setswana", "tn"); - already have
			_nameToCode.Add("S'gaw Karen (Latin Script)", "ksw-Latn");
			_nameToCode.Add("SiLozi", "loz");
			_nameToCode.Add("Sinhala", "si");
			_nameToCode.Add("Sisali", "sld");   // ?? Sisaali / Sissala
			_nameToCode.Add("Siswati", "ss");
			_nameToCode.Add("Surjapuri", "sjp");
			_nameToCode.Add("Swedish", "sv");
			_nameToCode.Add("Tamil", "ta");
			_nameToCode.Add("Telugu", "te");
			_nameToCode.Add("Thai", "th");
			_nameToCode.Add("Tibetan", "bo");
			_nameToCode.Add("Tigrigna", "ti");
			_nameToCode.Add("Tiv", "tiv");
			//_nameToCode.Add("Tshivenḓa", "ve"); - already have
			_nameToCode.Add("Urdu", "ur");
			_nameToCode.Add("Vietnamese", "vi");
			_nameToCode.Add("Xitsonga", "ts");
			_nameToCode.Add("Yoruba", "yo");
			// These appear to be diglots: from what I've seen, the internal HTML doesn't distinguish language so these aren't useful.
			//_nameToCode.Add("English-Afrikaans", "xxx");
			//_nameToCode.Add("English-Bengali", "xxx");
			//_nameToCode.Add("English-Farsi (Samim)", "xxx");
			//_nameToCode.Add("English-Gujarati", "xxx");
			//_nameToCode.Add("English-Hindi", "xxx");
			//_nameToCode.Add("English-Kannada", "xxx");
			//_nameToCode.Add("English-Kutchi (Gujarati Script)", "xxx");
			//_nameToCode.Add("English-Marathi", "xxx");
			//_nameToCode.Add("English-Odia", "xxx");
			//_nameToCode.Add("English-Sepedi", "xxx");
			//_nameToCode.Add("English-Sesotho", "xxx");
			//_nameToCode.Add("English-Setswana", "xxx");
			//_nameToCode.Add("English-Siswati", "xxx");
			//_nameToCode.Add("English-Tamil", "xxx");
			//_nameToCode.Add("English-Telugu", "xxx");
			//_nameToCode.Add("English-Tshivenḓa", "xxx");
			//_nameToCode.Add("English-Tulu", "xxx");
			//_nameToCode.Add("English-Urdu", "xxx");
			//_nameToCode.Add("English-Xitsonga", "xxx");
			//_nameToCode.Add("English-isiNdebele", "xxx");
			//_nameToCode.Add("English-isiXhosa", "xxx");
			//_nameToCode.Add("English-isiZulu", "xxx");
			//_nameToCode.Add("Hindi-Kurmali", "xxx");
			//_nameToCode.Add("Hindi-Kurukh", "xxx");
			//_nameToCode.Add("Hindi-Mundari", "xxx");
			//_nameToCode.Add("Hindi-Sadri", "xxx");
			//_nameToCode.Add("Hindi-Surjapuri", "xxx");
			//_nameToCode.Add("Juanga-Odia", "jun-Orya");
			//_nameToCode.Add("Kora (Bengali Script)-Bengali", "xxx");
			//_nameToCode.Add("Kui-Odia", "kxu-Orya");
			//_nameToCode.Add("Munda-Odia", "unr-Orya");
			//_nameToCode.Add("Runyoro / Runyakitara", "xxx");
			//_nameToCode.Add("Santali-Bengali (Bengali Script)", "xxx");
			//_nameToCode.Add("Saura-Odia", "srb-Orya");
		}

		public string GetCodeForName(string name)
		{
			string code;
			if (_nameToCode.TryGetValue(name, out code))
				return code;
			Console.WriteLine("INFO: try to obtain language code from SLDR information for {0}.", name);
			code = "";
			foreach (var language in _languageLookup.SuggestLanguages(name))
			{
				if (String.IsNullOrEmpty(code))
					code = language.LanguageTag;
				else if (language.LanguageTag.Length < code.Length)
					code = language.LanguageTag;
			}
			if (String.IsNullOrEmpty(code))
			{
				Console.WriteLine("INFO: could not find language code for {0}", name);
				return "qaa";
			}
			return code;
		}
	}
}
