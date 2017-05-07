﻿using gui;
using UnityEngine;

public class Eating : MonoBehaviour
{
	public int maxSnakeLength = 15;
	public GameObject snakeBodyPrefab;
	private int currentSnakeLength = 3;
	private GameObject lastSnakeBodyPart;
	private AudioSource audioSource;

    private RangeDisplayController rangeDisplayController;

	void Start()
    {
		lastSnakeBodyPart = GameObject.Find("snake_body_2");
		audioSource = GetComponent<AudioSource>();
        rangeDisplayController = GameObject.FindGameObjectWithTag("RangeDisplay").GetComponent<RangeDisplayController>();
	}

	void OnTriggerEnter2D (Collider2D other)
    {
        if (!other.gameObject.CompareTag("Food"))
        {
            return;
        }

        rangeDisplayController.UpdateRange(other.gameObject.transform.position);
        Destroy(other.gameObject);

		audioSource.Play();
		if (currentSnakeLength >= maxSnakeLength)
        {
			return;
		}

		//create new snake body part like defined prefab
		Vector3 pos = new Vector3(-100.0f,-100.0f,0);
		GameObject newSnakeBodyPart = Instantiate(snakeBodyPrefab, pos, Quaternion.identity);
		newSnakeBodyPart.name = "snake_body_" + currentSnakeLength;
		newSnakeBodyPart.GetComponent<SpriteRenderer>().color = new Color (Random.value, Random.value, Random.value);
		newSnakeBodyPart.GetComponent<SpriteRenderer>().sortingOrder = -currentSnakeLength;

		//new part should follow last part
		var followGameObject = newSnakeBodyPart.GetComponent<FollowGameObjectThatProvidesLastPosition>();
		followGameObject.objectToFollow = lastSnakeBodyPart;

		//we have a new last part
		lastSnakeBodyPart = newSnakeBodyPart;
		currentSnakeLength++;
	}
}
