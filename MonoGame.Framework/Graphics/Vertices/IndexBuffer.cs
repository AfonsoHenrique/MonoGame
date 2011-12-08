using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NACL
using GL = OpenTK.Graphics.ES20.GL;
using All = OpenTK.Graphics.ES20.All;
#else
using GL = OpenTK.Graphics.ES11.GL;
using All = OpenTK.Graphics.ES11.All;
#endif

using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    public class IndexBuffer
    {
        private GraphicsDevice _graphics;
        internal Type _type;
        internal int _count;
		private object _buffer;
		internal IntPtr _bufferPtr;
		internal IntPtr _sizePtr = default(IntPtr);
        private BufferUsage _bufferUsage;
		internal static IndexBuffer[] _allBuffers = new IndexBuffer[50];
		internal static int _bufferCount = 0;
		internal int _bufferIndex;
		internal int _size;
		internal uint _bufferStore;
		internal static List<Action> _delayedBufferDelegates = new List<Action>(); 
		
		internal static void CreateFrameBuffers()
		{
			foreach (var action in _delayedBufferDelegates)
				action.Invoke();
			
			_delayedBufferDelegates.Clear();
		}		
		
        public IndexBuffer(GraphicsDevice Graphics, Type type, int count, BufferUsage bufferUsage)
        {
			if (type != typeof(uint) && type != typeof(ushort) && type != typeof(byte))
				throw new NotSupportedException("The only types that are supported are: uint, ushort and byte");
			
            this._graphics = Graphics;
            this._type = type;
            this._count = count;
            this._bufferUsage = bufferUsage;
        }
        
		internal void GenerateBuffer<T>() where T : struct
		{
            // GG TODO commented this out to do NACL, should uncomment and fix
            //All bufferUsage = (_bufferUsage == BufferUsage.WriteOnly) ? All.StaticDraw : All.DynamicDraw;
            _bufferStore += 1; // disable unused variable warning
            //GL.GenBuffers(1, ref _bufferStore);
            //GL.BindBuffer(All.ElementArrayBuffer, _bufferStore);
            //GL.BufferData<T>(All.ElementArrayBuffer, (IntPtr)_size, (T[])_buffer, bufferUsage);			
		}
		
		public void SetData<T>(T[] indicesData) where T : struct
        {
			_bufferIndex = _bufferCount + 1;
            _buffer = indicesData;
			_size = indicesData.Length * Marshal.SizeOf(_type);
            _bufferPtr = GCHandle.Alloc(_buffer, GCHandleType.Pinned).AddrOfPinnedObject();
			_delayedBufferDelegates.Add(GenerateBuffer<T>);

			_allBuffers[_bufferIndex] = this;			
        }
		
		public void Dispose ()
		{
            // GG TODO commented this out to do NACL, should uncomment and fix
			//GL.GenBuffers(0, ref _bufferStore);
		}		
    }

	
	
    public class DynamicIndexBuffer : IndexBuffer
    {
        public DynamicIndexBuffer(GraphicsDevice Graphics, Type type, int count, BufferUsage bufferUsage) : base(Graphics, type, count, bufferUsage)
        {
        }
    }

}
