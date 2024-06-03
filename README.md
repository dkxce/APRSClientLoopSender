# APRSClientLoopSender

Simple Beacon APRS Sender. Looped send any APRS data to any APRS-IS servers.     

Features:   
- Allow Multiple Messages
- Allow Multiple Tasks
- Task Sheduler
- Config via XML

```
<?xml version="1.0" encoding="utf-8"?>
<XMLConfig>
  <servers>
    <server on="false" priority="9999" sever="localhost" port="14580" user="UNKNOWN" pass="-1" filter="p/R/U" ping="APRS,TCPIP*:>online" readIncomingPackets="false" />
  </servers>
  <tasks>
    <task on="false" fromTime="00:00:00" tillTime="23:59:59" fromDate="2024-06-03T00:00:00+03:00" tillDate="2034-06-03T00:00:00+03:00" intervalSeconds="90">
		SFROM>STO,WIDE1-1,WIDE2-2:Simple Text
		SFROM>STO,WIDE1-1,WIDE2-2:>Simple Status
		SFROM>STO,WIDE1-1,WIDE2-2::TOALL    :Message. Hello All (I'm a teapot)
	</task>
  </tasks>
</XMLConfig>
```

<img src="window.png"/>