// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using AdaptiveCards;
    using AdaptiveCards.Rendering;
    using AdaptiveCards.Rendering.Wpf;
    using Microsoft.Bot.Schema;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using Microsoft.Win32;
    using NAudio.Wave;
    using Newtonsoft.Json;
    using VoiceAssistantClient.Settings;
    using Newtonsoft.Json;
    using InHouseRobot_Body;
    using ROBOTIS;
    using Robot;
    using System.Windows.Media.Imaging;
    using System.Media;
    using NAudio;
    using System.Timers;
    using System.Windows.Media;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.Face;
    using Newtonsoft.Json.Linq;
    using OpenCvSharp;
    using OpenCvSharp.Extensions;
    using VideoFrameAnalyzer;
    using FaceAPI = Microsoft.Azure.CognitiveServices.Vision.Face;
    using VisionAPI = Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using LiveCameraSample;
    using System.Net;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Net.Http;
    using System.Web;
    //using Microsoft.Data.SqlClient;
    using System.Data.SqlClient;
    using System.Data.Common;
    using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
    using System.Runtime.InteropServices;
    using OnnxSample;
    using Microsoft.ML;
    //using ScsServoLib;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Drawing;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Objects are disposed OnClosed()")]
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private AppSettings settings = new AppSettings();
        private DialogServiceConnector connector = null;
        private WaveOutEvent player = new WaveOutEvent();
        private Queue<WavQueueEntry> playbackStreams = new Queue<WavQueueEntry>();
        private WakeWordConfiguration activeWakeWordConfig = null;
        private CustomSpeechConfiguration customSpeechConfig = null;
        private ListenState listening = ListenState.NotListening;
        private AdaptiveCardRenderer renderer;
        Thread Playingthread;

        private VideoCapture capture;
        private CancellationTokenSource cameraCaptureCancellationTokenSource;

        private OnnxOutputParser outputParser;
        private PredictionEngine<ImageInputData, TinyYoloPrediction> tinyYoloPredictionEngine;
        private PredictionEngine<ImageInputData, CustomVisionPrediction> customVisionPredictionEngine;

        private static readonly string modelsDirectory = Path.Combine(Environment.CurrentDirectory, @"ML\OnnxModels");

        public static MainWindow mainWindow;

        MediaPlayer mediaPlayer = new MediaPlayer();

        System.Timers.Timer HeadMotionTimer = new System.Timers.Timer();

        System.Timers.Timer DelayTimer = new System.Timers.Timer();

        System.Timers.Timer FacefunctiondelayTimer = new System.Timers.Timer();

        System.Timers.Timer CustomVisionfunctiondelayTimer = new System.Timers.Timer();

        System.Timers.Timer facdelayTimer = new System.Timers.Timer();

        public static System.Timers.Timer ChatbotRestartTimer = new System.Timers.Timer();

        public static System.Timers.Timer NaviResumeTimer = new System.Timers.Timer();

        System.Timers.Timer TourResumeTimer = new System.Timers.Timer();

        System.Timers.Timer FaceResumeTimer = new System.Timers.Timer();

        System.Timers.Timer ArmResumeTimer = new System.Timers.Timer();

        public static System.Timers.Timer FaceMaskWarningTimer = new System.Timers.Timer();

        public static System.Timers.Timer TimeCheckingTimer = new System.Timers.Timer();

        System.Timers.Timer UITimer = new System.Timers.Timer();
        System.Timers.Timer GIFTimer = new System.Timers.Timer();

        System.Timers.Timer Phase1Timer = new System.Timers.Timer();
        System.Timers.Timer Phase2Timer = new System.Timers.Timer();
        System.Timers.Timer Phase3Timer = new System.Timers.Timer();
        System.Timers.Timer Phase4Timer = new System.Timers.Timer();
        System.Timers.Timer Phase5Timer = new System.Timers.Timer();

        System.Timers.Timer IniTimer = new System.Timers.Timer();

        System.Timers.Timer VolumeUpTimer = new System.Timers.Timer();
        System.Timers.Timer VolumeDownTimer = new System.Timers.Timer();

        private FaceAPI.FaceClient _faceClient = null;
        private VisionAPI.ComputerVisionClient _visionClient = null;
        private readonly FrameGrabber<LiveCameraResult> _grabber;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };
        private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
        private bool _fuseClientRemoteResults;
        private LiveCameraResult _latestResultsToDisplay = null;
        private AppMode _mode;
        private DateTime _startTime;

        private BackgroundWorker bgWorker = new BackgroundWorker();
        private BackgroundWorker cameraprocessingworker = new BackgroundWorker();
        // public static BackgroundWorker ArmMotionWorker = new BackgroundWorker();
        public BackgroundWorker HeadMotionWorker = new BackgroundWorker();
        public BackgroundWorker HeadTrackingWorker = new BackgroundWorker();

        public BackgroundWorker WarningSpeechWorker = new BackgroundWorker();

        public static bool customvisionprocessing = false;
        public static bool armprocessing = false;

        private static Mutex activatemutex = new Mutex();

        // Recognition model 3 was released in 2020 May.
        // It is recommended since its overall accuracy is improved
        // compared with models 1 and 2.
        const string IMAGE_BASE_URL = "https://csdx.blob.core.windows.net/resources/Face/Images/";
        const string RECOGNITION_MODEL3 = RecognitionModel.Recognition02;


        string ENDPOINT = Environment.GetEnvironmentVariable("https://robotcustomvision.cognitiveservices.azure.com/");

        Guid ID = new Guid("4e60338b-00cc-4ae6-b977-fc23bfc2598c");
        // </snippet_endpoint>

        // <snippet_keys>
        // Add your training & prediction key from the settings page of the portal
        string trainingKey = Environment.GetEnvironmentVariable("CUSTOM_VISION_TRAINING_KEY");
        string predictionKey = Environment.GetEnvironmentVariable("76fe29fc61c34d9f8c9c5c104aa0d85e");


        IList<Guid?> targetFaceIds = new List<Guid?>();
        int FrameCount = 0;
        int VisionRequestCount = 0;

        public static SpeechSynthesizer synthesizer = new SpeechSynthesizer(SpeechConfig.FromSubscription("9458ed386eb348cfb85afb8902749d9b", "eastus"));

        public static string IUPath = "Neutral";
        public static string GreetingScript = "Press the button or simply say, Hey PIXA to activate me. After you hear the remider sound, then we can start the conversation";

        public static string Default2ndUI;

        //public static UpperBodyHelper _motor = new UpperBodyHelper(System.Windows.Forms.Application.StartupPath);

        Microsoft.CognitiveServices.Speech.SpeechSynthesizer speaker = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(SpeechConfig.FromSubscription("3e2ca1b2c988405aa5ed3813caa45677", "eastasia"));


        private static DeviceClient s_deviceClient;
        private static readonly Microsoft.Azure.Devices.Client.TransportType s_transportType = Microsoft.Azure.Devices.Client.TransportType.Mqtt;

        private static string s_connectionString = "HostName=robotnetwork.azure-devices.net;DeviceId=robot1;SharedAccessKey=TOjgoBGwvDPj2MJVMTmG5NxHx+i37cbcA6OJTeZwv1g=";

        public enum AppMode
        {
            Faces,
            Emotions,
            EmotionsWithClientFaceDetect,
            Tags,
            Celebrities
        }

        public MainWindow()
        {
            this.InitializeComponent();
            this.Dispatcher.UnhandledException += this.Dispatcher_UnhandledException;
            CommandBinding cb = new CommandBinding(ApplicationCommands.Copy, this.CopyCmdExecuted, this.CopyCmdCanExecute);
            this.ConversationView.ConversationHistory.CommandBindings.Add(cb);
            //this.ActivitiesPane.CommandBindings.Add(cb);
            this.DataContext = this;
            this.player.PlaybackStopped += this.Player_PlaybackStopped;
            Services.Tracker.Configure(this.settings).Apply();

            this.renderer = new AdaptiveCardRenderer();

            HeadMotionTimer.Interval = 4000; // specify interval time as you want
            HeadMotionTimer.Elapsed += HeadMotionTimer_Elapsed;
            HeadMotionTimer.Start();
            HeadMotionTimer.AutoReset = false;

            DelayTimer.Interval = 2000; // specify interval time as you want
            DelayTimer.Elapsed += DelayTimer_Elapsed;
            //DelayTimer.Start();
            DelayTimer.AutoReset = false;

            TimeCheckingTimer.Interval = 1000;
            TimeCheckingTimer.Elapsed += TimeCheckingTimer_Elapsed;
            TimeCheckingTimer.AutoReset = true;
            //TimeCheckingTimer.Start();

            UITimer.Interval = 60000;
            UITimer.Elapsed += UITimer_Elapsed;
            UITimer.AutoReset = false;

            //var project = trainingApi.GetProject(ID);
            /*
                 Xceed Enhanced Input Package
                 This optional package enhances the Adaptive Card input controls beyond what WPF provides out of the box.
                 To enable it:
                 1. Add the NuGet package Extended.Wpf.Toolkit by Xceed to the project
                 2. Add the NuGet 
            AdaptiveCards.Rendering.Wpf.Xceed by Microsoft to the project
                 3. Uncomment the one line below
                 This option is not included here because of its license terms.
                 For more info: https://docs.microsoft.com/en-us/adaptive-cards/sdk/rendering-cards/net-wpf/getting-started
               */
            // this.renderer.UseXceedElementRenderers();

            var configFile = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "AdaptiveCardsHostConfig.json");
            if (File.Exists(configFile))
            {
                this.renderer.HostConfig = AdaptiveHostConfig.FromJson(File.ReadAllText(configFile));
            }

            //UpperBodyInit();
            //Thread.Sleep(100);
            //UpperBodyHelper.Move("Reset");
            //HeadMotionTimer.Start();
            LoadModel();
            BaseHelper.Connect();

            


            Phase5Timer.Interval = 3000;
            Phase5Timer.Elapsed += Phase5Timer_Elapsed;
            Phase5Timer.AutoReset = false;
            Phase5Timer.Start();

            Phase1Timer.Interval = 8000;
            Phase1Timer.Elapsed += Phase1Timer_Elapsed;
            Phase1Timer.AutoReset = false;
            Phase1Timer.Start();

            Phase3Timer.Interval = 10000;
            Phase3Timer.Elapsed += Phase3Timer_Elapsed;
            Phase3Timer.AutoReset = false;
            Phase3Timer.Start();

            Phase2Timer.Interval = 5000;
            Phase2Timer.Elapsed += Phase2Timer_Elapsed;
            Phase2Timer.AutoReset = false;
            Phase2Timer.Start();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }


        private void Phase4Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Photo.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = @"\" + Default2ndUI + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\SubUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.Photo.Source = bitmapSource;
                        }
                   )
             );

            this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = "\\" + IUPath + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\MainUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                   )
             );
        }

        private void Phase5Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var fileContent = string.Empty;

            string _path = System.Windows.Forms.Application.StartupPath;
            string _file = @"\default.txt";
            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
            _path += @"\MainUICustomizeFolder\";

            string fileUri = _path + _file;

            using (StreamReader reader = new StreamReader(File.OpenRead(fileUri)))
            {
                fileContent = reader.ReadToEnd();
                // Debug.WriteLine(fileContent);
                if (fileContent.Contains("+"))
                {
                    char[] separator = { '+' };
                    string[] arr = fileContent.Split(separator);
                    string data = arr[0];
                    IUPath = data;
                    GreetingScript = arr[1];
                }
            }

            var fileContent2 = string.Empty;

            string _path2 = System.Windows.Forms.Application.StartupPath;
            string _file2 = @"\default.txt";
            _path2 = _path2.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
            _path2 += @"\SubUICustomizeFolder\";

            string fileUri2 = _path2 + _file2;

            using (StreamReader reader = new StreamReader(File.OpenRead(fileUri2)))
            {
                fileContent2 = reader.ReadToEnd();
                // Debug.WriteLine(fileContent);
                Default2ndUI = fileContent2;
            }

            Phase4Timer.Interval = 2000;
            Phase4Timer.Elapsed += Phase4Timer_Elapsed;
            Phase4Timer.AutoReset = false;
            Phase4Timer.Start();
        }

        private void Phase3Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //UpperBodyInit();

            //BodyDelay.Interval = 600;
            //BodyDelay.Elapsed += BodyDelay_Elapsed;
            //BodyDelay.AutoReset = false;
            //BodyDelay.Start(); ;
        }

        private async void Phase2Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            StartCameraCapture();
        }

        private void StartCameraCapture()
        {
            cameraCaptureCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CaptureCamera(cameraCaptureCancellationTokenSource.Token), cameraCaptureCancellationTokenSource.Token);
        }

        private void StopCameraCapture() => cameraCaptureCancellationTokenSource?.Cancel();

        private async Task CaptureCamera(CancellationToken token)
        {
            if (capture == null)
                capture = new VideoCapture(CaptureDevice.DShow);

            capture.Open(1);

            if (capture.IsOpened())
            {
                while (!token.IsCancellationRequested)
                {
                    MemoryStream memoryStream = capture.RetrieveMat().Flip(FlipMode.Y).ToMemoryStream();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var imageSource = new BitmapImage();

                        imageSource.BeginInit();
                        imageSource.CacheOption = BitmapCacheOption.OnLoad;
                        imageSource.StreamSource = memoryStream;
                        imageSource.EndInit();

                        RightImage.Source = imageSource;
                    });

                    var bitmapImage = new Bitmap(memoryStream);

                    await ParseWebCamFrame(bitmapImage);
                }

                capture.Release();
            }
        }



        System.Timers.Timer BodyDelay = new System.Timers.Timer();

        private void Phase1Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //throw new NotImplementedException();
            //UpperBodyInit();

            //BodyDelay.Interval = 800;
            //BodyDelay.Elapsed += BodyDelay_Elapsed;
            //BodyDelay.AutoReset = false;
            //BodyDelay.Start();
            this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            this.Reset();
                            this.UpdateConnectionProfileInfoBlock();

                        }));
        }

        private void BodyDelay_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpperBodyHelper.Move("Reset");
            HeadMotionTimer.Start();
        }

        private void UITimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = "\\" + IUPath + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\MainUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                   )
             );
            GIFTimer.Start();
        }

        public static bool Task1Lock = false;
        public static bool Task2Lock = false;
        public static bool Task3Lock = false;
        public static bool Task4Lock = false;

        public static bool onDuty = false;

        private void TimeCheckingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime time1 = DateTime.Today.AddMinutes(30).AddHours(11);

            DateTime time2 = DateTime.Today.AddMinutes(10).AddHours(12);

            DateTime time3 = DateTime.Today.AddMinutes(30).AddHours(13);

            DateTime time4 = DateTime.Today.AddMinutes(00).AddHours(17);

            if (now > time1 && Task1Lock == false && time2 > now) //Task1 Tour Outside
            {
                Task1Lock = true;
                GlobalData.TourMode = true;
                TourHelper.GetTourInfo("Outside");
                TourHelper.CurrentArea = "Outside";
                TourHelper.GoFirstPoint();
                this.StopAnyTTSPlayback();
            }
            else if (now > time2 && Task2Lock == false && time3 > now) // Back to outside startingpoint
            {
                Task2Lock = true;
                //GlobalData.TourMode = false;
                TourHelper.CurrentArea = "Outside";
                TourHelper.TourCanceled(TourHelper.CurrentArea);

                SchedulerEnabled = false;
                IniTimer.Stop();
                TimeCheckingTimer.AutoReset = false;
                TimeCheckingTimer.Stop();
            }
            else if (now > time3 && Task3Lock == false && time4 > now)
            {
                Task3Lock = true;
                GlobalData.TourMode = true;
                TourHelper.GetTourInfo("Inside");
                TourHelper.CurrentArea = "Inside";
                TourHelper.GoFirstPoint();
                this.StopAnyTTSPlayback();
                Debug.WriteLine("Touring at office");
            }
            else if (now > time4 && Task4Lock == false)
            {
                Task4Lock = true;
                TourHelper.CurrentArea = "Inside";
                //GlobalData.TourMode = false;
                TourHelper.TourCanceled(TourHelper.CurrentArea);
                //ShutdownHelper shutdown = new ShutdownHelper();
                //shutdown.Shutdown();

                GlobalData.DutyOff = true;
            }
        }

        public static void ResetTaskScheduler()
        {
            Task1Lock = false;
            Task2Lock = false;
            Task3Lock = false;
            Task4Lock = false;
        }

        private void ArmResumeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            armprocessing = false;
        }

        public static bool StartCustomVision = false;

        private void DelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //this.MainUI.Dispatcher.Invoke(
            //       new Action(
            //            delegate
            //            {
            //                string _path = System.Windows.Forms.Application.StartupPath;
            //                string _file = "\\" + IUPath + ".jpg";
            //                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
            //                _path += @"\MainUICustomizeFolder\";

            //                Uri fileUri = new Uri(_path + _file);
            //                BitmapImage bitmapSource = new BitmapImage();

            //                bitmapSource.BeginInit();
            //                bitmapSource.CacheOption = BitmapCacheOption.None;
            //                bitmapSource.UriSource = fileUri;
            //                bitmapSource.EndInit();

            //                this.MainUI.Source = bitmapSource;
            //            }
            //       )
            // );
            string _path2 = System.Windows.Forms.Application.StartupPath;
            string _file2 = @"\default.txt";
            _path2 = _path2.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
            _path2 += @"\SubUICustomizeFolder\";

            string fileUri2 = _path2 + _file2;

            string msg = Default2ndUI;
            byte[] myByte = System.Text.Encoding.UTF8.GetBytes(msg);
            using (FileStream fsWrite = new FileStream(fileUri2, FileMode.Append))
            {
                fsWrite.Seek(0, SeekOrigin.Begin);
                fsWrite.SetLength(0);
                fsWrite.Write(myByte, 0, myByte.Length);
            };
        }

        public async void OnLoad(object sender, RoutedEventArgs e)
        {
            // Thread.Sleep(200);

            System.Windows.Forms.MessageBoxButtons mess = System.Windows.Forms.MessageBoxButtons.OKCancel;
            System.Windows.Forms.DialogResult d = System.Windows.Forms.MessageBox.Show("Do you want to follow the schedule today?", "Confirmation", mess);
            if (d == System.Windows.Forms.DialogResult.OK)
            {
                Debug.WriteLine("Confirmed");

                SchedulerEnabled = true;

                IniTimer.Interval = 20000;
                IniTimer.Elapsed += IniTimer_Elapsed;
                IniTimer.AutoReset = false;
                IniTimer.Start();
                //TimeCheckingTimer.Start();
            }
            else if (d == System.Windows.Forms.DialogResult.Cancel)
            {
                SchedulerEnabled = false;

                IniTimer.Stop();
            }

            IOTinit();
            //sendcommunicationcommand("enable");
        }

        private void IniTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimeCheckingTimer.Start();
        }

        private void Cameraprocessingworker_DoWork(object sender, DoWorkEventArgs e)
        {
            _grabber.AnalysisFunction = FacesAnalysisFunction;
        }

        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            var attrs = new List<FaceAPI.Models.FaceAttributeType> {
                FaceAPI.Models.FaceAttributeType.Age,
                FaceAPI.Models.FaceAttributeType.Gender,
                FaceAPI.Models.FaceAttributeType.HeadPose
            };

            var faces = await _faceClient.Face.DetectWithStreamAsync(jpg, returnFaceAttributes: attrs);

            GlobalData.img = jpg;
            GlobalData.facescount = faces.Count;

            VisionRequestCount++;

            if (faces.Count > 0)
            {

            }

            //// Custom Vision Detection
            if (CustomVisionisactivated == false && chatbotisrunning == false && BroadcastHelper.Broadcasting == false)
            {
                CustomVisionisactivated = true;
                MakePredictionRequest(frame.Image.ToBitmap());

                CustomVisionfunctiondelayTimer.Interval = 2000;
                CustomVisionfunctiondelayTimer.Elapsed += CustomVisionfunctiondelayTimer_Elapsed;
                CustomVisionfunctiondelayTimer.AutoReset = false;
                CustomVisionfunctiondelayTimer.Start();
            }

            return new LiveCameraResult { Faces = faces.ToArray() };
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("done");
            //CustomVisionfunctiondelayTimer.Interval = 2000;
            //CustomVisionfunctiondelayTimer.Elapsed += CustomVisionfunctiondelayTimer_Elapsed;
            //CustomVisionfunctiondelayTimer.AutoReset = false;
            //CustomVisionfunctiondelayTimer.Start();
        }

        private async void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (CustomVisionisactivated == false)
            {
                CustomVisionisactivated = true;
                //MakePredictionRequest(GlobalData.img).Wait();

                CustomVisionfunctiondelayTimer.Interval = 1000;
                CustomVisionfunctiondelayTimer.Elapsed += CustomVisionfunctiondelayTimer_Elapsed;
                CustomVisionfunctiondelayTimer.AutoReset = false;
                CustomVisionfunctiondelayTimer.Start();
            }
        }



        /// <summary> Function which submits a frame to the Emotion API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the emotions returned by the API. </returns>
        private async Task<LiveCameraResult> EmotionAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            FaceAPI.Models.DetectedFace[] faces = null;

            // See if we have local face detections for this image.
            var localFaces = (OpenCvSharp.Rect[])frame.UserData;
            if (localFaces == null || localFaces.Count() > 0)
            {
                // If localFaces is null, we're not performing local face detection.
                // Use Cognigitve Services to do the face detection.
                //Properties.Settings.Default.FaceAPICallCount++;
                faces = (await _faceClient.Face.DetectWithStreamAsync(
                    jpg,
                    returnFaceId: false,
                    returnFaceLandmarks: false,
                    returnFaceAttributes: new FaceAPI.Models.FaceAttributeType[1] { FaceAPI.Models.FaceAttributeType.Emotion })).ToArray();
            }
            else
            {
                // Local face detection found no faces; don't call Cognitive Services.
                faces = new FaceAPI.Models.DetectedFace[0];
            }

            // Output. 
            return new LiveCameraResult
            {
                Faces = faces
            };
        }

        /// <summary> Function which submits a frame to the Computer Vision API for tagging. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the tags returned by the API. </returns>
        private async Task<LiveCameraResult> TaggingAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            var tagResult = await _visionClient.TagImageInStreamAsync(jpg);
            // Count the API call. 
            //Properties.Settings.Default.VisionAPICallCount++;
            // Output. 
            return new LiveCameraResult { Tags = tagResult.Tags.ToArray() };
        }

        /// <summary> Function which submits a frame to the Computer Vision API for celebrity
        ///     detection. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the celebrities returned by the API. </returns>
        private async Task<LiveCameraResult> CelebrityAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            var domainModelResults = await _visionClient.AnalyzeImageByDomainInStreamAsync("celebrities", jpg);
            // Count the API call. 
            //Properties.Settings.Default.VisionAPICallCount++;
            // Output. 
            var jobject = domainModelResults.Result as JObject;
            var celebs = jobject.ToObject<VisionAPI.Models.CelebrityResults>().Celebrities;
            return new LiveCameraResult
            {
                // Extract face rectangles from results. 
                Faces = celebs.Select(c => CreateFace(c.FaceRectangle)).ToArray(),
                // Extract celebrity names from results. 
                CelebrityNames = celebs.Select(c => c.Name).ToArray()
            };
        }

        private void HeadMotionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Factory.StartNew(() => HeadMove());
        }

        private bool _isShowing;

        DispatcherTimer timer = new DispatcherTimer();

        public bool isShowing
        {
            get { return _isShowing; }

            set
            {
                _isShowing = value;

                if (isShowing)
                {
                    isShowing = false;

                    timer.Stop();
                    timer.Interval = TimeSpan.FromMilliseconds(45000);
                    timer.Tick += Timer_Tick;
                    timer.Start();


                }
            }
        }



        private bool _isPlaying;
        public bool isPlaying
        {
            get { return _isPlaying; }

            set
            {
                _isPlaying = value;

                if (isPlaying)
                {
                    isPlaying = false;

                    this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\records.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }
                      )
                );

                }
            }
        }

        private bool _facedelay;
        public bool facedelay
        {
            get { return _facedelay; }

            set
            {
                _facedelay = value;

                if (facedelay)
                {
                    facdelayTimer.Interval = 25000;
                    facdelayTimer.Elapsed += FacdelayTimer_Elapsed;
                    facdelayTimer.AutoReset = false;
                    facdelayTimer.Start();
                }
            }
        }

        private void FacdelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            facedelay = false;
        }

        private bool _tourinteractionactivate;

        public bool tourinteractionactivate
        {
            get { return _tourinteractionactivate; }

            set
            {
                _tourinteractionactivate = value;

                if (tourinteractionactivate)
                {
                    tourinteractionactivate = false;

                    TourHelper.TourInterrupted();
                    // ArmMotionWorker.RunWorkerAsync();
                }
            }
        }

        private bool _Returninteractionactivate;

        public bool Returninteractionactivate
        {
            get { return _Returninteractionactivate; }

            set
            {
                _Returninteractionactivate = value;

                if (Returninteractionactivate)
                {
                    Returninteractionactivate = false;

                    TourHelper.ReturnInterrupted();

                    // ArmMotionWorker.RunWorkerAsync();
                }
            }
        }

        private bool _naviinteractionactivate;

        public bool naviinteractionactivate
        {
            get { return _naviinteractionactivate; }

            set
            {
                _naviinteractionactivate = value;

                if (naviinteractionactivate)
                {
                    naviinteractionactivate = false;

                    string Designation = "";

                    if (GlobalData.userDesignation == "Male")
                    {
                        Designation = "Gentleman";
                    }
                    else if (GlobalData.userDesignation == "Female")
                    {
                        Designation = "Lady";
                    }
                    else if (GlobalData.userDesignation == "Group")
                    {
                        Designation = "Guys";
                    }

                    SynthesizeAudioAsync("Hello" + Designation + "," + GreetingScript);

                    // ArmMotionWorker.RunWorkerAsync();
                    // Task.Factory.StartNew(() => armmotionevent("Dance"));

                    NaviResumeTimer.Interval = 20000;
                    NaviResumeTimer.Elapsed += NaviResumeTimer_Elapsed;
                    NaviResumeTimer.AutoReset = false;
                    NaviResumeTimer.Start();
                }
            }
        }

        private bool _interactionactivate;

        public bool interactionactivate
        {
            get { return _interactionactivate; }

            set
            {
                _interactionactivate = value;

                if (interactionactivate)
                {
                    interactionactivate = false;

                    string Designation = "";

                    if (GlobalData.userDesignation == "Male")
                    {
                        Designation = "Gentleman";
                    }
                    else if (GlobalData.userDesignation == "Female")
                    {
                        Designation = "Lady";
                    }
                    else if (GlobalData.userDesignation == "Group")
                    {
                        Designation = "Guys";
                    }

                    SynthesizeAudioAsync("Hello" + Designation + "," + GreetingScript);

                    // ArmMotionWorker.RunWorkerAsync();
                    // Task.Factory.StartNew(() => armmotionevent("Dance"));

                    ChatbotRestartTimer.Interval = 20000;
                    ChatbotRestartTimer.Elapsed += ChatbotRestartTimer_Elapsed;
                    ChatbotRestartTimer.AutoReset = false;
                    ChatbotRestartTimer.Start();
                }
            }
        }

        private bool _activate;
        public bool activate
        {
            get { return _activate; }

            set
            {
                _activate = value;

                if (activate)
                {
                    activate = false;
                    //chatbotisrunning = true;
                    targetFaceIds.Clear();

                    if (chatbotisrunning == false)
                    {

                        string Designation = "";

                        if (GlobalData.userDesignation == "Male")
                        {
                            Designation = "Gentleman";
                        }
                        else if (GlobalData.userDesignation == "Female")
                        {
                            Designation = "Lady";
                        }
                        else if (GlobalData.userDesignation == "Group")
                        {
                            Designation = "Guys";
                        }

                        SynthesizeAudioAsync("Hello" + Designation + "," + GreetingScript);

                        //ArmMotionWorker.RunWorkerAsync();

                    }
                }
            }
        }

        public static Mutex armmotionmutex = new Mutex();

        public void armmotionevent(string motionname)
        {

            if (armprocessing == false)
            {
                armprocessing = true;

                if (CarryHelper.CarryMode == false)
                {
                    if (BroadcastHelper.Broadcasting == false)
                    {
                        this.Dispatcher.Invoke(
                                  new Action(
                                       delegate
                                       {
                                           if (motionname == "Dance")
                                           {
                                               Task.Factory.StartNew(() => Dance());
                                           }
                                           else if (motionname == "Reset")
                                           {
                                               //Task.Factory.StartNew(() => UpperBodyHelper.Move("Reset"));
                                           }
                                           else if (motionname == "Carry")
                                           {
                                               //Task.Factory.StartNew(() => UpperBodyHelper.Move("Carry"));
                                           }
                                       }
                                        )
                                    );
                    }
                }

                ArmResumeTimer.Interval = 3000;
                ArmResumeTimer.Elapsed += ArmResumeTimer_Elapsed;
                ArmResumeTimer.AutoReset = false;
                ArmResumeTimer.Start();
            }

            armmotionmutex.ReleaseMutex();

        }

        public void activateevent()
        {
            Debug.WriteLine("Entered the un-protected area");

            if (activatemutex.WaitOne(4000))
            {
                Debug.WriteLine("protected area");

                targetFaceIds.Clear();

                if (chatbotisrunning == false)
                {
                    Debug.Write("Processing");

                    string Designation = "";

                    if (GlobalData.userDesignation == "Male")
                    {
                        Designation = "Gentleman";
                    }
                    else if (GlobalData.userDesignation == "Female")
                    {
                        Designation = "Lady";
                    }
                    else if (GlobalData.userDesignation == "Group")
                    {
                        Designation = "Guys";
                    }

                    //ArmMotionWorker.RunWorkerAsync();
                    // Task.Factory.StartNew(() => armmotionevent("Dance"));

                    string str;
                    string script = "Hello, Welcome to ask me any questions";

                   

                    str = "<speak version=\"1.0\"";
                    str += " xmlns=\"http://www.w3.org/2001/10/synthesis\"";
                    str += " xml:lang=\"zh-CN\">";
                    str += "<voice name =\"zh-CN-XiaoyouNeural\">";
                    str += script;
                    str += "</voice>";
                    str += "</speak>";
                    speaker.SpeakSsmlAsync(str);
                    Debug.WriteLine("Leaving the area");

                    activatemutex.ReleaseMutex();
                }
            }
            else
            {
                Debug.WriteLine("RequestDenied");
            }
        }

        public static Mutex interactionactivateMutex = new Mutex();

        public void interactionactivate_event()
        {
            if (interactionactivateMutex.WaitOne(4000))
            {
                BaseHelper.CancelNavigation();
                GlobalData.personinfront_standby = true;

                string Designation = "";

                if (GlobalData.userDesignation == "Male")
                {
                    Designation = "Gentleman";
                }
                else if (GlobalData.userDesignation == "Female")
                {
                    Designation = "Lady";
                }
                else if (GlobalData.userDesignation == "Group")
                {
                    Designation = "Guys";
                }

                // ArmMotionWorker.RunWorkerAsync();
                // Task.Factory.StartNew(() => armmotionevent("Dance"));

                SynthesizeAudioAsync("Hello" + Designation + ", " + GreetingScript).Wait();

                ChatbotRestartTimer.Interval = 20000;
                ChatbotRestartTimer.Elapsed += ChatbotRestartTimer_Elapsed;
                ChatbotRestartTimer.AutoReset = false;
                ChatbotRestartTimer.Start();

                interactionactivateMutex.ReleaseMutex();
            }
        }

        public static Mutex tourinteractionmutex = new Mutex();

        public void tourinteractionactivateevent()
        {
            // ArmMotionWorker.RunWorkerAsync();

            if (tourinteractionmutex.WaitOne(3000))
            {
                GlobalData.personinfront_tour = true;

                // Task.Factory.StartNew(() => armmotionevent("Dance"));

                TourHelper.TourInterrupted();

                tourinteractionmutex.ReleaseMutex();
            }

        }

        public static Mutex Returninteractionactivatemutex = new Mutex();

        public void Returninteractionactivate_event()
        {
            if (Returninteractionactivatemutex.WaitOne(3000))
            {
                GlobalData.personinfront_tour = true;

                // Task.Factory.StartNew(() => armmotionevent("Dance"));

                TourHelper.ReturnInterrupted();

                Returninteractionactivatemutex.ReleaseMutex();
            }
        }

        public static bool chatbotisrunning = false;

        private bool _CustomVisionisactivated;

        public bool CustomVisionisactivated = false;
        //{
        //    get { return _CustomVisionisactivated; }

        //    set
        //    {
        //        _CustomVisionisactivated = value;

        //        if (CustomVisionisactivated)
        //        {
        //            //CustomVisionfunctiondelayTimer.Interval = 3000;
        //            //CustomVisionfunctiondelayTimer.Elapsed += CustomVisionfunctiondelayTimer_Elapsed;
        //            //CustomVisionfunctiondelayTimer.AutoReset = false;
        //            //CustomVisionfunctiondelayTimer.Start();
        //        }
        //    }
        //}

        private void CustomVisionfunctiondelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //CustomVisionfunctiondelayTimer.Stop();
            CustomVisionisactivated = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            initUI();
        }
        public void initUI()
        {
            this.Photo.Dispatcher.Invoke(
       new Action(
            delegate
            {

                string _path = System.Windows.Forms.Application.StartupPath;
                string _file = @"\up.jpeg";
                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                _path += @"\ChatImg\";

                Uri fileUri = new Uri(_path + _file);
                BitmapImage bitmapSource = new BitmapImage();

                bitmapSource.BeginInit();
                bitmapSource.CacheOption = BitmapCacheOption.None;
                bitmapSource.UriSource = fileUri;
                bitmapSource.EndInit();

                this.Photo.Visibility = Visibility.Hidden;


                Photo.Source = bitmapSource;
            }
                )
            );
        }


        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the window title string, which includes the assembly version number.
        /// To update the assembly version number, edit this line in VoiceAssistantClient\Properties\AssemblyInfo.cs:
        ///     [assembly: AssemblyVersion("#.#.#.#")]
        /// Or in VS, right click on the VoiceAssistantClient project -> properties -> Assembly Information.
        /// Microsoft Version number is: [Major Version, Minor Version, Build Number, Revision]
        /// (see https://docs.microsoft.com/en-us/dotnet/api/system.version).
        /// Per GitHub guidance, we use Semantic Versioning with [Major, Minor, Patch], so we ignore
        /// the last number and treat the Build Number as the Patch (see https://semver.org/).
        /// </summary>
        public static string WindowTitle
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return $"Windows Voice Assistant Client v{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        public ObservableCollection<MessageDisplay> Messages { get; private set; } = new ObservableCollection<MessageDisplay>();

        public ObservableCollection<ActivityDisplay> Activities { get; private set; } = new ObservableCollection<ActivityDisplay>();

        public ListenState ListeningState
        {
            get
            {
                return this.listening;
            }

            private set
            {
                this.listening = value;
                this.OnPropertyChanged(nameof(this.ListeningState));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (this.connector != null)
            {
                this.connector.Dispose();
            }

            if (this.player != null)
            {
                this.player.Dispose();
            }

            base.OnClosed(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Connecting and using the client requires providing a speech subscription key along
            // with the region for that subscription or, for development against a specific custom
            // URL, a URL override. If the client doesn't meet these requirements (e.g. on first
            // run), pop up the settings dialog to prompt for it.
            var hasSubscriptionKey = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.SubscriptionKey);
            var hasSubscriptionRegion = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.SubscriptionKeyRegion);
            var hasUrlOverride = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.UrlOverride);

            if (!hasSubscriptionKey || (!hasSubscriptionRegion && !hasUrlOverride))
            {
                var settingsDialog = new SettingsDialog(this.settings.RuntimeSettings);
                bool succeeded;
                succeeded = settingsDialog.ShowDialog() ?? false;

                if (!succeeded)
                {
                    this.Close();
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            // Set this here as opposed to XAML since we do not do a full binding
            //this.CustomActivityCollectionCombo.ItemsSource = this.settings.DisplaySettings.CustomPayloadData;
            //this.CustomActivityCollectionCombo.DisplayMemberPath = "Name";
            //this.CustomActivityCollectionCombo.SelectedValuePath = "Name";

            //base.OnActivated(e);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            this.ShowException(e.Exception);
            e.Handled = true;
        }

        private void ShowException(Exception e)
        {
            this.RunOnUiThread(() =>
            {
                Debug.WriteLine(e);
                this.Messages.Add(new MessageDisplay($"App Error (see log for details): {Environment.NewLine} {e.Source} : {e.Message}", Sender.Channel));
                var trace = new Microsoft.Bot.Schema.Activity
                {
                    Type = "Exception",
                    Value = e,
                };
                this.Activities.Add(new ActivityDisplay(JsonConvert.SerializeObject(trace), trace, Sender.Channel));
            });
        }

        /// <summary>
        /// The method reads user-entered settings and creates a new instance of the DialogServiceConnector object
        /// when the "Reconnect" button is pressed (or the microphone button is pressed for the first time).
        /// </summary>
        private void InitSpeechConnector()
        {
            DialogServiceConfig config = null;

            var hasSubscription = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.SubscriptionKey);
            var hasRegion = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.SubscriptionKeyRegion);
            var hasBotId = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.BotId);
            var hasUrlOverride = !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.UrlOverride);

            if (hasSubscription && (hasRegion || hasUrlOverride))
            {
                if (!string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.CustomCommandsAppId))
                {
                    // NOTE: Custom commands is a preview Azure Service.
                    // Set the custom commands configuration object based on three items:
                    // - The Custom commands application ID
                    // - Cognitive services speech subscription key.
                    // - The Azure region of the subscription key(e.g. "westus").
                    config = CustomCommandsConfig.FromSubscription(this.settings.RuntimeSettings.Profile.CustomCommandsAppId, this.settings.RuntimeSettings.Profile.SubscriptionKey, this.settings.RuntimeSettings.Profile.SubscriptionKeyRegion);
                }
                else if (hasBotId)
                {
                    config = BotFrameworkConfig.FromSubscription(this.settings.RuntimeSettings.Profile.SubscriptionKey, this.settings.RuntimeSettings.Profile.SubscriptionKeyRegion, this.settings.RuntimeSettings.Profile.BotId);
                }
                else
                {
                    // Set the bot framework configuration object based on two items:
                    // - Cognitive services speech subscription key. It is needed for billing and is tied to the bot registration.
                    // - The Azure region of the subscription key(e.g. "westus").
                    config = BotFrameworkConfig.FromSubscription(this.settings.RuntimeSettings.Profile.SubscriptionKey, this.settings.RuntimeSettings.Profile.SubscriptionKeyRegion);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.ConnectionLanguage))
            {
                // Set the speech recognition language. If not set, the default is "en-us".
                config.Language = this.settings.RuntimeSettings.Profile.ConnectionLanguage;
            }

            if (this.settings.RuntimeSettings.Profile.CustomSpeechEnabled)
            {
                // Set your custom speech end-point id here, as given to you by the speech portal https://speech.microsoft.com/portal.
                // Otherwise the standard speech end-point will be used.
                config.SetServiceProperty("cid", this.settings.RuntimeSettings.Profile.CustomSpeechEndpointId, ServicePropertyChannel.UriQueryParameter);

                // Custom Speech does not support cloud Keyword Verification at the moment. If this is not done, there will be an error
                // from the service and connection will close. Remove line below when supported.
                config.SetProperty("KeywordConfig_EnableKeywordVerification", "false");
            }

            if (this.settings.RuntimeSettings.Profile.VoiceDeploymentEnabled)
            {
                // Set one or more IDs associated with the custom TTS voice your bot will use
                // The format of the string is one or more GUIDs separated by comma (no spaces). You get these GUIDs from
                // your custom TTS on the speech portal https://speech.microsoft.com/portal.
                config.SetProperty(PropertyId.Conversation_Custom_Voice_Deployment_Ids, this.settings.RuntimeSettings.Profile.VoiceDeploymentIds);
            }

            if (!string.IsNullOrEmpty(this.settings.RuntimeSettings.Profile.FromId))
            {
                // Set the from.id in the Bot-Framework Activity sent by this tool.
                // from.id field identifies who generated the activity, and may be required by some bots.
                // See https://github.com/microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md
                // for Bot Framework Activity schema and from.id.
                config.SetProperty(PropertyId.Conversation_From_Id, this.settings.RuntimeSettings.Profile.FromId);
            }

            if (!string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.LogFilePath))
            {
                // Speech SDK has verbose logging to local file, which may be useful when reporting issues.
                // Supply the path to a text file on disk here. By default no logging happens.
                config.SetProperty(PropertyId.Speech_LogFilename, this.settings.RuntimeSettings.Profile.LogFilePath);
            }

            if (hasUrlOverride)
            {
                // For prototyping new Direct Line Speech channel service feature, a custom service URL may be
                // provided by Microsoft and entered in this tool.
                config.SetProperty("SPEECH-Endpoint", this.settings.RuntimeSettings.Profile.UrlOverride);
            }

            if (!string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.ProxyHostName) &&
                !string.IsNullOrWhiteSpace(this.settings.RuntimeSettings.Profile.ProxyPortNumber) &&
                int.TryParse(this.settings.RuntimeSettings.Profile.ProxyPortNumber, out var proxyPortNumber))
            {
                // To funnel network traffic via a proxy, set the host name and port number here
                config.SetProxy(this.settings.RuntimeSettings.Profile.ProxyHostName, proxyPortNumber, string.Empty, string.Empty);
            }

            // If a the DialogServiceConnector object already exists, destroy it first
            if (this.connector != null)
            {
                // First, unregister all events
                this.connector.ActivityReceived -= this.Connector_ActivityReceived;
                this.connector.Recognizing -= this.Connector_Recognizing;
                this.connector.Recognized -= this.Connector_Recognized;
                this.connector.Canceled -= this.Connector_Canceled;
                this.connector.SessionStarted -= this.Connector_SessionStarted;
                this.connector.SessionStopped -= this.Connector_SessionStopped;

                // Then dispose the object
                this.connector.Dispose();
                this.connector = null;
            }

            // Create a new Dialog Service Connector for the above configuration and register to receive events
            this.connector = new DialogServiceConnector(config, AudioConfig.FromDefaultMicrophoneInput());
            this.connector.ActivityReceived += this.Connector_ActivityReceived;

            this.connector.ActivityReceived += async (sender, activityReceivedEventArgs) =>
            {

                dynamic activity = JsonConvert.DeserializeObject(activityReceivedEventArgs.Activity);
                var value = activity?.value != null ? activity.value.ToString() : string.Empty;
                var name = activity?.name != null ? activity.name.ToString() : string.Empty;

                if (name.Equals("Image"))
                {
                    string content = value;
                    char[] separator = { '"' };
                    string[] arr = content.Split(separator);
                    string data = arr[3];

                    this.Photo.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = data + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\ChatImg\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.Photo.Visibility = Visibility.Visible;
                            this.Photo.Source = bitmapSource;

                            this.isShowing = true;
                        }
                   )
             );


                }

                else if (name.Equals("Navi"))
                {
                    //MessageBox.Show("Navigation Request Detected");
                    string content = value;
                    char[] separator = { '"' };
                    string[] arr = content.Split(separator);
                    string data = arr[3];

                    GlobalData.isNavigating = true;
                    GlobalData.RobotisReturning = false;
                    GlobalData.goallocation = data;
                    GlobalData.Navitothegoalposition = true;

                    ChatbotRestartTimer.Stop();
                    NaviResumeTimer.Stop();

                    if (GlobalData.TourMode)
                    {
                        TourHelper.ResumeTimer.Stop();
                        TourHelper.ReturnTimer.Stop();
                        TourHelper.TourisInterruptedbyNavi = true;
                    }

                    BaseHelper.Go(data);
                }

                else if (name.Equals("Action"))
                {
                    // ArmMotionWorker.RunWorkerAsync();
                    // Task.Factory.StartNew(() => armmotionevent("Dance"));
                }

                else if (name.Equals("Record"))
                {
                    //MessageBox.Show(GlobalData.Questions);
                    DataRecoder.WritetoDatabase(GlobalData.Questions);
                }

                else if (name.Equals("TourMode"))
                {
                    string content = value;
                    char[] separator = { '"' };
                    string[] arr = content.Split(separator);
                    string data = arr[3];

                    GlobalData.TourMode = true;
                    TourHelper.GetTourInfo();
                    TourHelper.GoFirstPoint();
                    this.StopAnyTTSPlayback();

                    //BaseHelper.Go(GlobalData.LocationList[0]);
                }

                else if (name.Equals("CancelTourMode"))
                {
                    // GlobalData.TourMode = false;
                    TourHelper.TourCanceled();
                }

                else if (name.Equals("CarryMode"))
                {
                    CarryHelper.CarryMode = true;
                    //Task.Factory.StartNew(() => UpperBodyHelper.Move("Carry"));
                }

                else if (name.Equals("CancleCarryMode"))
                {
                    CarryHelper.CarryMode = true;
                    // Task.Factory.StartNew(() => UpperBodyHelper.Move("Reset"));
                }

                else if (name.Equals("CancleBroadcastingMode"))
                {
                    BroadcastHelper.Stop();
                    ShowDefaultImage = true;
                }

                else if (name.Equals("BroadcastingMode"))
                {
                    if (BroadcastHelper.Broadcasting == false)
                    {
                        BroadcastHelper.GetBroadcastInfo();
                        ShowImage = true;
                        BroadcastHelper.StartBroadcasting();
                    }
                }


            };
            this.connector.Recognizing += this.Connector_Recognizing;
            this.connector.Recognized += this.Connector_Recognized;
            this.connector.Canceled += this.Connector_Canceled;
            this.connector.SessionStarted += this.Connector_SessionStarted;
            this.connector.SessionStopped += this.Connector_SessionStopped;

            // Open a connection to Direct Line Speech channel
            this.connector.ConnectAsync();

            if (this.settings.RuntimeSettings.Profile.CustomSpeechEnabled)
            {
                this.customSpeechConfig = new CustomSpeechConfiguration(this.settings.RuntimeSettings.Profile.CustomSpeechEndpointId);
            }

            if (this.settings.RuntimeSettings.Profile.WakeWordEnabled)
            {
                // Configure wake word (also known as "keyword")
                this.activeWakeWordConfig = new WakeWordConfiguration(this.settings.RuntimeSettings.Profile.WakeWordPath);
                this.connector.StartKeywordRecognitionAsync(this.activeWakeWordConfig.WakeWordModel);
            }
        }

        private void Connector_SessionStopped(object sender, SessionEventArgs e)
        {
            var message = "Stopped listening";

            Debug.WriteLine($"SessionStopped event, id = {e.SessionId}");

            if (this.settings.RuntimeSettings.Profile.WakeWordEnabled)
            {
                message = "Stopped actively listening - waiting for wake word";
            }

            this.UpdateStatus(message);
            this.RunOnUiThread(() => this.ListeningState = ListenState.NotListening);

            targetFaceIds.Clear();

            GIFTimer.Stop();
            UITimer.Start();

            if (BroadcastHelper.Broadcasting)
            {
                BroadcastHelper.Suspended = false;
                BroadcastHelper.ResumeTimer_Start();
            }

            if (GlobalData.RobotisReturning == true && GlobalData.TourMode == false)
            {
                ChatbotRestartTimer.Interval = 8000;
                ChatbotRestartTimer.Elapsed += ChatbotRestartTimer_Elapsed;
                ChatbotRestartTimer.AutoReset = false;
                ChatbotRestartTimer.Start();
            }
            else if (GlobalData.RobotisReturning == false && GlobalData.Navigating == true && GlobalData.TourMode == false)
            {
                NaviResumeTimer.Interval = 8000;
                NaviResumeTimer.Elapsed += NaviResumeTimer_Elapsed;
                NaviResumeTimer.AutoReset = false;
                NaviResumeTimer.Start();
            }

            if (GlobalData.TourMode)
            {
                if (!TourHelper.TourisInterruptedbyNavi)
                {
                    if (TourHelper.BacktoStandyLocation == false)
                    {
                        TourHelper.ResumeTimer.Interval = 8000;
                        TourHelper.ResumeTimer.Elapsed += TourHelper.ResumeTimer_Elapsed;
                        TourHelper.ResumeTimer.AutoReset = false;
                        TourHelper.ResumeTimer.Start();
                    }
                    else if (TourHelper.BacktoStandyLocation == true)
                    {
                        TourHelper.ReturnTimer.Interval = 8000;
                        TourHelper.ReturnTimer.Elapsed += TourHelper.ReturnTimer_Elapsed;
                        TourHelper.ReturnTimer.AutoReset = false;
                        TourHelper.ReturnTimer.Start();
                    }
                }
            }

            //chatbotisrunning = false;
            this.FacefunctiondelayTimer.Interval = 15000;
            this.FacefunctiondelayTimer.Elapsed += FacefunctiondelayTimer_Elapsed;
            this.FacefunctiondelayTimer.AutoReset = false;
            this.FacefunctiondelayTimer.Start();
        }

        private void NaviResumeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GlobalData.isNavigating = true;
            BaseHelper.Go(GlobalData.goallocation);

            FaceResumeTimer.Interval = 3000;
            FaceResumeTimer.Elapsed += FaceResumeTimer_Elapsed;
            FaceResumeTimer.AutoReset = false;
            FaceResumeTimer.Start();
        }

        private void ChatbotRestartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GlobalData.goallocation = GlobalData.startlocation;
            GlobalData.isNavigating = true;
            GlobalData.RobotisReturning = true;
            BaseHelper.Go(GlobalData.goallocation);

            FaceResumeTimer.Interval = 3000;
            FaceResumeTimer.Elapsed += FaceResumeTimer_Elapsed;
            FaceResumeTimer.AutoReset = false;
            FaceResumeTimer.Start();
        }

        private void FaceResumeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GlobalData.personinfront_standby = false;
        }

        private void FacefunctiondelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            chatbotisrunning = false;
            FrameCount = 0;
            targetFaceIds.Clear();
            //this.FacefunctiondelayTimer.Start();
        }

        private void Connector_SessionStarted(object sender, SessionEventArgs e)
        {
            synthesizer.StopSpeakingAsync();

            chatbotisrunning = true;

            this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            this.MainUI.Source = null;

                        }));

            GIFTimer.Stop();
            UITimer.Start();

            UITimer.Stop();
            GIFTimer.Stop();

            //synthesizer.StopSpeakingAsync();

            if (BroadcastHelper.Broadcasting)
            {
                BroadcastHelper.BroadcastingSuspend();
                BroadcastHelper.BroadcastInterrupted = true;
                BroadcastHelper.ResumeTimer_Stop();
            }

            if ((GlobalData.RobotisReturning == true || GlobalData.Navigating == true) && GlobalData.TourMode == false)
            {
                ChatbotRestartTimer.Stop();
                NaviResumeTimer.Stop();
                BaseHelper.CancelNavigation();
            }

            if (GlobalData.TourMode)
            {
                BaseHelper.CancelNavigation();
                TourHelper.ResumeTimer.Stop();
                TourHelper.ReturnTimer.Stop();
            }

            // chatbotisrunning = true;
            GlobalData.NaviIsCanceled = true;

            this.isPlaying = true;

            targetFaceIds.Clear();

            Debug.WriteLine($"SessionStarted event, id = {e.SessionId}");
            this.UpdateStatus("Listening ...");
            this.player.Stop();
            this.RunOnUiThread(() => this.ListeningState = ListenState.Listening);
        }

        private void Connector_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason == CancellationReason.Error
                && e.ErrorCode == CancellationErrorCode.ConnectionFailure
                && e.ErrorDetails.Contains("1000"))
            {
                // Connection was closed by the remote host.
                // Error code: 1000.
                // Error details: Exceeded maximum websocket connection idle duration (>300000ms = 5 minutes).
                // A graceful timeout after a connection is idle manifests as an error but isn't an
                // exceptional condition -- we don't want it show up as a big red bubble!
                this.UpdateStatus("Active connection timed out but ready to reconnect on demand.");
            }
            else
            {
                var statusMessage = $"Error ({e.ErrorCode}) : {e.ErrorDetails}";
                //this.UpdateStatus(statusMessage);
                this.RunOnUiThread(() =>
                {
                    this.ListeningState = ListenState.NotListening;
                    //this.Messages.Add(new MessageDisplay(statusMessage, Sender.Channel));
                });

                Thread.Sleep(100);

                this.StopAnyTTSPlayback();

                if (this.ListeningState == ListenState.NotListening)
                {
                    this.StartListening();
                }

            }
        }

        private void Connector_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            //speaker.StopSpeakingAsync();

            System.Diagnostics.Debug.WriteLine($"Connector_Recognized ({e.Result.Reason}): {e.Result.Text}");
            this.RunOnUiThread(() =>
            {
                this.UpdateStatus(string.Empty);
                if (!string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    this.Messages.Add(new MessageDisplay(e.Result.Text, Sender.User));
                    this.ConversationView.ConversationHistory.ScrollIntoView(this.ConversationView.ConversationHistory.Items[this.ConversationView.ConversationHistory.Items.Count - 1]);
                    GlobalData.Questions = e.Result.Text;
                }
            });
        }

        private void Connector_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            //speaker.StopSpeakingAsync();

            this.UpdateStatus(e.Result.Text, tentative: true);
        }

        private void Connector_ActivityReceived(object sender, ActivityReceivedEventArgs e)
        {
            var json = e.Activity;
            var activity = JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Activity>(json);

            if (e.HasAudio && activity.Speak != null)
            {
                var audio = e.Audio;
                var stream = new ProducerConsumerStream();

                Task.Run(() =>
                {
                    var buffer = new byte[800];
                    uint bytesRead = 0;
                    while ((bytesRead = audio.Read(buffer)) > 0)
                    {
                        stream.Write(buffer, 0, (int)bytesRead);
                    }
                }).Wait();

                var channelData = activity.GetChannelData<SpeechChannelData>();
                var id = channelData?.ConversationalAiData?.RequestInfo?.InteractionId;
                if (!string.IsNullOrEmpty(id))
                {
                    System.Diagnostics.Debug.WriteLine($"Expecting TTS stream {id}");
                }

                var wavStream = new RawSourceWaveStream(stream, new WaveFormat(16000, 16, 1));
                this.playbackStreams.Enqueue(new WavQueueEntry(id, false, stream, wavStream));

                if (this.player.PlaybackState != PlaybackState.Playing)
                {
                    Task.Run(() => this.PlayFromAudioQueue());
                }
            }

            List<AdaptiveCard> cardsToBeRendered = new List<AdaptiveCard>();
            if (activity.Attachments?.Any() is true)
            {
                cardsToBeRendered = activity.Attachments
                    .Where(x => x.ContentType == AdaptiveCard.ContentType)
                    .Select(x =>
                    {
                        try
                        {
                            var parseResult = AdaptiveCard.FromJson(x.Content.ToString());
                            return parseResult.Card;
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception ex)
                        {
                            this.ShowException(ex);
                            return null;
                        }
#pragma warning restore CA1031 // Do not catch general exception types
                    })
                    .Where(x => x != null)
                    .ToList();
            }

            this.RunOnUiThread(() =>
            {
                this.Activities.Add(new ActivityDisplay(json, activity, Sender.Bot));
                if (activity.Type == ActivityTypes.Message || cardsToBeRendered?.Any() == true)
                {
                    var renderedCards = cardsToBeRendered.Select(x =>
                    {
                        var rendered = this.renderer.RenderCard(x);
                        rendered.OnAction += this.RenderedCard_OnAction;
                        rendered.OnMediaClicked += this.RenderedCard_OnMediaClicked;
                        return rendered?.FrameworkElement;
                    });
                    this.Messages.Add(new MessageDisplay(activity.Text, Sender.Bot, renderedCards));
                    this.ConversationView.ConversationHistory.ScrollIntoView(this.ConversationView.ConversationHistory.Items[this.ConversationView.ConversationHistory.Items.Count - 1]);
                }
            });
        }

        private void RenderedCard_OnMediaClicked(RenderedAdaptiveCard sender, AdaptiveMediaEventArgs e)
        {
            MessageBox.Show(this, JsonConvert.SerializeObject(e.Media), "Host received Media");
        }

        private void RenderedCard_OnAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs e)
        {
            if (e.Action is AdaptiveOpenUrlAction openUrlAction)
            {
                Process.Start(openUrlAction.Url.AbsoluteUri);
            }
            else if (e.Action is AdaptiveSubmitAction submitAction)
            {
                var inputs = sender.UserInputs.AsJson();

                // Merge the Action.Submit Data property with the inputs
                inputs.Merge(submitAction.Data);

                MessageBox.Show(this, JsonConvert.SerializeObject(inputs, Formatting.Indented), "SubmitAction");
            }
        }

        private void SwitchToNewBotEndpoint()
        {
            this.Reset();
            this.Messages.Add(new MessageDisplay("Switched to updated Bot Endpoint", Sender.Channel));
        }

        private void Reset()
        {
            this.Messages.Clear();
            this.Activities.Clear();
            this.ListeningState = ListenState.NotListening;
            this.UpdateStatus("New conversation started");
            this.StopAnyTTSPlayback();
            this.InitSpeechConnector();

            var message = "Press the button, or say 'Hey Julee' to activate me";
            if (this.settings.RuntimeSettings.Profile.WakeWordEnabled)
            {
                message = $"Press the button, or say 'Hey Julee' to activate me";
            }

            this.UpdateStatus(message);
        }

        private void Reconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            this.UpdateConnectionProfileInfoBlock();


            //GlobalData.TourMode = true;
            //TourHelper.GetTourInfo();
            //Thread.Sleep(5000);
            //TourHelper.GoFirstPoint();

            //if (BroadcastHelper.Broadcasting == false)
            //{
            //    BroadcastHelper.GetBroadcastInfo();
            //    BroadcastHelper.StartBroadcasting();
            //}

            //ShowImage = true;
            //this.StopAnyTTSPlayback();

            //chatbotisrunning = true;

            //if (GlobalData.waitingatthegoalposition == false)
            //{
            //    if (this.ListeningState == ListenState.NotListening)
            //    {
            //        this.StartListening();
            //    }
            //    else
            //    {
            //        //Todo: canceling listening not supported
            //    }
            //}
        }

        private void StartListening()
        {
            if (this.ListeningState == ListenState.NotListening)
            {
                if (this.connector == null)
                {
                    this.InitSpeechConnector();
                }

                try
                {
                    this.ListeningState = ListenState.Initiated;

                    this.connector.ListenOnceAsync();
                    System.Diagnostics.Debug.WriteLine("Started ListenOnceAsync");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    this.ShowException(ex);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            this.StopAnyTTSPlayback();

            chatbotisrunning = true;

            if (GlobalData.waitingatthegoalposition == false)
            {
                if (this.ListeningState == ListenState.NotListening)
                {
                    this.StartListening();
                }
                else
                {
                    //Todo: canceling listening not supported
                }
            }

        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count == 0)
                {
                    return;
                }

                var entry = this.playbackStreams.Dequeue();
                entry.Stream.Close();
            }

            if (!this.PlayFromAudioQueue())
            {
                if (this.Activities.LastOrDefault(x => x.Activity.Type == ActivityTypes.Message)
                    ?.Activity?.AsMessageActivity()?.InputHint == InputHints.ExpectingInput)
                {
                    this.StartListening();
                }
            }
        }

        public void StopAnyTTSPlayback()
        {
            lock (this.playbackStreams)
            {
                this.playbackStreams.Clear();
            }

            if (this.player.PlaybackState == PlaybackState.Playing)
            {
                this.player.Stop();
            }
        }

        private void StatusBox_KeyUp(object sender, KeyEventArgs e)
        {
            this.StopAnyTTSPlayback();
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            if (this.connector == null)
            {
                this.InitSpeechConnector();
            }

            var bfActivity = Microsoft.Bot.Schema.Activity.CreateMessageActivity();
            bfActivity.Text = this.statusBox.Text;
            if (!string.IsNullOrEmpty(this.settings.RuntimeSettings.Profile.FromId))
            {
                bfActivity.From = new ChannelAccount(this.settings.RuntimeSettings.Profile.FromId);
            }

            this.statusBox.Clear();
            var jsonConnectorActivity = JsonConvert.SerializeObject(bfActivity);
            this.Messages.Add(new MessageDisplay(bfActivity.Text, Sender.User));
            this.Activities.Add(new ActivityDisplay(jsonConnectorActivity, bfActivity, Sender.User));

            this.ConversationView.ConversationHistory.ScrollIntoView(this.ConversationView.ConversationHistory.Items[this.ConversationView.ConversationHistory.Items.Count - 1]);
        }

        private void ExportActivityLog_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*",
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllLines(
                    dialog.FileName,
                    this.Messages.Select(x => x.ToString()).Concat(
                        this.Activities.Select(x => x.ToString()).ToList()).ToArray());
            }
        }

        private void CopyCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            string copyContent = string.Empty;
            if (e.OriginalSource is ListBox lb)
            {
                foreach (var item in lb.SelectedItems)
                {
                    copyContent += item.ToString();
                    copyContent += Environment.NewLine;
                }
            }
            else if (e.OriginalSource is JsonViewerControl.JsonViewer jv)
            {
                copyContent = jv.SelectedItem?.ToString();
            }

            if (copyContent != null)
            {
                Clipboard.SetText(copyContent);
            }
        }

        private void CopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is ListBox lb)
            {
                if (lb.SelectedItems.Count > 0)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
            else if (e.OriginalSource is JsonViewerControl.JsonViewer)
            {
                e.CanExecute = true;
            }
        }

        private void RunOnUiThread(Action action)
        {
            this.statusBox.Dispatcher.InvokeAsync(action);
        }

        private void UpdateStatus(string msg, bool tentative = true)
        {
            if (Thread.CurrentThread != this.statusOverlay.Dispatcher.Thread)
            {
                this.RunOnUiThread(() =>
                {
                    this.UpdateStatus(msg, tentative);
                    if (this.ConversationView.ConversationHistory.Items.Count > 0)
                    {
                        this.ConversationView.ConversationHistory.ScrollIntoView(this.ConversationView.ConversationHistory.Items[this.ConversationView.ConversationHistory.Items.Count - 1]);
                    }

                });
                return;
            }

            const string pad = "   ";

            if (tentative)
            {
                this.statusOverlay.Text = pad + msg;
            }
            else
            {
                this.statusBox.Clear();
                this.statusBox.Text = pad + msg;
            }
        }

        private bool PlayFromAudioQueue()
        {
            WavQueueEntry entry = null;
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count > 0)
                {
                    entry = this.playbackStreams.Peek();
                }
            }

            if (entry != null)
            {
                System.Diagnostics.Debug.WriteLine($"START playing {entry.Id}");
                this.player.Init(entry.Reader);
                this.player.Play();
                return true;
            }

            return false;
        }

        private void BotEndpoint_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Reset();
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog(this.settings.RuntimeSettings);
            var succeeded = settingsDialog.ShowDialog();
            //if (BroadcastHelper.Broadcasting == false)
            //{
            //    BroadcastHelper.GetBroadcastInfo();
            //    ShowImage = true;
            //    BroadcastHelper.StartBroadcasting();
            //}

            // BUGBUG: Do not call reset, leave it for later as this is usually the first action.
        }

        private void UpdateConnectionProfileInfoBlock()
        {
            var settingsDialog = new SettingsDialog(this.settings.RuntimeSettings);

            var connectionProfileName = !string.IsNullOrWhiteSpace(settingsDialog.ConnectionProfileName);

            if (connectionProfileName)
            {
                //this.ConnectionProfileInfoBlock.Text = $"Connection Profile: {settingsDialog.ConnectionProfileName}";
            }
        }

        private void FunctionBtn_Click(object sender, RoutedEventArgs e)
        {

            //PostureManager postureManager = new PostureManager();
            //panel1.Children.Add(postureManager);
            //NavigationManager navigationManager = new NavigationManager();
            //panel1.Children.Add(navigationManager);
        }


        private void statusBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void UpperBodyInit()
        {
            //// all motor ids used in the robot (you can check in roboplus - dynamixel wizard
            //var motorIds = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            //// establish connection with the dynamixel chain
            ////UpperBodyHelper.Initialise(3, 1, motorIds);

            //try
            //{
            //    bool connected = _motor.Connect("COM7", 115200.ToString());
            //    if (connected)
            //    {
            //        MessageBox.Show("Connected successfully!");
            //    }
            //    else
            //    {
            //        MessageBox.Show("Connect failed!");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Connect failed: " + ex.Message);
            //}

            // all motor ids used in the robot (you can check in roboplus - dynamixel wizard
            var motorIds = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            // establish connection with the dynamixel chain
            UpperBodyHelper.Initialise(3, 1, motorIds);
        }

        private void ConversationView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public static Mutex Headmutex = new Mutex();

        private void HeadMove()
        {
            if (Headmutex.WaitOne(6000))
            {
                HeadMotionTimer.Stop();

                if (GlobalData.facescount < 1)
                {
                    Task.Factory.StartNew(() => HeadStaticMotion());
                }

                else if (GlobalData.facescount >= 1)
                {
                    Task.Factory.StartNew(() => HeadTracking());
                }

                HeadMotionTimer.Start();

                Headmutex.ReleaseMutex();
            }
        }

        private void HeadTrackingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HeadMotionTimer.Start();
        }

        public void HeadTracking()
        {
            int position;

            if (GlobalData.facemidpoint.X >= 272)
            {
                position = Convert.ToInt32((GlobalData.facemidpoint.X - 272) * 0.9);

                if (position >= 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.HeadMove(3096 - position));
                    //  UpperBodyHelper.HeadMove(3096 - position);
                }
            }
            else if (GlobalData.facemidpoint.X < 272 && GlobalData.facemidpoint.X > 0)
            {
                position = Convert.ToInt32((272 - GlobalData.facemidpoint.X) * 0.9);

                if (position >= 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.HeadMove(3096 + position));
                    // UpperBodyHelper.HeadMove(3096 + position);
                }
            }

            int neckposition;

            if (GlobalData.facemidpoint.Y >= 0 && GlobalData.facemidpoint.Y <= 200)
            {
                neckposition = Convert.ToInt32((GlobalData.facemidpoint.Y) * 1.1);

                if (neckposition > 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.NeckMove(1850 + neckposition));
                    //  UpperBodyHelper.NeckMove(1850 + neckposition);
                }
            }
        }

        private void HeadTrackingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int position;

            if (GlobalData.facemidpoint.X >= 272)
            {
                position = Convert.ToInt32((GlobalData.facemidpoint.X - 272) * 0.9);

                if (position >= 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.HeadMove(3096 - position));
                    //UpperBodyHelper.HeadMove(3096 - position);
                }
            }
            else if (GlobalData.facemidpoint.X < 272 && GlobalData.facemidpoint.X > 0)
            {
                position = Convert.ToInt32((272 - GlobalData.facemidpoint.X) * 0.9);

                if (position >= 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.HeadMove(3096 + position));
                    //  UpperBodyHelper.HeadMove(3096 + position);
                }
            }

            int neckposition;

            if (GlobalData.facemidpoint.Y >= 0 && GlobalData.facemidpoint.Y <= 200)
            {
                neckposition = Convert.ToInt32((GlobalData.facemidpoint.Y) * 1.1);

                if (neckposition > 0)
                {
                    //Task.Factory.StartNew(() => UpperBodyHelper.NeckMove(1850 + neckposition));
                    //UpperBodyHelper.NeckMove(1850 + neckposition);
                }
            }
        }

        private void HeadMotionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HeadMotionTimer.Start();
        }

        public static void HeadStaticMotion()
        {
            //List<string> headmotionlist = new List<string> { "look front", "look left", "look right", "look up", "look down" };

            //Random rm = new Random();
            //int i = rm.Next(headmotionlist.Count);
            //string motion = headmotionlist[i];

            //_motor.MoveTo(motion);
            UpperBodyHelper.Move("Head_Left");
            Task.Delay(2000).Wait();
            UpperBodyHelper.Move("Head_Center");
            Task.Delay(3000).Wait();
            UpperBodyHelper.Move("Head_Right");
            Task.Delay(2000).Wait();
            UpperBodyHelper.Move("Head_Center");
        }

        private void HeadMotionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            UpperBodyHelper.Move("Head_Left");
            Task.Delay(2000).Wait();
            UpperBodyHelper.Move("Head_Center");
            Task.Delay(3000).Wait();
            UpperBodyHelper.Move("Head_Right");
            Task.Delay(2000).Wait();
            UpperBodyHelper.Move("Head_Center");
        }

        private FaceAPI.Models.DetectedFace CreateFace(VisionAPI.Models.FaceRectangle rect)
        {
            return new FaceAPI.Models.DetectedFace
            {
                FaceRectangle = new FaceAPI.Models.FaceRectangle
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }

        private void MatchAndReplaceFaceRectangles(FaceAPI.Models.DetectedFace[] faces, OpenCvSharp.Rect[] clientRects)
        {
            // Use a simple heuristic for matching the client-side faces to the faces in the
            // results. Just sort both lists left-to-right, and assume a 1:1 correspondence. 

            // Sort the faces left-to-right. 
            var sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            // Sort the clientRects left-to-right.
            var sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            // Assume that the sorted lists now corrrespond directly. We can simply update the
            // FaceRectangles in sortedResultFaces, because they refer to the same underlying
            // objects as the input "faces" array. 
            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
            {
                // convert from OpenCvSharp rectangles
                OpenCvSharp.Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle = new FaceAPI.Models.FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
            }
        }

        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            // Draw any results on top of the image. 
            BitmapSource visImage = frame.Image.ToBitmapSource();

            var result = _latestResultsToDisplay;

            if (result != null)
            {
                // See if we have local face detections for this image.
                var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
                if (clientFaces != null && result.Faces != null)
                {
                    // If so, then the analysis results might be from an older frame. We need to match
                    // the client-side face detections (computed on this frame) with the analysis
                    // results (computed on the older frame) that we want to display. 
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
                }

                visImage = Visualization.DrawFaces(visImage, result.Faces, result.CelebrityNames);
                visImage = Visualization.DrawTags(visImage, result.Tags);
            }

            return visImage;
        }

        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            //  Task.Factory.StartNew(() => UpperBodyHelper.Move("Reset"));
            Task.Delay(3000).Wait();
            List<int> motorId = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            //   UpperBodyHelper.UnlockMotors(motorId);
            Thread.Sleep(600);
        }

        public static bool WarningSpeechLock = false;

        private bool _facemaskwarning;

        public bool FaceMaskWarning
        {
            get { return _facemaskwarning; }

            set
            {
                _facemaskwarning = value;

                if (FaceMaskWarning)
                {
                    FaceMaskWarning = false;

                    this.StopAnyTTSPlayback();

                    if (BroadcastHelper.Broadcasting == false)
                    {
                        SynthesizeAudioAsync("Please wear your mask");
                    }

                    this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\Warning.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }));

                    FaceMaskWarningTimer.Stop();

                    this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            this.MainUI.Visibility = Visibility.Visible;
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = @"\fm.jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\ChatImg\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                   )
             );

                    FaceMaskWarningTimer.Interval = 6000;
                    FaceMaskWarningTimer.Elapsed += FaceMaskWarningTimer_Elapsed;
                    FaceMaskWarningTimer.AutoReset = false;
                    FaceMaskWarningTimer.Start();
                }
            }
        }

        private void WarningSpeechWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // this.StopAnyTTSPlayback();
            SynthesizeAudioAsync("Please wear your mask").Wait();
        }

        private async void FaceMaskWarningTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = "\\" + IUPath + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\MainUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                   )
             );

            WarningSpeechLock = false;

            VisionRequestCount = 0;

            FaceMaskTimer.Interval = 500;
            FaceMaskTimer.Elapsed += FaceMaskTimer_Elapsed;
            FaceMaskTimer.AutoReset = false;
            FaceMaskTimer.Start();

            GIFTimer.Stop();
            UITimer.Start();
        }

        private void FaceMaskTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FaceMaskProcessing = false;
        }

        private async void MainWindows_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyStates == Keyboard.GetKeyStates(Key.D))
            {
                // ArmMotionWorker.RunWorkerAsync();
                // Task.Factory.StartNew(() => armmotionevent("Dance"));
            }


            else if (e.KeyStates == Keyboard.GetKeyStates(Key.R))
            {

                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              // UpperBodyHelper.Move("Reset");
                          }
                     )
               );

            }

            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Up))
            {

                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              BaseHelper.Move("forward");
                          }
                     )
               );
            }

            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Down))
            {
                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              BaseHelper.Move("backward");
                          }
                     )
               );
            }
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Right))
            {

                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              BaseHelper.Move("clockwise");
                          }
                     )
               );

            }
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Left))
            {
                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              BaseHelper.Move("anticlockwise");
                          }
                     )
               );

            }
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Space))
            {
                await this.Photo.Dispatcher.InvokeAsync(
                     new Action(
                          delegate
                          {
                              BaseHelper.Stop();
                          }
                     )
               );

            }
        }

        public static bool SpeechLock = false;

        public static async Task SynthesizeAudioAsync(string content)
        {
            if (BroadcastHelper.Broadcasting == false)
            {
                synthesizer.StopSpeakingAsync();

                await synthesizer.SpeakTextAsync(content);

                synthesizer.SynthesisCompleted += Synthesizer_SynthesisCompleted;
            }
        }

        private static void Synthesizer_SynthesisCompleted(object sender, SpeechSynthesisEventArgs e)
        {
            // throw new NotImplementedException();
            Debug.WriteLine("Speech Completed");
        }

        public static void Dance()
        {
            // UpperBodyHelper.Move("Dance1");
            Task.Delay(600).Wait();
            // UpperBodyHelper.Move("Dance2");
            Task.Delay(600).Wait();
            //UpperBodyHelper.Move("Dance3");
            Task.Delay(600).Wait();
            // UpperBodyHelper.Move("Dance_Highest");
            Task.Delay(1000).Wait();
            //  UpperBodyHelper.Move("Dance5");
            Task.Delay(600).Wait();
            //  UpperBodyHelper.Move("Dance6");
            Task.Delay(600).Wait();
            //  UpperBodyHelper.Move("Reset");
        }

        public async Task MakePredictionRequest(Bitmap bitmap)
        {
            Debug.WriteLine("MakePredictionRequest");
            await ParseWebCamFrame(bitmap);
            try
            {
                //var client = new HttpClient();

                //// Request headers - replace this example key with your valid Prediction-Key.
                //client.DefaultRequestHeaders.Add("Prediction-Key", "76fe29fc61c34d9f8c9c5c104aa0d85e");

                //// Prediction URL - replace this example URL with your valid Prediction URL.
                //string url = "https://robotcustomvision.cognitiveservices.azure.com/customvision/v3.0/Prediction/4e60338b-00cc-4ae6-b977-fc23bfc2598c/detect/iterations/Iteration22/image/nostore";

                //HttpResponseMessage response;

                //// Request body. Try this sample with a locally stored image.
                //byte[] byteData = GetImageAsByteArray(stream);

                //using (var content = new ByteArrayContent(byteData))
                //{
                //    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                //    response = await client.PostAsync(url, content);
                //    deserialiseJSON(await response.Content.ReadAsStringAsync());
                //}


            }

            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private static byte[] GetImageAsByteArray(MemoryStream stream)
        {

            byte[] bytes = stream.ToArray();
            return bytes;
        }

        System.Timers.Timer FaceMaskTimer = new System.Timers.Timer();

        public static bool FaceMaskProcessing = false;

        public void deserialiseJSON(string result)
        {
            VisionResult Jresult = new VisionResult();
            Jresult = JsonConvert.DeserializeObject<VisionResult>(result); // dynamic

            Debug.WriteLine(Jresult.Predictions[0].tagName + Jresult.Predictions[0].probability);

            //if (Jresult.Predictions[0].tagName == "WithoutFaceMask" && Convert.ToDouble(Jresult.Predictions[0].probability) >= 0.50)
            //{
            //    if (chatbotisrunning == false && FaceMaskProcessing == false)
            //    {
            //        FaceMaskProcessing = true;
            //        FaceMaskWarning = true;
            //    }
            //    // Task.Factory.StartNew(() => activateevent());
            //}
            //else if (Jresult.Predictions[0].tagName == "WithFaceMask" && Convert.ToDouble(Jresult.Predictions[0].probability) >= 0.65)
            //{
            //    //Debug.WriteLine("Activate");

            //    if (GlobalData.TourMode == false)
            //    {
            //        if (BroadcastHelper.Broadcasting == false)
            //        {
            //            if (GlobalData.isNavigating == false && facedelay == false && chatbotisrunning == false)
            //            {
            //                facedelay = true;
            //                Task.Factory.StartNew(() => activateevent());
            //            }

            //            if (GlobalData.isNavigating && GlobalData.goallocation == GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
            //            {
            //                Task.Factory.StartNew(() => interactionactivate_event());
            //            }
            //            else if (GlobalData.isNavigating && GlobalData.goallocation != GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
            //            {
            //                Task.Factory.StartNew(() => interactionactivate_event());
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (chatbotisrunning == false && GlobalData.personinfront_tour == false && TourHelper.BacktoStandyLocation == false)
            //        {
            //            // BaseHelper.CancelNavigation();
            //            // GlobalData.personinfront_tour = true;
            //            // tourinteractionactivate = true;
            //            Task.Factory.StartNew(() => tourinteractionactivateevent());
            //        }
            //        else if (chatbotisrunning == false && GlobalData.personinfront_tour == false && TourHelper.BacktoStandyLocation == true)
            //        {
            //            // GlobalData.personinfront_tour = true;
            //            // Returninteractionactivate = true;
            //            Task.Factory.StartNew(() => Returninteractionactivate_event());
            //        }
            //    }
            //}
        }

        private bool _ShowImage;
        public bool ShowImage
        {
            get { return _ShowImage; }

            set
            {
                _ShowImage = value;

                if (ShowImage)
                {
                    ShowImage = false;

                    if (BroadcastHelper.InfoRetrieved)
                    {
                        this.MainUI.Dispatcher.Invoke(
                       new Action(
                            delegate
                            {
                                this.MainUI.Visibility = Visibility.Visible;
                                string _path = System.Windows.Forms.Application.StartupPath;
                                string _file = GlobalData.BroadcastingImage + ".jpg";
                                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                                _path += @"\BroadcastingCustomizeFolder\";

                                Uri fileUri = new Uri(_path + _file);
                                BitmapImage bitmapSource = new BitmapImage();

                                bitmapSource.BeginInit();
                                bitmapSource.CacheOption = BitmapCacheOption.None;
                                bitmapSource.UriSource = fileUri;
                                bitmapSource.EndInit();

                                this.MainUI.Source = bitmapSource;
                            }));
                    }
                }
            }
        }


        private bool _ShowDefaultImage;
        public bool ShowDefaultImage
        {
            get { return _ShowDefaultImage; }

            set
            {
                _ShowDefaultImage = value;

                if (ShowDefaultImage)
                {
                    ShowDefaultImage = false;

                    if (BroadcastHelper.InfoRetrieved)
                    {
                        this.MainUI.Dispatcher.Invoke(
                       new Action(
                            delegate
                            {
                                this.MainUI.Visibility = Visibility.Visible;
                                string _path = System.Windows.Forms.Application.StartupPath;
                                string _file = IUPath + ".jpg";
                                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                                _path += @"\MainUICustomizeFolder\";

                                Uri fileUri = new Uri(_path + _file);
                                BitmapImage bitmapSource = new BitmapImage();

                                bitmapSource.BeginInit();
                                bitmapSource.CacheOption = BitmapCacheOption.None;
                                bitmapSource.UriSource = fileUri;
                                bitmapSource.EndInit();

                                this.MainUI.Source = bitmapSource;
                            }));

                    }
                }
            }
        }

        System.Timers.Timer HoldingTimer = new System.Timers.Timer();

        private void MainUI_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HoldingTimer.Interval = 35000;
            HoldingTimer.Elapsed += HoldingTimer_Elapsed;
            HoldingTimer.AutoReset = false;
            // HoldingTimer.Start();

            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                //HoldingTimer.Stop();
                //Debug.WriteLine("双击");

                this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            //this.MainUI.Source = null;

                            MicBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                        }));

                GIFTimer.Stop();
                UITimer.Start();

                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                //HoldingTimer.Stop();
                if (BroadcastHelper.Broadcasting == false)
                {
                    var fileContent = string.Empty;
                    var filePath = string.Empty;
                    var openDlg = new Microsoft.Win32.OpenFileDialog();

                    openDlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    bool? result = openDlg.ShowDialog();

                    // Return if canceled.
                    if (!(bool)result)
                    {
                        return;
                    }

                    filePath = openDlg.FileName;
                    var fileStream = openDlg.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                        // Debug.WriteLine(fileContent);
                        if (fileContent.Contains("+"))
                        {
                            char[] separator = { '+' };
                            string[] arr = fileContent.Split(separator);
                            string data = arr[0];
                            IUPath = data;
                            GreetingScript = arr[1];
                            Debug.WriteLine("InfoUpdated");
                        }
                    }

                    this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = "\\" + IUPath + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\MainUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                   )
             );
                }
                else //Broadcasting
                {
                    if (BroadcastHelper.Broadcasting == false)
                    {
                        BroadcastHelper.GetBroadcastInfo();
                        // BroadcastHelper.StartBroadcasting();
                    }
                }
            }
        }

        private void HoldingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (BroadcastHelper.Broadcasting == false)
            {
                var fileContent = string.Empty;
                var filePath = string.Empty;
                var openDlg = new Microsoft.Win32.OpenFileDialog();

                openDlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                bool? result = openDlg.ShowDialog();

                // Return if canceled.
                if (!(bool)result)
                {
                    return;
                }

                filePath = openDlg.FileName;
                var fileStream = openDlg.OpenFile();

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    fileContent = reader.ReadToEnd();
                    // Debug.WriteLine(fileContent);
                    if (fileContent.Contains("+"))
                    {
                        char[] separator = { '+' };
                        string[] arr = fileContent.Split(separator);
                        string data = arr[0];
                        IUPath = data;
                        GreetingScript = arr[1];
                        Debug.WriteLine("InfoUpdated");
                    }
                }

                this.MainUI.Dispatcher.Invoke(
               new Action(
                    delegate
                    {
                        string _path = System.Windows.Forms.Application.StartupPath;
                        string _file = "\\" + IUPath + ".jpg";
                        _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                        _path += @"\MainUICustomizeFolder\";

                        Uri fileUri = new Uri(_path + _file);
                        BitmapImage bitmapSource = new BitmapImage();

                        bitmapSource.BeginInit();
                        bitmapSource.CacheOption = BitmapCacheOption.None;
                        bitmapSource.UriSource = fileUri;
                        bitmapSource.EndInit();

                        this.MainUI.Source = bitmapSource;
                    }
               )
         );
            }
            else //Broadcasting
            {
                if (BroadcastHelper.Broadcasting == false)
                {
                    BroadcastHelper.GetBroadcastInfo();
                    ShowImage = true;
                    BroadcastHelper.StartBroadcasting();
                }
            }
        }

        private void MainUI_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void MainUI_TouchUp(object sender, TouchEventArgs e)
        {
        }

        private void MainUI_TouchMove(object sender, TouchEventArgs e)
        {

        }

        private void MainUI_TouchDown(object sender, TouchEventArgs e)
        {

        }

        private void Power_Click(object sender, RoutedEventArgs e)
        {
            StopCameraCapture();
            Application.Current.Shutdown();
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\VolumeReminder.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }
                      )
                );

            AudioManager.SetMasterVolume(AudioManager.GetMasterVolume() - 10);
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\VolumeReminder.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }
                      )
                );

            AudioManager.SetMasterVolume(0);
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\VolumeReminder.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }
                      )
                );

            AudioManager.SetMasterVolume(AudioManager.GetMasterVolume() + 10);
        }

        private bool _SchedulerEnabled;
        public bool SchedulerEnabled
        {
            get { return _SchedulerEnabled; }

            set
            {
                _SchedulerEnabled = value;

                if (SchedulerEnabled)
                {
                    this.Timer.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            Timer.Background = System.Windows.Media.Brushes.PaleVioletRed;
                        }
                        )
                    );
                }
                else if (!SchedulerEnabled)
                {
                    this.Timer.Dispatcher.Invoke(
                  new Action(
                       delegate
                       {
                           var bc = new BrushConverter();

                           Timer.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FFDDDDDD");
                       }
                       )
                   );
                }
            }
        }

        private void Timer_Click(object sender, RoutedEventArgs e)
        {
            this.Photo.Dispatcher.Invoke(
                      new Action(
                           delegate
                           {
                               string _path = System.Windows.Forms.Application.StartupPath;
                               string _file = @"\Timer.wav";
                               _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];

                               mediaPlayer.Open(new Uri(_path + _file));
                               mediaPlayer.Play();
                           }
                      )
                );

            if (SchedulerEnabled)
            {
                SchedulerEnabled = false;

                IniTimer.Stop();
                TimeCheckingTimer.AutoReset = false;
                TimeCheckingTimer.Stop();

                if (GlobalData.DutyOff)
                {
                    GlobalData.DutyOff = false;
                    GlobalData.shutdown.CancleShutDown();
                }

                if (GlobalData.TourMode)
                {
                    TourHelper.TourCanceled();
                }
            }
            else if (!SchedulerEnabled)
            {
                SchedulerEnabled = true;

                ResetTaskScheduler();
                TimeCheckingTimer.AutoReset = true;
                IniTimer.Start();
                //TimeCheckingTimer.Start();
            }
        }

        private void Photo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                //Image files|*.bmp;*.jpg;*.gif;*.png;*.tif|All files|*.*

                var fileContent = string.Empty;
                var filePath = string.Empty;
                var openDlg = new Microsoft.Win32.OpenFileDialog();

                openDlg.Filter = "Image files|*.bmp;*.jpg;*.gif;*.png;*.tif|All files|*.*";
                bool? result = openDlg.ShowDialog();

                // Return if canceled.
                if (!(bool)result)
                {
                    return;
                }

                filePath = openDlg.SafeFileName;
                fileContent = openDlg.FileName;
                Debug.WriteLine(filePath);

                if (filePath.Contains("."))
                {
                    char[] separator = { '.' };
                    string[] arr = filePath.Split(separator);
                    string data = arr[0];
                    Default2ndUI = data;
                }

                //Default2ndUI = filePath;
                Debug.WriteLine(filePath);

                Uri fileUri = new Uri(fileContent);
                BitmapImage bitmapSource = new BitmapImage();

                bitmapSource.BeginInit();
                bitmapSource.CacheOption = BitmapCacheOption.None;
                bitmapSource.UriSource = fileUri;
                bitmapSource.EndInit();

                this.Photo.Visibility = Visibility.Visible;

                this.Photo.Dispatcher.Invoke(
                       new Action(
                            delegate
                            {
                                this.Photo.Source = bitmapSource;
                            }
                       )
                 );

                //DelayTimer.Start();
            }
        }

        private void LoadModel()
        {
            // Check for an Onnx model exported from Custom Vision
            string _path3 = System.Windows.Forms.Application.StartupPath;
            string _file3 = @"\" + @"ML\OnnxModels";
            _path3 = _path3.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];


            var customVisionExport = Directory.GetFiles(_path3 + _file3, "*.zip").FirstOrDefault();

            // If there is one, use it.
            if (customVisionExport != null)
            {
                var customVisionModel = new CustomVisionModel(customVisionExport);
                var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

                outputParser = new OnnxOutputParser(customVisionModel);
                customVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();
            }
            else // Otherwise default to Tiny Yolo Onnx model
            {
                var tinyYoloModel = new TinyYoloModel(Path.Combine(modelsDirectory, "TinyYolo2_model.onnx"));
                var modelConfigurator = new OnnxModelConfigurator(tinyYoloModel);

                outputParser = new OnnxOutputParser(tinyYoloModel);
                tinyYoloPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<TinyYoloPrediction>();
            }
        }

        public async Task ParseWebCamFrame(Bitmap bitmap)
        {

            if (customVisionPredictionEngine == null && tinyYoloPredictionEngine == null)
                return;

            var frame = new ImageInputData { Image = bitmap };
            var filteredBoxes = DetectObjectsUsingModel(frame);

            List<string> targets = new List<string>();

            if (filteredBoxes.Any())
            {

                if (filteredBoxes[0].Label == "WithoutFaceMask" && filteredBoxes[0].Confidence >= 0.30)
                {
                    if (chatbotisrunning == false && FaceMaskProcessing == false)
                    {
                        //FaceMaskProcessing = true;
                        //FaceMaskWarning = true;
                        if (BroadcastHelper.Broadcasting == false)
                        {
                            if (GlobalData.isNavigating == false && facedelay == false && chatbotisrunning == false)
                            {
                                facedelay = true;
                                Task.Factory.StartNew(() => activateevent());
                            }

                            if (GlobalData.isNavigating && GlobalData.goallocation == GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
                            {
                                Task.Factory.StartNew(() => interactionactivate_event());
                            }
                            else if (GlobalData.isNavigating && GlobalData.goallocation != GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
                            {
                                Task.Factory.StartNew(() => interactionactivate_event());
                            }
                        }
                    }
                    // Task.Factory.StartNew(() => activateevent());
                }
                else if (filteredBoxes[0].Label == "WithFaceMask" && filteredBoxes[0].Confidence >= 0.65)
                {
                    //Debug.WriteLine("Activate");

                    if (GlobalData.TourMode == false)
                    {
                        if (BroadcastHelper.Broadcasting == false)
                        {
                            if (GlobalData.isNavigating == false && facedelay == false && chatbotisrunning == false)
                            {
                                facedelay = true;
                                Task.Factory.StartNew(() => activateevent());
                            }

                            if (GlobalData.isNavigating && GlobalData.goallocation == GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
                            {
                                Task.Factory.StartNew(() => interactionactivate_event());
                            }
                            else if (GlobalData.isNavigating && GlobalData.goallocation != GlobalData.startlocation && GlobalData.personinfront_standby == false && chatbotisrunning == false)
                            {
                                Task.Factory.StartNew(() => interactionactivate_event());
                            }
                        }
                    }
                    else
                    {
                        if (chatbotisrunning == false && GlobalData.personinfront_tour == false && TourHelper.BacktoStandyLocation == false)
                        {
                            // BaseHelper.CancelNavigation();
                            // GlobalData.personinfront_tour = true;
                            // tourinteractionactivate = true;
                            Task.Factory.StartNew(() => tourinteractionactivateevent());
                        }
                        else if (chatbotisrunning == false && GlobalData.personinfront_tour == false && TourHelper.BacktoStandyLocation == true)
                        {
                            // GlobalData.personinfront_tour = true;
                            // Returninteractionactivate = true;
                            Task.Factory.StartNew(() => Returninteractionactivate_event());
                        }
                    }
                }
            }
        }

        public List<OnnxSample.BoundingBox> DetectObjectsUsingModel(ImageInputData imageInputData)
        {
            var labels = customVisionPredictionEngine?.Predict(imageInputData).PredictedLabels ?? tinyYoloPredictionEngine?.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = outputParser.ParseOutputs(labels);
            var filteredBoxes = outputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);
            return filteredBoxes;
        }

        public async void IOTinit()
        {

            //// Connect to the IoT hub using the MQTT protocol
            //s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            //// Create a handler for the direct method call
            //await s_deviceClient.SetMethodHandlerAsync("communication", Request, null);
            //await s_deviceClient.SetMethodHandlerAsync("navigation", Request, null);
            //await s_deviceClient.SetMethodHandlerAsync("tourmode", Request, null);
            //await s_deviceClient.SetMethodHandlerAsync("vision", Request, null);
            //await s_deviceClient.SetMethodHandlerAsync("direction", Request, null);
            //await s_deviceClient.SetMethodHandlerAsync("chatbotenable", Request, null);

        }

        // Handle the direct method call
        private Task<MethodResponse> Request(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            dynamic subdata = JsonConvert.DeserializeObject(data.ToString());

            switch (methodRequest.Name)
            {
                case "navigation":
                    string goal = subdata.goal.ToString();

                    Debug.WriteLine(goal);

                    GlobalData.isNavigating = true;
                    GlobalData.RobotisReturning = false;
                    GlobalData.goallocation = goal;
                    GlobalData.Navitothegoalposition = true;

                    MainWindow.ChatbotRestartTimer.Stop();
                    MainWindow.NaviResumeTimer.Stop();

                    if (GlobalData.TourMode)
                    {
                        TourHelper.ResumeTimer.Stop();
                        TourHelper.ReturnTimer.Stop();
                        TourHelper.TourisInterruptedbyNavi = true;
                    }

                    BaseHelper.Go(goal);

                    break;
                case "interaction":
                    string interactionswitch = subdata.interactionswitch.ToString();
                    if (interactionswitch == "on")
                    {
                        GlobalData.interactionlock = true;
                    }
                    else if (interactionswitch == "off")
                    {
                        GlobalData.interactionlock = false;
                    }

                    break;
                case "direction":
                    string direction = subdata.movedirection.ToString();

                    if (GlobalData.TourMode)
                    {
                        if (!TourHelper.TourisInterruptedbyNavi)
                        {
                            if (TourHelper.BacktoStandyLocation == false)
                            {
                                TourHelper.ResumeTimer.Interval = 8000;
                                TourHelper.ResumeTimer.Elapsed += TourHelper.ResumeTimer_Elapsed;
                                TourHelper.ResumeTimer.AutoReset = false;
                                TourHelper.ResumeTimer.Start();
                            }
                            else if (TourHelper.BacktoStandyLocation == true)
                            {
                                TourHelper.ReturnTimer.Interval = 8000;
                                TourHelper.ReturnTimer.Elapsed += TourHelper.ReturnTimer_Elapsed;
                                TourHelper.ReturnTimer.AutoReset = false;
                                TourHelper.ReturnTimer.Start();
                            }
                        }
                    }

                    else
                    {
                        //if (GlobalData.TourMode)
                        //{
                        //    BaseHelper.CancelNavigation();
                        //    TourHelper.ResumeTimer.Stop();
                        //    TourHelper.ReturnTimer.Stop();
                        //}

                        //BaseHelper.CancelNavigation();
                        BaseHelper.Move(direction);
                    }

                    break;
                case "vision":

                    string modelname = subdata.model.ToString();
                    string modelswitch = subdata.visionswitch.ToString();
                    if (modelswitch == "on" || modelswitch == "off")
                    {
                        GlobalData.visionswitch(modelname, modelswitch);
                    }
                    break;
                case "chatbot":
                    string chatbotswitch = subdata.chatbotswitch.ToString();
                    if (chatbotswitch == "on")
                    {

                    }
                    else if (chatbotswitch == "off")
                    {
                        this.Reset();
                        this.UpdateConnectionProfileInfoBlock();
                    }
                    break;
                case "tourmode":
                    string tourswitch = subdata.tourswitch.ToString();
                    if (tourswitch == "on")
                    {
                        GlobalData.TourMode = true;
                        TourHelper.GetTourInfo();
                        TourHelper.GoFirstPoint();
                        this.StopAnyTTSPlayback();
                    }
                    else if (tourswitch == "off")
                    {
                        TourHelper.TourCanceled();
                    }
                    break;
                case "announcementmode":
                    string annmodelswitch = subdata.annmodelswitch.ToString();
                    if (annmodelswitch == "on")
                    {
                        if (BroadcastHelper.Broadcasting == false)
                        {
                            BroadcastHelper.RetrieveBroadcastInfo();
                            ShowImage = true;
                            BroadcastHelper.StartBroadcasting();
                        }
                    }
                    else if (annmodelswitch == "off")
                    {
                        BroadcastHelper.Stop();
                        ShowDefaultImage = true;
                    }
                    break;
                case "themeselect":
                    string themename = subdata.themename.ToString();
                    IUPath = themename;
                    this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            string _path = System.Windows.Forms.Application.StartupPath;
                            string _file = "\\" + IUPath + ".jpg";
                            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                            _path += @"\MainUICustomizeFolder\";

                            Uri fileUri = new Uri(_path + _file);
                            BitmapImage bitmapSource = new BitmapImage();

                            bitmapSource.BeginInit();
                            bitmapSource.CacheOption = BitmapCacheOption.None;
                            bitmapSource.UriSource = fileUri;
                            bitmapSource.EndInit();

                            this.MainUI.Source = bitmapSource;
                        }
                        )
                    );
                    break;
                case "announcement":
                    string script = subdata.robotscript.ToString();
                    GlobalData.broadcastscript = script;
                    break;
                case "pathroutine":
                    string path = subdata.path.ToString();

                    GlobalData.TourMode = true;
                    TourHelper.GetTourInfo(path);
                    TourHelper.GoFirstPoint();
                    break;
                case "broadcast":
                    string broadcastswitch = subdata.broadcastswitch.ToString();
                    if (BroadcastHelper.Broadcasting == false && broadcastswitch == "on")
                    {
                        BroadcastHelper.RetrieveBroadcastInfo();
                        ShowImage = true;
                        BroadcastHelper.StartBroadcasting();
                    }
                    else if (BroadcastHelper.Broadcasting == true && broadcastswitch == "off")
                    {
                        BroadcastHelper.Stop();
                        ShowDefaultImage = true;
                    }
                    break;
                case "communication":
                    string communicationstswitch = subdata.status.ToString();
                    if (communicationstswitch == "enable")
                    {
                        this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            this.Topmost = false;

                            this.WindowState = WindowState.Minimized;
                        }
                        )
                        );

                        Debug.WriteLine("enable");
                        //sendcommunicationcommand("enable");

                    }
                    else if (communicationstswitch == "disable")
                    {
                        //sendcommunicationcommand("disable");
                        this.MainUI.Dispatcher.Invoke(
                   new Action(
                        delegate
                        {
                            this.Topmost = true;
                            this.WindowState = WindowState.Maximized;
                        }
                        )
                        );
                       //MessageBox.Show("disable");
                    }
                    break;
                default:
                    break;
            }

            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));

        }

        private static ServiceClient s_serviceClient;

        private async static Task CommunicationInvokeMethodAsync(string status)
        {
            var methodInvocation = new CloudToDeviceMethod("communication")
            {

                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            if (status == "enable")
            {
                methodInvocation.SetPayloadJson("{\"status\": \"enable\"}");
            }
            else if (status == "disable")
            {
                methodInvocation.SetPayloadJson("{\"status\": \"disable\"}");
            }

            try
            {
                // Invoke the direct method asynchronously and get the response from the simulated device.
                var response = await s_serviceClient.InvokeDeviceMethodAsync("head1", methodInvocation);

                Debug.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static string service_connectionString = "HostName=robotnetwork.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=epdTWXHqZZhN8+l4I2mTTOoxjXQt+DAKfVlMILk+n8o=";

        public async static void sendcommunicationcommand(string status)
        {

            s_serviceClient = ServiceClient.CreateFromConnectionString(service_connectionString);

            await CommunicationInvokeMethodAsync(status);

            s_serviceClient.Dispose();
        }

    }
}
