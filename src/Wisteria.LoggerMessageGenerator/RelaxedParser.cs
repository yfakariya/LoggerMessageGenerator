// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Relaxed parsing logics.
	/// </summary>
	internal static partial class RelaxedParser
	{
		public static LogLevel ParseLevel(string value)
		{
			var trimmed = value?.Trim();
			if (!String.IsNullOrEmpty(trimmed))
			{
				switch (Char.ToUpperInvariant(trimmed.Normalize(NormalizationForm.FormKD)[0]))
				{
					case 'C': // Critical
					case 'F': // Fatal
					{
						return LogLevel.Critical;
					}
					case 'E':
					{
						return LogLevel.Error;
					}
					case 'W':
					{
						return LogLevel.Warning;
					}
					case 'I':
					{
						return LogLevel.Information;
					}
					case 'D': // Debug
					{
						return LogLevel.Debug;
					}
					case 'T': // Trace
					case 'V': // Verbose
					{
						return LogLevel.Trace;
					}
				}
			}

			return LogLevel.None;
		}

		public static bool ParseNonBlank(string value)
			=> !String.IsNullOrWhiteSpace(value);

		public static string ParseType(string value)
		{
			var trimmed = value?.Trim();
			if (String.IsNullOrEmpty(trimmed))
			{
				return null;
			}

			return TypeAlias.TryGetValue(trimmed, out var typeName) ? typeName : trimmed;
		}
	}
}
