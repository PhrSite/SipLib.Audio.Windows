# Capturing Audio
Windows applications can use the WindowsAudioIo class to capture audio from a selected audio device such as the microphone of a headset by performing the following steps.
1. Construct an instance of the WindowsAudioIo class.
1. Hook the AudioSamplesReady event
1. Call the StartAudio() method of the WindowsAudioIo object.

If you are using the same audio device for input and output of audio, then you must use the same instance of the WindowsAudioIo class for both receiving (capturing) and sending audio samples to the speakers or the headset. See [Sending Audio to the Speakers or Headset](~/articles/AudioOutput.md).

When the application needs to stop receiving audio, it should perform the following steps.
1. Unhook the AudioSamplesReady event.
1. Call the StopAudio() method of the WindowsAudioIo object.

The declaration of the constructor for the WindowsAudioIo class is:
```
public WindowsAudioIo(int SampleRate = 8000, string? DeviceName = null);
```

The SampleRate parameter must be either 8000 or 16000 samples per second. The DeviceName parameter must be one of the names returned by the GetAudioDeviceNames() method or null. If the DeviceName parameter is null, then the WaveAudioIo class will use the first audio device that it finds when the StartAudio() method is called.

The StartAudio() method returns a [WaveAudioStatusEnum](~/api/SipLib.Audio.Windows.WaveAudioStatusEnum.yml) value to indicate success or failure. Return values of WaveAudioStatusEnum.Success and WaveAudioStatusEnum.AlreadyStarted indicate that audio sampling has been successfully started.

The delegate type for the AudioSamplesReady event is:
```
public delegate void AudioInSamplesReadyDelegate(short[] PcmSamples);
```

The PcmSamples parameter will contain 160 linear PCM 16-bit audio samples if the SampleRate specified in the constructor was 8000 or 320 samples if the specified sample rate was 16000. This event is fired every 20 milliseconds.

