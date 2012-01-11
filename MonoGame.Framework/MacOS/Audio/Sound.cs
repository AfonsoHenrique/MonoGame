/*
	Sound.cs
	 
	Author:
	      Christian Beaumont chris@foundation42.org (http://www.foundation42.com)
	
	Copyright (c) 2009 Foundation42 LLC
	
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Media;


using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
//using System.Data

namespace Microsoft.Xna.Framework.Audio
{
    #pragma warning disable 0472

    internal class SoundData
    {
        public static FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();

        internal byte[] data = null;
        internal bool _looping = false;
        internal FMOD.Sound mSound = null;

        internal int _streamIndex = -1;
        internal long _size = 0;
        internal bool _persist = false;

        // streaming SoundData
        public SoundData(int streamIndex, long streamSize, bool streamToPersist)
        {
            _streamIndex = streamIndex;
            _size = streamSize;
            _persist = streamToPersist;
        }

        // in-memory SoundData
        public SoundData(byte[] audiodata)
        {
            data = audiodata;
        }

        public void createSound()
        {
            Debug.Assert(data != null);
            setPersistent(data);
        }

        // every level transition we dump all fmod data in an attempt to eliminate the silly bugs
        public void releaseSound()
        {
            if (mSound != null)
            {
                FMOD.RESULT result = mSound.release();
                ERRCHECK(result);
            }

            mSound = null;
        }

        public void setPersistent(byte[] audiodata)
        {
#if !NO_FMOD
            // there might have been concurrent requests to fulfill the streamToPersist, just ignore dupes
            if (mSound == null)
            {
                // purge our streaming params
                _streamIndex = -1;
                _size = 0;
                _persist = false;

                data = audiodata;
                FMOD.RESULT result;
                exinfo.cbsize = Marshal.SizeOf(exinfo);
                exinfo.length = (uint)audiodata.Length;

                result = FMOD.Framework.system.createSound(audiodata, (FMOD.MODE.SOFTWARE | FMOD.MODE.OPENMEMORY_POINT | FMOD.MODE.CREATECOMPRESSEDSAMPLE), ref exinfo, ref mSound);
                ERRCHECK(result);
            }
#endif
        }

        public void Dispose()
        {
            releaseSound();
            data = null;
        }

        public bool Looping
        {
            set
            {
                _looping = value;
            }
        }

        private void ERRCHECK(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                Console.WriteLine("FMOD error! " + result + " - " + FMOD.Error.String(result));
            }
        }
    }

    // instance of SoundData, has playback information
    internal class Sound
    {
        private SoundData data = null;
		private AudioEngine mAudioEngine = null;
		
#if !NO_FMOD
        private FMOD.Channel mChannel = null;
        private FMOD.Sound mSound = null;
        private bool mOwnSound = false;
        private float mBaseFrequency;
        private byte[] mStreamBytes = null;
        private bool mStreamRequested = false;
        private bool mStreamStarted = false;
        private bool mAbortStream = false;
#endif

        private static byte[] sStreamBytesA = new byte[5570141];
        private static Sound sBufferAActive = null;
        private static byte[] sStreamBytesB = new byte[5570141];
        private static Sound sBufferBActive = null;

        public Sound(string url, float volume, bool looping)
        {
            throw new NotImplementedException();
        }
        public Sound(byte[] audiodata, float volume, bool looping)
        {
            throw new NotImplementedException();
        }

        public Sound(SoundData _data, AudioEngine _engine)
        {
            data = _data;
			mAudioEngine = _engine;
#if !NO_FMOD
            mSound = null;
#endif
        }

        ~Sound()
        {
        }

        public bool Playing
        {
            get
            {
#if !NO_FMOD
                // if we're loading a stream, but haven't created the playing mSound yet, just say yes
                if (mStreamRequested && this.mSound == null)
                    return true;

                if (mChannel != null)
                {
                    Debug.Assert(mSound.getRaw() != null);

                    FMOD.Sound sound = null;
                    mChannel.getCurrentSound(ref sound);
                    if (sound != null &&
                        mSound.getRaw() == sound.getRaw())
                    {
                        bool playing = false;
                        mChannel.isPlaying(ref playing);

                        bool paused = false;
                        mChannel.getPaused(ref paused);
                        return paused || playing;
                    }
                }
#endif
                return false;
            }
        }
        public bool Paused
        {
            get
            {
#if !NO_FMOD
                if (mChannel != null)
                {
                    Debug.Assert(mSound.getRaw() != null);

                    FMOD.Sound sound = null;
                    mChannel.getCurrentSound(ref sound);
                    if (sound != null &&
                        mSound.getRaw() == sound.getRaw())
                    {
                        bool paused = false;
                        mChannel.getPaused(ref paused);
                        return paused;
                    }
                }
#endif
                return false;
            }
        }

        public void Play()
        {
#if !NO_FMOD
            if (Playing)
            {
                _paused = false;
                if (mChannel != null)
                {
                    bool paused = false;
                    mChannel.getPaused(ref paused);
                    if (paused)
                    {
                        mChannel.setPaused(false);
                        return;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            else
            {
                if (data._streamIndex >= 0)
                {
                    if (mStreamBytes != null)
                    {
                        mAbortStream = false;
                        return;
                    }
                    // if this isn't true, we may have 2 async requests to play, which would be super bad
                    Debug.Assert(mStreamBytes == null, "mStreamBytes == null");
                    Debug.Assert(mAbortStream == false, "mAbortStream == false");
                }
                if (mSound != null)
                    Dispose();

                FMOD.RESULT result;
                FMOD.CREATESOUNDEXINFO exinfo = SoundData.exinfo;
                exinfo.cbsize = Marshal.SizeOf(exinfo);

                // allocate streaming?
                if (data._size > 0)
                {
                    mStreamRequested = true;

                    int size = (int)data._size;
                    // don't even bother for the piddly allocs
                    if (sBufferAActive == null)
                    {
                        mStreamBytes = sStreamBytesA;
                        sBufferAActive = this;
                    }
                    else if (sBufferBActive == null)
                    {
                        mStreamBytes = sStreamBytesB;
                        sBufferBActive = this;
                    }
                    else
                    {
                        Console.WriteLine("missed streaming fragmentation" + size);

                        mStreamBytes = new byte[size];
                    }
                    Debug.Assert(mStreamBytes.Length >= size, "mStreamBytes.Length >= size");

                    string file = "Content/Audio/";
                    if (data._persist)
                        file += "Deferred/";
                    else
                        file += "Streaming/";
                    file += data._streamIndex;
                    file += ".mp3";


                    //Console.WriteLine("streaming audio " + file);
                    FileStream fs = new FileStream(file, System.IO.FileMode.Open);

                    fs.BeginRead(mStreamBytes,
                        0,
                        size,
                        delegate(IAsyncResult aresult)
                        {
                            int bytesRead;
                            try
                            {
                                bytesRead = fs.EndRead(aresult);
                            }
                            catch (System.IO.FileNotFoundException)
                            {
                                bytesRead = -1;
                            }
                            fs.Close();

                            if (bytesRead != data._size)
                            {
                                Console.WriteLine("Error streaming in sound " + data._streamIndex);
                                mStreamStarted = true;
                                Dispose();
                                return;
                            }

                            if (data._persist)
                            {
                                // we don't use this anymore
                                Debug.Assert(false, "persist no longer exists");
                                byte[] audiodata = mStreamBytes;

                                mStreamBytes = null;
                                mStreamRequested = false;
                                mStreamStarted = false;

                                // reconfigure parent Sound to be persistent now.
                                data.setPersistent(audiodata);

                                if (mAbortStream)
                                {
                                    Dispose();
                                }
                                else
                                {
                                    Play();
                                }
                            }
                            else
                            {
                                mStreamStarted = true;
                                if (mAbortStream)
                                {
                                    Dispose();
                                }
                                else
                                {
                                    exinfo.length = (uint)bytesRead;
                                    FMOD.MODE mode = FMOD.MODE.OPENMEMORY_POINT | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.SOFTWARE;
                                    if (data._looping)
                                        mode |= FMOD.MODE.LOOP_NORMAL;
                                    result = FMOD.Framework.system.createSound(mStreamBytes, mode, ref exinfo, ref mSound);
                                    mOwnSound = true;
                                    ERRCHECK(result);
								
                                    mAudioEngine.FmodSounds.Add(this);

                                    result = FMOD.Framework.system.playSound(FMOD.CHANNELINDEX.FREE, mSound, false, ref mChannel);
                                    if (mSound == null)
                                    {
                                        Dispose();
                                    }
                                    else
                                    {
                                        Debug.Assert(mSound != null, "mSound != null 1");
                                        ERRCHECK(result);
                                        mChannel.getFrequency(ref mBaseFrequency);
                                    }
                                }
                            }

                            // all streaming sounds need to assign these asap
                            Volume = _volume;
                            Pitch = _pitch;
                            LowPassCutoff = _lowPassCutoff;
                            Reverb = _reverb;
                            if (_paused)
                                Pause();

                            mAbortStream = false;
                        }, null);
                }
                else
                {
                    if (data.mSound == null)
                    {
                        data.createSound();
                    }

                    //exinfo.length = (uint)data.data.Length;
                    //FMOD.MODE mode = FMOD.MODE.OPENMEMORY_POINT | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.SOFTWARE;
                    //if (data._looping)
                    //    mode |= FMOD.MODE.LOOP_NORMAL;
                    //result = FMOD.Framework.system.createSound(data.data, mode, ref exinfo, ref mSound);

                    //ERRCHECK(result);

                    mSound = data.mSound;
                    if (mSound == null)
                    {
                        Dispose();
                        return;
                    }
                    Debug.Assert(mSound != null, "mSound != null 2");

					mAudioEngine.FmodSounds.Add(this);

                    result = FMOD.Framework.system.playSound(FMOD.CHANNELINDEX.FREE, mSound, false, ref mChannel);
                    Debug.Assert(mSound != null, "mSound != null 3");
                    ERRCHECK(result);
                    mChannel.getFrequency(ref mBaseFrequency);
                }
            }
#endif
        }

        public void Stop()
        {
#if !NO_FMOD
            Dispose();
#endif
        }

#if !NO_FMOD
        private float _volume = 1.0f;
        private float _lowPassCutoff = 22050.0f;
        private float _pitch = 0.0f;
        private int _reverb = 0;
        private bool _paused = false;
#endif
        public void Pause()
        {
#if !NO_FMOD
            _paused = true;
            if (Playing && mChannel != null)
            {
                mChannel.setPaused(true);
            }
#endif
        }

        public float Volume
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
#if !NO_FMOD
                _volume = value;
                if (Playing && mChannel != null)
                {
                    if (value > 3.0f)
                        mChannel.setVolume(3.0f);
                    else if (value < 0.0f)
                        mChannel.setVolume(0.0f);
                    else
                        mChannel.setVolume(value);
                }
#endif
            }
        }
        public float LowPassCutoff
        {
            set
            {
#if !NO_FMOD
                _lowPassCutoff = value;
                if (Playing && mChannel != null)
                {
                    float gainSq = value / 22050;
                    float gain = (float)Math.Sqrt(gainSq);
                    Debug.Assert(gain >= 0.0 && gain <= 1.0);
                    mChannel.setLowPassGain(gain);
                }
#endif
            }
        }
        public float Pitch
        {
            set
            {
#if !NO_FMOD
                _pitch = value;
                if (Playing && mChannel != null)
                {
                    Debug.Assert(value >= -24.0f && value <= 24.0f);
                    // input is supposedly semitones (* 2^(value/12))
                    float scalar = (float)Math.Pow(2, value / 12.0f);
                    mChannel.setFrequency(scalar * mBaseFrequency);
                }
#endif
            }
        }
#if NACL
        private int _reverb2 = 0;
        private float _lowPassCutoff2 = 22050;
        private static FMOD.REVERB_CHANNELPROPERTIES rcp = new FMOD.REVERB_CHANNELPROPERTIES();
#endif
        public int Reverb
        {
            set
            {
                // performance is terrible on PC!
#if NACL
#if !NO_FMOD
                Debug.Assert(value >= -10000);
                Debug.Assert(value <= 1000);
                _reverb = value;
                if (Playing && mChannel != null && (_reverb != _reverb2 || _lowPassCutoff != _lowPassCutoff2))
                {
                    if (_reverb == _reverb2 && _reverb2 == -10000)
                        return;

                    _lowPassCutoff2 = _lowPassCutoff;
                    _reverb2 = _reverb;

                    float gainSq = _lowPassCutoff / 22050;
                    float gain = (float)Math.Sqrt(gainSq);
                    Debug.Assert(gain >= 0);
                    Debug.Assert(gain <= 1);

                    rcp.Room = (int)(((value + 10000) * gain) - 10000);
                    Debug.Assert(rcp.Room >= -10000);
                    Debug.Assert(rcp.Room <= 1000);

                    rcp.Flags = FMOD.REVERB_CHANNELFLAGS.DEFAULT;
                    mChannel.setReverbProperties(ref rcp);
                }
#endif
#endif
            }
        }

        public double Duration
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double CurrentPosition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Looping
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Pan
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        // instances only dispose of streaming audio
        public void Dispose()
        {
#if !NO_FMOD
            if (Playing && mChannel != null)
            {
                mChannel.stop();
            }

            if (mSound != null)
            {
                mAudioEngine.FmodSounds.Remove(this);
            }

            if (mOwnSound && mSound != null)
            {
                FMOD.RESULT result = mSound.release();
                ERRCHECK(result);
            }

            mOwnSound = false;
            mSound = null;
            mChannel = null;

            if (mStreamRequested != mStreamStarted)
                mAbortStream = true;
            else
            {
                if (mStreamBytes != null)
                {
                    if (sBufferAActive == this)
                        sBufferAActive = null;
                    else if (sBufferBActive == this)
                        sBufferBActive = null;
                    mStreamBytes = null;
                }

                mStreamBytes = null;
                mStreamRequested = false;
                mStreamStarted = false;
            }
#endif
        }

        private void ERRCHECK(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                Console.WriteLine("FMOD error! " + result + " - " + FMOD.Error.String(result));
            }
        }
    }
}
