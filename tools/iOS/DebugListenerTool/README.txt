This is a simple tool for communicating directly with the Debug Listener running on mac. 
It provides a cli to lldb-mi that gets started from vcremote.
To use:

1. Set the hostname and port numbers correctly in the source code.
2. Make sure that vcremote is running and that the debug listener is running (I use POSTMAN in Chrome to send the get requests manually)
3. Run DebugListenerTool and you should see the output of lldb-mi from the mac.
