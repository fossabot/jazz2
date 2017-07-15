﻿using System;
using System.Runtime.CompilerServices;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    partial class TileMap : ICmpRenderer
    {
        private VertexC1P3T2[][] cachedVertices;

        float ICmpRenderer.BoundRadius
        {
            get
            {
                return float.MaxValue;
            }
        }

        bool ICmpRenderer.IsVisible(IDrawDevice device)
        {
            const VisibilityFlag visibilityGroup = VisibilityFlag.Group0;

            if ((device.VisibilityMask & VisibilityFlag.ScreenOverlay) != (visibilityGroup & VisibilityFlag.ScreenOverlay)) return false;
            if ((visibilityGroup & device.VisibilityMask & VisibilityFlag.AllGroups) == VisibilityFlag.None) return false;
            return true;
        }

        void ICmpRenderer.Draw(IDrawDevice device)
        {
            if (tileset == null) {
                return;
            }

            if (cachedVertices == null || cachedVertices.Length != levelLayout.Count) {
                cachedVertices = new VertexC1P3T2[levelLayout.Count][];
            }

            TileMapLayer[] layers = levelLayout.Data;
            for (int i = levelLayout.Count - 1; i >= 0; i--) {
                DrawLayer(device, ref layers[i], i);
            }

            DrawDebris(device);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float TranslateCoordinate(float coordinate, float speed, float offset, bool isY, float viewHeight, float viewWidth)
        {
            // Coordinate: the "vanilla" coordinate of the tile on the layer if the layer was fixed to the sprite layer with same
            // speed and no other options. Think of its position in JCS.
            // Speed: the set layer speed; 1 for anything that moves the same speed as the sprite layer (where the objects live),
            // less than 1 for backgrounds that move slower, more than 1 for foregrounds that move faster
            // Offset: any difference to starting coordinates caused by an inherent automatic speed a layer has

            // Literal 70 is the same as in drawLayer, it's the offscreen offset of the first tile to draw.
            // Don't touch unless absolutely necessary.
            return (coordinate * speed + offset + (70 + (isY ? (viewHeight - 200) : (viewWidth - 320)) / 2) * (speed - 1));
        }

        private void DrawLayer(IDrawDevice device, ref TileMapLayer layer, int layerIndex)
        {
            Vector2 viewSize = device.TargetSize;
            Vector3 viewCenter = device.RefCoord;

            Point2 tileCount = new Point2(layer.LayoutWidth, layer.Layout.Length / layer.LayoutWidth);
            Vector2 tileSize = new Vector2(tileset.TileSize, tileset.TileSize);

            // Update offsets for moving layers
            if (Math.Abs(layer.AutoSpeedX) > float.Epsilon) {
                layer.OffsetX += layer.AutoSpeedX * Time.TimeMult;
                if (layer.RepeatX) {
                    if (layer.AutoSpeedX > 0) {
                        while (layer.OffsetX > (tileCount.X * 32)) {
                            layer.OffsetX -= (tileCount.X * 32);
                        }
                    } else {
                        while (layer.OffsetX < 0) {
                            layer.OffsetX += (tileCount.X * 32);
                        }
                    }
                }
            }
            if (Math.Abs(layer.AutoSpeedY) > float.Epsilon) {
                layer.OffsetY += layer.AutoSpeedY * Time.TimeMult;
                if (layer.RepeatY) {
                    if (layer.AutoSpeedY > 0) {
                        while (layer.OffsetY > (tileCount.Y * 32)) {
                            layer.OffsetY -= (tileCount.Y * 32);
                        }
                    } else {
                        while (layer.OffsetY < 0) {
                            layer.OffsetY += (tileCount.Y * 32);
                        }
                    }
                }
            }

            // Get current layer offsets and speeds
            float lox = layer.OffsetX;
            float loy = layer.OffsetY - (layer.UseInherentOffset ? (viewSize.Y - 200) / 2 : 0);

            // Find out coordinates for a tile from outside the boundaries from topleft corner of the screen 
            float x1 = viewCenter.X - 70 - (viewSize.X * 0.5f);
            float y1 = viewCenter.Y - 70 - (viewSize.Y * 0.5f);

            // Figure out the floating point offset from the calculated coordinates and the actual tile
            // corner coordinates
            float x_t = TranslateCoordinate(x1, layer.SpeedX, lox, false, viewSize.Y, viewSize.X);
            float y_t = TranslateCoordinate(y1, layer.SpeedY, loy, true, viewSize.Y, viewSize.X);

            float rem_x = x_t % 32f;
            float rem_y = y_t % 32f;

            // Determine the actual drawing location on screen
            float xinter = x_t / 32f;
            float yinter = y_t / 32f;

            // Calculate the index (on the layer map) of the first tile that needs to be drawn to the
            // position determined earlier
            int tile_x, tile_y, tile_absx, tile_absy;

            // Get the actual tile coords on the layer layout
            if (xinter > 0) {
                tile_absx = (int)Math.Floor(xinter);
                tile_x = tile_absx % tileCount.X;
            } else {
                tile_absx = (int)Math.Ceiling(xinter);
                tile_x = tile_absx % tileCount.X;
                while (tile_x < 0) {
                    tile_x += tileCount.X;
                }
            }

            if (yinter > 0) {
                tile_absy = (int)Math.Floor(yinter);
                tile_y = tile_absy % tileCount.Y;

            } else {
                tile_absy = (int)Math.Ceiling(yinter);
                tile_y = tile_absy % tileCount.Y;
                while (tile_y < 0) {
                    tile_y += tileCount.Y;
                }
            }

            // Save the tile Y at the left border so that we can roll back to it at the start of
            // every row iteration
            int tile_ys = tile_y;

            // update x1 and y1 with the remainder so that we start at the tile boundary
            // minus 1 because indices are updated in the beginning of the loops
            x1 -= rem_x - 32f;
            y1 -= rem_y - 32f;

            // Calculate the last coordinates we want to draw to
            float x3 = x1 + 100 + viewSize.X;
            float y3 = y1 + 100 + viewSize.Y;

            if (layer.BackgroundStyle != BackgroundStyle.Plain && tileCount.Y == 8 && tileCount.X == 8) {
                const float perspectiveSpeed = 0.4f;
                RenderTexturedBackground(device, ref layer, layerIndex,
                    ((x1 + rem_x) * perspectiveSpeed + lox),
                    ((y1 + rem_y) * perspectiveSpeed + loy));
            } else {
                Material material = tileset.Material.Res;
                Texture texture = material.MainTexture.Res;
                ColorRgba mainColor = ColorRgba.White;

                // Reserve the required space for vertex data in our locally cached buffer
                VertexC1P3T2[] vertexData;

                // ToDo: This is wrong!
                //int neededVertices = (int)(((x3 - x1) / 32) * ((y3 - y1) / 32) * 5);
                int neededVertices = (int)((((x3 - x1) / 32) + 1) * (((y3 - y1) / 32) + 1) * 4);
                if (cachedVertices[layerIndex] == null || cachedVertices[layerIndex].Length < neededVertices) {
                    cachedVertices[layerIndex] = vertexData = new VertexC1P3T2[neededVertices];
                } else {
                    vertexData = cachedVertices[layerIndex];
                }

                int vertexBaseIndex = 0;

                int tile_xo = -1;
                for (float x2 = x1; x2 < x3; x2 += 32) {
                    tile_x = (tile_x + 1) % tileCount.X;
                    tile_xo++;
                    if (!layer.RepeatX) {
                        // If the current tile isn't in the first iteration of the layer horizontally, don't draw this column
                        if (tile_absx + tile_xo + 1 < 0 || tile_absx + tile_xo + 1 >= tileCount.X) {
                            continue;
                        }
                    }
                    tile_y = tile_ys;
                    int tile_yo = -1;
                    for (float y2 = y1; y2 < y3; y2 += 32) {
                        tile_y = (tile_y + 1) % tileCount.Y;
                        tile_yo++;

                        LayerTile tile = layer.Layout[tile_x + tile_y * layer.LayoutWidth];

                        if (!layer.RepeatY) {
                            // If the current tile isn't in the first iteration of the layer vertically, don't draw it
                            if (tile_absy + tile_yo + 1 < 0 || tile_absy + tile_yo + 1 >= tileCount.Y) {
                                continue;
                            }
                        }

                        Point2 offset;
                        bool isFlippedX, isFlippedY;
                        if (tile.IsAnimated) {
                            if (tile.TileID < animatedTiles.Count) {
                                offset = animatedTiles[tile.TileID].CurrentTile.MaterialOffset;
                                isFlippedX = (animatedTiles[tile.TileID].CurrentTile.IsFlippedX != tile.IsFlippedX);
                                isFlippedY = (animatedTiles[tile.TileID].CurrentTile.IsFlippedY != tile.IsFlippedY);

                                //mainColor.A = tile.MaterialAlpha;
                                mainColor.A = animatedTiles[tile.TileID].CurrentTile.MaterialAlpha;
                            } else {
                                continue;
                            }
                        } else {
                            offset = tile.MaterialOffset;
                            isFlippedX = tile.IsFlippedX;
                            isFlippedY = tile.IsFlippedY;

                            mainColor.A = tile.MaterialAlpha;
                        }

                        Rect uvRect = new Rect(
                            offset.X * texture.UVRatio.X / texture.ContentWidth,
                            offset.Y * texture.UVRatio.Y / texture.ContentHeight,
                            tileset.TileSize * texture.UVRatio.X / texture.ContentWidth,
                            tileset.TileSize * texture.UVRatio.Y / texture.ContentHeight
                        );

                        // ToDo: Flip normal map somehow
                        if (isFlippedX) {
                            uvRect.X += uvRect.W;
                            uvRect.W *= -1;
                        }
                        if (isFlippedY) {
                            uvRect.Y += uvRect.H;
                            uvRect.H *= -1;
                        }

                        Vector3 renderPos = new Vector3(x2, y2, layer.Depth);
                        float scale = 1.0f;
                        device.PreprocessCoords(ref renderPos, ref scale);

                        renderPos.X = MathF.Round(renderPos.X);
                        renderPos.Y = MathF.Round(renderPos.Y);
                        if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                            renderPos.X += 0.5f;
                        }
                        if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                            renderPos.Y += 0.5f;
                        }

                        vertexData[vertexBaseIndex + 0].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 0].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 0].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 0].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 0].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 0].Color = mainColor;

                        vertexData[vertexBaseIndex + 1].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 1].Pos.Y = renderPos.Y + tileSize.Y;
                        vertexData[vertexBaseIndex + 1].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 1].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 1].Color = mainColor;

                        vertexData[vertexBaseIndex + 2].Pos.X = renderPos.X + tileSize.X;
                        vertexData[vertexBaseIndex + 2].Pos.Y = renderPos.Y + tileSize.Y;
                        vertexData[vertexBaseIndex + 2].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 2].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 2].Color = mainColor;

                        vertexData[vertexBaseIndex + 3].Pos.X = renderPos.X + tileSize.X;
                        vertexData[vertexBaseIndex + 3].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 3].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 3].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 3].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 3].Color = mainColor;

                        vertexBaseIndex += 4;
                    }
                }

                // Submit all the vertices as one draw batch
                device.AddVertices(
                    material,
                    VertexMode.Quads,
                    vertexData,
                    0,
                    vertexBaseIndex);
            }
        }
    }
}