using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CustomVisionTrainer : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static CustomVisionTrainer Instance;

    /// <summary>
    /// Custom Vision Service URL root
    /// </summary>
    private string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.2/Training/projects/";

    /// <summary>
    /// Insert your prediction key here
    /// </summary>
    private string trainingKey = "cad3847e45e2425599381ec24b2f3b6a";

    /// <summary>
    /// Insert your Project Id here
    /// </summary>
    private string projectId = "2ca103b6-448b-48c3-8290-0699a1577e6d";

    /// <summary>
    /// Byte array of the image to submit for analysis
    /// </summary>
    internal byte[] imageBytes;

    /// <summary>
    /// The Tags accepted
    /// </summary>
    internal enum Tags { Mouse, Keyboard }

    /// <summary>
    /// The UI displaying the training Chapters
    /// </summary>
    private TextMesh trainingUI_TextMesh;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Runs at initialization right after Awake method
    /// </summary>
    private void Start()
    {
        trainingUI_TextMesh = SceneOrganiser.Instance.CreateTrainingUI("TrainingUI", 0.04f, 0, 4, false);
    }

    internal void RequestTagSelection()
    {
        trainingUI_TextMesh.gameObject.SetActive(true);
        trainingUI_TextMesh.text = $" \nUse voice command \nto choose between the following tags: \nMouse\nKeyboard \nor say Discard";

        VoiceRecognizer.Instance.keywordRecognizer.Start();
    }

    /// <summary>
    /// Verify voice input against stored tags.
    /// If positive, it will begin the Service training process.
    /// </summary>
    internal void VerifyTag(string spokenTag)
    {
        if (spokenTag == Tags.Mouse.ToString() || spokenTag == Tags.Keyboard.ToString())
        {
            trainingUI_TextMesh.text = $"Tag chosen: {spokenTag}";
            VoiceRecognizer.Instance.keywordRecognizer.Stop();
            StartCoroutine(SubmitImageForTraining(ImageCapture.Instance.filePath, spokenTag));
        }
    }

    /// <summary>
    /// Call the Custom Vision Service to submit the image.
    /// </summary>
    public IEnumerator SubmitImageForTraining(string imagePath, string tag)
    {
        yield return new WaitForSeconds(2);
        trainingUI_TextMesh.text = $"Submitting Image \nwith tag: {tag} \nto Custom Vision Service";
        string imageId = string.Empty;
        string tagId = string.Empty;

        // Retrieving the Tag Id relative to the voice input
        string getTagIdEndpoint = string.Format("{0}{1}/tags", url, projectId);
        using (UnityWebRequest www = UnityWebRequest.Get(getTagIdEndpoint))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;

            Tags_RootObject tagRootObject = JsonConvert.DeserializeObject<Tags_RootObject>(jsonResponse);

            foreach (TagOfProject tOP in tagRootObject.Tags)
            {
                if (tOP.Name == tag)
                {
                    tagId = tOP.Id;
                }
            }
        }

        // Creating the image object to send for training
        List<IMultipartFormSection> multipartList = new List<IMultipartFormSection>();
        MultipartObject multipartObject = new MultipartObject();
        multipartObject.contentType = "application/octet-stream";
        multipartObject.fileName = "";
        multipartObject.sectionData = GetImageAsByteArray(imagePath);
        multipartList.Add(multipartObject);

        string createImageFromDataEndpoint = string.Format("{0}{1}/images?tagIds={2}", url, projectId, tagId);

        using (UnityWebRequest www = UnityWebRequest.Post(createImageFromDataEndpoint, multipartList))
        {
            // Gets a byte array out of the saved image
            imageBytes = GetImageAsByteArray(imagePath);

            //unityWebRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            www.SetRequestHeader("Training-Key", trainingKey);

            // The upload handler will help uploading the byte array with the request
            www.uploadHandler = new UploadHandlerRaw(imageBytes);

            // The download handler will help receiving the analysis from Azure
            www.downloadHandler = new DownloadHandlerBuffer();

            // Send the request
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            ImageRootObject m = JsonConvert.DeserializeObject<ImageRootObject>(jsonResponse);
            imageId = m.Images[0].Image.Id;
        }
        trainingUI_TextMesh.text = "Image uploaded";
        StartCoroutine(TrainCustomVisionProject());
    }

    /// <summary>
    /// Call the Custom Vision Service to train the Service.
    /// It will generate a new Iteration in the Service
    /// </summary>
    public IEnumerator TrainCustomVisionProject()
    {
        yield return new WaitForSeconds(2);

        trainingUI_TextMesh.text = "Training Custom Vision Service";

        WWWForm webForm = new WWWForm();

        string trainProjectEndpoint = string.Format("{0}{1}/train", url, projectId);

        using (UnityWebRequest www = UnityWebRequest.Post(trainProjectEndpoint, webForm))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;
            Debug.Log($"Training - JSON Response: {jsonResponse}");

            // A new iteration that has just been created and trained
            Iteration iteration = new Iteration();
            iteration = JsonConvert.DeserializeObject<Iteration>(jsonResponse);

            if (www.isDone)
            {
                trainingUI_TextMesh.text = "Custom Vision Trained";

                // Since the Service has a limited number of iterations available,
                // we need to set the last trained iteration as default
                // and delete all the iterations you dont need anymore
                StartCoroutine(SetDefaultIteration(iteration));
            }
        }
    }

    /// <summary>
    /// Set the newly created iteration as Default
    /// </summary>
    private IEnumerator SetDefaultIteration(Iteration iteration)
    {
        yield return new WaitForSeconds(5);
        trainingUI_TextMesh.text = "Setting default iteration";

        // Set the last trained iteration to default
        iteration.IsDefault = true;

        // Convert the iteration object as JSON
        string iterationAsJson = JsonConvert.SerializeObject(iteration);
        byte[] bytes = Encoding.UTF8.GetBytes(iterationAsJson);

        string setDefaultIterationEndpoint = string.Format("{0}{1}/iterations/{2}",
                                                        url, projectId, iteration.Id);

        using (UnityWebRequest www = UnityWebRequest.Put(setDefaultIterationEndpoint, bytes))
        {
            www.method = "PATCH";
            www.SetRequestHeader("Training-Key", trainingKey);
            www.SetRequestHeader("Content-Type", "application/json");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            if (www.isDone)
            {
                trainingUI_TextMesh.text = "Default iteration is set \nDeleting Unused Iteration";
                StartCoroutine(DeletePreviousIteration(iteration));
            }
        }
    }

    /// <summary>
    /// Delete the previous non-default iteration.
    /// </summary>
    public IEnumerator DeletePreviousIteration(Iteration iteration)
    {
        yield return new WaitForSeconds(5);

        trainingUI_TextMesh.text = "Deleting Unused \nIteration";

        string iterationToDeleteId = string.Empty;

        string findAllIterationsEndpoint = string.Format("{0}{1}/iterations", url, projectId);

        using (UnityWebRequest www = UnityWebRequest.Get(findAllIterationsEndpoint))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            // The iteration that has just been trained
            List<Iteration> iterationsList = new List<Iteration>();
            iterationsList = JsonConvert.DeserializeObject<List<Iteration>>(jsonResponse);

            foreach (Iteration i in iterationsList)
            {
                if (i.IsDefault != true)
                {
                    Debug.Log($"Cleaning - Deleting iteration: {i.Name}, {i.Id}");
                    iterationToDeleteId = i.Id;
                    break;
                }
            }
        }

        string deleteEndpoint = string.Format("{0}{1}/iterations/{2}", url, projectId, iterationToDeleteId);

        using (UnityWebRequest www2 = UnityWebRequest.Delete(deleteEndpoint))
        {
            www2.SetRequestHeader("Training-Key", trainingKey);
            www2.downloadHandler = new DownloadHandlerBuffer();
            yield return www2.SendWebRequest();
            string jsonResponse = www2.downloadHandler.text;

            trainingUI_TextMesh.text = "Iteration Deleted";
            yield return new WaitForSeconds(2);
            trainingUI_TextMesh.text = "Ready for next \ncapture";

            yield return new WaitForSeconds(2);
            trainingUI_TextMesh.text = "";
            ImageCapture.Instance.ResetImageCapture();
        }
    }

    /// <summary>
    /// Returns the contents of the specified image file as a byte array.
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }
}
