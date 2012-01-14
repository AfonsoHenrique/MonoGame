#region License
/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Xna.Framework.Audio
{
    public class WaveBank
    {
        internal SoundData[] mSounds;
        internal string BankName = "";
        private const int OWB_VERSION = 4;

        private int mSoundReadIdx;
        private long[] mSizes = null;
        private FileStream mFS;
        private bool mIsLoaded = false;
        public bool IsLoaded { get { return mIsLoaded; } }
		internal AudioEngine mAudioEngine;
	
		public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename)
		{
#if !NO_FMOD
            int start = nonStreamingWaveBankFilename.LastIndexOf('/') + 1;
            BankName = nonStreamingWaveBankFilename.Substring(start, nonStreamingWaveBankFilename.Length - start - 4);

            audioEngine.Wavebanks.Add(this);
			mAudioEngine = audioEngine;

            mFS = new FileStream( nonStreamingWaveBankFilename, FileMode.Open, FileAccess.Read );

            byte[] buf = new byte[8];
            mFS.BeginRead(buf, 0, buf.Length, delegate(IAsyncResult result)
            {
                int bytesRead;
                try
                {
                    bytesRead = mFS.EndRead(result);
                }
                catch (System.IO.FileNotFoundException)
                {
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(bytesRead == 8, "Didn't read 8 bytes");
                System.IO.BinaryReader reader = new System.IO.BinaryReader(
                    new System.IO.MemoryStream(buf, false));
                int version = reader.ReadInt32();
                // anything else would be uncivilized -- run oggAct to correct this error
                if (version != OWB_VERSION)
                {
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(version == OWB_VERSION, "rebuild your wave bank with oggact");
                int count = reader.ReadInt32();

                int sizeHeaderLength = 8 * count;
                byte[] buf2 = new byte[sizeHeaderLength];
                mFS.BeginRead(buf2, 0, buf2.Length, delegate(IAsyncResult result2)
                {
                    int bytesRead2 = mFS.EndRead(result2);
                    Debug.Assert(bytesRead2 == buf2.Length,
                        String.Format("{0} != {1}", buf2.Length, bytesRead2));
                    System.IO.BinaryReader reader2 = new System.IO.BinaryReader(
                        new System.IO.MemoryStream(buf2, false));
                    mSizes = new long[count];
                    for (int i = 0; i < count; i++)
                    {
                        mSizes[i] = reader2.ReadInt64();
                    }

                    mSoundReadIdx = 0;
                    mSounds = new SoundData[count];
                    continueReadingSounds();
                }, null);
            }, null);
#endif		
		}
		
        public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetSize)
        {
#if !NO_FMOD
            int start = streamingWaveBankFilename.LastIndexOf('/') + 1;
            BankName = streamingWaveBankFilename.Substring(start, streamingWaveBankFilename.Length - start - 4);

            audioEngine.Wavebanks.Add(this);
			mAudioEngine = audioEngine;

            mFS = new FileStream( streamingWaveBankFilename, FileMode.Open, FileAccess.Read );
            byte[] buf = new byte[8];
            mFS.BeginRead(buf, 0, buf.Length, delegate(IAsyncResult result)
            {
                int bytesRead;
                try
                {
                    bytesRead = mFS.EndRead(result);
                }
                catch (System.IO.FileNotFoundException)
                {
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(bytesRead == 8, "Didn't read 8 bytes");
                System.IO.BinaryReader reader = new System.IO.BinaryReader(
                    new System.IO.MemoryStream(buf, false));
                int version = reader.ReadInt32();
                // anything else would be uncivilized -- run oggAct to correct this error
                if (version != OWB_VERSION)
                {
                   	Console.WriteLine("rebuild your wave bank with oggact");
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(version == OWB_VERSION, "rebuild your wave bank with oggact");
                int count = reader.ReadInt32();

                int sizeHeaderLength = 8 * count;
                byte[] buf2 = new byte[sizeHeaderLength];
                mFS.BeginRead(buf2, 0, buf2.Length, delegate(IAsyncResult result2)
                {
                    int bytesRead2 = mFS.EndRead(result2);
                    Debug.Assert(bytesRead2 == buf2.Length,
                        String.Format("{0} != {1}", buf2.Length, bytesRead2));
                    System.IO.BinaryReader reader2 = new System.IO.BinaryReader(
                        new System.IO.MemoryStream(buf2, false));
                    mSizes = new long[count];
                    for (int i = 0; i < count; i++)
                    {
                        mSizes[i] = reader2.ReadInt64();
                    }

                    mSoundReadIdx = 0;
                    mSounds = new SoundData[count];

                    for (int i = 0; i < count; i++)
                    {
                        // hard coded constant in Sound.cs, change it if you change the max size
                        Debug.Assert(mSizes[i] <= 5570141, "mSizes[i] <= 5570141");
                        mSounds[i] = new SoundData(i, mSizes[i], false);
                    }

                    mIsLoaded = true;
                    mFS.Close();
                    mFS = null;
                }, null);
            }, null);
#endif
        }

        private void continueReadingSounds()
        {
            if (mSoundReadIdx >= mSounds.Length)
            {
                mIsLoaded = true;
                mFS.Close();
                mFS = null;
                return;
            }

            long sz = mSizes[mSoundReadIdx];
            if (sz > 0)
            {
                byte[] bytes = new byte[sz];
                mFS.BeginRead(bytes, 0, (int)sz, soundReadCallback, bytes);
            }
            else
            {
                // doesn't happen anymore, if you get this, rebuild your local osb/owbs
                throw new NotImplementedException();
            }        
        }

        private void soundReadCallback(IAsyncResult result)
        {
            int bytesRead = mFS.EndRead(result);
            Debug.Assert(bytesRead == mSizes[mSoundReadIdx], 
                    String.Format("{0} != {1}", bytesRead, mSoundReadIdx));

            byte[] bytes = (byte[])result.AsyncState;

            mSounds[mSoundReadIdx] = new SoundData(bytes);
            mSoundReadIdx += 1;
            continueReadingSounds();
        }

		/*
        public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, bool streamToPersist)
        {
#if !NO_FMOD
            int start = streamingWaveBankFilename.LastIndexOf('/') + 1;
            BankName = streamingWaveBankFilename.Substring(start, streamingWaveBankFilename.Length - start - 4);

            audioEngine.Wavebanks.Add(this);

            mFS = new FileStream(streamingWaveBankFilename,
                System.IO.FileMode.Open);
            byte[] buf = new byte[8];
            mFS.BeginRead(buf, 0, buf.Length, delegate(IAsyncResult result)
            {
                int bytesRead;
                try
                {
                    bytesRead = mFS.EndRead(result);
                }
                catch (System.IO.FileNotFoundException)
                {
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(bytesRead == 8, "Didn't read 8 bytes");
                System.IO.BinaryReader reader = new System.IO.BinaryReader(
                    new System.IO.MemoryStream(buf, false));
                int version = reader.ReadInt32();
                // anything else would be uncivilized -- run oggAct to correct this error
                if (version != OWB_VERSION)
                {
                    mFS.Close();
                    mFS = null;
                    return;
                }
                Debug.Assert(version == OWB_VERSION, "rebuild your wave bank with oggact");
                int count = reader.ReadInt32();

                int sizeHeaderLength = 8 * count;
                byte[] buf2 = new byte[sizeHeaderLength];
                mFS.BeginRead(buf2, 0, buf2.Length, delegate(IAsyncResult result2)
                {
                    int bytesRead2 = mFS.EndRead(result2);
                    Debug.Assert(bytesRead2 == buf2.Length,
                        String.Format("{0} != {1}", buf2.Length, bytesRead2));
                    System.IO.BinaryReader reader2 = new System.IO.BinaryReader(
                        new System.IO.MemoryStream(buf2, false));
                    mSizes = new long[count];
                    for (int i = 0; i < count; i++)
                    {
                        mSizes[i] = reader2.ReadInt64();
                    }

                    mSoundReadIdx = 0;
                    mSounds = new SoundData[count];

                    for (int i = 0; i < count; i++)
                    {
                        // hard coded constant in Sound.cs, change it if you change the max size
                        Debug.Assert(mSizes[i] <= 5570141, "mSizes[i] <= 5570141");
                        mSounds[i] = new SoundData(i, mSizes[i], streamToPersist);
                    }

                    mIsLoaded = true;
                    mFS.Close();
                    mFS = null;
                }, null);
            }, null);
#endif
        }
        */

        public bool IsDisposed
        {
            get { return IsDisposed; }
        }

        public bool IsInUse
        {
            get { return IsInUse; }
        }

        public bool IsPrepared
        {
            get { return IsPrepared; }
        }

        public void Dispose()
        {
            if (mSounds != null)
            {
                for (uint i = 0; i < mSounds.Length; i++)
                {
                    mSounds[i].Dispose();
                    mSounds[i] = null;
                }
            }
            mSounds = null;

            mAudioEngine.Wavebanks.Remove(this);
        }

        // release our hooks so that we might build a better fmod environment
        public void releaseAll()
        {
            if (mSounds != null)
            {
                for (uint i = 0; i < mSounds.Length; i++)
                {
                    mSounds[i].releaseSound();
                }
            }
        }

        public void Dispose(Boolean disposing)
        {
        	throw new NotImplementedException();
        }

    }
}

