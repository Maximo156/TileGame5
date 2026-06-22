using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace ComposableBlocks
{
    public class StructureBlockBehaviour : BlockBehaviour, IStatefulBlockBehaviour, IInterfaceBlockBehaviour
    {
        public BlockBehaviourState GetState(Block baseBlock)
        {
            return new StructureBehaviourState();
        }
    }

    public class StructureBehaviourState : BlockBehaviourState
    { 
        LineRenderer _boundsGameObject;
        LineRenderer BoundsGameObject { get
            {
                if (_boundsGameObject == null)
                {
                    _boundsGameObject = new GameObject("Bounds Game Object", typeof(LineRenderer)).GetComponent<LineRenderer>();
                    _boundsGameObject.startColor = Color.red;
                    _boundsGameObject.endColor = Color.red;
                    _boundsGameObject.positionCount = 4;
                    _boundsGameObject.loop = true;
                    _boundsGameObject.useWorldSpace = false;
                    _boundsGameObject.widthMultiplier = 0.05f;
                    _boundsGameObject.material = new Material(Shader.Find("Sprites/Default"));
                    _boundsGameObject.sortingOrder = 1000;
                    Render();
                }
                return _boundsGameObject;
            } 
        }

        [JsonProperty]
        Vector2Int _size = new Vector2Int(5, 5);
        [JsonProperty]
        string _fileSaveLocation;

        [JsonIgnore]
        public Vector2Int Size
        {
            get => _size;
            set
            {
                _size = value;
                Render();
                TriggerStateChange();
            }
        }

        [JsonIgnore]
        public string FileSaveLocation
        {
            get => _fileSaveLocation;
            set
            {
                _fileSaveLocation = value;
                TriggerStateChange();
            }
        }
        private Vector2Int boundsPos;

        public override void CleanUp(Vector2Int worldPos)
        {
            Object.Destroy(BoundsGameObject.gameObject);
        }

        public void SetPos(Vector2Int pos)
        {
            boundsPos = pos + Vector2Int.up;
            BoundsGameObject.transform.position = boundsPos.ToVector3Int();
        }

        public void ToggleVisuals(bool enabled)
        {
            BoundsGameObject.gameObject.SetActive(enabled);
        }

        public bool VisualShown()
        {
            return BoundsGameObject.gameObject.activeSelf;
        }

        public void Render()
        {
            BoundsGameObject.SetPositions(new Vector3[]
            {
                new Vector3(0,0,0),
                new Vector3(_size.x,0,0),
                new Vector3(_size.x,_size.y,0),
                new Vector3(0,_size.y,0),
            });
        }

        public BoundsInt GetBounds()
        {
            return new BoundsInt(boundsPos.ToVector3Int(), _size.ToVector3Int(1));
        }
    }
}
