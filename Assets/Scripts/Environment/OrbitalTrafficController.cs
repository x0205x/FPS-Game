using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Spawns procedural spacecraft drifting across the sky when the player looks upward.
    /// </summary>
    public class OrbitalTrafficController : MonoBehaviour
    {
        [SerializeField] private float skyRadius = 1400f;
        [SerializeField] private float minLookUpAngle = 18f;
        [SerializeField] private int maxActiveShips = 7;
        [SerializeField] private float spawnInterval = 2.2f;
        [SerializeField] private float shipSpeed = 28f;
        [SerializeField] private Material shipHullMaterial;
        [SerializeField] private Material shipAccentMaterial;
        [SerializeField] private GameObject[] shipPrefabs;

        private Transform[] _activeShips;
        private Vector3[] _velocities;
        private int _activeCount;
        private float _spawnTimer;
        private int _shipSeed = 90210;

        private void Awake()
        {
            _activeShips = new Transform[maxActiveShips];
            _velocities = new Vector3[maxActiveShips];
        }

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            bool lookingUp = IsLookingUp(cam.transform);
            if (lookingUp)
            {
                _spawnTimer += Time.deltaTime;
                if (_activeCount < maxActiveShips && _spawnTimer >= spawnInterval)
                {
                    _spawnTimer = 0f;
                    SpawnShip(cam.transform);
                }
            }
            else
            {
                _spawnTimer = 0f;
            }

            MoveShips(cam.transform);
        }

        private bool IsLookingUp(Transform camTransform)
        {
            float upDot = Vector3.Dot(camTransform.forward, Vector3.up);
            float angle = Mathf.Acos(Mathf.Clamp(upDot, -1f, 1f)) * Mathf.Rad2Deg;
            return angle < (90f - minLookUpAngle);
        }

        private void SpawnShip(Transform camTransform)
        {
            int slot = FindFreeSlot();
            if (slot < 0) return;

            Vector3 origin = camTransform.position;
            Vector3 randomDir = Random.onUnitSphere;
            if (randomDir.y < 0.15f)
                randomDir.y = Mathf.Abs(randomDir.y) + 0.15f;
            randomDir.Normalize();

            Vector3 position = origin + randomDir * skyRadius;
            Vector3 tangent = Vector3.Cross(randomDir, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(randomDir, Vector3.right);
            tangent.Normalize();

            float lateral = Random.Range(-1f, 1f);
            Vector3 velocity = (tangent + randomDir * lateral * 0.15f).normalized * shipSpeed;

            Transform ship;
            if (shipPrefabs != null && shipPrefabs.Length > 0)
            {
                GameObject prefab = shipPrefabs[Random.Range(0, shipPrefabs.Length)];
                GameObject instance = Instantiate(prefab);
                instance.name = $"OrbitalShip_{slot}";
                ship = instance.transform;
            }
            else
            {
                ship = CreateShipVisual($"OrbitalShip_{slot}");
            }

            ship.SetParent(transform, worldPositionStays: true);
            ship.position = position;
            ship.rotation = Quaternion.LookRotation(velocity, randomDir);
            ship.localScale = Vector3.one * Random.Range(2.4f, 4.8f);

            _activeShips[slot] = ship;
            _velocities[slot] = velocity;
            _activeCount++;
        }

        private void MoveShips(Transform camTransform)
        {
            Vector3 origin = camTransform.position;
            for (int i = 0; i < _activeShips.Length; i++)
            {
                Transform ship = _activeShips[i];
                if (ship == null) continue;

                ship.position += _velocities[i] * Time.deltaTime;

                Vector3 dir = ship.position - origin;
                if (dir.sqrMagnitude < 0.001f) continue;
                dir.Normalize();
                ship.position = origin + dir * skyRadius;
                ship.rotation = Quaternion.Slerp(
                    ship.rotation,
                    Quaternion.LookRotation(_velocities[i], dir),
                    Time.deltaTime * 2f);

                if ((ship.position - origin).sqrMagnitude > skyRadius * skyRadius * 1.35f)
                    DespawnShip(i);
            }
        }

        private void DespawnShip(int index)
        {
            if (_activeShips[index] != null)
                Destroy(_activeShips[index].gameObject);
            _activeShips[index] = null;
            _velocities[index] = Vector3.zero;
            _activeCount = Mathf.Max(0, _activeCount - 1);
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _activeShips.Length; i++)
            {
                if (_activeShips[i] == null)
                    return i;
            }

            return -1;
        }

        private Transform CreateShipVisual(string name)
        {
            _shipSeed++;
            var rng = new System.Random(_shipSeed);
            var root = new GameObject(name);

            Material hull = shipHullMaterial;
            Material accent = shipAccentMaterial;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Hull";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(2.2f, 0.55f, 0.9f);
            ApplyMaterial(body, hull);
            Object.Destroy(body.GetComponent<Collider>());

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(root.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0f, 1.05f);
            nose.transform.localScale = new Vector3(0.7f, 0.35f, 0.55f);
            ApplyMaterial(nose, accent);
            Object.Destroy(nose.GetComponent<Collider>());

            bool deltaWing = rng.NextDouble() > 0.45;
            CreateWing(root.transform, hull, deltaWing, left: true);
            CreateWing(root.transform, hull, deltaWing, left: false);

            if (rng.NextDouble() > 0.35)
            {
                var engine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                engine.name = "Engine";
                engine.transform.SetParent(root.transform, false);
                engine.transform.localPosition = new Vector3(0f, 0f, -1.05f);
                engine.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                engine.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
                ApplyMaterial(engine, accent);
                Object.Destroy(engine.GetComponent<Collider>());
            }

            return root.transform;
        }

        private static void CreateWing(Transform parent, Material material, bool delta, bool left)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wing.name = left ? "Wing_L" : "Wing_R";
            wing.transform.SetParent(parent, false);
            float sign = left ? -1f : 1f;
            wing.transform.localPosition = new Vector3(sign * (delta ? 1.35f : 1.05f), 0f, delta ? -0.15f : 0.05f);
            wing.transform.localRotation = Quaternion.Euler(0f, sign * (delta ? 38f : 90f), 0f);
            wing.transform.localScale = new Vector3(delta ? 1.8f : 1.2f, 1f, delta ? 1.4f : 0.35f);
            ApplyMaterial(wing, material);
            Object.Destroy(wing.GetComponent<Collider>());
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null) return;
            if (go.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial = material;
        }
    }
}
