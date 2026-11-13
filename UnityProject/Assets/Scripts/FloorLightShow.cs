using UnityEngine;

namespace TikTokLiveGame
{
    /// <summary>
    /// Den san nhay: cac vung sang di chuyen theo nhac, vien LED chay va quang sang trung tam.
    /// Dung shader Custom/SimpleParticle (vong tron mem) nen khong can them asset moi.
    /// </summary>
    public sealed class FloorLightShow
    {
        private const float FloorY = 0.02f;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Color[] Palette =
        {
            new(0.26f, 0.91f, 0.88f),
            new(0.44f, 0.28f, 1f),
            new(1f, 0.31f, 0.64f),
            new(0.24f, 0.65f, 1f),
            new(1f, 0.77f, 0.42f)
        };

        private struct LightPool
        {
            public Transform Transform;
            public Renderer Renderer;
            public float Phase;
            public float Speed;
            public Vector2 Amplitude;
            public int ColorIndex;
        }

        private LightPool[] pools;
        private Renderer[] leds;
        private Renderer centerGlow;
        private MaterialPropertyBlock block;

        private readonly Transform transform;
        public static FloorLightShow Instance { get; private set; }

        public static FloorLightShow Create()
        {
            Instance = new FloorLightShow();
            return Instance;
        }

        private FloorLightShow()
        {
            transform = new GameObject("Floor Light Show").transform;
            Shader shader = Resources.Load<Shader>("Shaders/SimpleParticle");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new(shader) { renderQueue = 3050 };
            Mesh mesh = BuildQuadMesh();
            block = new MaterialPropertyBlock();

            centerGlow = CreateGlow("Floor Center Glow", mesh, material, new Vector3(0f, FloorY - 0.005f, 0.2f), 11f);

            pools = new LightPool[7];
            for (int index = 0; index < pools.Length; index++)
            {
                float radius = index % 2 == 0 ? 3.4f : 2.6f;
                Renderer renderer = CreateGlow($"Floor Light Pool {index}", mesh, material, new Vector3(0f, FloorY, 0f), radius);
                pools[index] = new LightPool
                {
                    Transform = renderer.transform,
                    Renderer = renderer,
                    Phase = index * 1.37f,
                    Speed = 0.32f + index * 0.045f,
                    Amplitude = new Vector2(4.6f - (index % 3) * 0.6f, 3.4f),
                    ColorIndex = index % Palette.Length
                };
            }

            // Vien LED quanh san: hang truoc + hai ben.
            const int frontCount = 22;
            const int sideCount = 12;
            leds = new Renderer[frontCount + sideCount * 2];
            int led = 0;
            for (int index = 0; index < frontCount; index++)
            {
                float x = Mathf.Lerp(-6.2f, 6.2f, index / (float)(frontCount - 1));
                leds[led++] = CreateGlow($"Floor LED Front {index}", mesh, material, new Vector3(x, FloorY + 0.01f, 4.9f), 0.55f);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int index = 0; index < sideCount; index++)
            {
                float z = Mathf.Lerp(4.4f, -4.6f, index / (float)(sideCount - 1));
                leds[led++] = CreateGlow($"Floor LED Side {side}-{index}", mesh, material, new Vector3(side * 6.4f, FloorY + 0.01f, z), 0.5f);
            }
        }

        private Renderer CreateGlow(string name, Mesh mesh, Material material, Vector3 position, float size)
        {
            GameObject glow = new(name);
            glow.transform.SetParent(transform, false);
            glow.transform.position = position;
            glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            glow.transform.localScale = new Vector3(size, size, 1f);
            glow.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = glow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public void Tick()
        {
            float beat = ClubBeatClock.Beat;
            float beatFraction = beat - Mathf.Floor(beat);
            float pulse = 0.55f + 0.45f * (1f - beatFraction) * (1f - beatFraction);
            int colorShift = Mathf.FloorToInt(beat / 4f);
            float time = Time.time;

            SetColor(centerGlow, new Color(0.12f, 0.45f, 0.55f, 0.12f + 0.1f * pulse));

            for (int index = 0; index < pools.Length; index++)
            {
                LightPool pool = pools[index];
                float t = time * pool.Speed + pool.Phase;
                float x = Mathf.Sin(t) * pool.Amplitude.x;
                float z = 0.2f + Mathf.Sin(t * 0.73f + pool.Phase * 1.7f) * pool.Amplitude.y;
                pool.Transform.position = new Vector3(x, FloorY + index * 0.001f, z);
                Color color = Palette[(pool.ColorIndex + colorShift) % Palette.Length];
                color.a = Mathf.Min(0.95f, 0.5f + 0.45f * pulse);
                SetColor(pool.Renderer, color);
            }

            int chase = Mathf.FloorToInt(beat * 2f);
            for (int index = 0; index < leds.Length; index++)
            {
                bool lit = (index + chase) % 4 == 0;
                Color color = Palette[((index / 4) + colorShift) % Palette.Length];
                color.a = lit ? 0.95f : 0.28f + 0.12f * pulse;
                SetColor(leds[index], color);
            }
        }

        private void SetColor(Renderer target, Color color)
        {
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }

        private static Mesh BuildQuadMesh()
        {
            Mesh mesh = new() { name = "Floor Glow Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
