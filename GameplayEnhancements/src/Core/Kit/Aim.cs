// What the player's crosshair points at.
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Aim
    {
        // the crosshair point (camera ray, 60 m, solid objects and terrain), else 8 m ahead of the player
        public static Vector3 Point(Player p, out bool aimed)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 60f, Masks.SolidAndTerrain))
                {
                    aimed = true;
                    return hit.point;
                }
            }
            aimed = false;
            return p.transform.position + p.transform.forward * 8f;
        }

        // the solid object under the crosshair (no terrain, triggers ignored) within 'range' m, or false
        public static bool SolidHit(float range, out RaycastHit hit)
        {
            hit = new RaycastHit();
            Camera cam = Camera.main;
            return cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, range, Masks.Solid, QueryTriggerInteraction.Ignore);
        }
    }
}
