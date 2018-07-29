// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Represents placeholder metadata in message template.
	/// </summary>
	internal readonly struct PlaceHolder
	{
		public string Name { get; }

		public string Type { get; }

		public PlaceHolder(string name, string type)
		{
			this.Name = name;
			this.Type = type;
		}
	}
}
