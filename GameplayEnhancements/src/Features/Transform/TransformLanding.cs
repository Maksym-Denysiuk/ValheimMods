using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    // after a flight ends in mid-air: keeps the fall-damage reference at the current height until the player lands or swims
    [DefaultExecutionOrder(1000)]
    public sealed class TransformLanding : MonoBehaviour
    {
        static readonly FieldInfo fMaxAir = typeof(Character).GetField("m_maxAirAltitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Player player;
        float start;

        void Start() { player = GetComponent<Player>(); start = Time.time; }

        void FixedUpdate()
        {
            if (player == null || player.IsOnGround() || player.InWater() || Time.time - start > 60f) { Destroy(this); return; }
            if (fMaxAir != null) fMaxAir.SetValue(player, player.transform.position.y);
        }
    }
}
