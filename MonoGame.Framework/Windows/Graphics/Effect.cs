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
using System.Collections.Generic;
using System.Text;
using NaCl.PLFS;
#if NACL
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif


namespace Microsoft.Xna.Framework.Graphics
{

	public class Effect : IDisposable
	{
        public static Effect currentEffect = null;

        internal string _name;
        internal int program_handle = -1337;
        public EffectParameterCollection Parameters { get; set; }
        public EffectTechniqueCollection Techniques { get; set; }
        public EffectTechnique CurrentTechnique { get; set; }
		private GraphicsDevice graphicsDevice;
		private int fragment_handle;
        private int vertex_handle;

        internal int position_index;
        internal int color_index;
        internal int texCoord_index;

        private int currentProgram = -1;

        // contains uniform locations for textures -- this is so you can ask "what is texture 0", etc
        internal int[] texture_locations;

        public bool IsDisposed = false; // GG TODO this should be hooked up along with the rest of the Disposable interface

        //GG EDIT
        private void Init(String assetName, String body, GraphicsDevice graphicsDevice)
        {
            _name = assetName;
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("Graphics Device Cannot Be Null");
            }
            this.graphicsDevice = graphicsDevice;

            program_handle = GL.CreateProgram();

            Parameters = new EffectParameterCollection();
            Techniques = new EffectTechniqueCollection();

            InitVertexShader("DEFAULT_VERTEX", GGShader.DEFAULT_VERTEX);
            InitFragmentShader(assetName, body);

            GL.AttachShader(program_handle, vertex_handle);
            GL.AttachShader(program_handle, fragment_handle);
            GL.LinkProgram(program_handle);

            int actUnis = 0;
            Parameters._parameters.Clear();
            List<int> texes = new List<int>();
            GL.GetProgram(program_handle, ProgramParameter.ActiveUniforms, out actUnis);
            for (int x = 0; x < actUnis; x++)
            {
                int length, size;
                ActiveUniformType type;
                StringBuilder name = new StringBuilder(100);
                GL.GetActiveUniform(program_handle, x, 100, out length, out size, out type, name);
                String fixedName = name.ToString();

                int location = GL.GetUniformLocation(program_handle, fixedName);
                if (fixedName.EndsWith("[0]"))
                {
                    fixedName = fixedName.Substring(0, fixedName.Length - 3);
                }
                Console.WriteLine("{0}: {1} {2} {3}", location, fixedName, type, size);
                EffectParameter efp = new EffectParameter(this, fixedName, location, type.ToString(), length, size);
                if (type == ActiveUniformType.Sampler2D)
                {
                    texes.Add(location);
                }

                Parameters._parameters.Add(efp.Name, efp);

                List<EffectParameter> _textureMappings = new List<EffectParameter>();

                if (efp.ParameterType == EffectParameterType.Texture2D)
                {
                    _textureMappings.Add(efp);
                }
            }
            texes.Sort();
            texture_locations = texes.ToArray();

            position_index = GL.GetAttribLocation(program_handle, "a_Position");
            color_index = GL.GetAttribLocation(program_handle, "a_Color");
            texCoord_index = GL.GetAttribLocation(program_handle, "a_TexCoord");
            
            CurrentTechnique = new EffectTechnique(this);
            CurrentTechnique.Passes[0] = new EffectPass(CurrentTechnique);
        }

        // GG EDIT added
        public void InitFragmentShader(string assetName, string body)
        {
            fragment_handle = GL.CreateShader(ShaderType.FragmentShader);
            InitShader(assetName, body, fragment_handle);
        }

        // GG EDIT added
        public void InitVertexShader(string assetName, string body)
        {
            vertex_handle = GL.CreateShader(ShaderType.VertexShader);
            InitShader(assetName, body, vertex_handle);
        }

        // GG EDIT added
        public void InitShader(string assetName, string body, int handle)
        {
            GL.ShaderSource(handle, GGShader.VARYING + body);
            GL.CompileShader(handle);

            int compiled = 0;
            GL.GetShader(handle, ShaderParameter.CompileStatus, out compiled);
            if (compiled == (int)All.False)
            {
#if NACL
                // GL.GetShaderInfoLog is broken, so uh yeah
                GSGE.Debug.logMessage("Shader source: {0}", GGShader.VARYING + body);
                GSGE.Debug.assert(false, "compiled failed");
#endif
                String log = GL.GetShaderInfoLog(handle);
                if (log != "")
                {
                    Console.WriteLine(assetName + ":" + log + "\n");
                    throw new Exception("Shader Error");
                }
            }
        }

        // GG EDIT added
        public Effect(String assetName, String body, GraphicsDevice graphicsDevice)
        {
            Init(assetName, body, graphicsDevice);
        }

        public Effect(String assetName, GraphicsDevice graphicsDevice) 
        {
            Init(assetName, GGShader.getShader(assetName), graphicsDevice);
        }

		public Effect (
         GraphicsDevice graphicsDevice,
         byte[] effectCode,
         CompilerOptions options,
         EffectPool pool)
		{
            // GG EDIT we are specializing this to the default shader because we happen to know
            // that this is only called with empty bytes
            Init("__NO_NAME__", GGShader.EmptyShader, graphicsDevice);
		}

        protected Effect(GraphicsDevice graphicsDevice, Effect cloneSource)
        {
            Parameters = new EffectParameterCollection();
            Techniques = new EffectTechniqueCollection();

            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("Graphics Device Cannot Be Null");
            }
            this.graphicsDevice = graphicsDevice;
        }

        internal virtual void Apply()
        {
            GLStateManager.Cull(graphicsDevice.RasterizerState.CullMode.OpenGL());
            // TODO: This is prolly not right (DepthBuffer, etc)
            GLStateManager.DepthTest(graphicsDevice.DepthStencilState.DepthBufferEnable);
        }
		
		public void Begin()
		{
            //GG EDIT
            GL.UseProgram(program_handle);
            currentEffect = this;
            CommitChanges();
		}
		
		public virtual Effect Clone(GraphicsDevice device)
		{
			Effect f = new Effect( graphicsDevice, this );
			return f;
		}
		
		public void Dispose()
		{
		}
		
		public void End()
		{
            //GG EDIT
            GL.UseProgram(graphicsDevice._defaultEffect.program_handle);
            currentEffect = graphicsDevice._defaultEffect;
		}

        public void CommitChanges()
        {
            if (currentProgram == -1)
            {
                GL.GetInteger(GetPName.CurrentProgram, out currentProgram);
            }
            if (currentProgram != program_handle)
            {
                GL.UseProgram(program_handle);
                CommitChangesHelper();
                GL.UseProgram(currentProgram);
            }
            else 
            {
                CommitChangesHelper();
            }
        }

        private void CommitChangesHelper() 
        {
            foreach (EffectParameter ef in Parameters)
            {
                ef._epv.setUniform(ef.internalIndex);
            }
        }

        internal static string Normalize(string FileName)
		{
			if (File.Exists(FileName))
				return FileName;
			
			// Check the file extension
			if (!string.IsNullOrEmpty(NaCl.PLFS.Path.GetExtension(FileName)))
			{
				return null;
			}
			
			// Concat the file name with valid extensions
            if (File.Exists(FileName + ".fsh"))
				return FileName+".fsh";
			if (File.Exists(FileName+".vsh"))
				return FileName+".vsh";
			
			return null;
		}

        internal Effect(GraphicsDevice device)
        {
            graphicsDevice = device;
            Parameters = new EffectParameterCollection();
            Techniques = new EffectTechniqueCollection();
            CurrentTechnique = new EffectTechnique(this);
        }

	}
}
