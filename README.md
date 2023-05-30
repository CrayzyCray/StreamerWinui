# Building
## 1. Dependencies
for successful build you need to put ffmpeg 6.0 dlls in StreamerLib/DLLs folder.
you may download ffmpeg-release-full-shared.7z from here https://www.gyan.dev/ffmpeg/builds/ and extract bin folder to StreamerLib/DLLs.
## 2. Script
also you need to run build.ps1 script from StreamerWinui/libutil/ to build libutil.dll.
# StreamerWinui
### now program can record audio only from devices with 48000 Hz sample rate
In most cases sample rate already sets to 48k.
if its not you case, you need to set 48kHz sample rate in System > Sound > *Click on device
