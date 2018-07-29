// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Globalization;

using Microsoft.Extensions.Logging;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Represents data model of logger message.
	/// </summary>
	internal sealed class LoggerMessageModel
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string ProgramIdentifier { get; set; }

		public LogLevel Level { get; set; }

		public bool HasException { get; set; }

		public string Message { get; set; }

		public IReadOnlyList<PlaceHolder> PlaceHolders { get; set; }

		public string Comment { get; set; }

		public IReadOnlyDictionary<CultureInfo, string> Localizations { get; set; }

		public IReadOnlyCollection<string> Warnings { get; set; }
	}
}
