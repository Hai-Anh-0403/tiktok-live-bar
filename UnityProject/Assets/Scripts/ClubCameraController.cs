using System.Collections.Generic;
using UnityEngine;

namespace TikTokLiveGame
{
    [RequireComponent(typeof(Camera))]
    public sealed class ClubCameraController : MonoBehaviour
    {
        private readonly Vector3 defaultPosition = new(0f, 7.6f, 14.2f);
        private readonly Vector3 focusWideBasePosition = new(0f, 6.4f, 12.4f);
        private readonly Vector3 defaultLookAt = new(0f, 1.2f, -1.2f);
        private Transform focusTarget;
        private PlayerActor focusActor;
        private float focusUntil;
        private bool focusVip;
        private bool focusWide;
        private bool focusFireworks;
        private bool focusCrowdWelcome;
        private float focusStartedAt;
        private float focusDuration;
        private readonly Queue<WelcomeRequest> welcomeQueue = new();
        private bool welcomeOverflow;
        private Vector3 currentLookAt;
        private int directorShot;
        private int directorCycle;
        private float directorShotStartBeat;
        private Vector3 directorBlendPosition;
        private Vector3 directorBlendLookAt;
        private float directorBlendFov;
        private float directorBlendStartBeat;
        private bool wasFocusing;
        private PlayerManager playerManager;

        private void Awake()
        {
            currentLookAt = defaultLookAt;
            directorShotStartBeat = ClubBeatClock.Beat;
            directorBlendStartBeat = directorShotStartBeat;
            directorBlendPosition = transform.position;
            directorBlendLookAt = currentLookAt;
            directorBlendFov = GetComponent<Camera>().fieldOfView;
            playerManager = FindFirstObjectByType<PlayerManager>();
        }

        public void Focus(PlayerActor actor, float seconds, bool vip, bool wide = false)
        {
            if (actor == null) return;
            focusTarget = actor.transform;
            focusActor = actor;
            focusUntil = Time.time + Mathf.Clamp(seconds, 2f, 12f);
            focusVip = vip;
            focusWide = wide;
            focusFireworks = false;
            focusCrowdWelcome = false;
            focusStartedAt = Time.time;
            focusDuration = Mathf.Clamp(seconds, 2f, 12f);
        }

        public void FocusFireworks(PlayerActor actor, float seconds = 6f)
        {
            if (actor == null) return;
            focusTarget = actor.transform;
            focusActor = actor;
            focusDuration = Mathf.Clamp(seconds, 5.5f, 8f);
            focusStartedAt = Time.time;
            focusUntil = focusStartedAt + focusDuration;
            focusVip = false;
            focusWide = false;
            focusFireworks = true;
            focusCrowdWelcome = false;
        }

        public void QueueWelcome(PlayerActor actor, float seconds = 2f)
        {
            if (actor == null) return;
            WelcomeRequest request = new(actor, Mathf.Clamp(seconds, 2f, 3f));
            if (Time.time >= focusUntil && welcomeQueue.Count == 0 && !welcomeOverflow)
            {
                StartWelcome(request);
                return;
            }
            if (welcomeQueue.Count < 3) welcomeQueue.Enqueue(request);
            else welcomeOverflow = true;
        }

        private void StartWelcome(WelcomeRequest request)
        {
            if (request.Actor == null) return;
            focusTarget = request.Actor.transform;
            focusActor = request.Actor;
            focusDuration = request.Seconds;
            focusStartedAt = Time.time;
            focusUntil = focusStartedAt + focusDuration;
            focusVip = false;
            focusWide = false;
            focusFireworks = false;
            focusCrowdWelcome = false;
        }

        private void StartCrowdWelcome()
        {
            focusTarget = null;
            focusActor = null;
            focusDuration = 2.5f;
            focusStartedAt = Time.time;
            focusUntil = focusStartedAt + focusDuration;
            focusVip = false;
            focusWide = false;
            focusFireworks = false;
            focusCrowdWelcome = true;
        }

        private void StartNextWelcomeIfReady()
        {
            if (Time.time < focusUntil) return;
            while (welcomeQueue.Count > 0)
            {
                WelcomeRequest request = welcomeQueue.Dequeue();
                if (request.Actor == null) continue;
                StartWelcome(request);
                return;
            }
            if (!welcomeOverflow) return;
            welcomeOverflow = false;
            StartCrowdWelcome();
        }

        private void LateUpdate()
        {
            StartNextWelcomeIfReady();
            bool focusing = Time.time < focusUntil && (focusTarget != null || focusCrowdWelcome);
            Vector3 desiredPosition = defaultPosition;
            Vector3 desiredLookAt = defaultLookAt;
            float desiredFov = 45f;
            float handheldPitch = 0f;
            float handheldYaw = 0f;
            float handheldRoll = 0f;
            if (focusing)
            {
                wasFocusing = true;
                if (focusCrowdWelcome)
                {
                    Bounds crowdBounds = new(new Vector3(0f, 0f, 0f), new Vector3(11.5f, 0f, 7.4f));
                    if (playerManager == null) playerManager = FindFirstObjectByType<PlayerManager>();
                    if (playerManager != null && playerManager.TryGetCrowdBounds(out Bounds liveCrowdBounds))
                        crowdBounds = liveCrowdBounds;
                    desiredPosition = new Vector3(crowdBounds.center.x, 8.2f, crowdBounds.max.z + 10.2f);
                    desiredLookAt = crowdBounds.center + Vector3.up * 1.1f;
                    desiredFov = 47f;
                }
                else
                {
                Vector3 focusPos = focusActor != null ? focusActor.FocusPosition : focusTarget.position;
                Vector3 target = focusPos + Vector3.up * 0.88f;
                if (focusFireworks)
                {
                    float elapsed = Time.time - focusStartedAt;
                    float reveal = Smooth(Mathf.InverseLerp(1.15f, 2.05f, elapsed));
                    Vector3 closePosition = focusPos + new Vector3(0f, 2.1f, 6.2f);
                    Vector3 widePosition = new(focusPos.x * 0.15f, 6.8f, 14.0f);
                    Vector3 closeLookAt = target;
                    Vector3 wideLookAt = new(focusPos.x * 0.24f, 1.8f, -2.0f);
                    desiredPosition = Vector3.Lerp(closePosition, widePosition, reveal);
                    desiredLookAt = Vector3.Lerp(closeLookAt, wideLookAt, reveal);
                    desiredFov = Mathf.Lerp(34f, 48f, reveal);
                }
                else if (focusWide)
                {
                    // Keep the gift sender centered while visibly crossing the stage.
                    desiredPosition = focusPos + new Vector3(0f, 2.55f, 7.4f);
                    desiredLookAt = target;
                    desiredFov = 35f;
                }
                else
                {
                    desiredPosition = focusPos + (focusVip
                        ? new Vector3(0f, 1.95f, 5.8f)
                        : new Vector3(0f, 2.1f, 6.3f));
                    desiredLookAt = target;
                    desiredFov = focusVip ? 33f : 35f;
                }
                }
            }
            else
            {
                float beat = ClubBeatClock.Beat;
                bool shotChanged = wasFocusing;
                if (wasFocusing)
                {
                    // Return to the stable establishing shot after every focus.
                    // Jumping directly from a close-up to the overhead crane was
                    // visually tiring during a busy live session.
                    directorShot = 0;
                    directorShotStartBeat = beat;
                    wasFocusing = false;
                }
                else if (Input.GetKeyDown(KeyCode.F6))
                {
                    directorShot = (directorShot + 1) % 12;
                    if (directorShot == 0) directorCycle++;
                    directorShotStartBeat = beat;
                    shotChanged = true;
                }

                float shotBeats = DirectorShotBars(directorShot) * 4f;
                while (beat - directorShotStartBeat >= shotBeats)
                {
                    directorShotStartBeat += shotBeats;
                    directorShot++;
                    if (directorShot >= 12)
                    {
                        directorShot = 0;
                        directorCycle++;
                    }
                    shotBeats = DirectorShotBars(directorShot) * 4f;
                    shotChanged = true;
                }

                if (shotChanged)
                {
                    directorBlendPosition = transform.position;
                    directorBlendLookAt = currentLookAt;
                    directorBlendFov = GetComponent<Camera>().fieldOfView;
                    directorBlendStartBeat = beat;
                }

                float progress = Mathf.Clamp01((beat - directorShotStartBeat) / Mathf.Max(1f, shotBeats));
                if (playerManager == null) playerManager = FindFirstObjectByType<PlayerManager>();
                Bounds crowdBounds = new(new Vector3(0f, 0f, -0.25f), new Vector3(7f, 0f, 7.4f));
                if (playerManager != null && playerManager.TryGetCrowdBounds(out Bounds liveCrowdBounds))
                    crowdBounds = liveCrowdBounds;
                ConfigureDirectorShot(directorShot, directorCycle, progress, crowdBounds, ref desiredPosition, ref desiredLookAt, ref desiredFov);
                float shotBlend = Smooth(Mathf.Clamp01((beat - directorBlendStartBeat) / 2f));
                desiredPosition = Vector3.Lerp(directorBlendPosition, desiredPosition, shotBlend);
                desiredLookAt = Vector3.Lerp(directorBlendLookAt, desiredLookAt, shotBlend);
                desiredFov = Mathf.Lerp(directorBlendFov, desiredFov, shotBlend);

                // No handheld shake in comfort mode. The dancers and lights
                // already provide enough motion for a lively frame.
            }
            float posResponsiveness = focusing ? 6.0f : 2.25f;
            float lookResponsiveness = focusing ? 9.5f : 2.5f;
            float posBlend = 1f - Mathf.Exp(-posResponsiveness * Time.deltaTime);
            float lookBlend = 1f - Mathf.Exp(-lookResponsiveness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, posBlend);
            currentLookAt = Vector3.Lerp(currentLookAt, desiredLookAt, lookBlend);
            Quaternion targetRotation = Quaternion.LookRotation(currentLookAt - transform.position) *
                Quaternion.Euler(handheldPitch, handheldYaw, handheldRoll);
            transform.rotation = focusing
                ? targetRotation
                : Quaternion.LookRotation(Quaternion.Slerp(transform.rotation, targetRotation, posBlend) * Vector3.forward);
            Camera camera = GetComponent<Camera>();
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, desiredFov, posBlend);
            ClubSceneBuilder.EnsureBackdropContains(camera);
        }

        private static float DirectorShotBars(int shot)
        {
            if (shot == 5 || shot >= 8) return 4f;
            if (shot == 6) return 4.5f;
            return 3.5f;
        }

        private static Bounds DirectorFramingBounds(int shot, Bounds crowd, Vector3 lookAt)
        {
            if (shot == 0)
                return new Bounds(crowd.center + Vector3.up * 1.9f, crowd.size + new Vector3(2.4f, 3.8f, 1.5f));
            if (shot == 4 || shot == 9)
                return new Bounds(new Vector3(-1.025f, 2.05f, -4.15f), new Vector3(10.5f, 4.4f, 1f));
            if (shot == 8)
                return new Bounds(new Vector3(0f, 3.3f, -4.1f), new Vector3(7.8f, 6.4f, 3f));
            if (shot == 10)
                return new Bounds(new Vector3(0f, 4f, -4.5f), new Vector3(14f, 8.4f, 7f));
            return new Bounds(lookAt, new Vector3(5.8f, 3.8f, 2.5f));
        }

        private static void FitDirectorShot(int shot, Bounds crowd, float aspect, ref Vector3 position, Vector3 lookAt, float fov)
        {
            Bounds subjects = DirectorFramingBounds(shot, crowd, lookAt);
            Vector3 forward = (lookAt - position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);
            float vertical = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * 0.9f;
            float horizontal = vertical * Mathf.Max(0.1f, aspect);
            float distance = (lookAt - position).magnitude;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = subjects.center + new Vector3(
                    (corner & 1) == 0 ? -subjects.extents.x : subjects.extents.x,
                    (corner & 2) == 0 ? -subjects.extents.y : subjects.extents.y,
                    (corner & 4) == 0 ? -subjects.extents.z : subjects.extents.z) - lookAt;
                float depth = Vector3.Dot(point, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(point, right)) / horizontal - depth);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(point, up)) / vertical - depth);
            }
            position = lookAt - forward * distance;
        }

        private static float Smooth(float value) => value * value * (3f - 2f * value);

        private readonly struct WelcomeRequest
        {
            public PlayerActor Actor { get; }
            public float Seconds { get; }

            public WelcomeRequest(PlayerActor actor, float seconds)
            {
                Actor = actor;
                Seconds = seconds;
            }
        }

        private static void ConfigureDirectorShot(
            int shot,
            int cycle,
            float progress,
            Bounds crowd,
            ref Vector3 position,
            ref Vector3 lookAt,
            ref float fov)
        {
            float t = Smooth(progress);
            float side = cycle % 2 == 0 ? -1f : 1f;
            float centerX = crowd.center.x;
            float halfWidth = Mathf.Max(2.6f, crowd.extents.x + 0.45f);
            float left = centerX - halfWidth;
            float right = centerX + halfWidth;
            float front = crowd.max.z;
            float back = crowd.min.z;
            if (front - back < 1.2f) back = front - 0.8f;
            float middle = (front + back) * 0.5f;
            float sweepX = Mathf.Lerp(left, right, side < 0f ? t : 1f - t);
            float mirroredSweepX = centerX * 2f - sweepX;
            float gentleSweepX = Mathf.Lerp(centerX, sweepX, 0.62f);
            float gentleMirroredSweepX = Mathf.Lerp(centerX, mirroredSweepX, 0.62f);
            float cameraFront = Mathf.Max(front + 8.2f, 11.0f);
            switch (shot)
            {
                case 0: // Establish the entire dance floor before scanning rows.
                    position = Vector3.Lerp(new Vector3(0f, 7.8f, cameraFront + 1.4f), new Vector3(0f, 7.2f, cameraFront + 0.6f), t);
                    lookAt = new Vector3(0f, 1.05f, middle - 0.2f);
                    fov = Mathf.Lerp(50f, 47f, t);
                    break;
                case 1: // Sweep every dancer across the front rows.
                    position = new Vector3(gentleSweepX * 0.58f, 4.8f, cameraFront - 0.3f);
                    lookAt = new Vector3(gentleSweepX * 0.85f, 1.15f, Mathf.Lerp(front, middle, 0.2f));
                    fov = 42f;
                    break;
                case 2: // Sweep the middle rows from the opposite direction.
                    position = new Vector3(gentleMirroredSweepX * 0.56f, 5.4f, cameraFront + 0.1f);
                    lookAt = new Vector3(gentleMirroredSweepX * 0.85f, 1.1f, middle);
                    fov = 43f;
                    break;
                case 3: // Higher scan dedicated to dancers in the back rows.
                    position = new Vector3(gentleSweepX * 0.46f, 6.6f, cameraFront + 0.8f);
                    lookAt = new Vector3(gentleSweepX * 0.85f, 1.05f, back + 0.35f);
                    fov = 44f;
                    break;
                case 4: // Track across the fixed Top 1-2-3 lineup, not the DJ.
                    position = Vector3.Lerp(
                        new Vector3(-1.025f - side * 2.2f, 4.6f, 5.8f),
                        new Vector3(-1.025f + side * 2.2f, 4.35f, 5.0f), t);
                    lookAt = new Vector3(-1.025f, 1.85f, -4.15f);
                    fov = Mathf.Lerp(43f, 40f, t);
                    break;
                case 5: // Gentle same-side dolly; never crosses the whole floor.
                    position = Vector3.Lerp(
                        new Vector3(centerX + side * (halfWidth * 0.75f + 1.4f), 5.4f, cameraFront),
                        new Vector3(centerX + side * (halfWidth * 0.5f + 1.2f), 5.8f, cameraFront + 0.8f),
                        t);
                    lookAt = new Vector3(
                        centerX + side * Mathf.Lerp(halfWidth * 0.24f, -halfWidth * 0.12f, t),
                        1.2f,
                        middle);
                    fov = Mathf.Lerp(45f, 43f, t);
                    break;
                case 6: // Title Reveal shot: Camera lowers and tilts up to show the background text
                    position = Vector3.Lerp(
                        new Vector3(centerX, 5.2f, cameraFront + 0.8f),
                        new Vector3(centerX + side * 1.1f, 3.5f, cameraFront - 1.2f),
                        t);
                    lookAt = Vector3.Lerp(
                        new Vector3(centerX, 1.4f, middle),
                        new Vector3(centerX - side * 0.45f, 1.45f, -11.5f),
                        t);
                    fov = Mathf.Lerp(45f, 47f, t);
                    break;
                case 7: // Finish the crowd pass on the opposite half.
                    position = Vector3.Lerp(
                        new Vector3(centerX - side * (halfWidth * 0.75f + 0.8f), 4.9f, cameraFront - 0.4f),
                        new Vector3(centerX - side * 1.6f, 4.5f, cameraFront - 1.1f),
                        t);
                    lookAt = new Vector3(centerX - side * Mathf.Max(1.1f, halfWidth * 0.4f), 1.25f, middle + 0.35f);
                    fov = Mathf.Lerp(44f, 41f, t);
                    break;
                case 8: // Push toward the main performer with the DJ booth behind her.
                    position = Vector3.Lerp(new Vector3(-side * 0.85f, 5.1f, 13.5f), new Vector3(side * 0.55f, 4.5f, 9.2f), t);
                    lookAt = new Vector3(0f, 2.35f, -4.1f);
                    fov = Mathf.Lerp(43f, 40f, t);
                    break;
                case 9: // A shallow arc keeps all three ranked dancers and wings together.
                    float angle = Mathf.Lerp(-14f, 14f, t) * side * Mathf.Deg2Rad;
                    position = new Vector3(-1.025f + Mathf.Sin(angle) * 10.4f, 5.2f, -4.15f + Mathf.Cos(angle) * 10.4f);
                    lookAt = new Vector3(-1.025f, 1.85f, -4.15f);
                    fov = 43f;
                    break;
                case 10: // Rise and pull back to reveal the complete stage.
                    position = Vector3.Lerp(new Vector3(side * 1.2f, 5.4f, 9.8f), new Vector3(-side * 1.0f, 8.2f, 12.2f), t);
                    lookAt = Vector3.Lerp(new Vector3(0f, 1.8f, -4.2f), new Vector3(0f, 2.1f, -5.0f), t);
                    fov = Mathf.Lerp(45f, 48f, t);
                    break;
                case 11: // Opposite-side truck with a small orbit toward the stage.
                    position = Vector3.Lerp(
                        new Vector3(centerX - side * (halfWidth * 0.75f + 1.8f), 5.8f, cameraFront + 1.0f),
                        new Vector3(centerX - side * (halfWidth * 0.55f + 1.4f), 6.4f, cameraFront + 0.2f), t);
                    lookAt = Vector3.Lerp(new Vector3(centerX - side * 0.75f, 1.45f, middle), new Vector3(-side * 0.45f, 1.85f, -4.15f), t);
                    fov = Mathf.Lerp(44f, 42f, t);
                    break;
            }
        }

    }
}
