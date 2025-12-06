/////////////////////////////////////////////////////////////////////////////////////
//  File:   WindowsAudioIo.cs                                       10 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using SipLib.Media;
using SipLib.Logging;

namespace SipLib.Audio.Windows;

/// <summary>
/// Delegate type for the AudioInSamplesReady event of the WindowsAudioIo class.
/// </summary>
/// <param name="PcmSamples">Block of new samples from the microphone.</param>
public delegate void AudioInSamplesReadyDelegate(short[] PcmSamples);

/// <summary>
/// Delegate type for the AudioDeviceStateChanged event of the WindowsAudioIo class.
/// </summary>
/// <param name="Connected">If true then the audio I/O device is connected. Else, the audio I/O device
/// is disconnected or not available.</param>
public delegate void AudioDeviceStateChangedDelegate(bool Connected);

/// <summary>
/// This class uses the Windows Wave API to capture audio from a microphone and to send audio to the speakers or 
/// a headset. It is intended for use with Voice Over IP (VoIP) applications running under Windows.
/// </summary>
public class WindowsAudioIo : IMMNotificationClient, IAudioSampleSource
{
    private MMDeviceEnumerator? m_deviceEnumerator;
    private WaveOut? m_WaveOut = null;
    private WaveOutDest? m_WaveOutDest = null;
    private WaveIn? m_WaveIn = null;
    private WaveFormat? m_WaveFormat = null;
    private int m_SampleRate;
    private string? m_DeviceName;
    private int m_DeviceNumber;

    private const int BufferMilliseconds = 20;

    private const int BufferCount = 5;
    private const int DesiredLatencyMs = 20;

    private const int SendBuffersPerSecond = 50;

    private int m_ReceivedBufferCount = 0;
    private bool m_WaveOutStarted = false;

    private short[]? m_SendBuffer = null;
    private int m_SendBufferIndex = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="SampleRate">Sample rate in samples per second. Must be either 8000 or 16000.
    /// The default is 8000.</param>
    /// <param name="DeviceName">Specifies the audio device name as known by the Windows Wave API subsystem. The
    /// default is null. If null, then this class will pick the first available audio device.</param>
    public WindowsAudioIo(int SampleRate = 8000, string? DeviceName = null)
    {
        m_SampleRate = SampleRate;
        m_DeviceName = DeviceName;
    }

    /// <summary>
    /// Gets the configured sample rate in samples/second.
    /// </summary>
    /// <value></value>
    public int SampleRate
    {
        get { return m_SampleRate; }
    }

    /// <summary>
    /// This event is fired when the state of the Windows audio I/O device changes state.
    /// </summary>
    public event AudioDeviceStateChangedDelegate? AudioDeviceStateChanged = null;

    /// <summary>
    /// This event is fired when a full block of 20 milliseconds of audio data has been received from the microphone.
    /// Each sample point is a PCM sample (16-bit linear, 8000 or 16000 samples/second).
    /// </summary>
    public event AudioSamplesReadyDelegate? AudioSamplesReady = null;

    /// <summary>
    /// Sets up audio acquisition from the microphone and gets ready to start sending audio to the speakers or headset.
    /// </summary>
    /// <returns>Returns Success if no errors were detected or an error code. Audio is not started if an error code
    /// is returned.</returns>
    public WaveAudioStatusEnum StartAudio()
    {
        if (m_WaveIn != null)
            return WaveAudioStatusEnum.AlreadyStarted;

        if (m_SampleRate != 8000 && m_SampleRate != 16000)
            return WaveAudioStatusEnum.InvalidSampleRate;
        
        if (string.IsNullOrEmpty(m_DeviceName) == false)
        {
            m_DeviceNumber = GetDeviceNumber(m_DeviceName);
            if (m_DeviceNumber < 0)
                return WaveAudioStatusEnum.AudioDeviceNameNotFound;
        }
        else
        {   // The device name was not specified so pick the first available audio device
            List<string> DeviceNames = GetAudioDeviceNames();
            if (DeviceNames.Count == 0)
                return WaveAudioStatusEnum.NoAudioDevicesFound;

            m_DeviceName = DeviceNames[0];
            m_DeviceNumber = GetDeviceNumber(m_DeviceName);
        }

        m_deviceEnumerator = new MMDeviceEnumerator();
        m_deviceEnumerator.RegisterEndpointNotificationCallback(this);
        m_SendBuffer = new short[m_SampleRate / SendBuffersPerSecond];
        m_SendBufferIndex = 0;

        m_WaveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, m_SampleRate, 1, 
            m_SampleRate * 16, 2, 16);

        m_WaveIn = new WaveIn();
        m_WaveIn.WaveFormat = m_WaveFormat;
        m_WaveIn.BufferMilliseconds = BufferMilliseconds;
        m_WaveIn.NumberOfBuffers = BufferCount;
        m_WaveIn.DataAvailable += WaveIn_DataAvailable!;
        m_WaveIn.RecordingStopped += WaveIn_RecordingStopped!;
        m_WaveIn.DeviceNumber = m_DeviceNumber;
        m_WaveIn.StartRecording();

        m_ReceivedBufferCount = 0;
        m_WaveOutStarted = false;
        m_WaveOutDest = new WaveOutDest(m_WaveFormat);
        m_WaveOut = new WaveOut();
        m_WaveOut.NumberOfBuffers = BufferCount;
        m_WaveOut.DesiredLatency = DesiredLatencyMs;
        m_WaveOut.Volume = (float)1.0;
        m_WaveOut.DeviceNumber = m_DeviceNumber;
        m_WaveOut.Init(m_WaveOutDest);

        return WaveAudioStatusEnum.Success;
    }

    /// <summary>
    /// Event handler for the DataAvailable event of the m_WaveIn object.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        int SampleCount = e.BytesRecorded / 2;      // Each sample is 2 bytes
        int SrcIdx = 0;
        short WordSample;

        // It is possible that the m_WaveIn object will send more or less than the desired block size
        // of 20 milliseconds worth of data. So process all samples received and when a full block of 20
        // milliseconds of data is ready, pass it to the user of this object by firing the SendAudioSamples
        // event.
        for (int i = 0; i < SampleCount; i++)
        {
            WordSample = BitConverter.ToInt16(e.Buffer, SrcIdx);
            SrcIdx += 2;
            m_SendBuffer![m_SendBufferIndex++] = WordSample;
            if (m_SendBufferIndex >= m_SendBuffer.Length)
            {   // The send buffer is full so send it
                short[] SendArray = new short[m_SendBuffer.Length];
                Array.Copy(m_SendBuffer, SendArray, SendArray.Length);

                AudioSamplesReady?.Invoke(SendArray, m_SampleRate);

                Array.Clear(m_SendBuffer);
                m_SendBufferIndex = 0 ;
            }
        }
    }

    private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
    {

    }

    /// <summary>
    /// Sends the received PCM samples to the output device (speakers or headset).
    /// </summary>
    /// <param name="PcmSamples">Received PCM samples (16-bit linear, 8000 or 16000 samples per  second.</param>
    public void AudioOutSamplesReady(short[] PcmSamples)
    {
        if (m_WaveOut == null || PcmSamples == null || PcmSamples.Length == 0 || m_WaveOutDest == null)
            return;

        byte[] PcmBytes = new byte[PcmSamples.Length * 2];
        int PcmIndex = 0;
        for (int i = 0; i < PcmSamples.Length; i++)
        {
            Array.ConstrainedCopy(BitConverter.GetBytes(PcmSamples[i]), 0, PcmBytes, PcmIndex, 2);
            PcmIndex += 2;
        }

        m_WaveOutDest.QueueSampleBlock(PcmBytes);

        m_ReceivedBufferCount += 1;
        if (m_WaveOutStarted == false && m_ReceivedBufferCount > BufferCount)
        {
            try
            {
                m_WaveOut.Play();
                m_WaveOutStarted = true;
            }
            catch (Exception Ex)
            {
                SipLogger.LogError(Ex, "Unable to start the m_WaveOut object");
            }
        }
    }

    /// <summary>
    /// Gets a list of available Wave API audio devices.
    /// </summary>
    /// <returns>Returns a list of device names. The list will be empty if there are no audio devices
    /// available.</returns>
    public static List<string> GetAudioDeviceNames()
    {
        List<string> DevNames = new List<string>();
        int NumDevices = WaveIn.DeviceCount;
        for (int i = 0; i < NumDevices; i++)
        {
            WaveInCapabilities Wc = WaveIn.GetCapabilities(i);
            DevNames.Add(Wc.ProductName);
        }

        return DevNames;
    }

    private int GetDeviceNumber(string DeviceName)
    {
        int DevNumber = -1;

        if (string.IsNullOrEmpty(DeviceName) == true)
            return DevNumber;

        int NumDevices = WaveIn.DeviceCount;
        for (int i = 0; i < NumDevices; i++)
        {
            WaveInCapabilities Wc = WaveIn.GetCapabilities(i);
            if (DeviceName == Wc.ProductName)
            {
                DevNumber = i;
                break;
            }

        }

        return DevNumber;
    }

    /// <summary>
    /// Call this method to stop the audio and gracefully shutdown.
    /// </summary>
    public void StopAudio()
    {
        if (m_WaveIn != null)
        {
            try
            {
                m_WaveIn.StopRecording();
            }
            catch (MmException) { }
            catch (Exception) { }

            m_WaveIn = null;
        }

        try
        {
            if (m_WaveOut != null)
            {
                m_WaveOut.Stop();
                m_WaveOut = null;
                m_WaveOutDest = null;
            }
        }
        catch (Exception) { }

        if (m_deviceEnumerator != null)
        {
            m_deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            m_deviceEnumerator = null;
        }
    }

    /// <summary>
    /// IMMNotificationClient interface method. Called by the Windows multi-media subsystem when the 
    /// audio device state changes. The user of this WindowsAudioIo object should never call this method.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="newState"></param>
    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (newState == DeviceState.NotPresent || newState == DeviceState.Unplugged)
            AudioDeviceStateChanged?.Invoke(false);
        else if (newState == DeviceState.Active)
            AudioDeviceStateChanged?.Invoke(true);
    }

    /// <summary>
    /// IMMNotificationClient interface method. Called by the Windows multi-media subsystem when an
    /// audio device has been added to the system. The user of this WindowsAudioIo object should never
    /// call this method.
    /// </summary>
    /// <param name="pwstrDeviceId"></param>
    public void OnDeviceAdded(string pwstrDeviceId)
    {
        
    }

    /// <summary>
    /// IMMNotificationClient interface method. Called by the Windows multi-media subsystem when an
    /// audio device has been removed from the system. The user of this WindowsAudioIo object should never
    /// call this method.
    /// </summary>
    /// <param name="deviceId"></param>
    public void OnDeviceRemoved(string deviceId)
    {
        
    }

    /// <summary>
    /// IMMNotificationClient interface method. Called by the Windows multi-media subsystem when the
    /// default audio device has changed. The user of this WindowsAudioIo object should never
    /// call this method.
    /// </summary>
    /// <param name="flow"></param>
    /// <param name="role"></param>
    /// <param name="defaultDeviceId"></param>
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        
    }

    /// <summary>
    /// IMMNotificationClient interface method. Called by the Windows multi-media subsystem when a
    /// property of an audio device has changed. The user of this WindowsAudioIo object should never
    /// call this method.
    /// </summary>
    /// <param name="pwstrDeviceId"></param>
    /// <param name="key"></param>
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        
    }

    public void Start()
    {   // No action required
    }

    public void Stop()
    {   // No action required
    }
}

/// <summary>
/// Enumeration for the values returned by the StartAudio method of the WindowsAudioIo class.
/// </summary>
public enum WaveAudioStatusEnum
{
    /// <summary>
    /// Success
    /// </summary>
    Success,

    /// <summary>
    /// No Wave API audio devices are available
    /// </summary>
    NoAudioDevicesFound,

    /// <summary>
    /// The specified Wave API audio device is not available
    /// </summary>
    AudioDeviceNameNotFound,

    /// <summary>
    /// The specified sample rate is not valid. Valid sample rates are 8000 or 16000 samples per second.
    /// </summary>
    InvalidSampleRate,

    /// <summary>
    /// Audio has already been started.
    /// </summary>
    AlreadyStarted,
}
    
