﻿using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game
{
    public class LevelRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

#if __ANDROID__
        private const int defaultWidth = 544, defaultHeight = 306;
#else
        private const int defaultWidth = 720, defaultHeight = 405;
#endif
        private Vector2 lastImageSize;

        private static readonly int PyramidSize = 3;

        private readonly LevelHandler levelHandler;
        private readonly ContentRef<DrawTechnique> lightingShader, lightingNoiseShader;
        private readonly ContentRef<DrawTechnique> combineSceneShader, combineSceneWaterShader;

        private readonly ContentRef<DrawTechnique> downsampleShader;
        private readonly ContentRef<DrawTechnique> blurShader;

        private readonly ContentRef<DrawTechnique> resizeShader;

        private Texture lightingTexture, mainTexture, normalTexture, finalTexture;
        private RenderTarget lightingTarget, mainTarget, finalTarget;

        private readonly RenderTarget[] targetPingPongA = new RenderTarget[PyramidSize];
        private readonly RenderTarget[] targetPingPongB = new RenderTarget[PyramidSize];

        private readonly ContentRef<Texture> noiseTexture;

        public Texture FunalTexture
        {
            get { return finalTexture; }
        }

        public LevelRenderSetup(LevelHandler levelHandler)
        {
            this.levelHandler = levelHandler;

            // Shaders
            lightingShader = ContentResolver.Current.RequestShader("Lighting");
            lightingNoiseShader = ContentResolver.Current.RequestShader("LightingNoise");

            downsampleShader = ContentResolver.Current.RequestShader("Downsample");
            blurShader = ContentResolver.Current.RequestShader("Blur");

            combineSceneShader = ContentResolver.Current.RequestShader("CombineScene");
            combineSceneWaterShader = ContentResolver.Current.RequestShader("CombineSceneWater");

            switch (Settings.Resize) {
                default:
                case Settings.ResizeMode.None:
                    resizeShader = DrawTechnique.Solid;
                    break;
                case Settings.ResizeMode.HQ2x:
                    resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                    break;
                case Settings.ResizeMode.xBRZ:
                    resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                    break;
            }

            // Main texture
            mainTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            normalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest, format: TexturePixelFormat.Rgb);
            mainTarget = new RenderTarget(AAQuality.Off, /*true*/false, mainTexture, normalTexture);

            // Lighting texture
            lightingTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest, format: TexturePixelFormat.Dual);
            lightingTarget = new RenderTarget(AAQuality.Off, false, lightingTexture);

            finalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            finalTarget = new RenderTarget(AAQuality.Off, false, finalTexture);

            //
            noiseTexture = ContentResolver.Current.RequestGraphicResource("_custom/noise.png", null).Texture;

            // Render steps
            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                MatrixMode = RenderMatrix.WorldSpace,
                VisibilityMask = VisibilityFlag.AllGroups,
                ClearFlags = ClearFlag.All,
                DefaultClearColor = true,

                Output = mainTarget
                //Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "CombineScene",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None,
                ClearFlags = ClearFlag.None,

                Input = new BatchInfo(DrawTechnique.Solid, ColorRgba.White, mainTexture),
                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.All,
                ClearFlags = ClearFlag.None,

                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Resize",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None,
                ClearFlags = ClearFlag.None
            });
        }

        protected override void OnDisposing(bool manually)
        {
            base.OnDisposing(manually);

            Disposable.Free(ref lightingTarget);
            Disposable.Free(ref lightingTexture);

            Disposable.Free(ref mainTarget);
            Disposable.Free(ref mainTexture);
            Disposable.Free(ref normalTexture);

            Disposable.Free(ref finalTarget);
            Disposable.Free(ref finalTexture);

            Disposable.FreeContents(targetPingPongA);
            Disposable.FreeContents(targetPingPongB);
        }

        protected override void OnRenderPointOfView(Scene scene, DrawDevice drawDevice, Rect viewportRect, Vector2 imageSize)
        {
            if (lastImageSize != imageSize) {
                lastImageSize = imageSize;

                const float defaultRatio = (float)defaultWidth / defaultHeight;
                float currentRatio = imageSize.X / imageSize.Y;

                int width, height;
                if (currentRatio > defaultRatio) {
                    width = MathF.Min(defaultWidth, (int)imageSize.X);
                    height = (int)(width / currentRatio);
                } else if (currentRatio < defaultRatio) {
                    height = MathF.Min(defaultHeight, (int)imageSize.Y);
                    width = (int)(height * currentRatio);
                } else {
                    width = MathF.Min(defaultWidth, (int)imageSize.X);
                    height = MathF.Min(defaultHeight, (int)imageSize.Y);
                }

                TargetSize = new Point2(width, height);

                ResizeRenderTarget(mainTarget, TargetSize);
                ResizeRenderTarget(lightingTarget, TargetSize);
                ResizeRenderTarget(finalTarget, TargetSize);
            }

            base.OnRenderPointOfView(scene, drawDevice, viewportRect, imageSize);
        }

        protected override void OnRenderSingleStep(RenderStep step, Scene scene, DrawDevice drawDevice)
        {
            if (step.Id == "Resize") {
                ProcessResizeStep(drawDevice);
            } else if(step.Id == "CombineScene") {
                ProcessCombineSceneStep(drawDevice);
            } else {
                base.OnRenderSingleStep(step, scene, drawDevice);
            }
        }

        private void ProcessResizeStep(DrawDevice drawDevice)
        {
            BatchInfo material = new BatchInfo(resizeShader, ColorRgba.White, finalTexture);
            material.SetUniform("mainTexSize", (float)finalTexture.ContentWidth, (float)finalTexture.ContentHeight);
            Blit(drawDevice, material, drawDevice.ViewportRect);
        }

        private void ProcessCombineSceneStep(DrawDevice drawDevice)
        {
            // ToDo: Split lighting to RGB channels
            // ToDo: Implement dynamic lighting/shadows (https://github.com/mattdesl/lwjgl-basics/wiki/2D-Pixel-Perfect-Shadows)

            Vector2 viewSize = drawDevice.TargetSize;
            Vector2 viewOffset = new Vector2(
                drawDevice.RefCoord.X - viewSize.X / 2,
                drawDevice.RefCoord.Y - viewSize.Y / 2
            );

            float ambientLight = levelHandler.AmbientLightCurrent;
            float viewWaterLevel = (levelHandler.WaterLevel - viewOffset.Y);

            // Removed optimization because of bright lights
            /*if (ambientLight >= 1f) {
                if (viewWaterLevel < viewSize.Y) {
                    // Render with water, skip lighting phase
                    BatchInfo material = new BatchInfo(combineSceneWaterShader, ColorRgba.White);
                    material.SetTexture("mainTex", mainTexture);
                    material.SetTexture("lightTex", Texture.White);
                    material.SetTexture("displacementTex", noiseTexture);

                    material.SetUniform("ambientLight", 1f);
                    material.SetUniform("waterLevel", viewWaterLevel / viewSize.Y);

                    Blit(drawDevice, material, finalTarget);
                } else {
                    // Full light, skip lighting phase
                    BatchInfo material = new BatchInfo(DrawTechnique.Solid, ColorRgba.White);
                    material.MainTexture = mainTexture;
                    Blit(drawDevice, material, finalTarget);
                }
                return;
            }*/

            // Blit ambient light color
            {
                BatchInfo material = new BatchInfo(DrawTechnique.Solid, new ColorRgba(ambientLight, 0, 0));
                Blit(drawDevice, material, lightingTarget);
            }

            // Render lights
            VertexC1P3T2[] vertices = new VertexC1P3T2[4];

            foreach (GameObject actor in levelHandler.ActiveObjects) {
                LightEmitter light = actor.GetComponent<LightEmitter>();
                if (light != null) {
                    Vector3 pos = actor.Transform.Pos;

                    if (pos.X - light.RadiusFar < viewOffset.X + viewSize.X &&
                        pos.Y - light.RadiusFar < viewOffset.Y + viewSize.Y &&
                        pos.X + light.RadiusFar > viewOffset.X &&
                        pos.Y + light.RadiusFar > viewOffset.Y) {

                        BatchInfo material;

                        switch (light.Type) {
                            default:
                            case LightType.Solid:
                                material = new BatchInfo(lightingShader, ColorRgba.White);
                                break;

                            case LightType.WithNoise:
                                material = new BatchInfo(lightingNoiseShader, ColorRgba.White);
                                material.SetTexture("noiseTex", noiseTexture);
                                break;

                            //case LightType.Unstable:
                            //    material = new BatchInfo(lightingUnstableShader, ColorRgba.White);
                            //    material.SetTexture("noiseTex", noiseTexture);
                            //    break;
                        }

                        pos.X -= viewOffset.X;
                        pos.Y -= viewOffset.Y;

                        material.SetUniform("center", pos.X, pos.Y);
                        material.SetUniform("intensity", light.Intensity);
                        material.SetUniform("brightness", light.Brightness);
                        material.SetUniform("radiusNear", light.RadiusNear);
                        material.SetUniform("radiusFar", light.RadiusFar);

                        material.SetTexture("normalBuffer", normalTexture);

                        float left = pos.X - light.RadiusFar;
                        float top = pos.Y - light.RadiusFar;
                        float right = pos.X + light.RadiusFar;
                        float bottom = pos.Y + light.RadiusFar;

                        vertices[0].Pos.X = left;
                        vertices[0].Pos.Y = top;
                        vertices[0].TexCoord.X = 0f;
                        vertices[0].TexCoord.Y = 0f;

                        vertices[1].Pos.X = left;
                        vertices[1].Pos.Y = bottom;
                        vertices[1].TexCoord.X = 0f;
                        vertices[1].TexCoord.Y = 1f;

                        vertices[2].Pos.X = right;
                        vertices[2].Pos.Y = bottom;
                        vertices[2].TexCoord.X = 1f;
                        vertices[2].TexCoord.Y = 1f;

                        vertices[3].Pos.X = right;
                        vertices[3].Pos.Y = top;
                        vertices[3].TexCoord.X = 1f;
                        vertices[3].TexCoord.Y = 0f;

                        drawDevice.PrepareForDrawcalls();
                        drawDevice.AddVertices(material, VertexMode.Quads, vertices);
                        drawDevice.Render();
                    }
                }
            }

/*#if __ANDROID__
            // Blit it into screen
            if (viewWaterLevel < viewSize.Y) {
                // Render lighting with water
                BatchInfo material = new BatchInfo(combineSceneWaterShader, ColorRgba.White);
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);
                material.SetTexture("displacementTex", noiseTexture);

                material.SetTexture("blurHalfTex", mainTexture);
                material.SetTexture("blurQuarterTex", mainTexture);

                material.SetUniform("ambientLight", ambientLight);
                material.SetUniform("waterLevel", viewWaterLevel / viewSize.Y);

                Blit(drawDevice, material, finalTarget);
            } else {
                // Render lighting without water
                BatchInfo material = new BatchInfo(combineSceneShader, ColorRgba.White);
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);

                material.SetTexture("blurHalfTex", mainTexture);
                material.SetTexture("blurQuarterTex", mainTexture);

                material.SetUniform("ambientLight", ambientLight);

                //Blit(drawDevice, material, viewportRect);
                Blit(drawDevice, material, finalTarget);
            }
#else*/
            SetupTargets((Point2)drawDevice.TargetSize);

            // Blit it into screen
            {
                BatchInfo material = new BatchInfo(DrawTechnique.Solid, ColorRgba.White);
                material.MainTexture = mainTexture;
                Blit(drawDevice, material, targetPingPongA[0]);
            }

            // Downsample to lowest target
            for (int i = 1; i < this.targetPingPongA.Length; i++) {
                BatchInfo material = new BatchInfo(downsampleShader, ColorRgba.White);

                material.MainTexture = targetPingPongA[i - 1].Targets[0];

                // ToDo: Using uniform instead of textureSize function
                material.SetUniform("pixelOffset", 1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight);

                Blit(drawDevice, material, targetPingPongA[i]);
            }

            // Blur all targets, separating horizontal and vertical blur
            for (int i = 0; i < targetPingPongA.Length; i++) {
                BatchInfo material = new BatchInfo(blurShader, ColorRgba.White);

                material.MainTexture = targetPingPongA[i].Targets[0];
                material.SetUniform("blurDirection", 1.0f, 0.0f);

                // ToDo: Using uniform instead of textureSize function
                material.SetUniform("pixelOffset", 1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight);

                Blit(drawDevice, material, targetPingPongB[i]);

                material.MainTexture = targetPingPongB[i].Targets[0];
                material.SetUniform("blurDirection", 0.0f, 1.0f);

                // ToDo: Using uniform instead of textureSize function
                material.SetUniform("pixelOffset", 1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight);

                Blit(drawDevice, material, targetPingPongA[i]);
            }

            // Blit it into screen
            if (viewWaterLevel < viewSize.Y) {
                // Render lighting with water
                BatchInfo material = new BatchInfo(combineSceneWaterShader, ColorRgba.White);
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);
                material.SetTexture("displacementTex", noiseTexture);

                material.SetTexture("blurHalfTex", targetPingPongA[1].Targets[0]);
                material.SetTexture("blurQuarterTex", targetPingPongA[2].Targets[0]);

                material.SetUniform("ambientLight", ambientLight);
                material.SetUniform("waterLevel", viewWaterLevel / viewSize.Y);

                Blit(drawDevice, material, finalTarget);
            } else {
                // Render lighting without water
                BatchInfo material = new BatchInfo(combineSceneShader, ColorRgba.White);
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);

                //material.SetTexture("blurFullTex", this.targetPingPongA[0].Targets[0]);
                material.SetTexture("blurHalfTex", targetPingPongA[1].Targets[0]);
                material.SetTexture("blurQuarterTex", targetPingPongA[2].Targets[0]);

                material.SetUniform("ambientLight", ambientLight);

                //Blit(drawDevice, material, viewportRect);
                Blit(drawDevice, material, finalTarget);
            }
//#endif
        }

        private static void Blit(DrawDevice device, BatchInfo source, RenderTarget target)
        {
            device.Target = target;
            device.TargetSize = target.Size;
            device.ViewportRect = new Rect(target.Size);

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }

        private static void Blit(DrawDevice device, BatchInfo source, Rect screenRect)
        {
            device.Target = null;
            device.TargetSize = screenRect.Size;
            device.ViewportRect = screenRect;

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }

        private void SetupTargets(Point2 size)
        {
            for (int i = 0; i < targetPingPongA.Length; i++) {
                SetupTarget(ref targetPingPongA[i], size);
                SetupTarget(ref targetPingPongB[i], size);
                size /= 2;
            }
        }

        private void SetupTarget(ref RenderTarget renderTarget, Point2 size)
        {
            // Create a new rendering target and backing texture, if not existing yet
            if (renderTarget == null) {
                Texture tex = new Texture(
                    size.X,
                    size.Y,
                    TextureSizeMode.NonPowerOfTwo,
                    TextureMagFilter.Linear,
                    TextureMinFilter.Linear);

                renderTarget = new RenderTarget(AAQuality.Off, false, tex);
            }

            // Resize the existing target to match the specified size
            if (renderTarget.Size != size) {
                Texture tex = renderTarget.Targets[0].Res;
                tex.Size = size;
                tex.ReloadData();
                renderTarget.SetupTarget();
            }
        }
    }
}