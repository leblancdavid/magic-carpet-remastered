using Godot;

namespace MagicCarpetRemastered.Scripts.Audio;

public partial class GameAudio : Node
{
    public static GameAudio? Instance { get; private set; }

    private const int MixRate = 22050;

    private AudioStreamPlayer _flightPlayer = null!;
    private AudioStreamGeneratorPlayback? _flightPlayback;
    private float _flightPhase;
    private float _flightVolume;
    private float _flightIntensity;

    public override void _Ready()
    {
        Instance = this;

        _flightPlayer = new AudioStreamPlayer
        {
            Name = "FlightLoop",
            Stream = new AudioStreamGenerator
            {
                MixRate = MixRate,
                BufferLength = 0.2f
            }
        };
        AddChild(_flightPlayer);
        _flightPlayer.Play();
        _flightPlayback = _flightPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Process(double delta)
    {
        FillFlightBuffer((float)delta);
    }

    public void SetFlightIntensity(float intensity)
    {
        _flightIntensity = Mathf.Clamp(intensity, 0.0f, 1.0f);
    }

    public void PlaySpellCast()
    {
        AddChild(new ToneEmitter(280.0f, 740.0f, 0.12f, 0.16f, Waveform.Saw));
    }

    public void PlayPickup()
    {
        AddChild(new ToneEmitter(520.0f, 1040.0f, 0.16f, 0.14f, Waveform.Sine));
    }

    public void PlayHit()
    {
        AddChild(new ToneEmitter(180.0f, 70.0f, 0.22f, 0.2f, Waveform.Square));
    }

    private void FillFlightBuffer(float delta)
    {
        if (_flightPlayback == null)
        {
            return;
        }

        _flightVolume = Mathf.Lerp(_flightVolume, Mathf.Lerp(0.025f, 0.11f, _flightIntensity), 4.0f * delta);
        float frequency = Mathf.Lerp(75.0f, 145.0f, _flightIntensity);
        int frames = _flightPlayback.GetFramesAvailable();

        for (int i = 0; i < frames; i++)
        {
            float low = Mathf.Sin(_flightPhase) * _flightVolume;
            float shimmer = Mathf.Sin(_flightPhase * 2.7f) * _flightVolume * 0.22f;
            float sample = low + shimmer;
            _flightPlayback.PushFrame(new Vector2(sample, sample));
            _flightPhase = Mathf.Wrap(_flightPhase + Mathf.Tau * frequency / MixRate, 0.0f, Mathf.Tau);
        }
    }

    private enum Waveform
    {
        Sine,
        Square,
        Saw
    }

    private partial class ToneEmitter : Node
    {
        private readonly float _startFrequency;
        private readonly float _endFrequency;
        private readonly float _durationSeconds;
        private readonly float _volume;
        private readonly Waveform _waveform;

        private AudioStreamGeneratorPlayback? _playback;
        private float _age;
        private float _phase;
        private int _samplesGenerated;

        public ToneEmitter(float startFrequency, float endFrequency, float durationSeconds, float volume, Waveform waveform)
        {
            _startFrequency = startFrequency;
            _endFrequency = endFrequency;
            _durationSeconds = durationSeconds;
            _volume = volume;
            _waveform = waveform;
        }

        public override void _Ready()
        {
            var player = new AudioStreamPlayer
            {
                Stream = new AudioStreamGenerator
                {
                    MixRate = MixRate,
                    BufferLength = 0.08f
                }
            };
            AddChild(player);
            player.Play();
            _playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            FillBuffer();

            if (_age >= _durationSeconds + 0.15f)
            {
                QueueFree();
            }
        }

        private void FillBuffer()
        {
            if (_playback == null)
            {
                return;
            }

            int totalSamples = Mathf.CeilToInt(_durationSeconds * MixRate);
            int frames = _playback.GetFramesAvailable();

            for (int i = 0; i < frames; i++)
            {
                if (_samplesGenerated >= totalSamples)
                {
                    _playback.PushFrame(Vector2.Zero);
                    continue;
                }

                float t = (float)_samplesGenerated / totalSamples;
                float frequency = Mathf.Lerp(_startFrequency, _endFrequency, t);
                float envelope = Mathf.Sin(Mathf.Pi * t);
                float sample = WaveSample() * _volume * envelope;
                _playback.PushFrame(new Vector2(sample, sample));
                _phase = Mathf.Wrap(_phase + Mathf.Tau * frequency / MixRate, 0.0f, Mathf.Tau);
                _samplesGenerated++;
            }
        }

        private float WaveSample()
        {
            return _waveform switch
            {
                Waveform.Square => Mathf.Sin(_phase) >= 0.0f ? 1.0f : -1.0f,
                Waveform.Saw => _phase / Mathf.Pi - 1.0f,
                _ => Mathf.Sin(_phase)
            };
        }
    }
}
