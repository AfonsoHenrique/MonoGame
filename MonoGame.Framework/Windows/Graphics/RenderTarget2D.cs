#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

using System;

#if NACL
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif
namespace Microsoft.Xna.Framework.Graphics
{
    public class RenderTarget2D : Texture2D
	{
        //GG EDIT
        public uint framebufferID;
        
        public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
            : this(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Unknown)
        { }

        public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
            : this(graphicsDevice, width, height, mipMap, preferredFormat,
                DepthFormat.Unknown, 0, RenderTargetUsage.PreserveContents)
        { }

        public RenderTarget2D(
            GraphicsDevice graphicsDevice, int width, int height, int numberLevels,
            SurfaceFormat format, MultiSampleType multiSampleType, int multiSampleQuality)
            : this(graphicsDevice, width, height, numberLevels > 1, format,
                   DepthFormat.Unknown, 0, RenderTargetUsage.PreserveContents)
        { }

        public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
            : base(graphicsDevice, width, height, mipMap, preferredFormat)
        {
            allocateOpenGLTexture();


            // GG EDIT
            framebufferID = 0;
            GL.GenFramebuffers(1, out framebufferID);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferID);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if NACL
                FramebufferSlot.ColorAttachment0,
#else
                FramebufferAttachment.ColorAttachment0,
#endif
                TextureTarget.Texture2D, 
                textureId, 
                0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

#if DEBUG
            // GG EDIT added this assert
            FramebufferErrorCode err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (err != FramebufferErrorCode.FramebufferComplete)
            {
                throw new ApplicationException("Framebuffer set up improperly");
            }
#endif
        }

        private void allocateOpenGLTexture()
        {
            // modeled after this
            // http://steinsoft.net/index.php?site=Programming/Code%20Snippets/OpenGL/no9

            // Allocate the space needed for the texture
            GL.BindTexture(TextureTarget.Texture2D, this.textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public Texture2D GetTexture()
        {
            return this; // GG TODO
        }
	}
}
