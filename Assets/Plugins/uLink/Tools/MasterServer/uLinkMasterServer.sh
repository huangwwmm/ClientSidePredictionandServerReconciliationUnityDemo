#!/bin/sh
type -P mono &>/dev/null || { 
	echo "ABORTING: The application needs the Mono framework to run, but it does not seem to be installed on your system." 
>&2
	exit 1
}
mono uLinkMasterServer.exe $*
