﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Collections;

public class CameraCondition : EnhancedTriggerBoxComponent
{
    /// <summary>
    /// The camera that will be used for the condition. By default this is the main camera
    /// </summary>
    public Camera cam;

    /// <summary>
    /// The type of condition you want. The Looking At condition only passes when the user can see a specific transform or gameobject. The Looking Away condition only passes when a transform or gameobject is out of the users camera frustum.
    /// </summary>
    public LookType cameraConditionType;

    /// <summary>
    /// This is the object that the condition is based upon.
    /// </summary>
    public GameObject conditionObject;

    /// <summary>
    /// The type of component the condition will be checked against.  Either transform (a single point in space), minimum box collider (any part of a box collider), full box collider (the entire box collider) or mesh renderer (any part of a mesh). For example with the Looking At condition and Minimum Box Collider, if any part of the box collider were to enter the camera's view, the condition would be met.
    /// </summary>
    public CameraConditionComponentParameters componentParameter;

    /// <summary>
    /// When using the Looking At condition type raycasts are fired to make sure nothing is blocking the cameras line of sight to the object. 
    /// Here you can customise how those raycasts should be fired. Ignore obstacles fires no raycasts and mean the condition will pass even
    /// if there is an object in the way. Very low does raycast checks at a maximum of once per second against the objects position. Low does
    /// raycast checks at a maximum of once per 0.1 secs against the objects position. Med does raycast checks once per frame against the objects
    /// position. High does raycast checks once per frame against every corner of the box collider.
    /// </summary>
    public RaycastIntensity raycastIntensity = RaycastIntensity.Med;

    /// <summary>
    /// This is the time that this camera condition must be met for in seconds. E.g. camera must be looking at object for 2 seconds for the condition to pass.
    /// </summary>
    public float conditionTime = 0f;

    /// <summary>
    /// The world to viewport point of the object the viewObject
    /// </summary>
    private Vector3 viewConditionScreenPoint = new Vector3();

    /// <summary>
    /// Holds raycast information when raycast checks are taking place. Only used when using the Looking At condition
    /// </summary>
    private RaycastHit viewConditionRaycastHit = new RaycastHit();

    /// <summary>
    /// This is the box collider of the viewObject. Only used when the condition involves Minimum Box Collider or Full Box Collider
    /// </summary>
    private BoxCollider viewConditionObjectCollider;

    /// <summary>
    /// This is the mesh renderer of the viewobject. Only used when the condition involves mesh renderer.
    /// </summary>
    private MeshRenderer viewConditionObjectMeshRenderer;

    /// <summary>
    /// The view planes of the camera
    /// </summary>
    private Plane[] viewConditionCameraPlane;

    /// <summary>
    /// This is a timer used to make sure the time the condition has been met for is longer than conditionTime
    /// </summary>
    private float viewTimer = 0f;

    /// <summary>
    /// The bool dictates whether the raycast check can be performed or not. 
    /// </summary>
    private bool canRaycast = true;

    /// <summary>
    /// The available types of camera conditions.
    /// </summary>
    public enum LookType
    {
        LookingAt,
        LookingAway,
    }

    /// <summary>
    /// The available component parameters that can be used with the camera condition.
    /// </summary>
    public enum CameraConditionComponentParameters
    {
        Transform,
        FullBoxCollider,
        MinimumBoxCollider,
        MeshRenderer,
    }

    /// <summary>
    /// The available options for raycast intensity. 
    /// </summary>
    public enum RaycastIntensity
    {
        IgnoreObstacles,
        VeryLow,
        Low,
        Med,
        High,
    }

    public override void DrawInspectorGUI()
    {
        cam = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera",
               "The camera that will be used for the condition. By default this is the main camera"), cam, typeof(Camera), true);

        cameraConditionType = (LookType)EditorGUILayout.EnumPopup(new GUIContent("Condition Type",
            "The type of condition you want. The Looking At condition only passes when the user can see a specific transform or gameobject. The Looking Away condition only passes when a transform or gameobject is out of the users camera frustum."), cameraConditionType);

        conditionObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Condition Object",
            "This is the object that the condition is based upon."), conditionObject, typeof(GameObject), true);

        componentParameter = (CameraConditionComponentParameters)EditorGUILayout.EnumPopup(new GUIContent("Component Parameter",
            "The type of component the condition will be checked against.  Either transform (a single point in space), minimum box collider (any part of a box collider), full box collider (the entire box collider) or mesh renderer (any part of a mesh). For example with the Looking At condition and Minimum Box Collider, if any part of the box collider were to enter the camera's view, the condition would be met."), componentParameter);

        if (cameraConditionType == LookType.LookingAt && componentParameter != CameraConditionComponentParameters.MeshRenderer)
        {
            raycastIntensity = (RaycastIntensity)EditorGUILayout.EnumPopup(new GUIContent("Raycast Intesity",
                "When using the Looking At condition type raycasts are fired to make sure nothing is blocking the cameras line of sight to the object. Here you can customise how those raycasts should be fired. Ignore obstacles fires no raycasts and mean the condition will pass even if there is an object in the way. Very low does raycast checks at a maximum of once per second against the objects position. Low does raycast checks at a maximum of once per 0.1 secs against the objects position. Med does raycast checks once per frame against the objects position. High does raycast checks once per frame against every corner of the box collider."), raycastIntensity);
        }

        conditionTime = EditorGUILayout.FloatField(new GUIContent("Condition Time",
            "This is the time that this camera condition must be met for in seconds. E.g. camera must be looking at object for 2 seconds for the condition to pass."), conditionTime);
    }

    public override void Validation()
    {
        // If the camera is null we'll set it to the main camera as this is what most people will be using. But users can still change it if they wish.
        if (!cam)
        {
            cam = Camera.main;
        }
        else if (componentParameter == CameraConditionComponentParameters.MeshRenderer && Camera.allCamerasCount > 1)
        {
            ShowWarningMessage("You have selected the Component Parameter for the camera condition to be Mesh Renderer however you have more than 1 camera in the scene. The condition will pass if the mesh can be seen by ANY camera, not just the one you selected above. This is a limitation caused by the MeshRenderer().isVisible bool.");
        }

        // Check if the user specified a gameobject to focus on 
        if (!conditionObject)
        {
            ShowWarningMessage("You have selected the " + ((cameraConditionType == LookType.LookingAt) ? "Looking At" : "Looking Away") + " camera condition but have not specified the gameobject for the condition!");
        }
        else
        {
            // If the user has selected full box collider check the object has a box collider
            if (componentParameter == CameraConditionComponentParameters.FullBoxCollider || componentParameter == CameraConditionComponentParameters.MinimumBoxCollider)
            {
                if (conditionObject.GetComponent<BoxCollider>() == null)
                {
                    ShowWarningMessage("You have selected the Component Parameter for the camera condition to be " + ((componentParameter == CameraConditionComponentParameters.FullBoxCollider) ? "Full Box Collider" : "Minimum Box Collider") + " but the object doesn't have a Box Collider component!");
                }
            } // Else if the user selected mesh render check the object has mesh renderer
            else if (componentParameter == CameraConditionComponentParameters.MeshRenderer)
            {
                if (conditionObject.GetComponent<MeshRenderer>() == null)
                {
                    ShowWarningMessage("You have selected the Component Parameter for the camera condition to be Mesh Renderer but the object doesn't have a Mesh Renderer component!");
                }
            }
        }

        if (componentParameter == CameraConditionComponentParameters.MeshRenderer && raycastIntensity == RaycastIntensity.High)
        {
            ShowWarningMessage("High raycast intensity will have no extra effect than med when using mesh renderer. This is because high intensity uses all the points from the a box colliders bounds but mesh renderers do not have bounds.");
        }

        // Check that condition time is above 0
        if (conditionTime < 0f)
        {
            ShowWarningMessage("You have set the camera condition timer to be less than 0 which isn't possible!");
        }
    }

    /// <summary>
    /// Cache the collider or mesh renderer so we do not do GetComponent every frame
    /// </summary>
    public override void OnAwake()
    {
        if (conditionObject)
        {
            if (componentParameter == CameraConditionComponentParameters.FullBoxCollider || componentParameter == CameraConditionComponentParameters.MinimumBoxCollider)
            {
                viewConditionObjectCollider = conditionObject.GetComponent<BoxCollider>();
            }
            else if (componentParameter == CameraConditionComponentParameters.MeshRenderer) // Cache the mesh renderer
            {
                viewConditionObjectMeshRenderer = conditionObject.GetComponent<MeshRenderer>();
            }
        }
    }

    public override bool ExecuteAction()
    {
        // This fixes a bug that occured when the player was very close to an object. Is this necessary? TODO: Find out if this is necessary
        if (Vector3.Distance(cam.transform.position, conditionObject.transform.position) < 2f)
        {
            return false;
        }

        switch (cameraConditionType)
        {
            case LookType.LookingAt:
                switch (componentParameter)
                {
                    case CameraConditionComponentParameters.Transform:
                        // This checks that the objects position is within the camera frustum
                        if (IsInCameraFrustum(conditionObject.transform.position))
                        {
                            // If ignore obstacles is true we don't need to raycast to determine if there's anything blocking
                            // the line of sight
                            if (raycastIntensity == RaycastIntensity.IgnoreObstacles)
                            {
                                // Check if we this condition has been met for longer than the conditionTimer
                                return CheckConditionTimer();
                            }
                            else
                            {
                                // Check if there's any objects in the way using raycasts
                                if (CheckRaycastTransform(conditionObject.transform.position - cam.transform.position))
                                {
                                    // Check if we this condition has been met for longer than the conditionTimer
                                    return CheckConditionTimer();
                                }
                            }
                        }
                        break;

                    case CameraConditionComponentParameters.MinimumBoxCollider:
                        // This test determines whether any bound is within the camera planes.
                        if (CheckMinimumBoxCollider(viewConditionObjectCollider.bounds))
                        {
                            if (raycastIntensity == RaycastIntensity.IgnoreObstacles)
                            {
                                return CheckConditionTimer();
                            }
                            else if (raycastIntensity == RaycastIntensity.High)
                            {
                                // If raycast intensity is set to high we'll raycast to every corner on the bounds instead of just the position
                                if (CheckRaycastMinimumCollider(viewConditionObjectCollider.bounds))
                                {
                                    return CheckConditionTimer();
                                }
                            }
                            else
                            {
                                if (CheckRaycastTransform(conditionObject.transform.position - cam.transform.position))
                                {
                                    return CheckConditionTimer();
                                }
                            }
                        }
                        break;

                    case CameraConditionComponentParameters.FullBoxCollider:
                        // The test determines if the all the bounds are in the camera planes
                        if (CheckFullBoxCollider(viewConditionObjectCollider.bounds))
                        {
                            if (raycastIntensity == RaycastIntensity.IgnoreObstacles)
                            {
                                return CheckConditionTimer();
                            }
                            else if (raycastIntensity == RaycastIntensity.High)
                            {
                                if (CheckRaycastFullCollider(viewConditionObjectCollider.bounds))
                                {
                                    return CheckConditionTimer();
                                }
                            }
                            else
                            {
                                if (CheckRaycastTransform(conditionObject.transform.position - cam.transform.position))
                                {
                                    return CheckConditionTimer();
                                }
                            }
                        }
                        break;

                    case CameraConditionComponentParameters.MeshRenderer:
                        // This is much simpler. Uses the built in isVisible checks to determine if the mesh can be seen by any camera.
                        if (viewConditionObjectMeshRenderer.isVisible)
                        {
                            if (raycastIntensity != RaycastIntensity.IgnoreObstacles)
                            {
                                return CheckConditionTimer();
                            }
                            else
                            {
                                if (CheckRaycastTransform(conditionObject.transform.position - cam.transform.position))
                                {
                                    return CheckConditionTimer();
                                }
                            }
                        }
                        break;
                }
                break;

            case LookType.LookingAway: // The below is nearly identical except for not having raycast checks. Do we need to add raycast checks?
                switch (componentParameter)
                {
                    case CameraConditionComponentParameters.Transform:
                        if (!IsInCameraFrustum(conditionObject.transform.position))
                        {
                            return CheckConditionTimer();
                        }
                        break;

                    case CameraConditionComponentParameters.MinimumBoxCollider:
                        if (CheckMinimumBoxCollider(viewConditionObjectCollider.bounds))
                        {
                            return CheckConditionTimer();
                        }
                        break;

                    case CameraConditionComponentParameters.FullBoxCollider:
                        if (CheckFullBoxCollider(viewConditionObjectCollider.bounds))
                        {
                            return CheckConditionTimer();
                        }
                        break;

                    case CameraConditionComponentParameters.MeshRenderer:
                        if (!viewConditionObjectMeshRenderer.isVisible)
                        {
                            return CheckConditionTimer();
                        }
                        break;
                }
                break;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a single corner on the bounds is within the camera frustum. Or is lookingAt is false, if a single corner is outside of the camera frustum.
    /// </summary>
    /// <param name="bounds">The object bounds that will be inspected</param>
    /// <returns>True or false depending on if any point in the bounds is inside or outside of the camera frustum</returns>
    private bool CheckMinimumBoxCollider(Bounds bounds)
    {
        // For every point on the bounding box check if it is inside the camera frustum. If it is then the condition is true.
        if (cameraConditionType == LookType.LookingAt)
        {
            if (IsInCameraFrustum(bounds.min))
            {
                return true;
            }

            if (IsInCameraFrustum(bounds.max))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)))
            {
                return true;
            }

            if (IsInCameraFrustum(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }

            return false;
        }
        else // For every point on the bounding box check if it is outside the camera frustum. If it is then the condition is true.
        {
            if (!IsInCameraFrustum(bounds.min))
            {
                return true;
            }

            if (!IsInCameraFrustum(bounds.max))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)))
            {
                return true;
            }

            if (!IsInCameraFrustum(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Checks if every point in a bounding box is inside or outside of the camera frustum
    /// </summary>
    /// <param name="bounds">The bounds to be inspected</param>
    /// <returns>True or false depending on if all point in the bounds is inside or outside of the camera frustum</returns>
    private bool CheckFullBoxCollider(Bounds bounds)
    {
        if (cameraConditionType == LookType.LookingAt)
        {
            if (IsInCameraFrustum(bounds.min) &&
                IsInCameraFrustum(bounds.max) &&
                IsInCameraFrustum(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)) &&
                IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)) &&
                IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)) &&
                IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)) &&
                IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)) &&
                IsInCameraFrustum(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (!IsInCameraFrustum(bounds.min) &&
                !IsInCameraFrustum(bounds.max) &&
                !IsInCameraFrustum(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)) &&
                !IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)) &&
                !IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)) &&
                !IsInCameraFrustum(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)) &&
                !IsInCameraFrustum(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)) &&
                !IsInCameraFrustum(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Checks if a single point is within the cameras planes. Does not check if any obstacles are in the way
    /// </summary>
    /// <param name="position">The point that will be checked</param>
    /// <returns>True or false depending on whether the point is in the cameras view or not</returns>
    private bool IsInCameraFrustum(Vector3 position)
    {
        // Get the viewport point of the object from its position
        viewConditionScreenPoint = cam.WorldToViewportPoint(position);

        // This checks that the objects position is within the camera frustum
        if (viewConditionScreenPoint.z > 0 && viewConditionScreenPoint.x > 0 &&
            viewConditionScreenPoint.x < 1 && viewConditionScreenPoint.y > 0 && viewConditionScreenPoint.y < 1)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// This function fires a raycast from the camera to the camera condition object to determine if there's any obstacles blocking the camera line of sight.
    /// </summary>
    /// <returns>Returns true or false depending if theres any obstacles in the way.</returns>
    private bool CheckRaycastTransform(Vector3 viewConditionDirection)
    {
        if (!canRaycast)
        {
            return false;
        }

        // Fire raycast from camera in the direction of the viewobject
        if (Physics.Raycast(cam.transform.position, viewConditionDirection.normalized, out viewConditionRaycastHit, viewConditionDirection.magnitude))
        {
            // If it hit something, inspect the returned data to set if it is the object we are checking that the camera can see
            if (viewConditionRaycastHit.transform == conditionObject.transform)
            {
                return true;
            }

            if (raycastIntensity == RaycastIntensity.Low || raycastIntensity == RaycastIntensity.VeryLow)
                StartCoroutine("WaitForSecs");

            // If it hit something which wasn't the correct object then something is in the way and we return false
            return false;
        }

        // The raycast hit nothing at all so there's nothing in the way so return true
        return true;
    }

    /// <summary>
    /// Fires a raycast to every point on a bounding box to see if it is in view of the camera. If the raycast check is successful on any point it will return true.
    /// </summary>
    /// <param name="bounds">The bounds to be inspected</param>
    /// <returns>True or false depending on if any points in the bounding box are in view of the camera</returns>
    private bool CheckRaycastMinimumCollider(Bounds bounds)
    {
        if (CheckRaycastTransform(bounds.min - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(bounds.max - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z) - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z) - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z) - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z) - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z) - cam.transform.position))
        {
            return true;
        }

        if (CheckRaycastTransform(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z) - cam.transform.position))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Fires a raycast to every point on a bounding box to see if it is in view of the camera. The raycast check must be successful on every point.
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    private bool CheckRaycastFullCollider(Bounds bounds)
    {
        if (CheckRaycastTransform(bounds.min - cam.transform.position) &&
            CheckRaycastTransform(bounds.max - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z) - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z) - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z) - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z) - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z) - cam.transform.position) &&
            CheckRaycastTransform(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z) - cam.transform.position))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// This function checks to make sure the condition has been met for a certain amount of time.
    /// </summary>
    /// <returns>Returns true or false depending on if the condition has been met for a certain about of time.</returns>
    private bool CheckConditionTimer()
    {
        if (viewTimer >= conditionTime)
        {
            return true;
        }
        else
        {
            viewTimer += Time.fixedDeltaTime;
        }

        return false;
    }

    /// <summary>
    /// Coroutine that stops raycast checks being performed for a number of seconds
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitForSecs()
    {
        canRaycast = false;
        switch (raycastIntensity)
        {
            case RaycastIntensity.VeryLow:
                yield return new WaitForSeconds(1f);
                break;

            case RaycastIntensity.Low:
                yield return new WaitForSeconds(0.1f);
                break;
        }

        canRaycast = true;
    }
}