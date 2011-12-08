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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Content;
#if NACL
using OpenTK.Graphics.ES20;
using GLPixelFormat = OpenTK.Graphics.ES20.PixelFormat; // PixelFormat aliases with property
#else
using OpenTK.Graphics.OpenGL;
using GLPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
#endif
using Buffer = System.Buffer;


namespace Microsoft.Xna.Framework.Graphics
{
	internal class ESTexture2D : IDisposable
	{
		private uint _name;
		private Size _size;
		private int _width,_height;
		private SurfaceFormat _format;
		private float _maxS,_maxT;

        //GG EDIT
        public ESTexture2D(int height, int width, uint id)
        {
            _name = id;
            _height = height;
            _width = width;
            _size = new Size(width, height);
            _format = SurfaceFormat.Color;//dunno about this
        }


        public ESTexture2D (IntPtr data, int dataLength, SurfaceFormat pixelFormat, int width, int height, Size size, All filter)
		{
			InitWithData(data, dataLength, pixelFormat,width,height,size, filter);
		}
		
		public ESTexture2D(Bitmap image, All filter)
		{
            InitWithBitmap(image, filter);
		}

        public void InitWithBitmap(Bitmap image, All filter)
        {
            BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                           System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _format = SurfaceFormat.Color;
            int dataLength = bitmapData.Height * bitmapData.Width * 4;
            InitWithData(bitmapData.Scan0, dataLength, _format, image.Width, image.Height, new Size(image.Width, image.Height), filter);
            image.UnlockBits(bitmapData);
        }

        public void InitWithData(IntPtr data, int dataLength, SurfaceFormat pixelFormat, int width, int height, Size size, All filter)
        {
            //GG somehow these become off on NACL
            size.Width = width;
            size.Height = height;

            //NaCl.Debug.print("InitWithData: " + pixelFormat + "," +data+ "," +dataLength+ "," +width+ "," +height+ "," +size+ "," +filter);
            GL.GenTextures(1, out _name);
            GL.BindTexture(TextureTarget.Texture2D, _name);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);

            switch (pixelFormat) {
                case SurfaceFormat.Dxt1: // GG EDIT added
                    GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.CompressedRgbaS3tcDxt1Ext, (int)width, (int)height, 0, dataLength, data);
                    break;
                case SurfaceFormat.Dxt3: // GG EDIT added
#if NO_DXT35
                    return; // throw new ContentLoadException("GG TEMPORARILY NOT SUPPORTING DXT3");
#else
                    GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.CompressedRgbaS3tcDxt3Ext, (int)width, (int)height, 0, dataLength, data);
                    break;
#endif
                case SurfaceFormat.Dxt5: // GG EDIT added
#if NO_DXT35
                    break;// throw new ContentLoadException("GG TEMPORARILY NOT SUPPORTING DXT5");
#else
                    GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.CompressedRgbaS3tcDxt5Ext, (int)width, (int)height, 0, dataLength, data);
                    break;
#endif
                case SurfaceFormat.Color: // GG EDIT using BGRA because that's what uncompressed on-disk textures are stored in
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)width, (int)height, 0, GLPixelFormat.Bgra, PixelType.UnsignedByte, data);
                    break;
                case SurfaceFormat.Rgba32: // GG EDIT RGBA32 is the code word for "CPU-decompressed DXT"
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)width, (int)height, 0, GLPixelFormat.Rgba, PixelType.UnsignedByte, data);
                    break;
                case SurfaceFormat.Bgra4444 /*kTexture2DPixelFormat_RGBA4444*/:
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)width, (int)height, 0, GLPixelFormat.Rgba, PixelType.UnsignedShort4444, data);
                    break;
                case SurfaceFormat.Bgra5551 /*kTexture2DPixelFormat_RGB5A1*/:
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)width, (int)height, 0, GLPixelFormat.Rgba, PixelType.UnsignedShort5551, data);
                    break;
                case SurfaceFormat.Alpha8 /*kTexture2DPixelFormat_A8*/:
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Alpha, (int)width, (int)height, 0, GLPixelFormat.Alpha, PixelType.UnsignedByte, data);
                    break;
                default:
                    throw new NotSupportedException("Texture format " + pixelFormat);
            }
            
            _size = size;
            _width = width;
            _height = height;
            _format = pixelFormat;
            _maxS = size.Width / (float)width;
            _maxT = size.Height / (float)height;
        }
				
		public unsafe void Dispose ()
		{
			if(_name != 0) 
			{
	 			GL.DeleteTextures(1, ref _name);
			}
		}
		
		private static byte GetBits64(ulong source, int first, int length, int shift)
        {
			uint[] bitmasks = { 0x00, 0x01, 0x03, 0x07, 0x0f, 0x1f, 0x3f, 0x7f, 0xff };
            uint bitmask = bitmasks[length];
            source = source >> first;
            source = source & bitmask;
            source = source << shift;
            return (byte)source;
        }
		
		private static byte GetBits(uint source, int first, int length, int shift)
        {
			uint[] bitmasks = { 0x00, 0x01, 0x03, 0x07, 0x0f, 0x1f, 0x3f, 0x7f, 0xff };
			
            uint bitmask = bitmasks[length];
            source = source >> first;
            source = source & bitmask;
            source = source << shift;
            return (byte)source;
        }

		
		private static void SetColorFromPacked(byte[] data, int offset, byte alpha, uint packed)
        {
            byte r = (byte)(GetBits(packed, 0, 8, 0));
            byte g = (byte)(GetBits(packed, 8, 8, 0));
            byte b = (byte)(GetBits(packed, 16, 8, 0));
            data[offset] = r;
            data[offset + 1] = g;
            data[offset + 2] = b;
            data[offset + 3] = alpha;
        }

		private static void ColorsFromPacked(uint[] colors, uint c0, uint c1, bool flag)
        {
            uint rb0, rb1, rb2, rb3, g0, g1, g2, g3;

            rb0 = (c0 << 3 | c0 << 8) & 0xf800f8;
            rb1 = (c1 << 3 | c1 << 8) & 0xf800f8;
            rb0 += (rb0 >> 5) & 0x070007;
            rb1 += (rb1 >> 5) & 0x070007;
            g0 = (c0 << 5) & 0x00fc00;
            g1 = (c1 << 5) & 0x00fc00;
            g0 += (g0 >> 6) & 0x000300;
            g1 += (g1 >> 6) & 0x000300;

            colors[0] = rb0 + g0;
            colors[1] = rb1 + g1;

            if (c0 > c1 || flag)
            {
                rb2 = (((2 * rb0 + rb1) * 21) >> 6) & 0xff00ff;
                rb3 = (((2 * rb1 + rb0) * 21) >> 6) & 0xff00ff;
                g2 = (((2 * g0 + g1) * 21) >> 6) & 0x00ff00;
                g3 = (((2 * g1 + g0) * 21) >> 6) & 0x00ff00;
                colors[3] = rb3 + g3;
            }
            else
            {
                rb2 = ((rb0 + rb1) >> 1) & 0xff00ff;
                g2 = ((g0 + g1) >> 1) & 0x00ff00;
                colors[3] = 0;
            }

            colors[2] = rb2 + g2;
        }
		
		public static byte[] GetBits (int width, int length, int height, System.IO.BinaryReader rdr)
		{
			int xoffset = 0;
			int yoffset = 0;
			int rowLength = width * 4;
			byte[] b = new byte[length];
			ulong alpha;
			ushort c0, c1;
			uint[] colors = new uint[4];
			uint lu;
			for (int y = 0; y < height / 4; y++) {
				yoffset = y * 4;
				for (int x = 0; x < width / 4; x++) {
					xoffset = x * 4;
					alpha = rdr.ReadUInt64 ();
					c0 = rdr.ReadUInt16 ();
					c1 = rdr.ReadUInt16 ();
					ColorsFromPacked (colors, c0, c1, true);
					lu = rdr.ReadUInt32 ();
					for (int i = 0; i < 16; i++) {
						int idx = GetBits (lu, 30 - i * 2, 2, 0);
						uint ci = colors[idx];
						int ii = 15 - i;
						byte a = (byte)(GetBits64 (alpha, ii * 4, 4, 0));
						a += (byte)(a << 4);
						int yy = yoffset + (ii / 4);
						int xx = xoffset + (ii % 4);
						int offset = yy * rowLength + xx * 4;
						SetColorFromPacked (b, offset, a, ci);
					}
				}
			}
			return b;
		}
                
                // GG EDIT removed DrawAtPoint and DrawInRect because they weren't used and weren't NACL-compatible
                
		public Size ContentSize
		{
			get 
			{
				return _size;
			}
		}
		
		public SurfaceFormat PixelFormat
		{
			get 
			{
				return _format;
			}
		}
		
		public int PixelsWide 
		{
			get 
			{
				return _width;
			}
		}
		
		public int PixelsHigh 
		{
			get 
			{
				return _height;
			}
		}
		
		public uint Name 
		{
            get { return _name; }
		}
		
		public float MaxS 
		{
			get 
			{
				return _maxS;
			}
		}
		
		public float MaxT 
		{
			get 
			{
				return _maxT;
			}
		}
		
	}
}
