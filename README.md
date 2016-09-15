# Welcome!

Elevator is a utility that allows other programs to collect traces through Windows Performance Recorder. It responds to commands to start, end or cancel tracing via WPR. It has a set number of profiles that it knows how to trace with, though you can configure them in `DefaultTraceProfile.wprp`.

> **WARNING:** Elevator runs with administrator rights, and responds to commands from other programs, which means you should handle it with care. Elevator attempts to ensure it's not going to do anything harmful before executing a command, but you still shouldn't leave it running when you're not using it.

## Dependencies
Using Elevator requires the [Windows Performance Toolkit.](https://msdn.microsoft.com/en-us/library/windows/hardware/dn927310(v=vs.85).aspx)

## Coding Style
This project follows the [dotnet/corefx C# Coding Style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md).

## Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Known uses
* [BrowserEfficiencyTest](https://github.com/MicrosoftEdge/BrowserEfficiencyTest) uses Elevator to measure the resource consumption on a system while it uses Webdriver to automate workloads in different browsers.