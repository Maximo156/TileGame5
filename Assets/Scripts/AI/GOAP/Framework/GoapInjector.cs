using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using System;
using UnityEngine;

namespace Goap
{
    interface IInjectable
    {
        public void Inject(GoapInjector injector);
    }

    public class GoapInjector : MonoBehaviour, IGoapInjector
    {
        public void Inject(IAction action)
        {
            if (action is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }

        public void Inject(IGoal goal)
        {
            if (goal is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }

        public void Inject(ISensor sensor)
        {
            if (sensor is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }

        public void Inject(IAgentTypeFactory factory)
        {
            if (factory is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }

        public void Inject(ICapabilityFactory factory)
        {
            if (factory is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }
    }
}
