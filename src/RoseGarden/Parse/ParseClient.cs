// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Collections.Generic;
using RoseGarden.Parse.Model;
using System.Linq;

namespace RoseGarden.Parse
{
	public enum EnvironmentSetting
	{
		Unknown,
		Local,
		Dev,
		Test,
		Prod
	}

	/// <summary>
	/// Connect and communicate with a parse.com server
	/// </summary>
	/// <remarks>
	/// This code is adapted (and simplified) from that in BloomDesktop and BloomHarvester.
	/// </remarks>
	public class ParseClient
	{
		// Fields and properties
		private string _user;
		private string _password;
		private RestClient _client;
		protected string _sessionToken = String.Empty;
		private string _applicationId;

		// Constructors
		public ParseClient(string user, string password)
		{
			_user = user;
			_password = password;
			var environmentSetting = GetEnvironmentSetting();
			_client = CreateRestClient(environmentSetting);
			_applicationId = GetApplicationId(environmentSetting);
			Debug.Assert(!String.IsNullOrWhiteSpace(_applicationId), "Parse Application ID is invalid. Retrieving books from Parse probably won't work. Consider checking your environment variables.");
		}

		private static RestClient CreateRestClient(EnvironmentSetting environment)
		{
			string url;
			switch (environment)
			{
				case EnvironmentSetting.Prod:
					url = "https://bloom-parse-server-production.azurewebsites.net/parse";
					break;
				case EnvironmentSetting.Test:
					url = "https://bloom-parse-server-unittest.azurewebsites.net/parse";
					break;
				case EnvironmentSetting.Dev:
				default:
					url = "https://bloom-parse-server-develop.azurewebsites.net/parse";
					break;
				case EnvironmentSetting.Local:
					url = "http://localhost:1337/parse";
					break;
			}
			return new RestClient(url);
		}


		/// <summary>
		/// Determine from the RoseGardenEnvironment environment variable what environment we're under.
		/// (P)roduction versus (D)evelopment is the primary choice.  Other choices may be used in time.
		/// The default value is (Dev)elopment.
		/// </summary>
		public static EnvironmentSetting GetEnvironmentSetting()
		{
			var env = Program.GetEnvironmentVariable("RoseGardenEnvironment");
			if (!String.IsNullOrEmpty(env))
			{
				if (env.ToLowerInvariant().StartsWith("p", StringComparison.InvariantCulture))
					return EnvironmentSetting.Prod;
				if (env.ToLowerInvariant().StartsWith("l", StringComparison.InvariantCulture))
					return EnvironmentSetting.Local;
				if (env.ToLowerInvariant().StartsWith("t", StringComparison.InvariantCulture))
					return EnvironmentSetting.Test;
			}
			return EnvironmentSetting.Dev;        // default to development
		}

		private static string GetApplicationId(EnvironmentSetting environment)
		{
			string appIdEnvVarKey;

			switch (environment)
			{
				case EnvironmentSetting.Prod:
					appIdEnvVarKey = "RoseGardenParseAppIdProd";
					break;
				case EnvironmentSetting.Test:
					appIdEnvVarKey = "RoseGardenParseAppIdTest";
					break;
				case EnvironmentSetting.Dev:
				default:
					appIdEnvVarKey = "RoseGardenParseAppIdDev";
					break;
				case EnvironmentSetting.Local:
					appIdEnvVarKey = null;
					break;
			}

			string applicationId = "myAppId";
			if (environment != EnvironmentSetting.Local)
			{
				applicationId = Environment.GetEnvironmentVariable(appIdEnvVarKey);
				if (String.IsNullOrEmpty(applicationId))
					applicationId = Environment.GetEnvironmentVariable(appIdEnvVarKey.ToUpperInvariant());	// Linux environment variables tend to be all uppercase.
			}

			return applicationId;
		}

		/// <summary>
		/// Logs in, if necessary
		/// </summary>
		private void EnsureLogIn()
		{
			if (String.IsNullOrEmpty(_sessionToken))
				LogIn();
		}

		/// <summary>
		/// Sends a request to log in
		/// </summary>
		private void LogIn()
		{
			_sessionToken = String.Empty;

			// The officially recommended way to pass the password is via a URL parameter of a GET request
			// (https://docs.parseplatform.org/rest/guide/#logging-in)
			// If you connect via HTTPS, it should get a secure TCP connection first, then send the HTTP request over SSL...
			// Which means that having the password in plaintext in the URL (which is at the HTTP layer) isn't immediately terrible.
			// Other than the fact that the server could very well want to log every URL it processes into a log... or print it out in an exception...
			// Or that you don't need https for localhost..
			var request = MakeRequest("login", Method.GET);
			request.AddParameter("username", _user);
			request.AddParameter("password", _password);

			var response = this.Client.Execute(request);
			CheckForResponseError(response, "Failed to log in");

			var dy = JsonConvert.DeserializeObject<dynamic>(response.Content);
			_sessionToken = dy.sessionToken; //there's also an "error" in there if it fails, but a null sessionToken tells us all we need to know
		}

		/// <summary>
		/// Makes a request of the specified type, and sets common headers and authentication tokens
		/// </summary>
		/// <param name="path">The Parse relative path, e.g. "classes/{className}"</param>
		/// <param name="requestType">e.g., GET, POST, etc.</param>
		private RestRequest MakeRequest(string path, Method requestType)
		{
			var request = new RestRequest(path, requestType);
			SetCommonHeaders(request);
			return request;
		}

		private void SetCommonHeaders(RestRequest request)
		{
			request.AddHeader("X-Parse-Application-Id", _applicationId);
			//request.AddHeader("X-Parse-REST-API-Key", RestApiKey); // REVIEW: Is this actually needed/used by our own parse-server? parse-server index.js suggests it is optional.
			if (!string.IsNullOrEmpty(_sessionToken))
				request.AddHeader("X-Parse-Session-Token", _sessionToken);
		}

		private void AddJsonToRequest(RestRequest request, string json)
		{
			request.AddParameter("application/json", json, ParameterType.RequestBody);
		}

		/// <summary>
		/// Updates an object in a Parse class (table)
		/// This method may take about half a second to complete.
		/// </summary>
		/// <param name="className">The name of the class (table). Do not prefix it with "classes/"</param>
		/// <param name="objectId">id string for the specific object (table row)</param>
		/// <param name="updateJson">The JSON of the object to update. It doesn't need to be the full object, just of the fields to update</param>
		/// <exception>Throws an application exception if the request fails</exception>
		/// <returns>The response after executing the request</returns>
		internal IRestResponse UpdateObject(string className, string objectId, string updateJson)
		{
			EnsureLogIn();
			var request = MakeRequest($"classes/{className}/{objectId}", Method.PUT);
			AddJsonToRequest(request, updateJson);

			var response = this.Client.Execute(request);
			CheckForResponseError(response, "Update failed.\nRequest.Json: {0}", updateJson);

			return response;
		}

		/// <summary>
		/// Create a new object in a Parse class (table).
		/// </summary>
		/// <param name="className">The name of the class (table). Do not prefix it with "classes/".</param>
		/// <param name="updateJson">Full json of the object to create.</param>
		/// <returns>The response after executing the request</returns>
		internal IRestResponse CreateObject(string className, string updateJson)
		{
			EnsureLogIn();
			var request = MakeRequest($"classes/{className}", Method.POST);
			AddJsonToRequest(request, updateJson);
			var response = this.Client.Execute(request);
			CheckForResponseError(response, "Update failed.\nRequest.Json: {0}", updateJson);
			return response;
		}

		private void CheckForResponseError(IRestResponse response, string exceptionInfoFormat, params object[] args)
		{
			if (!IsResponseCodeSuccess(response.StatusCode))
			{
				throw new ParseException(response, exceptionInfoFormat + "\n", args);
			}
		}

		private static bool IsResponseCodeSuccess(HttpStatusCode statusCode)
		{
			return ((int)statusCode >= 200) && ((int)statusCode <= 299);
		}

		/// <summary>
		/// Gets all rows from the Parse "books" class/table
		/// </summary>
		internal IEnumerable<Book> GetBooks(string whereCondition = "", IEnumerable<string> fieldsToDereference = null)
		{
			var request = new RestRequest("classes/books", Method.GET);
			SetCommonHeaders(request);
			request.AddParameter("keys", "object_id,importerName,importerMajorVersion,importerMinorVersion,importedBookSourceUrl,title,authors,bookInstanceId,uploader,lastUploaded,updateSource,tags,inCirculation,publisher");

			if (!String.IsNullOrEmpty(whereCondition))
			{
				request.AddParameter("where", whereCondition);
			}

			// Instead of representing as object pointers (and requiring us to perform a 2nd query to get the object), Parse will dereference the pointer for us automatically
			if (fieldsToDereference != null)
			{
				foreach (var field in fieldsToDereference)
				{
					request.AddParameter("include", field);
				}
			}

			IEnumerable<Book> results = GetAllResults<Book>(request);
			return results;
		}

		internal IEnumerable<RelatedBooks> GetRelatedBooks(string id)
		{
			var request = new RestRequest("classes/relatedBooks", Method.GET);
			SetCommonHeaders(request);
			request.AddParameter("keys", "books");
			request.AddParameter("where", $"{{\"books\": {{\"__type\": \"Pointer\", \"className\": \"books\", \"objectId\": \"{id}\"}} }}");
			request.AddParameter("include", "books");
			var results = GetAllResults<RelatedBooks>(request);
			return results;
		}

		/// <summary>
		/// Lazily gets all the results from a Parse database in chunks
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="request">The request should not include count, limit, skip, or order fields. This method will populate those in order to provide the functionality</param>
		/// <returns>Yields the results through an IEnumerable as needed</returns>
		private IEnumerable<T> GetAllResults<T>(IRestRequest request)
		{
			//Console.WriteLine("DEBUG GetAllResults(): request={0}", RequestToString(request));
			int numProcessed = 0;
			int totalCount = 0;
			do
			{
				// Make sure you don't have duplicate instances of a lot of these parameters, especially limit and skip.
				// Parse will not give you the results you want if you have them multiple times.
				AddOrReplaceParameter(request, "count", "1");
				AddOrReplaceParameter(request, "limit", "1000");   // The limit should probably be on the higher side. The fewer DB calls, the better, probably.
				AddOrReplaceParameter(request, "order", "createdAt");
				AddOrReplaceParameter(request, "skip", numProcessed.ToString());

				//Logger?.TrackEvent("ParseClient::GetAllResults Request Sent");
				var restResponse = this.Client.Execute(request);
				string responseJson = restResponse.Content;
				//Console.WriteLine("DEBUG GetAllResults(): response={0}", responseJson);
				var response = JsonConvert.DeserializeObject<Parse.ParseResponse<T>>(responseJson);

				totalCount = response.Count;
				if (totalCount == 0)
				{
					//Console.Out.WriteLine("Query returned no results.");
					break;
				}

				var currentResultCount = response.Results.Length;
				if (currentResultCount <= 0)
				{
					break;
				}

				for (int i = 0; i < currentResultCount; ++i)
				{
					yield return response.Results[i];
				}

				numProcessed += currentResultCount;

				if (numProcessed < totalCount)
				{
					string message = $"GetAllResults Rows Retrieved: {numProcessed} out of {totalCount}.";
					//Logger?.LogVerbose(message);
				}
			}
			while (numProcessed < totalCount);
		}

		private string RequestToString(IRestRequest request)
		{
			var bldr = new System.Text.StringBuilder();
			if (!String.IsNullOrEmpty(request.Resource))
				bldr.AppendFormat("RESOURCE={0}", request.Resource);
			if (request.Parameters != null)
			{
				if (bldr.Length > 0)
					bldr.Append("; ");
				bldr.AppendFormat("PARAMETERS=");
				var sep = "";
				foreach (var param in request.Parameters)
				{
					bldr.AppendFormat("{0}{1}:{2}", sep, param.Name, param.Value);
					sep = "; ";
				}
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Adds the specified parameter with the specified value. If the parameter already exists, the existing value will be overwritten.
		/// </summary>
		/// <param name="request">The object whose Parameters field will be modified</param>
		/// <param name="parameterName">The name of the parameter</param>
		/// <param name="parameterValue">The new value of the parameter</param>
		public void AddOrReplaceParameter(IRestRequest request, string parameterName, string parameterValue)
		{
			if (request.Parameters != null)
			{
				foreach (var param in request.Parameters)
				{
					if (param.Name == parameterName)
					{
						param.Value = parameterValue;
						return;
					}
				}
			}

			// At this point, indicates that no replacements were made while iterating over the params. We'll have to add it in.
			request.AddParameter(parameterName, parameterValue);
		}

		protected RestClient Client
		{
			get
			{
				if (_client == null)
				{
					_client = CreateRestClient(EnvironmentSetting.Dev);
				}
				return _client;
			}
		}

		public static Dictionary<string, Book> LoadBloomLibraryInfo(string user, string password)
		{
			var bloomlibraryBooks = new Dictionary<string, Book>();
			ParseClient parseClient = new ParseClient(user, password);
			string importedFilter = "{\"importedBookSourceUrl\": {\"$regex\": \".\"}}";
			IEnumerable<Book> bookList = parseClient.GetBooks(importedFilter, new[] { "uploader" });
			foreach (var book in bookList)
			{
				if (bloomlibraryBooks.ContainsKey(book.ImportedBookSourceUrl))
				{
					Console.WriteLine("ERROR: the same book has been imported twice with different book instance ids!? ({0}/{1})", book.Title, book.ImportedBookSourceUrl);
					Environment.Exit(2);
				}
				bloomlibraryBooks.Add(book.ImportedBookSourceUrl, book);
			}
			return bloomlibraryBooks;
		}
	}
}
