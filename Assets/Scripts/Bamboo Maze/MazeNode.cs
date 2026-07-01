using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MazeNeighborLink
{
    public MazeNode neighbor;
    public MazeEdgeType edgeType = MazeEdgeType.BidirectionalDoorClosed;
    public MazeGate gate;

    public bool HasMovableDoor => edgeType == MazeEdgeType.BidirectionalDoorClosed ||
                                  edgeType == MazeEdgeType.BidirectionalDoorOpen;
}

public class MazeNode : MonoBehaviour
{
    [Header("Neighbor Edges")]
    public List<MazeNeighborLink> neighbors = new();

    [Header("Generated Gate Order")]
    public List<MazeGate> gates = new();

    public MazeGate GetNextGate(MazeGate currentGate)
    {
        if (gates == null || gates.Count == 0)
            return null;

        int currentIndex = gates.IndexOf(currentGate);

        if (currentIndex < 0)
        {
            Debug.LogWarning($"{currentGate.name} is not registered in {name}.");
            return null;
        }

        int nextIndex = (currentIndex + 1) % gates.Count;
        return gates[nextIndex];
    }

    public bool HasGate(MazeGate gate)
    {
        return gate != null && gates != null && gates.Contains(gate);
    }

    public MazeNeighborLink GetLinkTo(MazeNode neighbor)
    {
        if (neighbor == null || neighbors == null)
            return null;

        for (int i = 0; i < neighbors.Count; i++)
        {
            MazeNeighborLink link = neighbors[i];
            if (link != null && link.neighbor == neighbor)
                return link;
        }

        return null;
    }

    public MazeNeighborLink GetOrAddLinkTo(MazeNode neighbor, MazeEdgeType edgeType)
    {
        MazeNeighborLink link = GetLinkTo(neighbor);
        if (link != null)
        {
            link.edgeType = edgeType;
            return link;
        }

        link = new MazeNeighborLink
        {
            neighbor = neighbor,
            edgeType = edgeType
        };

        neighbors.Add(link);
        return link;
    }

    public string DescribeConnectedEdgeStates()
    {
        if (neighbors == null || neighbors.Count == 0)
            return "No connected edges.";

        StringBuilder builder = new();

        for (int i = 0; i < neighbors.Count; i++)
        {
            MazeNeighborLink link = neighbors[i];
            if (link == null)
                continue;

            if (builder.Length > 0)
                builder.Append("; ");

            string neighborName = link.neighbor != null ? link.neighbor.name : "None";
            builder.Append(neighborName);
            builder.Append(" [");
            builder.Append(link.edgeType);

            if (link.HasMovableDoor)
            {
                builder.Append(", ");
                builder.Append(link.gate != null ? link.gate.name : "No Gate");

                if (link.gate != null)
                {
                    builder.Append(", ");
                    builder.Append(link.gate.state);
                }
            }
            else if (link.edgeType == MazeEdgeType.BidirectionalAlwaysOpen)
            {
                builder.Append(", Open");
            }

            builder.Append(']');
        }

        return builder.Length > 0 ? builder.ToString() : "No connected edges.";
    }

    public void RefreshGeneratedGateList()
    {
        gates.Clear();

        if (neighbors == null)
            return;

        for (int i = 0; i < neighbors.Count; i++)
        {
            MazeNeighborLink link = neighbors[i];
            if (link == null || !link.HasMovableDoor || link.gate == null)
                continue;

            if (link.neighbor != null)
                link.gate.ConfigureEdge(this, link.neighbor, link.edgeType);

            if (!gates.Contains(link.gate))
                gates.Add(link.gate);
        }

        for (int i = 0; i < gates.Count; i++)
        {
            MazeGate gate = gates[i];
            if (gate == null)
                continue;

            gate.ownerNode = this;
            gate.gateIndex = i;
        }
    }

    private void OnValidate()
    {
        RefreshGeneratedGateList();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);

        DrawNeighborGizmos();
        DrawGateGizmos();
    }

    private void DrawNeighborGizmos()
    {
        if (neighbors == null)
            return;

        foreach (MazeNeighborLink link in neighbors)
        {
            if (link == null || link.neighbor == null)
                continue;

            Gizmos.color = GetEdgeColor(link);

            if (link.HasMovableDoor && link.gate != null)
            {
                Gizmos.DrawLine(transform.position, link.gate.transform.position);
                Gizmos.DrawLine(link.gate.transform.position, link.neighbor.transform.position);
            }
            else
            {
                Gizmos.DrawLine(transform.position, link.neighbor.transform.position);
            }

            if (link.edgeType == MazeEdgeType.OneWay)
            {
                Vector3 end = link.neighbor.transform.position;
                Vector3 start = transform.position;
                Vector3 direction = (end - start).normalized;
                Vector3 marker = Vector3.Lerp(start, end, 0.78f);
                Gizmos.DrawSphere(marker, 0.08f);
                Gizmos.DrawLine(marker, marker - direction * 0.25f + Vector3.up * 0.12f);
                Gizmos.DrawLine(marker, marker - direction * 0.25f - Vector3.up * 0.12f);
            }
        }
    }

    private void DrawGateGizmos()
    {
        if (gates == null)
            return;

        for (int i = 0; i < gates.Count; i++)
        {
            MazeGate gate = gates[i];
            if (gate == null)
                continue;

            Gizmos.color = gate.IsClosed ? Color.red : Color.yellow;
            Gizmos.DrawSphere(gate.transform.position, 0.15f);
        }
    }

    private Color GetEdgeColor(MazeNeighborLink link)
    {
        return link.edgeType switch
        {
            MazeEdgeType.BidirectionalDoorClosed => Color.red,
            MazeEdgeType.BidirectionalDoorOpen => Color.yellow,
            MazeEdgeType.BidirectionalAlwaysOpen => Color.green,
            MazeEdgeType.OneWay => Color.blue,
            _ => Color.white
        };
    }
}
