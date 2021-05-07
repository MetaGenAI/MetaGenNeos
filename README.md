This plugin allows one to record all users in a session. It is able to record:

* Movement (up to any number of trackers Neos supports)
* Voice
* Hearing (stereoscopic, from the perspective of one user)
* Vision (records video from the perspective of the users)

This plugin is what the [NeosVR metagenbot](https://www.youtube.com/watch?v=PgQmuIQYoBE&ab_channel=GuillermoValle) is running (minus a trick to connect to sessions that aren't running the plugin). This release is inteded to be used as a normal Neos plugin, i.e. either locally or with other users running the plugin. Metagenbot is provided for convenience, but recording locally usually gives the best results for single-person recordings. If you want to use metagenbot for multi-person recordings, you can either invite it if it's online, or contact me [@guillefix](https://twitter.com/guillefix) if it isn't online, or if you want maximum performance or other special requirements for your usecase.

You can visit http://metagen.ai/ to find more about the vision and motivation behind this tool beyond helping people make awesome stuff :)

# How to use

## How to set up and load the plugin

1. Download `metagenneos_v0.8.5.zip` and `run_neos_metagen.bat` from the [Releases tab](https://github.com/MetaGenAI/MetaGenNeos/releases/). 
2. Unzip the contents of `metagen-0.9.zip` into the Neos installation folder, and copy `run_neos_metagen.bat` into the Neos installation folder also.
3. Download ffmpeg from here https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-full.7z, and the contenst of `bin` to the System32 folder (`C:\Windows\System32`).
4. Execute `run_neos_metagen.bat` (double click on it). This will start Neos with the plugin loaded, and it will open your local home
5. Once neos has loaded, take a logix tip, and open the node menu. You will find a new folder called `AAAA`. In it you will find `MetaGenLoader`. Equip it with the Logix tip and spawn it in the world. I recommend saving the world now, as doing so (and assuming the save has worked), will mean you can skip this step in the future, as the logix node being present in the world is enough for the system to load.
6. Open a new world/session, and the MetagenBot UI will spawn. You can now use the different functionality now, as explained in the next section

## What can I do with it

The basic functionality is the same as explained in the release of Metagenbot Beta, you can check the videos:

* [Video in English here](https://www.youtube.com/watch?v=PgQmuIQYoBE&ab_channel=GuillermoValle) ([another video by sirkitree](https://www.youtube.com/watch?v=79xguu735XE&ab_channel=sirkitree))
* [Video in Japanese here](https://twitter.com/sleeping_vrc/status/1355868840081510400) (thanks sleepingkaikai!)

### Recording

You can record yourself and other users by pressing start record. This will record the movement, voice, and hearing (hearing only for one user). Optionally it can also record the vision. Note:
* Only the users who have the (local) checkmark "Record me" checked will be recorded.
* The recordings are saved in a folder called `data` in the Neos installation folder, and are organized by world, and by recording session (each given a unique hash). Within each recording session the recordings are found in numbered folders.
* The recordings are done in a robust way that implies that if you crash during a long/important recording, the resulting files will either not be affected, or be easily fixable. Also all recordings are written direclty to disk and thus one can record arbitrarily long recordings without memory issues, although right now recordings are automatically chunked in maximum lengths of 10 minutes.

### Playback

You can press Start play to play the last recording made in the current world. You can navigate older recordings by changing the "Recording index" (it's an index increasing from newest to older). Features include:
* You can choose to replay just the motion, or add the recorded voices and hearing. 
* You can set the avatar used for playback by dropping a slot reference in the Avatar slot field in the UI. At the moment all the people in the recording will be played back with the same avatar. Tho I may add the option to choose an avatar for each in the future. If the field is null, it uses a default mixamo avatar.
* There's an advanced option to receive streamed pose data from an external source, which at the moment is only used for integration with ThreeDPoseTracker (see below), but will be used for more things in the future (e.g. procedurally or AI-driven animations).

### Exporting

You can export the recordings as either a native Neos animated mesh (as showcased in the above videos), and/or as a Bvh file. You can do these by selecting the checkboxes. These checkboxes will determine whether these exports are generated while doing a recording, but also while doing a playback! The later is useful in case you want to generate an animation of a previously recorded motion/voice acting, but with a different avatar, or you want to generate a bvh file for a different skeleton! Note:
* Generating an animation while recording has the advantage that the animation will record all blendshapes and bone transforms of every mesh under the user avatar. (for example it will record if you are holding and using a gun, or if you have facial or other animations!)

### Generate animations from video

This is using a version of [ThreeDPoseTracker for Unity](https://github.com/digital-standard/ThreeDPoseUnityBarracuda) which I've modified to send pose data to Neos. This allows you to generate an animation from a video. You can check [here](https://youtu.be/x-VGy3X0bME?t=162) for guidelines of which videos will give best results, but this is still a technology in development. Check this video for a demo:

[![image](https://user-images.githubusercontent.com/7515537/111398938-96675b00-86c4-11eb-8f9f-7bbe0e34d8b7.png)](https://www.youtube.com/watch?v=k5a_MJhzbdc&ab_channel=GuillermoValle)

To use it:

1. Download this Unity project and unzip it: https://drive.google.com/file/d/1G2OTyhVysEKXAmIU0-K2IkWU99DgeARa/view?usp=sharing
2. Open the Unity project. Open the scene SampleScene in Scenes. Select Video Player and on its Video Player component drop a video on the Video Clip property. Then play the scene, and after it's begun playing, go to Neos, check the "External source" checkmark on the Debug play section, and the press Start Play. The 3D movement inferred by ThreeDPoseTracker should be reproduced on the avatar which you are playing with.

-------------

Credits:

The code to record meshes and export as an AnimX file uses the recording tool by LucasRo7 and jeana https://github.com/jeanahelver/NeosAnimationToolset <3
