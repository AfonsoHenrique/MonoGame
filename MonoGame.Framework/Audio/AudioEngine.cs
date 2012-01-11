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
using System.IO;
using System.Text;

namespace Microsoft.Xna.Framework.Audio
{
    public class Variable
    {
        internal string mName;
        internal float mValue;
        float mMin;
        float mMax;
        internal bool mGlobal;

        public Variable(System.IO.BinaryReader reader)
        {
            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            mName = Encoding.ASCII.GetString(bytes);

            mGlobal = reader.ReadBoolean();

            mMin = (float)reader.ReadDouble();
            mMax = (float)reader.ReadDouble();
        }
        public void Set(float value)
        {
            mValue = value;
        }
    }
    public class RPCPoint
    {
        public float x;
        public float y;
        public int curve; // 0 = linear; 1 = fast; 3 = sin/cos
        public RPCPoint(System.IO.BinaryReader reader)
        {
            x = (float)reader.ReadDouble();
            y = (float)reader.ReadDouble();
            curve = reader.ReadInt32();
        }
    }
    public class RPCCurve
    {
        public string mName;
        Variable mVar;
        string varname;
        RPCPoint[] mPoints;
        public int mParameterType; // 0 = volume; 2 = reverb; 3 = filter frequency
        public RPCCurve(System.IO.BinaryReader reader, Variable[] variables)
        {
            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            mName = Encoding.ASCII.GetString(bytes);

            strLen = reader.ReadInt16();
            bytes = reader.ReadBytes((int)strLen);
            varname = Encoding.ASCII.GetString(bytes);
            mVar = null;
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].mName == varname)
                {
                    mVar = variables[i];
                    break;
                }
            }
            Debug.Assert(mVar != null);

            int pointCount = reader.ReadInt32();
            mPoints = new RPCPoint[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                mPoints[i] = new RPCPoint(reader);
            }

            mParameterType = reader.ReadInt32();
        }

        public float Value(Cue cue)
        {
            float input = mVar.mGlobal ? mVar.mValue : cue.GetVariable(varname);

            if (input <= mPoints[0].x)
            {
                return mPoints[0].y;
            }
            else if (input >= mPoints[mPoints.Length - 1].x)
            {
                return mPoints[mPoints.Length - 1].y;
            }

            int i;
            for (i = 0; i < mPoints.Length; i++)
            {
                if (input <= mPoints[i].x)
                    break;
            }

            if (i == mPoints.Length)
                return mPoints[i-1].y;

            if (mPoints[i].x == input)
                return mPoints[i].y;

            Debug.Assert(input < mPoints[i].x);
            float num = mPoints[i].x - input;
            float denom = mPoints[i].x - mPoints[i - 1].x;

            float ratio = num / denom;
            Debug.Assert(ratio >= 0 && ratio <= 1);
            switch (mPoints[i].curve)
            {
                case 0: // linear
                    break;

                case 1: // "fast"
                    ratio = ratio * ratio;
                    break;
                
                case 3: // sin/cos
                    float scos = (float)Math.Cos(Math.PI * ratio);

                    // shift from [1, -1] to [0, 1]
                    ratio = (1 - scos) / 2;
                    break;

                default:
                    Debug.Assert(false);
                    return 0;
            }
            Debug.Assert(ratio >= 0.0f && ratio <= 1.0f);
            return mPoints[i - 1].y * ratio + mPoints[i].y * (1 - ratio);
        }
    }


    public class AudioEngine : IDisposable
    {
        public const int ContentVersion = 39;
        AudioCategory[] mCategories;
        Variable[] mVariables;
        RPCCurve[] mRPCCurves;

        internal System.Collections.Generic.List<WaveBank> Wavebanks = new System.Collections.Generic.List<WaveBank>();
        internal System.Collections.Generic.List<SoundBank> SoundBanks = new System.Collections.Generic.List<SoundBank>();

        internal System.Collections.Generic.List<Sound> FmodSounds = new System.Collections.Generic.List<Sound>();

        public AudioEngine(string settingsFile)
        {
            System.IO.BinaryReader reader = new System.IO.BinaryReader(new FileStream(settingsFile, System.IO.FileMode.Open));

            int version = reader.ReadInt32();
            // anything else would be uncivilized -- run oggAct to correct this error
            const int OGS_VERSION = 1;
            Debug.Assert(version == OGS_VERSION, "rebuild your global settings with oggact");

            int catCount = reader.ReadInt32();
            mCategories = new AudioCategory[catCount];
            for (int i = 0; i < catCount; i++)
            {
                mCategories[i] = new AudioCategory(reader);
            }

            int varCount = reader.ReadInt32();
            mVariables = new Variable[varCount];
            for (int i = 0; i < varCount; i++)
            {
                mVariables[i] = new Variable(reader);
            }

            int rpcCount = reader.ReadInt32();
            mRPCCurves = new RPCCurve[rpcCount];
            for (int i = 0; i < rpcCount; i++)
            {
                mRPCCurves[i] = new RPCCurve(reader, mVariables);
            }
#if !NO_FMOD
            FMOD.Framework.init();
#endif
        }

        public AudioCategory GetCategory(string name)
        {
            for (int i = 0; i < mCategories.Length; i++)
            {
                if (mCategories[i].mName == name)
                {
                    return mCategories[i];
                }
            }
            Debug.Assert(false);
            return null;
        }

        public RPCCurve GetRPC(string name)
        {
            for (int i = 0; i < mRPCCurves.Length; i++)
            {
                if (mRPCCurves[i].mName == name)
                {
                    return mRPCCurves[i];
                }
            }
            Debug.Assert(false);
            return null;
        }

        public void SetGlobalVariable(string name, float value)
        {
            for (int i = 0; i < mVariables.Length; i++)
            {
                if (mVariables[i].mName == name)
                {
                    Debug.Assert(mVariables[i].mGlobal);
                    mVariables[i].Set(value);
                    return;
                }
            }
            Debug.Assert(false);
        }

        public void Update()
        {
#if !NO_FMOD
            FMOD.Framework.update();

            foreach (SoundBank soundbank in SoundBanks)
            {
                soundbank.Update();
            }
#endif
        }

        public virtual void Dispose()
        {
            foreach (SoundBank soundbank in SoundBanks)
            {
                soundbank.Dispose();
            }
            foreach (WaveBank wavebank in Wavebanks)
            {
                wavebank.Dispose();
            }
        }

        public void ResetFMOD()
        {
            // release all our hooks
            int i = 0;

            System.Collections.Generic.List<Sound> FmodSoundsBuf = FmodSounds;
            FmodSounds = new System.Collections.Generic.List<Sound>();

            foreach (Sound sound in FmodSoundsBuf)
            {
                sound.Dispose();
                i++;
            }

            foreach (WaveBank wavebank in Wavebanks)
            {
                wavebank.releaseAll();
            }

            System.GC.Collect();

            FMOD.Framework.resetSystem();
        }
    }
}
