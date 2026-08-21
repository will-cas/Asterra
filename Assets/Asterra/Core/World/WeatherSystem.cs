using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Core.World
{
    public enum WeatherEventKind : byte
    {
        TransitionStarted = 1,
        TransitionCompleted = 2,
        Lightning = 3,
        Gust = 4,
    }

    public readonly struct WeatherEvent
    {
        public readonly WeatherEventKind Kind;
        public readonly WeatherKind Weather;
        public readonly float Intensity;
        public readonly float X;
        public readonly float Z;

        public WeatherEvent(WeatherEventKind kind, WeatherKind weather, float intensity, float x = 0f, float z = 0f)
        {
            Kind = kind;
            Weather = weather;
            Intensity = intensity;
            X = x;
            Z = z;
        }
    }

    public readonly struct LightningStrike
    {
        public readonly float X;
        public readonly float Z;
        public readonly float Intensity;
        /// <summary>Seconds until thunder should play at this listener distance (presentation).</summary>
        public readonly float ThunderDelayHint;

        public LightningStrike(float x, float z, float intensity, float thunderDelayHint)
        {
            X = x;
            Z = z;
            Intensity = intensity;
            ThunderDelayHint = thunderDelayHint;
        }
    }

    /// <summary>
    /// Deterministic weather state machine with smooth transitions, wind, and lightning hooks.
    /// Optional terrain coupling (waterlog / snow / ice) is applied in throttled scans.
    /// </summary>
    public sealed class WeatherSystem : IWeatherSystem
    {
        private readonly WeatherDefData[] _defs;
        private readonly Dictionary<string, int> _defIndex = new();
        private readonly WorldTerrainGrid _grid;
        private readonly List<WeatherEvent> _events = new();
        private DeterministicRandom _rng;

        private WeatherDefData _currentDef;
        private WeatherDefData _targetDef;
        private float _holdRemaining;
        private float _transitionElapsed;
        private float _transitionDuration;
        private bool _transitioning;
        private float _displayIntensity;
        private float _temperature = 0.15f; // -1 cold .. +1 hot, relative

        private float _windDirX = 1f;
        private float _windDirZ;
        private float _windIntensity = 0.2f;
        private float _windTargetIntensity = 0.2f;
        private float _gustTimer;

        private float _envAcc;
        private int _envCursor;
        private const float EnvInterval = 0.5f;
        private const int EnvCellsPerPulse = 48;

        public WeatherState Current { get; private set; }
        public WeatherState? TransitionTarget { get; private set; }
        public float WindDirX => _windDirX;
        public float WindDirZ => _windDirZ;
        public float WindIntensity => _windIntensity;
        public float Temperature => _temperature;
        public float FogDensity { get; private set; }
        public float PrecipitationRate { get; private set; }
        public float SnowfallRate { get; private set; }
        public IReadOnlyList<WeatherEvent> Events => _events;

        /// <summary>Ring buffer of recent snow footprints (sim-space). No GameObjects.</summary>
        public SnowFootprintBuffer Footprints { get; } = new SnowFootprintBuffer(256);

        public WeatherSystem(uint seed, WorldTerrainGrid grid = null, WeatherDefData[] defs = null)
        {
            _rng = new DeterministicRandom(seed);
            _grid = grid;
            _defs = defs ?? DefaultWeatherCatalog.CreateDefs();
            for (int i = 0; i < _defs.Length; i++)
                _defIndex[_defs[i].Id] = i;

            _currentDef = FindDef(WeatherKind.Clear) ?? _defs[0];
            _targetDef = _currentDef;
            _displayIntensity = _currentDef.DefaultIntensity;
            _holdRemaining = Lerp(_currentDef.MinDurationSeconds, _currentDef.MaxDurationSeconds, _rng.NextFloat());
            _transitionDuration = _currentDef.TransitionSeconds;
            RebuildState(remaining: _holdRemaining);
            PickNewWindTarget();
        }

        public void ClearEvents() => _events.Clear();

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;

            ClearEvents();
            TickWind(deltaSeconds);
            TickWeatherMachine(deltaSeconds);
            TickLightning(deltaSeconds);
            TickEnvironment(deltaSeconds);
            RebuildState(_transitioning
                ? Math.Max(0f, _transitionDuration - _transitionElapsed)
                : _holdRemaining);
        }

        public float EffectiveVisibility()
        {
            float v = Current.VisibilityModifier;
            if (FogDensity > 0f)
                v *= 1f - FogDensity * 0.55f;
            return Math.Max(0.15f, v);
        }

        public float EffectiveMovement()
        {
            return Current.MovementModifier;
        }

        /// <summary>Force a transition (tests / abilities). Deterministic once called.</summary>
        public void ForceTransitionTo(WeatherKind kind)
        {
            var def = FindDef(kind);
            if (def == null)
                return;
            BeginTransition(def);
        }

        private void TickWeatherMachine(float dt)
        {
            if (_transitioning)
            {
                _transitionElapsed += dt;
                float t = _transitionDuration <= 0.001f ? 1f : Math.Min(1f, _transitionElapsed / _transitionDuration);
                // Smoothstep blend of intensity and temperature toward target.
                float s = t * t * (3f - 2f * t);
                _displayIntensity = Lerp(_currentDef.DefaultIntensity, _targetDef.DefaultIntensity, s);
                _temperature = Lerp(
                    _temperature,
                    Clamp(_temperature + (_targetDef.TemperatureDelta - _currentDef.TemperatureDelta) * s, -1f, 1f),
                    0.15f);

                if (t >= 1f)
                {
                    _currentDef = _targetDef;
                    _displayIntensity = _currentDef.DefaultIntensity;
                    _transitioning = false;
                    _holdRemaining = Lerp(_currentDef.MinDurationSeconds, _currentDef.MaxDurationSeconds, _rng.NextFloat());
                    TransitionTarget = null;
                    _events.Add(new WeatherEvent(WeatherEventKind.TransitionCompleted, _currentDef.Kind, _displayIntensity));
                }

                return;
            }

            _holdRemaining -= dt;
            // Intensity can gently breathe while holding.
            float breathe = 0.04f * (float)Math.Sin(_holdRemaining * 0.35f);
            _displayIntensity = Clamp(_currentDef.DefaultIntensity + breathe, 0f, 1f);
            _temperature = MoveToward(_temperature, Clamp(_currentDef.TemperatureDelta, -1f, 1f), dt * 0.05f);

            if (_holdRemaining <= 0f)
            {
                var next = PickNextDef(_currentDef.Kind);
                BeginTransition(next);
            }
        }

        private void BeginTransition(WeatherDefData next)
        {
            _targetDef = next;
            _transitioning = true;
            _transitionElapsed = 0f;
            _transitionDuration = Math.Max(0.5f, next.TransitionSeconds);
            TransitionTarget = BuildState(next, next.DefaultIntensity, next.MaxDurationSeconds, _transitionDuration, _transitionDuration);
            _events.Add(new WeatherEvent(WeatherEventKind.TransitionStarted, next.Kind, next.DefaultIntensity));
        }

        private void TickWind(float dt)
        {
            _gustTimer -= dt;
            if (_gustTimer <= 0f)
            {
                PickNewWindTarget();
                if (_rng.NextFloat() < 0.25f)
                {
                    _windTargetIntensity = Clamp(_windTargetIntensity + 0.35f + _rng.NextFloat() * 0.4f, 0f, 1.5f);
                    _events.Add(new WeatherEvent(WeatherEventKind.Gust, _currentDef.Kind, _windTargetIntensity));
                }

                _gustTimer = 4f + _rng.NextFloat() * 8f;
            }

            // Rotate wind slowly.
            float ang = (_rng.NextFloat() - 0.5f) * 0.02f * dt;
            float cos = (float)Math.Cos(ang);
            float sin = (float)Math.Sin(ang);
            float nx = _windDirX * cos - _windDirZ * sin;
            float nz = _windDirX * sin + _windDirZ * cos;
            float len = MathF.Sqrt(nx * nx + nz * nz);
            if (len > 0.0001f)
            {
                _windDirX = nx / len;
                _windDirZ = nz / len;
            }

            _windIntensity = MoveToward(_windIntensity, _windTargetIntensity, dt * 0.2f);
        }

        private void TickLightning(float dt)
        {
            bool stormy = _currentDef.EnablesLightning
                          || (_transitioning && _targetDef.EnablesLightning);
            if (!stormy)
                return;

            float chance = _currentDef.LightningChancePerSecond;
            if (_transitioning)
                chance = Lerp(chance, _targetDef.LightningChancePerSecond, Math.Min(1f, _transitionElapsed / Math.Max(0.1f, _transitionDuration)));
            chance *= Math.Max(0.2f, _displayIntensity);

            if (_rng.NextFloat() < chance * dt)
            {
                float x = 0f;
                float z = 0f;
                if (_grid != null)
                {
                    x = _grid.OriginX + _rng.NextFloat() * _grid.Width * _grid.CellSize;
                    z = _grid.OriginZ + _rng.NextFloat() * _grid.Height * _grid.CellSize;
                }
                else
                {
                    x = (_rng.NextFloat() - 0.5f) * 800f;
                    z = (_rng.NextFloat() - 0.5f) * 800f;
                }

                float intensity = 0.6f + _rng.NextFloat() * 0.4f;
                // Hint: ~343 m/s sound; map units ~meters → delay ≈ distance/343 for a listener at origin.
                float dist = MathF.Sqrt(x * x + z * z);
                float delay = dist / 343f;
                _events.Add(new WeatherEvent(WeatherEventKind.Lightning, WeatherKind.Storm, intensity, x, z));
                // Thunder is presentation-side using delay hint via event position.
                _ = new LightningStrike(x, z, intensity, delay);
            }
        }

        private void TickEnvironment(float dt)
        {
            PrecipitationRate = 0f;
            SnowfallRate = 0f;
            FogDensity = 0f;

            var active = _transitioning ? BlendDefsVisual() : _currentDef;
            float intensity = _displayIntensity;
            if (active.Kind == WeatherKind.Rain || active.Kind == WeatherKind.Storm)
                PrecipitationRate = active.PrecipitationRate * intensity;
            if (active.Kind == WeatherKind.Snow)
                SnowfallRate = active.SnowfallRate * intensity;
            if (active.Kind == WeatherKind.Fog)
                FogDensity = intensity;
            else if (active.Kind == WeatherKind.Storm || active.Kind == WeatherKind.Rain)
                FogDensity = intensity * 0.15f;

            if (_grid == null)
                return;

            _envAcc += dt;
            if (_envAcc < EnvInterval)
                return;
            _envAcc = 0f;

            int total = _grid.Width * _grid.Height;
            if (total <= 0)
                return;

            for (int n = 0; n < EnvCellsPerPulse; n++)
            {
                int index = _envCursor % total;
                _envCursor++;
                int cx = index % _grid.Width;
                int cz = index / _grid.Width;
                float wx = _grid.OriginX + (cx + 0.5f) * _grid.CellSize;
                float wz = _grid.OriginZ + (cz + 0.5f) * _grid.CellSize;
                if (!_grid.TryGetCell(wx, wz, out var cell))
                    continue;

                var def = _grid.GetDef(cell.TerrainDefIndex);
                bool changed = false;

                // Waterlogging from rain.
                if (PrecipitationRate > 0f && def.WaterlogSensitivity > 0f)
                {
                    int add = (int)(PrecipitationRate * def.WaterlogSensitivity * 12f);
                    int next = cell.Waterlog01 + add;
                    if (next > 255)
                        next = 255;
                    if (next != cell.Waterlog01)
                    {
                        cell.Waterlog01 = (byte)next;
                        if (cell.Waterlog01 > 160)
                            cell.Flags = (byte)(cell.Flags | TerrainCell.FlagMuddy);
                        changed = true;
                    }
                }
                else if (cell.Waterlog01 > 0 && def.DrainageRate > 0f)
                {
                    // Sunny / dry weather drains faster.
                    float drainMul = active.Kind == WeatherKind.Sunny ? 2.2f : active.Kind == WeatherKind.Clear ? 1.4f : 1f;
                    int sub = (int)(def.DrainageRate * drainMul * 10f);
                    int next = cell.Waterlog01 - sub;
                    if (next < 0)
                        next = 0;
                    if (next != cell.Waterlog01)
                    {
                        cell.Waterlog01 = (byte)next;
                        if (cell.Waterlog01 < 80)
                            cell.Flags = (byte)(cell.Flags & ~TerrainCell.FlagMuddy);
                        changed = true;
                    }
                }

                // Snow accumulation / melt.
                if (SnowfallRate > 0f)
                {
                    int add = (int)(SnowfallRate * 14f);
                    int next = cell.SnowDepth01 + add;
                    if (next > 255)
                        next = 255;
                    if (next != cell.SnowDepth01)
                    {
                        cell.SnowDepth01 = (byte)next;
                        changed = true;
                    }
                }
                else if (cell.SnowDepth01 > 0 && _temperature > -0.05f)
                {
                    int sub = (int)((0.15f + Math.Max(0f, _temperature)) * (active.Kind == WeatherKind.Sunny ? 22f : 10f));
                    int next = cell.SnowDepth01 - sub;
                    if (next < 0)
                        next = 0;
                    if (next != cell.SnowDepth01)
                    {
                        cell.SnowDepth01 = (byte)next;
                        changed = true;
                    }
                }

                // Ice freeze / melt over water.
                bool isWater = def.Category == TerrainCategory.WaterRiver
                               || def.Category == TerrainCategory.WaterLake
                               || def.Category == TerrainCategory.WaterOcean;
                if (isWater && _temperature < -0.25f && cell.Ice == IceState.None)
                {
                    cell.Ice = _temperature < -0.55f ? IceState.Thick : IceState.Thin;
                    // Thin/thick ice as land-capable overlay is tracked on the cell; gameplay may query Ice.
                    changed = true;
                }
                else if (cell.Ice != IceState.None && _temperature > 0.1f)
                {
                    // Warm weather melts ice over time pulses.
                    if (_rng.NextFloat() < 0.2f + _temperature)
                    {
                        if (cell.Ice == IceState.Thick)
                            cell.Ice = IceState.Thin;
                        else if (cell.Ice == IceState.Thin)
                            cell.Ice = IceState.Broken;
                        else
                            cell.Ice = IceState.None;
                        changed = true;
                    }
                }

                if (changed)
                    _grid.SetCell(cx, cz, cell);
            }
        }

        private WeatherDefData BlendDefsVisual()
        {
            // During transition, precipitation/fog use the wetter/foggier of the two.
            return _targetDef.PrecipitationRate >= _currentDef.PrecipitationRate
                   || _targetDef.Kind == WeatherKind.Fog
                   || _targetDef.Kind == WeatherKind.Storm
                ? _targetDef
                : _currentDef;
        }

        private void RebuildState(float remaining)
        {
            float vis = _currentDef.VisibilityModifier;
            float move = _currentDef.MovementModifier;
            float sound = _currentDef.SoundModifier;
            if (_transitioning)
            {
                float t = _transitionDuration <= 0.001f ? 1f : Math.Min(1f, _transitionElapsed / _transitionDuration);
                float s = t * t * (3f - 2f * t);
                vis = Lerp(_currentDef.VisibilityModifier, _targetDef.VisibilityModifier, s);
                move = Lerp(_currentDef.MovementModifier, _targetDef.MovementModifier, s);
                sound = Lerp(_currentDef.SoundModifier, _targetDef.SoundModifier, s);
            }

            // Muddy cells globally nudge movement slightly when raining hard — cheap aggregate.
            if (PrecipitationRate > 0.6f)
                move *= 0.97f;

            Current = new WeatherState(
                _transitioning ? _targetDef.Kind : _currentDef.Kind,
                _transitioning ? _targetDef.Id : _currentDef.Id,
                _displayIntensity,
                _transitioning ? _targetDef.MaxDurationSeconds : (_currentDef.MaxDurationSeconds),
                _transitioning ? _transitionDuration : 0f,
                remaining,
                vis,
                move,
                sound);
        }

        private WeatherState BuildState(WeatherDefData def, float intensity, float duration, float transition, float remaining)
        {
            return new WeatherState(
                def.Kind,
                def.Id,
                intensity,
                duration,
                transition,
                remaining,
                def.VisibilityModifier,
                def.MovementModifier,
                def.SoundModifier);
        }

        private WeatherDefData PickNextDef(WeatherKind from)
        {
            // Authored transition graph (not a hard switch).
            switch (from)
            {
                case WeatherKind.Clear:
                    return Pick(WeatherKind.Sunny, WeatherKind.Cloudy);
                case WeatherKind.Sunny:
                    return Pick(WeatherKind.Clear, WeatherKind.Cloudy);
                case WeatherKind.Cloudy:
                    return Pick(WeatherKind.Rain, WeatherKind.Fog, WeatherKind.Snow, WeatherKind.Clear);
                case WeatherKind.Rain:
                    return Pick(WeatherKind.Storm, WeatherKind.Cloudy, WeatherKind.Rain);
                case WeatherKind.Storm:
                    return Pick(WeatherKind.Rain, WeatherKind.Cloudy);
                case WeatherKind.Snow:
                    return Pick(WeatherKind.Cloudy, WeatherKind.Clear, WeatherKind.Fog);
                case WeatherKind.Fog:
                    return Pick(WeatherKind.Cloudy, WeatherKind.Clear, WeatherKind.Rain);
                default:
                    return FindDef(WeatherKind.Clear) ?? _defs[0];
            }
        }

        private WeatherDefData Pick(params WeatherKind[] kinds)
        {
            // Prefer matching intensity variants (light vs heavy rain).
            var options = new List<WeatherDefData>(4);
            for (int k = 0; k < kinds.Length; k++)
            {
                for (int i = 0; i < _defs.Length; i++)
                {
                    if (_defs[i].Kind == kinds[k])
                        options.Add(_defs[i]);
                }
            }

            if (options.Count == 0)
                return _defs[0];
            return options[_rng.NextInt(0, options.Count)];
        }

        private WeatherDefData FindDef(WeatherKind kind)
        {
            for (int i = 0; i < _defs.Length; i++)
            {
                if (_defs[i].Kind == kind)
                    return _defs[i];
            }

            return null;
        }

        private void PickNewWindTarget()
        {
            float ang = _rng.NextFloat() * (float)(Math.PI * 2.0);
            _windDirX = (float)Math.Cos(ang);
            _windDirZ = (float)Math.Sin(ang);
            float baseWind = _currentDef.Kind == WeatherKind.Storm ? 0.7f
                : _currentDef.Kind == WeatherKind.Rain ? 0.45f
                : _currentDef.Kind == WeatherKind.Snow ? 0.35f
                : 0.2f;
            _windTargetIntensity = Clamp(baseWind + (_rng.NextFloat() - 0.5f) * 0.3f, 0.05f, 1.2f);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float MoveToward(float current, float target, float maxDelta)
        {
            if (current < target)
                return Math.Min(current + maxDelta, target);
            return Math.Max(current - maxDelta, target);
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }
    }

    /// <summary>Fixed-capacity footprint ring — presentation samples this; no per-print GameObjects.</summary>
    public sealed class SnowFootprintBuffer
    {
        private readonly float[] _x;
        private readonly float[] _z;
        private readonly byte[] _strength;
        private int _write;
        private int _count;

        public int Capacity { get; }
        public int Count => _count;

        public SnowFootprintBuffer(int capacity)
        {
            Capacity = capacity > 8 ? capacity : 8;
            _x = new float[Capacity];
            _z = new float[Capacity];
            _strength = new byte[Capacity];
        }

        public void Add(float x, float z, byte strength = 200)
        {
            _x[_write] = x;
            _z[_write] = z;
            _strength[_write] = strength;
            _write = (_write + 1) % Capacity;
            if (_count < Capacity)
                _count++;
        }

        public bool TryGet(int index, out float x, out float z, out byte strength)
        {
            if (index < 0 || index >= _count)
            {
                x = 0f;
                z = 0f;
                strength = 0;
                return false;
            }

            // Oldest first.
            int start = (_write - _count + Capacity) % Capacity;
            int i = (start + index) % Capacity;
            x = _x[i];
            z = _z[i];
            strength = _strength[i];
            return true;
        }
    }
}
