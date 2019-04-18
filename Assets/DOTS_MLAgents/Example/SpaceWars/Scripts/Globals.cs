using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Networking;

namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    public class Globals : MonoBehaviour
    {
        public static int NumberShips;
        
        public static float SHIP_SPEED = 2f;
        public static float SHIP_ROTATION_SPEED = 1f;
        
        public static float RELOAD_TIME = 20f;

        public const float SPAWN_DISTANCE = 20f;
        public const float PROJECTILE_SPEED = 200f;
        public const float BOUNDARIES = 1000000f;
        public static RenderMesh ProjectileRenderer;
        public static RenderMesh ExplosionRenderer;
        
        public const float PROJECTILE_SCALE = 0.15f;
        
        public const float SHIP_SCALE = 0.1f;

#pragma warning disable 0649
        [SerializeField] private RenderMesh Projectile;
        [SerializeField] private RenderMesh Explosion;
#pragma warning restore 0649

        private void Awake()
        {
            ProjectileRenderer = Projectile;
            ExplosionRenderer = Explosion;
        }
    }
}
