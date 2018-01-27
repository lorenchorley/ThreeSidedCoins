using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Events;
using System;

public class ExperimentManager : MonoBehaviour {

    [Header("Objects")]
    public CoinComponent CoinPrefab;
    public Transform Ground;
    public Transform CoinContainer;

    [Header("Testing grid")]
    [Tooltip("Determines the size of the experiment grid (Total number of experiments = TestGridSize ^ 2)")]
    public int TestGridSize = 10;
    [Tooltip("Determines how far apart each experiment is")]
    public float TestGridScale = 10;

    [Header("Randomised parameters")]
    public bool RandomRotation = true; // TODO 
    [Range(0, 100)] public float MaximumHorizontalVelocity;
    [Range(1, 50)] public float HeightMin;
    [Range(1, 50)] public float HeightMax;

    [Header("Experiment options")]
    [Tooltip("The number of seconds to wait before excluding any experiment still in progress")]
    public float Timeout = 10; // TODO

    [Header("Controls")]
    [Tooltip("Check to start a new experiment")]
    public bool startExperiment = false;
    [Tooltip("Automatically start the next experiment when the current one finishes")]
    public bool Automate = false;

    [Header("Events")]
    public UnityEvent OnFinishedExperiment;

    [Header("Results")]
    public int TotalCoins;
    public int CoinsShowingHeads;
    public int CoinsShowingTails;
    public int CoinsOnSide;
    public int CoinsUndetermined;
    public float ProbabilityOfHeads;
    public float ProbabilityOfTails;
    public float ProbabilityOfSide;

    private List<CoinComponent> Coins;
    private HashSet<CoinComponent> MovingCoins;
    private bool experimentStarted;
    private long timeStarted;

    void Start () {
        Assert.IsNotNull(CoinPrefab);
        Assert.IsNotNull(Ground);
        Assert.IsNotNull(CoinContainer);
        Reset();
        TotalCoins = 0;
        CoinsShowingHeads = 0;
        CoinsShowingTails = 0;
        CoinsOnSide = 0;
        CoinsUndetermined = 0;
        ProbabilityOfHeads = 0;
        ProbabilityOfTails = 0;
        ProbabilityOfSide = 0;
        experimentStarted = false;
    }

    void Update() {
        if (startExperiment) {
            startExperiment = false;

            // Check if the previous experiment has finished
            if (!IsExperimentFinished()) {
                Debug.LogWarning("Previous experiment has not yet finished");
                return;
            }

            CleanUpExperimentSpace();

            timeStarted = DateTime.Now.Ticks;
            experimentStarted = true;

            Vector3 GridCenterOffset = new Vector3(1, 0, 1) * TestGridScale * TestGridSize * 0.5f;
            for (int index = 0; index < TestGridSize * TestGridSize; index++) {

                // Create a new coin
                CoinComponent coin = GameObject.Instantiate(CoinPrefab);

                // Set the manager in the coin so it can let the manager know when the coin has settled into a final position
                coin.ExperimentManager = this;

                // Determine the coins position
                int row = index % TestGridSize; 
                int column = index / TestGridSize; 
                Vector3 HorizontalOffset = TestGridScale * new Vector3(row, 0, column);
                float VerticalOffset = Mathf.Lerp(HeightMin, HeightMax, UnityEngine.Random.value);

                // Set the coin position and rotation
                coin.transform.rotation = Quaternion.Euler(UnityEngine.Random.value * 360, UnityEngine.Random.value * 360, UnityEngine.Random.value * 360);
                coin.transform.position = Ground.transform.position // The position of the ground
                                          + Vector3.up * VerticalOffset // The random height
                                          + HorizontalOffset // The horiztal position within the grid
                                          - GridCenterOffset; // A grid offset to recenter the grid over the center of the ground

                // Make sure the coin has physics capabilities
                Rigidbody rb = coin.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = coin.gameObject.AddComponent<Rigidbody>();

                // Set random velocity
                Vector3 v = UnityEngine.Random.onUnitSphere * MaximumHorizontalVelocity * UnityEngine.Random.value;
                v.y = 0;
                rb.velocity = v;

                // Set the container for the coin, so it's nice and tiny
                coin.transform.SetParent(CoinContainer);

                // Register the new coin with the manager so it knows when the experiment is finished
                RegisterNewCoin(coin);

            }

        }
        
        // Check if timed out
        if (experimentStarted && (DateTime.Now.Ticks - timeStarted) >= Timeout * TimeSpan.TicksPerSecond) {

            // Take out coins that haven't yet settled
            foreach (CoinComponent coin in MovingCoins) {
                Coins.Remove(coin);
            }

            OnExperimentFinished();
        }

    }

    void Reset() {
        Coins = new List<CoinComponent>();
        MovingCoins = new HashSet<CoinComponent>();
    }

    bool IsExperimentFinished() {
        return !experimentStarted || MovingCoins.Count == 0; // The experiment is finished if there are no coins left moving
    }

    void RegisterNewCoin(CoinComponent coin) {
        Assert.IsFalse(Coins.Contains(coin));
        Coins.Add(coin);
        MovingCoins.Add(coin);
    }

    public void RegisterSelfAsStopped(CoinComponent coin) {
        if (!experimentStarted)
            return;

        Assert.IsTrue(MovingCoins.Contains(coin));
        MovingCoins.Remove(coin);

        // If no move coins are registered as moving, finish up this round
        if (MovingCoins.Count == 0)
            OnExperimentFinished();

    }

    void CleanUpExperimentSpace() {

        // Clear out the coin objects
        for (int i = 0; i < Coins.Count; i++) {
            Destroy(Coins[i].gameObject);
        }

        // Reset the data
        Reset();

    }

    public void StartExperiment() {
        startExperiment = true;
    }

    void OnExperimentFinished() {
        Debug.Log("Experiment finished");
        experimentStarted = false;

        // Collect statistics
        CountCoins();
        UpdateProbabilities();

        // Trigger event
        OnFinishedExperiment.Invoke();

        // Restart the experiment if the option is checked
        if (Automate)
            StartExperiment();

    }

    void CountCoins() {
        TotalCoins += Coins.Count;
        for (int i = 0; i < Coins.Count; i++) {
            switch (Coins[i].DetermineOrientation()) {
            case CoinOrientation.Heads:
                CoinsShowingHeads++;
                break;
            case CoinOrientation.Tails:
                CoinsShowingTails++;
                break;
            case CoinOrientation.Side:
                CoinsOnSide++;
                break;
            case CoinOrientation.Undetermined:
                CoinsUndetermined++;
                break;
            }
        }
    }

    void UpdateProbabilities() {
        ProbabilityOfHeads = ((float) CoinsShowingHeads) / ((float) TotalCoins);
        ProbabilityOfTails = ((float) CoinsShowingTails) / ((float) TotalCoins);
        ProbabilityOfSide = ((float) CoinsOnSide) / ((float) TotalCoins);
    }

}
