#!/usr/bin/osascript

on run argv
   set debuggerTitle to (item 1 of argv)
   set executeCommand to (item 2 of argv)
   # Note: if other tabs are open on the terminal window that is opened by this script, this won't behave properly.
   set command to "clear; " & ¬
                  executeCommand & ¬
                  "osascript -e 'tell application \"Terminal\" to close (every window whose tty is \"'\"$(tty)\"'\")' & exit"

    tell application "Terminal"
        if it is running then
            activate
            set newTab to do script command
        else
            activate
            set newTab to do script command in window 1
        end if

        set custom title of newTab to debuggerTitle
    end tell
end run
