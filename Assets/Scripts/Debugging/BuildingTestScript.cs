using UnityEngine;
using CatchMeowIfYouCan.Environment;

/// <summary>
/// Simple script để test building spawning
/// </summary>
public class BuildingTestScript : MonoBehaviour 
{
    [Header("Manual Controls")]
    [SerializeField] private bool testBuildingSpawn = false;
    [SerializeField] private bool debugEverything = false;
    [SerializeField] private bool createTestGround = false;
    
    private void Update()
    {
        if (testBuildingSpawn)
        {
            testBuildingSpawn = false;
            TestBuildingSpawn();
        }
        
        if (debugEverything)
        {
            debugEverything = false;
            DebugEverything();
        }
        
        if (createTestGround)
        {
            createTestGround = false;
            CreateTestGround();
        }
    }
    
    [ContextMenu("Test Building Spawn")]
    public void TestBuildingSpawn()
    {
        Debug.Log("=== TESTING BUILDING SPAWN ===");
        
        var buildingSpawner = FindFirstObjectByType<BuildingSpawner>();
        if (buildingSpawner == null)
        {
            Debug.LogError("No BuildingSpawner found!");
            return;
        }
        
        // Force enable debug logs and spawning
        var field1 = typeof(BuildingSpawner).GetField("enableDebugLogs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field1 != null) field1.SetValue(buildingSpawner, true);
        
        var field2 = typeof(BuildingSpawner).GetField("enableSpawning", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field2 != null) field2.SetValue(buildingSpawner, true);
        
        
        Debug.Log("Building spawn test complete - check console for details");
    }
    
    [ContextMenu("Debug Everything")]
    public void DebugEverything()
    {
        Debug.Log("=== SYSTEM DEBUG ===");
        
        var buildingSpawner = FindFirstObjectByType<BuildingSpawner>();
        Debug.Log($"BuildingSpawner: {(buildingSpawner != null ? "✓" : "✗")}");
        
        var groundSpawner = FindFirstObjectByType<GroundSpawner>();
        Debug.Log($"GroundSpawner: {(groundSpawner != null ? "✓" : "✗")}");
        
        var fixedBg = FindFirstObjectByType<FixedBackgroundManager>();
        Debug.Log($"FixedBackgroundManager: {(fixedBg != null ? "✓" : "✗")}");
        
        GameObject[] grounds = GameObject.FindGameObjectsWithTag("Ground");
        Debug.Log($"Grounds with 'Ground' tag: {grounds.Length}");
        
        for (int i = 0; i < grounds.Length; i++)
        {
            GameObject ground = grounds[i];
            GroundMover mover = ground.GetComponent<GroundMover>();
            Debug.Log($"  [{i}] {ground.name}: Active={ground.activeInHierarchy}, GroundMover={mover != null}");
        }
        
        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
        Debug.Log($"Buildings with 'Building' tag: {buildings.Length}");
        
        if (buildings.Length == 0)
        {
            // Search by name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int buildingCount = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("building") || obj.name.ToLower().Contains("house"))
                {
                    buildingCount++;
                    Debug.Log($"  Found building by name: {obj.name} (active: {obj.activeInHierarchy})");
                }
            }
            Debug.Log($"Total buildings found by name: {buildingCount}");
        }
    }
    
    [ContextMenu("Create Test Ground")]
    public void CreateTestGround()
    {
        Debug.Log("=== CREATING TEST GROUND ===");
        
        GameObject testGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testGround.name = "TestGround_Manual";
        testGround.tag = "Ground";
        testGround.transform.position = Vector3.zero;
        testGround.transform.localScale = new Vector3(10, 1, 5);
        
        // Add GroundMover
        GroundMover mover = testGround.AddComponent<GroundMover>();
        var groundSpawner = FindFirstObjectByType<GroundSpawner>();
        if (groundSpawner != null)
        {
            mover.Initialize(groundSpawner);
        }
        
        Debug.Log($"Created test ground: {testGround.name}");
        
        // Try to spawn buildings on it
        var buildingSpawner = FindFirstObjectByType<BuildingSpawner>();
        if (buildingSpawner != null)
        {
            Debug.Log("Attempting to spawn buildings on test ground...");
            // Force enable spawning
            var field = typeof(BuildingSpawner).GetField("enableSpawning", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(buildingSpawner, true);
            
            // Force spawn
            var method = typeof(BuildingSpawner).GetMethod("SpawnBuildingsOnGround", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(buildingSpawner, new object[] { testGround });
            }
        }
    }
}