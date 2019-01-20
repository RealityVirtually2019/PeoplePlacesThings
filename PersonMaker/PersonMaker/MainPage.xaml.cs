using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PersonMaker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string authKey;
        string personGroupId;
        string personGroupName;

        Guid personId;
        string personName;
        StorageFolder personFolder;

        private FaceServiceClient faceServiceClient;
        private PersonGroup knownGroup;
        private int minPhotos = 6;

        public MainPage()
        {
            this.InitializeComponent();
            personName = string.Empty;
            authKey = string.Empty;
            personGroupId = string.Empty;
            personGroupName = string.Empty;
            personId = Guid.Empty;
        }

        /// <summary>
        /// Create a person group with ID and name provided if none can be found in the service.
        /// </summary>
        private async void CreatePersonGroupButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            authKey = AuthKeyTextBox.Text;

            if (string.IsNullOrWhiteSpace(personGroupId) == false && string.IsNullOrWhiteSpace(personGroupName) == false && string.IsNullOrWhiteSpace(authKey) == false)
            {
                PersonGroupCreateErrorText.Visibility = Visibility.Collapsed;
                await ApiCallAllowed(true);
                faceServiceClient = new FaceServiceClient(authKey);

                if (null != faceServiceClient)
                {
                    // You may experience issues with this below call, if you are attempting connection with
                    // a service location other than 'West US'
                    PersonGroup[] groups = await faceServiceClient.ListPersonGroupsAsync();
                    var matchedGroups = groups.Where(p => p.PersonGroupId == personGroupId);

                    if (matchedGroups.Count() > 0)
                    {
                        knownGroup = matchedGroups.FirstOrDefault();

                        PersonGroupStatusTextBlock.Text = "Found existing: " + knownGroup.Name;
                    }

                    if (null == knownGroup)
                    {
                        await ApiCallAllowed(true);
                        await faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupName);
                        knownGroup = await faceServiceClient.GetPersonGroupAsync(personGroupId);

                        PersonGroupStatusTextBlock.Text = "Created new group: " + knownGroup.Name;
                    }

                    if (PersonGroupStatusTextBlock.Text != "- Person Group status -")
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
            }
            else
            {
                PersonGroupCreateErrorText.Text = "Make sure you provide: a Person Group ID, a Person Group Name, and the Authentication Key in the section above.";
                PersonGroupCreateErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void FetchPersonGroup_Click(object sender, RoutedEventArgs e)
        {
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            authKey = AuthKeyTextBox.Text;

            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);

            if (null != faceServiceClient)
            {
                // You may experience issues with this below call, if you are attempting connection with
                // a service location other than 'West US'
                PersonGroup[] groups = await faceServiceClient.ListPersonGroupsAsync();
                var matchedGroups = groups.Where(p => p.PersonGroupId == personGroupId);

                if (matchedGroups.Count() > 0)
                {
                    knownGroup = matchedGroups.FirstOrDefault();

                    PersonGroupStatusTextBlock.Text = "Found existing: " + knownGroup.Name;
                }

                if (null == knownGroup)
                {
                    PersonGroupStatusTextBlock.Text = "Could not find group: " + knownGroup.Name;
                }

                if (PersonGroupStatusTextBlock.Text.ToLower().Contains("found"))
                {
                    PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private async void CreatePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personName = PersonNameTextBox.Text;
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            if (knownGroup != null && personName.Length > 0)
            {
                CreatePersonErrorText.Visibility = Visibility.Collapsed;
                //Check if this person already exist
                bool personAlreadyExist = false;
                Person[] ppl = await GetKnownPeople();
                foreach (Person p in ppl)
                {
                    if (p.Name == personName)
                    {
                        personAlreadyExist = true;
                        PersonStatusTextBlock.Text = $"Person already exist: {p.Name} ID: {p.PersonId}";

                        PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }

                if (!personAlreadyExist)
                {
                    await ApiCallAllowed(true);
                    CreatePersonResult result = await faceServiceClient.CreatePersonAsync(personGroupId, personName);
                    if (null != result && null != result.PersonId)
                    {
                        personId = result.PersonId;

                        PersonStatusTextBlock.Text = "Created new person: " + result.PersonId;

                        PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }
            }
            else
            {
                CreatePersonErrorText.Text = "Please provide a name above, and ensure that the above person group section has been completed.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }


        private async void CreateFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (personName.Length > 0 && personId != Guid.Empty)
            {
                CreateFolderErrorText.Visibility = Visibility.Collapsed;
                StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
                personFolder = await picturesFolder.CreateFolderAsync(personName, CreationCollisionOption.OpenIfExists);
                await Launcher.LaunchFolderAsync(personFolder);
            }
            else
            {
                CreateFolderErrorText.Text = "You must have created a person in section 3.";
                CreateFolderErrorText.Visibility = Visibility.Visible;
            }
        }



        private async void SubmitToAzureButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string successfullySubmitted = string.Empty;
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

            int imageCounter = 0;
            if (null != personFolder)
            {
                var items = await personFolder.GetFilesAsync();

                if (items.Count > 0)
                {
                    List<StorageFile> imageFilesToUpload = new List<StorageFile>();
                    foreach (StorageFile item in items)
                    {
                        //Windows Cam default save type is jpg
                        if (item.FileType.ToLower() == ".jpg" || item.FileType.ToLower() == ".png")
                        {
                            imageCounter++;
                            imageFilesToUpload.Add(item);
                        }
                        else
                        {
                            Debug.WriteLine(string.Format("Photo {0}, from {1}, is in the wrong format. Images must be jpg or png!", item.DisplayName, item.Path));
                        }
                    }

                    if (imageCounter >= minPhotos)
                    {
                        imageCounter = 0;
                        try
                        {
                            foreach (StorageFile imageFile in imageFilesToUpload)
                            {
                                imageCounter++;
                                using (Stream s = await imageFile.OpenStreamForReadAsync())
                                {
                                    await ApiCallAllowed(true);
                                    AddPersistedFaceResult addResult = await faceServiceClient.AddPersonFaceAsync(personGroupId, personId, s);
                                    Debug.WriteLine("Add result: " + addResult + addResult.PersistedFaceId);
                                }
                                SubmissionStatusTextBlock.Text = string.Format("Submission Status: {0}", imageCounter);
                            }
                            SubmissionStatusTextBlock.Text = "Submission Status: Total Images submitted: " + imageCounter;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Submission Exc: " + ex.Message);
                        }
                    }
                    else
                    {
                        SubmissionStatusTextBlock.Text = $"Submission Status: Please add at least {minPhotos} face images to the person folder.";
                    }
                }
                else
                {
                    successfullySubmitted = "Submission Status: No Image Files Found.";
                }
            }
            else
            {
                successfullySubmitted = "Submission Status: No person folder found! Have you completed section five?";
            }

            if (successfullySubmitted != string.Empty)
            {
                SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                SubmissionStatusTextBlock.Text = successfullySubmitted;
            }
            else
            {
                SubmissionStatusTextBlock.Text = "Submission completed successfully! Now train your service!";
            }
        }

        private async void TrainButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (personGroupId.Length > 0)
            {
                TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                await ApiCallAllowed(true);
                await faceServiceClient.TrainPersonGroupAsync(personGroupId);

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    await ApiCallAllowed(true);
                    trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                    if (trainingStatus.Status != Status.Running)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }

                TrainStatusTextBlock.Text = "Submission Status: Training Completed!";
            }
            else
            {
                TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                TrainStatusTextBlock.Text = "Submission Status: No person group ID found. Have you completed section two?";
            }
        }

        private async void DeletePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personName = PersonNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(personName) == false)
            {
                CreatePersonErrorText.Visibility = Visibility.Collapsed;
                bool personExist = false;
                Person[] ppl = await GetKnownPeople();
                foreach (Person p in ppl)
                {
                    if (p.Name == personName)
                    {
                        personExist = true;
                        PersonStatusTextBlock.Text = $"Deleting person: {p.Name} ID: {p.PersonId}";
                        await RemovePerson(p);
                    }
                }
                if (!personExist)
                {
                    PersonStatusTextBlock.Text = $"No persons found to delete.";
                }
            }
            else
            {
                CreatePersonErrorText.Text = "Cannot delete: No name has been provided.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }

        internal async Task<Person[]> GetKnownPeople()
        {
            Person[] people = null;
            if (null != faceServiceClient)
            {
                await ApiCallAllowed(true);
                people = await faceServiceClient.ListPersonsAsync(personGroupId);
            }
            return people;
        }

        internal async Task RemovePerson(Person person)
        {
            if (null != person)
            {
                await ApiCallAllowed(true);
                await faceServiceClient.DeletePersonAsync(personGroupId, person.PersonId);
            }
        }

        #region Image Upload Throttling

        public int apiMaxCallsPerMinute = 20;
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();
        public List<UInt64> apiCallTimes = new List<UInt64>();
        public void NoteApiCallTime()
        {
            apiCallTimes.Add(GetTickCount64());
        }

        public async Task ApiCallAllowed(bool addAnApiCall)
        {
            bool throttleActive = false;
            UInt64 now = GetTickCount64();
            UInt64 boundary = now - 60 * 1000; // one minute ago
            // remove any in list longer than one minute ago
            while (true && apiCallTimes.Count > 0)
            {
                UInt64 sample = apiCallTimes[0];
                if (sample < boundary)
                {
                    apiCallTimes.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            if (apiCallTimes.Count >= apiMaxCallsPerMinute)
            {
                throttleActive = true;
                Debug.WriteLine("forced to wait for " + (61 * 1000 - (int)(now - apiCallTimes[0])));
                await Task.Delay(61 * 1000 - (int)(now - apiCallTimes[0]));
            }
            if (addAnApiCall)
            {
                NoteApiCallTime();
            }

            ThrottlingActive.Foreground = new SolidColorBrush(throttleActive == true ? Colors.Red : Colors.Green);
            ThrottlingActive.Text = string.Format("Throttling Status: {0}", throttleActive == true ? "ACTIVE!" : "IN-ACTIVE");
        }

        #endregion


    }
}
