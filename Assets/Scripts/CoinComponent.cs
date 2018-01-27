using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CoinComponent : MonoBehaviour {

    private const float MinAngle = 10;
    private const float UnityCylinderVerticalMultiplier = 0.25f;

    [Tooltip("In Unity distance units")]
    public float Radius = 1;

    //[Tooltip("In multiples of the radius")]
    public float Thickness = 1;

    [Range(0, 1)]
    public float Range = 0;

    public float Min, Max;

    public Rigidbody Rigidbody;
    public ExperimentManager ExperimentManager;

    void Start() { // TODO This for the editor as well
        Rigidbody = GetComponent<Rigidbody>();
        Min = Mathf.Sqrt(3);
        Max = 2 * Mathf.Sqrt(2);
        UpdateColliderAndMesh();
    }

    void Update() {
        UpdateColliderAndMesh();
         
        if (Rigidbody.IsSleeping()) {
            enabled = false;
            ExperimentManager.RegisterSelfAsStopped(this);
        }
    }

    void UpdateColliderAndMesh() {
        Range = Mathf.Clamp01(Range);
        Thickness = (1 / Mathf.Lerp(Min, Max, Range)) * UnityCylinderVerticalMultiplier;

        transform.localScale = new Vector3(Radius, Thickness, Radius);
    }

    public CoinOrientation DetermineOrientation() {
        float VerticalAngle = Vector3.Angle(Vector3.up, transform.up);
        if (Mathf.Abs(VerticalAngle) < MinAngle) {
            return CoinOrientation.Heads;
        } else if (Mathf.Abs(VerticalAngle - 180) < MinAngle) {
            return CoinOrientation.Tails;
        } else if (Mathf.Abs(VerticalAngle - 90) < MinAngle) {
            return CoinOrientation.Side;
        } else {
            return CoinOrientation.Undetermined;
        }
    }

}
