# Introduction
This class library contains classes that provide the following functionality for using audio in SIP calls on a Windows computer.
1. Enumerate audio devices on a Windows computer. 
1. A class for capturing from a microphone.
1. A class for sending audio media to the speakers or earphones.

This class library can be used with the [SipLib](https://github.com/PhrSite/SipLib) class library to build Voice over IP (VoIP) applications requiring audio support in the Windows environment. It may be used by applications or other class libraries on Windows 10, Windows 11 or Windows Server. 

# Documentation
The documentation pages are located at https://phrsite.github.io/SipLib.Audio.Windows. The documentation web site includes class documentation and articles that explain the usage of the classes in this library.

# Installation
This class library is available on NuGet.

To install it from the .NET CLI, type:

```
dotnet add package SipLib.Audio.Windows --version X.X.X
```
"X.X.X" is the version number of the packet to add.

To install using the NuGET Package Manager Command window, type:

```
NuGet\Install-Package SipLib.Audio.Windows --version X.X.X
```
Or, you can install it from the Visual Studio GUI.

1. Right click on a project
2. Select Manage NuGet Packages
3. Search for SipLib.Audio.Windows
4. Click on Install

# Dependancies
This project has direct dependencies on the following NuGet packages.

1. NAudio (2.2.1)
1. SipLib (0.0.4 or later)
