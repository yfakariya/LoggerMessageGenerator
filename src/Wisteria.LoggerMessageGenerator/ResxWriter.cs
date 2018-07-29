// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Writer for resx files.
	/// </summary>
	internal sealed class ResxWriter : OutputWriter
	{
		// From https://github.com/mono/mono/blob/884318b119611f9bc3a574fc9fa4542d9d1a5166/mcs/class/System.Windows.Forms/System.Resources/ResXResourceWriter.cs#L630 (MIT License)
		private static readonly string Schema = @"
  <xsd:schema id='root' xmlns='' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
    <xsd:element name='root' msdata:IsDataSet='true'>
      <xsd:complexType>
        <xsd:choice maxOccurs='unbounded'>
          <xsd:element name='data'>
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1' />
                <xsd:element name='comment' type='xsd:string' minOccurs='0' msdata:Ordinal='2' />
              </xsd:sequence>
              <xsd:attribute name='name' type='xsd:string' msdata:Ordinal='1' />
              <xsd:attribute name='type' type='xsd:string' msdata:Ordinal='3' />
              <xsd:attribute name='mimetype' type='xsd:string' msdata:Ordinal='4' />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name='resheader'>
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1' />
              </xsd:sequence>
              <xsd:attribute name='name' type='xsd:string' use='required' />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
".Replace("'", "\"");

		public ResxWriter(string outputDirectoryPath, string fileNameWithoutExtension, Func<string, Stream> streamFactory)
			: base(outputDirectoryPath, fileNameWithoutExtension, streamFactory)
		{
		}

		public async Task WriteAsync(IEnumerable<LoggerMessageModel> entries, IEnumerable<CultureInfo> locales, CancellationToken cancellationToken)
		{
			await this.WriteAsync(entries, default(CultureInfo), cancellationToken).ConfigureAwait(false);

			foreach (var locale in locales)
			{
				await this.WriteAsync(entries, locale, cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task WriteAsync(IEnumerable<LoggerMessageModel> entries, CultureInfo locale, CancellationToken cancellationToken)
		{
			var root =
				new XElement(
					XName.Get("root"),
					XElement.Parse(Schema),
					new XElement(
						"resheader",
						new XAttribute("name", "resmimetype"),
						new XElement("value", "text/microsoft-resx")
					),
					new XElement(
						"resheader",
						new XAttribute("name", "version"),
						new XElement("value", "1.3")
					),
					new XElement(
						"resheader",
						new XAttribute("name", "reader"),
						new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
					),
					new XElement(
						"resheader",
						new XAttribute("name", "writer"),
						new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
					)
				);
			foreach (var entry in entries)
			{
				var message = entry.Message;
				if (locale != null && !entry.Localizations.TryGetValue(locale, out message))
				{
					// missing localization.
					continue;
				}

				var data =
					new XElement(
						"data",
						new XAttribute("name", entry.Name),
						new XAttribute(XNamespace.Xml.GetName("space"), "preserve"),
						new XElement(
							"value",
							message
						)
					);
				root.Add(data);
			}

			using (var stream = this.OpenStream(Path.Combine(this.OutputDirectoryPath, this.FileNameWithoutExtension + (locale == null ? String.Empty : ('.' + locale.Name)) + ".resx")))
			using (var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Indent = true }))
			{
				await root.WriteToAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
