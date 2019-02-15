using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Networking;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class Globals : MonoBehaviour
    {
        public static float SHIP_SPEED = 2f;
        public static float SHIP_ROTATION_SPEED = 1f;
        
        public static float RELOAD_TIME = 20f;

        public const float SPAWN_DISTANCE = 20f;
        public const float PROJECTILE_SPEED = 200f;
        public const float BOUNDARIES = 1000000f;
        public static MeshInstanceRenderer ProjectileRenderer;
        public static MeshInstanceRenderer ExplosionRenderer;
        
        public const float PROJECTILE_SCALE_X = 0.15f;
        public const float PROJECTILE_SCALE_Y = 0.1f;
        public const float PROJECTILE_SCALE_Z = 1f;
        
        public const float SHIP_SCALE_X = 0.1f;//0.2f;
        public const float SHIP_SCALE_Y = 0.1f;//0.4f;
        public const float SHIP_SCALE_Z = 0.1f;//1f;

        
        [SerializeField] private MeshInstanceRenderer Projectile;
        [SerializeField] private MeshInstanceRenderer Explosion;

        private void Awake()
        {
            ProjectileRenderer = Projectile;
            ExplosionRenderer = Explosion;
        }
    }
}
