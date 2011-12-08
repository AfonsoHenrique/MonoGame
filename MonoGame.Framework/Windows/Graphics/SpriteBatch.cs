using System;
using System.Text;
using System.Collections.Generic;


using Microsoft.Xna.Framework;
using OpenTK;
#if NACL
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
	public class SpriteBatch : GraphicsResource
	{
        SpriteBatcher _batcher;

        SpriteSortMode _sortMode;
        BlendState _blendState;
        SamplerState _samplerState;
        DepthStencilState _depthStencilState;
        RasterizerState _rasterizerState;
        Effect _effect;
        Matrix _matrix;
		
		public SpriteBatch ( GraphicsDevice graphicsDevice )
		{
			if (graphicsDevice == null )
			{
				throw new ArgumentException("graphicsDevice");
			}	
			
			this.graphicsDevice = graphicsDevice;

            _batcher = new SpriteBatcher();
            SpriteBatcher._graphicsDevice = graphicsDevice;

		}

        public void Begin()
		{
			Begin( SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity );			
		}

        public void Begin(SpriteBlendMode blendMode)
        {
            // GG EDIT
            Begin(SpriteSortMode.Deferred, BlendState.FromSpriteBlendMode(blendMode), SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);			
        }

        public void Begin(SpriteBlendMode blendMode, SpriteSortMode sortMode, SaveStateMode saveStateMode)
        {
            // GG EDIT
            // GG TODO implement saveStateMode
            Begin(sortMode, BlendState.FromSpriteBlendMode(blendMode), SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);			
        }

        // GG EDIT removed uncalled XNA 4.0 Begin methods, for confusion reduction

		public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
		{
			_sortMode = sortMode;

			_blendState = blendState ?? BlendState.AlphaBlend;
			_depthStencilState = depthStencilState ?? DepthStencilState.None;
			_samplerState = samplerState ?? SamplerState.LinearClamp;
			_rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
                        UseBlendState();
			
			if(effect != null)
				_effect = effect;
			_matrix = transformMatrix;
		}

        public void UseBlendState()
        {
            // set the blend mode
            if (_blendState == BlendState.NonPremultiplied ||
                _blendState == BlendState.AlphaBlend ||
                _blendState == BlendState.Additive)
            {
                GL.Enable(EnableCap.Blend);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
            }
        }

		public void End()
		{
            // set up camera
            switch (this.graphicsDevice.PresentationParameters.DisplayOrientation)
	        {
				case DisplayOrientation.LandscapeLeft:
				case DisplayOrientation.LandscapeRight:
				case DisplayOrientation.PortraitUpsideDown:
                    throw new NotImplementedException();
				default:
                    if (this.graphicsDevice.RenderTarget != null)
                    {
                        Matrix4.CreateOrthographicOffCenter(0, this.graphicsDevice.RenderTarget.Width, this.graphicsDevice.RenderTarget.Height, 0, -1, 1, out GLStateManager.Projection);
                        break;
                    }
                    else 
                    {
                        Matrix4.CreateOrthographicOffCenter(0, this.graphicsDevice.Viewport.Width, this.graphicsDevice.Viewport.Height, 0, -1, 1, out GLStateManager.Projection);
                        break;
                    }
			}

			GL.Viewport(this.graphicsDevice.Viewport.X, this.graphicsDevice.Viewport.Y, 
                        this.graphicsDevice.Viewport.Width, this.graphicsDevice.Viewport.Height);

			// Enable Scissor Tests if necessary
			if ( this.graphicsDevice.RenderState.ScissorTestEnable )
            {
                GL.Enable(EnableCap.ScissorTest);
				GL.Scissor(this.graphicsDevice.ScissorRectangle.X, this.graphicsDevice.ScissorRectangle.Y, this.graphicsDevice.ScissorRectangle.Width, this.graphicsDevice.ScissorRectangle.Height );
			}

            GLStateManager.ModelView = _matrix.ToMatrix4();
						
			// Initialize OpenGL states (ideally move this to initialize somewhere else)	
			GLStateManager.DepthTest(false);
            GLStateManager.Textures2D(true);
			
			// Enable Culling for better performance
            GLStateManager.Cull(FrontFaceDirection.Cw);
		
            switch( _sortMode )
            {
                case SpriteSortMode.Immediate:
                    break;

                default:
                    this.graphicsDevice.RenderState.SourceBlend = _blendState.ColorSourceBlend;
                    this.graphicsDevice.RenderState.DestinationBlend = _blendState.ColorDestinationBlend;
                    break;
            }

            GLStateManager.BlendFunc( (BlendingFactorSrc)this.graphicsDevice.RenderState.SourceBlend, (BlendingFactorDest)this.graphicsDevice.RenderState.DestinationBlend );

			_batcher.DrawBatch ( _sortMode );

            // GG EDIT always disable scissor test after drawing a batch
            if (this.graphicsDevice.RenderState.ScissorTestEnable)
            {
                GL.Disable(EnableCap.ScissorTest);
            }
			
		}
		
		public void Draw(Texture2D texture, Vector2 position, Nullable<Rectangle> sourceRectangle, Color color, float rotation,
			 Vector2 origin, Vector2 scale, SpriteEffects effect, float depth )
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = depth;
			item.TextureID = (int) texture.ID;
			
			Rectangle rect;
			if ( sourceRectangle.HasValue)
				rect = sourceRectangle.Value;
			else
				rect = new Rectangle( 0, 0, texture.Image.ImageWidth, texture.Image.ImageHeight );
						
			Vector2 texCoordTL = texture.Image.GetTextureCoord ( rect.X, rect.Y );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( rect.X+rect.Width, rect.Y+rect.Height );
			
			if ( (effect & SpriteEffects.FlipVertically) != 0 )
			{
				float temp = texCoordBR.Y;
				texCoordBR.Y = texCoordTL.Y;
				texCoordTL.Y = temp;
			}
			if ( (effect & SpriteEffects.FlipHorizontally) != 0 )
			{
				float temp = texCoordBR.X;
				texCoordBR.X = texCoordTL.X;
				texCoordTL.X = temp;
			}
			
			item.Set
				(
				 position.X,
				 position.Y,
				 -origin.X*scale.X,
				 -origin.Y*scale.Y,
				 rect.Width*scale.X,
				 rect.Height*scale.Y,
				 (float)Math.Sin(rotation),
				 (float)Math.Cos(rotation),
				 color,
				 texCoordTL,
				 texCoordBR
				 );
		}


        public void Draw(Texture2D imgTexture, Texture2D locTexture, Vector2 position, Nullable<Rectangle> sourceRectangle, Color color, float rotation,
     Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
        {//GG EDIT THIS IS NEW
            if (locTexture == null || imgTexture == null)
            {
                throw new ArgumentException("texture");
            }

            SpriteBatchItem item = _batcher.CreateBatchItem();

            item.Depth = depth;
            item.TextureID = (int)imgTexture.ID;

            Rectangle rect;
            if (sourceRectangle.HasValue)
                rect = sourceRectangle.Value;
            else
                rect = new Rectangle(0, 0, locTexture.Image.ImageWidth, locTexture.Image.ImageHeight);

            Vector2 texCoordTL = locTexture.Image.GetTextureCoord(rect.X, rect.Y);
            Vector2 texCoordBR = locTexture.Image.GetTextureCoord(rect.X + rect.Width, rect.Y + rect.Height);

            if ((effect & SpriteEffects.FlipVertically) != 0)
            {
                float temp = texCoordBR.Y;
                texCoordBR.Y = texCoordTL.Y;
                texCoordTL.Y = temp;
            }
            if ((effect & SpriteEffects.FlipHorizontally) != 0)
            {
                float temp = texCoordBR.X;
                texCoordBR.X = texCoordTL.X;
                texCoordTL.X = temp;
            }

            item.Set
                (
                 position.X,
                 position.Y,
                 -origin.X * scale.X,
                 -origin.Y * scale.Y,
                 rect.Width * scale.X,
                 rect.Height * scale.Y,
                 (float)Math.Sin(rotation),
                 (float)Math.Cos(rotation),
                 color,
                 texCoordTL,
                 texCoordBR
                 );
        }



		public void Draw( Texture2D texture, Rectangle destinationRectangle, Nullable<Rectangle> sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth )
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = depth;
			item.TextureID = (int) texture.ID;
			
			Rectangle rect;
			if ( sourceRectangle.HasValue)
				rect = sourceRectangle.Value;
			else
				rect = new Rectangle( 0, 0, texture.Image.ImageWidth, texture.Image.ImageHeight );

			Vector2 texCoordTL = texture.Image.GetTextureCoord ( rect.X, rect.Y );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( rect.X+rect.Width, rect.Y+rect.Height );
			if ( (effect & SpriteEffects.FlipVertically) != 0 )
			{
				float temp = texCoordBR.Y;
				texCoordBR.Y = texCoordTL.Y;
				texCoordTL.Y = temp;
			}
			if ( (effect & SpriteEffects.FlipHorizontally) != 0 )
			{
				float temp = texCoordBR.X;
				texCoordBR.X = texCoordTL.X;
				texCoordTL.X = temp;
			}
			
			item.Set 
				( 
				 destinationRectangle.X, 
				 destinationRectangle.Y, 
				 -origin.X, 
				 -origin.Y, 
				 destinationRectangle.Width,
				 destinationRectangle.Height,
				 (float)Math.Sin(rotation),
				 (float)Math.Cos(rotation),
				 color,
				 texCoordTL,
				 texCoordBR );
		}
		
        public void Draw( Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = 0.0f;
			item.TextureID = (int) texture.ID;
			
			Rectangle rect;
			if ( sourceRectangle.HasValue)
				rect = sourceRectangle.Value;
			else
				rect = new Rectangle( 0, 0, texture.Image.ImageWidth, texture.Image.ImageHeight );
			
			Vector2 texCoordTL = texture.Image.GetTextureCoord ( rect.X, rect.Y );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( rect.X+rect.Width, rect.Y+rect.Height );
			
			item.Set ( position.X, position.Y, rect.Width, rect.Height, color, texCoordTL, texCoordBR );
		}
		
		public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = 0.0f;
			item.TextureID = (int) texture.ID;
			
			Rectangle rect;
			if ( sourceRectangle.HasValue)
				rect = sourceRectangle.Value;
			else
				rect = new Rectangle( 0, 0, texture.Image.ImageWidth, texture.Image.ImageHeight );
			
			Vector2 texCoordTL = texture.Image.GetTextureCoord ( rect.X, rect.Y );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( rect.X+rect.Width, rect.Y+rect.Height );
			
			item.Set 
				( 
				 destinationRectangle.X, 
				 destinationRectangle.Y, 
				 destinationRectangle.Width, 
				 destinationRectangle.Height, 
				 color, 
				 texCoordTL, 
				 texCoordBR );
		}
		
		public void Draw( Texture2D texture, Vector2 position, Color color)
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = 0;
			item.TextureID = (int) texture.ID;
			
			Rectangle rect = new Rectangle( 0, 0, texture.Image.ImageWidth, texture.Image.ImageHeight );
			
			Vector2 texCoordTL = texture.Image.GetTextureCoord ( rect.X, rect.Y );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( rect.X+rect.Width, rect.Y+rect.Height );
			
			item.Set 
				(
				 position.X,
			     position.Y,
				 rect.Width,
				 rect.Height,
				 color,
				 texCoordTL,
				 texCoordBR
				 );

		}
		
		public void Draw( Texture2D texture, Rectangle rectangle, Color color)
		{
			if (texture == null )
			{
				throw new ArgumentException("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem();
			
			item.Depth = 0;
			item.TextureID = (int) texture.ID;
			
			Vector2 texCoordTL = texture.Image.GetTextureCoord ( 0, 0 );
			Vector2 texCoordBR = texture.Image.GetTextureCoord ( texture.Image.ImageWidth, texture.Image.ImageHeight );
			
			item.Set
				(
				 rectangle.X,
				 rectangle.Y,
				 rectangle.Width,
				 rectangle.Height,
				 color,
				 texCoordTL,
				 texCoordBR
			    );
		}
		
		
		public void DrawString( SpriteFont spriteFont, string text, Vector2 position, Color color)
		{
			if (spriteFont == null )
			{
				throw new ArgumentException("spriteFont");
			}
			
			Vector2 p = position;
			
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    p.Y += spriteFont.LineSpacing;
                    p.X = position.X;
                    continue;
                }
                if (spriteFont.characterData.ContainsKey(c) == false) 
					continue;
                GlyphData g = spriteFont.characterData[c];
				
				SpriteBatchItem item = _batcher.CreateBatchItem();
				
				item.Depth = 0.0f;
				item.TextureID = (int) spriteFont._texture.ID;

				Vector2 texCoordTL = spriteFont._texture.Image.GetTextureCoord ( g.Glyph.X, g.Glyph.Y );
				Vector2 texCoordBR = spriteFont._texture.Image.GetTextureCoord ( g.Glyph.X+g.Glyph.Width, g.Glyph.Y+g.Glyph.Height );

				item.Set
					(
					 p.X,
					 p.Y+g.Cropping.Y,
					 g.Glyph.Width,
					 g.Glyph.Height,
					 color,
					 texCoordTL,
					 texCoordBR
					 );

                p.X += (g.Kerning.Y + g.Kerning.Z + spriteFont.Spacing);
            }			
		}
		
		public void DrawString( SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
		{

			if (spriteFont == null )
			{
				throw new ArgumentException("spriteFont");
			}
			
			Vector2 p = new Vector2(-origin.X,-origin.Y);
			
			float sin = (float)Math.Sin(rotation);
			float cos = (float)Math.Cos(rotation);
			
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    p.Y += spriteFont.LineSpacing;
                    p.X = -origin.X;
                    continue;
                }
                if (spriteFont.characterData.ContainsKey(c) == false) 
					continue;
                GlyphData g = spriteFont.characterData[c];
				
				SpriteBatchItem item = _batcher.CreateBatchItem();
				
				item.Depth = depth;
				item.TextureID = (int) spriteFont._texture.ID;

				Vector2 texCoordTL = spriteFont._texture.Image.GetTextureCoord ( g.Glyph.X, g.Glyph.Y );
				Vector2 texCoordBR = spriteFont._texture.Image.GetTextureCoord ( g.Glyph.X+g.Glyph.Width, g.Glyph.Y+g.Glyph.Height );
				
				if ( effects == SpriteEffects.FlipVertically )
				{
					float temp = texCoordBR.Y;
					texCoordBR.Y = texCoordTL.Y;
					texCoordTL.Y = temp;
				}
				else if ( effects == SpriteEffects.FlipHorizontally )
				{
					float temp = texCoordBR.X;
					texCoordBR.X = texCoordTL.X;
					texCoordTL.X = temp;
				}
				
				item.Set
					(
					 position.X,
					 position.Y,
					 p.X*scale,
					 (p.Y+g.Cropping.Y)*scale,
					 g.Glyph.Width*scale,
					 g.Glyph.Height*scale,
					 sin,
					 cos,
					 color,
					 texCoordTL,
					 texCoordBR
					 );

				p.X += (g.Kerning.Y + g.Kerning.Z + spriteFont.Spacing);
            }			
		}
		
		public void DrawString( SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
		{
			DrawString( spriteFont, text.ToString(), position, color );
		}
		
		public void DrawString
			(
			SpriteFont spriteFont, 
			StringBuilder text, 
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float depth
			)
		{
			DrawString ( spriteFont, text.ToString(), position, color, rotation, origin, scale, effects, depth );
		}
	}
}