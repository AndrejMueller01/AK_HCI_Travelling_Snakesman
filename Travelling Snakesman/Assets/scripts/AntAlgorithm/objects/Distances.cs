﻿/****************************************************
 * IML ACO implementation for TSP 
 * More information: http://hci-kdd.org/project/iml/
 * Author: Andrej Mueller
 * Year: 2017
 *****************************************************/

/* Distances represents the distances between cities */

using System;
using System.Collections.Generic;

public class Distances
{
    // Matrix of distances between city x and city y
    private double[][] distances;
    // nearest neighbours of a cities
    private int[][] nearestNeighbours;

    private List<City> cities;

    public Distances(List<City> cities)
    {
        this.cities = cities;
        //build the distance matix
        CalculateDistances();
        //build the nearestNeighbour matix
        CalculateNearestNeighbours();
    }

    // calculates the nearest neighbour heuristic of the tsp graph
    public double CalculateNNHeuristic()
    {
        bool[] visited = new bool[cities.Count];
        double nnHeuristic = 0;
        int cityIndexTemp = 0;
        int startCityIndex = 0;

        visited[0] = true;
        for (int i = 0; i < cities.Count - 1; i++)
        {
            double distance = Double.MaxValue;
            for (int j = 0; j < cities.Count; j++)
            {
                if (!visited[j] && distances[startCityIndex][j] < distance)
                {
                    distance = distances[startCityIndex][j];
                    cityIndexTemp = j;
                }
            }
            visited[cityIndexTemp] = true;
            startCityIndex = cityIndexTemp;

            nnHeuristic += distance;
        }
        nnHeuristic += distances[startCityIndex][0];
        return nnHeuristic;
    }

    // Calculate the initial distances between cities in array order 
    private void CalculateDistances()
    {
        distances = new double[cities.Count][];
        String str = "";

        for (int i = 0; i < cities.Count; i++)
        {
            distances[i] = new double[cities.Count];
        }

        for (int i = 0; i < cities.Count; i++)
        {
            for (int j = 0; j < cities.Count; j++)
            {
                // distance matrix from cityA to cityB
                double distance = CalculateCityDistance(cities[i].Id, cities[j].Id);

                distances[i][j] = distance;
                distances[j][i] = distance;
                str += distances[i][j] + " ";

            }
            str += "\n";
        }
        //Debug.Log("Distance matrix: " + str);
    }

    /* Calculate all nearest neigbours of all cities in array order 
     * (for performance increasing pruposes)
    * example: cityB is the nearest neighbour of cityA and cityC the 2nd nearest neighbour of cityA
    *          nn[cityAid][0] = cityBid
    *          nn[cityAid][1] = cityCid
    */
    private void CalculateNearestNeighbours()
    {
        nearestNeighbours = new int[cities.Count][];
        double lowestValue = double.MaxValue;
        int lowestValueIndex = -1;
        String str = "";

        double[][] distancesHelper = new double[cities.Count][];
        for (int i = 0; i < cities.Count; i++)
        {
            distancesHelper[i] = new double[cities.Count];
        }

        for (int i = 0; i < cities.Count; i++)
            nearestNeighbours[i] = new int[cities.Count - 1];


        for (int i = 0; i < cities.Count; i++)
        {
            for (int j = 0; j < cities.Count; j++)
            {
                distancesHelper[i][j] = distances[i][j];
            }
        }

        for (int i = 0; i < cities.Count; i++)
        {
            for (int j = 0; j < cities.Count - 1; j++)
            {
                for (int k = 0; k < cities.Count; k++)
                {
                    if (distancesHelper[i][k] < lowestValue && distancesHelper[i][k] != 0)
                    {
                        lowestValue = distancesHelper[i][k];
                        lowestValueIndex = k;
                    }
                }
                distancesHelper[i][lowestValueIndex] = double.MaxValue;
                nearestNeighbours[i][j] = lowestValueIndex;
                str += nearestNeighbours[i][j] + " ";
                lowestValue = double.MaxValue;
            }
            str += "\n";
        }
        //Debug.Log("NearestNeighbours matrix: " + str);

    }

    // Calculate the the vpoint distance between 2 cities with 2D coordinates
    private double CalculateCityDistance(int cityAID, int cityBID)
    {
        return Math.Sqrt(Math.Pow(cities[cityAID].XPosition - cities[cityBID].XPosition, 2) + Math.Pow(cities[cityAID].YPosition - cities[cityBID].YPosition, 2));
    }

    // returns the distance between two cities
    public double GetDistance(int cityAId, int cityBId)
    {
        return distances[cityAId][cityBId];
    }
}
