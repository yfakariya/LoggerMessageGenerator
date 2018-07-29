// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Defines basic features and interfaces of source reader.
	/// </summary>
	internal abstract class SourceReader
	{
		protected SourceReader() { }

		public Task<IReadOnlyCollection<LoggerMessageModel>> ReadAsync(TextReader reader, CancellationToken cancellationToken)
			=> this.ReadAsyncCore(reader ?? throw new ArgumentNullException(nameof(reader)), cancellationToken);

		protected abstract Task<IReadOnlyCollection<LoggerMessageModel>> ReadAsyncCore(TextReader reader, CancellationToken cancellationToken);
	}
}
