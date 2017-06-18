﻿using System.Collections.Generic;
using System.Linq;
using AntAlgorithm;
using AntAlgorithm.tools;
using util;
using UnityEngine;
using System.Collections;

public class AntAlgorithmManager : Singleton<AntAlgorithmManager>
{
    public enum ScalingMethod
    {
        Max,
        MinMax
    }

	[HideInInspector]
	public string playerName;

    [SerializeField] private uint GameBoardSize = 30;
    [SerializeField] private float MaximumEnlargementFactor = 0.5f;
    [SerializeField] private ScalingMethod Scaling;

    protected AntAlgorithmManager() {
	}

    private AntAlgorithmSimple _antAlgorithm;
    private const string TspFileToUse = "berlin52.tsp";
    private GameObject[] _remainingFood;
    private List<int> _userTour;
    private List<City> _userTourCities;
    private Vector3 _nextBestFoodPosition;

    public List<City> Cities { get; private set; }

    public bool IsGameFinished
    {
        get { return GameObject.FindGameObjectsWithTag("Food").Length == 0; }
    }

    public void Start()
    {
		playerName = PlayerPrefs.GetString ("PlayerName");
		Debug.Log ("AntAlgorithmManager Start(). Player name = " + playerName);

        Debug.Log("--- FIND EDITION ---");
        

        #if UNITY_STANDALONE_WIN
            Debug.Log("Stand Alone Windows");
            Cities = TSPImporter.ImportTsp(TspFileToUse);
            init();
        #endif


        #if UNITY_WEBGL
            Debug.Log("WebGL");
        
            TSPImporter tsp = new TSPImporter();
            IEnumerator startTspWebLoad = tsp.ImportTspFromWeb(TspFileToUse);
            StartCoroutine(startTspWebLoad);
            IEnumerator waitLoad = waitUntilLoadFinished(tsp);
            StartCoroutine(waitLoad);
        #endif

    }

	public void Awake()
	{
		Debug.Log ("Awake called!");

	}	

    public IEnumerator waitUntilLoadFinished(TSPImporter tsp)
    {
        while (!tsp.loadingComplete)
            yield return new WaitForEndOfFrame();

        Debug.Log(" ---- Web-Load Finished ----- ");
        Cities = tsp.Cities;
        Debug.Log("Count: ");
        Debug.Log(Cities.Count);
        Debug.Log(Cities);

        init();
        yield break;
    }

    public void init()
    {
        _remainingFood = new GameObject[Cities.Count];
        _antAlgorithm = transform.GetOrAddComponent<AntAlgorithmSimple>();
        _antAlgorithm.SetCities(Cities);
        _antAlgorithm.Init();
        _userTour = new List<int>();
        _userTourCities = new List<City>();
        FoodController.InitializeFoodPositions(GameBoardSize);

        _nextBestFoodPosition = new Vector3(0, 0, 0); // init
        RunXIterations(52*5);
        PrintBestTour("algo best tour: ");
    }

    public void RunXIterations(int numIter)
    {
        for (var i = 0; i < numIter; ++i)
        {
            _antAlgorithm.Iteration();
        }
        //_antAlgorithm.PrintBestTour("After Run");
    }

    public void PrintBestTour(string str)
    {
        _antAlgorithm.PrintBestTour(str);
    }

    public void RegisterFood(int id, GameObject go)
    {
        Debug.Assert(_remainingFood[id] == null);
        _remainingFood[id] = go;
        Debug.Assert(_remainingFood[id] != null);
    }

    public Vector3 getNextPosition()
    {
        return _nextBestFoodPosition;
    }

    public void UnregisterEatenFood(int id)
    {
        _userTour.Add(id);
        _userTourCities.Add(Cities.ElementAt(id));
        Vector3 from = _remainingFood[id].transform.position;

        _remainingFood[id] = null;
        var pheromones = _antAlgorithm.Pheromones.GetPheromones(id);
        var max = GetRemainingMaximum(pheromones);
        var min = GetRemainingMinimum(pheromones);

        //Debug.Log(string.Format("PHEROMONES - Min: {0} - Max: {1}", min, max));

        for (int idx = 0; idx < pheromones.Length; idx++)
        {
            if(_remainingFood[idx] == null)
                continue;
            //_remainingFood[idx].GetComponent<FoodController>().Rescale(GetScalingFactor(min, pheromones[idx], max));
			_remainingFood[idx].GetComponent<FoodController>().Redye(GetRedyeFactor(min, pheromones[idx], max));
            
            // set nextBestFoodPosition because of maximum of pheromones
            if (max == pheromones[idx])
            {
                _nextBestFoodPosition = _remainingFood[idx].transform.position;
            }
        }

        UpdatePheromones();
        RunXIterations(5);
    }

    private void UpdatePheromones()
    {
        if (_userTour.Count < 2) return;
        var lastIdx = _userTour.Count - 1;
        var cityA = _userTour[lastIdx];
        var cityB = _userTour[lastIdx - 1];
        _antAlgorithm.Pheromones.IncreasePheromone(cityA, cityB, _antAlgorithm.Pheromones.GetPheromone(cityA, cityB));
    }

    #region Helper Methods
    private float GetScalingFactor(double min, double value, double max)
    {
        switch (Scaling)
        {
            case ScalingMethod.Max:
                return 1 + (float)(value / max) * MaximumEnlargementFactor;
            case ScalingMethod.MinMax:
                return 1 + (float)((value - min) / (max - min)) * MaximumEnlargementFactor;
            default:
                return 1;
        }
    }

	//returns value between 0 (for min value) and 1 (for max value)
	private float GetRedyeFactor(double min, double value, double max)
	{
		double divisor = max - min;
		if (divisor == 0) 
        {
			divisor = 0.001;
		}

		return (float)( (value - min) / divisor );
	}

    private double GetRemainingMaximum(double[] arr)
    {
        var tmp = (double[])arr.Clone();

        foreach (var visitedCityIdx in _userTour)
        {
            tmp[visitedCityIdx] = double.MinValue;
        }
        return tmp.Max();
    }

    private double GetRemainingMinimum(double[] arr)
    {
        var tmp = (double[])arr.Clone();

        foreach (var visitedCityIdx in _userTour)
        {
            tmp[visitedCityIdx] = double.MaxValue;
        }
        return tmp.Min();
    }

    public double CalcOverallUserDistance()
    {
        double distance = 0;
        
        for(int i = 0; i < _userTourCities.Count - 1; i++)
        {
            City city1 = _userTourCities.ElementAt(i);
            City city2 = _userTourCities.ElementAt(i+1);

            Vector2 city1pos = new Vector2(city1.getXPosition(), city1.getYPosition());
            Vector2 city2pos = new Vector2(city2.getXPosition(), city2.getYPosition());

            distance += Vector2.Distance(city1pos, city2pos);
        }

        return distance;
    }

    public double CalcOverallDistance(List<City> cities)
    {
        double distance = 0;
        
        for(int i = 0; i < cities.Count - 1; i++)
        {
            City city1 = cities.ElementAt(i);
            City city2 = cities.ElementAt(i+1);

            Vector2 city1pos = new Vector2(city1.getXPosition(), city1.getYPosition());
            Vector2 city2pos = new Vector2(city2.getXPosition(), city2.getYPosition());

            distance += Vector2.Distance(city1pos, city2pos);
        }

        return distance;
    }

    #endregion
}