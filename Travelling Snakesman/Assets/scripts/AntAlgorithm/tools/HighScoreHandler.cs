﻿using UnityEngine;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using util;

namespace AntAlgorithm.tools
{
    [Serializable]
    public class HighScoreEntry
    {
        public string ID;
        public string Name;
        public double UserScore;
        public double AlgoScore;

        private HighScoreEntry()
        {
            ID = null;
            Name = null;
            UserScore = -1;
            AlgoScore = -1;
        }

        public static HighScoreEntry Create(string line)
        {
            HighScoreEntry entry = JsonUtility.FromJson<HighScoreEntry>(line);
            return entry.IsInitialized() ? entry : null;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Name, UserScore);
        }

        public bool IsInitialized()
        {
            return ID != null
                   && Name != null
                   && UserScore != -1;
        }
    }


    public class HighScoreHandler : MonoBehaviour
    {
        public static int ORDER_TYPE_ASC = 1;
        private const string SecretKey = "rLdZyTAJeynUh6JDR8Sut8Yj1sLXIPWO";
        private const string AddScoreURL = "http://iml.hci-kdd.org/serverscripts/addscore.php?";
        private const string HighscoreURL = "http://iml.hci-kdd.org/serverscripts/getscores.php?";
        public bool ReadHighScoresFinished = false;
        public List<HighScoreEntry> Result;

        public void PostScores(string userName,
            string tspName,
            double algoScore,
            int algoBestIteration,
            string algoTour,
            double userScore,
            int userBestIteration,
            string userTour)
        {
            string url = AddScoreURL.TrimEnd('?');
            var postValues = new Dictionary<string, string>();
            postValues["name"] = userName;
            postValues["tspname"] = tspName;

            postValues["algoscore"] = algoScore.ToString();
            postValues["algobestiteration"] = algoBestIteration.ToString();
            postValues["algotour"] = algoTour;

            postValues["userscore"] = userScore.ToString();
            postValues["userbestiteration"] = userBestIteration.ToString();
            postValues["usertour"] = userTour;

            postValues["hash"] = Hash(SecretKey);

#if UNITY_STANDALONE_WIN
            var hsPost = WebFunctions.Post(url, postValues);

            print("HSPOST " + hsPost.url);

            if (!string.IsNullOrEmpty(hsPost.error))
            {
                print("There was an error posting the high score: " + hsPost.error);
            }
#endif

#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
            StartCoroutine(PostScoresAsync(userName, tspName, algoScore, algoBestIteration, algoTour, userScore, userBestIteration, userTour));
#endif
        }

        public static IEnumerator PostScoresAsync(string userName,
                                                  string tspName,
                                                  double algoScore,
                                                  int algoBestIteration,
                                                  string algoBestTour,
                                                  double userScore,
                                                  int userBestIteration,
                                                  string userBestTour
                                                  )
        {
            string url = AddScoreURL
                             + "name=" + WWW.EscapeURL(userName)
                             + "&userscore=" + userScore
                             + "&tspname=" + WWW.EscapeURL(tspName)
                             + "&hash=" + WWW.EscapeURL(Hash(SecretKey))
                             + "&userbestiteration=" + userBestIteration
                             + "&usertour=" + userBestTour
                             + "&algoscore=" + algoScore
                             + "&algobestiteration=" + algoBestIteration
                             + "&algotour=" + algoBestTour;
                          
            print(url);
            WWW hsPost = new WWW(url);
            yield return hsPost;

            if (!string.IsNullOrEmpty(hsPost.error))
            {
                print("There was an error posting the high score: " + hsPost.error);
            }
        }

        public IEnumerator ScoresWebGL()
        {
            Result = new List<HighScoreEntry>();
            ReadHighScoresFinished = false;
            string tspName = PlayerPrefs.GetString("TspName");
            int orderType = ORDER_TYPE_ASC;

            int numberOfEntries = AntAlgorithmManager.NumHighScoreEntries;

            var url = HighscoreURL
                  + "tsp=" + WWW.EscapeURL(tspName)
                  + "&num=" + numberOfEntries
                  + "&order=" + orderType;
            WWW www = new WWW(url);

            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                print("There was an error getting the high score: " + www.error);
            }
            else
            {
                foreach (var line in www.text.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var entry = HighScoreEntry.Create(line);

                    if (entry != null)
                    {
                        Result.Add(entry);
                    }
                }
            }
            ReadHighScoresFinished = true;

        }

        public List<HighScoreEntry> GetScores(string tspName, int orderType,
            string gameName = AntAlgorithmManager.GameName,
            int numberOfEntries = AntAlgorithmManager.NumHighScoreEntries)
        {
            Debug.Log("Retrieving High Scores...");
            ReadHighScoresFinished = false;
            Result = new List<HighScoreEntry>();
            var url = HighscoreURL
                      + "tsp=" + WWW.EscapeURL(tspName)
                      + "&num=" + numberOfEntries
                      + "&order=" + orderType;
            print(url);


            WWW hsGet = WebFunctions.Get(url);

            if (!string.IsNullOrEmpty(hsGet.error))
            {
                print("There was an error getting the high score: " + hsGet.error);
            }
            else
            {
                foreach (var line in hsGet.text.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var entry = HighScoreEntry.Create(line);
                    if (entry != null)
                    {
                        Result.Add(entry);
                    }
                }
            }
            ReadHighScoresFinished = true;
            return Result;
        }


        private static string Hash(string password)
        {
            return BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(password)))
                .Replace("-", string.Empty).ToUpper();
        }
    }
}
