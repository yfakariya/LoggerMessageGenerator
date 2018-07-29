// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mono.Options;

namespace Wisteria.LoggerMessageGenerator
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			try
			{
				var help = false;
				var locales = new List<CultureInfo>();
				var tsvFilePath = default(string);
				var outputDirectory = ".";
				var @namespace = "Logging";
				var fileName = "LogMessages";
				var isPulic = false;
				var indent = "    ";

				var options =
					new OptionSet
					{
						{ "?|h|help", "Show this help.", _ => help = true },
						{ "l|locale=", "Specify localization locales in order to in specified TSV data.", v => locales.Add(CultureInfo.GetCultureInfo(v)) },
						{ "i|input=", "Specify input TSV file path.", v => tsvFilePath = v },
						{ "o|output-dir=", "Specify output directory path. Default is current directory.", v => outputDirectory = v },
						{ "n|namespace=", "Specify namespace of generated code. Default is Logging", v => @namespace = v },
						{ "f|file-name=", "Specify file name of generated code and resx without their extensions. Default is LogMessages", v => fileName = v },
						{ "p|public", "Specify generated accessor classes visibility is public or not. Default is false.", _ => isPulic = true },
						{ "indent=", "Specify generated accessor code indent string. Default is 4 whitespace chars.", v => indent = v }
					};

				try
				{
					options.Parse(args);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex.Message);
					help = true;
				}

				if (help || String.IsNullOrEmpty(tsvFilePath))
				{
					options.WriteOptionDescriptions(Console.Error);
					return 1;
				}

				var reader = new TsvSourceReader(locales.ToArray());
				var models = await reader.ReadAsync(File.OpenText(tsvFilePath), CancellationToken.None).ConfigureAwait(false);

				await new ResxWriter(outputDirectory, fileName, File.Create).WriteAsync(models, locales, CancellationToken.None).ConfigureAwait(false);
				await new CSharpAccessorWriter(@namespace, indent, isPulic, outputDirectory, fileName, File.Create).WriteAsync(models, CancellationToken.None).ConfigureAwait(false);

				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				return ex.HResult;
			}
		}
	}
}
