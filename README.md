## Chaber
## Remote control Desktop Windows over SSH gateway
#### Source code C # / VisualStudio 2013 CE

Manage your windows desktop over the Internet. You for your infrastructure deploy ssh server and manage your remote stations (desktops). The idea is that when a large number of small branches, units, etc. VPN tunnels are not always installed with them. But to manage these machines there is always a need (through NAT), you deploy a gateway.

The basis is Vnc through the SSH reverse tunnel.

### The program consists of 2 parts:
1. Client part.
2. Gateway, SSH server, with the ability of GatewayPorts
And the part of the administrator, viewer, acts any VNC viewer (I prefer TigthVNC), but it does not matter.

### How to deploy:
Install on any Linux distribution, ssh server.

Settings are set from gateway / ssh_settings
```ssh
In /portable/client/
	ClickHelp.zip files /
		..hookldr.exe
		..sas.dll
		..screenhooks32.dll
		..screenhooks64.dll
		..server.ini
		..vncuser.pem
		..WinVNC.exe
	ClickHelp.exe
	Renci.SshNet.dll
```
where in server.ini it is necessary to set (change) the parameters:

- SSHServer = 111.11.11.11 - gateway address with ssh server
- SSHServerPort = 443 - port on which the ssh server listen
- LocalVncPortControl = 2211 - local port on the machine being started
- MinRandomPort = 40000 - port (start) for the generation that will be used for the ssh tunnel
- MaxRandomPort = 45550 - port (final)

You pack the archive and send it to the client, or place it for downloading on your internal resource. The client part of the portable extract the archive to any place on the disk.

The client unpacks the archive.

Running ClickHelp.exe (the first, and only the first, must be mandatory from the Administrator)

The client reports the ID from the form
Connect to the client by means of any VNC viewer - IPSshServer: Port, the connection port is the last 5 digits of the password.
When prompting for a password, you must enter the full password named by the customer.

### License
MIT
