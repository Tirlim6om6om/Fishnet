using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using FishNet;
using FishNet.Component.Prediction;
using FishNet.Example.Scened;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private float jump;
    private Rigidbody _rb;
    private PredictedObject _predictedObject;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _predictedObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") &&
            other.gameObject.TryGetComponent(out CharacterController player))
        {
            Vector3 dir = player.velocity * force + new Vector3(0, jump, 0);
            _rb.AddForce(dir);
        }
    }
}
