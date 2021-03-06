﻿namespace Jazz2.Actors.Solid
{
    public class PushBox : SolidObjectBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort theme = details.Params[0];

            Movable = true;

            switch (theme) {
                case 0: RequestMetadata("Object/PushBoxRock"); break;
                case 1: RequestMetadata("Object/PushBoxCrate"); break;
            }

            SetAnimation("PushBox");
        }
    }
}