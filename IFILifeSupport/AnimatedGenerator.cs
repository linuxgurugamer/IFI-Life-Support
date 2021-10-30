#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;

/// <summary>
/// This was lifted from the BioMass mod to support the parts copied from BioMass
/// </summary>
namespace IFILifeSupport
{

    /// <summary>
    /// ModuleResourceConverter w/ Animations
    /// Adds animation functionality to the ModuleResourceConverter for parts that require an animation as well as converter/generator functionality.
    /// </summary>
    public class AnimatedGenerator : ModuleIFILifeSupport
    {
        /// The name of the animation.
        [KSPField] // for setting animation name via cfg file
        public string AnimationName;

        [KSPField]
        public float animationLength = -1;
        // amount of animation change per tic, calculated in Start
        float animationTicAmount;

        /// Gets the bio animate.
        Animation BioAnimate;
        AnimationState[] animStates;

        bool useAnimation = false;



        public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                Log.Info("SetupAnimation, anim found");
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.Loop;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }


        public void Start()
        {
            Log.Info("AnimatedGenerator.Start, part: " + part.partInfo.name);
            
            if (AnimationName == null)
            {
                Log.Info("AnimationName is null");
                return;
            }
            var ma = part.FindModelAnimators(AnimationName);
            if (ma == null)
                return;
            if (ma.Count() == 0)
                return;
            BioAnimate = ma[0];
            if (BioAnimate == null)
                return;
            Log.Info("animationLength: " + animationLength + ",   BioAnimate[AnimationName].length: " + BioAnimate[AnimationName].length);
            if (animationLength == -1)
                animationLength = BioAnimate[AnimationName].length;
            animationTicAmount = 1 / (animationLength * 20);
            
            animStates = SetUpAnimation(AnimationName, this.part);
            Log.Info("animStates.Length: " + animStates.Count());
            if (animStates.Count() > 0)
                useAnimation = true;

        }


        public new void FixedUpdate()
        {
            if (useAnimation)
            {
                Log.Info("AnimatedGenerator.FixedUpdate, IsActivated: " + ModuleIsActive() + ",   normalizedTime: " + animStates[0]);

                if (ModuleIsActive() && animStates[0].normalizedTime < 1)
                {
                    Log.Info("Artificial Lights Activated");
                    for (int i = 0; i < animStates.Count(); i++)
                        animStates[i].normalizedTime += animationTicAmount;
                }
                else
                    animStates[0].normalizedTime = 0;
                /*
                if (!ModuleIsActive() && animStates[0].normalizedTime > 0)
                {
                    Log.Info("Deactivated");
                    for (int i = 0; i < animStates.Count(); i++)
                        animStates[i].normalizedTime -= animationTicAmount;

                }
                */
            }
            base.FixedUpdate();
        }
    }//END AnimatedGenerator
}

#endif