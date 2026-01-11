using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public abstract class CallbackAnimator<TType> : MonoBehaviour where TType : class
{
    TType curAnim { get; set; } = null;
    public bool isPlaying => curAnim != null;

    protected class AnimationInfo
    {
        public int index;
        public ScriptPlayable<PlayableCallbackBehaviour> callbackPlayable;
    }

    protected PlayableGraph playableGraph;
    protected AnimationMixerPlayable topLevelMixer;

    protected virtual void SetupGraph(int totalAnimCount)
    {
        var animator = GetComponent<Animator>();
        playableGraph = PlayableGraph.Create("MobAnimation");

        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

        topLevelMixer = AnimationMixerPlayable.Create(playableGraph, totalAnimCount);
        output.SetSourcePlayable(topLevelMixer);
        playableGraph.GetRootPlayable(0).SetInputWeight(0, 1);
        playableGraph.Play();
    }

    protected abstract AnimationInfo GetAnimationInfo(TType type);

    public void PlayAnimation(TType animId, Action callback = null)
    {
        if (curAnim == animId) return;
        InteruptAnim();
        var animInfo = GetAnimationInfo(animId);
        curAnim = animId;
        animInfo.callbackPlayable.GetBehaviour().Setup(AwaitAnimCallback(callback, animInfo.index));

        topLevelMixer.SetInputWeight(0, 0);
        topLevelMixer.SetInputWeight(animInfo.index, 1);
    }

    Action AwaitAnimCallback(Action callback, int animIndex)
    {
        var token = NextToken();
        return () =>
        {
            if (token.Version != animationVersion) return;
            topLevelMixer.SetInputWeight(0, 1);
            topLevelMixer.SetInputWeight(animIndex, 0);
            curAnim = null;
            callback?.Invoke();
        };
    }

    public void InteruptAnim()
    {
        topLevelMixer.SetInputWeight(0, 1);
        for (int i = 1; i < topLevelMixer.GetInputCount(); i++)
        {
            topLevelMixer.SetInputWeight(i, 0);
        }
        curAnim = null; 
        playableGraph.Evaluate(0);
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }

    int animationVersion;
    struct AnimationToken
    {
        public int Version;
    }
    AnimationToken NextToken()
    {
        animationVersion++;
        return new AnimationToken { Version = animationVersion };
    }
}
