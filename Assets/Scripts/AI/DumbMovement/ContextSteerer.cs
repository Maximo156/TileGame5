using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ContextSteerer : MonoBehaviour
{
    public int DirectionCount = 8;

    [Header("Debug")]
    public bool showWeights;
    public bool logWeights;

    Vector2[] _directons;
    Vector2[] Directions
    {
        get
        {
            if (_directons == null || _directons.Length != DirectionCount)
            {
                _directons = GenDirections();
            }
            return _directons;
        }
    }


    Vector2[] WeightedDirections = new Vector2[0];
    float[] lastDangers = new float[0];
    float[] lastInterests = new float[0];

    public Vector2 Direction { get; private set; }

    List<SteeringBehavior> Behaviors;

    private void Start()
    {
        Behaviors = GetComponents<SteeringBehavior>().ToList();
    }

    public void UpdateWeights()
    {
        var results = new float[DirectionCount];
        lastDangers = new float[DirectionCount];
        lastInterests = new float[DirectionCount];

        //Loop through each behaviour
        foreach (var behaviour in Behaviors)
        {
            (lastInterests, lastDangers) =  behaviour.GetWeights(Directions, lastInterests, lastDangers, this);
        }

        for (int i = 0; i < DirectionCount; i++)
        {
            results[i] = Mathf.Clamp01(lastInterests[i] - lastDangers[i]);
        }

        WeightedDirections = results.Select((w, i) => w / Behaviors.Count * Directions[i]).ToArray();

        if (logWeights)
        {
            print("Results:");
            printWeights(results);
            print("Interests:");
            printWeights(lastInterests);
            print("Dangers:");
            printWeights(lastDangers);
            print("----------------------");
        }

        Direction = WeightedDirections.Aggregate((total, next) => total + next).normalized;
    }

    Vector2[] GenDirections()
    {
        Vector2[] directions = new Vector2[DirectionCount];
        var dir = Vector2.up;
        for(int i = 0; i < DirectionCount; i++)
        {
            directions[i] = dir;
            dir = (Quaternion.Euler(0, 0, 360/DirectionCount) * dir).normalized;
        }
        return directions;
    }

    private void OnDrawGizmos()
    {
        if (showWeights && Application.isPlaying)
        {
            for(int i = 0; i<DirectionCount; i++)
            {
                Gizmos.color = Color.yellow;
                var dir = Directions[i] *  Mathf.Abs(lastInterests[i]);
                Gizmos.DrawRay(transform.position, dir);


                Gizmos.color = Color.red;
                dir = Directions[i] * Mathf.Abs(lastDangers[i]);
                Gizmos.DrawRay(transform.position, dir);
            }
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Direction * 1.5f);
        }
    }

    void printWeights(IEnumerable<float> weights)
    {
        print($"{string.Join(", ", weights)}");
    }
}
