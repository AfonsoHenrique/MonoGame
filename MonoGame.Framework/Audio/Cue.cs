#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Audio
{
    // instance of a cue, so Playing works and stuff
    public class Cue : IDisposable
    {
		internal static Random random = new Random();
		
		internal static int RandomInt( int high )
		{
			return random.Next( high );
		}
		
        internal  CueData mData;
        internal SoundHelperInstance mSound;
        private int currIdx = 0;
        private float mDistance = 0.0f;
        internal AudioEngine mAudioEngine;
		
		internal Cue(CueData data, AudioEngine engine)
        {
            Debug.Assert(data != null);
            mData = data;
			mAudioEngine = engine;
        }
        public bool IsPaused
        {
            get { return mSound != null && mSound.Paused; }
        }

        public bool IsPlaying
        {
            get { return mSound != null && mSound.Playing; }
        }

        public bool IsStopped
        {
            get { return mSound == null || !mSound.Playing; }
        }

        public string Name
        {
            get { return mData.Name; }
        }
        public void Pause()
        {
            if (mSound != null)
                mSound.Pause();
        }

        public void Play()
        {
            // purge old instances when we need to limit instances
            if (mData._maxInstances != -1 && mData.totalPlays > 0)
            {
                float scalar = mData.totalInstances / mData.totalPlays;
                int max = (int)(scalar * mData._maxInstances);

                if (max <= mData.instances.Count)
                {
                    for (int i = mData.instances.Count - 1; i >= 0; i--)
                    {
                        if (!mData.instances[i].Playing)
                        {
                            mData.instances.RemoveAt(i);
                        }
                    }

                    // Bastion's only usage case is Behavior 0 = Fail to Play
                    if (max <= mData.instances.Count)
                    {
                        return;
                    }
                }
            }

            switch (mData._variation)
            {
                case 0: //kOrdered
                    mData.currIdx++;
                    if (mData.currIdx >= mData._sounds.Length)
                        mData.currIdx = 0;
                    break;
                case 2://kRandom
                    mData.currIdx = RandomInt(mData._sounds.Length);
                    break;
                case 3://kRandomNoImmediateRepeat

                    int newIdx = RandomInt(mData._sounds.Length - 1);
                    if (newIdx >= mData.currIdx)
                        mData.currIdx = newIdx + 1;
                    else
                        mData.currIdx = newIdx;

                    break;
                case 4://kShuffle
                    mData.shuffleIdx++;
                    if (mData.shuffleIdx >= mData._sounds.Length)
                    {
                        mData.shuffleIdx = 0;

                        int i;

                        // reorder next random set
                        List<int> ls = new List<int>();
                        for (i = 0; i < mData._sounds.Length; i++)
                        {
                            ls.Add(i);
                        }

                        i = 0;
                        while (ls.Count > 0)
                        {
                            int idx = RandomInt(ls.Count);
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

            int instancesCount = mData.instances.Count;
            currIdx = mData.currIdx;
            if (mSound != null)
               mSound.Dispose();
            mSound = new SoundHelperInstance(mData._sounds[mData.currIdx],mAudioEngine);
            mSound.Play(this);

            // keep track of when multi-track things play, because they muck with my lousy maxInstances impl
            mData.totalPlays++;
            mData.totalInstances += mData.instances.Count - instancesCount;

            // don't track instances if we don't limit them
            if (mData._maxInstances == -1)
                mData.instances.Clear();
        }

        public void Resume()
        {
            if (mSound.Paused)
                mSound.Play(this);
        }

        public void Stop(AudioStopOptions options)
        {
            if (mSound != null)
                mSound.Stop();
        }

        public void SetVariable(string name, float value)
        {
            if (name == "Distance")
                mDistance = value;
            else
                throw new NotImplementedException();
        }

        public float GetVariable(string name)
        {
            if (name == "Distance")
                return mDistance;
            else
                throw new NotImplementedException();
        }

        public void Apply3D(AudioListener listener, AudioEmitter emitter)
        {
            SetVariable("Distance", (listener.Position - emitter.Position).Length());
        }

        public void Dispose()
        {
            if (mSound != null)
            {
                mSound.Dispose();
                mSound = null;
            }
            mData = null;
        }
        public bool IsDisposed
        {
            get
            {
                return mData == null;
            }
        }
    }

    public class CueData
    {
        private string _name;
        internal SoundHelper[] _sounds;
        internal System.Collections.Generic.List<Sound> instances = new System.Collections.Generic.List<Sound>();

        internal int _variation;
        internal int currIdx;
        internal int[] shuffle;
        internal int shuffleIdx;
        internal int _maxInstances;
        internal int totalInstances;
        internal int totalPlays;
        internal SoundBank bank;

        public string Name
        {
            get { return _name; }
        }

        internal CueData(System.IO.BinaryReader reader, SoundHelper[] sounds, SoundBank b)
        {
            bank = b;

            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            _name = Encoding.ASCII.GetString(bytes);

            int variation = reader.ReadInt32();
            int soundCount2 = reader.ReadInt32();
            _sounds = new SoundHelper[soundCount2];
            for (int j = 0; j < soundCount2; j++)
            {
                int index = reader.ReadInt32();
                _sounds[j] = sounds[index];
            }

            _maxInstances = reader.ReadInt32();


            // funnel fixed sound cues to a specific variation so I don't have to deal with it for others
            if (_sounds.Length == 1)
                _variation = 0;
            else if (_variation == 4)
            {
                shuffle = new int[_sounds.Length];
                shuffleIdx = _sounds.Length;
            }
            // give it an invalid initial cue idx so nothing is assumed about its previous play
            currIdx = _sounds.Length;
        }
    }
}

