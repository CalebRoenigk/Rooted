using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AccidentalNoise;

public class WorldGenerator : MonoBehaviour
{
    public List<Chunk> Chunks = new List<Chunk>();
    
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int chunkGenerationRadius = 6;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int seed = 100;

    [Header("Rock")]
    [SerializeField] private int rockOctaves = 6;
    [SerializeField] private float rockFrequency = 1f;
    [SerializeField] private RockSpawnThreshold rockSpawnThreshold;
    [SerializeField] private RockAssets rockAssets;
    [SerializeField] private Transform rockParent;
    
    private ImplicitFractal _rockNoise;

    [Header("Dirt")]
    [Range(0f, 1f)]
    [SerializeField] private float dirtThreshold = 0.5f;
    [SerializeField] private Tilemap dirtTilemap;
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private int dirtOctaves = 3;
    [SerializeField] private float dirtFrequency = 1f;

    private ImplicitFractal _dirtNoise;
    
    void Start()
    {
        // Initialize the rock noise
        _rockNoise = new ImplicitFractal (FractalType.MULTI, 
            BasisType.SIMPLEX, 
            InterpolationType.QUINTIC, 
            rockOctaves, 
            rockFrequency, 
            seed);
        
        // Initialize the dirt noise
        _dirtNoise = new ImplicitFractal (FractalType.MULTI, 
            BasisType.SIMPLEX, 
            InterpolationType.QUINTIC, 
            dirtOctaves, 
            dirtFrequency, 
            seed);
        
        // Set the random seed
        UnityEngine.Random.InitState(seed);
    }
    
    void Update()
    {
        GenerateChunks();
    }

    private void OnDrawGizmos()
    {
        // Draw each chunk border
        foreach (Chunk chunk in Chunks)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(chunk.Bounds.center,  chunk.Bounds.size);
        }
    }
    
    // Generates chunks from the player transform position
    private void GenerateChunks()
    {
        // Get the current chunk index of the player
        Vector2Int playerChunkIndex = WorldToChunkIndex(playerTransform.position);

        for (int x = playerChunkIndex.x - chunkGenerationRadius; x < playerChunkIndex.x + chunkGenerationRadius + 1; x++)
        {
            for (int y = playerChunkIndex.y - chunkGenerationRadius; y < playerChunkIndex.y + chunkGenerationRadius + 1; y++)
            {
                // Check if the chunk is within the radius of generation
                Vector2Int chunkIndex = new Vector2Int(x, y);
                Vector3 chunkCenter = new Vector3(chunkIndex.x * chunkSize, chunkIndex.y * chunkSize, 0) + ((Vector3.one * chunkSize) / 2f);
                if (Vector3.Distance(playerTransform.position, (Vector3)((Vector2)chunkIndex * chunkSize)) <= chunkGenerationRadius * chunkSize) 
                {
                    // Check if the current chunk index has already been generated
                    if (Chunks.FindIndex(c => c.Index == chunkIndex) == -1)
                    {
                        // The chunk has not been generated
                        // Generate the chunk
                        GenerateChunk(chunkIndex);
                    }
                }
            }
        }
    }
    
    // Converts a world position to a chunk index
    public Vector2Int WorldToChunkIndex(Vector3 position)
    {
        return new Vector2Int((int)Mathf.Floor(position.x / (float)chunkSize), (int)Mathf.Floor(position.y / (float)chunkSize));
    }
    
    // Generates a chunk
    private void GenerateChunk(Vector2Int index)
    {
        // Create the chunk
        Chunk chunk = new Chunk(index, chunkSize);
        
        // Check if a rock can be generated in the chunk
        if (chunk.Index.y <= -2 && CanGenerateRock(chunk.Index))
        {
            // Get a random position within the chunk
            Vector3Int rockPosition = new Vector3Int(UnityEngine.Random.Range(1, chunkSize - 2), UnityEngine.Random.Range(1, chunkSize - 2), 0) + chunk.Bounds.min;
            
            // Generate rock
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            Instantiate(rockAssets.GetRandomRock(), rockPosition + Vector3.one / 2f, rotation, rockParent);
        }
        
        // Iterate over each tile and place it in the scene
        foreach (Vector3Int tile in chunk.Bounds.allPositionsWithin)
        {
            // Generate the dirt if the y is less than or equal to zero
            if (tile.y <= 0)
            {
                // Sample the dirt noise to see if dirt should be placed here
                if (CanGenerateDirt(new Vector2Int(tile.x, tile.y)))
                {
                    dirtTilemap.SetTile(tile, dirtTile);
                }
            }
        }
        
        // Add the chunk to the list
        Chunks.Add(chunk);
    }
    
    // Returns true if a rock should be generated in the chunk
    private bool CanGenerateRock(Vector2Int position)
    {
        float value = Mathf.Clamp(RemapFloat((float)_rockNoise.Get(position.x, position.y), -1f, 1f, 0f, 1f), 0f, 1f);
        // Only spawn a rock if dirt is at the position of the rock
        if(CanGenerateDirt(new Vector2Int(position.x, position.y)))
        {
            return value > 1f - rockSpawnThreshold.GetThreshold(position.y);
        }
        return false;
    }
    
    // Returns true if dirt can be generated at the position
    private bool CanGenerateDirt(Vector2Int position)
    {
        float value = Mathf.Clamp(RemapFloat((float)_dirtNoise.Get(position.x, position.y), -1f, 1f, 0f, 1f), 0f, 1f);
        return value > 1f - dirtThreshold;
    }
    
    
    // Remaps a float to a new range
    public float RemapFloat(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    [Serializable]
    private class RockSpawnThreshold
    {
        public int MinDepth;
        public float MinChance;
        public int MaxDepth;
        public float MaxChance;

        public RockSpawnThreshold(int minDepth, float minChance, int maxDepth, float maxChance)
        {
            this.MinDepth = minDepth;
            this.MinChance = minChance;
            this.MaxDepth = maxDepth;
            this.MaxChance = maxChance;
        }
        
        // Returns the spawn threshold based on the depth
        public float GetThreshold(int depth)
        {
            return Mathf.Clamp(RemapFloat((float)depth, (float)MinDepth, (float)MaxDepth, MinChance, MaxChance), MinChance, MaxChance);
        }
        
        // Remaps a float
        private float RemapFloat(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
