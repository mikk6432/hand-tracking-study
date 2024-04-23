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
* Error transparent

TODO for main study
* (Done) Investigate fix for track sometimes appearing below/above ground when starting the app.
* Chest mount + hand mount should be "plug and play" for participants. There should be no need for adjustments.


# Notes after "pilot study" with pavel 16/04: 
TODO:
* Investigate: Circle track jumps to other side of room (happened to Pavel)
* Verify: After invalidating a diameter, the remaining diameters should be randomized again (It does not matter who invalidates - us or an error)
* Add circle direction to log (clockwise/counter clockwise)
* Randomize circle direction (so it might change between each diameter), and vizualize with arrow on ground
* Make tracks into lines again (borth linear and circle)
* Set circle radius to 1.114
* Change study/latin square to 3 conditions (3 refs), and just randomize contexts within these, and diameters within each context.
* Verify that the latin square is correct
* (maybe) add to procedure: Keep repeating during study: "Remember to raise any issues you encounter. Then we will redo the current task"
* Update consent forms with new study details (circle track etc.) (Pavel probably does this)
* After hiding track the first time (on study step: -1), hide all objects like target box etc. to let the participant walk around freely.
* Add to procedure: Ask for permission to put on glove.
* Add to procedure: Before putting the headset on participant: Make sure the volume is turned up.
* Add to procedure: We should show the targets while initially explaining the target selection task.
* Remove Gizmo
* Remove error on invalidate
* Decide on / practice appropriate circle metronome training: E.g.: do at least 3 full revolutions in each direction, or until comfortable.
* Define "emergency plan" if headset crashes. E.g. quickly restart OR continue on other headset OR something else?
* On circle track (and all other contexts), don't show more errors after one has been shown in the current diameter. (e.g. when the participant is walking back to the start again, no more "left the track" errors should be thrown)


Notes:
* If controllers are misalligned: Try to disable the boundary OR hold controllers correctly in hands.
* During initial task explanation we should clearly explain the different target sizes: "To maintain a balance between speed and accuracy you (the participant) should probably slow down on smaller target sizes.
* Whenever we put the headset back on the participant (e.g. after mandatory break), verify that the physical controllers correctly allign with the virtual ones.

Pilot study notes
* print on both sides.
* Should show target size
* Should be able to cancel trial 
* Make sure to mantion the names of the reference frames during the conditions (for the final questionire)
* Add break to runconfig UI thing on server
* Change all names of hand reference to palm W/o rotation.