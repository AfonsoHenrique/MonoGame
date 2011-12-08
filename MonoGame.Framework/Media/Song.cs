
using System;
using System.IO;

using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.Media
{
    public class Song : IEquatable<Song>, IDisposable
    {
		private Sound _sound;
		private string _name;
		private int _playCount;
		
		internal Song(string fileName)
		{			
			_name = fileName;
			_sound = new Sound(_name, 1.0f, true);
		}
		
		public void Dispose()
        {
			_sound.Dispose();
        }
		
		public bool Equals(Song song) 
		{
			return ((object)song != null) && (Name == song.Name);
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
		
		public override bool Equals(Object obj)
		{
			if(obj == null)
			{
				return false;
			}
			
			return Equals(obj as Song);  
		}
		
		public static bool operator ==(Song song1, Song song2)
		{
			if((object)song1 == null)
			{
				return (object)song2 == null;
			}

			return song1.Equals(song2);
		}
		
		public static bool operator !=(Song song1, Song song2)
		{
		  return ! (song1 == song2);
		}
		
		internal void Play()
		{			
			if ( _sound != null )
			{
				_sound.Play();
				_playCount++;
			}
        }
		
		internal void Pause()
		{			
			if ( _sound != null )
			{
				_sound.Pause();
			}
        }
		
		internal void Stop()
		{
			if ( _sound != null )
			{
				_sound.Stop();
			}
		}
		
		internal bool Loop
		{
			get
			{
				if ( _sound != null )
				{
					return _sound.Looping;
				}
				else
				{
				 	return false;	
				}
			}
			set 
			{
				if ( _sound != null )
				{
					if ( _sound.Looping != value )
					{
						_sound.Looping = value;
					}
				}
			}
		}
		
		internal float Volume
		{
			get
			{
				if (_sound != null)
				{
					return _sound.Volume;
				}
				else
				{
					return 0.0f;
				}
			}
			
			set
			{
				if ( _sound != null )
				{
					if ( _sound.Volume != value )
					{
						_sound.Volume = value;
					}
				}
			}			
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
		
		public TimeSpan Position
        {
            get
            {
				if ( _sound != null )
				{
					return new TimeSpan(0,0,(int)_sound.CurrentPosition);
				}
				else
				{
					return new TimeSpan(0);
				}
            }
        }

        public bool IsProtected
        {
            get
            {
				return false;
            }
        }

        public bool IsRated
        {
            get
            {
				return false;
            }
        }

        public string Name
        {
            get
            {
				return Path.GetFileNameWithoutExtension(_name);
            }
        }

        public int PlayCount
        {
            get
            {
				return _playCount;
            }
        }

        public int Rating
        {
            get
            {
				return 0;
            }
        }

        public int TrackNumber
        {
            get
            {
				return 0;
            }
        }
    }
}

