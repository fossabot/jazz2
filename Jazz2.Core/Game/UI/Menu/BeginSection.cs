﻿using System;
using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Jazz2.Game.UI.Menu.Settings;

namespace Jazz2.Game.UI.Menu
{
    public class BeginSection : MenuSection
    {
        private List<Tuple<string, Action>> items;

        private int selectedIndex;
        private float animation;

        public BeginSection()
        {
            items = new List<Tuple<string, Action>> {
                Tuple.Create<string, Action>("Play Story", OnPlayStoryPressed),
                Tuple.Create<string, Action>("Play Custom Game", OnPlayCustomGamePressed),
                Tuple.Create<string, Action>("Settings", OnSettingsPressed),
                Tuple.Create<string, Action>("About", OnAboutPressed),
                Tuple.Create<string, Action>("Exit", OnExitPressed),
            };
        }

        public override void OnShow(IMenuContainer root)
        {
            animation = 0f;
            base.OnShow(root);
        }

        public override void OnPaint(Canvas canvas)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.76f;

            int charOffset = 0;
            for (int i = 0; i < items.Count; i++) {
                if (selectedIndex == i) {
                    float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                    api.DrawMaterial("MenuGlow", center.X, center.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (items[i].Item1.Length + 3) * 0.5f * size, 4f * size);

                    api.DrawStringShadow(ref charOffset, items[i].Item1, center.X, center.Y,
                        Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
                } else {
                    api.DrawString(ref charOffset, items[i].Item1, center.X, center.Y,
                        Alignment.Center, ColorRgba.TransparentBlack, 0.9f);
                }

                center.Y += 34f + 8f;
            }
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }
            
            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                api.PlaySound("MenuSelect", 0.5f);
                items[selectedIndex].Item2();
            } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                if (selectedIndex != items.Count - 1) {
                    api.PlaySound("MenuSelect", 0.5f);
                    animation = 0f;
                    selectedIndex = items.Count - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = items.Count - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex < items.Count - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
            }
        }

        private void OnPlayStoryPressed()
        {
            api.SwitchToSection(new EpisodeSelectSection());
        }

        private void OnPlayCustomGamePressed()
        {
#if MULTIPLAYER
            api.SwitchToSection(new CustomGameSection());
#else
            api.SwitchToSection(new CustomLevelSelectSection());
#endif
        }

        private void OnSettingsPressed()
        {
            api.SwitchToSection(new SettingsSection());
        }

        private void OnAboutPressed()
        {
            api.SwitchToSection(new AboutSection());
        }

        private void OnExitPressed()
        {
            DualityApp.Terminate();
        }
    }
}