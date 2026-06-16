# VR grab setup

## Packages

The project already contains these packages in `Packages/manifest.json`:

- XR Interaction Toolkit `3.0.10`
- OpenXR Plugin `1.16.1`
- Input System `1.18.0`
- XR Mock HMD `1.5.0-exp.3`

If any of them are missing in another copy of the project, install them through Window > Package Manager.

## Create the test scene

1. Open the project in Unity.
2. Wait for scripts to compile.
3. Run Tools > VR > Create Grab Test Scene.
4. Open `Assets/Scenes/VRGrabTestScene.unity` if Unity did not open it automatically.
5. Press Play with an OpenXR headset connected, or add/import XR Device Simulator for editor-only testing.

The generated scene contains:

- XR Origin (XR Rig)
- Main Camera
- Left Controller
- Right Controller
- XR Interaction Manager
- XR Direct Interactor for each hand
- XR Ray Interactor for each hand
- floor with collider
- light
- world-space Canvas
- EventSystem with XR UI Input Module
- button that calls `VRTestSpawner.SpawnRandomObject`
- test cube, sphere, and capsule spawned by `VRTestSpawner`

## Project Settings

Check these settings:

- Edit > Project Settings > Player > Active Input Handling: Input System Package or Both.
- Edit > Project Settings > XR Plug-in Management: initialize XR on startup.
- XR Plug-in Management > OpenXR: enabled for your target platform.
- OpenXR Interaction Profiles: add the profile for your headset/controllers, for example Oculus Touch Controller Profile, Valve Index Controller Profile, or Meta Quest Touch Plus Controller Profile.
- XR Interaction Toolkit validation: run Fix All where appropriate.

## Hands and controllers

Each controller should have:

- Tracked Pose Driver (Input System)
- XR Controller (Action-based) for compatibility with older XRI workflows
- XR Ray Interactor
- XR Interactor Line Visual
- child object `Left Direct Interactor` / `Right Direct Interactor`
- XR Direct Interactor on the child
- trigger SphereCollider on the direct interactor child
- kinematic Rigidbody on the direct interactor child

The direct interactor uses grip/select for grabbing nearby objects. The ray interactor uses trigger/UI Press for world-space UI.

## Grabbable objects

Each grabbable object should have:

- Rigidbody
- non-trigger Collider
- XR Grab Interactable
- `VRGrabbableSetup` if you want the object to self-configure

Recommended Rigidbody settings:

- Use Gravity: true
- Is Kinematic: false
- Collision Detection: Continuous Dynamic
- Interpolate: Interpolate

Recommended XR Grab Interactable settings:

- Movement Type: Velocity Tracking
- Track Position: true
- Track Rotation: true
- Throw On Detach: true
- Retain Transform Parent: false

## Editor testing

For fast editor testing:

1. Import XR Interaction Toolkit samples if they are not imported:
   - Starter Assets
   - XR Device Simulator
2. Use Tools > VR > Add XR Device Simulator To SampleScene for the existing scene, or place the simulator prefab manually in `VRGrabTestScene`.
3. Press Play and use the simulator controls to move the HMD/controllers.

## Troubleshooting checklist

- Hands do not see objects: check the direct interactor trigger collider radius, interaction layer masks, and that the object collider is not a trigger.
- Object is not grabbed: check `XRGrabInteractable`, `Rigidbody`, enabled collider, and select/grip input binding.
- Object falls through floor: check floor collider, object collider, Rigidbody collision detection, and object scale.
- UI is not clickable: Canvas must be World Space, have `TrackedDeviceGraphicRaycaster`, EventSystem must use `XRUIInputModule`, and ray interactor must have UI interaction enabled.
- VR does not start: check XR Plug-in Management, OpenXR enabled for the build target, headset runtime selected as active OpenXR runtime, and Initialize XR on Startup.
- Input Actions do not work: check Active Input Handling, XRI Default Input Actions or direct actions, imported Starter Assets, and OpenXR interaction profiles.
