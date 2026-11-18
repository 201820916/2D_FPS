using UnityEngine;

namespace GungeonFPS.Core
{
    public class VfxSpawner : MonoBehaviour
    {
        public static VfxSpawner Instance { get; private set; }

        private Material _particleMat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _particleMat = new Material(shader);
        }

        public void SpawnBurst(Vector3 position, Color color, int count = 8, float speed = 6f, float size = 0.14f)
        {
            var go = new GameObject("PixelBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _particleMat;
            renderer.material.color = color;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }
    }
}
