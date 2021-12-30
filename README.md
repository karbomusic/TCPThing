# TCPThing

This app is for testing various TCP connections and socket options and/or various TCP connection based testing. Currently only the TCP keep-alive socket option (in minutes) is configurable via comand line arguments. It needs some refactoring and cleanup but it's something I modify as needed since it's just a test tool for corner-case issues.

Ideally you can run as as a client on machine A and a server (listener) on machine B, or the same machine for that matter and connect client to server. Set server or client behavior by modifying the 3rd argument (1=server or 0=client). Or you can just use a single instance as a client to connect to the endpoint of your choice. You could also run it as a server, then connnect from another endpoint using telnet. TCPThing will attempt to perform a DNS lookup if a hostname is provided instead of an IP address.

#### USAGE: **TCPThing &lt;IPAddress | Hostname&gt; &lt;TCPPort&gt; &lt;IsServer {0|1}&gt; [KeepAlive {0=SystemSetting | UserValue}]** 
  
  ### Examples  

  Create a listener on port 80: **TCPThing 127.0.0.1 80 1 0**  
  Create a client on port 80: **TCPThing 127.0.0.1 80 0 0**    
  Create a listener w/keep-alive on port 50000: **TCPThing 127.0.0.1 5000 1 1**  
  Create a client w/keep-alive of 1 minutes: **TCPThing 127.0.0.1 80 0 1**  
  Create a client w/keep-alive of 30 minutes: **TCPThing 127.0.0.1 80 0 30**  
  Create a server w/keep-alive of 4 minutes on port 8081: **TCPThing 127.0.0.1 8081 1 4**  
  Create a client on port 80 w/keep-alive using system default values: **TCPThing 127.0.0.1 80 0 0**     
  <br>
      
  ![Example Image](./example.png)   
 
Note: When running as a server on a remote machine, you must ensure the port used is open in the firewall. This app does not pass any data or packets. It's essentially a glorified TCP Handshake creation tool with the ability to set keep-alive time. See comments at the top of program.cs for additional usage information.

Binaries: You can download the binaries, instead of building, from the /bin/release folder.

