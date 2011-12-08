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
using System.Data;

using GSGE;

namespace Microsoft.Xna.Framework.Audio
{
    public class PlayWaveHelper
    {
        internal string _bankname;
        internal int _index;
        internal byte _weight;

        public PlayWaveHelper(System.IO.BinaryReader reader)
        {
            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            _bankname = Encoding.ASCII.GetString(bytes);

            _index = reader.ReadInt32();
            _weight = reader.ReadByte();
        }
    }
    public class TrackHelper
    {
        internal PlayWaveHelper[] mEntries;
        internal float mVolume;

        internal float mPitchVariationMin;
        internal float mPitchVariationMax;
        internal float mVolumeVariationMin;
        internal float mVolumeVariationMax;

        internal bool mFiltered;
        internal float mFilterFrequency;
        internal double mFilterQFactor;
        internal int mLoopCount;
        internal byte mVariation;

        public TrackHelper(System.IO.BinaryReader reader)
        {
            int playWaveEvents = reader.ReadInt32();
            mEntries = new PlayWaveHelper[playWaveEvents];
            for (int i = 0; i < playWaveEvents; i++)
            {
                mEntries[i] = new PlayWaveHelper(reader);
            }

            mVolume = (float)reader.ReadInt32() / 100.0f;
            mVariation = reader.ReadByte();
            mFiltered = reader.ReadBoolean();
            mFilterFrequency = (float)reader.ReadDouble();
            GSGE.Debug.assert(mFilterFrequency <= 20000);
            mFilterQFactor = reader.ReadDouble();
            mPitchVariationMin = (float)reader.ReadInt32() / 100.0f;
            mPitchVariationMax = (float)reader.ReadInt32() / 100.0f;
            mVolumeVariationMin = (float)reader.ReadInt32() / 100.0f;
            mVolumeVariationMax = (float)reader.ReadInt32() / 100.0f;
            GSGE.Debug.assert(mPitchVariationMin < 24);
            mLoopCount = reader.ReadInt32();



            // funnel fixed sound cues to a specific variation so I don't have to deal with it for others
            if (mEntries.Length == 1)
                mVariation = 0;
            else if (mVariation == 4)
            {
                shuffle = new int[mEntries.Length];
                shuffleIdx = mEntries.Length;
            }
            // give it an invalid initial cue idx so nothing is assumed about its previous play
            currIdx = mEntries.Length;
        }

        internal int currIdx;
        internal int[] shuffle;
        internal int shuffleIdx;
    }

    internal class TrackHelperInstance : IDisposable
    {
        internal TrackHelper mData;
        internal Sound _sound;
        internal float mVolVar;
        internal float mPitchVar;
        internal TrackHelperInstance(TrackHelper th)
        {
            mData = th;
        }

        // get some new randomness in this sucker
        public void Reroll()
        {
            try
            {
                // new sound instance plz
                _sound = null;

                int totWeight = 0;
                for (int i = 0; i < mData.mEntries.Length; i++)
                {
                    totWeight += mData.mEntries[i]._weight;
                }

                int weight = 0;
                switch (mData.mVariation)
                {
                    case 0: //kOrdered
                        mData.currIdx++;
                        if (mData.currIdx >= mData.mEntries.Length)
                            mData.currIdx = 0;
                        break;
                    case 2://kRandom
                        weight = GSGE.RandomMath.RandomInt(totWeight);
                        totWeight = 0;
                        mData.currIdx = 0;
                        for (int i = 0; i < mData.mEntries.Length; i++)
                        {
                            totWeight += mData.mEntries[i]._weight;
                            if (weight < totWeight)
                            {
                                mData.currIdx = i;
                                break;
                            }
                        }
                        break;
                    case 3://kRandomNoImmediateRepeat
                        if (mData.currIdx >= 0 && mData.currIdx < mData.mEntries.Length)
                            totWeight -= mData.mEntries[mData.currIdx]._weight;
                        else
                            mData.currIdx = 0;

                        weight = GSGE.RandomMath.RandomInt(totWeight);
                        totWeight = 0;
                        for (int i = 0; i < mData.mEntries.Length; i++)
                        {
                            if (i == mData.currIdx)
                                continue;

                            totWeight += mData.mEntries[i]._weight;
                            if (weight < totWeight)
                            {
                                mData.currIdx = i;
                                break;
                            }
                        }

                        break;
                    case 4://kShuffle
                        mData.shuffleIdx++;
                        if (mData.shuffleIdx >= mData.mEntries.Length)
                        {
                            mData.shuffleIdx = 0;

                            int i;

                            // reorder next random set
                            List<int> ls = new List<int>();
                            for (i = 0; i < mData.mEntries.Length; i++)
                            {
                                ls.Add(i);
                            }

                            i = 0;
                            while (ls.Count > 0)
                            {
                                int idx = GSGE.RandomMath.RandomInt(ls.Count);
                                mData.shuffle[i] = ls[idx];
                                ls.RemoveAt(idx);
                                i++;
                            }
                        }
                        mData.currIdx = mData.shuffle[mData.shuffleIdx];
                        break;
                    default:
                        throw new NotImplementedException();

                }
            }
            catch (Exception)
            {
                mData.currIdx = 0;
            }

            if (0 <= mData.currIdx && mData.currIdx < mData.mEntries.Length)
            {
                PlayWaveHelper pwh = mData.mEntries[mData.currIdx];

                AudioEngine audioengine = AudioManager.GetAudioManager().GetAudioEngine();
                foreach (WaveBank wavebank in audioengine.Wavebanks)
                {
                    if (wavebank.IsLoaded && wavebank.BankName == pwh._bankname)
                    {
                        if (0 <= pwh._index && wavebank.mSounds.Length > pwh._index)
                        {
                            SoundData sd = wavebank.mSounds[pwh._index];
                            sd.Looping = mData.mLoopCount == 255;
                            _sound = new Sound(sd);
                        }
                        break;
                    }
                }
                //GSGE.Debug.assert(_sound != null);

                mVolVar = RandomMath.RandomBetween(mData.mVolumeVariationMin, mData.mVolumeVariationMax) + mData.mVolume;
                mPitchVar = RandomMath.RandomBetween(mData.mPitchVariationMin, mData.mPitchVariationMax);
            }
        }
        public void Dispose()
        {
            if (_sound != null)
                _sound.Dispose();
            _sound = null;
            mData = null;
        }
    }

    public class SoundHelper
    {
        internal int _priority; // [0, 255], 0 is highest priority
        internal float gain;
        internal float _pitch; // semitones
        internal TrackHelper[] _tracks;
        internal AudioCategory category;
        internal RPCCurve[] rpcs;
        internal string effect;

        public SoundHelper(AudioEngine audioengine, System.IO.BinaryReader reader)
        {
            _priority = reader.ReadInt32();

            int volume = reader.ReadInt32();
            double attenuation_in_db = volume / 100.0f;
            GSGE.Debug.assert(attenuation_in_db <= 0);
            gain = (float)Math.Pow(10, attenuation_in_db / 20.0);
            GSGE.Debug.assert(gain <= 1.0f);

            _pitch = (float)(reader.ReadInt32() / 100.0f);

            int trackCount = reader.ReadInt32();
            _tracks = new TrackHelper[trackCount];
            for (int i = 0; i < trackCount; i++)
            {
                _tracks[i] = new TrackHelper(reader);
            }

            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            string catname = Encoding.ASCII.GetString(bytes);
            category = audioengine.GetCategory(catname);

            int rpcCount = reader.ReadInt32();
            rpcs = new RPCCurve[rpcCount];
            for (int i = 0; i < rpcCount; i++)
            {
                strLen = reader.ReadInt16();
                bytes = reader.ReadBytes((int)strLen);
                string rpc = Encoding.ASCII.GetString(bytes);

                rpcs[i] = audioengine.GetRPC(rpc);
            }

            strLen = reader.ReadInt16();
            bytes = reader.ReadBytes((int)strLen);
            effect = Encoding.ASCII.GetString(bytes);
        }
    }

    public class SoundHelperInstance : IDisposable
    {
        private SoundHelper mData;
        private TrackHelperInstance[] _tracks;
        internal Cue _player;
        private bool mUpdating = false;

        internal SoundHelperInstance(SoundHelper sh)
        {
            mData = sh;

            _tracks = new TrackHelperInstance[sh._tracks.Length];
            for (int i = 0; i < _tracks.Length; i++)
            {
                _tracks[i] = new TrackHelperInstance(sh._tracks[i]);
            }
        }
        private Sound GetSound(int i)
        {
            return _tracks[i]._sound;
        }

        public bool Playing
        {
            get
            {
                for (int i = 0; i < _tracks.Length; i++)
                {
                    Sound _sound = GetSound(i);
                    if (_sound != null && _sound.Playing)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool Paused
        {
            get
            {
                for (int i = 0; i < _tracks.Length; i++)
                {
                    Sound _sound = GetSound(i);
                    if (_sound != null && _sound.Paused)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Play(Cue player)
        {
            bool paused = Paused;

            _player = player;
            GSGE.Debug.assert(_player != null);
            for (int i = 0; i < _tracks.Length; i++)
            {
                // pick some new randomness
                if (!paused)
                    _tracks[i].Reroll();

                Sound _sound = GetSound(i);
                if (_sound != null)
                {
                    // ensure we dont exceed our audio category limit
                    mData.category.AddPlaying(this);

                    _sound.Play();

                    Update();

                    if (!paused)
                        player.mData.instances.Add(_sound);
                }
                else
                    GSGE.Debug.logMessage("missing sound, probably streaming");
                    //throw new NotImplementedException();
            }
        }

        public void Update()
        {
            if (Playing)
            {
                if (!mUpdating)
                {
                    mUpdating = true;
                    _player.mData.bank.instances.Add(this);
                }

                float rpcDb = 0;
                float rpcFilter = 20000;
                int rpcReverb = -10000;
                //float reverb = 0;
                for (int i = 0; i < mData.rpcs.Length; i++)
                {
                    float v = mData.rpcs[i].Value(_player);
                    if (mData.rpcs[i].mParameterType == 0)
                    {
                        // GG HAX make volume ducking more duckalicious
                        rpcDb += 1.5f * v;
                    }
                    else if (mData.rpcs[i].mParameterType == 2)
                    {
                        rpcReverb = (int)v;
                    }
                    else
                    {
                        GSGE.Debug.assert(mData.rpcs[i].mParameterType == 3);
                        GSGE.Debug.assert(v <= 20000);
                        rpcFilter = Math.Min(rpcFilter, v);
                    }
                }

                for (int i = 0; i < _tracks.Length; i++)
                {
                    Sound _sound = GetSound(i);
                    if (_sound != null)
                    {
                        TrackHelperInstance track = _tracks[i];

                        double attenuation_in_db = (rpcDb / 100.0f) + track.mVolVar;
                        float rpcGain = (float)Math.Pow(10, attenuation_in_db / 20.0);

                        _sound.Volume = mData.gain * mData.category.Volume * rpcGain;

                        float filter = rpcFilter;
                        if (track.mData.mFiltered && track.mData.mFilterFrequency < filter)
                            filter = track.mData.mFilterFrequency;

                        _sound.LowPassCutoff = filter;

                        _sound.Pitch = track.mPitchVar + mData._pitch;

                        _sound.Reverb = rpcReverb;
                    }
                }
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _tracks.Length; i++)
            {
                Sound _sound = GetSound(i);
                if (_sound != null)
                    _sound.Stop();
            }
            if (mUpdating)
            {
                mUpdating = false;
                _player.mData.bank.instances.Remove(this);
            }
        }

        public void Pause()
        {
            for (int i = 0; i < _tracks.Length; i++)
            {
                Sound _sound = GetSound(i);
                if (_sound != null)
                    _sound.Pause();
            }
            if (mUpdating)
            {
                mUpdating = false;
                _player.mData.bank.instances.Remove(this);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i] != null)
                {
                    _tracks[i].Dispose();
                    _tracks[i] = null;
                }
            }
            mData = null;
            if (mUpdating)
            {
                mUpdating = false;
                _player.mData.bank.instances.Remove(this);
            }
        }
    }
}
