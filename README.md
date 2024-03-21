## Week 1.
What did we do:
*  Literature search for relevant hand tracking capability/comparison studies.
* Decide to do a simple hand-tracking evaluation study, instead of full motion capture version.
    * Motion capture was too timeconsuming for a preliminary study, and simply hand-tracking evaluation could easily compare many different technologies.
* Setup development environment

TODO for next week:
* Investigate which hand versions (1.0, 2.0) can be used with which HMD, and how to distinguish them.
* Implemented OpenXR handtracking.
* Develop study details (sphere target + data collection)

TODO for week 12:
* Setup full pilot study
* Make hand mount again
* "Constant" algorithm to placing targets for Torso ref and path ref (X cm down and Y cm out from head of participant).
* Come prepared
* Implement 3 passthrough options: VR, AR, blurred behind hands.

TODO for week 13: 
* Better palm ref angle (more towards you)
* (Done) Track is invisible/below ground
* (Done) Place lights should work (lights at each end of the track)
* (Done) place track on top of mesh
* Look at why controller is misalligned
    * Changing Quest 3 tracking frequency from "auto" to "50hz" might help with tracking (setting found in Quest 3 settings -> System -> Headset Tracking).
    This still has to be tested
* hand-on-controller/detached controller/just hand state fix
* Slow down speed (just speed, not tempo)
    * Slowed from 1 m/s to 0.8 m/s.
    * Still need to test this or research what an appropiate threshold is.
* error transparent

TODO for main study
* (Done) Investigate fix for track sometimes appearing below/above ground when starting the app.
* Chest mount + hand mount should be "plug and play" for participants. There should be no need for adjustments.