﻿/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///    Walt Niehoff
///     

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
    #region Materials class
	public class Materials
    {
        public static SceneryShader SceneryShader = null;
        public static SkyShader SkyShader = null;
        public static PrecipShader PrecipShader = null;
        public static LightGlowShader LightGlowShader = null;
        public static SpriteBatchMaterial SpriteBatchMaterial = null;
		private static Dictionary<string, WaterMaterial> WaterMaterials = new Dictionary<string, WaterMaterial>();
		private static SkyMaterial SkyMaterial = null;
        private static PrecipMaterial PrecipMaterial = null;
        private static DynatrackMaterial DynatrackMaterial = null;
        private static LightGlowMaterial LightGlowMaterial = null;
        private static Dictionary<string, TerrainMaterial> TerrainMaterials = new Dictionary<string, TerrainMaterial>();
        private static Dictionary<string, ForestMaterial> ForestMaterials = new Dictionary<string, ForestMaterial>();
        private static Dictionary<string, SceneryMaterial> SceneryMaterials = new Dictionary<string, SceneryMaterial>();
		private static Dictionary<string, SignalLightMaterial> SignalLightMaterials = new Dictionary<string, SignalLightMaterial>();
        public static Texture2D MissingTexture = null;  // sub this when we are missing the required texture
		public static Material YellowMaterial = null;   // for debug and experiments
        public static ShadowMapMaterial ShadowMapMaterial = null;
        public static ShadowMapShader ShadowMapShader = null;
        public static Color FogColor = new Color(110, 110, 110, 255);
        public static float FogCoeff = 0.75f;
		public static PopupWindowMaterial PopupWindowMaterial = null;
		public static PopupWindowShader PopupWindowShader = null;
		private static bool IsInitialized = false;
        
        /// <summary>
        /// THREAD SAFETY:  XNA Content Manager is not thread safe and must only be called from the Game thread.
        /// ( per Shawn Hargreaves )
        /// </summary>
        /// <param name="renderProcess"></param>
		public static void Initialize(RenderProcess renderProcess)
        {
            SceneryShader = new SceneryShader(renderProcess.GraphicsDevice, renderProcess.Content);
            SceneryShader.NormalMap_Tex = MSTS.ACEFile.Texture2DFromFile(renderProcess.GraphicsDevice, 
                                                        renderProcess.Viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace");
            SkyShader = new SkyShader(renderProcess.GraphicsDevice, renderProcess.Content);
            PrecipShader = new PrecipShader(renderProcess.GraphicsDevice, renderProcess.Content);
            LightGlowShader = new LightGlowShader(renderProcess.GraphicsDevice, renderProcess.Content);
            SpriteBatchMaterial = new SpriteBatchMaterial(renderProcess);
            // WaterMaterial here.
            SkyMaterial = new SkyMaterial(renderProcess);
            PrecipMaterial = new PrecipMaterial(renderProcess);
            DynatrackMaterial = new DynatrackMaterial(renderProcess);
            LightGlowMaterial = new LightGlowMaterial(renderProcess);
            MissingTexture = renderProcess.Content.Load<Texture2D>("blank");
            YellowMaterial = new YellowMaterial(renderProcess);
            ShadowMapMaterial = new ShadowMapMaterial(renderProcess);
            ShadowMapShader = new ShadowMapShader(renderProcess.GraphicsDevice, renderProcess.Content);
			PopupWindowMaterial = new PopupWindowMaterial(renderProcess);
			PopupWindowShader = new PopupWindowShader(renderProcess.GraphicsDevice, renderProcess.Content);
            IsInitialized = true;
        }

		public static Material Load(RenderProcess renderProcess, string materialName)
        {
            return Load(renderProcess, materialName, null, 0, 0);
        }
		public static Material Load(RenderProcess renderProcess, string materialName, string textureName)
        {
            return Load(renderProcess, materialName, textureName, 0, 0);
        }

		public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options)
        {
            return Load(renderProcess, materialName, textureName, options, 0);
        }

		public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options, float mipMapBias )
        {
            System.Diagnostics.Debug.Assert(IsInitialized, "Must initialize Materials before using.");
            if (!IsInitialized)             // this shouldn't happen, but if it does
            {
                Trace.TraceWarning("Program Bug: Must initialize Materials before using.");
                Initialize(renderProcess);  // warn, and do it now rather than fail
            }

            if( textureName != null )
                textureName = textureName.ToLower();

            switch (materialName)
            {
                case "SpriteBatch":
                    return SpriteBatchMaterial;
                case "Terrain":
                    if (!TerrainMaterials.ContainsKey(textureName))
                    {
                        TerrainMaterial material = new TerrainMaterial(renderProcess, textureName);
                        TerrainMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
                        return TerrainMaterials[textureName];
                    }
                case "SceneryMaterial":
                    string key;
                    if (textureName != null)
                        key = options.ToString() + ":" + mipMapBias.ToString() + ":" + textureName;
                    else
                        key = options.ToString() + ":";
                    if (!SceneryMaterials.ContainsKey(key))
                    {
                        SceneryMaterial sceneryMaterial = new SceneryMaterial(renderProcess, textureName, options, mipMapBias);
                        SceneryMaterials.Add(key, sceneryMaterial);
                        return sceneryMaterial;
                    }
                    else
                    {
                        return SceneryMaterials[key];
                    }
                case "WaterMaterial":
					if (!WaterMaterials.ContainsKey(textureName))
					{
						WaterMaterial material = new WaterMaterial(renderProcess, textureName);
						WaterMaterials.Add(textureName, material);
						return material;
					}
					else
					{
						return WaterMaterials[textureName];
					}
                case "SkyMaterial":
                    return SkyMaterial;
                case "PrecipMaterial":
                    return PrecipMaterial;
                case "DynatrackMaterial":
                    return DynatrackMaterial;
                case "LightGlowMaterial":
                    return LightGlowMaterial;
                case "ForestMaterial":
                    if (!ForestMaterials.ContainsKey(textureName))
                    {
                        ForestMaterial material = new ForestMaterial(renderProcess, textureName);
                        ForestMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
                        return ForestMaterials[textureName];
                    }
				case "SignalLightMaterial":
					if (!SignalLightMaterials.ContainsKey(textureName))
                    {
						var material = new SignalLightMaterial(renderProcess, textureName);
						SignalLightMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
						return SignalLightMaterials[textureName];
                    }
                default:
                    return Load(renderProcess, "SceneryMaterial");
            }
        }

        public static float ViewingDistance = 3000;  // TODO, this is awkward, viewer must set this to control fog

        static internal Vector3 sunDirection;
        static Vector3 headlightPosition;
        static Vector3 headlightDirection;
        static int lastLightState = 0, currentLightState = 0;
		static double fadeStartTimer = 0;
		static float fadeDuration = -1;
		internal static void UpdateShaders(RenderProcess renderProcess, GraphicsDevice graphicsDevice)
		{
			sunDirection = renderProcess.Viewer.SkyDrawer.solarDirection;
			SceneryShader.LightVector = sunDirection;

			// Headlight illumination
			if (renderProcess.Viewer.PlayerLocomotiveViewer != null
				&& renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer != null
				&& renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightMesh.hasHeadlight)
			{
				currentLightState = renderProcess.Viewer.PlayerLocomotive.Headlight;
				if (currentLightState != lastLightState)
				{
					if (currentLightState == 2 && lastLightState == 1)
					{
						fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
						fadeDuration = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightconeFadein;
					}
					else if (currentLightState == 1 && lastLightState == 2)
					{
						fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
						fadeDuration = -renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightconeFadeout;
					}
					lastLightState = currentLightState;
				}
				headlightPosition = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.xnaLightconeLoc;
				headlightDirection = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.xnaLightconeDir;

				SceneryShader.SetHeadlight(ref headlightPosition, ref headlightDirection, (float)(renderProcess.Viewer.Simulator.ClockTime - fadeStartTimer), fadeDuration);
			}
			// End headlight illumination

			SceneryShader.Overcast = renderProcess.Viewer.SkyDrawer.overcast;
			SceneryShader.ViewerPos = renderProcess.Viewer.Camera.XNALocation(renderProcess.Viewer.Camera.CameraWorldLocation);

			SceneryShader.SetFog(ViewingDistance * 0.5f * FogCoeff, ref Materials.FogColor);
		}
    }
    #endregion

    #region Shared texture manager
    public class SharedTextureManager
    {
        private static Dictionary<string, Texture2D> SharedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D Get(GraphicsDevice device, string path)
        {
            if (path == null)
                return Materials.MissingTexture;

            if (!SharedTextures.ContainsKey(path))
            {
                try { 
                    Texture2D texture = MSTS.ACEFile.Texture2DFromFile(device, path);
                    SharedTextures.Add(path, texture);
                    return texture;
                }
                catch (Exception error)
                {
					Trace.TraceInformation(path);
					Trace.WriteLine(error);
					return Materials.MissingTexture;
                }
            }
            else
            {
                return SharedTextures[path];
            }
        }
    }
    #endregion

    #region Material interface
	public abstract class Material
	{
		public virtual void SetState(GraphicsDevice graphicsDevice, Material previousMaterial) { }
		public virtual void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix) { }
		public virtual void ResetState(GraphicsDevice graphicsDevice) { }

		public virtual bool GetBlending(RenderPrimitive renderPrimitive) { return false; }
		public virtual Texture2D GetShadowTexture(RenderPrimitive renderPrimitive) { return null; }
	}
    #endregion

    #region Empty material
	public class EmptyMaterial : Material
	{
	}
    #endregion

    #region Sprite batch material
	public class SpriteBatchMaterial : Material
	{
		public SpriteBatch SpriteBatch;
		public SpriteFont DefaultFont;
		public RenderProcess RenderProcess;  // for diagnostics only

		public SpriteBatchMaterial(RenderProcess renderProcess)
		{
			RenderProcess = renderProcess;
			SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
			DefaultFont = renderProcess.Content.Load<SpriteFont>("Arial");
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			float scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
			Vector3 screenScaling = new Vector3(scaling);
			Matrix xForm = Matrix.CreateScale(screenScaling);
			RenderProcess.RenderStateChangesCount++;
			SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState, xForm);
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
            foreach (var item in renderItems)
            {
                item.RenderPrimitive.Draw(graphicsDevice);
            }
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SpriteBatch.End();
		}

        public override bool GetBlending(RenderPrimitive renderPrimitive)
        {
            return true;
        }
	}
    #endregion

    #region Scenery material
	public class SceneryMaterial : Material
    {
		readonly int Options = 0;
		readonly float MipMapBias = 0;
        readonly SceneryShader SceneryShader;
		readonly Texture2D Texture;
		readonly Texture2D nightTexture = null;
		bool isNightEnabled = false;
		readonly public RenderProcess RenderProcess;  // for diagnostics only
		IEnumerator<EffectPass> ShaderPassesDarkShade;
		IEnumerator<EffectPass> ShaderPassesFullBright;
		IEnumerator<EffectPass> ShaderPassesHalfBright;
		IEnumerator<EffectPass> ShaderPassesImage;
		IEnumerator<EffectPass> ShaderPassesVegetation;
		IEnumerator<EffectPass> ShaderPasses;

		public SceneryMaterial(RenderProcess renderProcess, string texturePath, int options, float mipMapBias)  
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            Options = options;
            MipMapBias = mipMapBias;
			// note: texturePath may be null if the object isn't textured, results in default 'blank texture' being loaded.
            Texture = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
            if (texturePath != null)
            {
                int idx = texturePath.LastIndexOf("textures");
                if (idx > 0)
                {
                    string strTexname;
                    string nightTexturePath = texturePath.Remove(idx + 9);
                    idx = texturePath.LastIndexOf(@"\");
                    strTexname = texturePath.Remove(0, idx);
                    nightTexturePath += "night";
                    nightTexturePath += strTexname;
                    if (File.Exists(nightTexturePath))
                        nightTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, nightTexturePath);
                }
            }
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

			if (ShaderPassesDarkShade == null) ShaderPassesDarkShade = SceneryShader.Techniques["DarkShade"].Passes.GetEnumerator();
			if (ShaderPassesFullBright == null) ShaderPassesFullBright = SceneryShader.Techniques["FullBright"].Passes.GetEnumerator();
			if (ShaderPassesHalfBright == null) ShaderPassesHalfBright = SceneryShader.Techniques["HalfBright"].Passes.GetEnumerator();
			if (ShaderPassesImage == null) ShaderPassesImage = SceneryShader.Techniques["Image"].Passes.GetEnumerator();
			if (ShaderPassesVegetation == null) ShaderPassesVegetation = SceneryShader.Techniques["Vegetation"].Passes.GetEnumerator();

			/////////////// MATERIAL OPTIONS //////////////////
			//
			// Material options are specified in a 32-bit int named "options"
			// Following are the bit assignments:
			// (name, dec value, hex, bits)
			// 
			// SHADERS bits 0 through 3 (allow for future shaders)
			// Diffuse            1     0x0001      0000 0000 0000 0001
			// Tex                2     0x0002      0000 0000 0000 0010
			// TexDiff            3     0x0003      0000 0000 0000 0011
			// BlendATex          4     0x0004      0000 0000 0000 0100
			// AddAtex            5     0x0005      0000 0000 0000 0101
			// BlendATexDiff      6     0x0006      0000 0000 0000 0110
			// AddATexDiff        7     0x0007      0000 0000 0000 0111
			// AND mask          15     0x000f      0000 0000 0000 1111
			//
			// LIGHTING  bits 4 through 7 ( >> 4 )
			// DarkShade         16     0x0010      0000 0000 0001 0000
			// OptHalfBright     32     0x0020      0000 0000 0010 0000
			// CruciformLong     48     0x0030      0000 0000 0011 0000
			// Cruciform         64     0x0040      0000 0000 0100 0000
			// OptFullBright     80     0x0050      0000 0000 0101 0000
			// OptSpecular750    96     0x0060      0000 0000 0110 0000
			// OptSpecular25    112     0x0070      0000 0000 0111 0000
			// OptSpecular0     128     0x0080      0000 0000 1000 0000
			// AND mask         240     0x00f0      0000 0000 1111 0000 
			//
			// ALPHA TEST bit 8 ( >> 8 )
			// None               0     0x0000      0000 0000 0000 0000
			// Trans            256     0x0100      0000 0001 0000 0000
			// AND mask         256     0x0100      0000 0001 0000 0000
			//
			// Z BUFFER bits 9 and 10 ( >> 9 )
			// None               0     0x0000      0000 0000 0000 0000
			// Normal           512     0x0200      0000 0010 0000 0000
			// Write Only      1024     0x0400      0000 0100 0000 0000
			// Test Only       1536     0x0600      0000 0110 0000 0000
			// AND mask        1536     0x0600      0000 0110 0000 0000
			//
			// TEXTURE ADDRESS MODE bits 11 and 12 ( >> 11 )
			// Wrap               0     0x0000      0000 0000 0000 0000             
			// Mirror          2048     0x0800      0000 1000 0000 0000
			// Clamp           4096     0x1000      0001 0000 0000 0000
			// Border          6144     0x1800      0001 1000 0000 0000
			// AND mask        6144     0x1800      0001 1000 0000 0000
			//
			// NIGHT TEXTURE bit 13 ( >> 13 )
			// Disabled           0     0x0000      0000 0000 0000 0000
			// Enabled         8192     0x2000      0010 0000 0000 0000
			//

			var shaders = Options & 0x000f;
			var lighting = (Options & 0x00f0) >> 4;
			var alphaTest = (Options & 0x0100) >> 8;
			var textureAddressMode = (Options & 0x1800) >> 11;

			switch (shaders)
			{
				case 1: // Diffuse
				case 3: // TexDiff
				case 6: // BlendATexDiff
				case 7: // AddATexDiff
					SceneryShader.LightingDiffuse = 1;
					break;
				default:
					SceneryShader.LightingDiffuse = 0;
					break;
			}

			switch (lighting)
			{
				case 1: // DarkShade
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["DarkShade"];
					ShaderPasses = ShaderPassesDarkShade;
					break;
				case 2: // OptHalfBright
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["HalfBright"];
					ShaderPasses = ShaderPassesHalfBright;
					break;
				case 3: // Cruciform
				case 4: // CruciformLong
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["Vegetation"];
					ShaderPasses = ShaderPassesVegetation;
					break;
				case 5: // OptFullBright
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["FullBright"];
					ShaderPasses = ShaderPassesFullBright;
					break;
				default:
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["Image"];
					ShaderPasses = ShaderPassesImage;
					break;
			}

			switch (lighting)
			{
				case 6: // OptSpecular750
					SceneryShader.LightingSpecular = 750;
					break;
				case 7: // OptSpecular25
					SceneryShader.LightingSpecular = 25;
					break;
				case 8: // OptSpecular0
				default:
					SceneryShader.LightingSpecular = 0;
					break;
			}

			if (alphaTest != 0)
			{
				// Transparency test
				graphicsDevice.RenderState.AlphaTestEnable = true;
				graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel
				graphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become
			}
			else if (shaders >= 4)
			{
				// Translucency
				graphicsDevice.RenderState.AlphaTestEnable = true;
				graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
				graphicsDevice.RenderState.ReferenceAlpha = 10;  // ie lightcode is 9 in full transparent areas
				graphicsDevice.RenderState.AlphaBlendEnable = true;
				graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
				graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
				graphicsDevice.RenderState.SeparateAlphaBlendEnabled = true;
				graphicsDevice.RenderState.AlphaSourceBlend = Blend.Zero;
				graphicsDevice.RenderState.AlphaDestinationBlend = Blend.One;
			}

			// Texture addressing
			switch (textureAddressMode)
			{
				case 0: // wrap
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
					break;
				case 1: // mirror
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Mirror;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Mirror;
					break;
				case 2: // clamp
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
					break;
				case 3: // border
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Border;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Border;
					break;
			}

			// Night texture toggle
			if ((Options & 0x2000) >> 13 == 1)
				isNightEnabled = true;

			if (Materials.sunDirection.Y < 0.0f && nightTexture != null && isNightEnabled) // Night
			{
				SceneryShader.ImageMap_Tex = nightTexture;
				SceneryShader.IsNight_Tex = true;
			}
			else
			{
				SceneryShader.ImageMap_Tex = Texture;
				SceneryShader.IsNight_Tex = false;
			}

			SceneryShader.Apply();

			if (MipMapBias < -1)
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = -1;   // clamp to -1 max
			else
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = MipMapBias;

			RenderProcess.RenderStateChangesCount++;
			RenderProcess.ImageChangesCount++;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

            // With the GPU configured, now we can draw the primitive
            SceneryShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach(RenderItem item in renderItems)
                {
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewProj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SceneryShader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SceneryShader.IsNight_Tex = false;
			SceneryShader.LightingDiffuse = 1;
			SceneryShader.LightingSpecular = 0;
			SceneryShader.Apply();

			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.AlphaDestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
			graphicsDevice.RenderState.AlphaSourceBlend = Blend.One;
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.ReferenceAlpha = 0;
			graphicsDevice.RenderState.SeparateAlphaBlendEnabled = false;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			// Transparency test
			int alphaTest = (Options & 0x0100) >> 8;
			if (alphaTest != 0)
				return false;

			// Translucency
			int shaders = Options & 0x000f;
			if (alphaTest == 0 && shaders >= 4)
				return true;

			return false;
		}

		public override Texture2D GetShadowTexture(RenderPrimitive renderPrimitive)
		{
			if (Materials.sunDirection.Y < 0.0f && nightTexture != null && isNightEnabled) // Night
				return nightTexture;
			
			return Texture;
		}
	}
    #endregion

    #region Terrain material
	public class TerrainMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D PatchTexture;
        readonly public RenderProcess RenderProcess;  // for diagnostics only
		IEnumerator<EffectPass> ShaderPasses;

		public TerrainMaterial(RenderProcess renderProcess, string terrainTexture )
        {
            SceneryShader = Materials.SceneryShader;
            PatchTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, terrainTexture);
            RenderProcess = renderProcess;
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;
			RenderProcess.ImageChangesCount++;

			SceneryShader.CurrentTechnique = SceneryShader.Techniques["Terrain"];
			if (ShaderPasses == null) ShaderPasses = SceneryShader.Techniques["Terrain"].Passes.GetEnumerator();
			SceneryShader.ImageMap_Tex = PatchTexture;

			graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

			graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
			graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;

            SceneryShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
			{
				ShaderPasses.Current.Begin();
				foreach (RenderItem item in renderItems)
				{
					SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
					SceneryShader.ZBias = item.RenderPrimitive.ZBias;
					SceneryShader.CommitChanges();
					item.RenderPrimitive.Draw(graphicsDevice);
				}
				ShaderPasses.Current.End();
			}
            SceneryShader.End();
        }
	}
    #endregion

    #region Sky material
    public class SkyMaterial : Material
    {
        SkyShader SkyShader;
        Texture2D skyTexture;
        Texture2D starTextureN;
        Texture2D starTextureS;
        Texture2D moonTexture;
        Texture2D moonMask;
        Texture2D cloudTexture;
        private Matrix XNAMoonMatrix;
        public RenderProcess RenderProcess;
		IEnumerator<EffectPass> ShaderPasses;

		public SkyMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            SkyShader = Materials.SkyShader;
            skyTexture = renderProcess.Content.Load<Texture2D>("SkyDome1");
            starTextureN = renderProcess.Content.Load<Texture2D>("Starmap_N");
            starTextureS = renderProcess.Content.Load<Texture2D>("Starmap_S");
            moonTexture = renderProcess.Content.Load<Texture2D>("MoonMap");
            moonMask = renderProcess.Content.Load<Texture2D>("MoonMask");
            cloudTexture = renderProcess.Content.Load<Texture2D>("Clouds01");
        }

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			if (ShaderPasses == null) ShaderPasses = SkyShader.Techniques["SkyTechnique"].Passes.GetEnumerator();

            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                RenderProcess.Viewer.SkyDrawer.solarDirection.Y,
                RenderProcess.Viewer.SkyDrawer.overcast);
            Materials.FogCoeff = RenderProcess.Viewer.SkyDrawer.fogCoeff;

            SkyShader.CurrentTechnique = SkyShader.Techniques["SkyTechnique"];
            SkyShader.SkyTexture = skyTexture;
            SkyShader.StarTexture = skyTexture;

            // Variables passed from SkyDrawer
            SkyShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            if (RenderProcess.Viewer.SkyDrawer.latitude > 0)
                SkyShader.StarTexture = starTextureN;
            else
                SkyShader.StarTexture = starTextureS;
            SkyShader.SunpeakColor = RenderProcess.Viewer.SkyDrawer.sunpeakColor;
            SkyShader.SunriseColor = RenderProcess.Viewer.SkyDrawer.sunriseColor;
            SkyShader.SunsetColor = RenderProcess.Viewer.SkyDrawer.sunsetColor;
            SkyShader.Time = (float)RenderProcess.Viewer.Simulator.ClockTime/100000;
            SkyShader.MoonScale = SkyConstants.skyRadius / 20;

            // Save existing render state
            bool fogEnable = graphicsDevice.RenderState.FogEnable;
            CullMode cullMode = graphicsDevice.RenderState.CullMode;
            // Set render state for drawing sky
            graphicsDevice.RenderState.FogEnable = false;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            // Sky dome
            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 1;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();

            // Moon
            // Send the transform matrices to the shader
            int skyRadius = RenderProcess.Viewer.SkyDrawer.SkyMesh.skyRadius;
            int cloudRadiusDiff = RenderProcess.Viewer.SkyDrawer.SkyMesh.cloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(RenderProcess.Viewer.SkyDrawer.lunarDirection * (skyRadius - (cloudRadiusDiff / 2)));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            // Shader setup
            SkyShader.CurrentTechnique = SkyShader.Techniques["MoonTechnique"];
            SkyShader.MoonTexture = moonTexture;
            SkyShader.MoonMaskTexture = moonMask;
            SkyShader.Random = RenderProcess.Viewer.SkyDrawer.moonPhase;

            // Save the existing alpha render state
            bool alphaBlendEnable = graphicsDevice.RenderState.AlphaBlendEnable;
            Blend destinationBlend = graphicsDevice.RenderState.DestinationBlend;
            Blend sourceBlend = graphicsDevice.RenderState.SourceBlend;
            bool alphaTestEnable = graphicsDevice.RenderState.AlphaTestEnable;
            // Set alpha render state for drawing the moon and clouds
            graphicsDevice.RenderState.AlphaBlendEnable = true;
            graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 2;
            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();

                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();

            // Clouds
           
            // Shader setup
            SkyShader.CurrentTechnique = SkyShader.Techniques["CloudTechnique"];
            SkyShader.CloudTexture = cloudTexture;
            SkyShader.Overcast = RenderProcess.Viewer.SkyDrawer.overcast;
            SkyShader.WindSpeed = RenderProcess.Viewer.SkyDrawer.windSpeed;
            SkyShader.WindDirection = RenderProcess.Viewer.SkyDrawer.windDirection;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 3;
            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();

                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();

            // Restore the pre-existing render state
            graphicsDevice.RenderState.AlphaBlendEnable = alphaBlendEnable;
            graphicsDevice.RenderState.DestinationBlend = destinationBlend;
            graphicsDevice.RenderState.SourceBlend = sourceBlend;
            graphicsDevice.RenderState.AlphaTestEnable = alphaTestEnable;
            graphicsDevice.RenderState.CullMode = cullMode;
            graphicsDevice.RenderState.FogEnable = fogEnable;
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return false;
		}

        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector</param>
        /// <param name="overcast">The amount of overcast</param>
        private void FogDay2Night(float sunHeight, float overcast)
        {
            // We'll work with floating-point values, then convert to a "Color" object
            const float nightStart = 0.15f; // The sun's Y value where it begins to get dark
            const float nightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady
            Vector3 startColor; // Original daytime fog color - must be preserved!
            Vector3 finishColor; //Darkest nighttime fog color
            Vector3 floatColor; // A scratchpad variable

            // These should be user defined in the Environment files (future)
            startColor = new Vector3(0.647f, 0.651f, 0.655f);
            finishColor = new Vector3(0.05f, 0.05f, 0.05f);

            if (sunHeight > nightStart)
                floatColor = startColor;
            else if (sunHeight < nightFinish)
                floatColor = finishColor;
            else
            {
                float amount = (sunHeight - nightFinish) / (nightStart - nightFinish);
                floatColor.X = MathHelper.Lerp(finishColor.X, startColor.X, amount);
                floatColor.Y = MathHelper.Lerp(finishColor.Y, startColor.Y, amount);
                floatColor.Z = MathHelper.Lerp(finishColor.Z, startColor.Z, amount);
            }

            // Adjust fog color for overcast
            floatColor *= (1 - 0.5f * overcast);

            // Convert color format
            Materials.FogColor.R = (byte)(floatColor.X * 255);
            Materials.FogColor.G = (byte)(floatColor.Y * 255);
            Materials.FogColor.B = (byte)(floatColor.Z * 255);
        }
    }
    #endregion

    #region Precipitation material
	public class PrecipMaterial : Material
    {
        PrecipShader PrecipShader;
        Texture2D rainTexture;
        Texture2D snowTexture;
        public RenderProcess RenderProcess;
		IEnumerator<EffectPass> ShaderPasses;

		public PrecipMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            PrecipShader = Materials.PrecipShader;
            rainTexture = renderProcess.Content.Load<Texture2D>("Raindrop");
            snowTexture = renderProcess.Content.Load<Texture2D>("Snowflake");
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;

            var weatherType = RenderProcess.Viewer.PrecipDrawer.weatherType;
            PrecipShader.CurrentTechnique = PrecipShader.Techniques["RainTechnique"];
			if (ShaderPasses == null) ShaderPasses = PrecipShader.Techniques["RainTechnique"].Passes.GetEnumerator();
            PrecipShader.WeatherType = weatherType;
            PrecipShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            PrecipShader.ViewportHeight = (int)RenderProcess.Viewer.DisplaySize.Y;
            PrecipShader.CurrentTime = (float)RenderProcess.Viewer.Simulator.ClockTime;
            switch (weatherType)
            {
                case 1:
                    PrecipShader.PrecipTexture = snowTexture;
                    break;
                case 2:
                    PrecipShader.PrecipTexture = rainTexture;
                    break;
                // Safe? or need a default here? If so, what?
            }

			graphicsDevice.RenderState.AlphaBlendEnable = true;
			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
			graphicsDevice.RenderState.PointSpriteEnable = true;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			if (RenderProcess.Viewer.PrecipDrawer.weatherType == 0)
				return;

            PrecipShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    PrecipShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref Camera.XNASkyProjection);
                    PrecipShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            PrecipShader.End();
        }

        // Is this needed? PrecipMaterial doesn't change any of these render states.
		public override void ResetState(GraphicsDevice graphicsDevice)
        {
			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.PointSpriteEnable = false;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
	#endregion

    #region Dynatrack material
	public class DynatrackMaterial : Material
    {
        SceneryShader SceneryShader;
        Texture2D Image1;
        Texture2D Image1s;
        Texture2D Image2;
        string TexturePath;
        public RenderProcess RenderProcess;

		public DynatrackMaterial(RenderProcess renderProcess)
        {
            TrProfile profile = renderProcess.Viewer.Simulator.TRP.TrackProfile;
            
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            TexturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures" + @"\" + profile.Image1Name; 
            Image1 = SharedTextureManager.Get(renderProcess.GraphicsDevice, TexturePath);
            TexturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures\snow" + @"\" + profile.Image1sName; 
            if (File.Exists(TexturePath))
                Image1s = SharedTextureManager.Get(renderProcess.GraphicsDevice, TexturePath);
            else // Use file in base texture folder
                Image1s = Image1;
            TexturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures" + @"\" + profile.Image2Name; 
            Image2 = SharedTextureManager.Get(renderProcess.GraphicsDevice, TexturePath);
        } // end DynatrackMaterial() (constructor)

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;

			SceneryShader.CurrentTechnique = SceneryShader.Techniques["Image"];

			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
		} // end SetState()

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            RenderProcess.ImageChangesCount++;

            foreach (var item in renderItems)   // Is this ever more than one for dynamic track?
                                                // I guess there's nothing to be gained from changing it.
            {
                DynatrackMesh mesh = (DynatrackMesh)item.RenderPrimitive;
                for (int lodIndex = 0; lodIndex <= mesh.LastIndex; lodIndex++)
                {
                    LODItem lod = (LODItem)mesh.TrProfile.LODItems[lodIndex];
                    // The following are controlled by options in the track profile
                    if (lod.Texture == DynatrackTextures.Image1)
                    {
                        if (RenderProcess.Viewer.Simulator.Weather == MSTS.WeatherType.Snow ||
                                RenderProcess.Viewer.Simulator.Season == MSTS.SeasonType.Winter)
                            SceneryShader.ImageMap_Tex = Image1s;
                        else
                            SceneryShader.ImageMap_Tex = Image1;
                    }
                    else if (lod.Texture == DynatrackTextures.Image2)
                        SceneryShader.ImageMap_Tex = Image2;
                    // else SceneryShader.ImageMap_Tex will remain unchanged

                    SceneryShader.LightingSpecular = lod.LightingSpecular;
                    graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias =
                                lod.MipMapLevelOfDetailBias;
                    graphicsDevice.RenderState.AlphaBlendEnable =
                                lod.AlphaBlendEnable;
                    graphicsDevice.RenderState.AlphaTestEnable =
                                lod.AlphaTestEnable;

                    Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
					SceneryShader.Apply();

                    mesh.DrawIndex = lodIndex; // Communicate to Draw which LOD to draw.
                    SceneryShader.Begin();
                    foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
                    {
                        pass.Begin();
                        item.RenderPrimitive.Draw(graphicsDevice);
                        pass.End();
                    }
                    SceneryShader.End();
                } // end for i
            }
        } // end Render() (DynatrackMaterial)

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SceneryShader.LightingSpecular = 0;
			SceneryShader.Apply();

			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
		} // end ResetState()

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		} // end GetBlending()
    } // end class DynatrackMaterial
    #endregion

    #region Forest material
	public class ForestMaterial : Material
    {
        public readonly RenderProcess RenderProcess;  // for diagnostics only
        readonly Texture2D TreeTexture = null;
		IEnumerator<EffectPass> ShaderPasses;

		public ForestMaterial(RenderProcess renderProcess, string treeTexture)
        {
            RenderProcess = renderProcess;
            TreeTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, treeTexture);
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;
			RenderProcess.ImageChangesCount++;

			Materials.SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques["Forest"];
			if (ShaderPasses == null) ShaderPasses = Materials.SceneryShader.Techniques["Forest"].Passes.GetEnumerator();
			Materials.SceneryShader.ImageMap_Tex = TreeTexture;

			graphicsDevice.RenderState.AlphaTestEnable = true;
			graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
			graphicsDevice.RenderState.ReferenceAlpha = 200;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var shader = Materials.SceneryShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

			shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
					shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
					shader.ZBias = item.RenderPrimitive.ZBias;
					shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
			shader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
			graphicsDevice.RenderState.ReferenceAlpha = 0;
		}
	}
    #endregion

    #region LightGlow material
	public class LightGlowMaterial : Material
    {
        LightGlowShader LightGlowShader;
        Texture2D lightGlowTexture;
        public RenderProcess RenderProcess;
        int lastLightState = 0, currentLightState = 0;
        double fadeTimer = 0;

		public LightGlowMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            LightGlowShader = Materials.LightGlowShader;
            lightGlowTexture = renderProcess.Content.Load<Texture2D>("Lightglow");
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;

            LightGlowShader.CurrentTechnique = LightGlowShader.Techniques["LightGlow"];
            LightGlowShader.LightGlowTexture = lightGlowTexture;

			graphicsDevice.RenderState.AlphaBlendEnable = true;
			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
			graphicsDevice.RenderState.SeparateAlphaBlendEnabled = true;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Lights fade-in / fade-out
            currentLightState = RenderProcess.Viewer.PlayerLocomotive.Headlight;
            if (currentLightState != lastLightState)
            {
                if (currentLightState == 1 && lastLightState == 0)
                    LightGlowShader.StateChange = 1;
                else if (currentLightState == 2 && lastLightState == 1)
                    LightGlowShader.StateChange = 2;
                else if (currentLightState == 1 && lastLightState == 2)
                    LightGlowShader.StateChange = 3;
                else if (currentLightState == 0 && lastLightState == 1)
                    LightGlowShader.StateChange = 4;
                // Reset fade timer
                fadeTimer = RenderProcess.Viewer.Simulator.ClockTime;
                lastLightState = currentLightState;
            }
            LightGlowShader.FadeTime = (float)(RenderProcess.Viewer.Simulator.ClockTime - fadeTimer);

            LightGlowShader.Begin();
            foreach (EffectPass pass in LightGlowShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    LightGlowShader.SetMatrix(ref wvp);
                    LightGlowShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            LightGlowShader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.SeparateAlphaBlendEnabled = false;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
    #endregion
    
    #region Water material
	public class WaterMaterial : Material
    {
        public readonly RenderProcess RenderProcess;  // for diagnostics only
        readonly Texture2D WaterTexture;
		IEnumerator<EffectPass> ShaderPasses;

		public WaterMaterial(RenderProcess renderProcess, string waterTexturePath)
		{
			RenderProcess = renderProcess;
			WaterTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, waterTexturePath);
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;
			RenderProcess.ImageChangesCount++;

			Materials.SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques["Image"];
			if (ShaderPasses == null) ShaderPasses = Materials.SceneryShader.Techniques["Image"].Passes.GetEnumerator();
			Materials.SceneryShader.ImageMap_Tex = WaterTexture;

			graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

			graphicsDevice.RenderState.AlphaBlendEnable = true;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

			graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var shader = Materials.SceneryShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

			shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
					shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
					shader.ZBias = item.RenderPrimitive.ZBias;
					shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
			shader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
    #endregion

    #region Shadow Map material
	public class ShadowMapMaterial : Material
    {
		public readonly RenderProcess RenderProcess;  // for diagnostics only
		IEnumerator<EffectPass> ShaderPassesShadowMap;
		IEnumerator<EffectPass> ShaderPassesShadowMapBlocker;
		IEnumerator<EffectPass> ShaderPasses;

		public ShadowMapMaterial(RenderProcess renderProcess)
        {
			RenderProcess = renderProcess;
        }

		public void SetState(GraphicsDevice graphicsDevice, bool blocker)
		{
			var shader = Materials.ShadowMapShader;
			shader.CurrentTechnique = shader.Techniques[blocker ? "ShadowMapBlocker" : "ShadowMap"];
			if (ShaderPassesShadowMap == null) ShaderPassesShadowMap = shader.Techniques["ShadowMap"].Passes.GetEnumerator();
			if (ShaderPassesShadowMapBlocker == null) ShaderPassesShadowMapBlocker = shader.Techniques["ShadowMapBlocker"].Passes.GetEnumerator();
			ShaderPasses = blocker ? ShaderPassesShadowMapBlocker : ShaderPassesShadowMap;

			var rs = graphicsDevice.RenderState;
			rs.CullMode = blocker ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			var shader = Materials.ShadowMapShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

			shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
			{
				ShaderPasses.Current.Begin();
				foreach (var item in renderItems)
				{
					var wvp = item.XNAMatrix * viewproj;
					shader.SetData(ref wvp, item.Material.GetShadowTexture(item.RenderPrimitive));
					shader.CommitChanges();
					item.RenderPrimitive.Draw(graphicsDevice);
				}
				ShaderPasses.Current.End();
			}
			shader.End();
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
		}
	}
    #endregion

	#region Popup Window material
	public class PopupWindowMaterial : Material
	{
		public SpriteFont DefaultFont;
		IEnumerator<EffectPass> ShaderPassesPopupWindow;
		IEnumerator<EffectPass> ShaderPassesPopupWindowGlass;
		IEnumerator<EffectPass> ShaderPasses;

		public PopupWindowMaterial(RenderProcess renderProcess)
		{
			DefaultFont = renderProcess.Content.Load<SpriteFont>("Arial");
		}

		public void SetState(GraphicsDevice graphicsDevice, Texture2D screen)
		{
			var shader = Materials.PopupWindowShader;
			shader.CurrentTechnique = screen == null ? shader.Techniques["PopupWindow"] : shader.Techniques["PopupWindowGlass"];
			if (ShaderPassesPopupWindow == null) ShaderPassesPopupWindow = shader.Techniques["PopupWindow"].Passes.GetEnumerator();
			if (ShaderPassesPopupWindowGlass == null) ShaderPassesPopupWindowGlass = shader.Techniques["PopupWindowGlass"].Passes.GetEnumerator();
			ShaderPasses = screen == null ? ShaderPassesPopupWindow : ShaderPassesPopupWindowGlass;
			shader.Screen = screen;
			shader.GlassColor = Color.Black;

			var rs = graphicsDevice.RenderState;
			rs.AlphaBlendEnable = true;
			rs.AlphaFunction = CompareFunction.Greater;
			rs.AlphaTestEnable = true;
			rs.CullMode = CullMode.None;
			rs.DepthBufferEnable = false;
			rs.DepthBufferFunction = CompareFunction.Always;
			rs.DepthBufferWriteEnable = false;
		}

        public void Render(GraphicsDevice graphicsDevice, RenderPrimitive renderPrimitive, ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Materials.PopupWindowShader;

            Matrix wvp = XNAWorldMatrix * XNAViewMatrix * XNAProjectionMatrix;
            shader.SetMatrix(XNAWorldMatrix, ref wvp);

            shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                renderPrimitive.Draw(graphicsDevice);
                ShaderPasses.Current.End();
            }
            shader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
			graphicsDevice.RenderState.DepthBufferEnable = true;
			graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
			graphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
	#endregion

	#region Yellow (testing) material
    /// <summary>
    /// This material is used for debug and testing.
    /// </summary>
	public class YellowMaterial : Material
    {
        static BasicEffect basicEffect = null;
        RenderProcess RenderProcess;

		public YellowMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            if( basicEffect == null )
            {
                basicEffect = new BasicEffect(renderProcess.GraphicsDevice, null);
                basicEffect.Alpha = 1.0f;
                basicEffect.DiffuseColor = new Vector3(197.0f/255.0f, 203.0f/255.0f, 37.0f/255.0f);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;

			graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            
            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            basicEffect.Begin();
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    basicEffect.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            basicEffect.End();
        }
	}
    #endregion
}
