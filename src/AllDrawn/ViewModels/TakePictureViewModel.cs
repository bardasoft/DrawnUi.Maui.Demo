﻿using AppoMobi.Maui.DrawnUi.Demo.Interfaces;
using DrawnUi.Maui;
using DrawnUi.Maui.Camera;
using DrawnUi.Maui.Controls;
using System.Windows.Input;

namespace AppoMobi.Maui.DrawnUi.Demo.ViewModels
{
    /// <summary>
    /// Intended to return the captured image immediately using callback parameter
    /// </summary>
    public class TakePictureViewModel : ProjectViewModel, ICameraViewModel, IQueryAttributable
    {
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query != null)
            {
                query.TryGetValue("callback", out var command);
                if (command != null)
                {
                    Callback = command as ICommand;
                }
            }
        }

        #region COMMANDS

        public ICommand CommandCaptureStillPhoto
        {
            get
            {
                return new Command(async (object context) =>
                {
                    if (CheckLockAndSet())
                        return;


                    if (Camera.State == CameraState.On && !Camera.IsBusy)
                    {
                        Camera.FlashScreen(Color.Parse("#EEFFFFFF"));
                        await Camera.TakePicture().ConfigureAwait(false);
                    }
                });
            }
        }

        public Command CommandPreviewTapped => new Command(async () =>
        {
            if (CheckLockAndSet())
                return;


            if (CheckLockAndSet() || string.IsNullOrEmpty(_lastSavedPath))
                return;

#if ANDROID
            Camera.OpenFileInGallery(_lastSavedPath);
#endif

        });

        #endregion

        /// <summary>
        /// Cat be set from arguments by shell
        /// </summary>
        public ICommand Callback { get; set; }

        private LoadedImageSource _displayPreview;
        public LoadedImageSource DisplayPreview
        {
            get
            {
                return _displayPreview;
            }
            set
            {
                if (_displayPreview != value)
                {
                    _displayPreview = value;
                    OnPropertyChanged();
                }
            }
        }

        private ImageSource _LoadImage;
        public ImageSource LoadImage
        {
            get
            {
                return _LoadImage;
            }
            set
            {
                if (_LoadImage != value)
                {
                    _LoadImage = value;
                    OnPropertyChanged();
                }
            }
        }

        SemaphoreSlim semaphoreProcessingFrame = new(1, 1);

        private string _lastSavedPath;

        public void AttachCamera(SkiaCamera camera)
        {
            if (Camera == null && camera != null)
            {
                Camera = camera;
                Camera.CaptureSuccess += OnCaptureSuccess;
                Camera.StateChanged += OnCameraStateChanged;
                Camera.NewPreviewSet += OnNewPreviewSet;
            }
        }

        public override void OnDisposing()
        {


            if (Camera != null)
            {
                Camera.CaptureSuccess -= OnCaptureSuccess;
                Camera.StateChanged -= OnCameraStateChanged;
                Camera.NewPreviewSet -= OnNewPreviewSet;
                Camera = null;
            }

            base.OnDisposing();
        }

        private void OnNewPreviewSet(object sender, LoadedImageSource source)
        {
            //Task.Run(async () =>
            //{

            //    ProcessPreviewFrame(source); was used in detection mode

            //}).ConfigureAwait(false);
        }

        private void OnCameraStateChanged(object sender, CameraState state)
        {
            if (state == CameraState.On)
            {
                Camera.Display.Blur = 0;
                ShowResume = false;
            }
            else
            {
                ShowResume = true;
                if (Camera != null && Camera.Display != null)
                {
                    Camera.Display.Blur = 10;
                }
            }
        }

        private bool _ShowResume;
        public bool ShowResume
        {
            get
            {
                return _ShowResume;
            }
            set
            {
                if (_ShowResume != value)
                {
                    _ShowResume = value;
                    OnPropertyChanged();
                }
            }
        }

        bool _loadOnce;

        public void OnAppearing()
        {
            if (!_loadOnce)
            {
                _loadOnce = true;
                Task.Run(async () =>
                {
                    Camera.IsOn = true;
                }).ConfigureAwait(false);
            }
            else
            {
                Camera.IsOn = true;
            }
        }

        public void OnAppeared()
        { }

        public void OnDisappeared()
        {
            Camera.IsOn = false;
        }

        public void OnDisappearing()
        {

        }

        protected SkiaCamera Camera { get; set; }

        private async void OnCaptureSuccess(object sender, CapturedImage captured)
        {
            // normaly you would have several way to display a captured preview after you receive a large capture
            // 1 - create small bitmap rotated according to device orientation
            // 2 - just display the large bitmap but set preview UseCache="Bitmap"
            // 3 - create a new rotated large bitmap with any overlays etc and display it, dont forget UseCache="Bitmap"

            //this demonstrates case 3

            //creating an overlay to be rendered over the captured photo
            var overlay = new SkiaLayout()
            {
                Type = LayoutType.Column,
                Spacing = 10,
                BackgroundColor = Color.Parse("#46000000"),
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(16),
            };

            overlay.AddSubView(new SkiaLabel()
            {
                Tag = "ProtoTitle",
                Text = $"DrawnUi Camera {Version}",
                FontFamily = "FontTextBold",
                FontSize = 24,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = DrawTextAlignment.Center,
                VerticalOptions = LayoutOptions.Start
            });

            //overlay some info over bitmap
            var newBitmap = Camera.RenderCapturedPhoto(captured, overlay);

            //going to use the newly created bitmap with effects applied and overlay info
            //to save to gallery, so need to dispose the original one
            captured.Image.Dispose();
            captured.Image = newBitmap;

            //save to device, can use custom album name if needed
            Camera.CameraDevice.Meta.Orientation = 1; //since we rotate the capture to overlay info the orientation will always be 1 (default)
            await Camera.SaveToGallery(captured, false);

            //display preview
            //captured.Bitmap will be disposed by image ImagePreview when it
            //changes source to a new one, or when ImagePreview is disposed
            //ImagePreview.SetBitmapInternal(captured.Bitmap);
            //DisplayPreview = new(captured.Image);  //not using this in this screen

            _lastSavedPath = captured.Path;

            if (Callback != null)
            {
                Callback.Execute(captured);
            }
        }

        public TakePictureViewModel(NavigationViewModel navModel) : base(navModel)
        { }
    }
}
