﻿using System.Collections.Generic;
using AntAlgorithm;
using util;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class FoodController : MonoBehaviour
{
    private static GameObject _foodPrefab;
    private static GameObject _arrowPrefab;
    private static Vector3 _defaultScale;

    public int Id { get; private set; }

    public static void InitializeFoodPositions(uint gameboardSize)
    {
        //get min and max values of x and y coordinates, so that we can calculate normalized values
        int minX, maxX, minY, maxY;

        var cities = AntAlgorithmManager.Instance.Cities;

        GetExtremeCityCoordinates(cities, out minX, out maxX, out minY, out maxY);

        int absoluteMaxX = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX));
        int absoluteMaxY = Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY));

        //initialize food objects with normalized values
        foreach (var city in cities)
        {
            float xPos = (city.getXPosition() / (float)absoluteMaxX) * gameboardSize;
            float yPos = (city.getYPosition() / (float)absoluteMaxY) * gameboardSize;
            Vector3 pos = new Vector3(xPos, yPos, 0);

            Create(pos, city.getId());
            //foodGameObject.transform.localScale *= Random.Range (0.8f, 2.5f); //TODO: use real values!
        }
    }

    public static FoodController Create(Vector3 position, int id)
    {
        if (_foodPrefab == null)
        {
            _foodPrefab = Resources.Load("Prefabs/FoodPrefab") as GameObject;
            _defaultScale = _foodPrefab.transform.localScale;
        }
        if(_arrowPrefab == null)
            _arrowPrefab = Resources.Load("Prefabs/ArrowPointAtObject") as GameObject;

        var foodGameObject = Instantiate(_foodPrefab, position, Quaternion.identity);
        var controller = foodGameObject.transform.GetOrAddComponent<FoodController>();
        controller.Id = id;

        var arrowThatPointsToFood = Instantiate(_arrowPrefab, Vector3.zero, Quaternion.identity);
        arrowThatPointsToFood.transform.parent = Camera.main.transform;
        arrowThatPointsToFood.GetComponent<PointAtObject>().objectToPointAt = foodGameObject;

        AntAlgorithmManager.Instance.RegisterFood(id, foodGameObject);
        return controller;
    }

    public void Rescale(float newScaleFactor = 1f)
    {
        gameObject.transform.localScale = _defaultScale * newScaleFactor;
    }

    public void OnDestroy()
    {
        //Debug.Log(string.Format("City {0} visited.", Id));
        AntAlgorithmManager.Instance.UnregisterEatenFood(Id);
    }

    #region Helper Methods

    private static void GetExtremeCityCoordinates(IEnumerable<City> cities, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = minY = int.MaxValue;
        maxX = maxY = int.MinValue;

        foreach (var city in cities)
        {
            var x = city.getXPosition();
            var y = city.getYPosition();

            if (x < minX)
                minX = x;
            if (x > maxX)
                maxX = x;
            if (y < minY)
                minY = y;
            if (y > maxY)
                maxY = y;
        }
    }
    #endregion
}
