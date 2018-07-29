// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace Wisteria.LoggerMessageGenerator
{
	partial class RelaxedParser
	{
		private static readonly IReadOnlyDictionary<string, string> TypeAlias =
			new Dictionary<string, string>(14, StringComparer.OrdinalIgnoreCase)
			{
				{ "bool", typeof(bool).Name },
				{ "char", typeof(char).Name },
				{ "string", typeof(string).Name },
				{ "decimal", typeof(decimal).Name },
				{ "byte", typeof(byte).Name },
				{ "short", typeof(short).Name },
				{ "int", typeof(int).Name },
				{ "long", typeof(long).Name },
				{ "sbyte", typeof(sbyte).Name },
				{ "ushort", typeof(ushort).Name },
				{ "uint", typeof(uint).Name },
				{ "ulong", typeof(ulong).Name },
				{ "float", typeof(float).Name },
				{ "double", typeof(double).Name },
				{ "datetime", typeof(DateTimeOffset).Name },
				{ "date", typeof(DateTimeOffset).Name },
				{ "time", typeof(DateTimeOffset).Name },
				{ "timespan", typeof(TimeSpan).Name },
				{ "duration", typeof(TimeSpan).Name },
				{ "interval", typeof(TimeSpan).Name },
				{ "exception", typeof(Exception).Name }
			};
	}
}
