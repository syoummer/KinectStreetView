using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Samples.Kinect.WpfViewers;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowsInput;

namespace KinectStreetview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();

        private Skeleton[] skeletons = new Skeleton[0];


        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();

            // initialize the Kinect sensor manager
            KinectSensorManager = new KinectSensorManager();
            KinectSensorManager.KinectSensorChanged += this.KinectSensorChanged;

            // locate an available sensor
            sensorChooser.Start();

            // bind chooser's sensor value to the local sensor manager
            var kinectSensorBinding = new System.Windows.Data.Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);
        }

        #region Kinect Discovery & Setup

        private void KinectSensorChanged(object sender, KinectSensorManagerEventArgs<KinectSensor> args)
        {
            if (null != args.OldValue)
                UninitializeKinectServices(args.OldValue);

            if (null != args.NewValue)
                InitializeKinectServices(KinectSensorManager, args.NewValue);
        }

        /// <summary>
        /// Kinect enabled apps should customize which Kinect services it initializes here.
        /// </summary>
        /// <param name="kinectSensorManager"></param>
        /// <param name="sensor"></param>
        private void InitializeKinectServices(KinectSensorManager kinectSensorManager, KinectSensor sensor)
        {
            // Application should enable all streams first.

            // configure the color stream
            kinectSensorManager.ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;
            kinectSensorManager.ColorStreamEnabled = true;

            // configure the depth stream
            kinectSensorManager.DepthStreamEnabled = true;

            kinectSensorManager.TransformSmoothParameters =
                new TransformSmoothParameters
                {
                    Smoothing = 0.5f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                };

            // configure the skeleton stream
            sensor.SkeletonFrameReady += OnSkeletonFrameReady;
            kinectSensorManager.SkeletonStreamEnabled = true;


            kinectSensorManager.KinectSensorEnabled = true;

            if (!kinectSensorManager.KinectSensorAppConflict)
            {
                // addition configuration, as needed
            }
        }

        /// <summary>
        /// Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        /// </summary>
        /// <param name="sensor"></param>
        private void UninitializeKinectServices(KinectSensor sensor)
        {

        }

        #endregion Kinect Discovery & Setup

        public static readonly DependencyProperty KinectSensorManagerProperty =
          DependencyProperty.Register(
              "KinectSensorManager",
              typeof(KinectSensorManager),
              typeof(MainWindow),
              new PropertyMetadata(null));

        public KinectSensorManager KinectSensorManager
        {
            get { return (KinectSensorManager)GetValue(KinectSensorManagerProperty); }
            set { SetValue(KinectSensorManagerProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                // resize the skeletons array if needed
                if (skeletons.Length != frame.SkeletonArrayLength)
                    skeletons = new Skeleton[frame.SkeletonArrayLength];

                // get the skeleton data
                frame.CopySkeletonDataTo(skeletons);

                int closestSkeletonIdx = -1;
                int i = 0;
                //we only want to track the first person
                foreach (var skeleton in skeletons)
                {
                    // skip the skeleton if it is not being tracked
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (closestSkeletonIdx == -1)
                            closestSkeletonIdx = i;
                        else
                        {
                            if (skeleton.Position.Z < skeletons[closestSkeletonIdx].Position.Z)
                                closestSkeletonIdx = i;
                        }
                    }
                    i++;
                }
                if (closestSkeletonIdx >= 0 )
                {
                    gestureDetection(skeletons[closestSkeletonIdx]);
                }
                //textBlock1.Text = " " + RightSwipeState;
            }
        }



        private bool RightHandTracked = false;
        private bool RightHandDone = false;
        private int RightSwipeState = 0;
        private Joint initRightJoint = new Joint();
        private Joint prevRightJoint = new Joint();
        private Joint initLeftJoint = new Joint();
        private Joint prevLeftJoint = new Joint();
        private bool zoomDone = false;
        private bool zoomTracked = false;
        private int zoomState = 0;

        private int gestureSpacing = 0;

        private void gestureDetection(Skeleton skeleton)
        {
            
            Joint rightHand = skeleton.Joints[JointType.HandRight];
            Joint leftHand = skeleton.Joints[JointType.HandLeft];
            Joint spine = skeleton.Joints[JointType.Spine];

            if (gestureSpacing > 0)
            {
                gestureSpacing--;
                prevRightJoint = rightHand;
                prevLeftJoint = leftHand;
                return;
            }

            Boolean rightHandFront = false;
            Boolean leftHandFront = false;
            rightHandFront = (spine.Position.Z - rightHand.Position.Z > 0.2) ? true : false;
            leftHandFront = (spine.Position.Z - leftHand.Position.Z > 0.2) ? true : false;
            
            //right hand in front of body
            if (rightHandFront && !leftHandFront && !zoomTracked)
            {
                if (!RightHandTracked)
                {
                    RightHandDone = false;
                    RightHandTracked = true;
                    RightSwipeState = 0;
                    initRightJoint = rightHand;
                }
                else
                {
                    //horizontal move
                    if (Math.Abs(initRightJoint.Position.Y - rightHand.Position.Y) < 0.05 &&
                        Math.Abs(initRightJoint.Position.Z - rightHand.Position.Z) < 0.05 &&
                        Math.Abs(rightHand.Position.Y - initRightJoint.Position.Y) < 0.05 )
                    {
                        //move right
                        if (rightHand.Position.X - prevRightJoint.Position.X > 0.03
                            && rightHand.Position.X - initRightJoint.Position.X > 0.03)
                        {
                            if (RightSwipeState >= 0)
                                RightSwipeState++;
                            else
                                RightHandDone = true;
                        }
                        else if (rightHand.Position.X - prevRightJoint.Position.X < -0.03
                              && rightHand.Position.X - initRightJoint.Position.X < -0.03)
                        {
                            if (RightSwipeState <= 0)
                                RightSwipeState--;
                            else
                                RightHandDone = true;
                        }
                        else
                        {
                            RightHandDone = true;
                        }
                    }
                    else
                    {
                        //stop
                        RightHandDone = true;
                    }
                }
            }

            if (!rightHandFront && RightHandTracked)
            {
                RightHandDone = true;
            }


            if (RightHandDone)
            {
                RightHandDone = false;
                RightHandTracked = false;
                gestureSpacing = 3;
                if (RightSwipeState > 1)
                {
                    textBlock1.Text = "Right " + RightSwipeState;
                    SimulateButtonPress(VirtualKeyCode.RIGHT, 300);
                }
                else if (RightSwipeState < -1)
                {
                    SimulateButtonPress(VirtualKeyCode.LEFT, 300);
                    textBlock1.Text = "Left " + RightSwipeState;
                    //gestureSpacing = 20;
                }
                else
                {
                    //textBlock1.Text = "None " + RightSwipeState;
                }
                RightSwipeState = 0;
                
            }

            //two hands in front
            if (leftHandFront && rightHandFront)
            {
                //detect zoom in/out
                if (!zoomTracked)
                {
                    zoomTracked = true;
                    zoomState = 0;
                    initLeftJoint = leftHand;
                    initRightJoint = rightHand;
                }
                else
                {
                    if (Math.Abs(leftHand.Position.Z - rightHand.Position.Z) < 0.1&&
                        leftHand.Position.Y > skeleton.Joints[JointType.ElbowLeft].Position.Y &&
                        rightHand.Position.Y > skeleton.Joints[JointType.ElbowRight].Position.Y)
                    {
                        //zoom in
                        if (dist(leftHand, rightHand) - dist(prevLeftJoint, prevRightJoint) > 0.01
                            )//&& dist(leftHand, rightHand) - dist(initLeftJoint, initRightJoint) > 0.01)
                        {
                            if (zoomState >= 0)
                                zoomState++;
                            else
                                zoomDone = true;
                        }
                        else if (dist(prevLeftJoint, prevRightJoint) - dist(leftHand, rightHand) > 0.01
                            )//&& dist(leftHand, rightHand) - dist(initLeftJoint, initRightJoint) < 0.01)
                        {
                            if (zoomState <= 0)
                                zoomState--;
                            else
                                zoomDone = true;
                        }
                        else
                        {
                            zoomDone = true;
                        }

                    }
                    else
                        zoomDone = true;
                }
            }

            if (!rightHandFront && !leftHandFront && zoomTracked)
                zoomDone = true;
            if (zoomDone)
            {
                float zoomDist = Math.Abs(dist(leftHand, rightHand) - dist(initLeftJoint, initRightJoint));
                if (zoomState < -1 && zoomDist > 0.2)
                {
                    textBlock1.Text = "Zoom out";
                    SimulateButtonPress(VirtualKeyCode.OEM_MINUS, 300);
                }
                //else if (zoomLeftState < -1 && zoomRightState > 1)
                else if (zoomState > 1 && zoomDist > 0.2)
                {
                    textBlock1.Text = "Zoom in";
                    SimulateButtonPress(VirtualKeyCode.OEM_PLUS, 300);
                }
                else
                {
                    textBlock1.Text = " ";
                }
                zoomDone = false;
                zoomTracked = false;
                zoomState = 0;
                gestureSpacing = 3;
            }
            prevRightJoint = rightHand;
            prevLeftJoint = leftHand;
            //walk
            Joint kneeRight = skeleton.Joints[JointType.KneeRight];
            Joint kneeLeft = skeleton.Joints[JointType.KneeLeft];
            bool isWalk = false;
            if (kneeLeft.Position.Y - kneeRight.Position.Y > 0.03 && walkState == 0)
            {
                walkState = 1;
            }
            else if (kneeRight.Position.Y - kneeLeft.Position.Y > 0.03 && walkState == 1)
            {
                walkState = 2;
            }
            else
            {
                if (walkState == 2)
                {
                    isWalk = true;
                }
                else
                {
                    //walkState = 0;
                }
            }
            if (isWalk)
            {
                SimulateButtonPress(VirtualKeyCode.UP, 300);
                walkState = 0;
            }
        }

        private int walkState = 0;

        private float dist(Joint a, Joint b)
        {
            return (float)Math.Sqrt((a.Position.X - b.Position.X) * (a.Position.X - b.Position.X) +
                (a.Position.Y - b.Position.Y) * (a.Position.Y - b.Position.Y));
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            String path = System.IO.Path.GetFullPath(".");
            String url = urlBox.Text;//@"E:\SYou\kinect\SkeletonRecorder\KinectStreetview\streetviewmap.htm";
            if (System.IO.File.Exists(url))
            {
                webBrowser1.Navigate(url);
            }
            webBrowser1.Focus();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.WebBrowser w = new System.Windows.Forms.WebBrowser();
            //System.Threading.Thread.Sleep(1000);
            //webBrowser1.InvokeScript("gasStart");

            webBrowser1.Focus();
            //System.Windows.Forms.SendKeys.Send("{UP}");
            //InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
            SimulateButtonPress(VirtualKeyCode.UP, 300);
            
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            //webBrowser1.InvokeScript("gasEnd");
            webBrowser1.Focus();
            InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
        }

        private void right_Click(object sender, RoutedEventArgs e)
        {
            
            SimulateButtonPress(VirtualKeyCode.LEFT, 200);
        }

        private void SimulateButtonPress(VirtualKeyCode key, int time)
        {
            webBrowser1.Focus();
            System.Threading.Thread t =
                new System.Threading.Thread(() => buttonPress(key, time));
            t.Start();
        }

        private void buttonPress(VirtualKeyCode key, int time)
        {
            InputSimulator.SimulateKeyDown(key);
            System.Threading.Thread.Sleep(time);
            InputSimulator.SimulateKeyUp(key);
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            webBrowser1.Focus();
            InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
        }

        private void webBrowser1_LoadCompleted(object sender, NavigationEventArgs e)
        {
            
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            SimulateButtonPress(VirtualKeyCode.OEM_MINUS, 300);
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            SimulateButtonPress(VirtualKeyCode.OEM_PLUS, 300);
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int angle =  (int)slider1.Value;
            KinectSensorManager.ElevationAngle = angle;
        }

        bool debugOn = true;
        private void button8_Click(object sender, RoutedEventArgs e)
        {
            if (!debugOn)
            {
                button2.Visibility = Visibility.Visible;
                button3.Visibility = Visibility.Visible;
                button4.Visibility = Visibility.Visible;
                button5.Visibility = Visibility.Visible;
                button6.Visibility = Visibility.Visible;
                button7.Visibility = Visibility.Visible;
            }
            else
            {
                button2.Visibility = Visibility.Collapsed;
                button3.Visibility = Visibility.Collapsed;
                button4.Visibility = Visibility.Collapsed;
                button5.Visibility = Visibility.Collapsed;
                button6.Visibility = Visibility.Collapsed;
                button7.Visibility = Visibility.Collapsed;
            }
            debugOn = !debugOn;
        }
        
    }
}
