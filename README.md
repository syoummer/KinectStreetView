# Kinect Street View #
## Simin You ##
### Version 0.0.2###

## Overview ##
Using Kinect to control Google street view. 
Supports "zoom in", "zoom out", "walk", "turn left", "turn right".

This project depends on Kinect viewer from Kinect SDK 1.6 (for visulizing Kinect output), and [InputSimulator](http://inputsimulator.codeplex.com/) (for keyboard hacking)

## Demo ##
- http://www.youtube.com/watch?v=cWBIo0twNJ4
- http://www.youtube.com/watch?v=hV3Vh_WxQHc


## Boring Description ##
This project provides Kinect interface to manipulate Google Street View as well as Google Earth. We proposed and implemented one hand and two hands gestures to support “Zoom”, “Turn”, etc. And also, we detect “Walk” to simulate user walking on the street. The control signals are sent by simulating keystrokes and both Street View and Earth are rendered using HTML+JavaScript inside a web browser. 
This project can be divided into two parts but using the same framework. The first part is Street View, which supports “Zoom In/Out”, “Turn Left/Right”, and “Walk”. “Zoom” gestures use two hands, two hands in front of the body and split from center is “Zoom in”, and two hands in front of body and merge together is “Zoom Out”. Right hand is dedicated to control the “Turn” gestures. Putting right hand in front of body and swipe left/right to turn left/right. And finally, “Walk” is detected when user mimic walk in place, the gesture is to track two knees go up and down.
The second part of this project is controlling Google Earth truck example.  We modified the example to be used in our framework. In this part, instead of requiring user walk to simulate move forward, we map “Zoom” gestures to move forward/backward. 


Special thanks to [Yan](http://grapeot.me "Yan") for tremendous help and valuable discussion!

## Reference ##
- Google Earth API Monster Milktruck Example. [http://earth-api-samples.googlecode.com/svn/trunk/demos/milktruck/index.html](http://earth-api-samples.googlecode.com/svn/trunk/demos/milktruck/index.html)
- InputSimulator [http://inputsimulator.codeplex.com/](http://inputsimulator.codeplex.com/)
