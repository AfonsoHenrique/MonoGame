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
using System.Drawing;
using NaCl.PLFS;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Graphics;


namespace Microsoft.Xna.Framework.Content
{
    public class ContentManager : IDisposable
    {
        private string _rootDirectory = string.Empty;
        private IServiceProvider serviceProvider;
		private IGraphicsDeviceService graphicsDeviceService;

        private Queue<Action> mainThreadCallbacks;

        public ContentManager(IServiceProvider serviceProvider)
        {
			if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            this.serviceProvider = serviceProvider;
            this.mainThreadCallbacks = new Queue<Action>();
		}

        public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
          	if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            if (rootDirectory == null)
            {
                throw new ArgumentNullException("rootDirectory");
            }
            this.RootDirectory = rootDirectory;
            this.serviceProvider = serviceProvider;
            this.mainThreadCallbacks = new Queue<Action>();
        }

        // GG EDIT added
        // does one enqueued callback; must be called from the main thread (no enforcement of this, though)
        public bool ExecuteOneMainThreadCallback()
        {
            lock (mainThreadCallbacks)
            {
                if (mainThreadCallbacks.Count > 0)
                {
                    mainThreadCallbacks.Dequeue()();
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
        }
		
        public virtual T Load<T>(string assetName)
        {			
			// GG TODO
            // this technically should be checking to see if the asset is in-memory
            // but we may not need that functionality
            return ReadAsset<T>(assetName, null);
        }

        // GG EDIT added
        // do a little work before any real ReadAsset call; might return an asset (but probably null)
        private object ReadAssetPrelim<T>(string assetName)
        {
            if (this.graphicsDeviceService == null)
            {
                this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                if (this.graphicsDeviceService == null)
                    throw new InvalidOperationException("No Graphics Device Service");
            }

            //GG EDIT
            if (typeof(T).FullName == "Microsoft.Xna.Framework.Graphics.Effect")
            {
                Object e = new Effect(assetName, this.graphicsDeviceService.GraphicsDevice);
                return e;
            }
            return null;
        }

        // GG EDIT added
        // reads an asset asynchronously, communicating it back via the Action
        protected void ReadAssetAsync<T>(string assetName, Action<IDisposable> recordDisposableObject, Action<T> loadAction)
        {
            object earlyAsset = ReadAssetPrelim<T>(assetName);
            if (earlyAsset != null)
            {
                loadAction((T)earlyAsset);
                return;
            }

            OpenStreamAsync(assetName,
                delegate(System.IO.Stream assetStream) {
                    T obj = ReadAsset<T>(assetName, assetStream, recordDisposableObject);
                    loadAction(obj);
                });
        }

        protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
        {
            object earlyAsset = ReadAssetPrelim<T>(assetName);
            if (earlyAsset != null)
                return (T)earlyAsset;

            System.IO.Stream assetStream = OpenStream(assetName);
            return ReadAsset<T>(assetName, assetStream, recordDisposableObject);
        }

        private T ReadAsset<T>(string assetName, System.IO.Stream assetStream, 
                               Action<IDisposable> recordDisposableObject)
        {
            // GG EDIT removed code for loading raw assets like pngs

            // Load a XNB file
            ContentReader reader = new ContentReader(this, assetStream, this.graphicsDeviceService.GraphicsDevice);
            ContentTypeReaderManager typeManager = new ContentTypeReaderManager(reader);
            reader.TypeReaders = typeManager.LoadAssetReaders(reader);
            foreach (ContentTypeReader r in reader.TypeReaders)
            {
                r.Initialize(typeManager);
            }
            // we need to read a byte here for things to work out, not sure why
            byte dummy = reader.ReadByte();
            System.Diagnostics.Debug.Assert(dummy == 0);

            // Get the 1-based index of the typereader we should use to start decoding with
            int index = reader.ReadByte();
            ContentTypeReader contentReader = reader.TypeReaders[index - 1];
            object result = reader.ReadObject<T>(contentReader);

            reader.Close();
            assetStream.Close();

            if (result == null)
            {
                throw new ContentLoadException("Could not load " + assetName + " asset!");
            }

            // GG EDIT added IDisposable recording
            T tresult = (T)result;
            if (tresult is IDisposable)
            {
                if (recordDisposableObject == null)
                {
                    // GG TODO: would call local method here
                }
                else
                {
                    recordDisposableObject((IDisposable)tresult);
                }
            }

            return tresult;
        }

        protected string figureOutExtension(string originalAssetName)
        {
            //Lowercase assetName (monodroid specification all assests are lowercase)
            // Check for windows-style directory separator character
            originalAssetName = Path.Combine(_rootDirectory, originalAssetName.Replace('\\', Path.DirectorySeparatorChar));

            // GG EDIT just assuming all our assets are xnb files...
            if (File.Exists(originalAssetName + ".xnb"))//GG TODO
                return originalAssetName + ".xnb";

            return null;
        }

        // GG EDIT added
        // opens a stream asynchronously, calling the callback when the contents of the file are completely in
        // a buffer in memory (only intended for small files)
        protected virtual void OpenStreamAsync(string assetName, Action<System.IO.Stream> openAction)
        {
            // GG EDIT just assuming all our assets are xnb files...
            assetName = Path.Combine(_rootDirectory, assetName.Replace('\\', Path.DirectorySeparatorChar));
            assetName = assetName + ".xnb";

            new AsyncFileBuffer(assetName,
                delegate(AsyncFileBuffer r)
                {
                    // this delegate is very likely to be called in a non-main thread
                    // so we wrap all the data into a thunk and have the main thread
                    // ask for it whenever it wants it
                    lock(mainThreadCallbacks) {
                        mainThreadCallbacks.Enqueue(delegate()
                        {
                            openAction(r.Stream);
                        });
                    }
                });
        }

        protected virtual System.IO.Stream OpenStream(string originalAssetName)
        {
            string assetName = figureOutExtension(originalAssetName);
            if (string.IsNullOrEmpty(assetName))
            {
                // this usually means that the asset was meant to be already loaded from a package but 
                // the package loading failed for some reasons that didn't terminate the program
                throw new ContentLoadException("Could not OpenStream on " + originalAssetName+" "+ Directory.GetCurrentDirectory());
            }
            System.IO.Stream s = File.Open(assetName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            return s;
        }

		
        public virtual void Unload()
        {
        }

        public string RootDirectory
        {
            get
            {
                return _rootDirectory;
            }
            set
            {
                _rootDirectory = value;
            }
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                return this.serviceProvider;
            }
        }
    }
}

