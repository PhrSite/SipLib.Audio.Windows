# Enumerating Audio Devices
The static GetAudioDeviceNames() method of the WindowsAudioIo class enumerates the available audio devices on a Windows PC. The declaration of this method is:
```
public static List<string> GetAudioDeviceNames()
```
This method returns a list containing the names of the audio devices that are available. The list will be empty if there are no audio devices available.

The application can present this list to the application user and it can store the user's choice in the application configuration settings and use that device name the next time that it starts using the audio device.

If operating in an environment where it is safe to assume there is always a single device then the application can call this method and always use the first audio device name in the returned list.

