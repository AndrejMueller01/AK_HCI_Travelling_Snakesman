﻿using UnityEngine;
using UnityEngine.UI;

namespace gui
{
    public class RangeDisplayController : MonoBehaviour
    {
        [SerializeField]
        private Text rangeLabel;
        private bool IsInitialized { get; set; }
        private Vector3 lastPosition;

        // Use this for initialization
        void Start()
        {
            rangeLabel.text = "0.00";
        }

        // Update is called once per frame
        void Update()
        {
            if (!AntAlgorithmManager.Instance.BestTourLength.Equals(null))
                if (AntAlgorithmManager.Instance.BestTourLength <= (AntAlgorithmManager.Instance.BestAlgorithmLength))
                {
                    rangeLabel.color = Color.green;
                }
                else if (AntAlgorithmManager.Instance.BestTourLength > AntAlgorithmManager.Instance.BestAlgorithmLength)
                {
                    rangeLabel.color = Color.red;
                }

            rangeLabel.text = "Score: " + AntAlgorithmManager.Instance.BestTourLength;
        }
    }
}
