﻿using UnityEngine;

public class FollowMouse : MonoBehaviour
{

    private bool showLine = true; 

	public float Speed = 1.5f;
	private Vector3 _target;

    private LineRenderer lineRenderer;
    public Material material;

    void Start()
    {
		_target = transform.position;

        lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        //lineRenderer.material.color = UnityEngine.Color.white;

        // Assigns a material named "Assets/Resources/DEV_Orange" to the object.
        //Material newMat = Resources.Load("Materials/lineColor") as Material;
        //Debug.Log(newMat);
        //lineRenderer.material = newMat;
    }

    void Update()
    {
		//don't rotate main camera
		Camera.main.transform.rotation = Quaternion.identity;


        if (AntAlgorithmManager.Instance.IsGameFinished)
        {
            Speed = 0;
            return;
        }

        if (showLine)
        {
            // set Positions of line
            lineRenderer.SetPosition(0, transform.position); // snakehead position
            lineRenderer.SetPosition(1, AntAlgorithmManager.Instance.getNextPosition()); // "optimal" next position
        }
    }

    void FixedUpdate()
    {
        _target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _target.z = transform.position.z;

        //follow mouse
        transform.position = Vector3.MoveTowards(transform.position, _target, Speed * Time.deltaTime);

        float angle = Mathf.Atan2(_target.y - Camera.main.transform.position.y, _target.x - Camera.main.transform.position.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}