using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

#if NACL
using OpenTK.Graphics.ES20;
using GL = OpenTK.Graphics.ES20.GL;
using All = OpenTK.Graphics.ES20.All;
#else
using OpenTK.Graphics.ES11;
using GL = OpenTK.Graphics.ES11.GL;
using All = OpenTK.Graphics.ES11.All;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public class VertexBuffer : IDisposable
    {
        private GraphicsDevice Graphics;
        internal Type _type;
        private int _vertexCount;
        private BufferUsage _bufferUsage;
        internal object _buffer = null;
        internal IntPtr _bufferPtr;
        internal int _bufferIndex = 0;
		internal int _size;		
        internal static int _bufferCount = 0;
		internal uint _bufferStore; 
		// allow for 50 buffers initially
		internal static VertexBuffer[] _allBuffers = new VertexBuffer[50];
		internal static List<Action> _delayedBufferDelegates = new List<Action>();

        public VertexBuffer(GraphicsDevice graphicsDevice, int sizeInBytes, BufferUsage usage)
        {
            // GG EDIT
            this.Graphics = graphicsDevice;
            this._size = sizeInBytes;
            this._bufferUsage = usage;
        }

        public VertexBuffer(GraphicsDevice Graphics, Type type, int vertexCount, BufferUsage bufferUsage)
        {
            this.Graphics = Graphics;
            this._type = type;
            this._vertexCount = vertexCount;
            this._bufferUsage = bufferUsage;
        }
        
		public int VertexCount { get; set; }
		
		internal static void CreateFrameBuffers()
		{
			foreach (var action in _delayedBufferDelegates)
				action.Invoke();
			
			_delayedBufferDelegates.Clear();
		}
		
		internal void GenerateBuffer<T>() where T : struct, IVertexType
		{
			var vd = VertexDeclaration.FromType(_type);
			
			_size = vd.VertexStride * ((T[])_buffer).Length;
			
            // GG TODO commented this out to do NACL, should uncomment and fix
            //All bufferUsage = (_bufferUsage == BufferUsage.WriteOnly) ? All.StaticDraw : All.DynamicDraw;
			
            _bufferStore += 1; // disable unused variable warning
            //GL.GenBuffers(1, ref _bufferStore);
            //GL.BindBuffer(All.ArrayBuffer, _bufferStore);
            //GL.BufferData<T>(All.ArrayBuffer, (IntPtr)_size, (T[])_buffer, bufferUsage);			
		}
		
        public unsafe void GetData<T>(T[] vertices) where T : IVertexType
        {
            if (_buffer == null)
                throw new Exception("Can't get data on an empty buffer");

            var _tbuff = (T[])_buffer;
            for (int i = 0; i < _tbuff.Length; i++)
                vertices[i] = _tbuff[i];
        }

        public unsafe void SetData<T>(T[] vertices) where T : struct, IVertexType
        {
			//the creation of the buffer should mb be moved to the constructor and then glMapBuffer and Unmap should be used to update it
			//glMapBuffer - sets data
			//glUnmapBuffer - finished setting data
			
            _buffer = vertices;
            _bufferPtr = GCHandle.Alloc(_buffer, GCHandleType.Pinned).AddrOfPinnedObject();			
			
			_bufferIndex = _bufferCount + 1;
			_allBuffers[_bufferIndex] = this;
			
			_delayedBufferDelegates.Add(GenerateBuffer<T>);
			
            _bufferCount++;
            // TODO: Kill buffers in PhoneOSGameView.DestroyFrameBuffer()
        }
		
		public void Dispose ()
		{
            // GG TODO commented this out to do NACL, should uncomment and fix
			//GL.GenBuffers(0, ref _bufferStore);
		}
    }
	
    public class DynamicVertexBuffer : VertexBuffer
    {
        public DynamicVertexBuffer(GraphicsDevice graphics, Type type, int vertexCount, BufferUsage bufferUsage)
            : base(graphics, type, vertexCount, bufferUsage)
        {
        }
    }
}
