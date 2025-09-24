using UnityEngine;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Handles parallax scrolling background with multiple layers
    /// Supports infinite scrolling, variable speeds, and dynamic background changes
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("Scrolling Configuration")]
        [SerializeField] private float baseScrollSpeed = 5f;
        [SerializeField] private bool useGameSpeed = true;
        [SerializeField] private float gameSpeedMultiplier = 1f;
        
        [Header("Background Layers")]
        [SerializeField] private BackgroundLayer[] backgroundLayers;
        
        [Header("Sky Configuration")]
        [SerializeField] private SkySettings skySettings;
        [SerializeField] private bool enableDynamicSky = true;
        [SerializeField] private float dayNightCycleDuration = 300f; // 5 minutes
        
        [Header("Weather Effects")]
        [SerializeField] private bool enableWeatherEffects = true;
        [SerializeField] private ParticleSystem[] weatherParticles;
        [SerializeField] private float weatherChangeInterval = 120f; // 2 minutes
        
        // Runtime state
        private float currentScrollSpeed = 0f;
        private float timeSinceStart = 0f;
        private float weatherTimer = 0f;
        private WeatherType currentWeather = WeatherType.Clear;
        
        // Layer management
        private List<ScrollingLayer> activeLayers = new List<ScrollingLayer>();
        
        // Game integration
        private Core.GameManager gameManager;
        
        public enum WeatherType
        {
            Clear,
            Cloudy,
            Rain,
            Snow
        }
        
        [System.Serializable]
        public class BackgroundLayer
        {
            [Header("Layer Settings")]
            public string layerName = "Background Layer";
            public GameObject[] backgroundPrefabs;
            public float parallaxSpeed = 0.5f;
            public float verticalOffset = 0f;
            public int layerCount = 2;
            
            [Header("Visual Settings")]
            public bool fadeWithDistance = true;
            public Color tintColor = Color.white;
            public float brightnessMultiplier = 1f;
            
            [Header("Advanced")]
            public bool enableRandomOffset = false;
            public float randomOffsetRange = 2f;
            public bool useAlternatingSprites = false;
        }
        
        [System.Serializable]
        public class SkySettings
        {
            [Header("Day/Night Colors")]
            public Gradient skyGradient = new Gradient();
            public Color sunColor = new Color(1f, 1f, 0.8f);
            public Color moonColor = new Color(0.8f, 0.8f, 1f);
            
            [Header("Sky Objects")]
            public Transform sun;
            public Transform moon;
            public Transform[] stars;
            
            [Header("Fog Settings")]
            public bool enableFog = true;
            public Color fogColor = Color.white;
            public float fogDensity = 0.01f;
        }
        
        private class ScrollingLayer
        {
            public List<GameObject> segments = new List<GameObject>();
            public BackgroundLayer config;
            public float segmentWidth;
            public int currentSegmentIndex = 0;
            
            public ScrollingLayer(BackgroundLayer layerConfig)
            {
                config = layerConfig;
            }
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeLayers();
        }
        
        private void Start()
        {
            gameManager = Core.GameManager.Instance;
            currentScrollSpeed = baseScrollSpeed;
            
            if (enableDynamicSky)
            {
                InitializeSky();
            }
            
            if (enableWeatherEffects)
            {
                InitializeWeather();
            }
        }
        
        private void Update()
        {
            UpdateScrollSpeed();
            UpdateBackgroundScrolling();
            
            if (enableDynamicSky)
            {
                UpdateSkySystem();
            }
            
            if (enableWeatherEffects)
            {
                UpdateWeatherSystem();
            }
            
            timeSinceStart += Time.deltaTime;
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeLayers()
        {
            foreach (BackgroundLayer layer in backgroundLayers)
            {
                if (layer.backgroundPrefabs != null && layer.backgroundPrefabs.Length > 0)
                {
                    ScrollingLayer scrollingLayer = new ScrollingLayer(layer);
                    CreateLayerSegments(scrollingLayer);
                    activeLayers.Add(scrollingLayer);
                }
            }
        }
        
        private void CreateLayerSegments(ScrollingLayer layer)
        {
            GameObject samplePrefab = layer.config.backgroundPrefabs[0];
            Renderer renderer = samplePrefab.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                layer.segmentWidth = renderer.bounds.size.x;
            }
            else
            {
                layer.segmentWidth = 10f; // Default width
            }
            
            // Create initial segments
            for (int i = 0; i < layer.config.layerCount; i++)
            {
                CreateSegment(layer, i);
            }
        }
        
        private void CreateSegment(ScrollingLayer layer, int index)
        {
            GameObject prefab = GetRandomPrefab(layer);
            Vector3 position = new Vector3(
                index * layer.segmentWidth,
                layer.config.verticalOffset,
                transform.position.z + activeLayers.Count * 0.1f
            );
            
            // Add random offset if enabled
            if (layer.config.enableRandomOffset)
            {
                position.y += Random.Range(-layer.config.randomOffsetRange, layer.config.randomOffsetRange);
            }
            
            GameObject segment = Instantiate(prefab, position, Quaternion.identity, transform);
            ApplyLayerVisualSettings(segment, layer.config);
            
            layer.segments.Add(segment);
        }
        
        private GameObject GetRandomPrefab(ScrollingLayer layer)
        {
            if (layer.config.useAlternatingSprites && layer.config.backgroundPrefabs.Length >= 2)
            {
                // Alternate between first two prefabs
                return layer.config.backgroundPrefabs[layer.segments.Count % 2];
            }
            
            return layer.config.backgroundPrefabs[Random.Range(0, layer.config.backgroundPrefabs.Length)];
        }
        
        private void ApplyLayerVisualSettings(GameObject segment, BackgroundLayer layer)
        {
            Renderer renderer = segment.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = layer.tintColor;
                
                if (layer.fadeWithDistance)
                {
                    Color color = renderer.material.color;
                    color.a *= (1f - layer.parallaxSpeed) * 0.5f + 0.5f;
                    renderer.material.color = color;
                }
            }
        }
        
        #endregion
        
        #region Scrolling System
        
        private void UpdateScrollSpeed()
        {
            if (useGameSpeed && gameManager != null)
            {
                float gameSpeed = gameManager.CurrentGameSpeed;
                currentScrollSpeed = baseScrollSpeed * gameSpeed * gameSpeedMultiplier;
            }
            else
            {
                currentScrollSpeed = baseScrollSpeed * gameSpeedMultiplier;
            }
        }
        
        private void UpdateBackgroundScrolling()
        {
            foreach (ScrollingLayer layer in activeLayers)
            {
                float layerSpeed = currentScrollSpeed * layer.config.parallaxSpeed;
                ScrollLayer(layer, layerSpeed);
            }
        }
        
        private void ScrollLayer(ScrollingLayer layer, float speed)
        {
            for (int i = 0; i < layer.segments.Count; i++)
            {
                GameObject segment = layer.segments[i];
                if (segment != null)
                {
                    segment.transform.Translate(Vector3.left * speed * Time.deltaTime);
                    
                    // Check if segment needs to be recycled
                    if (segment.transform.position.x < -layer.segmentWidth)
                    {
                        RecycleSegment(layer, i);
                    }
                }
            }
        }
        
        private void RecycleSegment(ScrollingLayer layer, int segmentIndex)
        {
            GameObject segment = layer.segments[segmentIndex];
            
            // Find rightmost segment position
            float rightmostX = GetRightmostSegmentPosition(layer);
            
            // Move segment to the right
            Vector3 newPosition = new Vector3(
                rightmostX + layer.segmentWidth,
                layer.config.verticalOffset,
                segment.transform.position.z
            );
            
            // Add random offset if enabled
            if (layer.config.enableRandomOffset)
            {
                newPosition.y += Random.Range(-layer.config.randomOffsetRange, layer.config.randomOffsetRange);
            }
            
            segment.transform.position = newPosition;
            
            // Optionally change the sprite for variety
            if (layer.config.backgroundPrefabs.Length > 1)
            {
                ChangeSegmentSprite(segment, layer);
            }
        }
        
        private float GetRightmostSegmentPosition(ScrollingLayer layer)
        {
            float rightmostX = float.MinValue;
            
            foreach (GameObject segment in layer.segments)
            {
                if (segment != null && segment.transform.position.x > rightmostX)
                {
                    rightmostX = segment.transform.position.x;
                }
            }
            
            return rightmostX;
        }
        
        private void ChangeSegmentSprite(GameObject segment, ScrollingLayer layer)
        {
            SpriteRenderer spriteRenderer = segment.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                GameObject newPrefab = GetRandomPrefab(layer);
                SpriteRenderer newSpriteRenderer = newPrefab.GetComponent<SpriteRenderer>();
                
                if (newSpriteRenderer != null)
                {
                    spriteRenderer.sprite = newSpriteRenderer.sprite;
                }
            }
        }
        
        #endregion
        
        #region Sky System
        
        private void InitializeSky()
        {
            if (skySettings.enableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = skySettings.fogColor;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = skySettings.fogDensity;
            }
        }
        
        private void UpdateSkySystem()
        {
            float dayProgress = (timeSinceStart % dayNightCycleDuration) / dayNightCycleDuration;
            
            // Update sky color
            UpdateSkyColor(dayProgress);
            
            // Update sun and moon positions
            UpdateCelestialBodies(dayProgress);
            
            // Update stars visibility
            UpdateStars(dayProgress);
        }
        
        private void UpdateSkyColor(float dayProgress)
        {
            if (Camera.main != null)
            {
                Color skyColor = skySettings.skyGradient.Evaluate(dayProgress);
                Camera.main.backgroundColor = skyColor;
                
                // Update fog color to match sky
                if (skySettings.enableFog)
                {
                    RenderSettings.fogColor = Color.Lerp(skyColor, skySettings.fogColor, 0.5f);
                }
            }
        }
        
        private void UpdateCelestialBodies(float dayProgress)
        {
            // Sun movement (visible during day: 0.25 to 0.75)
            if (skySettings.sun != null)
            {
                float sunAlpha = Mathf.Clamp01(1f - Mathf.Abs(dayProgress - 0.5f) * 4f);
                Vector3 sunPosition = GetCelestialPosition(dayProgress);
                skySettings.sun.position = sunPosition;
                
                SpriteRenderer sunRenderer = skySettings.sun.GetComponent<SpriteRenderer>();
                if (sunRenderer != null)
                {
                    Color sunColor = skySettings.sunColor;
                    sunColor.a = sunAlpha;
                    sunRenderer.color = sunColor;
                }
            }
            
            // Moon movement (visible during night: 0.75 to 1.0 and 0.0 to 0.25)
            if (skySettings.moon != null)
            {
                float moonAlpha = Mathf.Clamp01(Mathf.Abs(dayProgress - 0.5f) * 4f - 1f);
                Vector3 moonPosition = GetCelestialPosition(dayProgress + 0.5f);
                skySettings.moon.position = moonPosition;
                
                SpriteRenderer moonRenderer = skySettings.moon.GetComponent<SpriteRenderer>();
                if (moonRenderer != null)
                {
                    Color moonColor = skySettings.moonColor;
                    moonColor.a = moonAlpha;
                    moonRenderer.color = moonColor;
                }
            }
        }
        
        private Vector3 GetCelestialPosition(float progress)
        {
            float angle = progress * Mathf.PI * 2f - Mathf.PI * 0.5f; // Start from bottom
            float radius = 15f;
            
            return new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius + 5f,
                10f
            );
        }
        
        private void UpdateStars(float dayProgress)
        {
            if (skySettings.stars == null) return;
            
            // Stars are visible during night
            float starAlpha = Mathf.Clamp01(Mathf.Abs(dayProgress - 0.5f) * 2f - 0.5f);
            
            foreach (Transform star in skySettings.stars)
            {
                if (star != null)
                {
                    SpriteRenderer starRenderer = star.GetComponent<SpriteRenderer>();
                    if (starRenderer != null)
                    {
                        Color starColor = starRenderer.color;
                        starColor.a = starAlpha * (0.5f + Random.value * 0.5f); // Twinkling effect
                        starRenderer.color = starColor;
                    }
                }
            }
        }
        
        #endregion
        
        #region Weather System
        
        private void InitializeWeather()
        {
            currentWeather = WeatherType.Clear;
            weatherTimer = 0f;
            
            // Initialize all weather particles as inactive
            if (weatherParticles != null)
            {
                foreach (ParticleSystem particles in weatherParticles)
                {
                    if (particles != null)
                    {
                        particles.Stop();
                    }
                }
            }
        }
        
        private void UpdateWeatherSystem()
        {
            weatherTimer += Time.deltaTime;
            
            if (weatherTimer >= weatherChangeInterval)
            {
                ChangeWeather();
                weatherTimer = 0f;
            }
        }
        
        private void ChangeWeather()
        {
            WeatherType[] weatherTypes = System.Enum.GetValues(typeof(WeatherType)) as WeatherType[];
            WeatherType newWeather = weatherTypes[Random.Range(0, weatherTypes.Length)];
            
            // Avoid same weather twice in a row
            if (newWeather == currentWeather)
            {
                newWeather = weatherTypes[(System.Array.IndexOf(weatherTypes, currentWeather) + 1) % weatherTypes.Length];
            }
            
            SetWeather(newWeather);
        }
        
        private void SetWeather(WeatherType weather)
        {
            currentWeather = weather;
            
            // Stop all weather effects
            if (weatherParticles != null)
            {
                foreach (ParticleSystem particles in weatherParticles)
                {
                    if (particles != null)
                    {
                        particles.Stop();
                    }
                }
            }
            
            // Start appropriate weather effect
            switch (weather)
            {
                case WeatherType.Rain:
                    StartWeatherEffect(0); // Assuming index 0 is rain
                    break;
                    
                case WeatherType.Snow:
                    StartWeatherEffect(1); // Assuming index 1 is snow
                    break;
                    
                case WeatherType.Cloudy:
                    // No particles, just atmospheric changes
                    UpdateAtmosphere(weather);
                    break;
                    
                case WeatherType.Clear:
                default:
                    UpdateAtmosphere(weather);
                    break;
            }
        }
        
        private void StartWeatherEffect(int particleIndex)
        {
            if (weatherParticles != null && particleIndex < weatherParticles.Length)
            {
                ParticleSystem particles = weatherParticles[particleIndex];
                if (particles != null)
                {
                    particles.Play();
                }
            }
        }
        
        private void UpdateAtmosphere(WeatherType weather)
        {
            // Adjust fog and lighting based on weather
            switch (weather)
            {
                case WeatherType.Cloudy:
                    if (skySettings.enableFog)
                    {
                        RenderSettings.fogDensity = skySettings.fogDensity * 1.5f;
                        RenderSettings.fogColor = Color.Lerp(skySettings.fogColor, Color.gray, 0.3f);
                    }
                    break;
                    
                case WeatherType.Rain:
                    if (skySettings.enableFog)
                    {
                        RenderSettings.fogDensity = skySettings.fogDensity * 2f;
                        RenderSettings.fogColor = Color.Lerp(skySettings.fogColor, Color.blue, 0.2f);
                    }
                    break;
                    
                case WeatherType.Snow:
                    if (skySettings.enableFog)
                    {
                        RenderSettings.fogDensity = skySettings.fogDensity * 1.2f;
                        RenderSettings.fogColor = Color.Lerp(skySettings.fogColor, Color.white, 0.5f);
                    }
                    break;
                    
                default:
                    // Clear weather - reset to default
                    if (skySettings.enableFog)
                    {
                        RenderSettings.fogDensity = skySettings.fogDensity;
                        RenderSettings.fogColor = skySettings.fogColor;
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Set the scroll speed multiplier
        /// </summary>
        public void SetScrollSpeedMultiplier(float multiplier)
        {
            gameSpeedMultiplier = multiplier;
        }
        
        /// <summary>
        /// Force a specific weather type
        /// </summary>
        public void ForceWeather(WeatherType weather)
        {
            SetWeather(weather);
            weatherTimer = 0f; // Reset timer
        }
        
        /// <summary>
        /// Get current weather type
        /// </summary>
        public WeatherType GetCurrentWeather()
        {
            return currentWeather;
        }
        
        /// <summary>
        /// Set time of day (0 = midnight, 0.5 = noon)
        /// </summary>
        public void SetTimeOfDay(float timeOfDay)
        {
            timeSinceStart = timeOfDay * dayNightCycleDuration;
        }
        
        /// <summary>
        /// Get current time of day (0-1)
        /// </summary>
        public float GetTimeOfDay()
        {
            return (timeSinceStart % dayNightCycleDuration) / dayNightCycleDuration;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            // Draw layer information
            if (activeLayers != null)
            {
                for (int i = 0; i < activeLayers.Count; i++)
                {
                    ScrollingLayer layer = activeLayers[i];
                    
                    Gizmos.color = Color.HSVToRGB((float)i / activeLayers.Count, 0.7f, 1f);
                    
                    foreach (GameObject segment in layer.segments)
                    {
                        if (segment != null)
                        {
                            Gizmos.DrawWireCube(segment.transform.position, Vector3.one * 0.5f);
                        }
                    }
                }
            }
            
            // Draw weather info
            if (Application.isPlaying)
            {
                Gizmos.color = GetWeatherColor(currentWeather);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 1f);
            }
        }
        
        private Color GetWeatherColor(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return Color.yellow;
                case WeatherType.Cloudy: return Color.gray;
                case WeatherType.Rain: return Color.blue;
                case WeatherType.Snow: return Color.white;
                default: return Color.white;
            }
        }
        
        public string GetDebugInfo()
        {
            return $"Background Scroller Debug\n" +
                   $"Scroll Speed: {currentScrollSpeed:F2}\n" +
                   $"Active Layers: {activeLayers.Count}\n" +
                   $"Weather: {currentWeather}\n" +
                   $"Time of Day: {GetTimeOfDay():F2}\n" +
                   $"Weather Timer: {weatherTimer:F1}s";
        }
        
        #endregion
    }
}