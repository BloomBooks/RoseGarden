// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using Newtonsoft.Json;

namespace RoseGarden.Parse
{
	[JsonObject]
	class ParseResponse<T>
	{
#pragma warning disable 649
		[JsonProperty("count")]
		internal int Count;

		[JsonProperty("results")]
		internal T[] Results;
#pragma warning restore 649
	}
}
