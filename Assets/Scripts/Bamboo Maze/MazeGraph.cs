using System.Collections.Generic;
using UnityEngine;

public class MazeGraphController : MonoBehaviour
{
    [Header("Debug")]
    public int maxMoveChainLength = 16;
    [SerializeField] private bool logGateMoves = true;

    [Header("Node Fog Zone Tool")]
    [Min(0.1f)] public float defaultFogZoneRadius = 6f;
    [Range(0f, 1f)] public float defaultFogZoneDensity = 0.75f;
    [Min(0f)] public float defaultFogZoneTransitionDuration = 2f;
    public int defaultFogZonePriority;

    private readonly List<GateMoveStep> moveSteps = new();
    private readonly List<MazeGate> visitedGates = new();

    public void ResetAllGatesToGraphStateInstant()
    {
        MazeGate[] gates = GetComponentsInChildren<MazeGate>(true);
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] != null)
                gates[i].ResetToGraphStateInstant();
        }

        Debug.Log($"[MazeGraph] Reset {gates.Length} gate(s) to graph edge states.", this);
    }

    public void MoveGate(MazeGate startGate)
    {
        TryMoveGate(startGate, startGate != null ? startGate.ownerNode : null);
    }

    public void MoveGate(MazeGate startGate, Transform player)
    {
        if (startGate == null || player == null)
            return;

        TryMoveGate(startGate, GetPlayerSideNode(startGate, player.position));
    }

    public void MoveGate(MazeGate startGate, Vector3 playerPosition)
    {
        if (startGate == null)
            return;

        TryMoveGate(startGate, GetPlayerSideNode(startGate, playerPosition));
    }

    public void MoveGate(MazeGate startGate, MazeNode activeNode)
    {
        TryMoveGate(startGate, activeNode);
    }

    public bool TryOpenPath(MazeNode fromNode, MazeNode toNode)
    {
        Debug.Log(
            $"[MazeGraph] TryOpenPath called. From: {(fromNode != null ? fromNode.name : "None")}, To: {(toNode != null ? toNode.name : "None")}.",
            this);

        if (fromNode == null || toNode == null)
        {
            Debug.LogWarning("[MazeGraph] TryOpenPath failed because fromNode or toNode is null.", this);
            return false;
        }

        MazeNeighborLink link = fromNode.GetLinkTo(toNode);
        if (link == null)
        {
            LogOpenPathFailed(fromNode, toNode, "No edge from source node to target node.");
            return false;
        }

        if (link.edgeType == MazeEdgeType.BidirectionalAlwaysOpen || link.edgeType == MazeEdgeType.OneWay)
        {
            LogOpenPathAlreadyAvailable(fromNode, toNode, link);
            return true;
        }

        if (!link.HasMovableDoor || link.gate == null)
        {
            LogOpenPathFailed(fromNode, toNode, "Edge has no movable gate.");
            return false;
        }

        if (link.gate.IsOpen)
        {
            LogOpenPathAlreadyAvailable(fromNode, toNode, link);
            link.gate.SetOpen();
            return true;
        }

        bool moved = TryMoveGate(link.gate, fromNode);
        Debug.Log(
            $"[MazeGraph] TryOpenPath finished. Gate: {link.gate.name}, moved: {moved}, gate state: {link.gate.state}.",
            this);
        return moved;
    }

    public bool TryMoveGate(MazeGate startGate, MazeNode activeNode)
    {
        if (!CanStartMove(startGate, activeNode))
        {
            Debug.LogWarning(
                $"[MazeGraph] TryMoveGate blocked by CanStartMove. Gate: {(startGate != null ? startGate.name : "None")}, Active node: {(activeNode != null ? activeNode.name : "None")}.",
                this);
            return false;
        }

        LogMoveRequest(startGate, activeNode);

        if (!BuildMoveSteps(startGate, activeNode))
        {
            LogMoveFailed(startGate, activeNode);
            return false;
        }

        ApplyMoveSteps(activeNode);
        return true;
    }

    private bool CanStartMove(MazeGate startGate, MazeNode activeNode)
    {
        if (startGate == null || activeNode == null)
            return false;

        bool canStart = startGate.HasMovableDoor && activeNode.HasGate(startGate);
        Debug.Log(
            $"[MazeGraph] CanStartMove = {canStart}. Gate: {startGate.name}, HasMovableDoor: {startGate.HasMovableDoor}, Active node has gate: {activeNode.HasGate(startGate)}.",
            this);
        return canStart;
    }

    private MazeNode GetPlayerSideNode(MazeGate startGate, Vector3 playerPosition)
    {
        MazeNode playerSideNode = startGate.GetNodeForPosition(playerPosition);

        if (playerSideNode == null || !playerSideNode.HasGate(startGate))
            playerSideNode = startGate.GetNearestSideNode(playerPosition);

        return playerSideNode;
    }

    private void ApplyMoveSteps(MazeNode activeNode)
    {
        for (int i = moveSteps.Count - 1; i >= 0; i--)
        {
            GateMoveStep step = moveSteps[i];
            step.fromGate.SetOpen();
            step.toGate.SetClosed();

            if (logGateMoves)
            {
                Debug.Log(
                    $"[MazeGraph] Move step on node {activeNode.name}: open {step.fromGate.name}, close {step.toGate.name}.",
                    this);
            }
        }

        LogNodeEdgeStates(activeNode, "After move");
    }

    private bool BuildMoveSteps(MazeGate startGate, MazeNode activeNode)
    {
        moveSteps.Clear();
        visitedGates.Clear();

        MazeGate currentGate = startGate;

        for (int i = 0; i < maxMoveChainLength; i++)
        {
            if (currentGate == null || currentGate.IsOpen || !currentGate.HasMovableDoor)
                return false;

            if (visitedGates.Contains(currentGate))
                return false;

            visitedGates.Add(currentGate);

            MazeGate targetGate = activeNode.GetNextGate(currentGate);
            if (targetGate == null || targetGate == currentGate || !targetGate.HasMovableDoor)
                return false;

            moveSteps.Add(new GateMoveStep(currentGate, targetGate));

            if (targetGate.IsOpen)
                return true;

            currentGate = targetGate;
        }

        return false;
    }

    private void LogMoveRequest(MazeGate startGate, MazeNode activeNode)
    {
        if (!logGateMoves)
            return;

        Debug.Log(
            $"[MazeGraph] Active node {activeNode.name} requests gate move from {startGate.name}. Before move: {activeNode.DescribeConnectedEdgeStates()}",
            this);
    }

    private void LogMoveFailed(MazeGate startGate, MazeNode activeNode)
    {
        if (!logGateMoves)
            return;

        Debug.LogWarning(
            $"[MazeGraph] Gate move failed. Active node: {activeNode.name}, start gate: {startGate.name}. Current edges: {activeNode.DescribeConnectedEdgeStates()}",
            this);
    }

    private void LogOpenPathAlreadyAvailable(MazeNode fromNode, MazeNode toNode, MazeNeighborLink link)
    {
        if (!logGateMoves)
            return;

        string gateText = link.gate != null ? $", gate: {link.gate.name}, state: {link.gate.state}" : string.Empty;
        Debug.Log(
            $"[MazeGraph] Path already open from {fromNode.name} to {toNode.name}. Edge: {link.edgeType}{gateText}. Source edges: {fromNode.DescribeConnectedEdgeStates()}",
            this);
    }

    private void LogOpenPathFailed(MazeNode fromNode, MazeNode toNode, string reason)
    {
        if (!logGateMoves)
            return;

        Debug.LogWarning(
            $"[MazeGraph] Open path failed from {fromNode.name} to {toNode.name}. {reason} Source edges: {fromNode.DescribeConnectedEdgeStates()}",
            this);
    }

    private void LogNodeEdgeStates(MazeNode activeNode, string prefix)
    {
        if (!logGateMoves)
            return;

        Debug.Log($"[MazeGraph] {prefix} on node {activeNode.name}: {activeNode.DescribeConnectedEdgeStates()}", this);
    }

    private struct GateMoveStep
    {
        public MazeGate fromGate;
        public MazeGate toGate;

        public GateMoveStep(MazeGate fromGate, MazeGate toGate)
        {
            this.fromGate = fromGate;
            this.toGate = toGate;
        }
    }
}
