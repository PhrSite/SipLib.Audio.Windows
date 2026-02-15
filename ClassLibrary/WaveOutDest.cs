/////////////////////////////////////////////////////////////////////////////////////
//	File:		WaveOutDest.cs									    10 Oct 23 PHR
//////////////////////////////////////////////////////////////////////////////////////

using NAudio.Wave;

namespace SipLib.Audio.Windows;

/// <summary>
/// Class that implements the NAudio IWaveProvider interface for the Wave API for the NAudio class library.
/// Applications that use class library do not normally need to use this class.
/// </summary>
public class WaveOutDest : IWaveProvider
{
    private WaveFormat? m_Wfmt = null;
    private object m_LockObj = new object();
    private Queue<byte[]> m_SampleBlockQueue = new Queue<byte[]>();

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="Wfmt">WaveFormat object containing the format information for the audio samples.</param>
    public WaveOutDest(WaveFormat Wfmt)
    {
        m_Wfmt = Wfmt;
    }

    /// <summary>
    /// Adds a block of samples to the sample block queue. These samples will be sent to the wave output
    /// when the wave API asks for more samples. Each sample block will contain 20 ms worth of audio. 
    /// Each sample is 2 bytes long.
    /// </summary>
    /// <param name="SampleBlock">Byte array containing the next block of audio samples.</param>
    public void QueueSampleBlock(byte[] SampleBlock)
    {
        lock (m_LockObj)
        {
            m_SampleBlockQueue.Enqueue(SampleBlock);
        }
    }

    //////////////////////////////////////////////////////////////////////////////
    // IWaveProvider interface methods and properties.
    //////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Called when the WAVE API needs bytes to play to the output.
    /// </summary>
    /// <param name="buffer">Location to put the new sample bytes.</param>
    /// <param name="offset">Offset into buffer for the new sample bytes.</param>
    /// <param name="count">Number of bytes to write into buffer. Note: Each sample is two bytes long.</param>
    /// <returns>Returns the number of bytes actually written into buffer.</returns>
    public int Read(byte[] buffer, int offset, int count)
    {
        int RetVal = 0;

        lock (m_LockObj)
        {
            byte[]? Samples = null;
            int CurOffset = offset;
            int CopyLength = 0;

            if (m_SampleBlockQueue.Count == 0)
            {   // The input queue is empty. The wave API needs to get something or else it just stops
                // working so send it some silence.
                Samples = new byte[count];
                Array.Clear(Samples, 0, count);
                Array.Copy(Samples, 0, buffer, CurOffset, count);
                CurOffset = CurOffset + Samples.Length;
                RetVal = Samples.Length;
            }
            else
            {
                while (m_SampleBlockQueue.Count > 0 && RetVal < count)
                {
                    Samples = m_SampleBlockQueue.Dequeue();
                    CopyLength = Samples.Length;
                    if ((CurOffset + CopyLength) > buffer.Length)
                        CopyLength = buffer.Length - CurOffset;
                    Array.Copy(Samples, 0, buffer, CurOffset, CopyLength);
                    CurOffset = CurOffset + CopyLength;
                    RetVal += CopyLength;
                }
            }
        }

        return RetVal;
    }

    /// <summary>
    /// Gets the WaveFormat object.
    /// </summary>
    public WaveFormat? WaveFormat
    {
        get { return m_Wfmt; }
    }
}

