// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoseGarden.Parse.Model
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class RelatedBooks : ParseObject
	{
		[JsonProperty("books")]
		public List<Book> Books;

		public override string GetParseClassName()
		{
			return "relatedBooks";
		}
	}
}
