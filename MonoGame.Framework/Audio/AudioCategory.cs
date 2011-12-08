
using System;
using System.Text;

namespace Microsoft.Xna.Framework.Audio
{
    public class AudioCategory
    {
        public string mName;
        //bool mBackgroundMusic;
        //int mBehavior; // behavior at max
        //int mMaxInstances;

        float mVolume; // category gain

        //internal System.Collections.Generic.List<SoundHelper> instances = new System.Collections.Generic.List<SoundHelper>();

        public AudioCategory(System.IO.BinaryReader reader)
        {
            int strLen = reader.ReadInt16();
            byte[] bytes = reader.ReadBytes((int)strLen);
            mName = Encoding.ASCII.GetString(bytes);

            bool mBackgroundMusic = reader.ReadBoolean();
            int volume = reader.ReadInt32(); // 100 * db
            int mBehavior = reader.ReadInt32();
            int mMaxInstances = reader.ReadInt32();
            //GSGE.Debug.logMessage("category " + mName + " maxInstances = " + mMaxInstances);
            GSGE.Debug.assert(mMaxInstances > 0);

            // XACT stores it as a 2 decimal fixed point value
            double attenuation_in_db = volume / 100.0f;
            float gain = (float)Math.Pow(10, attenuation_in_db / 20.0);
            mVolume = gain;
        }

        public void AddPlaying(SoundHelperInstance s)
        {
            /*
            instances.Remove(s);
            if (instances.Count > mMaxInstances)
            {
                Update();
                if (instances.Count > mMaxInstances)
                {
                    instances[0].Stop();
                }
            }
            instances.Add(s);
             */
        }

        /*
        public void Update()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (!instances[i].Playing)
                {
                    instances.RemoveAt(i);
                }
            }
        }
         */

        public void SetVolume(float gain)
        {
            GSGE.Debug.assert(gain <= 1.0f);
            // GG HAX make everything lounder
            gain *= 2.5f;
            mVolume = gain;
        }

        public float Volume
        {
            get
            {
                return mVolume;
            }
        }
    }
}
