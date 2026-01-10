using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class MobAnimator : CallbackAnimator<string>, IAnimationClipSource
{
    [Serializable]
    public class MobAnimationInfo
    {
        public string name;
        public AnimationClip clip;
    }

    public SpriteRenderer sprite;

    public AnimationClip Idle;
    public AnimationClip Walk;

    public List<MobAnimationInfo> additionalAnimations;

    AnimationMixerPlayable locomotionMixer;

    Dictionary<string, List<AnimationInfo>> mobAnimationCache = new Dictionary<string, List<AnimationInfo>>();

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        var additionalCount = additionalAnimations.Count;
        SetupGraph(1+ additionalCount);
        

        for(int i = 0; i < additionalCount; i++)
        {
            var info = additionalAnimations[i];
            var animPlayable = AnimationClipPlayable.Create(playableGraph, info.clip); 
            var scriptPlayable = ScriptPlayable<PlayableCallbackBehaviour>.Create(playableGraph);
            scriptPlayable.GetBehaviour().clip = animPlayable;
            scriptPlayable.AddInput(animPlayable, 0, 1f);
            topLevelMixer.ConnectInput(i + 1, scriptPlayable, 0);
            if (!mobAnimationCache.TryGetValue(info.name, out var clips))
            {
                clips = new List<AnimationInfo>();
                mobAnimationCache.Add(info.name, clips);
            }
            clips.Add(new() { index = i + 1, callbackPlayable = scriptPlayable});
        }

        locomotionMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        topLevelMixer.ConnectInput(0, locomotionMixer, 0);
        topLevelMixer.SetInputWeight(0, 1);

        var idlePlayable = AnimationClipPlayable.Create(playableGraph, Idle);
        var walkPlayable = AnimationClipPlayable.Create(playableGraph, Walk);

        idlePlayable.GetAnimationClip().wrapMode = WrapMode.Loop;
        walkPlayable.GetAnimationClip().wrapMode = WrapMode.Loop;

        locomotionMixer.ConnectInput(0, idlePlayable, 0);
        locomotionMixer.ConnectInput(1, walkPlayable, 0);

        locomotionMixer.SetInputWeight(0, 0);
        locomotionMixer.SetInputWeight(1, 1);
    }

    public void UpdateLocomotion(Vector2 velocity)
    {
        sprite.flipX = velocity.x < 0 || (velocity.magnitude == 0 && sprite.flipX);
        var idleWeight = velocity.magnitude == 0 ? 0 : 1;

        locomotionMixer.SetInputWeight(0, idleWeight);
        locomotionMixer.SetInputWeight(1, 1-idleWeight);
    }

    public void GetAnimationClips(List<AnimationClip> results)
    {
        results.Add(Idle);
        results.Add(Walk);
        foreach (var anim in additionalAnimations)
        {
            results.Add(anim.clip);
        }
    }

    protected override AnimationInfo GetAnimationInfo(string type)
    {
        if(!mobAnimationCache.TryGetValue(type, out var info))
        {
            throw new Exception($"Animation {type} not found for mob {name}");
        }
        return info.SelectRandom();
    }
}
