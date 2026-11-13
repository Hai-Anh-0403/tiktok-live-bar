using System;
using System.IO;
using UnityEngine;

namespace TikTokLiveGame
{
    /// <summary>
    /// Nhan vat chinh (nhanvatchinh.gif da tach thanh PNG trong LiveAssets/MAIN_CHARACTER).
    /// Dat o trung tam san khau voi vong hao quang buc san khau va sortingOrder cao
    /// de khong bao gio bi dam dong che khuat.
    /// </summary>
    public sealed class MainCharacterGif
    {
        private const float FramesPerSecond = 25f;
        private const int FramesLoadedPerTick = 10;
        private const float CharacterScale = 1.22f;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static MainCharacterGif Instance { get; private set; }

        private readonly Transform actorTransform;
        private readonly SpriteRenderer spriteRenderer;
        private readonly Renderer podiumOuter;
        private readonly Renderer podiumInner;
        private readonly Renderer backAura;
        private readonly MaterialPropertyBlock glowBlock = new();
        private readonly string[] files;
        private readonly Sprite[] sprites;
        private readonly Vector3 basePosition;
        private int loadedCount;
        private float clock;
        private int frameIndex;

        public static MainCharacterGif Create(Vector3 feetPosition)
        {
            string folder = FindFolder();
            if (folder == null)
            {
                Debug.LogWarning("Main character frames not found (LiveAssets/MAIN_CHARACTER).");
                return null;
            }
            Instance = new MainCharacterGif(folder, feetPosition);
            return Instance;
        }

        private MainCharacterGif(string folder, Vector3 feetPosition)
        {
            files = Directory.GetFiles(folder, "*.png");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            sprites = new Sprite[files.Length];
            basePosition = feetPosition;

            GameObject root = new("Main Character Stage Root");
            Shader glowShader = Resources.Load<Shader>("Shaders/SimpleParticle");
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            Material glowMaterial = new(glowShader) { renderQueue = 3080 };
            Mesh quadMesh = DiscoBallRig.BuildQuadMesh();

            podiumOuter = DiscoBallRig.CreateGlow(
                root.transform,
                "Stage Podium Outer Halo",
                quadMesh,
                glowMaterial,
                feetPosition + new Vector3(0f, 0.02f, 0f),
                3.8f
            );
            podiumOuter.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            podiumInner = DiscoBallRig.CreateGlow(
                root.transform,
                "Stage Podium Inner Halo",
                quadMesh,
                glowMaterial,
                feetPosition + new Vector3(0f, 0.03f, 0f),
                2.2f
            );
            podiumInner.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            backAura = DiscoBallRig.CreateGlow(
                root.transform,
                "Main Character Back Aura",
                quadMesh,
                glowMaterial,
                feetPosition + new Vector3(0f, 2.1f, -0.25f),
                4.4f
            );

            GameObject go = new("Main Character (nhanvatchinh)");
            go.transform.SetParent(root.transform, false);
            actorTransform = go.transform;
            actorTransform.position = feetPosition;
            actorTransform.localScale = Vector3.one * CharacterScale;

            spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sharedMaterial = GameMaterials.Sprite();
            spriteRenderer.sortingOrder = 2600;
            spriteRenderer.flipX = true; // camera nhin theo huong -Z

            if (files.Length > 0)
            {
                Sprite first = LoadFrame(0);
                if (first != null) spriteRenderer.sprite = first;
            }
            Debug.Log($"Main character: {files.Length} frames from {folder}");
        }

        public void Tick()
        {
            for (int n = 0; n < FramesLoadedPerTick && loadedCount < files.Length; n++)
                LoadFrame(loadedCount);

            if (loadedCount == 0) return;

            float beat = ClubBeatClock.Beat;
            float beatFraction = beat - Mathf.Floor(beat);
            float pulse = 0.6f + 0.4f * (1f - beatFraction) * (1f - beatFraction);

            CharacterMotion.Pose pose = CharacterMotion.Sample(beat, 0f, 0.55f);
            float lean = pose.Lean * 0.4f;
            float footLift = spriteRenderer.sprite == null ? 0f
                : Mathf.Abs(Mathf.Sin(lean * Mathf.Deg2Rad)) * spriteRenderer.sprite.bounds.extents.x * CharacterScale;
            actorTransform.position = basePosition + new Vector3(pose.Sway * 0.35f, pose.Lift * 0.35f + footLift, 0f);
            actorTransform.rotation = Quaternion.Euler(0f, pose.Yaw * 0.25f, lean);
            actorTransform.localScale = new Vector3(CharacterScale * (1f + pose.Stretch * 0.5f), CharacterScale * (1f - pose.Stretch * 0.35f), CharacterScale);

            SetGlow(podiumOuter, new Color(1f, 0.12f, 0.28f, 0.42f + 0.32f * pulse));
            SetGlow(podiumInner, new Color(1f, 0.72f, 0.2f, 0.48f + 0.35f * pulse));
            SetGlow(backAura, new Color(0.95f, 0.18f, 0.55f, 0.22f + 0.18f * pulse));

            clock += Time.deltaTime;
            float frameDuration = 1f / FramesPerSecond;
            while (clock >= frameDuration)
            {
                clock -= frameDuration;
                frameIndex = (frameIndex + 1) % loadedCount;
            }
            if (loadedCount < files.Length) frameIndex %= loadedCount;
            if (sprites[frameIndex] != null) spriteRenderer.sprite = sprites[frameIndex];
        }

        private void SetGlow(Renderer target, Color color)
        {
            if (target == null) return;
            glowBlock.Clear();
            glowBlock.SetColor(ColorId, color);
            target.SetPropertyBlock(glowBlock);
        }

        private Sprite LoadFrame(int index)
        {
            if (sprites[index] != null)
            {
                loadedCount = Mathf.Max(loadedCount, index + 1);
                return sprites[index];
            }
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileName(files[index]),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                texture.LoadImage(File.ReadAllBytes(files[index]));
                texture.Apply(false, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Main character frame failed: {exception.Message}");
            }
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f),
                100f
            );
            sprites[index] = sprite;
            loadedCount = Mathf.Max(loadedCount, index + 1);
            return sprite;
        }

        private static string FindFolder()
        {
            string buildRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string[] candidates =
            {
                Path.Combine(buildRoot, "LiveAssets", "MAIN_CHARACTER"),
                Path.Combine(projectRoot, "LiveAssets", "MAIN_CHARACTER"),
                Path.Combine(buildRoot, "LiveAssets", "DJ_GIF"),
                Path.Combine(projectRoot, "LiveAssets", "DJ_GIF")
            };
            foreach (string candidate in candidates)
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.png").Length > 0) return candidate;
            return null;
        }
    }

    /// <summary>
    /// Dan den Beam & Laser 2 ben vom mai Quán Bar Âm Phủ, quet theo nhac 128 BPM
    /// va ton vinh Nhan Vat Chinh ma khong che khuat tam nhin.
    /// </summary>
    public sealed class StageLightRig
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Color[] BeamPalette =
        {
            new(0.26f, 0.91f, 0.88f),
            new(1f, 0.31f, 0.64f),
            new(1f, 0.77f, 0.42f),
            new(0.24f, 0.65f, 1f),
            new(0.7f, 0.55f, 1f)
        };

        public static StageLightRig Instance { get; private set; }

        private struct BeamUnit
        {
            public Vector3 Origin;
            public LineRenderer Beam;
            public Renderer LensGlow;
            public Renderer FloorSpot;
            public float Side;
            public int Index;
            public float Phase;
        }

        private readonly BeamUnit[] units;
        private readonly MaterialPropertyBlock block = new();

        public static StageLightRig Create()
        {
            Instance = new StageLightRig();
            return Instance;
        }

        private StageLightRig()
        {
            GameObject root = new("Stage Light Rig");
            Shader glowShader = Resources.Load<Shader>("Shaders/SimpleParticle");
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            Material glowMaterial = new(glowShader) { renderQueue = 3070 };
            Material beamMaterial = GameMaterials.BackgroundSprite();
            Mesh quadMesh = DiscoBallRig.BuildQuadMesh();

            float[] xOffsets = { 1.65f, 2.55f, 3.55f, 4.55f, 5.45f };
            float[] yOffsets = { 6.55f, 6.35f, 6.1f, 5.85f, 5.55f };
            units = new BeamUnit[xOffsets.Length * 2];
            int count = 0;

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < xOffsets.Length; i++)
                {
                    Vector3 origin = new(side * xOffsets[i], yOffsets[i], -3.6f - i * 0.18f);
                    GameObject unitObj = new($"Stage Beam {sideIndex}-{i}");
                    unitObj.transform.SetParent(root.transform, false);

                    LineRenderer line = unitObj.AddComponent<LineRenderer>();
                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.startWidth = 0.08f;
                    line.endWidth = 0.36f;
                    line.numCapVertices = 3;
                    line.sharedMaterial = beamMaterial;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    MaterialPropertyBlock lineBlock = new();
                    lineBlock.SetTexture("_MainTex", Texture2D.whiteTexture);
                    line.SetPropertyBlock(lineBlock);

                    Renderer lens = DiscoBallRig.CreateGlow(
                        root.transform,
                        $"Stage Lens Glow {sideIndex}-{i}",
                        quadMesh,
                        glowMaterial,
                        origin,
                        0.68f
                    );

                    Renderer floorSpot = DiscoBallRig.CreateGlow(
                        root.transform,
                        $"Stage Beam Floor Spot {sideIndex}-{i}",
                        quadMesh,
                        glowMaterial,
                        new Vector3(origin.x * 0.6f, 0.03f, 0f),
                        1.45f
                    );
                    floorSpot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                    units[count] = new BeamUnit
                    {
                        Origin = origin,
                        Beam = line,
                        LensGlow = lens,
                        FloorSpot = floorSpot,
                        Side = side,
                        Index = i,
                        Phase = count * 0.68f
                    };
                    count++;
                }
            }
        }

        public void Tick()
        {
            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float beatFraction = beat - Mathf.Floor(beat);
            float pulse = 0.55f + 0.45f * (1f - beatFraction) * (1f - beatFraction);
            int colorShift = Mathf.FloorToInt(beat / 2f);
            Camera camera = Camera.main;

            for (int i = 0; i < units.Length; i++)
            {
                BeamUnit unit = units[i];
                Color baseColor = BeamPalette[(unit.Index + colorShift + (unit.Side > 0 ? 1 : 0)) % BeamPalette.Length];

                float sweep = Mathf.Sin(time * 1.45f + unit.Phase);
                float depthWave = Mathf.Cos(time * 1.05f + unit.Index * 0.9f);
                // Quet cheo hai ben san nhay, tranh che mat nhan vat chinh
                float targetX = unit.Side * Mathf.Lerp(1.6f, 4.8f, (sweep + 1f) * 0.5f) - unit.Side * 0.8f * depthWave;
                float targetZ = Mathf.Lerp(-2.6f, 3.6f, (depthWave + 1f) * 0.5f);
                Vector3 endPoint = new(targetX, 0.03f, targetZ);

                unit.Beam.SetPosition(0, unit.Origin);
                unit.Beam.SetPosition(1, endPoint);
                unit.Beam.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.42f * pulse);
                unit.Beam.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.04f);

                if (camera != null)
                {
                    unit.LensGlow.transform.rotation = Quaternion.LookRotation(
                        unit.LensGlow.transform.position - camera.transform.position
                    );
                }
                SetGlow(unit.LensGlow, new Color(baseColor.r, baseColor.g, baseColor.b, 0.72f + 0.25f * pulse));

                unit.FloorSpot.transform.position = endPoint;
                SetGlow(unit.FloorSpot, new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f + 0.28f * pulse));
            }
        }

        private void SetGlow(Renderer target, Color color)
        {
            block.Clear();
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Qua cau disco: vo guong xoay, tia lap lanh, quang sang va cac dom sang quet tren san.
    /// </summary>
    public sealed class DiscoBallRig
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public static DiscoBallRig Instance { get; private set; }

        private readonly Transform ball;
        private readonly Renderer[] tiles;
        private readonly Vector3[] tileNormals;
        private readonly float[] tileSeeds;
        private readonly Renderer halo;
        private readonly Transform[] spots;
        private readonly Renderer[] spotRenderers;
        private readonly float[] spotRadius;
        private readonly float[] spotAngle;
        private readonly Vector3 floorCenter;
        private readonly MaterialPropertyBlock block = new();

        private static readonly Color[] SpotColors =
        {
            new(1f, 1f, 1f), new(1f, 0.35f, 0.45f), new(0.75f, 0.45f, 1f), new(1f, 0.75f, 0.35f), new(0.45f, 0.85f, 1f)
        };

        public static DiscoBallRig Create(Vector3 position, float diameter)
        {
            Instance = new DiscoBallRig(position, diameter);
            return Instance;
        }

        private DiscoBallRig(Vector3 position, float diameter)
        {
            GameObject root = new("Disco Ball Rig");
            ball = new GameObject("Disco Mirror Ball").transform;
            ball.SetParent(root.transform, false);
            ball.position = position;
            ball.localScale = Vector3.one * diameter;

            Material core = GameMaterials.Club(new Color(0.05f, 0.05f, 0.07f), new Color(0.02f, 0.02f, 0.03f));
            GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "Disco Ball Core";
            UnityEngine.Object.Destroy(coreSphere.GetComponent<Collider>());
            coreSphere.transform.SetParent(ball, false);
            coreSphere.transform.localScale = Vector3.one * 0.97f;
            coreSphere.GetComponent<Renderer>().sharedMaterial = core;

            Material tileMaterial = GameMaterials.Club(new Color(0.55f, 0.58f, 0.65f), Color.black);
            const int latitudeCount = 12;
            int total = 0;
            int[] perRow = new int[latitudeCount];
            for (int lat = 0; lat < latitudeCount; lat++)
            {
                float phi = Mathf.Lerp(-80f, 80f, lat / (float)(latitudeCount - 1)) * Mathf.Deg2Rad;
                perRow[lat] = Mathf.Max(6, Mathf.RoundToInt(26 * Mathf.Cos(phi)));
                total += perRow[lat];
            }
            tiles = new Renderer[total];
            tileNormals = new Vector3[total];
            tileSeeds = new float[total];
            int t = 0;
            for (int lat = 0; lat < latitudeCount; lat++)
            {
                float phi = Mathf.Lerp(-80f, 80f, lat / (float)(latitudeCount - 1)) * Mathf.Deg2Rad;
                for (int lon = 0; lon < perRow[lat]; lon++)
                {
                    float theta = lon * Mathf.PI * 2f / perRow[lat] + (lat % 2) * 0.12f;
                    Vector3 normal = new(Mathf.Cos(phi) * Mathf.Sin(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Cos(theta));
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Mirror Tile {lat}-{lon}";
                    UnityEngine.Object.Destroy(tile.GetComponent<Collider>());
                    tile.transform.SetParent(ball, false);
                    tile.transform.localPosition = normal * 0.5f;
                    tile.transform.localRotation = Quaternion.LookRotation(normal);
                    tile.transform.localScale = new Vector3(0.105f, 0.105f, 0.02f);
                    Renderer renderer = tile.GetComponent<Renderer>();
                    renderer.sharedMaterial = tileMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    tiles[t] = renderer;
                    tileNormals[t] = normal;
                    tileSeeds[t] = UnityEngine.Random.value * 10f;
                    t++;
                }
            }

            // Day treo
            GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable.name = "Disco Ball Cable";
            UnityEngine.Object.Destroy(cable.GetComponent<Collider>());
            cable.transform.SetParent(root.transform, false);
            cable.transform.position = position + Vector3.up * (diameter * 0.5f + 2f);
            cable.transform.localScale = new Vector3(0.025f, 2f, 0.025f);
            cable.GetComponent<Renderer>().sharedMaterial = GameMaterials.Club(new Color(0.08f, 0.08f, 0.1f), Color.black);

            // Quang sang + dom sang
            Shader glowShader = Resources.Load<Shader>("Shaders/SimpleParticle");
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            Material glowMaterial = new(glowShader) { renderQueue = 3060 };
            Mesh quadMesh = BuildQuadMesh();

            halo = CreateGlow(root.transform, "Disco Ball Halo", quadMesh, glowMaterial, position, diameter * 2.9f);

            floorCenter = new Vector3(position.x, 0.035f, 0.5f);
            const int spotCount = 36;
            spots = new Transform[spotCount];
            spotRenderers = new Renderer[spotCount];
            spotRadius = new float[spotCount];
            spotAngle = new float[spotCount];
            for (int i = 0; i < spotCount; i++)
            {
                spotRadius[i] = Mathf.Lerp(1.2f, 5.8f, (i * 0.618f) % 1f);
                spotAngle[i] = i * 137.5f * Mathf.Deg2Rad;
                float size = Mathf.Lerp(0.32f, 0.55f, (i * 0.37f) % 1f);
                Renderer spot = CreateGlow(root.transform, $"Disco Spot {i}", quadMesh, glowMaterial, floorCenter, size);
                spot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                spots[i] = spot.transform;
                spotRenderers[i] = spot;
            }
        }

        public void Tick()
        {
            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float beatFraction = beat - Mathf.Floor(beat);
            float pulse = 0.6f + 0.4f * (1f - beatFraction) * (1f - beatFraction);
            float spin = time * 24f;
            ball.rotation = Quaternion.Euler(0f, spin, 0f);

            Camera camera = Camera.main;
            Vector3 toCamera = camera != null ? (camera.transform.position - ball.position).normalized : Vector3.forward;
            Quaternion rotation = ball.rotation;
            for (int i = 0; i < tiles.Length; i++)
            {
                Vector3 normal = rotation * tileNormals[i];
                float facing = Mathf.Clamp01(Vector3.Dot(normal, toCamera));
                float glint = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 3.1f + tileSeeds[i] + normal.x * 4f)), 18f);
                float level = 0.22f + 0.58f * facing * facing + glint * 1.25f;
                Color baseColor = new Color(0.66f, 0.64f, 0.76f) * level;
                Color tint = SpotColors[(i + Mathf.FloorToInt(beat)) % SpotColors.Length] * (glint * 0.65f);
                block.Clear();
                block.SetColor(ColorId, baseColor);
                block.SetColor(EmissionId, tint);
                tiles[i].SetPropertyBlock(block);
            }

            if (camera != null) halo.transform.rotation = Quaternion.LookRotation(halo.transform.position - camera.transform.position);
            SetGlow(halo, new Color(1f, 0.85f, 0.95f, 0.2f + 0.16f * pulse));

            float spinRad = spin * Mathf.Deg2Rad;
            for (int i = 0; i < spots.Length; i++)
            {
                float angle = spotAngle[i] + spinRad;
                Vector3 offset = new(Mathf.Cos(angle) * spotRadius[i], (i % 3) * 0.001f, Mathf.Sin(angle) * spotRadius[i] * 0.75f);
                spots[i].position = floorCenter + offset;
                Color color = SpotColors[i % SpotColors.Length];
                float twinkle = 0.6f + 0.4f * Mathf.Sin(time * 5f + i * 1.7f);
                color.a = Mathf.Clamp01(0.5f * twinkle * pulse + 0.12f);
                SetGlow(spotRenderers[i], color);
            }
        }

        private void SetGlow(Renderer target, Color color)
        {
            block.Clear();
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }

        public static Renderer CreateGlow(Transform parent, string name, Mesh mesh, Material material, Vector3 position, float size)
        {
            GameObject glow = new(name);
            glow.transform.SetParent(parent, false);
            glow.transform.position = position;
            glow.transform.localScale = new Vector3(size, size, 1f);
            glow.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = glow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static Mesh BuildQuadMesh()
        {
            Mesh mesh = new() { name = "Disco Glow Quad" };
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
