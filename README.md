# StreamerWinui
![image](https://github.com/CrayzyCray/StreamerWinui/assets/85997602/9d93167d-ef88-460f-9132-e203a8222ee8)
### in the future I plan to add the ability to stream the desktop and view it on many clients
now program can record audio only from devices with 48000 Hz sample rate
In most cases sample rate already sets to 48k.
if its not you case, you need to set 48kHz sample rate in 
```
Settings > System > Sound > *Click on device
```
# Building
## 1. Dependencies
for successful build you need to put ffmpeg 6.0 dlls in StreamerLib/DLLs folder.
you may download 
```
ffmpeg-release-full-shared.7z
```
from here https://www.gyan.dev/ffmpeg/builds/ and extract bin folder to StreamerLib/DLLs.
## 2. Script
also you need to run 
```
build.ps1
```
script from StreamerWinui/libutil/ to build libutil.dll.
