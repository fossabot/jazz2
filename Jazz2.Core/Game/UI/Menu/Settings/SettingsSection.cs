﻿using Duality;
using Duality.Drawing;
using Jazz2.Storage;
using static Jazz2.SettingsCache;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class SettingsSection : MenuSectionWithControls
    {
        private ChoiceControl resizeMode;
#if __ANDROID__
        private ChoiceControl vibrations;
#else
        private ChoiceControl screenMode;
#endif
        private SliderControl musicVolume;
        private SliderControl sfxVolume;

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

#if __ANDROID__
            // 3xBRZ shader is not available in OpenGL ES 3.0 version
            resizeMode = new ChoiceControl(api, "Resize Mode", (int)Resize, "None", "HQ2x");
            vibrations = new ChoiceControl(api, "Vibrations", Duality.Android.InnerView.allowVibrations ? 1 : 0, "Disable", "Enable");
#else
            resizeMode = new ChoiceControl(api, "Resize Mode", (int)Resize, "None", "HQ2x", "3xBRZ", "4xBRZ", "CRT");

            ScreenMode screenModeCurrent = api.ScreenMode;
            int screenModeValue;
            if ((screenModeCurrent & ScreenMode.FullWindow) != 0) {
                screenModeValue = 1;
            } else {
                screenModeValue = 0;
            }
            screenMode = new ChoiceControl(api, "Screen Mode", screenModeValue, "Window", "Fullscreen");
#endif
            musicVolume = new SliderControl(api, "Music Volume", MusicVolume, 0f, 1f);
            sfxVolume = new SliderControl(api, "SFX Volume", SfxVolume, 0f, 1f);

#if __ANDROID__
            controls = new MenuControlBase[] {
                resizeMode, vibrations, musicVolume, sfxVolume,
                new LinkControl(api, "Controls", OnControlsPressed)
            };
#else
            controls = new MenuControlBase[] {
                resizeMode, screenMode, musicVolume, sfxVolume,
                new LinkControl(api, "Controls", OnControlsPressed)
            };
#endif
        }

        public override void OnHide(bool isRemoved)
        {
            if (isRemoved) {
                Commit();
            }

            base.OnHide(isRemoved);
        }

        public override void OnPaint(Canvas canvas)
        {
            base.OnPaint(canvas);

            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

#if __ANDROID__
            var fs = (DualityApp.SystemBackend.FileSystem as Duality.Backend.Android.NativeFileSystem);
            if (fs != null) {
                api.DrawMaterial("MenuSettingsStorage", 180f, center.Y + 140f - 3f, Alignment.Right, ColorRgba.White);

                int charOffset = 0;
                api.DrawStringShadow(ref charOffset, "Content Path:",
                    180f + 10f, center.Y + 140f, Alignment.Left, new ColorRgba(0.68f, 0.46f, 0.42f, 0.5f), 0.8f, charSpacing: 0.9f);

                api.DrawString(ref charOffset, fs.RootPath,
                    180f + 10f + 98f, center.Y + 140f, Alignment.Left, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.85f);
            }
#endif
        }

        private void Commit()
        {
            Resize = (ResizeMode)resizeMode.SelectedIndex;
            MusicVolume = musicVolume.CurrentValue;
            SfxVolume = sfxVolume.CurrentValue;

            Preferences.Set("Resize", (byte)Resize);
            Preferences.Set("MusicVolume", (byte)(MusicVolume * 100));
            Preferences.Set("SfxVolume", (byte)(SfxVolume * 100));

#if __ANDROID__
            Duality.Android.InnerView.allowVibrations = (vibrations.SelectedIndex == 1);
            Preferences.Set("Vibrations", Duality.Android.InnerView.allowVibrations);
#else
            ScreenMode newScreenMode;
            switch (screenMode.SelectedIndex) {
                default:
                case 0: newScreenMode = ScreenMode.Window; break;
                case 1: newScreenMode = ScreenMode.FullWindow; break;
            }
            api.ScreenMode = newScreenMode;

            Preferences.Set("Screen", screenMode.SelectedIndex);
#endif

            Preferences.Commit();
        }

        private void OnControlsPressed()
        {
            api.SwitchToSection(new ControlsSection());
        }
    }
}