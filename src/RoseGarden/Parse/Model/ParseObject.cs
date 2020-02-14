// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoseGarden.Parse.Model
{
	/// <summary>
	/// This class contains fields common to every object in a Parse class
	/// </summary>
	/// <remarks>
	/// This class is essentially lifted from BloomHarvester.
	/// </remarks>
	[JsonObject]
	public abstract class ParseObject
	{
		[JsonProperty("objectId")]
		public string ObjectId { get; set;  }

		// Returns the class name (like a table name) of the class on the Parse server that this object corresponds to
		public abstract string GetParseClassName();

		/// <summary>
		/// Serialize the current object to JSON
		/// </summary>
		/// <returns>the JSON string representation of this object</returns>
		public virtual string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		/// <summary>
		/// Utility function to convert a list to JSON
		/// </summary>
		/// <param name="list"></param>
		/// <returns>the JSON string representation</returns>
		public static string ToJson(IEnumerable<string> list)
		{
			string json = "[]";
			if (list != null && list.Any())
			{
				json = $"[\"{String.Join("\", \"", list)}\"]";
			}

			return json;
		}
	}
}
