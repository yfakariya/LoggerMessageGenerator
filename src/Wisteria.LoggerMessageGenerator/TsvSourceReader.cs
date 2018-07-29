// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Reader for Tab Separated Value.
	/// </summary>
	internal sealed class TsvSourceReader : SourceReader
	{
		private static readonly char[] Delimiter = { '\t' };
		private readonly CultureInfo[] _locales;

		public TsvSourceReader(params CultureInfo[] localizationLocales)
		{
			this._locales = localizationLocales ?? Array.Empty<CultureInfo>();
			if (new HashSet<CultureInfo>(this._locales).Count != this._locales.Length)
			{
				throw new ArgumentException("There are duplicated locales.", nameof(localizationLocales));
			}
		}

		private const int MinimumColumnsCount = 17;
		private const int MaximumPlaceHoldersCount = 6;
		private const int PlaceHoldersOffset = 5;
		private const int LocalizationColumnOffset = MinimumColumnsCount + 1;

		protected override async Task<IReadOnlyCollection<LoggerMessageModel>> ReadAsyncCore(TextReader reader, CancellationToken cancellationToken)
		{
			// ID   Name   Level   Exception   Message Name1    Type1   ... Name6   Type6   Comment
			var result = new List<LoggerMessageModel>();
			var existingNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var existingIdentifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			var warnings = new List<string>();
			var lineNumber = -1;
			while (true)
			{
				var line = await reader.ReadLineAsync().ConfigureAwait(false);
				if (line == null)
				{
					break;
				}

				lineNumber++;

				line = line.Trim();
				if (line.Length == 0 || line[0] == '#' || line[0] == '/')
				{
					continue;
				}

				var tokens = line.Split(Delimiter);
				if (tokens.Length < MinimumColumnsCount)
				{
					warnings.Add($"Line {lineNumber}: This line only contains {tokens.Length} fields. 17 fields are required.");
					continue;
				}

				var hasError = false;

				if (!Int32.TryParse(tokens[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var id) && !Int32.TryParse(tokens[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
				{
					warnings.Add($"Line {lineNumber}: Field 0: ID format is not valid. It must be decimal or hexa-decimal integer.");
					id = 0;
					hasError = true;
				}

				var (name, identifier) = IdentifierUtilities.NormalizeName(tokens[1], w => warnings.Add($"Line {lineNumber}: Field 0: {w}"));
				if (!existingNames.TryAdd(name, lineNumber))
				{
					warnings.Add($"Line {lineNumber}: Field 0: Normalized name '{name}' already exists at line {existingNames[name]}.");
					hasError = true;
				}

				if (!existingIdentifiers.TryAdd(identifier, lineNumber))
				{
					warnings.Add($"Line {lineNumber}: Field 0: Escaped name '{identifier}' already exists at line {existingIdentifiers[name]}.");
					hasError = true;
				}

				var level = RelaxedParser.ParseLevel(tokens[2]);

				if (level == LogLevel.None)
				{
					warnings.Add($"Line {lineNumber}: Field 2: Unknown level '{tokens[2]}'.");
					hasError = true;
				}

				var hasException = RelaxedParser.ParseNonBlank(tokens[3]);

				var message = tokens[4];

				var invariantPlaceHolderNames = new LogValuesFormatter(message).ValueNames;
				if (invariantPlaceHolderNames.Count > MaximumPlaceHoldersCount)
				{
					warnings.Add($"Line {lineNumber}: Field 3: Too many placeholders. Maximum allowed count is 6, but there are {invariantPlaceHolderNames.Count} placeholders.");
					hasError = true;
				}

				foreach (var (index, placeHolderName) in invariantPlaceHolderNames.Select((x, i) => (index: i, name: x)))
				{
					switch (placeHolderName)
					{
						case "logger":
						case "exception":
						{
							warnings.Add($"Line {lineNumber}: Field 3: Placeholder name '{placeHolderName}' at index {index} is reserved.");
							hasError = true;
							break;
						}
					}
				}

				var placeHolders = ParsePlaceHolders(lineNumber, tokens, invariantPlaceHolderNames, warnings, ref hasError);

				var comment = tokens.Length >= MinimumColumnsCount + 1 ? tokens[MinimumColumnsCount] : null;

				var localizations = new Dictionary<CultureInfo, string>(this._locales.Length);

				for (var localizationIndex = 0; localizationIndex < this._locales.Length; localizationIndex++)
				{
					if (tokens.Length <= localizationIndex + LocalizationColumnOffset)
					{
						warnings.Add($"Line {lineNumber}: Field {localizationIndex + LocalizationColumnOffset}: Localization for '{this._locales[localizationIndex]}' is missing.");
						hasError = true;
						continue;
					}

					var localizedPlaceHolderNames = new LogValuesFormatter(tokens[localizationIndex + LocalizationColumnOffset]).ValueNames;
					var hasMismatch = false;
					foreach (var invariantPlaceHolder in invariantPlaceHolderNames)
					{
						if (!localizedPlaceHolderNames.Contains(invariantPlaceHolder))
						{
							warnings.Add($"Line {lineNumber}: Field {localizationIndex + LocalizationColumnOffset}: Localization for '{this._locales[localizationIndex]}' does not contain placeholder '{{{invariantPlaceHolder}}}'.");
							hasError = true;
							hasMismatch = true;
						}
					}

					foreach (var localizedPlaceHolder in localizedPlaceHolderNames)
					{
						if (!invariantPlaceHolderNames.Contains(localizedPlaceHolder))
						{
							warnings.Add($"Line {lineNumber}: Field {localizationIndex + LocalizationColumnOffset}: Localization for '{this._locales[localizationIndex]}' has extra placeholder '{{{localizedPlaceHolder}}}'.");
							hasError = true;
							hasMismatch = true;
						}
					}

					if (!hasMismatch)
					{
						Debug.Assert(localizedPlaceHolderNames.Count == invariantPlaceHolderNames.Count, "localizedPlaceHolderNames.Count == invariantPlaceHolderNames.Count");

						for (var placeHolderIndex = 0; placeHolderIndex < invariantPlaceHolderNames.Count; placeHolderIndex++)
						{
							if (invariantPlaceHolderNames[placeHolderIndex] != localizedPlaceHolderNames[placeHolderIndex])
							{
								warnings.Add($"Line {lineNumber}: Field {localizationIndex + LocalizationColumnOffset}: Localization for '{this._locales[localizationIndex]}' has placeholder position error. A placeholder '{{{invariantPlaceHolderNames[placeHolderIndex]}}}' must be at index {placeHolderIndex} but placed at index {localizedPlaceHolderNames.IndexOf(invariantPlaceHolderNames[placeHolderIndex])}.");
								hasError = true;
							}
						}

						localizations.Add(this._locales[localizationIndex], tokens[localizationIndex + LocalizationColumnOffset]);
					}
				}

				if (hasError)
				{
					continue;
				}

				var entry =
					new LoggerMessageModel
					{
						Comment = comment,
						HasException = hasException,
						Id = id,
						Level = level,
						Message = message,
						Name = name,
						ProgramIdentifier = identifier,
						PlaceHolders = placeHolders,
						Localizations = localizations,
						Warnings = warnings.ToArray()
					};

				result.Add(entry);

				warnings.Clear();
			}

			if (result.Count == 0)
			{
				throw new InvalidOperationException($"There are no valid messages.{Environment.NewLine}{String.Join(Environment.NewLine, warnings)}");
			}

			return result;
		}

		private static IReadOnlyList<PlaceHolder> ParsePlaceHolders(int lineNumber, string[] tokens, List<string> placeHolderNames, List<string> warnings, ref bool hasError)
		{
			var placeHolders = new List<PlaceHolder>(MaximumPlaceHoldersCount);
			var missingTypes = new List<int>(MaximumPlaceHoldersCount);
			for (var i = 0; i < MaximumPlaceHoldersCount; i++)
			{
				var nameToken = tokens[(i * 2) + PlaceHoldersOffset];
				var typeToken = tokens[(i * 2) + PlaceHoldersOffset + 1];
				var hasPairError = false;
				var isExtra = false;

				if (String.IsNullOrWhiteSpace(nameToken))
				{
					if (i < placeHolderNames.Count)
					{
						warnings.Add($"Line {lineNumber}: Field {(i * 2) + PlaceHoldersOffset}: Name label of placeholder '{{{placeHolderNames[i]}}}'(index {i}) may be missing.");
						hasError = true;
						hasPairError = true;
					}
					else
					{
						// skip
						continue;
					}
				}
				else if (i >= placeHolderNames.Count)
				{
					warnings.Add($"Line {lineNumber}: Field {(i * 2) + PlaceHoldersOffset}: Name label of placeholder '{nameToken}'(index {i}) may be extra.");
					isExtra = true;
				}

				if (String.IsNullOrWhiteSpace(typeToken))
				{
					if (i < placeHolderNames.Count)
					{
						warnings.Add($"Line {lineNumber}: Field {(i * 2) + PlaceHoldersOffset + 1}: Type information of placeholder '{{{placeHolderNames[i]}}}'(index {i}) may be missing.");
						hasError = true;
						hasPairError = true;
					}
					else
					{
						// skip
						continue;
					}
				}
				else if (i >= placeHolderNames.Count)
				{
					warnings.Add($"Line {lineNumber}: Field {(i * 2) + PlaceHoldersOffset}: Type information of placeholder '{nameToken}'(index {i}) may be extra.");
					isExtra = true;
				}

				if (isExtra)
				{
					continue;
				}

				if (!String.Equals(placeHolderNames[i], nameToken, StringComparison.OrdinalIgnoreCase))
				{
					warnings.Add($"Line {lineNumber}: Field {(i * 2) + PlaceHoldersOffset}: Name label of placeholder '{{{placeHolderNames[i]}}}'(index {i}) should not be '{nameToken}'.");
					// ignorable error.
				}

				if (!hasPairError)
				{
					placeHolders.Add(new PlaceHolder(placeHolderNames[i], RelaxedParser.ParseType(typeToken)));
				}
				else
				{
					// Add dummy entry for further validdation.
					placeHolders.Add(new PlaceHolder(placeHolderNames[i], String.Empty));
				}
			}

			return placeHolders;
		}
	}
}
