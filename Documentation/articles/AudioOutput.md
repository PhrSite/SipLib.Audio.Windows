# Sending Audio to the Speakers or Headset
Windows applications can use the WaveAudioIo class to send blocks of 16-bit linear samples to the speakers or the headset by performing the following steps.
1. Construct an instance of the WindowsAudioIo class.
1. Call the StartAudio() method of the WindowsAudioIo object.
1. Call the AudioOutSamplesReady() method of the WindowsAudioIo object every time 20 milliseconds worth of audio data has been received from the IP network.

If you are using the same audio device for input and output of audio, then you must use the same instance of the WindowsAudioIo class for both receiving (capturing) and sending audio samples to the speakers or the headset. See [Capturing Audio](~/articles/CapturingAudio.md).

The declaration of the constructor for the WindowsAudioIo class is:
```
public WindowsAudioIo(int SampleRate = 8000, string? DeviceName = null);
```

The SampleRate parameter must be either 8000 or 16000 samples per second. The DeviceName parameter must be one of the names returned by the GetAudioDeviceNames() method or null. If the DeviceName parameter is null, then the WaveAudioIo class will use the first audio device that it finds when the StartAudio() method is called.

The declaration of the AudioOutSamplesReady() method is:
```
 public void AudioOutSamplesReady(short[] PcmSamples);
```

The PcmSamples array must contain 20 milliseconds of 16-bit linear audio data. The length of this array must be 160 if the SamplerRate parameter specified in the constructor was 8000 samples/second or 320 if the specified sample rate was 16000 samples/second.

The application must call the StopAudio() method when it no longer needs to send audio data to the audio device.
