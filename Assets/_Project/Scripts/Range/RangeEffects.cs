using System;
using System.Collections.Generic;
using RangeControl.Core;
using RangeControl.Targets;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>
    /// Everything a shot looks and sounds like once it has left the gun: tracers, impact particles, bullet
    /// holes and impact sounds. It listens to the event channel and knows nothing about weapons or targets.
    /// Tracers, decals and sound sources are pooled; each kind of impact shares one particle system that is
    /// moved to the hit and told to emit, so a round creates and destroys nothing.
    /// </summary>
    public sealed class RangeEffects : MonoBehaviour
    {
        struct PlayingSound
        {
            public AudioSource Source;
            public float Remaining;
        }

        [SerializeField] RangeEvents m_Events;

        [Header("Tracers")]
        [SerializeField] Tracer m_TracerPrefab;
        [SerializeField] int m_TracerPrewarm = 10;

        [Header("Impact particles, one shared emitter per surface")]
        [SerializeField] ParticleSystem m_DirtImpact;
        [SerializeField] ParticleSystem m_WoodImpact;
        [SerializeField] ParticleSystem m_SteelImpact;
        [SerializeField] ParticleSystem m_PaperImpact;
        [SerializeField] int m_ParticlesPerImpact = 8;

        [Header("Bullet holes")]
        [SerializeField] Transform m_DecalPrefab;
        [SerializeField] int m_MaxDecals = 48;
        [SerializeField] float m_DecalOffset = 0.003f;

        [Header("Impact sounds")]
        [SerializeField] AudioSource m_SoundPrefab;
        [SerializeField] int m_SoundPrewarm = 6;
        [SerializeField] AudioClip m_DirtClip;
        [SerializeField] AudioClip m_WoodClip;
        [SerializeField] AudioClip m_SteelClip;
        [SerializeField] AudioClip m_PaperClip;
        [Tooltip("Width in metres of the steel plate that rings at the clip's own pitch.")]
        [SerializeField] float m_ReferencePlateWidth = 0.3f;
        [Tooltip("Plays at the player when a far target is hit, so the hit is heard without waiting for the ping.")]
        [SerializeField] AudioSource m_HitTick;
        [SerializeField] float m_FarHitDistance = 20f;

        readonly List<PlayingSound> m_Playing = new List<PlayingSound>(16);
        readonly int[] m_LastSoundFrame = new int[4];
        PrefabPool<Tracer> m_Tracers;
        PrefabPool<AudioSource> m_Sounds;
        Action<Tracer> m_ReleaseTracer;
        Transform[] m_Decals;
        int m_NextDecal;

        void Awake()
        {
            m_ReleaseTracer = tracer => m_Tracers.Release(tracer);
            if (m_TracerPrefab != null)
                m_Tracers = new PrefabPool<Tracer>(m_TracerPrefab, transform, m_TracerPrewarm);
            if (m_SoundPrefab != null)
                m_Sounds = new PrefabPool<AudioSource>(m_SoundPrefab, transform, m_SoundPrewarm);

            m_Decals = new Transform[m_DecalPrefab != null ? m_MaxDecals : 0];
            for (var i = 0; i < m_Decals.Length; i++)
            {
                m_Decals[i] = Instantiate(m_DecalPrefab, transform);
                m_Decals[i].gameObject.SetActive(false);
            }

            for (var i = 0; i < m_LastSoundFrame.Length; i++)
                m_LastSoundFrame[i] = -1;
        }

        void OnEnable()
        {
            m_Events.BulletTraced += OnBulletTraced;
            m_Events.TargetHit += OnTargetHit;
        }

        void OnDisable()
        {
            m_Events.BulletTraced -= OnBulletTraced;
            m_Events.TargetHit -= OnTargetHit;
        }

        void Update()
        {
            for (var i = m_Playing.Count - 1; i >= 0; i--)
            {
                var sound = m_Playing[i];
                sound.Remaining -= Time.deltaTime;
                if (sound.Remaining > 0f)
                {
                    m_Playing[i] = sound;
                    continue;
                }

                m_Sounds.Release(sound.Source);
                m_Playing[i] = m_Playing[m_Playing.Count - 1];
                m_Playing.RemoveAt(m_Playing.Count - 1);
            }
        }

        void OnBulletTraced(BulletTrace trace)
        {
            if (trace.ShowTracer && m_Tracers != null)
                m_Tracers.Get().Fire(trace.Origin, trace.End, m_ReleaseTracer);

            if (trace.Collider == null)
                return;

            var surface = SurfaceType.Dirt;
            var leavesDecal = false;
            var pitch = 1f;
            if (trace.Collider.TryGetComponent(out SurfaceMarker marker))
            {
                surface = marker.Surface;
                leavesDecal = marker.LeavesDecal;
                // A smaller plate rings higher.
                if (surface == SurfaceType.Steel)
                    pitch = Mathf.Clamp(m_ReferencePlateWidth / Mathf.Max(0.05f, trace.Collider.bounds.size.x), 0.6f, 1.6f);
            }

            var particles = ParticlesFor(surface);
            if (particles != null)
            {
                particles.transform.SetPositionAndRotation(trace.End, Quaternion.LookRotation(trace.Normal));
                particles.Emit(m_ParticlesPerImpact);
            }

            if (leavesDecal && m_Decals.Length > 0)
            {
                var decal = m_Decals[m_NextDecal];
                m_NextDecal = (m_NextDecal + 1) % m_Decals.Length;
                decal.SetPositionAndRotation(trace.End + trace.Normal * m_DecalOffset, Quaternion.LookRotation(-trace.Normal));
                decal.gameObject.SetActive(true);
            }

            PlayImpactSound(surface, trace.End, pitch);
        }

        void OnTargetHit(TargetHit hit)
        {
            if (m_HitTick != null && hit.BasePoints > 0 && hit.Distance >= m_FarHitDistance)
                m_HitTick.Play();
        }

        void PlayImpactSound(SurfaceType surface, Vector3 position, float pitch)
        {
            var clip = ClipFor(surface);
            if (clip == null || m_Sounds == null)
                return;

            // One sound per surface per frame: a shotgun pattern should thump once, not eight times.
            var index = (int)surface;
            if (m_LastSoundFrame[index] == Time.frameCount)
                return;
            m_LastSoundFrame[index] = Time.frameCount;

            var source = m_Sounds.Get(position, Quaternion.identity);
            source.clip = clip;
            source.pitch = pitch;
            source.Play();
            m_Playing.Add(new PlayingSound { Source = source, Remaining = clip.length / pitch + 0.05f });
        }

        ParticleSystem ParticlesFor(SurfaceType surface)
        {
            switch (surface)
            {
                case SurfaceType.Wood: return m_WoodImpact;
                case SurfaceType.Steel: return m_SteelImpact;
                case SurfaceType.Paper: return m_PaperImpact;
                default: return m_DirtImpact;
            }
        }

        AudioClip ClipFor(SurfaceType surface)
        {
            switch (surface)
            {
                case SurfaceType.Wood: return m_WoodClip;
                case SurfaceType.Steel: return m_SteelClip;
                case SurfaceType.Paper: return m_PaperClip;
                default: return m_DirtClip;
            }
        }
    }
}
