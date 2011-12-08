// #region License
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
// #endregion License
// 
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if NACL
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class FrameStats
    {
        internal static long rects = 0;
        internal static int batches = 0;
        internal static int draws = 0;
        internal static int textures = 0;

        internal static void startFrame()
        {
            rects = batches = draws = textures = 0;
        }

        internal static void printStats()
        {
            NaCl.Debug.print(String.Format("Frame Stats: rects {0} draws {1} textures {2} batches {3} rects/draw {4} rects/batch {5}", rects, draws, textures, batches, draws == 0 ? 0 : rects / draws, batches == 0 ? 0 : rects / batches));
        }
    }

    internal class SpriteBatcher
    {
        public static GraphicsDevice _graphicsDevice = null;
        List<SpriteBatchItem> _batchItemList;
        Queue<SpriteBatchItem> _freeBatchItemQueue;
        VertexPosition2ColorTexture[] _vertexArray;
        GCHandle _vertexHandle;
        Int32 _vertexVbo;

        static bool isDrawingBatch = false;

        static int sNumIndices = 0;
        static ushort[] sIndices;
        static GCHandle sIndexHandle;
        static Int32 sIndexVbo = -1;
        static Int32 sCurrentVbo = -1;

        static void ensureIndexCapacity(int indices)
        {
            if (indices > 65532 * 6 / 4)
                throw new ArgumentException("Too many indices");
            if (sNumIndices >= indices)
                return;
            if (sNumIndices == 0) // special case: starting condition
                sNumIndices = 128 * 6; // should always be a multiple of 6
            if (sIndexVbo == -1) // starting condition
                GL.GenBuffers(1, out sIndexVbo);
            while (indices > sNumIndices)
                sNumIndices *= 2;
            if (sNumIndices > 65532 * 6 / 4)
                sNumIndices = 65532 * 6 / 4;  // can have more indices than 65536, but can only address vertex 65532
            if (sIndexHandle.IsAllocated)
                sIndexHandle.Free();
            sIndices = new ushort[sNumIndices];
            sIndexHandle = GCHandle.Alloc(sIndices, GCHandleType.Pinned);
            for (int i = 0; i < sNumIndices / 6; i++)
            {
                sIndices[i * 6 + 0] = (ushort)(i * 4);
                sIndices[i * 6 + 1] = (ushort)(i * 4 + 1);
                sIndices[i * 6 + 2] = (ushort)(i * 4 + 2);
                sIndices[i * 6 + 3] = (ushort)(i * 4 + 1);
                sIndices[i * 6 + 4] = (ushort)(i * 4 + 3);
                sIndices[i * 6 + 5] = (ushort)(i * 4 + 2);
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, sIndexVbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sNumIndices * sizeof(ushort)), sIndexHandle.AddrOfPinnedObject(),
#if NACL
 OpenTK.Graphics.ES20.BufferUsage.StaticDraw);
#else
                BufferUsageHint.StaticDraw);
#endif
        }

        public static void startFrame()
        {
            if (sIndexVbo != -1) // make sure it's been initialized
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, sIndexVbo);
            sCurrentVbo = -1; // force at least one BindBuffer
            //FrameStats.printStats();
            FrameStats.startFrame();
        }

        public SpriteBatcher()
        {
            int initialRects = 256;
            _batchItemList = new List<SpriteBatchItem>(initialRects);
            _freeBatchItemQueue = new Queue<SpriteBatchItem>(initialRects);

            _vertexArray = new VertexPosition2ColorTexture[4 * initialRects];
            _vertexHandle = GCHandle.Alloc(_vertexArray, GCHandleType.Pinned);
            GL.GenBuffers(1, out _vertexVbo);
            ensureIndexCapacity(6 * initialRects);
        }

        public SpriteBatchItem CreateBatchItem()
        {
            SpriteBatchItem item;
            if (_freeBatchItemQueue.Count > 0)
                item = _freeBatchItemQueue.Dequeue();
            else
                item = new SpriteBatchItem();
            _batchItemList.Add(item);
            return item;
        }
        int CompareTexture(SpriteBatchItem a, SpriteBatchItem b)
        {
            return a.TextureID.CompareTo(b.TextureID);
        }
        int CompareDepth(SpriteBatchItem a, SpriteBatchItem b)
        {
            return a.Depth.CompareTo(b.Depth);
        }
        int CompareReverseDepth(SpriteBatchItem a, SpriteBatchItem b)
        {
            return b.Depth.CompareTo(a.Depth);
        }
        public void DrawBatch(SpriteSortMode sortMode)
        {
            if (_batchItemList.Count == 0)
                return; // nothing to do

            if (isDrawingBatch)
                NaCl.Debug.print("double drawing batch!!!!!");
            isDrawingBatch = true;

            FrameStats.rects += _batchItemList.Count;
            FrameStats.batches++;

            // sort the batch items
            switch (sortMode)
            {
                case SpriteSortMode.Texture:
                    _batchItemList.Sort(CompareTexture);
                    break;
                case SpriteSortMode.FrontToBack:
                    _batchItemList.Sort(CompareDepth);
                    break;
                case SpriteSortMode.BackToFront:
                    _batchItemList.Sort(CompareReverseDepth);
                    break;
            }

            int vsize = Marshal.SizeOf(typeof(VertexPosition2ColorTexture));

            // setup the vertexArray array
            int startIndex = 0;
            int index = 0;
            int texID = -1;
            bool first = true;
            Effect effect = null;

            // make sure the vertexArray has enough space
            if (_batchItemList.Count * 4 > _vertexArray.Length)
                ExpandVertexArray(_batchItemList.Count);

            // build up the vertex array from SpriteBatchItems
            foreach (SpriteBatchItem item in _batchItemList)
            {
                _vertexArray[index++] = item.vertexTL;
                _vertexArray[index++] = item.vertexTR;
                _vertexArray[index++] = item.vertexBL;
                _vertexArray[index++] = item.vertexBR;
            }

            if (sCurrentVbo != _vertexVbo)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexVbo);
                sCurrentVbo = _vertexVbo;
            }
            // start transferring bytes to the renderer
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vsize * _batchItemList.Count * 4),
                _vertexHandle.AddrOfPinnedObject(),
#if NACL
 OpenTK.Graphics.ES20.BufferUsage.DynamicDraw);
#else
                BufferUsageHint.DynamicDraw);
#endif

            index = 0;
            foreach (SpriteBatchItem item in _batchItemList)
            {
                // need to switch shaders if that's different between items
                if (first || item.effect != effect)
                {
                    first = false;
                    FlushVertexArray(startIndex, index);

                    startIndex = index;
                    effect = item.effect;
                    GL.UseProgram(effect.program_handle);

                    // GG TODO: all of the following only need to happen once per program, not every draw
                    // vertex data
                    GL.VertexAttribPointer(effect.position_index,
                        2, VertexAttribPointerType.Float, false, vsize, 0);
                    GL.EnableVertexAttribArray(effect.position_index);

                    // color data
                    if (effect.color_index >= 0)
                    {
                        // note that this is normalized because it's stored as bytes
                        GL.VertexAttribPointer(effect.color_index,
                            4, VertexAttribPointerType.UnsignedByte, true, vsize, (IntPtr)(sizeof(float) * 2));
                        GL.EnableVertexAttribArray(effect.color_index);
                    }

                    // texcoord data
                    if (effect.texCoord_index >= 0)
                    {
                        GL.VertexAttribPointer(effect.texCoord_index,
                            2, VertexAttribPointerType.Float, false, vsize, (IntPtr)(sizeof(float) * 2 + sizeof(uint)));
                        GL.EnableVertexAttribArray(effect.texCoord_index);
                    }

                    // pass along the matrices as uniforms
                    GL.UniformMatrix4(effect.Parameters["u_projection"].internalIndex,
                        false, ref GLStateManager.Projection);
                    GL.UniformMatrix4(effect.Parameters["u_modelview"].internalIndex,
                        false, ref GLStateManager.ModelView);
                }

                // if the texture changed, we need to flush and bind the new texture
                if (item.TextureID != texID && effect.texture_locations.Length > 0)
                {
                    FlushVertexArray(startIndex, index);
                    FrameStats.textures++;
                    startIndex = index;
                    texID = item.TextureID;
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, texID);
                    GL.Uniform1(effect.texture_locations[0], 0);
                }
                index += 4;

                _freeBatchItemQueue.Enqueue(item);
            }
            // flush the remaining vertexArray data
            FlushVertexArray(startIndex, index);

            _batchItemList.Clear();

            _graphicsDevice.RenderState.SourceBlend = (Blend)BlendingFactorSrc.SrcAlpha;
            _graphicsDevice.RenderState.DestinationBlend = (Blend)BlendingFactorDest.OneMinusSrcAlpha;
            isDrawingBatch = false;
        }

        void FlushVertexArray(int start, int end)
        {
            // draw stuff
            if (start != end)
            {
                FrameStats.draws++;
                // assumes GL.BindBuffer already called for indices and vertices
                GL.DrawElements(BeginMode.Triangles, (end - start) * 6 / 4, DrawElementsType.UnsignedShort, start * 6 / 4 * sizeof(ushort));
            }
        }

        void ExpandVertexArray(int batchSize)
        {
            // increase the size of the vertexArray
            int newCount = _vertexArray.Length / 4;

            while (batchSize * 4 > newCount)
                newCount += 128;

            _vertexHandle.Free();
            _vertexArray = new VertexPosition2ColorTexture[4 * newCount];
            _vertexHandle = GCHandle.Alloc(_vertexArray, GCHandleType.Pinned);
            ensureIndexCapacity(6 * newCount);
        }
    }
}

