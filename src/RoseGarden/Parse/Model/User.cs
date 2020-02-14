// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using Newtonsoft.Json;

namespace RoseGarden.Parse.Model
{
	/// <summary>
	/// This class represents the user information we may need that is stored for the person who
	/// uploaded each book.
	/// </summary>
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class User : ParseObject
	{
		/// <summary>
		/// The UserName is probably an email address but not necessarily a valid email address.
		/// This is used only to double-check that the standard importer id uploaded the book.
		/// </summary>
		/// <remarks>
		/// We may decide we don't need this functionality.  For user information, it's better
		/// to utilize as little as needed.
		/// </remarks>
		[JsonProperty("username")]
		public string UserName;

		public override string GetParseClassName()
		{
			return "User";
		}
	}
}
