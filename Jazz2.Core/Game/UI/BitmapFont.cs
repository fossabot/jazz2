﻿using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;

namespace Jazz2.Game.UI
{
    public class BitmapFont
    {
        // ToDo: JJ2 uses different colors for menu and in-game
        private static readonly ColorRgba[] colors = {
            new ColorRgba(0.4f, 0.55f, 0.85f, 0.5f),
            new ColorRgba(0.7f, 0.45f, 0.42f, 0.5f),
            new ColorRgba(0.58f, 0.48f, 0.38f, 0.5f),
            new ColorRgba(0.25f, 0.45f, 0.3f, 0.5f),
            new ColorRgba(0.7f, 0.42f, 0.7f, 0.5f),
            new ColorRgba(0.44f, 0.44f, 0.8f, 0.5f),
            new ColorRgba(0.54f, 0.54f, 0.54f, 0.5f)
        };

        private ContentRef<Material> materialPlain, materialColor;
        private Rect[] chars = new Rect[256];
        private int spacing, height;

        private readonly Canvas canvas;

        public int Height => height;

        // ToDo: Move parameters to .config file, rework .config file format
        public BitmapFont(Canvas canvas, string path, int width, int height, int cols, int first, int last, int defaultSpacing)
        {
            this.canvas = canvas;

#if UNCOMPRESSED_CONTENT
            string png = PathOp.Combine(DualityApp.DataDirectory, "Animations", path + ".png");
#else
            string png = PathOp.Combine(DualityApp.DataDirectory, ".dz", "Animations", path + ".png");
#endif
            string config = png + ".config";

            IImageCodec imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);
            using (Stream s = FileOp.Open(png, FileAccessMode.Read)) {
                PixelData pixelData = imageCodec.Read(s);

                ColorRgba[] palette = ContentResolver.Current.Palette.Res.BasePixmap.Res.PixelData[0].Data;

                ColorRgba[] data = pixelData.Data;
                Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                    for (int i = range.Item1; i < range.Item2; i++) {
                        int colorIdx = data[i].R;
                        data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));
                    }
                });

                Texture texture = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Linear, TextureMinFilter.Linear);

                materialPlain = new Material(DrawTechnique.Alpha, texture);
                materialColor = new Material(ContentResolver.Current.RequestShader("Colorize"), texture);
            }

            byte[] widthFromFileTable = new byte[256];
            using (Stream s = FileOp.Open(config, FileAccessMode.Read)) {
                s.Read(widthFromFileTable, 0, widthFromFileTable.Length);
            }

            this.height = height;
            spacing = defaultSpacing;

            uint charCode = 0;
            for (int i = first; i < last; i++, charCode++) {
                chars[i] = new Rect(
                    (float)((i - first) % cols) / cols,
                    (float)((i - first) / cols) / cols,
                    widthFromFileTable[charCode],
                    height);

                if (charCode > last || i >= 255) {
                    break;
                }
            }
        }

        public unsafe void DrawString(ref int charOffset, string text, float x, float y, Alignment alignment, ColorRgba? color = null, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f, float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            float phase = (float)Time.GameTimer.TotalSeconds * speed;

            bool hasColor = false;
            // Pre-compute text size
            //int lines = 1;
            float totalWidth = 0f, lastWidth = 0f, totalHeight = 0f;
            float charSpacingPre = charSpacing;
            float scalePre = scale;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    if (lastWidth < totalWidth) {
                        lastWidth = totalWidth;
                    }
                    totalWidth = 0f;
                    totalHeight += (height * scale * lineSpacing);
                    //lines++;
                    continue;
                } else if (text[i] == '\f' && text[i + 1] == '[') {
                    i += 2;
                    int formatIndex = i;
                    while (text[i] != ']') {
                        i++;
                    }

                    if (text[formatIndex + 1] == ':') {
                        int paramInt;
                        switch (text[formatIndex]) {
                            case 'c': // Color
                                hasColor = true;
                                break;
                            case 's': // Scale
                                      //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    scalePre = paramInt * 0.01f;
                                }
                                break;
                            case 'w': // Char spacing
                                      //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    charSpacingPre = paramInt * 0.01f;
                                }
                                break;
                        }
                    }
                    continue;
                }

                Rect uvRect = chars[(byte)text[i]];
                if (uvRect.W > 0 && uvRect.H > 0) {
                    totalWidth += (uvRect.W + spacing) * charSpacingPre * scalePre;
                }
            }
            if (lastWidth < totalWidth) {
                lastWidth = totalWidth;
            }
            totalHeight += (height * scale * lineSpacing);

            VertexC1P3T2[] vertexData = canvas.RentVertices(text.Length * 4);

            // Set default material
            bool colorize, allowColorChange;
            ContentRef<Material> material;
            ColorRgba mainColor;
            if (color.HasValue) {
                mainColor = color.Value;
                if (mainColor == ColorRgba.TransparentBlack) {
                    if (hasColor) {
                        material = materialColor;
                        mainColor = new ColorRgba(0.46f, 0.46f, 0.4f, 0.5f);
                    } else {
                        material = materialPlain;
                        mainColor = ColorRgba.White;
                    }
                } else {
                    material = materialColor;
                }
                colorize = false;

                if (mainColor.R == 0 && mainColor.G == 0 && mainColor.B == 0) {
                    allowColorChange = false;
                } else {
                    allowColorChange = true;
                }
            } else {
                material = materialColor;
                mainColor = ColorRgba.White;
                colorize = true;
                allowColorChange = false;
            }

            Vector2 uvRatio = new Vector2(
                1f / materialPlain.Res.MainTexture.Res.ContentWidth,
                1f / materialPlain.Res.MainTexture.Res.ContentHeight
            );

            int vertexIndex = 0;

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(lastWidth /** scale*/, totalHeight/*lines * height * scale * lineSpacing*/));
            float lineStart = originPos.X;

            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    // New line
                    originPos.X = lineStart;
                    originPos.Y += (height * scale * lineSpacing);
                    continue;
                } else if (text[i] == '\f' && text[i + 1] == '[') {
                    // Format
                    i += 2;
                    int formatIndex = i;
                    while (text[i] != ']') {
                        i++;
                    }

                    if (text[formatIndex + 1] == ':') {
                        int paramInt;
                        switch (text[formatIndex]) {
                            case 'c': // Color
                                //if (allowColorChange && int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (allowColorChange && int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    if (paramInt == -1) {
                                        colorize = true;
                                    } else {
                                        colorize = false;
                                        mainColor = colors[paramInt % colors.Length];
                                    }
                                }
                                break;
                            case 's': // Scale
                                //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    scale = paramInt * 0.01f;
                                }
                                break;
                            case 'w': // Char spacing
                                //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    charSpacing = paramInt * 0.01f;
                                }
                                break;
                        }
                    }
                    continue;
                }

                Rect uvRect = chars[(byte)text[i]];
                if (uvRect.W > 0 && uvRect.H > 0) {
                    if (colorize) {
                        mainColor = colors[charOffset % colors.Length];
                    }

                    Vector3 pos = new Vector3(originPos);

                    if (angleOffset > 0f) {
                        pos.X += MathF.Cos((phase + charOffset) * angleOffset * MathF.Pi) * varianceX * scale;
                        pos.Y += MathF.Sin((phase + charOffset) * angleOffset * MathF.Pi) * varianceY * scale;
                    }

                    pos.X = MathF.Round(pos.X);
                    pos.Y = MathF.Round(pos.Y);

                    float x2 = MathF.Round(pos.X + uvRect.W * scale);
                    float y2 = MathF.Round(pos.Y + uvRect.H * scale);

                    vertexData[vertexIndex + 0].Pos = pos;
                    vertexData[vertexIndex + 0].TexCoord.X = uvRect.X;
                    vertexData[vertexIndex + 0].TexCoord.Y = uvRect.Y;
                    vertexData[vertexIndex + 0].Color = mainColor;

                    vertexData[vertexIndex + 1].Pos.X = pos.X;
                    vertexData[vertexIndex + 1].Pos.Y = y2;
                    vertexData[vertexIndex + 1].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 1].TexCoord.X = uvRect.X;
                    vertexData[vertexIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H * uvRatio.Y;
                    vertexData[vertexIndex + 1].Color = mainColor;

                    vertexData[vertexIndex + 2].Pos.X = x2;
                    vertexData[vertexIndex + 2].Pos.Y = y2;
                    vertexData[vertexIndex + 2].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 2].TexCoord.X = uvRect.X + uvRect.W * uvRatio.X;
                    vertexData[vertexIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H * uvRatio.Y;
                    vertexData[vertexIndex + 2].Color = mainColor;

                    vertexData[vertexIndex + 3].Pos.X = x2;
                    vertexData[vertexIndex + 3].Pos.Y = pos.Y;
                    vertexData[vertexIndex + 3].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 3].TexCoord.X = uvRect.X + uvRect.W * uvRatio.X;
                    vertexData[vertexIndex + 3].TexCoord.Y = uvRect.Y;
                    vertexData[vertexIndex + 3].Color = mainColor;

                    if (MathF.RoundToInt(canvas.DrawDevice.TargetSize.X) != (MathF.RoundToInt(canvas.DrawDevice.TargetSize.X) / 2) * 2) {
                        float align = 0.5f / canvas.DrawDevice.TargetSize.X;

                        vertexData[vertexIndex + 0].Pos.X += align;
                        vertexData[vertexIndex + 1].Pos.X += align;
                        vertexData[vertexIndex + 2].Pos.X += align;
                        vertexData[vertexIndex + 3].Pos.X += align;
                    }

                    if (MathF.RoundToInt(canvas.DrawDevice.TargetSize.Y) != (MathF.RoundToInt(canvas.DrawDevice.TargetSize.Y) / 2) * 2) {
                        float align = 0.5f * scale / canvas.DrawDevice.TargetSize.Y;

                        vertexData[vertexIndex + 0].Pos.Y += align;
                        vertexData[vertexIndex + 1].Pos.Y += align;
                        vertexData[vertexIndex + 2].Pos.Y += align;
                        vertexData[vertexIndex + 3].Pos.Y += align;
                    }

                    vertexIndex += 4;

                    originPos.X += ((uvRect.W + spacing) * scale * charSpacing);
                }
                charOffset++;
            }
            charOffset++;

            // Submit all the vertices as one draw batch
            canvas.DrawDevice.AddVertices(
                material,
                VertexMode.Quads,
                vertexData,
                0,
                vertexIndex);
        }

        public static string StripFormatting(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf('\f') == -1) {
                return text;
            }

            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\f' && i + 2 < text.Length && text[i + 1] == '[') {
                    i = text.IndexOf(']', i);
                } else {
                    sb.Append(text[i]);
                }
            }
            return sb.ToString();
        }
    }
}