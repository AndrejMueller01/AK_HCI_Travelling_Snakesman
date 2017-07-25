﻿using System;
using System.Collections.Generic;
using System.Linq;
using AntAlgorithm;
using AntAlgorithm.tools;
using util;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AntAlgorithmManager : Singleton<AntAlgorithmManager>
{
    [SerializeField] private uint GameBoardSize = 25;

    protected AntAlgorithmManager()
    {
        //PlayerPrefs.DeleteAll();
        _initializationFinished = false;
        Cities = new List<City>();
        _userTour = new List<int>();
        _userTourCities = new List<City>();
    }

    private AntAlgorithmSimple _antAlgorithm;

    public const string GameName = "TravellingSnakesman";
    public const int NumHighScoreEntries = 5;

    private string TspFileToUse;
    private string TspFileName;
    private GameObject[] _remainingFood;
    private readonly List<int> _userTour;
    private readonly List<City> _userTourCities;
    private bool _initializationFinished;
    private Vector3 _nextBestFoodPosition;

    public List<City> Cities { get; private set; }

    public double BestTourLength { get { return _antAlgorithm.BestTourLength; } }

    public double BestAlgorithmLength { get; private set; }

    public bool IsGameFinished
    {
        get { return _initializationFinished && GameObject.FindGameObjectsWithTag("Food").Length == 0; }
    }


    public void Start()
    {
        TspFileName = PlayerPrefs.GetString("TspName");
        _initializationFinished = false;
        Debug.Log(string.Format("!Start called on {0}!", GetHashCode()));
        Debug.Log("--- FIND EDITION ---");

#if UNITY_STANDALONE_WIN
        Debug.Log("Stand Alone Windows");
        Cities = TSPImporter.ImportTsp(TspFileToUse);
        Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif

#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
        TSPImporter tsp = new TSPImporter();
        Debug.Log("WebGL or Mobile");
        TspFileToUse = TspFileName + ".tsp";
        StartCoroutine(tsp.importTspFromWebWebGL(TspFileToUse));
        StartCoroutine(initWebGL(tsp));
        
        Cities = tsp.Cities;
        #endif
    }

    private IEnumerator initWebGL(TSPImporter tsp)
    {

        while (!tsp.loadingComplete)
            yield return new WaitForSeconds(0.1f);
        Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainGameScreen")
        {
            return;
        }
        Init();
    }

    public void Update()
    {
        if (IsGameFinished)
        {
            _initializationFinished = false;
        }
    }

    public void Init()
    {
        Debug.Log(string.Format("!RUNNING INIT on {0}!", GetHashCode()));
        if (_userTour.Count != 0)
        {
            _userTour.Clear();
            Debug.Log("\tUser tour cleared.");
        }
        if (_userTourCities.Count != 0)
        {
            _userTourCities.Clear();
            Debug.Log("\tUser tour cities cleared.");
        }
        if (Cities.Count == 0)
        {
            Debug.Log("\tNo Cities loaded.");
        }

        _remainingFood = new GameObject[Cities.Count];
        _antAlgorithm = transform.GetOrAddComponent<AntAlgorithmSimple>();
        _antAlgorithm.SetCities(Cities);
        _antAlgorithm.Init();
        FoodController.InitializeFoodPositions(GameBoardSize);

        _nextBestFoodPosition = new Vector3(0, 0, 0); // init
        RunXIterations(52*5);
        PrintBestTour("algo best tour: ");
        BestAlgorithmLength = BestTourLength;
        _initializationFinished = true;
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

    public Vector3 GetNextPosition()
    {
        return _nextBestFoodPosition;
    }

    public void UnregisterEatenFood(int id)
    {
        Debug.Log(string.Format("!UnregisterEatenFood on {0}!", GetHashCode()));
        _userTour.Add(id);
        _userTourCities.Add(Cities[id]);

        _remainingFood[id] = null;
        var pheromones = _antAlgorithm.Pheromones.GetPheromones(id);
        var max = GetRemainingMaximum(pheromones);
        var min = GetRemainingMinimum(pheromones);

        //Debug.Log(string.Format("PHEROMONES - Min: {0} - Max: {1}", min, max));

        for (int idx = 0; idx < pheromones.Length; idx++)
        {
            if(_remainingFood[idx] == null)
                continue;
			_remainingFood[idx].GetComponent<FoodController>().Redye(GetRedyeFactor(min, pheromones[idx], max));
            
            // set nextBestFoodPosition because of maximum of pheromones
            if (Math.Abs(max - pheromones[idx]) < 1e-6 * max)
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
		//_antAlgorithm.Pheromones.SetPheromone(cityA, cityB, 0.7);
    }

    #region Helper Methods

    //returns value between 0 (for min value) and 1 (for max value)
	private float GetRedyeFactor(double min, double value, double max)
	{
		double divisor = max - min;
		if (Math.Abs(divisor) < 1e-6 * min) 
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
		Debug.Log ("CalcOverallUserDistance. number of cities: " + _userTourCities.Count);

        double distance = 0;
        
        for(int i = 0; i < _userTourCities.Count - 1; i++)
        {
            City city1 = _userTourCities[i];
            City city2 = _userTourCities[i + 1];

            Vector2 city1Pos = new Vector2(city1.getXPosition(), city1.getYPosition());
            Vector2 city2Pos = new Vector2(city2.getXPosition(), city2.getYPosition());

            distance += Vector2.Distance(city1Pos, city2Pos);
        }

        return distance;
    }

    public double CalcOverallDistance(List<City> cities)
    {
        double distance = 0;
        
		Debug.Log("CalcOverallDistance. #cities: " + _userTourCities.Count);

		for(int i = 0; i < cities.Count - 1; i++)
        {
            City city1 = cities[i];
            City city2 = cities[i + 1];

            Vector2 city1Pos = new Vector2(city1.getXPosition(), city1.getYPosition());
            Vector2 city2Pos = new Vector2(city2.getXPosition(), city2.getYPosition());

            distance += Vector2.Distance(city1Pos, city2Pos);
        }

        return distance;
    }

    #endregion
}