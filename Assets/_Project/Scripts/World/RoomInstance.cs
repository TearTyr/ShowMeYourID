using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.World
{
    // One built room: root, light, interactables, spawn points.
    public class RoomInstance
    {
        // Shared floor line for every room (backgrounds, player, anomaly bands all use it).
        public const float FloorY = -2.4f;

        public RoomId Id;
        public GameObject Root;
        public float HalfWidth;
        public LightController Light;
        public SpriteRenderer Darkness;
        public List<Interactable> Interactables = new List<Interactable>();
        public List<Vector3> AnomalySpawnPoints = new List<Vector3>();
        public Vector3 PlayerSpawn;
        public string DisplayName;
    }
}
