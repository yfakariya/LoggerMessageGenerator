LoggerMessageGenerator
===

Simple command line source code generator for `Microsoft.Extensions.Loggins.LoggerMessage` and relevants from TSV file.

Usage
---

Minimal:

```shell
dotnet Wisteria.LoggerMessageGenerator -i Logs.tsv -o path/to/source/dir/
```

With namespace:

```shell
dotnet Wisteria.LoggerMessageGenerator -i Logs.tsv -o path/to/source/dir/ -n MyCompany.MyProduct.MyComponent
```

With namespace and localization (example locales are quoted from Visual Studio language pack selection)

```shell
dotnet Wisteria.LoggerMessageGenerator -i Logs.tsv -o path/to/source/dir/ -n MyCompany.MyProduct.MyComponent -l cs -l de -l es -l fr -l it -l ja -l ko -l po -l pt-BR -l ru -l zh-Hans -l zh-Hant
```

Speficiation
---

### TSV file format

Create TSV(tab separeted value) file with UTF-8 encoding as following:

|**Column**|**Name**|**Format**|**Description**|**Note**|
|--|--|--|--|--|
|0|Event ID|`Int32`(decimal or hexadecimal)|Numeric ID. This value will be `EventId.Id` of the log message.||
|1|Event Name|`string`(dot separated identifier)|String ID. This value will be `EventId.Name` of the log message.|The value should be `PascalCasing`. Each component separated by dot must be `^[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}]*`|
|2|Log Level|`{F|C|E|W|I|D|T|V}`(case insensitive, only first char is significant)|Log level.|F and C are same meaning. Also, T and V are same meaning.|
|3|Has exception|(any char or blank)|If not blank, indicates this log accepts cached `Exception`.|This field affects wrapper extension method generation. If blank, the method will not accept `Exception` and will pass `null` for the delegate.|
|4|Message|`string` with [message template](https://messagetemplates.org/)|Logging message content.|This value should be English because all enviroments should show English message. Note that placeholders do not be `logger` nor `exception`.|
|5|Name of placeholder #1|`identifier` or blank|Label for placeholder.|This value will not be used code generation, but should be same as placeholder in the message template.|
|6|Type of placeholder #1|`TypeName`, `alias`, or blank|Type of placeholder.|This value can be alias described bellow. Otherwise, specify type's full name (that means you must specify its namespace components) to avoid compilation error.|
|7|Name of placeholder #2|(see #1)|(see #1)|(see #1)|
|8|Type of placeholder #2|(see #1)|(see #1)|(see #1)|
|9|Name of placeholder #3|(see #1)|(see #1)|(see #1)|
|10|Type of placeholder #3|(see #1)|(see #1)|(see #1)|
|11|Name of placeholder #4|(see #1)|(see #1)|(see #1)|
|12|Type of placeholder #4|(see #1)|(see #1)|(see #1)|
|13|Name of placeholder #5|(see #1)|(see #1)|(see #1)|
|14|Type of placeholder #5|(see #1)|(see #1)|(see #1)|
|15|Name of placeholder #6|(see #1)|(see #1)|(see #1)|
|16|Type of placeholder #6|(see #1)|(see #1)|(see #1)|
|17|Comment|`string`|Arbitrary comment.||
|18...|Localized message|`string` with [message template](https://messagetemplates.org/)|Localized logging message content.|The order of localized messages must match to command line's `-l` options order.|

#### Type alias

For placeholder types, you can specify C#'s type alias keywords like `int` or `decimal`. In addition, there are few special aliases for commonly used types:
* `datetime`, `date`, and `time` are considered as `System.DateTimeOffset`.
* `timespan`, `duration`, and `interval` are considered as `System.TimeSpan`.

#### Note to arguments

Currently, `Microsoft.Extensions.Logging.LoggerMessage` up to 6 arguments, so this tool assumes that plaholders count is up to 6.  
When the library introduces more arguments, a new TSV file format will be introduced.

#### Why TSV format?

* It is simple text format, so you can easily edit with text editor.
* You can easily import to or export from spread sheet via copy & pasting.
  * I believe that you can create more convinience tools to interact with spread sheet products directly even if the file uses calculation for easy authoring :)
* CSV format looks a bit covinient than TSV because some product in some platform can easily open `.csv` file in the product, but there are many caveats:
  * The product uses system default encoding which orients backward compatibility -- it cannot open the file as UTF-8.
  * Comma or other printable separator chars often appear in message itself. Escaping rules are complex for both of developers and users. It is rare to include holizontal tab charactor in log message itself.
  * AFAIK, some people argues that "C does not mean comma -- we use semicolon as separator".

So, I use TSV as input format.

### Output

The tool generates following files to the directory specified via `-o` command line option:

* `{name}.resx` which contains invariant messages.
  * Its `<name>` will be `EventName` of the TSV record. Charactors which cannot be used in identifier regarding to Unicode Annex 31 are replaced with `_`.
* `{name}.{culture}.resx` which contains localized messages for each `-l` command line options and TSV columns after #18.
* `{name}.cs` which contains extension methods for `Microsoft.Extensions.ILogger` to provide convinient and efficient logging.
  * Their `<summary>` comments are generated as `[{EventId}]{EventName}: {Message}`.
  * Their `<param>` comments and parameters are generated with message template in `Message` and `Type of placeholder #n`.
  * This file also contains `ChangeCulture(CultureInfo)` static method which change log message culture to specified culture.
    * Generally, you want to use specific culture for logging. It should be selected for your operation team.
    * If you specify `null`, `CultureInfo.CurrentUICulture` in that point will be used. The culture is not dynamic because the code caches gotten message strings until next `ChangeCulture` call.
    * If you want to use invariant culture, specify `CultureInfo.InvariantCulture`, not `null`.
    * The default is `null`, it means that you can control logging locale with platform settings.
* `{name}.Messages.cs` which contains `LoggerMessage.Define` calls and culture related codes.

Note that the code searches message resource as same as `resources.designers.cs` generated with Visual Studio for `*.resx`, so you must follow [naming rules of the resource files](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization#resource-file-naming).  
So if you specify `-f Something` and `-n MyCompany.MyProduct.MyComponent.Logging`, you must place generated `Something*.*` files in `Logging` directory of `MyCompany.MyProduct.MyComponent` project (assuming the root namespace property of the project is same as its assembly name). In a word, specify `-n` to match the namespace of the `xxx.designer.cs` file when you create resource file to the target directory in the project via Visual Studio.

### Command line options

Specify `-?`, `-h`, or `--help` to the command line to show help.

|**Short form**|**Long form**|**Value format**|**Allow multiple**|**Description**|
|--|--|--|--|--|
|?||(none)|no|Alias of `-h`|
|h|help|(none)|no|Show help.|
|l|locale|`CultureInfo.Name` compatible strings.|yes|Specify localization locales in order to in specified TSV data.|
|i|input|Valid existent file path|no|Required. Specify input TSV file path.|
|o|output-dir|Valid existent directory path|no|Specify output directory path. Default is current directory.|
|n|namespace|Valid C# namespace string|no|Specify namespace of generated code. Default is `Logging`.|
|f|file-name|Valid file name which compilant with type identifier|no|Specify file name of generated code and resx without their extensions. Default is `LogMessages`.|
|p|public|(none)|no|Specify generated accessor classes visibility is public or not. Default is `false`.|
||indent|Valid whitespace chars|no|Specify generated accessor code indent string. Default is 4 whitespace chars.|

FAQ
---

* Why the command line is not std-in/out friendly?
  * Because it writes mutiple outputs at once, it cannot use stdout for output. Since std-in based input does not look symmetric to output, it takes input via command line option.
* How about backward compatibility of exposed members?
  * Just best effort because this is a tool, not a library.
