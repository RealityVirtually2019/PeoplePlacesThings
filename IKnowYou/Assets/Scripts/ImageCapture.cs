using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using UnityEngine.XR.WSA.WebCam;

public class ImageCapture : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static ImageCapture Instance;

    /// <summary>
    /// Keep counts of the taps for image renaming
    /// </summary>
    private int captureCount = 0;

    /// <summary>
    /// Photo Capture object
    /// </summary>
    private PhotoCapture photoCaptureObject = null;

    /// <summary>
    /// Allows gestures recognition in HoloLens
    /// </summary>
    private GestureRecognizer recognizer;

    /// <summary>
    /// Loop timer
    /// </summary>
    private float secondsBetweenCaptures = 10f;

    /// <summary>
    /// Application main functionalities switch
    /// </summary>
    internal enum AppModes { Analysis, Training }

    /// <summary>
    /// Local variable for current AppMode
    /// </summary>
    internal AppModes AppMode { get; private set; }

    /// <summary>
    /// Flagging if the capture loop is running
    /// </summary>
    internal bool captureIsActive;

    /// <summary>
    /// File path of current analysed photo
    /// </summary>
    internal string filePath = string.Empty;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        Instance = this;

        // Change this flag to switch between Analysis Mode and Training Mode 
        AppMode = AppModes.Training;
        //AppMode = AppModes.Analysis;
    }

    /// <summary>
    /// Runs at initialization right after Awake method
    /// </summary>
    void Start()
    {
        // Clean up the LocalState folder of this application from all photos stored
        DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                Debug.LogFormat("Cannot delete file: ", file.Name);
            }
        }

        // Subscribing to the Hololens API gesture recognizer to track user gestures
        recognizer = new GestureRecognizer();
        recognizer.SetRecognizableGestures(GestureSettings.Tap);
        recognizer.Tapped += TapHandler;
        recognizer.StartCapturingGestures();

        SceneOrganiser.Instance.SetCameraStatus("Ready");
    }

    /// <summary>
    /// Respond to Tap Input.
    /// </summary>
    private void TapHandler(TappedEventArgs obj)
    {
        switch (AppMode)
        {
            case AppModes.Analysis:
                if (!captureIsActive)
                {
                    captureIsActive = true;

                    // Set the cursor color to red
                    SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.red;

                    // Update camera status to looping capture.
                    SceneOrganiser.Instance.SetCameraStatus("Looping Capture");

                    // Begin the capture loop
                    InvokeRepeating("ExecuteImageCaptureAndAnalysis", 0, secondsBetweenCaptures);
                }
                else
                {
                    // The user tapped while the app was analyzing 
                    // therefore stop the analysis process
                    ResetImageCapture();
                }
                break;

            case AppModes.Training:
                if (!captureIsActive)
                {
                    captureIsActive = true;

                    // Call the image capture
                    ExecuteImageCaptureAndAnalysis();

                    // Set the cursor color to red
                    SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.red;

                    // Update camera status to uploading image.
                    SceneOrganiser.Instance.SetCameraStatus("Uploading Image");
                }
                break;
        }
    }


    /// <summary>
    /// Begin process of Image Capturing and send To Azure Custom Vision Service.
    /// </summary>
    private void ExecuteImageCaptureAndAnalysis()
    {
        // Update camera status to analysis.
        SceneOrganiser.Instance.SetCameraStatus("Analysis");

        // Create a label in world space using the SceneOrganiser class 
        // Invisible at this point but correctly positioned where the image was taken
        SceneOrganiser.Instance.PlaceAnalysisLabel();

        // Set the camera resolution to be the highest possible
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Begin capture process, set the image format
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            CameraParameters camParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = targetTexture.width,
                cameraResolutionHeight = targetTexture.height,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            // Capture the image from the camera and save it in the App internal folder
            captureObject.StartPhotoModeAsync(camParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                string filename = string.Format(@"CapturedImage{0}.jpg", captureCount);
                filePath = Path.Combine(Application.persistentDataPath, filename);
                captureCount++;
                photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            });
        });
    }


    /// <summary>
    /// Register the full execution of the Photo Capture. 
    /// </summary>
    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        // Call StopPhotoMode once the image has successfully captured
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    /// <summary>
    /// The camera photo mode has stopped after the capture.
    /// Begin the Image Analysis process.
    /// </summary>
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.LogFormat("Stopped Photo Mode");

        // Dispose from the object in memory and request the image analysis 
        photoCaptureObject.Dispose();
        photoCaptureObject = null;

        switch (AppMode)
        {
            case AppModes.Analysis:
                // Call the image analysis
                StartCoroutine(CustomVisionAnalyser.Instance.AnalyseLastImageCaptured(filePath));
                break;

            case AppModes.Training:
                // Call training using captured image
                CustomVisionTrainer.Instance.RequestTagSelection();
                break;
        }
    }

    /// <summary>
    /// Stops all capture pending actions
    /// </summary>
    internal void ResetImageCapture()
    {
        captureIsActive = false;

        // Set the cursor color to green
        SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.green;

        // Update camera status to ready.
        //SceneOrganiser.Instance.SetCameraStatus("Ready");

        // Stop the capture loop if active
        CancelInvoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
