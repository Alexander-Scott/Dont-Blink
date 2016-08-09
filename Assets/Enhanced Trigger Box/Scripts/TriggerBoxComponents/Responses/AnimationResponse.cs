﻿using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class AnimationResponse : EnhancedTriggerBoxComponent
{
    /// <summary>
    /// The gameobject to apply the animation to
    /// </summary>
    public GameObject animationTarget;

    /// <summary>
    /// The name of the trigger in the animator that you want to trigger.
    /// </summary>
    public string setMecanimTrigger;

    /// <summary>
    /// Fades the animation in on the animation target over 0.3 seconds and fades other animations out.
    /// </summary>
    public bool stopAnim;

    /// <summary>
    /// The animation clip to play
    /// </summary>
    public AnimationClip playLegacyAnimation;

    public override void DrawInspectorGUI()
    {
        animationTarget = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Animation Target",
            "The gameobject to apply the animation to."), animationTarget, typeof(GameObject), true);

        setMecanimTrigger = EditorGUILayout.TextField(new GUIContent("Set Mecanim Trigger",
            "The name of the trigger in the animator that you want to trigger."), setMecanimTrigger);

        stopAnim = EditorGUILayout.Toggle(new GUIContent("Stop Animation",
            "Stops the current animation on the animation target."), stopAnim);

        playLegacyAnimation = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Play Animation Clip",
            "Fades the animation in on the animation target over 0.3 seconds and fades other animations out."),
            playLegacyAnimation, typeof(AnimationClip), true);
    }

    public override void Validation()
    {
        // If there is a mecanim trigger check there is a target for it
        if (!string.IsNullOrEmpty((setMecanimTrigger)))
        {
            if (animationTarget == null)
            {
                ShowErrorMessage("You have set a Mecanim Trigger as an Animation Response but haven't set an Animation Target to apply it to!");
            }
        }

        // If stop anim is set check there is a target for it
        if (stopAnim)
        {
            if (animationTarget == null)
            {
                ShowErrorMessage("You have set Stop Animation as an Animation Response but haven't set an Animation Target to apply it to!");
            }
        }

        // If legacy animat is set to play check there is a target for it
        if (playLegacyAnimation != null)
        {
            if (animationTarget == null)
            {
                ShowErrorMessage("You have chosen to play a legacy animation as an Animation Response but haven't set an Animation Target to apply it to!");
            }
        }
    }

    public override bool ExecuteAction()
    {
        if (stopAnim && animationTarget)
        {
            animationTarget.GetComponent<Animation>().Stop();
        }

        if (playLegacyAnimation && animationTarget)
        {
            // Plays an animation clip on the target animation over 0.3 seconds and fades other animations out
            animationTarget.GetComponent<Animation>().CrossFade(playLegacyAnimation.name, 0.3f, PlayMode.StopAll);
        }

        if (!string.IsNullOrEmpty(setMecanimTrigger))
        {
            animationTarget.GetComponent<Animator>().SetTrigger(setMecanimTrigger);
        }

        return true;
    }
}