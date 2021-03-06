﻿using Duality;
using Jazz2.Game;

namespace Jazz2.Actors
{
    public class Explosion : ActorBase
    {
        // Available explosion types
        public const ushort Tiny = 0;
        public const ushort TinyBlue = 1;
        public const ushort TinyDark = 2;
        public const ushort Small = 3;
        public const ushort SmallDark = 4;
        public const ushort Large = 5;

        public const ushort SmokeBrown = 6;
        public const ushort SmokeGray = 7;
        public const ushort SmokeWhite = 8;
        public const ushort SmokePoof = 9;

        public const ushort WaterSplash = 10;

        public const ushort Pepper = 11;
        public const ushort RF = 12;

        public const ushort Generator = 20;

        private ushort type;
        private LightEmitter light;

        public static void Create(ActorApi api, Vector3 pos, ushort type)
        {
            Explosion explosion = new Explosion();
            explosion.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos,
                Params = new[] { type }
            });
            api.AddActor(explosion);
        }

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.None;

            type = details.Params[0];

            RequestMetadata("Common/Explosions");

            switch (type)
            {
                default:
                case Tiny: SetAnimation("Tiny"); break;
                case TinyBlue: SetAnimation("TinyBlue"); break;
                case TinyDark: SetAnimation("TinyDark"); break;
                case Small: SetAnimation("Small"); break;
                case SmallDark: SetAnimation("SmallDark"); break;
                case Large: {
                    SetAnimation("Large");

                    light = AddComponent<LightEmitter>();
                    light.Intensity = 0.8f;
                    light.Brightness = 0.9f;
                    light.RadiusNear = 0f;
                    light.RadiusFar = 55f;
                    break;
                }

                case SmokeBrown: SetAnimation("SmokeBrown"); break;
                case SmokeGray: SetAnimation("SmokeGray"); break;
                case SmokeWhite: SetAnimation("SmokeWhite"); break;
                case SmokePoof: SetAnimation("SmokePoof"); break;

                case WaterSplash: SetAnimation("WaterSplash"); break;

                case Pepper: {
                    SetAnimation("Pepper");

                    light = AddComponent<LightEmitter>();
                    light.Intensity = 0.5f;
                    light.Brightness = 0.2f;
                    light.RadiusNear = 7f;
                    light.RadiusFar = 14f;
                    break;
                }
                case RF: {
                    SetAnimation("RF");

                    light = AddComponent<LightEmitter>();
                    light.Intensity = 0.8f;
                    light.Brightness = 0.9f;
                    light.RadiusNear = 0f;
                    light.RadiusFar = 50f;
                    break;
                }

                case Generator: {
                    SetAnimation("Generator");

                    // Apply random orientation
                    Transform.Angle = MathF.Rnd.Next(4) * MathF.PiOver2;
                    IsFacingLeft = (MathF.Rnd.NextFloat() < 0.5f);
                    break;
                }
            }
        }

        protected override void OnAnimationFinished()
        {
            //base.OnAnimationFinished();

            DecreaseHealth(int.MaxValue);
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();

            switch (type) {
                case Large: {
                    float timeMult = Time.TimeMult;
                    light.RadiusFar -= timeMult * 5f;
                    break;
                }

                case Pepper: {
                    float timeMult = Time.TimeMult;
                    light.Intensity -= timeMult * 0.05f;
                    break;
                }

                case RF: {
                    float timeMult = Time.TimeMult;
                    light.RadiusFar -= timeMult * 0.8f;
                    light.Intensity -= timeMult * 0.02f;
                    break;
                }
            }
        }
    }
}