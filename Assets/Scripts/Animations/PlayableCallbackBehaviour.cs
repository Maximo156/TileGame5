using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

// A behaviour that is attached to a playable
public class PlayableCallbackBehaviour : PlayableBehaviour
{
    public AnimationClipPlayable clip;

    Action callBack;
    bool completed = true;
    // Called each frame while the state is set to Play
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (completed) return;

        if (clip.GetTime() >= clip.GetAnimationClip().length)
        {
            completed = true;
            callBack?.Invoke();
        }
    }

    public void Setup(Action callBack)
    {
        this.callBack = callBack;
        completed = false;

        //Double set time is needed for some reason to trigger animation events in correct order
        clip.SetTime(0);
        clip.SetTime(0);
    }
}
