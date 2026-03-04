using UnityEngine;

[CreateAssetMenu(menuName = "Rooms/Room Noise Profile", fileName = "RoomNoiseProfile")]
public sealed class RoomNoiseProfile : ScriptableObject
{
    [Header("Сид и смещение")]
    [Tooltip("Сид для детерминированности. Один и тот же сид даёт одинаковый шум.")]
    [SerializeField] private int _seed = 1337;

    [Tooltip("Сдвиг координат шума. Удобно получать вариации без смены сида.")]
    [SerializeField] private Vector2 _offset;

    [Header("Базовый шум (Simplex fBm)")]
    [Tooltip("Частота базового шума.\nМеньше — крупнее пятна.\nБольше — мельче детали.")]
    [SerializeField, Min(0.0001f)] private float _frequency = 0.06f;

    [Tooltip("Количество октав (слоёв) фрактала.\nБольше — богаче детали, но дороже по CPU.")]
    [SerializeField, Range(1, 8)] private int _fractalOctaves = 4;

    [Tooltip("Затухание амплитуды по октавам (0..1).\nМеньше — быстрее затухают детали.")]
    [SerializeField, Range(0f, 1f)] private float _fractalGain = 0.5f;

    [Tooltip("Рост частоты по октавам.\nОбычно 2.0 — стандартный вариант.")]
    [SerializeField, Min(1f)] private float _fractalLacunarity = 2f;

    [Header("Domain Warp (органичность форм)")]
    [Tooltip("Включить доменное искажение.\nДелает формы менее регулярными, больше похоже на естественные кучи/пятна.")]
    [SerializeField] private bool _domainWarpEnabled = true;

    [Tooltip("Сила искажения.\nЧем больше — тем сильнее “ломает” координаты, тем органичнее формы.")]
    [SerializeField, Min(0f)] private float _domainWarpAmplitude = 6f;

    [Tooltip("Частота шума, который искажает координаты.\nМеньше — крупные изгибы, больше — мелкая рябь.")]
    [SerializeField, Min(0.0001f)] private float _domainWarpFrequency = 0.03f;

    [Tooltip("Октавность искажения.\nОбычно 1–3 достаточно.")]
    [SerializeField, Range(1, 8)] private int _domainWarpOctaves = 2;

    [Tooltip("Затухание амплитуды искажения по октавам (0..1).")]
    [SerializeField, Range(0f, 1f)] private float _domainWarpGain = 0.5f;

    [Tooltip("Рост частоты искажения по октавам.")]
    [SerializeField, Min(1f)] private float _domainWarpLacunarity = 2f;

    [Header("Полянка (центр пустой)")]
    [Tooltip("Включить органично пустую зону (поляну).\nЭто НЕ вырезанный круг: край дополнительно “рвётся” шумом.")]
    [SerializeField] private bool _clearingEnabled = false;

    [Tooltip("Центр поляны в нормализованных координатах (0..1) относительно карты шума.")]
    [SerializeField] private Vector2 _clearingCenter01 = new Vector2(0.5f, 0.5f);

    [Tooltip("Радиус поляны (0..1) относительно меньшей стороны карты.\n0.25 — четверть комнаты.")]
    [SerializeField, Range(0f, 1f)] private float _clearingRadius01 = 0.25f;

    [Tooltip("Ширина мягкого перехода края (0..1).\nЧем больше — тем плавнее край.")]
    [SerializeField, Range(0.001f, 1f)] private float _clearingFalloff01 = 0.18f;

    [Tooltip("Сила разрежения в центре (0..1).\n1 — в центре почти ноль.\n0.5 — в центре в 2 раза меньше.")]
    [SerializeField, Range(0f, 1f)] private float _clearingStrength = 1f;

    [Tooltip("Частота шума, который “рвёт” границу поляны.\nМеньше — крупные неровности.\nБольше — мелкая бахрома.")]
    [SerializeField, Min(0.0001f)] private float _clearingEdgeNoiseFrequency = 0.09f;

    [Tooltip("Амплитуда неровности края (0..1).\n0.2 — слегка живой край.\n0.4 — сильно неровно.")]
    [SerializeField, Range(0f, 1f)] private float _clearingEdgeNoiseAmplitude01 = 0.22f;

    [Header("Постобработка карты")]
    [Tooltip("Контраст карты.\n> 1 — сильнее разделение на “пусто/плотно”.\n< 1 — мягче переходы.")]
    [SerializeField, Range(0.1f, 4f)] private float _contrast = 1.2f;

    [Tooltip("Смещение карты перед контрастом.\nПоложительное — больше “плотных” зон.\nОтрицательное — больше “пустых” зон.")]
    [SerializeField, Range(-1f, 1f)] private float _bias = 0f;

    [Tooltip("Инвертировать карту (пики становятся долинами и наоборот).")]
    [SerializeField] private bool _invert = false;

    [Tooltip("Сглаживание кривой значений (soft threshold).\nДелает распределение более мягким.")]
    [SerializeField] private bool _applySmoothstep = true;

    [Header("Превью в инспекторе")]
    [Tooltip("Разрешение картинки предпросмотра в инспекторе.\nЧем больше — тем детальнее, но тяжелее обновлять.")]
    [SerializeField, Min(8)] private int _previewResolution = 64;

    [System.NonSerialized] private bool _hasRuntimeSeed = false;
    [System.NonSerialized] private int _runtimeSeed = 1;

    public int PreviewResolution => _previewResolution;

    public void SetRuntimeSeed(int seed)
    {
        _runtimeSeed = seed;
        _hasRuntimeSeed = true;
    }

    public void ClearRuntimeSeed()
    {
        _runtimeSeed = 1;
        _hasRuntimeSeed = false;
    }

    public float[,] GenerateNoiseMap(int width, int height)
    {
        int seed = _seed;

        if (_hasRuntimeSeed == true)
        {
            seed = _runtimeSeed;
        }

        SimplexNoise2D baseNoise = new SimplexNoise2D(seed);
        SimplexNoise2D warpNoiseX = new SimplexNoise2D(seed + 1);
        SimplexNoise2D warpNoiseY = new SimplexNoise2D(seed + 2);
        SimplexNoise2D clearingEdgeNoise = new SimplexNoise2D(seed + 3);

        float[,] values = new float[width, height];

        float frequency = _frequency;

        if (frequency < 0.0001f)
            frequency = 0.0001f;


        float domainWarpFrequency = _domainWarpFrequency;

        if (domainWarpFrequency < 0.0001f)
            domainWarpFrequency = 0.0001f;


        int fractalOctaves = _fractalOctaves;

        if (fractalOctaves < 1)
            fractalOctaves = 1;


        int domainWarpOctaves = _domainWarpOctaves;

        if (domainWarpOctaves < 1)
            domainWarpOctaves = 1;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float coordinateX = x + _offset.x;
                float coordinateY = y + _offset.y;

                if (_domainWarpEnabled == true)
                {
                    float warpCoordinateX = coordinateX * domainWarpFrequency;
                    float warpCoordinateY = coordinateY * domainWarpFrequency;

                    float warpValueX = GetFractalNoise(warpCoordinateX, warpCoordinateY, warpNoiseX, domainWarpOctaves, _domainWarpGain, _domainWarpLacunarity);
                    float warpValueY = GetFractalNoise(warpCoordinateX + 19.19f, warpCoordinateY + 7.31f, warpNoiseY, domainWarpOctaves, _domainWarpGain, _domainWarpLacunarity);

                    coordinateX += warpValueX * _domainWarpAmplitude;
                    coordinateY += warpValueY * _domainWarpAmplitude;
                }

                float baseCoordinateX = coordinateX * frequency;
                float baseCoordinateY = coordinateY * frequency;

                float fractalValue = GetFractalNoise(baseCoordinateX, baseCoordinateY, baseNoise, fractalOctaves, _fractalGain, _fractalLacunarity);

                float normalized = (fractalValue * 0.5f) + 0.5f;
                float processed = ApplyRemap(normalized);

                if (_clearingEnabled == true)
                    processed = ApplyClearing(processed, x, y, width, height, clearingEdgeNoise);


                values[x, y] = processed;
            }
        }

        return values;
    }

    private float ApplyClearing(float value, int x, int y, int width, int height, SimplexNoise2D edgeNoise)
    {
        float safeWidth = width;
        float safeHeight = height;

        if (safeWidth < 1f)
            safeWidth = 1f;

        if (safeHeight < 1f)
            safeHeight = 1f;


        float u = (x + 0.5f) / safeWidth;
        float v = (y + 0.5f) / safeHeight;

        float centerU = Mathf.Clamp01(_clearingCenter01.x);
        float centerV = Mathf.Clamp01(_clearingCenter01.y);

        float dx = u - centerU;
        float dy = v - centerV;

        float distance = Mathf.Sqrt((dx * dx) + (dy * dy));

        float radius = Mathf.Clamp01(_clearingRadius01);
        float falloff = Mathf.Max(0.001f, _clearingFalloff01);

        float edgeFrequency = _clearingEdgeNoiseFrequency;

        if (edgeFrequency < 0.0001f)
            edgeFrequency = 0.0001f;


        float edgeNoiseValue = edgeNoise.Evaluate(u * edgeFrequency, v * edgeFrequency);
        float edgeOffset = edgeNoiseValue * _clearingEdgeNoiseAmplitude01 * falloff;

        float distortedDistance = distance + edgeOffset;

        float t = Mathf.InverseLerp(radius, radius + falloff, distortedDistance);

        t = Mathf.Clamp01(t);

        if (_applySmoothstep == true)
            t = t * t * (3f - (2f * t));


        float strength = Mathf.Clamp01(_clearingStrength);

        float factor = Mathf.Lerp(1f - strength, 1f, t);

        float result = value * factor;

        return Mathf.Clamp01(result);
    }

    private float GetFractalNoise(float x, float y, SimplexNoise2D noise, int octaves, float gain, float lacunarity)
    {
        float amplitude = 1f;
        float frequency = 1f;

        float sum = 0f;
        float amplitudeSum = 0f;

        for (int octaveIndex = 0; octaveIndex < octaves; octaveIndex++)
        {
            float value = noise.Evaluate(x * frequency, y * frequency);

            sum += value * amplitude;
            amplitudeSum += amplitude;

            amplitude *= gain;
            frequency *= lacunarity;
        }

        if (amplitudeSum == 0f)
            return 0f;


        return sum / amplitudeSum;
    }

    private float ApplyRemap(float value)
    {
        float result = Mathf.Clamp01(value + _bias);
        result = Mathf.Clamp01((result - 0.5f) * _contrast + 0.5f);

        if (_applySmoothstep == true)
            result = result * result * (3f - (2f * result));


        if (_invert == true)
            result = 1f - result;


        return Mathf.Clamp01(result);
    }

    private sealed class SimplexNoise2D
    {
        private static readonly int[] GradientX = new int[8] { 1, -1, 1, -1, 1, -1, 0, 0 };
        private static readonly int[] GradientY = new int[8] { 1, 1, -1, -1, 0, 0, 1, -1 };

        private const float SkewFactor = 0.3660254037844386f;
        private const float UnskewFactor = 0.21132486540518713f;

        private readonly int[] _permutation;

        public SimplexNoise2D(int seed)
        {
            int[] source = new int[256];

            for (int index = 0; index < 256; index++)
            {
                source[index] = index;
            }

            System.Random random = new System.Random(seed);

            for (int index = 255; index >= 0; index--)
            {
                int swapIndex = random.Next(0, index + 1);

                int swapValue = source[swapIndex];
                source[swapIndex] = source[index];
                source[index] = swapValue;
            }

            _permutation = new int[512];

            for (int index = 0; index < 512; index++)
            {
                _permutation[index] = source[index & 255];
            }
        }

        public float Evaluate(float x, float y)
        {
            float skew = (x + y) * SkewFactor;

            int cellX = FastFloor(x + skew);
            int cellY = FastFloor(y + skew);

            float unskew = (cellX + cellY) * UnskewFactor;

            float cellOriginX = cellX - unskew;
            float cellOriginY = cellY - unskew;

            float localX0 = x - cellOriginX;
            float localY0 = y - cellOriginY;

            int offsetX1 = 0;
            int offsetY1 = 0;

            if (localX0 > localY0)
            {
                offsetX1 = 1;
                offsetY1 = 0;
            }
            else
            {
                offsetX1 = 0;
                offsetY1 = 1;
            }

            float localX1 = localX0 - offsetX1 + UnskewFactor;
            float localY1 = localY0 - offsetY1 + UnskewFactor;

            float localX2 = localX0 - 1f + (2f * UnskewFactor);
            float localY2 = localY0 - 1f + (2f * UnskewFactor);

            int maskX = cellX & 255;
            int maskY = cellY & 255;

            int gradientIndex0 = _permutation[maskX + _permutation[maskY]] & 7;
            int gradientIndex1 = _permutation[maskX + offsetX1 + _permutation[maskY + offsetY1]] & 7;
            int gradientIndex2 = _permutation[maskX + 1 + _permutation[maskY + 1]] & 7;

            float contribution0 = CalculateContribution(gradientIndex0, localX0, localY0);
            float contribution1 = CalculateContribution(gradientIndex1, localX1, localY1);
            float contribution2 = CalculateContribution(gradientIndex2, localX2, localY2);

            float value = 70f * (contribution0 + contribution1 + contribution2);

            return Mathf.Clamp(value, -1f, 1f);
        }

        private float CalculateContribution(int gradientIndex, float x, float y)
        {
            float t = 0.5f - (x * x) - (y * y);

            if (t <= 0f)
                return 0f;


            float dot = (GradientX[gradientIndex] * x) + (GradientY[gradientIndex] * y);

            float t2 = t * t;
            float t4 = t2 * t2;

            return t4 * dot;
        }

        private int FastFloor(float value)
        {
            int integerValue = (int)value;

            if (value < integerValue)
                return integerValue - 1;


            return integerValue;
        }
    }
}
