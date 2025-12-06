# Monitoring Audio Device Status
The WindowsAudioIo class has an event called AudioDeviceStateChanged that allows an application to monitor the status of the Windows PC's audio device that it is using.

The delegate declaration of this event is:
```
public delegate void AudioDeviceStateChangedDelegate(bool Connected);
```

If Connected is false then the audio device was disconnected. If Connected is true then the audio device was connected.

This event is fired only when the status of the audio device changes so it will not be fired initially if the audio device is connected when the StartAudio() method of the WaveAudioIo object is called.
