using System;
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
    void Start()
    {

        Array tagsArray = Enum.GetValues(typeof(CustomVisionTrainer.Tags));

        foreach (object tagWord in tagsArray)
        {
            _keywords.Add(tagWord.ToString(), () =>
            {
                // When a word is recognized, the following line will be called
                CustomVisionTrainer.Instance.VerifyTag(tagWord.ToString());
            });
        }

        _keywords.Add("Discard", () =>
        {
            // When a word is recognized, the following line will be called
            // The user does not want to submit the image
            // therefore ignore and discard the process
            ImageCapture.Instance.ResetImageCapture();
            keywordRecognizer.Stop();
        });

        //Create the keyword recognizer 
        keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        // Register for the OnPhraseRecognized event 
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
    }

    /// <summary>
    /// Handler called when a word is recognized
    /// </summary>
    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        // if the keyword recognized is in our dictionary, call that Action.
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
}
