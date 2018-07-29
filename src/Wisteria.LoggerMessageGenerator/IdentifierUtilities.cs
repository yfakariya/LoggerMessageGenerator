// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using System.Text;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Utilities about code identifiers.
	/// </summary>
	internal static class IdentifierUtilities
	{
		public static (string Name, string Identifier) NormalizeName(string value, Action<string> reportWarning)
		{
			var trimmed = value?.Trim();
			if (String.IsNullOrEmpty(trimmed))
			{
				reportWarning("Name cannot be blank.");
				return default;
			}

			// As C# specification, use Form C and Unicode Annex 31.

			var normalized = trimmed.Normalize(NormalizationForm.FormC);
			if (trimmed != normalized)
			{
				reportWarning($"Name '{trimmed}'was normalized to '{normalized}' with Form C for compatibility.");
			}

			// Minimally escaped names with dot separator.
			var name = new StringBuilder();
			// Escaped name, dots are replaced with underscores.
			var identifier = new StringBuilder();

			var enumerator = StringInfo.GetTextElementEnumerator(normalized);
			while (enumerator.MoveNext())
			{
				var cp = Char.ConvertToUtf32(normalized, enumerator.ElementIndex);
				if (cp != '_')
				{
					if (IsFirstIdentifier(cp))
					{
						if (cp < 0x10000)
						{
							// fast path
							name.Append((char)cp);
							identifier.Append((char)cp);
						}
						else
						{
							name.Append(Char.ConvertFromUtf32(cp));
							identifier.Append(Char.ConvertFromUtf32(cp));
						}
						break;
					}
					else
					{
						reportWarning($"Name cannot begin with '{Char.ConvertFromUtf32(cp)}'(U+{cp:X4}). This charactor replaced with '_'");
					}
				}

				name.Append('_');
				identifier.Append('_');
				break;
			}

			while (enumerator.MoveNext())
			{
				var cp = Char.ConvertToUtf32(normalized, enumerator.ElementIndex);
				if (cp == '.')
				{
					name.Append('.');
					identifier.Append('_');
					continue;
				}

				if (IsIdentifier(cp))
				{
					if (cp < 0x10000)
					{
						// fast path
						name.Append((char)cp);
						identifier.Append((char)cp);
					}
					else
					{
						name.Append(Char.ConvertFromUtf32(cp));
						identifier.Append(Char.ConvertFromUtf32(cp));
					}
				}
				else
				{
					reportWarning($"Name cannot contain '{Char.ConvertFromUtf32(cp)}'(U+{cp:X4}). This charactor replaced with '_'");
					name.Append('_');
					identifier.Append('_');
				}
			}

			if (name.Length == 0)
			{
				reportWarning("Name cannot be blank.");
				return default;
			}

			return (name.ToString(), identifier.ToString());
		}

		private static bool IsFirstIdentifier(int cp)
		{
			switch (CharUnicodeInfo.GetUnicodeCategory(cp))
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.LetterNumber:
				{
					return true;
				}
				default:
				{
					return false;
				}
			}
		}

		private static bool IsIdentifier(int cp)
		{
			switch (CharUnicodeInfo.GetUnicodeCategory(cp))
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.ConnectorPunctuation:
				{
					return true;
				}
				default:
				{
					// Format char is not allowed here as Unicode Annex31 even if they are permitted in C# spec.
					return false;
				}
			}
		}
	}
}
