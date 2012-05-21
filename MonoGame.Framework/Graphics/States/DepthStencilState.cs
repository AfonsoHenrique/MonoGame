using System;
namespace Microsoft.Xna.Framework.Graphics
{
	public class DepthStencilState : GraphicsResource
	{
        public bool DepthBufferEnable { get; set; }
        public bool DepthBufferWriteEnable { get; set; }
        public StencilOperation CounterClockwiseStencilDepthBufferFail { get; set; }
        public StencilOperation CounterClockwiseStencilFail { get; set; }
        public CompareFunction CounterClockwiseStencilFunction { get; set; }
        public StencilOperation CounterClockwiseStencilPass { get; set; }
        public CompareFunction DepthBufferFunction { get; set; }
        public int ReferenceStencil { get; set; }
        public StencilOperation StencilDepthBufferFail { get; set; }
        public bool StencilEnable { get; set; }
        public StencilOperation StencilFail { get; set; }
        public CompareFunction StencilFunction { get; set; }
        public int StencilMask { get; set; }
        public StencilOperation StencilPass { get; set; }
        public int StencilWriteMask { get; set; }
        public bool TwoSidedStencilMode { get; set; }

		public DepthStencilState ()
		{
			CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
			CounterClockwiseStencilFail = StencilOperation.Keep;
			CounterClockwiseStencilFunction = CompareFunction.Always;
			CounterClockwiseStencilPass = StencilOperation.Keep;
            DepthBufferEnable = true;
            DepthBufferFunction = CompareFunction.LessEqual;
            DepthBufferWriteEnable = true;
			ReferenceStencil = 0;
			StencilDepthBufferFail = StencilOperation.Keep;
			StencilEnable = false;
			StencilFail = StencilOperation.Keep;
			StencilFunction = CompareFunction.Always;
			StencilMask = Int32.MaxValue;
			StencilPass = StencilOperation.Keep;
			StencilWriteMask = Int32.MaxValue;
			TwoSidedStencilMode = false;
		}
		
		static DepthStencilState defaultState;
		
		public static DepthStencilState Default {
			get {
				if (defaultState == null) {
					defaultState = new DepthStencilState () {
						DepthBufferEnable = true,
						DepthBufferWriteEnable = true
					};
				}
				
				return defaultState;
			}
		}
		
		static DepthStencilState depthReadState;
		
		public static DepthStencilState DepthRead {
			get {
				if (depthReadState == null) {
					depthReadState = new DepthStencilState () {
						DepthBufferEnable = true,
						DepthBufferWriteEnable = false
					};
				}
				
				return depthReadState;
			}
		}
		
		static DepthStencilState noneState;
		public static DepthStencilState None {
			get {
				if (noneState == null) {
					noneState = new DepthStencilState () {
						DepthBufferEnable = false,
						DepthBufferWriteEnable = false
					};
				}
				
				return noneState;
			}
		}
	}
}

