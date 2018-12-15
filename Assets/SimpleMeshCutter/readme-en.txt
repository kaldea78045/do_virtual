/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_
// Simple Mesh Cutter
// © 2017-2018 Kazuya Hiruma
// Version 1.1.0
/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_

Simple Mesh Cutter provide simple mesh cut solution.

=====================================================================================
## Update history

Version 1.1.0:
Improvement threading. Cutter class uses thread pool for cutting.
Adding new feature callback for cut method.

Bug fixed. Ver1.0.0 had a workder thread bug.
The bug was using non thread safe variables. It has been fixed to thread safe.


Version 1.0.0:
- First Release
=====================================================================================

## How to use

This asset uses two components for cutting.

- Cutter
- CutterTarget


### CutterTarget

This componet is used to cutter target. To attach this componet to for cutting target objects.

This component show two parameters in an inspector.
First, "Cut Material" that is a material for the cut surface.

This asset will create a surface for cutting. The material will be attached to the surface.
If you don't set a material, the system will use "Cutter" component's default material.

Second, "Mesh" that is the mesh of cut target object.
The system will search the mesh from indicating "Mesh" object, then cut this.


### Cutter

Cutter component execute cutting to the cut target object.
You should attach this component to an empty game object or something like that.

#### Execute cutting

To execute cutting is to use "Cut" method.
Syntax is below.

```
public void Cut(CutterTarget target, Vector3 position, Vector3 normal, CutterCallback callback = null, object userdata = null);
```

First argument is "CutterTarget".
Second argument is a position for cutting.
Third argument is a normal vector for the cutting plane.

--------------------------------------
[Version 1.1.0 update]
Fourth argument is callback for end of cutting.
Fifth argument is just user data. It will only use it in callback argument.

CutterCallback Syntax

```
public delegate void CutterCallback(bool success, GameObject[] cuttedObjects, CutterPlane plane, object userdata);
```

First argumet is a flag for cutting succee.
Second argument is cutted objects array.
Third argument is to used cutting plane.
Fourth argument is user data when passing Cutter.Cut method.
--------------------------------------


#### Callback is invoked when cutting is done.

Cutter component has two callback actions.

OnCutted will be called when cutting is done.
It has two arguments. First one is cutted objects, second one is the cutting plane.

OnFailed will be called when cutting is failed.
Cutting failed case is if all vertices of mesh are one side.

--------------------------------------
[Version 1.1.0 update]

You can use callback as an argument for Cutter.Cut method.
The callback will be passed a user data. OnCutted event never passed a user data.
--------------------------------------

