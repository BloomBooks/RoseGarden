// Copyright (c) 2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.IO;

namespace RoseGarden
{
	public class UploadToBloomLibrary
	{
		UploadOptions _options;
		public UploadToBloomLibrary(UploadOptions opts)
		{
			_options = opts;
		}

		internal int RunUpload()
		{
			if (!VerifyOptions())
				return 1;
			var process = new Process
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = _options.BloomExe,
					Arguments = $"upload {(_options.SingleBookshelfLevel?"-s":"")} -u \"{_options.UploadUser}\" -p \"{_options.UploadPassword}\" \"{_options.BookShelfContainer}\"",
					UseShellExecute = false,
					CreateNoWindow = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				},
			};
			if (_options.VeryVerbose)
				Console.WriteLine("DEBUG: upload arguments={0}", process.StartInfo.Arguments);
			process.Start();
			process.WaitForExit();

			var standardOutput = process.StandardOutput.ReadToEnd();
			var standardError = process.StandardError.ReadToEnd();
			if (!String.IsNullOrWhiteSpace(standardError))
			{
				Console.WriteLine("WARNING: uploading {0} produced the following error messages:", _options.BookShelfContainer);
				Console.WriteLine(standardError);
			}
			if (_options.VeryVerbose && !String.IsNullOrWhiteSpace(standardOutput))
			{
				Console.WriteLine("DEBUG: uploading {0} produced this output with exit code {1}", _options.BookShelfContainer, process.ExitCode);
				Console.WriteLine(standardOutput);
			}
			else if (_options.Verbose)
			{
				Console.WriteLine("INFO: uploading {0} has finished with exit code {1}", _options.BookShelfContainer, process.ExitCode);
			}
			return process.ExitCode;
		}

		private bool VerifyOptions()
		{
			var allValid = true;
			if (!File.Exists(_options.BloomExe))
			{
				Console.WriteLine("WARNING: {0} does not exist!", _options.BloomExe);
				allValid = false;
			}
			if (!Directory.Exists(_options.BookShelfContainer))
			{
				Console.WriteLine("WARNING: {0} does not exist!", _options.BookShelfContainer);
				allValid = false;
			}
			return allValid;
		}
	}
}
