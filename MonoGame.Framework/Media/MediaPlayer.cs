
using System;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.Media
{
    public static class MediaPlayer
    {
		private static Song _song = null;
		private static MediaState _mediaState = MediaState.Stopped;
		private static float _volume = 1.0f;
		private static bool _looping = true;
		
        public static void Pause()
        {
			if (_song != null)
			{
				_song.Pause();
				_mediaState = MediaState.Paused;
			}			
        }

        public static void Play(Song song)
        {
			if ( song != null )
			{
				if ( _song != null )
				{
					_song.Dispose();
					_song = null;
				}
				
				_song = song;
				_song.Volume = _volume;
				_song.Loop = _looping;
				_song.Play();
				_mediaState = MediaState.Playing;
			}
        }

        public static void Resume()
        {
			if (_song != null)
			{
				_song.Play();
				_mediaState = MediaState.Playing;
			}					
        }

        public static void Stop()
        {
			if (_song != null)
			{
				_song.Stop();
				_mediaState = MediaState.Stopped;
			}
        }

        public static bool IsMuted
        {
            get
            {
				if (_song != null)
				{
					return _song.Volume == 0.0f;
				}
				else
				{
					return false;
				}
            }
            set
            {
				if (_song != null) 
				{
					if (value)
					{
						_song.Volume = 0.0f;
					}
					else 
					{
						_song.Volume = _volume;
					}
				}
            }
        }

        public static bool IsRepeating
        {
            get
            {
				if (_song != null)
				{
					return _song.Loop;
				}
				else
				{
					return false;
				}
            }
            set
            {
				_looping = value;
            }
        }

        public static bool IsShuffled
        {
            get
            {
				return false;
            }
        }

        public static bool IsVisualizationEnabled
        {
            get
            {
				return false;
            }
        }

        public static TimeSpan PlayPosition
        {
            get
            {
				if (_song != null)
				{
					return _song.Position;
				}
				else
				{
					return new TimeSpan(0);
				}
            }
        }

        public static MediaState State
        {
            get
            {
				return _mediaState;
            }
        }
		
		public static bool GameHasControl
        {
            get
            {
            	return true;
			}
		}

        public static float Volume
        {
            get
            {
            	return _volume;
			}
            set
            {         
				if (_song != null)
				{
					_volume = value;
					_song.Volume = value;
				}
			}
        }
    }
}

