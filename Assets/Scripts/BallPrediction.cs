using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class BallPrediction : NetworkBehaviour
{
    #region Types.

    public struct MoveData : IReplicateData
    {
        public bool Kick;
        public Vector3 Direction;
        
        public MoveData(bool kick, Vector3 direction)
        {
            Kick = kick;
            Direction = direction;
            _tick = 0;
        }

        private uint _tick;

        public void Dispose()
        {
        }

        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            _tick = 0;
        }

        private uint _tick;

        public void Dispose()
        {
        }

        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #endregion

    [SerializeField] private float force;
    [SerializeField] private float jump;
    [SerializeField] private Vector3 _dir;
    private bool _kick;
    private Rigidbody _rb;

    private void Awake()
    {
        TryGetComponent(out _rb);
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null)
        {
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    public override void OnStartClient()
    {
        base.PredictionManager.OnPreReplicateReplay += PredictionManager_OnPreReplicateReplay;
    }

    public override void OnStopClient()
    {
        base.PredictionManager.OnPreReplicateReplay -= PredictionManager_OnPreReplicateReplay;
    }

    /// <summary>
    /// Called every time any predicted object is replaying. Replays only occur for owner.
    /// Currently owners may only predict one object at a time.
    /// </summary>
    private void PredictionManager_OnPreReplicateReplay(uint arg1, PhysicsScene arg2, PhysicsScene2D arg3)
    {
        /* Server does not replay so it does
         * not need to add gravity. */
        if (!base.IsServer)
            AddGravity();
    }


    private void TimeManager_OnTick()
    {
        if (base.IsOwner)
        {
            Reconciliation(default, false);
            BuildMoveData(out MoveData md);
            Move(md, false);
        }

        if (base.IsServer)
        {
            Move(default, true);
        }

        /* Server and all clients must add the additional gravity.
         * Adding gravity is not necessarily required in general but
         * to make jumps more snappy extra gravity is added per tick.
         * All clients and server need to simulate the gravity to keep
         * prediction equal across the network. */
        AddGravity();
    }

    private void TimeManager_OnPostTick()
    {
        /* Reconcile is sent during PostTick because we
         * want to send the rb data AFTER the simulation. */
        if (base.IsServer)
        {
            ReconcileData rd =
                new ReconcileData(transform.position, transform.rotation, _rb.velocity, _rb.angularVelocity);
            Reconciliation(rd, true);
        }
    }


    /// <summary>
    /// Builds a MoveData to use within replicate.
    /// </summary>
    /// <param name="md"></param>
    private void BuildMoveData(out MoveData md)
    {
        md = default;

        md = new MoveData(_kick, _dir);
    }

    /// <summary>
    /// Adds gravity to the rigidbody.
    /// </summary>
    private void AddGravity()
    {
        _rb.AddForce(Physics.gravity * 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") &&
            collision.gameObject.TryGetComponent(out Rigidbody player))
        {
            _dir = player.velocity * force + new Vector3(0, jump, 0);
            _kick = true;
        }
    }


    [Replicate]
    private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
    {
        if(md.Kick)
            _rb.AddForce(md.Direction);
        _kick = false;
        //_rb.velocity = md.Direction;
    }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
    {
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _rb.velocity = rd.Velocity;
        _rb.angularVelocity = rd.AngularVelocity;
    }

}
