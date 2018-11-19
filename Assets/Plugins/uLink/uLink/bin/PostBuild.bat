@echo off

rem This is like a switch case block:

if not "%1"=="" goto %1
goto :eof

:Release

	Eazfuscator.NET.exe uLink.dll -n --newline-flush

:Debug

	copy /Y uLink.dll "..\..\..\Packager\Assets\Plugins\uLink\Assembly\"
	copy /Y uLink.xml "..\..\..\Packager\Assets\Plugins\uLink\Assembly\"

	goto :eof

:Test

	rem Do nothing

	goto :eof
