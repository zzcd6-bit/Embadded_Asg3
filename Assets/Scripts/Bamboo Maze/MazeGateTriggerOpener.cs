using UnityEngine;

public class MazeGateTriggerOpener : MonoBehaviour
{
    [SerializeField] private MazeGate gate;
    [SerializeField] private MazeGraphController graph;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool logOpen = true;

    private void Awake()
    {
        if (gate == null)
            gate = GetComponentInParent<MazeGate>();

        if (graph == null)
            graph = GetComponentInParent<MazeGraphController>();

        if (graph == null)
            graph = FindAnyObjectByType<MazeGraphController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gate == null || gate.IsOpen)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        MazeNode openerSideNode = GetOpenerSideNode(other.transform.position);
        MazeNode moveNode = gate.ownerNode != null ? gate.ownerNode : openerSideNode;

        LogOpenRequest(other, openerSideNode, moveNode);

        if (graph != null)
            graph.MoveGate(gate, moveNode);
        else
            gate.SetOpen();
    }

    private MazeNode GetOpenerSideNode(Vector3 openerPosition)
    {
        MazeNode openerSideNode = gate.GetNodeForPosition(openerPosition);
        if (openerSideNode == null || !openerSideNode.HasGate(gate))
            openerSideNode = gate.GetNearestSideNode(openerPosition);

        return openerSideNode;
    }

    private void LogOpenRequest(Collider opener, MazeNode openerSideNode, MazeNode moveNode)
    {
        if (!logOpen)
            return;

        string openerNodeName = openerSideNode != null ? openerSideNode.name : "Unknown";
        string moveNodeName = moveNode != null ? moveNode.name : "Unknown";
        string edgeStates = moveNode != null
            ? moveNode.DescribeConnectedEdgeStates()
            : "No node found.";

        Debug.Log(
            $"[MazeGateTrigger] {opener.name} opens {gate.name} from player side node {openerNodeName}. Move node: {moveNodeName}. Connected edges: {edgeStates}",
            this);
    }
}
