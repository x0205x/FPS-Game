using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Elite Dangerous-inspired thruster plumes: hot main engines and maneuver puffs.
    /// </summary>
    public class AircraftThrusterVfx : MonoBehaviour
    {
        [SerializeField] private AircraftFlightController flight;
        [SerializeField] private AircraftThrusterPoint[] thrusters;
        [SerializeField] private Color mainEngineColor = new(0.35f, 0.72f, 1f, 1f);
        [SerializeField] private Color boostEngineColor = new(1f, 0.55f, 0.12f, 1f);
        [SerializeField] private Color maneuverColor = new(0.75f, 0.9f, 1f, 1f);

        private ThrusterRuntime[] _runtime;

        private struct ThrusterRuntime
        {
            public AircraftThrusterPoint Point;
            public ParticleSystem Particles;
            public ParticleSystem.EmissionModule Emission;
            public ParticleSystem.MainModule Main;
            public Light Glow;
            public float BaseRate;
        }

        private void Awake()
        {
            if (flight == null) flight = GetComponent<AircraftFlightController>();
            if (thrusters == null || thrusters.Length == 0)
                thrusters = GetComponentsInChildren<AircraftThrusterPoint>(true);

            BuildThrusterSystems();
        }

        private void Update()
        {
            if (flight == null || _runtime == null) return;

            Vector3 linear = flight.LocalLinearThrust;
            Vector3 angular = flight.LocalAngularThrust;
            bool boost = flight.BoostActive;

            for (int i = 0; i < _runtime.Length; i++)
            {
                ref ThrusterRuntime rt = ref _runtime[i];
                if (rt.Point == null) continue;

                float intensity = ComputeIntensity(rt.Point, linear, angular, boost);
                rt.Emission.rateOverTime = rt.BaseRate * intensity;

                Color color = rt.Point.Kind == AircraftThrusterPoint.ThrusterKind.Main
                    ? (boost ? boostEngineColor : mainEngineColor)
                    : maneuverColor;
                rt.Main.startColor = color;

                if (rt.Glow != null)
                {
                    rt.Glow.enabled = intensity > 0.05f;
                    rt.Glow.intensity = Mathf.Lerp(0.4f, boost ? 6f : 3.5f, intensity);
                    rt.Glow.color = color;
                }
            }
        }

        private float ComputeIntensity(
            AircraftThrusterPoint point, Vector3 linear, Vector3 angular, bool boost)
        {
            Vector3 dir = point.LocalThrustDirection;
            float linearDemand = Mathf.Max(0f, Vector3.Dot(linear, dir));
            float angularDemand = 0f;

            if (point.Kind == AircraftThrusterPoint.ThrusterKind.Maneuver)
            {
                angularDemand = Mathf.Abs(Vector3.Dot(angular, dir));
                if (linearDemand < 0.05f && angularDemand < 0.05f)
                    return 0f;
                return Mathf.Clamp01(Mathf.Max(linearDemand, angularDemand * 0.85f));
            }

            if (linearDemand < 0.02f) return 0.05f;
            return Mathf.Clamp01(linearDemand * (boost ? 1.35f : 1f));
        }

        private void BuildThrusterSystems()
        {
            _runtime = new ThrusterRuntime[thrusters.Length];
            for (int i = 0; i < thrusters.Length; i++)
            {
                AircraftThrusterPoint point = thrusters[i];
                if (point == null) continue;

                bool main = point.Kind == AircraftThrusterPoint.ThrusterKind.Main;
                GameObject fxGo = new GameObject(point.name + "_VFX");
                fxGo.transform.SetParent(point.transform, worldPositionStays: false);
                fxGo.transform.localPosition = Vector3.zero;
                fxGo.transform.localRotation = Quaternion.LookRotation(point.LocalThrustDirection);

                ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
                ConfigureParticles(ps, main);

                Light glow = fxGo.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.range = main ? 14f : 4f;
                glow.shadows = LightShadows.None;
                glow.enabled = false;

                var emission = ps.emission;
                float baseRate = main ? 85f : 24f;
                emission.rateOverTime = 0f;

                _runtime[i] = new ThrusterRuntime
                {
                    Point = point,
                    Particles = ps,
                    Emission = emission,
                    Main = ps.main,
                    Glow = glow,
                    BaseRate = baseRate
                };
            }
        }

        private static void ConfigureParticles(ParticleSystem ps, bool mainEngine)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.startLifetime = mainEngine ? new ParticleSystem.MinMaxCurve(0.35f, 0.7f) : new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed = mainEngine ? new ParticleSystem.MinMaxCurve(18f, 32f) : new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSize = mainEngine ? new ParticleSystem.MinMaxCurve(0.35f, 0.9f) : new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.maxParticles = mainEngine ? 220 : 64;

            var emission = ps.emission;
            emission.enabled = true;

            var shape = ps.shape;
            shape.shapeType = mainEngine ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Hemisphere;
            shape.angle = mainEngine ? 8f : 25f;
            shape.radius = mainEngine ? 0.15f : 0.05f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = mainEngine;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.z = new ParticleSystem.MinMaxCurve(8f, 18f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.4f, 0.7f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateThrusterMaterial();
        }

        private static Material CreateThrusterMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            return mat;
        }
    }
}
