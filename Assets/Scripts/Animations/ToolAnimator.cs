using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ToolAnimator : CallbackAnimator<Item>
{
    Dictionary<Item, AnimationInfo> animationInfo = new Dictionary<Item, AnimationInfo>();

    AnimationInfo defaultInfo;

    public void Init(ToolDisplay.ItemDisplayInfo defaultDisplayInfo, List<ToolDisplay.ItemDisplayInfo> displayInfo)
    {
        var count = displayInfo.Count;
        SetupGraph(count + 2);

        SetupDefault(defaultDisplayInfo);
        for (int i = 0; i < count; i++)
        {
            var info = displayInfo[i];
            SetupInfo(info, i+2);
        }
    }

    void SetupInfo(ToolDisplay.ItemDisplayInfo info, int index)
    {
        var scriptPlayable = MakeBehaviours(info);
        topLevelMixer.ConnectInput(index, scriptPlayable, 0);
        animationInfo.Add(info.item, new() { index = index, callbackPlayable = scriptPlayable });
    }

    void SetupDefault(ToolDisplay.ItemDisplayInfo info)
    {
        var scriptPlayable = MakeBehaviours(info);
        topLevelMixer.ConnectInput(1, scriptPlayable, 0);
        defaultInfo = new() { index = 1, callbackPlayable = scriptPlayable };
    }

    ScriptPlayable<PlayableCallbackBehaviour> MakeBehaviours(ToolDisplay.ItemDisplayInfo info)
    {
        var animPlayable = AnimationClipPlayable.Create(playableGraph, info.animation);
        var scriptPlayable = ScriptPlayable<PlayableCallbackBehaviour>.Create(playableGraph);
        scriptPlayable.GetBehaviour().clip = animPlayable;
        scriptPlayable.AddInput(animPlayable, 0, 1f);
        animPlayable.SetSpeed(info.AnimationSpeed);
        return scriptPlayable;
    }

    protected override AnimationInfo GetAnimationInfo(Item type)
    {
        if(animationInfo.TryGetValue(type, out var info))
        {
            return info;
        }
        return defaultInfo;
    }
}
