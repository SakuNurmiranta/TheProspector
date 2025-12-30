@echo off
set EXE=C:\Path\To\SEMM91dev.exe "C:\TheProspector\TheProspector\Builds\Networking_Demo_Test\TheOneBuild\SEMM91dev.exe"
set IP=16.170.122.234
set PORT=7777

start "" "%EXE%" -mode=client -connect=%IP% -port=%PORT% -bot=1 -logFile bot1.log -simDelayMs=0  -simJitterMs=0  -simDropPct=0
start "" "%EXE%" -mode=client -connect=%IP% -port=%PORT% -bot=1 -logFile bot2.log -simDelayMs=50 -simJitterMs=10 -simDropPct=1
start "" "%EXE%" -mode=client -connect=%IP% -port=%PORT% -bot=1 -logFile bot3.log -simDelayMs=80 -simJitterMs=20 -simDropPct=2
start "" "%EXE%" -mode=client -connect=%IP% -port=%PORT% -bot=1 -logFile bot4.log -simDelayMs=120 -simJitterMs=30 -simDropPct=3
start "" "%EXE%" -mode=client -connect=%IP% -port=%PORT% -bot=1 -logFile bot5.log -simDelayMs=200 -simJitterMs=50 -simDropPct=5
