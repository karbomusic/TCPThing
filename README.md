# TCPThing

This app is for testing various TCP connections and socket options and/or various TCP connection based testing. Right now only TCP keep-alive is configurable by passing in arguments. It needs some refactoring, cleanup and better coding but it's something I modify as needed since it's just a test tool for corner-case issues.

  Ideally you can run as as a client on machine A and a server on machine B, or the same machine for that matter and connect client to server. Just set the 3rd argument to toggle (1=server, 0=client)

#### USAGE: **TCPThing &lt;IPAddress&gt; &lt;TCPPort&gt; &lt;IsServer {0|1}&gt; [KeepAlive]** 
  
  Create a listener: **TCPThing 127.0.0.1 80 1 0**  
  Create a client: **TCPThing 127.0.0.1 80 0 0**    
  Create a listener w/keep-alive: **TCPThing 127.0.0.1 80 1 1**  
  Create a client w/keep-alive: **TCPThing 127.0.0.1 80 0 1**  
  Create a client w/keep-alive of 30 seconds: **TCPThing 127.0.0.1 80 0 30**  
  Create a server w/keep-alive of 1 minute: **TCPThing 127.0.0.1 80 1 60**  
  Create a client w/keep-alive system default values: **TCPThing 127.0.0.1 80 0 0**   
  
  
  Note: When running as a server on a remote machine, you must ensure the port used is open in the firewall. This app also does not pass any data or packets. It's essentially a glorified TCP Handshake creation tool with the ability to set keep-alive time.
