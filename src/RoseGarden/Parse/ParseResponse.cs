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
