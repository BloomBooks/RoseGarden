// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoseGarden.Parse.Model
{
	/// <summary>
	/// This class represents the data of interest stored for each book in bloomlibrary.org.
	/// </summary>
	/// <remarks>
	/// There are many more properties in the book table that we can add if they are needed.
	/// When adding a property here, you need to also add it to the list of keys selected in
	/// ParseClient.GetBooks().
	/// This code was adapted from BloomHarvester.
	/// </remarks>
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Book : ParseObject
    {
		public const string kImporterNameField = "importerName";
		public const string kImporterMajorVersionField = "importerMajorVersion";
		public const string kImporterMinorVersionField = "importerMinorVersion";
		public const string kImportedBookSourceUrl = "importedBookSourceUrl";

		#region RoseGarden-related properties
		/// <summary>
		/// This is set to "RoseGarden" for books that have been converted and uploaded by RoseGarden.
		/// </summary>
		[JsonProperty(kImporterNameField)]
		public string ImporterName;

		/// <summary>
		/// Represents the major version number of the last RoseGarden instance that uploaded this book.
		/// If the major version changes, then we will redo processing of all imported books.
		/// </summary>
		[JsonProperty(kImporterMajorVersionField)]
		public int ImporterMajorVersion;

		/// <summary>
		/// Represents the minor version number of the last RoseGarden instance that uploaded this book.
		/// If the minor version is updated, then we will redo processing of all imported books.
		/// </summary>
		[JsonProperty(kImporterMinorVersionField)]
		public int ImporterMinorVersion;

		/// <summary>
		/// The URL used to download the book's original ePUB (and possibly image file) from an OPDS server.
		/// This is set only by books uploaded by RoseGarden.  Other uploads may set it to an empty string,
		/// but not to any content.  (This may change if any other importer programs/processes are invented.)
		/// Alternatively, it may point to an online readable version of the book.  In either case, it
		/// should be unique to the book and the source we obtained it from.
		/// </summary>
		[JsonProperty(kImportedBookSourceUrl)]
		public string ImportedBookSourceUrl;
		#endregion


		#region Other properties of the book that RoseGarden cares about
		[JsonProperty("bookInstanceId")]
		public string BookInstanceId;

		[JsonProperty("title")]
		public string Title;

		[JsonProperty("authors")]
		public List<string> Authors;

		[JsonProperty("uploader")]
		public User Uploader;

		[JsonProperty("lastUploaded")]
		public Date LastUploaded;

		[JsonProperty("inCirculation")]
		public bool? InCirculation;

		[JsonProperty("updateSource")]
		public string UpdateSource;

		[JsonProperty("tags")]
		public List<string> Tags;

		[JsonProperty("publisher")]
		public string Publisher;
		#endregion

		// Returns the class name (like a table name) of the class on the Parse server that this object corresponds to
		public override string GetParseClassName()
        {
            return "books";
		}
	}
}
