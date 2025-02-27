# gretaUnity-SocialTouch
<!---
Utiliser ces blocs https://stackoverflow.com/questions/58737436/how-to-create-a-good-looking-notification-or-warning-box-in-github-flavoured-mar
-->


# Working base for SocialTouch


This repository contains required or highly useful assets for all SocialTouch projects/modules.
The repository itself is a Unity project that you can copy and use as a base for developing a new experiment, etc.
It features several work-in-progress scenes for experiments in the context of the ANR Social Touch and MATCH research projects.

## Usage

The current version of the project has only been tested with Unity version 2020.3.48f LTS.

If upon launching a scene Unity 'crashes' (closes without any error message), you should check in the project settings whether the XR plugin is initialized on launch. If it is and you don't have a VR HMD connected to the computer, Unity will crash. In that case you would need to uncheck the XR plugin initialization etc.

### Greta

#### Explanations

[Greta](https://github.com/isir/greta) is a *a virtual character engine that allows generating socio-emotional behaviors in order to build natural interactional scenario with human users*.

In the project, you will find a 3D character prefab named `CamilleUnityGRETAHybrid`, which you can use to connect with the GRETA application once you have downloaded, compiled and launched it with the config `ConfigGretaExperiment.xml`, found at the root of this repository.

| :memo: Note : |
|:---|
| Some more documentation about the Greta-Unity implementation in this repository can be retrieved from the [wiki](https://github.com/isir/gretaUnity-SocialTouch/wiki). |

Greta communicates with Unity thanks to [Thrift](https://thrift.apache.org/), which allow inter-language communications and IPC.


#### Usage

You will find a script (`GretaAnimationManager` or `GretaAnimationManagerDEMO`) on `CamilleSocialTouchNewColliders` object. This scripts shows how to ask Greta to "play" an FML file. See Greta's documentation for explanations of FMLs. Basically they are XML files describing a sequence of things to do with priority, timings, etc.

### Gaze-following and user distance

On `CamilleUnityGRETAHybrid`, you will find a `HeadLookController` and a `AICharacterControl` scripts.

Why ? Because for now

* Even if Greta is capable of following an object with gaze, it requires a lot of messages to synchronize the Unity and GRETA internal environments.
* Greta cannot move an agent's legs.

`HeadLookController` is capable of following a Unity object with its head, neck and eyes. When activated, the Greta animation for the **neck and eye direction** will be **overriden**, however the gestures, facial expressions etc. will continue to work.

`AICharacterControl` is used to turn in the direction of a target, and walk toward it if it is too far from the agent.

Example of activation that is either already implemented or can be implemented in a `GretaAnimationManager`.

```c#
// Touch on a global collider, start to follow user (hands and eyes)
public void OnTriggerEnter(Collider other)
{
    if (FollowHeadOnTouch)
    {
        GetComponent<AICharacterControl>().target = UserHead;
        GetComponent<HeadLookController>().target = UserHead;
    }
}
```

As you can see, if the public parameter `FollowHeadOnTouch` is `true`, then any collision with Camille will make her start following the user.

You can play with `ThirdPersonCharacter` and `NavMeshAgent` parameters to control how far Camille will stop from its target, how fast she walks, etc. The prefab default makes Camille stop walking through the user at 2 meters.

Obviously, you can trigger gaze following alone, with an other event than collision, and even follow something that is not the user head.

### Qualification of touch and proximity detection

In SocialTouch, the user may be made able to touch the agent, in which case we have a simple fuzzy logic algorithm to try to interpret the touch type (tap, caress, pat, etc).

We won't explain the mechanism there, but you can look at the TactileSequence and HandTouchManager files to get an idea of how this works.

#### Visual perception

See the `LookCamille` and `DistanceInterpretation` scripts. These scripts will try to interpret the distance between the user and Camille (for example), and if the user is looking towards Camille (or where). Interpretation is printed in the console if `debug` is true and is based on a similar mechanism as the qualification of touch discussed in the next section.

These scripts provide two subscribable events :
* `LookCamille` provides `LookAtCamilleChanged`, with parameters defined in `LookAtCamilleEventArgs`. This event is triggered each time the script thinks that the look direction has changed.
* `DistanceInterpretation` provides `DistanceInterpretationChanged`, with parameters defined in `DistanceInterpretationEventArgs`. This event is triggered each time the interpretation class of the distance between the human and Camille changes.

Note that the values of the interpretation classes of distance are based on the proxemics literature.

#### Tactile perception

|:warning: Warning :|
|:---|
|Since the initial work, other `AnatomyParameters` have been added : `Arm`, `Forearm`, `Neck` and `Hand` now **replace** `Member`. The behaviour is the same but allows to know where the touch occurs.|

See the `HandManager` script.
To sum up, Camille's skeleton is covered by colliders (on arms, on chest, on head, etc). The goal is to capture a tactile touch from the user to Camille, and given physical parameters (speed, pressure, distance, duration...) to infer a category of touch.

In order to do this :
* A trick with a spring joint following the hand of the user is used to estimate the pressure,
* Fuzzy-logic is used to guess the category of touch from the estimated physical parameters of touch.

Interpretation is printed in the console if `debug` is true.

`HandTouchManager` provides several subscribable events :

* `TouchStarted`, with `TouchEventArgs` parameters, which is triggered each time a new touch sequence is detected.
* `TouchChanged`, with `TouchEventArgs` parameters, which is triggered each time :
    * The force has changed significantly (relative to the defined threshold), or
    * The velocity has changed significantly (relative to the defined threshold)
    * *Note that these threshold can be changed in the editor, in `HandTouchManager` script.*
 * `TouchEnded`, with `TouchEventArgs` parameters, which is triggered at the end of a touch sequence (defined by an arbitrary amount of time during which nothing new happens).
 * `EtherealBodyEntered`, with `EtherealBodyEventArgs` parameters, which is triggered each time the human enters the "ethereal body" of Camille.
 * `EtherealBodyLeft`, with `EtherealBodyEventArgs` parameters, which is triggered each time the human leaves the "ethereal body" of Camille.


|:exclamation: Beware !|
|:---|
|The scripts for visual and tactile perception **heavily rely** on the exact positioning of colliders on Camille's skeleton, their parameters and their tags. Also, the user hand, made of a rigidbody (controlled by Optitrack or whatever tracking system), another object attached by a spring joint and another object attached by a fixed joint is **really** important so the whole thing works.|

Any modification in the hand or skeleton prefab which removes or change this configuration will break the perception system.
    
## Adding a new avatar model for the human in the VR HMD environment

In order to make the leap hands work with a custom avatar for the human, the current easiest way to proceed is as follows:
* Create the custom model so that it has a correct humanoid rigging for Unity.
* Edit the model so as to delete the hands' meshes (In blender: select the corresponding mesh -> Edit mode -> Select all the vertices corresponding to the hands and delete them), but make sure to keep the hand bones so that the humanoid rig can still be applied in Unity.
* Export the model as a fbx and make sure that rotations and scales are correct (in blender make sure to import with FBX unit scales for the rotation)
* Import the fbx in Unity and drop it into the scene.
* If you want to use the outline leap hands, replace all the materials of your new model by either a transparent material or an empty material.
* Add an outline shader to the character (here we use the free QuickOutline asset from the Unity Asset Store), preferably at the root of the character in Unity's editor hierarchy.
* Add the inverse kinematics script so that the target of each arm is the wrist of the leap hands.
* Play the scene and check that the arms follow the leap hands appropriately. In case of offsets, tweak the IK parameters.
