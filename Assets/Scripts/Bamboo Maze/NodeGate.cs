using UnityEngine;

public enum GateState
{
    Open,
    Closed
}

public enum MazeEdgeType
{
    BidirectionalDoorClosed,
    BidirectionalDoorOpen,
    BidirectionalAlwaysOpen,
    OneWay
}

public class MazeGate : MonoBehaviour
{
    [Header("Graph")]
    public MazeNode ownerNode;
    public int gateIndex;
    public MazeNode nodeA;
    public MazeNode nodeB;
    public MazeNode frontSideNode;
    public MazeNode backSideNode;
    public MazeEdgeType edgeType = MazeEdgeType.BidirectionalDoorClosed;

    [Header("State")]
    public GateState state = GateState.Open;
    [SerializeField] private bool logStateChanges = true;

    [Header("Wall Colliders")]
    [SerializeField] private Collider[] wallColliders;

    public bool IsOpen => state == GateState.Open;
    public bool IsClosed => state == GateState.Closed;
    public bool HasMovableDoor => edgeType == MazeEdgeType.BidirectionalDoorClosed ||
                                  edgeType == MazeEdgeType.BidirectionalDoorOpen;

    private void Awake()
    {
        CacheWallCollidersIfNeeded();
        RefreshStateFromEdgeType(false);
    }

    private void OnValidate()
    {
        CacheWallCollidersIfNeeded();
        RefreshStateFromEdgeType(false);
    }

    public void ConfigureEdge(MazeNode firstNode, MazeNode secondNode, MazeEdgeType type)
    {
        nodeA = firstNode;
        nodeB = secondNode;
        ownerNode = firstNode;
        edgeType = type;

        backSideNode = firstNode;
        frontSideNode = secondNode;

        RefreshStateFromEdgeType(false);
    }

    public MazeNode GetNodeForPosition(Vector3 worldPosition)
    {
        if (frontSideNode == null && backSideNode == null)
            return ownerNode;

        if (frontSideNode == null)
            return backSideNode;

        if (backSideNode == null)
            return frontSideNode;

        Vector3 toPosition = worldPosition - transform.position;
        float side = Vector3.Dot(transform.forward, toPosition);
        return side >= 0f ? frontSideNode : backSideNode;
    }

    public MazeNode GetNearestSideNode(Vector3 worldPosition)
    {
        if (frontSideNode == null && backSideNode == null)
            return ownerNode;

        if (frontSideNode == null)
            return backSideNode;

        if (backSideNode == null)
            return frontSideNode;

        float frontDistance = Vector3.SqrMagnitude(frontSideNode.transform.position - worldPosition);
        float backDistance = Vector3.SqrMagnitude(backSideNode.transform.position - worldPosition);
        return frontDistance <= backDistance ? frontSideNode : backSideNode;
    }

    public void SetOpen(bool notifyView = true)
    {
        GateState previousState = state;
        state = GateState.Open;
        Debug.Log($"[MazeGate] SetOpen called on {name}. Previous: {previousState}, notifyView: {notifyView}.", this);
        ApplyState(notifyView);
        LogStateChange(previousState);
    }

    public void SetClosed(bool notifyView = true)
    {
        GateState previousState = state;
        state = GateState.Closed;
        Debug.Log($"[MazeGate] SetClosed called on {name}. Previous: {previousState}, notifyView: {notifyView}.", this);
        ApplyState(notifyView);
        LogStateChange(previousState);
    }

    public void ResetClosedInstant()
    {
        GateState previousState = state;
        state = GateState.Closed;
        Debug.Log($"[MazeGate] ResetClosedInstant called on {name}. Previous: {previousState}.", this);
        ApplyState(true, true);
    }

    public void ResetOpenInstant()
    {
        GateState previousState = state;
        state = GateState.Open;
        Debug.Log($"[MazeGate] ResetOpenInstant called on {name}. Previous: {previousState}.", this);
        ApplyState(true, true);
    }

    public void ResetToGraphStateInstant()
    {
        Debug.Log($"[MazeGate] ResetToGraphStateInstant called on {name}. Edge type: {edgeType}.", this);
        state = edgeType == MazeEdgeType.BidirectionalDoorClosed
            ? GateState.Closed
            : GateState.Open;

        ApplyState(true, true);
    }

    public void RefreshStateFromEdgeType(bool notifyView = false)
    {
        state = edgeType == MazeEdgeType.BidirectionalDoorClosed
            ? GateState.Closed
            : GateState.Open;

        ApplyState(notifyView);
    }

    private void ApplyState(bool notifyView, bool instantView = false)
    {
        bool wallEnabled = IsClosed;

        if (wallColliders != null)
        {
            for (int i = 0; i < wallColliders.Length; i++)
            {
                if (wallColliders[i] != null)
                    wallColliders[i].enabled = wallEnabled;
            }
        }

        if (notifyView)
        {
            string message = IsOpen
                ? (instantView ? "ResetBamboosOpenInstant" : "PlayOpen")
                : (instantView ? "ResetBamboosClosedInstant" : "PlayClose");

            Debug.Log($"[MazeGate] Sending view message {message} on {name}.", this);
            MazeGateBambooView[] views = GetComponents<MazeGateBambooView>();
            if (views.Length == 0)
            {
                SendMessage(message, SendMessageOptions.DontRequireReceiver);
                return;
            }

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                    continue;

                if (IsOpen)
                {
                    if (instantView)
                        views[i].ResetBamboosOpenInstant();
                    else
                        views[i].PlayOpen();
                }
                else
                {
                    if (instantView)
                        views[i].ResetBamboosClosedInstant();
                    else
                        views[i].PlayClose();
                }
            }
        }
    }

    private void LogStateChange(GateState previousState)
    {
        if (!logStateChanges || !Application.isPlaying || previousState == state)
            return;

        string nodeAName = nodeA != null ? nodeA.name : "None";
        string nodeBName = nodeB != null ? nodeB.name : "None";
        Debug.Log($"[MazeGate] {name}: {previousState} -> {state}. Nodes: {nodeAName} <-> {nodeBName}.", this);
    }

    private void CacheWallCollidersIfNeeded()
    {
        if (wallColliders != null && wallColliders.Length > 0)
            return;

        Transform wallColliderRoot = transform.Find("Wall Collider");
        if (wallColliderRoot == null)
            return;

        wallColliders = wallColliderRoot.GetComponentsInChildren<Collider>(true);
    }
}
