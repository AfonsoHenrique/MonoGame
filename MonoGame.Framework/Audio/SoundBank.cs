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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Xna.Framework.Audio
{
    public class SoundBank
    {
        public System.Collections.Generic.List<SoundHelperInstance> instances = new System.Collections.Generic.List<SoundHelperInstance>();
        SoundHelper[] sounds;
        CueData[] cues;
        AudioEngine audioengine;

        public SoundBank(AudioEngine audioEngine, string filename)
        {
            audioengine = audioEngine;

            System.IO.BinaryReader reader = new System.IO.BinaryReader(new FileStream(filename, System.IO.FileMode.Open, FileAccess.Read ));

            int version = reader.ReadInt32();
            // anything else would be uncivilized -- run oggAct to correct this error
            const int OSB_VERSION = 4;
            Debug.Assert(version == OSB_VERSION, "rebuild your sound bank with oggact");

            int soundCount = reader.ReadInt32();
            sounds = new SoundHelper[soundCount];
            for (int i = 0; i < soundCount; i++)
            {
                sounds[i] = new SoundHelper(audioEngine, reader);
            }
            int cueCount = reader.ReadInt32();
            cues = new CueData[cueCount];
            for (int i = 0; i < cueCount; i++)
            {
                cues[i] = new CueData(reader, sounds, this);
            }

            audioEngine.SoundBanks.Add(this);
        }

        public Cue GetCue(string name)
        {
            for (int i = 0; i < cues.Length - 1; i++)
            {
                if (cues[i].Name == name)
                {
                    return new Cue(cues[i],audioengine);
                }
            }
            return null;
        }

        public void Update()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].Update();
            }
        }

        public void Dispose()
        {
            audioengine.SoundBanks.Remove(this);
        }
    }
}

