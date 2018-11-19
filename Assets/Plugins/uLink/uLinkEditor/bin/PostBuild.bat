@echo off

rem This is like a switch case block:

if not "%1"=="" goto %1
goto :eof

:Release

	Eazfuscator.NET.exe uLinkEditor.dll -n --newline-flush

:Debug

	copy /Y uLinkEditor.dll "..\..\..\Packager\Assets\Plugins\Editor\uLink\Assembly\"

	goto :eof

:Test

	rem Do nothing

	goto :eof
