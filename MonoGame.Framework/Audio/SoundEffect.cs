
using System;

using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.Audio
{
    public sealed class SoundEffect : IDisposable
    {
		private static Sound _sound;
		private string _name = "";
		private string _filename = "";
		private byte[] _data;
		
		internal SoundEffect(string fileName)
		{
			_filename = fileName;		
			
			if (_filename == string.Empty )
			{
			  throw new System.IO.FileNotFoundException("Supported Sound Effect formats are wav, mp3, acc, aiff");
			}
			
			_sound = new Sound(_filename, 1.0f, false);
			_name = System.IO.Path.GetFileNameWithoutExtension(fileName);
		}
		
		//SoundEffect from playable audio data
		internal SoundEffect(string name, byte[] data)
		{
			_data = data;
			_name = name;
			_sound = new Sound(_data, 1.0f, false);
		}
		
		public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
		{
			//buffer should contain 16-bit PCM wave data
			short bitsPerSample = 16;
			
			System.IO.MemoryStream mStream = new System.IO.MemoryStream(44+buffer.Length);
			System.IO.BinaryWriter writer = new System.IO.BinaryWriter(mStream);
			
			writer.Write("RIFF".ToCharArray()); //chunk id
			writer.Write((int)(36+buffer.Length)); //chunk size
			writer.Write("WAVE".ToCharArray()); //RIFF type
			
			writer.Write("fmt ".ToCharArray()); //chunk id
			writer.Write((int)16); //format header size
			writer.Write((short)1); //format (PCM)
			writer.Write((short)channels);
			writer.Write((int)sampleRate);
			short blockAlign = (short)((bitsPerSample/8)*(int)channels);
			writer.Write((int)(sampleRate*blockAlign)); //byte rate
			writer.Write((short)blockAlign);
			writer.Write((short)bitsPerSample);
			
			writer.Write("data".ToCharArray()); //chunk id
			writer.Write((int)buffer.Length); //data size
			writer.Write(buffer);
			
			writer.Close();
			mStream.Close();
			
			_data = mStream.ToArray();
			_name = "";
			_sound = new Sound(_data, 1.0f, false);
		}
		
        public bool Play()
        {				
			return Play(MasterVolume, 0.0f, 0.0f);
        }

        public bool Play(float volume, float pitch, float pan)
        {
			if ( MasterVolume > 0.0f )
			{
				SoundEffectInstance instance = CreateInstance();
				instance.Volume = volume;
				instance.Pitch = pitch;
				instance.Pan = pan;
				instance.Play();
				return instance.Sound.Playing;
			}
			return false;
        }
		
		public TimeSpan Duration 
		{ 
			get
			{
				if ( _sound != null )
				{
					return new TimeSpan(0,0,(int)_sound.Duration);
				}
				else
				{
					return new TimeSpan(0);
				}
			}
		}

        public string Name
        {
            get
            {
				return _name;
            }
        }
		
		public SoundEffectInstance CreateInstance ()
		{
			var instance = new SoundEffectInstance();
			if (_data != null) {
				_sound = new Sound(_data, MasterVolume, false);
			} else {
				_sound = new Sound(_filename, MasterVolume, false);
			}
			instance.Sound = _sound;
			return instance;
			
		}
		
		#region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
		
		static float _masterVolume = 1.0f;
		public static float MasterVolume 
		{ 
			get
			{
				return _masterVolume;
			}
			set
			{
				_masterVolume = value;	
			}
		}
    }
}

