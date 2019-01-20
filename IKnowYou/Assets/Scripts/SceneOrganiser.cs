using System;
using UnityEngine;

public class SceneOrganiser : MonoBehaviour
{/// <summary>
 /// Allows this class to behave like a singleton
 /// </summary>
    public static SceneOrganiser Instance;

    /// <summary>
    /// The cursor object attached to the camera
    /// </summary>
    internal GameObject cursor;

    /// <summary>
    /// The label used to display the analysis on the objects in the real world
    /// </summary>
    internal GameObject label;

    /// <summary>
    /// Object providing the current status of the camera.
    /// </summary>
    internal TextMesh cameraStatusIndicator;

    /// <summary>
    /// Reference to the last label positioned
    /// </summary>
    internal Transform lastLabelPlaced;

    /// <summary>
    /// Reference to the last label positioned
    /// </summary>
    internal TextMesh lastLabelPlacedText;

    /// <summary>
    /// Current threshold accepted for displaying the label
    /// Reduce this value to display the recognition more often
    /// </summary>
    internal float probabilityThreshold = 0.5f;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        // Use this class instance as singleton
        Instance = this;

        // Add the ImageCapture class to this GameObject
        gameObject.AddComponent<ImageCapture>();

        // Add the CustomVisionAnalyser class to this GameObject
        gameObject.AddComponent<CustomVisionAnalyser>();

        // Add the CustomVisionTrainer class to this GameObject
        gameObject.AddComponent<CustomVisionTrainer>();

        // Add the VoiceRecogniser class to this GameObject
        //gameObject.AddComponent<VoiceRecognizer>();

        // Add the CustomVisionObjects class to this GameObject
        gameObject.AddComponent<CustomVisionObjects>();

        // Add the DictationRecognizerBehaviour class to this GameObject
        gameObject.AddComponent<DictationRecognizerBehaviour>();

        // Create the camera Cursor
        cursor = CreateCameraCursor();

        // Load the label prefab as reference
        label = CreateLabel();

        // Create the camera status indicator label, and place it above where predictions
        // and training UI will appear.
        cameraStatusIndicator = CreateTrainingUI("Status Indicator", 0.02f, 0.2f, 3, true);

        // Set camera status indicator to loading.
        SetCameraStatus("Loading");
    }

    /// <summary>
    /// Spawns cursor for the Main Camera
    /// </summary>
    private GameObject CreateCameraCursor()
    {
        // Create a sphere as new cursor
        GameObject newCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Attach it to the camera
        newCursor.transform.parent = gameObject.transform;

        // Resize the new cursor
        newCursor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        // Move it to the correct position
        newCursor.transform.localPosition = new Vector3(0, 0, 4);

        // Set the cursor color to red
        newCursor.GetComponent<Renderer>().material = new Material(Shader.Find("Diffuse"));
        newCursor.GetComponent<Renderer>().material.color = Color.green;

        return newCursor;
    }

    /// <summary>
    /// Create the analysis label object
    /// </summary>
    private GameObject CreateLabel()
    {
        // Create a sphere as new cursor
        GameObject newLabel = new GameObject();

        // Resize the new cursor
        newLabel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Creating the text of the label
        TextMesh t = newLabel.AddComponent<TextMesh>();
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.fontSize = 50;
        t.text = "";

        return newLabel;
    }

    /// <summary>
    /// Set the camera status to a provided string. Will be coloured if it matches a keyword.
    /// </summary>
    /// <param name="statusText">Input string</param>
    public void SetCameraStatus(string statusText)
    {
        if (string.IsNullOrEmpty(statusText) == false)
        {
            string message = "white";

            switch (statusText.ToLower())
            {
                case "loading":
                    message = "yellow";
                    break;

                case "ready":
                    message = "green";
                    break;

                case "uploading image":
                    message = "red";
                    break;

                case "looping capture":
                    message = "yellow";
                    break;

                case "analysis":
                    message = "red";
                    break;
            }

            //cameraStatusIndicator.GetComponent<TextMesh>().text = $"Camera Status:\n<color={message}>{statusText}..</color>";


            string s = "Words recognized:\n";
            string[] words = statusText.Split(' ');
            bool commandFound = false;
            foreach (var word in words)
            {
                if (commandFound)
                {
                    commandFound = false;
                    s += "<color=green>"+word+"</color>";
                }
                else
                {
                    s += word;
                }
                s += " ";
                if (String.Equals("hello", word, StringComparison.OrdinalIgnoreCase))
                {
                    commandFound = true;
                }
            }
            cameraStatusIndicator.GetComponent<TextMesh>().text = s;
        }
    }

    /// <summary>
    /// Instantiate a label in the appropriate location relative to the Main Camera.
    /// </summary>
    public void PlaceAnalysisLabel()
    {
        lastLabelPlaced = Instantiate(label.transform, cursor.transform.position, transform.rotation);
        lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();
    }

    /// <summary>
    /// Set the Tags as Text of the last label created. 
    /// </summary>
    public void SetTagsToLastLabel(AnalysisObject analysisObject)
    {
        lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();

        if (analysisObject.Predictions != null)
        {
            foreach (Prediction p in analysisObject.Predictions)
            {
                if (p.Probability > 0.02)
                {
                    lastLabelPlacedText.text += $"Detected: {p.TagName} {p.Probability.ToString("0.00 \n")}";
                    Debug.Log($"Detected: {p.TagName} {p.Probability.ToString("0.00 \n")}");
                }
            }
        }
    }

    public void UpdateLabel(string text)
    {
        //lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();
        //lastLabelPlacedText.text += text;

        //cameraStatusIndicator.GetComponent<TextMesh>().text = $"Camera Status:\n<color={message}>{statusText}..</color>";
        //cameraStatusIndicator.GetComponent<TextMesh>().text = $"Camera Status:\n<color={message}>{statusText}..</color>";

        //TextMesh lblText = label.GetComponent<TextMesh>();
        //lblText.text = "woof";

        SetCameraStatus(text);
    }

    /// <summary>
    /// Create a 3D Text Mesh in scene, with various parameters.
    /// </summary>
    /// <param name="name">name of object</param>
    /// <param name="scale">scale of object (i.e. 0.04f)</param>
    /// <param name="yPos">height above the cursor (i.e. 0.3f</param>
    /// <param name="zPos">distance from the camera</param>
    /// <param name="setActive">whether the text mesh should be visible when it has been created</param>
    /// <returns>Returns a 3D text mesh within the scene</returns>
    internal TextMesh CreateTrainingUI(string name, float scale, float yPos, float zPos, bool setActive)
    {
        GameObject display = new GameObject(name, typeof(TextMesh));
        display.transform.parent = Camera.main.transform;
        display.transform.localPosition = new Vector3(0, yPos, zPos);
        display.SetActive(setActive);
        display.transform.localScale = new Vector3(scale, scale, scale);
        display.transform.rotation = new Quaternion();
        TextMesh textMesh = display.GetComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        return textMesh;
    }
}
