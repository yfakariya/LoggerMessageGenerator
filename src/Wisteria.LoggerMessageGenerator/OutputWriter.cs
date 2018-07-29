// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.IO;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Defines basic features of output writer.
	/// </summary>
	internal abstract class OutputWriter
	{
		protected string OutputDirectoryPath { get; }

		protected string FileNameWithoutExtension { get; }

		private readonly Func<string, Stream> _streamFactory;

		protected OutputWriter(string outputDirectoryPath, string fileNameWithoutExtension, Func<string, Stream> streamFactory)
		{
			try
			{
				this.OutputDirectoryPath = Path.GetFullPath(outputDirectoryPath);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message, nameof(outputDirectoryPath), ex);
			}

			try
			{
				this.FileNameWithoutExtension = Path.GetFileName(fileNameWithoutExtension);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message, nameof(fileNameWithoutExtension), ex);
			}

			this._streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
		}

		protected Stream OpenStream(string path)
			=> this._streamFactory(path);
	}
}
