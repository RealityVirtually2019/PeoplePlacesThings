using UnityEngine;
using System.Collections;
using UnityEngine.Windows.Speech;

public class DictationRecognizerBehaviour : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    //public static DictationRecognizerBehaviour Instance;
    //public static DictationRecognizerBehaviour Instance;

    //DictationRecognizer dictationRecognizer;
    public static DictationRecognizer dictationRecognizer;

    /// <summary>
    /// Initialises this class
    /// </summary>
    ///
    /*
    private void Awake()
    {
        // Allows this instance to behave like a singleton
        Instance = this;
    }
    */

    // Use this for initialization
    void Start()
    {
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.InitialSilenceTimeoutSeconds = 60;
        dictationRecognizer.AutoSilenceTimeoutSeconds = 60;

        dictationRecognizer.DictationResult += onDictationResult;
        dictationRecognizer.DictationHypothesis += onDictationHypothesis;
        dictationRecognizer.DictationComplete += onDictationComplete;
        dictationRecognizer.DictationError += onDictationError;

        dictationRecognizer.Start();
    }

    void onDictationResult(string text, ConfidenceLevel confidence)
    {
        // write your logic here
        Debug.LogFormat("Dictation result: " + text);
        SceneOrganiser.Instance.UpdateLabel(text);
    }

    void onDictationHypothesis(string text)
    {
        // write your logic here
        Debug.LogFormat("Dictation hypothesis: {0}", text);
    }

    void onDictationComplete(DictationCompletionCause cause)
    {
        // write your logic here
        if (cause != DictationCompletionCause.Complete)
            Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", cause);
    }

    void onDictationError(string error, int hresult)
    {
        // write your logic here
        Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
    }
}