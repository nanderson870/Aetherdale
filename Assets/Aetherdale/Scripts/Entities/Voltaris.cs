using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

namespace Aetherdale
{
    public class Voltaris : IdolForm
    {
        // Attacks Configuration
        [SerializeField] Hitbox rightPunchHitbox;
        [SerializeField] Hitbox leftPunchHitbox;
        public readonly int punchDamage = 10;

        // Thunderous Throw Configuration
        [SerializeField] Projectile voltarisBoulder;

        float boulderVelocity = 12.0F;
        Vector3 boulderSpawnOffset = new(0, -1, 4.0F);
        float boulderChargeTime = 1.5F;
        float boulderHeightIncrease = 5.0F;


        // Galvanic Strikes Configuration
        [SerializeField] VisualEffect rightPunchEffect;
        [SerializeField] VisualEffect leftPunchEffect;
        [SerializeField] Hitbox rightGalvanicStrikeHitbox;
        [SerializeField] Hitbox leftGalvanicStrikeHitbox;
        public readonly int galvanicStrikeDamage = 40;
        public readonly int galvanicStrikeCharges = 4;


        // Runtime
        [SyncVar] bool ability1Down = false;
        Projectile currentBoulder = null;
        bool usingAbility1 = false;

        Vector3 currentAim = new();

        [SyncVar] int galvanicStrikesRemaining = 0;


        public override void Update()
        {
            base.Update();

            if (ability1Down)
            {
                CmdSetAim(GetCamera().transform.forward);
            }
        }

        protected override void Animate()
        {
            base.Animate();

            Vector2 horizontalVelocity = new(GetLocalVelocity().x, GetLocalVelocity().z);
            if (horizontalVelocity.magnitude > 0.5F && !airborne)
            {
                animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 1.0F);
            }
            else
            {
                animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 0.0F);
            }
        }

        [Command]
        void CmdSetAim(Vector3 aim)
        {
            currentAim = aim;
        }

        public override bool CanMove()
        {
            return base.CanMove() && !usingAbility1;
        }

        #region Attacks
        [ServerCallback]
        void RightPunchStart()
        {
            rightPunchHitbox.StartHit(punchDamage, Element.Physical, HitType.Attack, this, impact: 150);
        }

        [ServerCallback]
        void RightPunchEnd()
        {
            rightPunchHitbox.EndHit();
        }

        [ServerCallback]
        void LeftPunchStart()
        {
            leftPunchHitbox.StartHit(punchDamage, Element.Physical, HitType.Attack, this, impact: 150);
        }

        [ServerCallback]
        void LeftPunchEnd()
        {
            leftPunchHitbox.EndHit();
        }
        #endregion




        #region Ability 1
        [Server]
        protected override void Ability1()
        {
            TargetEnterAbility1();

            usingAbility1 = true;
            ability1Down = true;
            currentBoulder = Projectile.Create(voltarisBoulder, transform.position + transform.TransformVector(boulderSpawnOffset), Quaternion.identity, gameObject, Vector3.zero);
            currentBoulder.GetComponent<VoltarisBoulder>().SetVoltaris(this);
            currentBoulder.enabled = false;
            currentBoulder.GetComponent<Collider>().enabled = false;

            SetAnimatorTrigger("ThunderousThrowEnter");

            StartCoroutine(nameof(SpinUpBoulder));
        }

        [TargetRpc]
        void TargetEnterAbility1()
        {
            GetCameraContext().AddOffset(new Vector3(0, 3.0F, 0));

            GetOwningPlayer().GetUI().ShowReticle();

            SetRotationTrackCamera(true);
        }

        IEnumerator SpinUpBoulder()
        {
            yield return new WaitForSeconds(0.4F); // give delay for animation to play

            float boulderHeight = currentBoulder.gameObject.transform.position.y;
            float boulderTargetHeight = boulderHeight + boulderHeightIncrease;

            float boulderRiseVelocity = boulderHeightIncrease / boulderChargeTime;

            currentBoulder.GetComponent<Rigidbody>().useGravity = false;

            while (currentBoulder.transform.position.y <= boulderTargetHeight)
            {
                currentBoulder.GetComponent<Rigidbody>().linearVelocity = new(currentBoulder.GetComponent<Rigidbody>().linearVelocity.x, boulderRiseVelocity, currentBoulder.GetComponent<Rigidbody>().linearVelocity.z);
                yield return new WaitForFixedUpdate();
            }

            currentBoulder.GetComponent<Rigidbody>().linearVelocity = new(0, 0, 0);

            while (ability1Down)
            {
                yield return new WaitForFixedUpdate();
            }

            SetAnimatorTrigger("ThunderousThrowThrow");

            yield return new WaitForSeconds(0.4F); // give delay for animation to play

            // Throw!
            currentBoulder.GetComponent<VoltarisBoulder>().SetVoltaris(null);
            currentBoulder.enabled = true;
            currentBoulder.GetComponent<Collider>().enabled = true;
            currentBoulder.GetComponent<Rigidbody>().useGravity = true;
            currentBoulder.SetVelocity((boulderVelocity * currentAim) + new Vector3(0, 3, 0));
            currentBoulder = null;
            usingAbility1 = false;
        }

        protected override void ReleaseAbility1()
        {
            TargetExitAbility1();

            ability1Down = false;


            CastedAbility1();
        }

        [TargetRpc]
        void TargetExitAbility1()
        {
            GetCameraContext().RemoveOffset(new Vector3(0, 3.0F, 0));

            SetRotationTrackCamera(false);

            //GetOwningPlayer().GetUI().HideReticle();
        }
        #endregion




        #region Ability 2
        [Server]
        protected override void Ability2()
        {
            galvanicStrikesRemaining = galvanicStrikeCharges;
        }

        public void GalvanicStrikeRight()
        {
            if (galvanicStrikesRemaining > 0)
            {
                if (isServer)
                {
                    rightGalvanicStrikeHitbox.HitOnce(galvanicStrikeDamage, Element.Storm, this, hitType: HitType.Ability);
                    galvanicStrikesRemaining = galvanicStrikesRemaining - 1;
                }

                if (isClient || !isServerOnly)
                {
                    StartCoroutine(PlayPunchEffect(rightPunchEffect));
                }
            }
        }

        public void GalvanicStrikeLeft()
        {
            if (galvanicStrikesRemaining > 0)
            {
                if (isServer)
                {
                    leftGalvanicStrikeHitbox.HitOnce(galvanicStrikeDamage, Element.Storm, this, hitType: HitType.Ability);
                    galvanicStrikesRemaining = galvanicStrikesRemaining - 1;
                }

                if (isClient || !isServerOnly)
                {
                    StartCoroutine(PlayPunchEffect(leftPunchEffect));
                }
            }
        }

        IEnumerator PlayPunchEffect(VisualEffect punchEffect)
        {
            punchEffect.enabled = false;
            punchEffect.enabled = true;
            punchEffect.Stop();
            punchEffect.Reinit();

            punchEffect.SendEvent("Play");

            yield return new WaitForSeconds(0.1F);

            punchEffect.Stop();
        }
        #endregion

        #region MISC
        public override float GetAimAssistMaxDistance()
        {
            return MELEE_AIM_ASSIST_MAX_DEFAULT;
        }
        
        #endregion
    }
}
