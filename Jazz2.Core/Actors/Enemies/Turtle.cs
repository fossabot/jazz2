﻿using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Turtle : EnemyBase
    {
        private float DefaultSpeed = 1f;

        private ushort theme;
        private bool isTurning;
        private bool isWithdrawn;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(1);
            scoreValue = 100;

            theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Enemy/Turtle");
                    break;

                case 1: // Xmas
                    RequestMetadata("Enemy/TurtleXmas");
                    break;
            }

            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 24);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
                if (MathF.Abs(speedX) > float.Epsilon && !CanMoveToPosition(speedX * 4, 0)) {
                    SetTransition(AnimState.TransitionWithdraw, false, delegate {
                        HandleTurn(true);
                    });
                    isTurning = true;
                    canHurtPlayer = false;
                    speedX = 0;
                    PlaySound("Withdraw", 0.4f);
                }
            }

            if (!isTurning && !isWithdrawn && !isAttacking) {
                Hitbox hitbox = currentHitbox + new Vector2(speedX * 32, 0);
                if (api.TileMap.IsTileEmpty(ref hitbox, true)) {
                    foreach (Player player in api.GetCollidingPlayers(currentHitbox + new Vector2(speedX * 32, 0))) {
                        if (!player.IsInvulnerable) {
                            Attack();
                            break;
                        }
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            TurtleShell shell = new TurtleShell(speedX * 1.1f, 1.1f);
            shell.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = Transform.Pos,
                Params = new[] { theme }
            });
            api.AddActor(shell);

            Explosion.Create(api, Transform.Pos, Explosion.SmokeGray);

            return base.OnPerish(collider);
        }

        private void HandleTurn(bool isFirstPhase)
        {
            if (isTurning) {
                if (isFirstPhase) {
                    IsFacingLeft = !IsFacingLeft;
                    SetTransition(AnimState.TransitionWithdrawEnd, false, delegate {
                       HandleTurn(false);
                    });
                    PlaySound("WithdrawEnd", 0.4f);
                    isWithdrawn = true;
                } else {
                    canHurtPlayer = true;
                    isWithdrawn = false;
                    isTurning = false;
                    speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                }
            }
        }

        private void Attack()
        {
            speedX = 0;
            isAttacking = true;
            PlaySound("Attack");

            SetTransition(AnimState.TransitionAttack, false, delegate {
                speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                isAttacking = false;

                // ToDo: Bad timing
                //PlaySound("Attack2");
            });
        }
    }
}