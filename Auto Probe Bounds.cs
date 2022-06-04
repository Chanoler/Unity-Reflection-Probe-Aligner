using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AutoProbeAligner : EditorWindow
{
    private float margin = 0.01f;
    private bool moveProbe = true;
    private bool eyeHeight = true;
    private float eyeHeightOffset = 1.8f;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Tools/ReflectionProbe Aligner")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        AutoProbeAligner window = (AutoProbeAligner)EditorWindow.GetWindow(typeof(AutoProbeAligner));
        window.titleContent = new GUIContent("Reflection Probe Aligner");
        window.Show();
    }

    void OnGUI()
    {
        //Display the Settings label
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        //Display the margin input field
        margin = EditorGUILayout.FloatField(new GUIContent("Margins", "The amount of extra area to encapsulate to ensure objects on the walls receive reflection probing"), margin);

        //Display the move probe checkbox
        moveProbe = EditorGUILayout.Toggle(new GUIContent("Move Probe", "Move the probe to the center of the bounds"), moveProbe);

        //Display the eye height checkbox only if the move probe checkbox is checked
        if (moveProbe)
        {
            //Eye height checkbox
            eyeHeight = EditorGUILayout.Toggle(new GUIContent("Eye Height", "Move the probe to the center of the bounds"), eyeHeight);

            //Display the eye height offset input field only if the eye height checkbox is checked
            if (eyeHeight)
            {
                eyeHeightOffset = EditorGUILayout.FloatField(new GUIContent("Eye Height Offset", "The distance from the floor to the eye height"), eyeHeightOffset);
            }
        }

        //Button that will align all the probes
        if (GUILayout.Button("Align Probes"))
        {
            AlignProbes();
        }

    }

    private void AlignProbes()
    {
        //Get the selected objects
        GameObject[] selectedObjects = Selection.gameObjects;

        //Ensure at least one object is selected
        if (selectedObjects.Length < 1)
        {
            Debug.Log("No objects selected");
            return;
        }

        //Ensure exactly one of the selected objects is a reflection probe
        ReflectionProbe probe = null;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            //Get the probe from the current object
            ReflectionProbe currentProbe = selectedObjects[i].GetComponent<ReflectionProbe>();

            //If the probe has already been assigned but another was found then display an error
            if (probe != null && currentProbe != null)
            {
                Debug.LogError("Multiple probes selected");
                return;
            }

            //If the current probe is not null then assign it to the probe variable
            if (currentProbe != null)
            {
                probe = currentProbe;
            }
        }
        //If the probe is null then display an error
        if (probe == null)
        {
            Debug.LogError("No probe selected");
            return;
        }

        //Remove objects without renderers
        List<GameObject> objectsToAlign = new List<GameObject>();
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Renderer renderer = selectedObjects[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                objectsToAlign.Add(selectedObjects[i]);
            }
        }
        selectedObjects = objectsToAlign.ToArray();

        //Get the renderer of the selected objects
        Renderer[] renderers = new Renderer[selectedObjects.Length];
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            //Skip null renderers
            if (selectedObjects[i].GetComponent<Renderer>() == null)
                continue;

            //Add the renderer to the renderers array
            renderers[i] = selectedObjects[i].GetComponent<Renderer>();
        }

        //Get the bounds of the selected objects
        Bounds[] bounds = new Bounds[selectedObjects.Length];
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            bounds[i] = renderers[i].bounds;
        }

        //Get the total bounds of the selected objects
        Bounds totalBounds = bounds[0];

        //Add the bounds of each object to the total bounds
        for (int i = 1; i < bounds.Length; i++)
        {
            totalBounds.min = Vector3.Min(totalBounds.min, bounds[i].min);
            totalBounds.max = Vector3.Max(totalBounds.max, bounds[i].max);
        }

        //If the move probe checkbox is checked then move the probe to the center of the bounds
        if (moveProbe)
        {
            //Get the center of the total bounds
            Vector3 center = totalBounds.center;

            //If the eye height checkbox is checked then move the probe to the eye height
            if (eyeHeight)
            {
                //Get the center of the floor
                Vector3 floorCenter = new Vector3(center.x, totalBounds.min.y, center.z);

                //Get the eye position
                Vector3 eyePosition = floorCenter + Vector3.up * eyeHeightOffset;

                //Move the probe to the eye position
                probe.transform.position = eyePosition;
            }
            else
            {
                //Move the probe to the center of the bounds
                probe.transform.position = center;
            }
        }

        //Room Local center
        Vector3 roomLocalCenter = totalBounds.center - probe.transform.position;

        //Move the box offset to the center of the total bounds
        probe.center = roomLocalCenter;

        //Set the probe size to encapsulate the total bounds
        probe.size = totalBounds.size + new Vector3(margin, margin, margin);
    }
}
