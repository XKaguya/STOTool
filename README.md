# STO-Tool

A tool for checking the server status of Star Trek Online written in C#.

![img](https://github.com/XKaguya/STOTool/assets/96401952/02eaa90d-a557-43be-a7fd-434c24c395a6)

# Features
## Fast
By using Cache, The program will response with specific information less than 5 sec.
## Initiative Ability
* Real-time news display
  
* Real-time maintenance information display

* Real-time events display

## Passive Ability
* Real-time server status monitoring
* Ability to connect with any client using a pipe server / websocket server to send information
* Automaticly send the newest news screenshot

# Usage
1. Register for a Google Calendar API key at [Google API](https://console.cloud.google.com/apis/credentials).

2. Download the credentials JSON file and place it near the executable file, like this:

![image](https://github.com/user-attachments/assets/c52f22f3-4b31-49aa-83ba-5673f2bd161e)


# Settings
```xml
<Settings>
  <!--Cache life time in minute. -->
  <!--Default value: 15, 10, 1-->
  <CacheLifeTime>15,10,1</CacheLifeTime>
  <!--Program Level. -->
  <!--Default value: Normal-->
  <ProgramLevel>Normal</ProgramLevel>
  <!--Log level. -->
  <!--Default value: Info-->
  <LogLevel>Debug</LogLevel>
  <!--Whether or not enable the Legacy Pipe Server instead of the Websocket Server. -->
  <!--Default value: False-->
  <LegacyPipeMode>False</LegacyPipeMode>
  <!--WebSocket Listener address. -->
  <!--Default value: http://localhost-->
  <WebSocketListenerAddress>http://localhost</WebSocketListenerAddress>
  <!--WebSocket Listener port. -->
  <!--Default value: 9500-->
  <WebSocketListenerPort>9500</WebSocketListenerPort>
</Settings>
```

# QQ Client Plugin
[Client Plugin](https://github.com/XKaguya/zhenxun_STO_ServerChecker)

![image](https://github.com/XKaguya/STOTool/assets/96401952/a71fbe08-9f74-43c2-90ed-d594a9ec91f6)

**Currently does not support other platforms. Contributions are welcome.**



