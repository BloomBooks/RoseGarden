// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;
using RoseGarden.Parse;
using RoseGarden.Parse.Model;

namespace RoseGarden
{
	public class FixTable
	{
		private FixTableOptions _options;

		public FixTable(FixTableOptions opts)
		{
			this._options = opts;
		}

		public int RunFix()
		{
			if (!VerifyOptions())
				return 1;
			var client = new ParseClient(_options.UploadUser, _options.UploadPassword);
			string importedFilter = "{\"importedBookSourceUrl\": {\"$regex\": \".\"}}";
			IEnumerable<Book> bookList = client.GetBooks(importedFilter, new[] { "uploader" });

			var bookCount = 0;
			var fixedCount = 0;
			foreach (var book in bookList)
			{
				++bookCount;
				if (_options.VeryVerbose)
					Console.WriteLine("DEBUG: For \"{0}\", tags = [{1}]", book.Title, String.Join(", ", book.Tags).TrimEnd(',', ' '));
				if (book.Tags == null || book.Tags.Count == 0)
					continue;
				bool updateTags = false;
				if (book.Tags.Contains("system:Incoming"))
				{
					updateTags = true;
					book.Tags.Remove("system:Incoming");
				}
				var newTags = new List<string>();
				foreach (var tag in book.Tags)
				{
					if (tag.StartsWith("level:Level ", StringComparison.InvariantCulture))
					{
						var newTag = UploadToBloomLibrary.GetTagForLrmiReadingLevel(tag.Substring(6));
						newTags.Add(newTag);
						updateTags = true;
					}
					else if (tag.StartsWith("level:ደረጃ ", StringComparison.InvariantCulture))
					{
						var newTag = UploadToBloomLibrary.GetTagForLrmiReadingLevel(tag.Substring(6));
						newTags.Add(newTag);
						updateTags = true;
					}
					else if (tag == "level:Read aloud" || tag == "level:ጮክ ብለህ አንብብ")
					{
						var newTag = UploadToBloomLibrary.GetTagForLrmiReadingLevel(tag.Substring(6));
						newTags.Add(newTag);
						updateTags = true;
					}
					else if (tag == "level:Decodable" || tag == "level:መፍታት የሚችል")
					{
						var newTag = UploadToBloomLibrary.GetTagForLrmiReadingLevel(tag.Substring(6));
						newTags.Add(newTag);
						updateTags = true;
					}
					else
					{
						newTags.Add(tag);
					}
				}

				if (updateTags)
				{
					++fixedCount;
					var updateJson = new StringBuilder("{ \"updateSource\":\"importerbot@bloomlibrary.org\", \"tags\":[");
					var sep = "";
					foreach (var tag in newTags)
					{
						updateJson.AppendFormat("{0}\"{1}\"", sep, tag);
						sep = ",";
					}
					updateJson.Append("] }");
					if (_options.VeryVerbose)
						Console.WriteLine("DEBUG: For \"{0}\", fix tags json = {1}", book.Title, updateJson);
					var response = client.UpdateObject("books", book.ObjectId, updateJson.ToString());
					if (response.StatusCode != System.Net.HttpStatusCode.OK)
						Console.WriteLine("WARNING: updating the book table for \"{0}\" failed: {1}", book.Title, response.Content);

				}
			}
			if (_options.Verbose)
				Console.WriteLine("INFO: fixed tags in {0} of {1} books.", fixedCount, bookCount);
			return 0;
		}

		private bool VerifyOptions()
		{
			var allValid = true;
			if (_options.VeryVerbose)
				_options.Verbose = true;
			if (String.IsNullOrWhiteSpace(_options.UploadUser) || String.IsNullOrWhiteSpace(_options.UploadPassword))
			{
				if (String.IsNullOrWhiteSpace(_options.UploadUser))
					_options.UploadUser = Program.GetEnvironmentVariable("RoseGardenUserName");
				if (String.IsNullOrWhiteSpace(_options.UploadPassword))
					_options.UploadPassword = Program.GetEnvironmentVariable("RoseGardenUserPassword");
				if (String.IsNullOrWhiteSpace(_options.UploadUser) || String.IsNullOrWhiteSpace(_options.UploadPassword))
				{
					Console.WriteLine("WARNING: without a user name (-U/--user) and password (-P/--password), RoseGarden cannot fix the tags for books that have already been uploaded.  These values may be supplied by the RoseGardenUserName and RoseGardenUserPassword environment variables.");
					allValid = false;
				}
			}
			return allValid;
		}
	}
}