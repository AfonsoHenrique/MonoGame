#region License
/*
Microsoft Public License (Ms-PL)
XnaTouch - Copyright © 2009 The XnaTouch Team

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
using NaCl.PLFS;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Framework
{
    public class Game : IDisposable
    {
		private const float FramesPerSecond = 60.0f; // ~60 frames per second
		
        private GameTime _updateGameTime;
        private GameTime _drawGameTime;
        private DateTime _lastUpdate;
        private bool _initialized = false;
		private bool _initializing = false;
		private bool _isActive = true;
        private GameComponentCollection _gameComponentCollection;
        public GameServiceContainer _services;
        private ContentManager _content;
        private WindowsGameWindow _view;
		private bool _isFixedTimeStep = true;
        private TimeSpan _targetElapsedTime = TimeSpan.FromSeconds(1 / FramesPerSecond); 
        
		internal IGraphicsDeviceManager graphicsDeviceManager;
        internal IGraphicsDeviceService graphicsDeviceService;
        private bool _devicesLoaded;

		internal static bool _playingVideo = false;
		private SpriteBatch spriteBatch;
		private Texture2D splashScreen;
		
		delegate void InitialiseGameComponentsDelegate();
       

		public Game()
		{

			// Initialize collections
			_services = new GameServiceContainer();
			_gameComponentCollection = new GameComponentCollection();

            _view = new WindowsGameWindow();
			_view.Game = this;
					
			// Initialize GameTime
            _updateGameTime = new GameTime();
            _drawGameTime = new GameTime();
		}
		
		~Game()
		{
			
		}

		public void Dispose ()
		{
			// do nothing
            _view.Dispose();
		}

    
        public bool IsActive
        {
            get
			{
				return _isActive;
			}
			protected set
			{
				if (_isActive != value )
				{
					_isActive = value;
				}
			}
        }

        public bool IsMouseVisible
        {
            get
			{
				return false;
			}
            set
			{
				// do nothing; ignore
			}
        }

        public TimeSpan TargetElapsedTime
        {
            get
            {
                return _targetElapsedTime;
            }
            set
            {
                _targetElapsedTime = value;			
				if(_initialized) {
					throw new NotSupportedException();
				}
            }
        }
		
        public void Run()
    	{			
			_lastUpdate = DateTime.Now;
			
            //Need to execute this on the rendering thread
            _view.OpenTkGameWindow.RenderFrame += delegate
            {
                if (!_devicesLoaded)
                {
                    Initialize();
                    _devicesLoaded = true;
                }
            };

            _view.OpenTkGameWindow.Run( FramesPerSecond );	
            //_view.OpenTkGameWindow.Run( FramesPerSecond / ( FramesPerSecond * TargetElapsedTime.TotalSeconds ), FramesPerSecond / ( FramesPerSecond * TargetElapsedTime.TotalSeconds ) );	
        }

        public void RunSlice()
        {
            _view.OpenTkGameWindow.RunSlice();
        }

        public void RedrawFrame()
        {
            if (_view != null && _view.OpenTkGameWindow != null)
                _view.OpenTkGameWindow.RedrawFrame();
        }

        public void Tick()
        {
            RunSlice();
        }
		
		internal void DoUpdate(GameTime aGameTime)
		{
            if (!_devicesLoaded)
                return;

			if (_isActive)
			{
				Update(aGameTime);
			}
		}
		
		internal void DoDraw(GameTime aGameTime)
		{
            if (!_devicesLoaded)
                return;

            // GG EDIT
			if (_isActive && _initialized)
			{
				SpriteBatcher.startFrame();
				Draw(aGameTime);
			}
		}
		
		internal void DoStep()
		{
			var timeNow = DateTime.Now;
			
			// Update the game			
            _updateGameTime.Update(timeNow - _lastUpdate);
            Update(_updateGameTime);

            // Draw the screen
            _drawGameTime.Update(timeNow - _lastUpdate);
            _lastUpdate = timeNow;
            Draw(_drawGameTime);       			
		}

        public bool IsFixedTimeStep
        {
            get
			{
				return _isFixedTimeStep;
			}
            set
			{
				_isFixedTimeStep = value;
			}
        }

        public GameWindow Window
        {
            get
            {
                return _view;
            }
        }
		
		public void ResetElapsedTime()
        {
            _lastUpdate = DateTime.Now;
        }


        public GameServiceContainer Services
        {
            get
            {
                return _services;
            }
		}

        public ContentManager Content
        {
            get
            {
                if (_content == null)
                {
                    _content = new ContentManager(_services);
                }
                return _content;
            }
            set
            {
                //GG EDIT
                _content = value;
            }
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                if (this.graphicsDeviceService == null)
                {
                    this.graphicsDeviceService = this.Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                    if (this.graphicsDeviceService == null)
                    {
                        throw new InvalidOperationException("No Graphics Device Service");
                    }
                }
                return this.graphicsDeviceService.GraphicsDevice;
            }
        }
		
		public void EnterBackground()
    	{
			_isActive = false;
			 if (Deactivated != null)
                Deactivated.Invoke(this, null);
		}
		
		public void EnterForeground()
    	{
			_isActive = true;
			if (Activated != null)
                Activated.Invoke(this, null);
		}
		
		protected virtual bool BeginDraw()
		{
			return true;
		}
		
		protected virtual void EndDraw()
		{
			
		}
		
		protected virtual void LoadContent()
		{			
			string DefaultPath = "Default.png";
			if (File.Exists(DefaultPath))
			{
				// Store the RootDir for later 
				string backup = Content.RootDirectory;
				
				try 
				{
					// Clear the RootDirectory for this operation
					Content.RootDirectory = string.Empty;
					
					spriteBatch = new SpriteBatch(GraphicsDevice);
					splashScreen = Content.Load<Texture2D>(DefaultPath);			
				}
				finally 
				{
					// Reset RootDir
					Content.RootDirectory = backup;
				}
				
			}
			else
			{
				spriteBatch = null;
				splashScreen = null;
			}
		}
		
		protected virtual void UnloadContent()
		{
			// do nothing
		}
		
        protected virtual void Initialize()
        {
			this.graphicsDeviceManager = this.Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;			
			this.graphicsDeviceService = this.Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;			

			if ((this.graphicsDeviceService != null) && (this.graphicsDeviceService.GraphicsDevice != null))
            {
                LoadContent();
            }
            // GG EDIT InitializeGameComponents started being called here
            InitializeGameComponents();
        }
		
		private void InitializeGameComponents()
		{
            try
            {
                foreach (GameComponent gc in _gameComponentCollection)
                {
                    gc.Initialize();
                }
            }
            catch (System.Exception e) // GG EDIT
            {
                throw e;
                //NaCl.Debug.print(String.Format("Initialize exception: {0}\n", e));
                //Exit();
            }
		}

        protected virtual void Update(GameTime gameTime)
        {
            // GG EDITED -- removed background thread stuff
            _initializing = false;
            _initialized = true;
            // super crappy copy
            GameComponentCollection gcc = new GameComponentCollection();
            foreach (GameComponent gc in _gameComponentCollection)
            {
                gcc.Add(gc);
            }
            foreach (GameComponent gc in gcc)
            {
                if (gc.Enabled)
                {
                    gc.Update(gameTime);
                }
            }
        }
		
        protected virtual void Draw(GameTime gameTime)
        {
			if ( _initializing )
			{
				if ( spriteBatch != null )
				{
					spriteBatch.Begin();
					
					// We need to turn this into a progress bar or animation to give better user feedback
					spriteBatch.Draw(splashScreen, new Vector2(0, 0), Color.White );
					spriteBatch.End();
				}
			}
			else
			{
				if (!_playingVideo) 
				{
		            foreach (GameComponent gc in _gameComponentCollection)
		            {
		                if (gc.Enabled && gc is DrawableGameComponent)
		                {
		                    DrawableGameComponent dc = gc as DrawableGameComponent;
		                    if (dc.Visible)
		                    {
		                        dc.Draw(gameTime);
		                    }
		                }
		            }
				}
			}
        }

        public void Exit()
        {
            if (!_view.OpenTkGameWindow.IsExiting)
                _view.OpenTkGameWindow.Exit();
        }

        public GameComponentCollection Components
        {
            get
            {
                return _gameComponentCollection;
            }
        }

        protected virtual void OnExiting(object sender, EventArgs args)
        {
            // GG TODO
        }

		#region Events
		public event EventHandler Activated;
		public event EventHandler Deactivated;
		//public event EventHandler Disposed;
		//public event EventHandler Exiting;
		#endregion
    }
}

