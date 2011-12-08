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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
#if NACL
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content
{
    internal class Texture2DReader : ContentTypeReader<Texture2D>
    {
        internal Texture2DReader()
        {
            // Do nothing
        }

        protected internal override Texture2D Read(ContentReader reader, Texture2D existingInstance)
		{
			Texture2D texture = null;
			
			SurfaceFormat surfaceFormat = (SurfaceFormat)reader.ReadInt32 ();
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			/*int levelCount =*/ reader.ReadInt32();
			int imageLength = reader.ReadInt32();
						
			byte[] imageData = reader.ReadBytes(imageLength);
                        
#if NO_DXT35
            System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
            sw2.Reset();
            sw2.Start();
            // GG TODO need to remove this manual decompression KTHX
            switch(surfaceFormat)
			{
                case SurfaceFormat.Dxt1: imageData = DxtUtil.DecompressDxt1(imageData, width, height); surfaceFormat = SurfaceFormat.Rgba32; break;
                case SurfaceFormat.Dxt3: imageData = DxtUtil.DecompressDxt3(imageData, width, height); surfaceFormat = SurfaceFormat.Rgba32; break;
			}	
            if (sw2.ElapsedMilliseconds > 16)
            {
                GSGE.Debug.logMessage("Surface Format decompression took " + sw2.ElapsedMilliseconds);
            }
#endif
                       
            unsafe
            {
                fixed (byte* pData = imageData)
                {
                    ESTexture2D esTexture = new ESTexture2D((IntPtr)pData, 
                        imageData.Length, 
                        surfaceFormat, 
                        width, height, 
                        new Size(width, height), 
                        All.Linear);
                    texture = new Texture2D(new ESImage(esTexture));
                }
            }
			
			return texture;
		}
    }
}
