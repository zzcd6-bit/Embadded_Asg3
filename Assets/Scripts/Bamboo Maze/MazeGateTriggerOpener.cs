using UnityEngine;

public class MazeGateTriggerOpener : MonoBehaviour
{
    [SerializeField] private MazeGate gate;
    [SerializeField] private MazeGraphController graph;
    [SerializeField] private bool openOnTriggerEnter = true;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool logOpen = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!openOnTriggerEnter || gate == null || gate.IsOpen)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        OpenFromPosition(other.transform.position, other);
    }

    public bool OpenFromPosition(Vector3 openerPosition, Object opener = null)
    {
        ResolveReferences();

        if (gate == null || gate.IsOpen)
            return false;

        MazeNode openerSideNode = GetOpenerSideNode(openerPosition);
        MazeNode moveNode = gate.ownerNode != null ? gate.ownerNode : openerSideNode;

        LogOpenRequest(opener, openerSideNode, moveNode);

        if (graph != null)
            return graph.TryMoveGate(gate, moveNode);

        gate.SetOpen();
        return true;
    }

    public bool OpenFromTransform(Transform opener)
    {
        Vector3 openerPosition = opener != null ? opener.position : transform.position;
        return OpenFromPosition(openerPosition, opener);
    }

    private void ResolveReferences()
    {
        if (gate == null)
            gate = GetComponentInParent<MazeGate>();

        if (graph == null)
            graph = GetComponentInParent<MazeGraphController>();
    }

    private MazeNode GetOpenerSideNode(Vector3 openerPosition)
    {
        MazeNode openerSideNode = gate.GetNodeForPosition(openerPosition);
        if (openerSideNode == null || !openerSideNode.HasGate(gate))
            openerSideNode = gate.GetNearestSideNode(openerPosition);

        return openerSideNode;
    }

    private void LogOpenRequest(Object opener, MazeNode openerSideNode, MazeNode moveNode)
    {
        if (!logOpen)
            return;

        string openerName = opener != null ? opener.name : "Unknown";
        string openerNodeName = openerSideNode != null ? openerSideNode.name : "Unknown";
        string moveNodeName = moveNode != null ? moveNode.name : "Unknown";
        string edgeStates = moveNode != null
            ? moveNode.DescribeConnectedEdgeStates()
            : "No node found.";

        Debug.Log(
            $"[MazeGateTrigger] {openerName} opens {gate.name} from side node {openerNodeName}. Move node: {moveNodeName}. Connected edges: {edgeStates}",
            this);
    }
}
