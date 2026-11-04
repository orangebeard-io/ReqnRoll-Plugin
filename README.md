<h1 align="center">
  <a href="https://github.com/orangebeard-io/ReqnRoll-Plugin">
    <img src="https://raw.githubusercontent.com/orangebeard-io/ReqnRoll-Plugin/main/.github/logo.svg" alt="Orangebeard.io FitNesse TestSystemListener" height="200">
  </a>
  <br>Orangebeard.io ReqnRoll Plugin<br>
</h1>

<h4 align="center">A Plugin to report ReqnRoll tests in Orangebeard.</h4>

<p align="center">
  <a href="https://github.com/orangebeard-io/ReqnRoll-Plugin/blob/main/LICENSE.txt">
    <img src="https://img.shields.io/github/license/orangebeard-io/ReqnRoll-Plugin?style=flat-square"
      alt="License" />
  </a>
</p>

<div align="center">
  <h4>
    <a href="https://orangebeard.io">Orangebeard</a> |
    <a href="#build">Build</a> |
    <a href="#install">Install</a> |
    <a href="#attaching-files-and-binary-data">Attachments</a>
  </h4>
</div>

## Compatibility
Version 3.x.x plugin is compatible with ReqnRoll 3.x only. If you are on Reqnroll 2.x, use v1.x of the plugin.

## Build
 * Clone this repository
 * Open in a .Net IDE
 * Reference the Orangebeard.Client DLL (Find it on NuGet)
 * Build the Plugin DLL

## Install
 * Reference the Plugin in your Solution, make sure it is copied to your output directory (You can find the Plugin on Nuget!)
 * Optional: Add hooks file for custom runtime hooks (see HooksExample.cs)
 * create orangebeard.json (and set it to copy to output dir):

```json
{
  "enabled": true,
  "server": {
    "url": "https://my.orangebeard.app/",
    "project": "MY_PROJECT_NAME",
    "authentication": {
      "accessToken": "MY_AUTH_TOKEN"
    }
  },
  "testSet": {
    "name": "Test run name",
    "description": "test run description",
    "attributes": [ "tag1", "somekey:somevalue" ]
  }
}

```

Now run your test as you normally do and see the results find their way to Orangebeard!

## Attaching files and binary data
Log output written through Reqnroll's `IReqnrollOutputHelper` is mirrored to Orangebeard:

```csharp
var output = scenarioContext.ScenarioContainer.Resolve<IReqnrollOutputHelper>();
```

 * `output.WriteLine("message")` logs a message. If the message contains a file path, that file is uploaded as an attachment.
 * `output.AddAttachment(path)` attaches the file at `path`.
 * `output.WriteLine(label, data[, fileName][, formatArgs...])` attaches in-memory content, where `data` is a `byte[]` or a `Stream` (e.g. a PDF fetched from an API). `label` becomes the log message (and may contain `{0}` placeholders filled from `formatArgs`).

The attachment's file name is determined as follows:

 1. An explicit `fileName` (a string directly after `data`) is used as is.
 2. Otherwise `label` is used. If it already has an extension, it is kept.
 3. If `label` names a known file type (`pdf`, `json`, `xml`, `csv`, ...) it is used as the extension, e.g. `json` becomes `json.json`.
 4. Otherwise the type is detected from the content (PDF, PNG, JPG, ZIP).
 5. Otherwise `.bin` is used.

The content type is derived from the resulting file name's extension.

| Call | Stored file name |
|---|---|
| `WriteLine("Invoice", pdfBytes, "invoice-2024.pdf")` | `invoice-2024.pdf` |
| `WriteLine("pdf", pdfBytes)` | `pdf.pdf` |
| `WriteLine("json", jsonBytes)` | `json.json` |
| `WriteLine("response.xml", xmlBytes)` | `response.xml` |
| `WriteLine("Invoice", pdfBytes)` | `Invoice.pdf` (detected from content) |
| `WriteLine("Response", jsonBytes)` | `Response.bin` (unknown label, JSON is not detected) |
| `WriteLine("Download", stream, "export.csv")` | `export.csv` |
| `WriteLine("Order {0}", pdfBytes, "o.pdf", 42)` | `o.pdf` (log message `Order 42`) |

Note: a label containing a dot (e.g. `"Response v1.2"`) is treated as already having an extension; pass an explicit `fileName` in that case.

