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
    <a href="#install">Install</a>
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

