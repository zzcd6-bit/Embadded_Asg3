using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGraphController))]
public class MazeGraphControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MazeGraphController graph = (MazeGraphController)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Rebuild Graph From Child Nodes"))
            MazeGraphEditorBuilder.Rebuild(graph);

        if (GUILayout.Button("Create/Update Spherical Fog Zones For Child Nodes"))
            MazeGraphEditorBuilder.CreateOrUpdateFogZones(graph);
    }
}

public static class MazeGraphEditorBuilder
{
    public static void Rebuild(MazeGraphController graph)
    {
        if (graph == null)
            return;

        MazeNode[] nodes = graph.GetComponentsInChildren<MazeNode>(true);

        for (int i = 0; i < nodes.Length; i++)
            SyncNode(graph, nodes[i]);

        for (int i = 0; i < nodes.Length; i++)
        {
            Undo.RecordObject(nodes[i], "Refresh Maze Gates");
            nodes[i].RefreshGeneratedGateList();
            EditorUtility.SetDirty(nodes[i]);
        }
    }

    public static void CreateOrUpdateFogZones(MazeGraphController graph)
    {
        if (graph == null)
            return;

        MazeNode[] nodes = graph.GetComponentsInChildren<MazeNode>(true);

        for (int i = 0; i < nodes.Length; i++)
        {
            MazeNode node = nodes[i];
            if (node == null)
                continue;

            CreateOrUpdateFogZone(graph, node);
        }
    }

    private static void CreateOrUpdateFogZone(MazeGraphController graph, MazeNode node)
    {
        if (graph == null || node == null)
            return;

        Transform container = GetOrCreateFogZoneContainer(graph);
        BambooFogZone zone = FindExistingFogZone(graph, node);
        GameObject zoneObject;

        if (zone == null)
        {
            zoneObject = new GameObject($"{node.name}_FogZone");
            Undo.RegisterCreatedObjectUndo(zoneObject, "Create Maze Fog Zone");
            zoneObject.transform.SetParent(container);
            zoneObject.transform.position = node.transform.position;
            zoneObject.transform.rotation = Quaternion.identity;

            SphereCollider sphereCollider = zoneObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;

            zone = zoneObject.AddComponent<BambooFogZone>();
        }
        else
        {
            zoneObject = zone.gameObject;
            Undo.RecordObject(zoneObject.transform, "Update Maze Fog Zone Transform");
            zoneObject.transform.SetParent(container);
            zoneObject.transform.position = node.transform.position;
            zoneObject.transform.rotation = Quaternion.identity;
        }

        Undo.RecordObject(zoneObject.transform, "Configure Maze Fog Zone");
        zoneObject.transform.localScale = Vector3.one;

        SphereCollider collider = zoneObject.GetComponent<SphereCollider>();
        if (collider == null)
            collider = zoneObject.AddComponent<SphereCollider>();

        Undo.RecordObject(collider, "Configure Maze Fog Zone Collider");
        collider.isTrigger = true;
        collider.radius = graph.defaultFogZoneRadius;
        EditorUtility.SetDirty(collider);

        Collider[] colliders = zoneObject.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider otherCollider = colliders[i];
            if (otherCollider != null && otherCollider != collider)
                Undo.DestroyObjectImmediate(otherCollider);
        }

        Undo.RecordObject(zone, "Configure Maze Fog Zone");
        BambooFogSettings currentSettings = zone.GetSettings();
        zone.Configure(
            new BambooFogSettings(
                graph.defaultFogZoneDensity,
                currentSettings.visibilityDistance,
                currentSettings.clearRadius,
                graph.defaultFogZoneTransitionDuration
            ),
            graph.defaultFogZonePriority
        );
        zone.SetTriggerRadius(graph.defaultFogZoneRadius);

        EditorUtility.SetDirty(zoneObject);
        EditorUtility.SetDirty(zone);
    }

    private static Transform GetOrCreateFogZoneContainer(MazeGraphController graph)
    {
        const string containerName = "Fog Zones";
        Transform container = graph.transform.Find(containerName);
        if (container != null)
            return container;

        GameObject containerObject = new GameObject(containerName);
        Undo.RegisterCreatedObjectUndo(containerObject, "Create Maze Fog Zone Container");
        containerObject.transform.SetParent(graph.transform);
        containerObject.transform.localPosition = Vector3.zero;
        containerObject.transform.localRotation = Quaternion.identity;
        containerObject.transform.localScale = Vector3.one;
        return containerObject.transform;
    }

    private static BambooFogZone FindExistingFogZone(MazeGraphController graph, MazeNode node)
    {
        string zoneName = $"{node.name}_FogZone";
        BambooFogZone[] zones = graph.GetComponentsInChildren<BambooFogZone>(true);
        for (int i = 0; i < zones.Length; i++)
        {
            BambooFogZone zone = zones[i];
            if (zone != null && zone.name == zoneName)
                return zone;
        }

        return null;
    }

    private static void SyncNode(MazeGraphController graph, MazeNode node)
    {
        if (node == null || node.neighbors == null)
            return;

        for (int i = 0; i < node.neighbors.Count; i++)
        {
            MazeNeighborLink link = node.neighbors[i];
            if (link == null || link.neighbor == null || link.neighbor == node)
                continue;

            SyncLink(graph, node, link);
        }
    }

    private static void SyncLink(MazeGraphController graph, MazeNode node, MazeNeighborLink link)
    {
        if (link.edgeType == MazeEdgeType.OneWay)
        {
            link.gate = null;
            return;
        }

        MazeNeighborLink reverseLink = link.neighbor.GetOrAddLinkTo(node, link.edgeType);
        reverseLink.edgeType = link.edgeType;

        if (!link.HasMovableDoor)
        {
            link.gate = null;
            reverseLink.gate = null;
            return;
        }

        MazeGate gate = link.gate != null ? link.gate : reverseLink.gate;
        if (gate == null)
        {
            Debug.LogWarning($"{node.name} -> {link.neighbor.name} is a door edge but has no MazeGate assigned.");
            return;
        }

        Undo.RecordObject(node, "Sync Maze Link");
        Undo.RecordObject(link.neighbor, "Sync Maze Link");

        link.gate = gate;
        reverseLink.gate = gate;
        ConfigureGate(gate, node, link.neighbor, link.edgeType);

        EditorUtility.SetDirty(node);
        EditorUtility.SetDirty(link.neighbor);
    }

    private static void ConfigureGate(MazeGate gate, MazeNode firstNode, MazeNode secondNode, MazeEdgeType edgeType)
    {
        if (gate == null || firstNode == null || secondNode == null)
            return;

        Undo.RecordObject(gate, "Configure Maze Gate");
        gate.ConfigureEdge(firstNode, secondNode, edgeType);

        EditorUtility.SetDirty(gate);
    }
}
