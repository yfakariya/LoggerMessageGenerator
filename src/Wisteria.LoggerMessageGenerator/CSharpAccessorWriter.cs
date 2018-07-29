// Copyright (c) Yusuke Fujiwara. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wisteria.LoggerMessageGenerator
{
	/// <summary>
	/// Writer for C# portions.
	/// </summary>
	internal sealed class CSharpAccessorWriter : OutputWriter
	{
		private static readonly string ToolIdentifier =
			$"{typeof(CSharpAccessorWriter).Assembly.GetName().Name} version " +
			$"{typeof(CSharpAccessorWriter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? typeof(CSharpAccessorWriter).Assembly.GetName().Version.ToString()}";

		private static readonly string[] MessagesClassUsings =
			new[]
			{
				"System",
				"System.Globalization",
				"System.Resources",
				"System.Threading",
				"Microsoft.Extensions.Logging"
			};

		private static readonly string[] ExtensionMethodClassUsings =
			new[]
			{
				"System",
				"System.Diagnostics",
				"System.Globalization",
				"System.Runtime.CompilerServices",
				"Microsoft.Extensions.Logging"
			};

		private readonly string _namespace;
		private readonly string _indent;
		private readonly bool _isPublic;

		public CSharpAccessorWriter(string @namespace, string indent, bool isPublic, string outputDirectoryPath, string fileNameWithoutExtension, Func<string, Stream> streamFactory)
			: base(outputDirectoryPath, fileNameWithoutExtension, streamFactory)
		{
			this._namespace = @namespace;
			this._indent = indent;
			this._isPublic = isPublic;
		}

		public async Task WriteAsync(IReadOnlyCollection<LoggerMessageModel> models, CancellationToken cancellationToken)
		{
			var typeName = this.GetFirstSegmentOfFileName();
			await Task.WhenAll(
				this.WriteMessageHolderAsync(models, typeName, cancellationToken),
				this.WriteExtensionMethodsAsync(models, typeName, cancellationToken)
			).ConfigureAwait(false);
		}

		private string GetFirstSegmentOfFileName()
		{
			var fileName = this.FileNameWithoutExtension;
			var firstDotIndex = fileName.IndexOf('.');
			if (firstDotIndex < 0)
			{
				return IdentifierUtilities.NormalizeName(fileName, _ => { }).Name;
			}
			else
			{
				return IdentifierUtilities.NormalizeName(fileName.Remove(firstDotIndex), _ => { }).Name;
			}
		}

		private async Task WriteMessageHolderAsync(IReadOnlyCollection<LoggerMessageModel> models, string enclosingTypeName, CancellationToken cancellationToken)
		{
			var fileName = this.FileNameWithoutExtension + ".Messages.cs";
			using (var writer = new StringWriter())
			{
				this.WriteClassHeader(writer, MessagesClassUsings);
				{
					// enclosing class
					this.Indent(writer, 1).WriteLine($"partial class {enclosingTypeName}");
					this.Indent(writer, 1).WriteLine("{");
					{
						// target inner class
						this.Indent(writer, 2).WriteLine($"internal sealed class Messages");
						this.Indent(writer, 2).WriteLine("{");
						{
							// singleton resource manager
							this.Indent(writer, 3).WriteLine($"private static readonly ResourceManager ResourceManager = new ResourceManager(typeof({enclosingTypeName}).FullName, typeof({enclosingTypeName}).Assembly);");
							writer.WriteLine();

							// "current" instance
							this.Indent(writer, 3).WriteLine("private static Messages s_instance = new Messages(null);");
							writer.WriteLine();

							// current instance accessor
							this.Indent(writer, 3).WriteLine("public static Messages GetInstance() => s_instance;");
							writer.WriteLine();

							// culture changer
							this.Indent(writer, 3).WriteLine("public static void ChangeCulture(CultureInfo newCulture)");
							this.Indent(writer, 3).WriteLine("{");
							{
								this.Indent(writer, 4).WriteLine("var newInstance = new Messages(newCulture);");
								this.Indent(writer, 4).WriteLine("Volatile.Write(ref s_instance, newInstance);");
							}
							this.Indent(writer, 3).WriteLine("}");

							// fields and proeprties
							foreach (var model in models)
							{
								var actionType = $"Action<{MakeActionTypeArguments(model)}>";

								writer.WriteLine();
								this.Indent(writer, 3).WriteLine($"private readonly {actionType} _{model.ProgramIdentifier};");

								writer.WriteLine();
								this.Indent(writer, 3).WriteLine("/// <summary>");
								this.WriteDocumentationComment(writer, 3, MakeSummaryComment(model));
								this.Indent(writer, 3).WriteLine("/// </summary>");
								this.Indent(writer, 3).WriteLine($"public static {actionType} {model.ProgramIdentifier} => GetInstance()._{model.ProgramIdentifier};");
							}

							writer.WriteLine();

							// constructor
							this.Indent(writer, 3).WriteLine("private Messages(CultureInfo culture)");
							this.Indent(writer, 3).WriteLine("{");
							{
								foreach (var model in models)
								{
									this.Indent(writer, 4).Write($"this._{model.ProgramIdentifier} = LoggerMessage.Define");

									if (model.PlaceHolders.Count > 0)
									{
										writer.Write($"<{MakeDefineTypeArguments(model)}>");
									}

									writer.WriteLine($"(LogLevel.{model.Level}, new EventId({model.Id}, \"{model.Name}\"), ResourceManager.GetString(\"{model.Name}\", culture));");
								}
							}
							this.Indent(writer, 3).WriteLine("}");
						}
						this.Indent(writer, 2).WriteLine("}");
					}
					this.Indent(writer, 1).WriteLine("}");
				}
				this.WriteClassFooter(writer);

				await this.FlushAsync(writer, fileName, cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task WriteExtensionMethodsAsync(IReadOnlyCollection<LoggerMessageModel> models, string typeName, CancellationToken cancellationToken)
		{
			var fileName = this.FileNameWithoutExtension + ".cs";
			using (var writer = new StringWriter())
			{
				this.WriteClassHeader(writer, ExtensionMethodClassUsings);
				{
					// class
					this.Indent(writer, 1).WriteLine("/// <summary>");
					this.WriteDocumentationComment(writer, 1, $"Defines extension methods for <see ref=\"ILogger\" /> which achieve efficient logging.");
					this.Indent(writer, 1).WriteLine("/// </summary>");
					this.Indent(writer, 1).WriteLine("[DebuggerNonUserCode]");
					this.Indent(writer, 1).WriteLine("[CompilerGenerated]");
					this.Indent(writer, 1).WriteLine($"{(this._isPublic ? "public" : "internal")} static partial class {typeName}");
					this.Indent(writer, 1).WriteLine("{");
					{
						// change culture
						this.Indent(writer, 2).WriteLine("/// <summmary>");
						this.WriteDocumentationComment(writer, 2, "Change locale of logging messages.");
						this.Indent(writer, 2).WriteLine("/// </summary>");
						this.Indent(writer, 2).WriteLine($"/// <param name=\"culture\">New culture. If the value is <c>null</c>, a value of <see cref=\"CultureInfo.CurrentUICulture\" /> will be used.</param>");
						this.Indent(writer, 2).WriteLine($"public static void ChangeCulture(CultureInfo culture) => Messages.ChangeCulture(culture);");

						// accessors
						foreach (var model in models)
						{
							writer.WriteLine();

							this.Indent(writer, 2).WriteLine("/// <summary>");
							this.WriteDocumentationComment(writer, 2, MakeSummaryComment(model));
							this.Indent(writer, 2).WriteLine("/// </summary>");

							foreach (var placeHolder in model.PlaceHolders)
							{
								this.Indent(writer, 2).WriteLine($"/// <param name=\"{placeHolder.Name}\">Value for placeholder '{placeHolder.Name}' as {placeHolder.Type}.</param>");
							}

							if (!String.IsNullOrWhiteSpace(model.Comment))
							{
								this.Indent(writer, 2).WriteLine("/// <remarks>");
								this.WriteDocumentationComment(writer, 2, model.Comment);
								this.Indent(writer, 2).WriteLine("/// </summary>");
							}

							this.Indent(writer, 2).Write($"public static void {model.ProgramIdentifier}(this ILogger logger");
							if (model.HasException)
							{
								writer.Write(", Exception exception");
							}

							foreach (var placeHolder in model.PlaceHolders)
							{
								writer.Write($", {placeHolder.Type} {placeHolder.Name}");
							}

							writer.WriteLine(")");

							this.Indent(writer, 3).Write($"=> Messages.{model.ProgramIdentifier}(logger");

							foreach (var placeHolder in model.PlaceHolders)
							{
								writer.Write($", {placeHolder.Name}");
							}

							writer.WriteLine($", {(model.HasException ? "exception" : "null")});");
						}
					}
					this.Indent(writer, 1).WriteLine("}");
				}
				this.WriteClassFooter(writer);

				await this.FlushAsync(writer, fileName, cancellationToken).ConfigureAwait(false);
			}
		}

		private TextWriter Indent(TextWriter writer, int level)
		{
			for (var i = 0; i < level; i++)
			{
				writer.Write(this._indent);
			}

			return writer;
		}

		private void WriteClassHeader(TextWriter writer, IEnumerable<string> usingDirectives)
		{
			writer.WriteLine("//------------------------------------------------------------------------------");
			writer.WriteLine("// <auto-generated>");
			writer.WriteLine($"// This file was generated by {ToolIdentifier}");
			writer.WriteLine("// </auto-generated>");
			writer.WriteLine("//------------------------------------------------------------------------------");
			writer.WriteLine();
			foreach (var usingDirective in usingDirectives)
			{
				writer.WriteLine($"using {usingDirective};");
			}

			writer.WriteLine();
			writer.WriteLine($"namespace {this._namespace}");
			writer.WriteLine("{");
		}

		private void WriteClassFooter(TextWriter writer)
		{
			writer.WriteLine("}");
		}

		private static string MakeSummaryComment(LoggerMessageModel model)
			=> $"[0x{model.Id:X}]{model.Name}: {model.Message}";

		private static string MakeActionTypeArguments(LoggerMessageModel model)
			=> String.Concat(MakeActionTypeArgumentsCore(model));

		private static IEnumerable<string> MakeActionTypeArgumentsCore(LoggerMessageModel model)
		{
			yield return "ILogger";

			foreach (var placeHolder in model.PlaceHolders)
			{
				yield return ", ";
				yield return placeHolder.Type;
			}

			yield return ", Exception";
		}

		private static string MakeDefineTypeArguments(LoggerMessageModel model)
			=> String.Join(", ", model.PlaceHolders.Select(x => x.Type));

		private void WriteDocumentationComment(TextWriter writer, int indentLevel, string content)
		{
			using (var reader = new StringReader(content))
			{
				for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
				{
					this.Indent(writer, indentLevel).WriteLine($"/// {new XText(line)}");
				}
			}
		}

		private async Task FlushAsync(TextWriter writer, string fileName, CancellationToken cancellationToken)
		{
			using (var stream = this.OpenStream(Path.Combine(this.OutputDirectoryPath, fileName)))
			{
				await stream.WriteAsync(Encoding.UTF8.GetBytes(writer.ToString())).ConfigureAwait(false);
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
