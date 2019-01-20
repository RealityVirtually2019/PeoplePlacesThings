using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceRecognizer : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static VoiceRecognizer Instance;

    /// <summary>
    /// Recognizer class for voice recognition
    /// </summary>
    internal KeywordRecognizer keywordRecognizer;

    /// <summary>
    /// List of Keywords registered
    /// </summary>
    private Dictionary<string, Action> _keywords = new Dictionary<string, Action>();

    internal enum Commands { Hello, Again }

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        Instance = this;
        Debug.Log("VoiceRecognizer.Awake");
    }

    internal void HandleCommand(string command)
    //public IEnumerator HandleCommand(string command)
    {
        //Debug.LogFormat("command: ", command);
        string s = "HandleCommand - command:" + command;
        Debug.Log(s);
        keywordRecognizer.Stop();

        //yield return new WaitForSeconds(2);

        DictationRecognizerBehaviour.dictationRecognizer.Start();
        //DictationRecognizerBehaviour dictationRecognizerBehaviour = new DictationRecognizerBehaviour();
        //dictationRecognizerBehaviour.dictationRecognizer.Start();
    }

    /// <summary>
    /// Runs at initialization right after Awake method
    /// </summary>
    void Start()
    {
        Debug.Log("VoiceRecognizer.Start");
        //Array commandArray = Enum.GetValues(typeof(CustomVisionTrainer.Tags));
        Array commandArray = Enum.GetValues(typeof(Commands));

        foreach (object command in commandArray)
        {
            _keywords.Add(command.ToString(), () =>
            {
                /*
                // When a word is recognized, the following line will be called
                CustomVisionTrainer.Instance.VerifyTag(tagWord.ToString());
                */
                //Debug.LogFormat("Word recognized: ", tagWord.ToString());
                HandleCommand(command.ToString());
            });
        }

        /*
        _keywords.Add("Discard", () =>
        {
            // When a word is recognized, the following line will be called
            // The user does not want to submit the image
            // therefore ignore and discard the process
            ImageCapture.Instance.ResetImageCapture();
            keywordRecognizer.Stop();
        });
        */

        //Create the keyword recognizer 
        keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        // Register for the OnPhraseRecognized event 
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    /// <summary>
    /// Handler called when a word is recognized
    /// </summary>
    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log("1");
        Action keywordAction;
        // if the keyword recognized is in our dictionary, call that Action.
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
            //Debug.Log("1");
        }
    }
}
