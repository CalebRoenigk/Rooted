using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AccidentalNoise;
using Random = UnityEngine.Random;

namespace World
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Chunk")] 
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
        [Range(0f, 1f)] [SerializeField] private float dirtThreshold = 0.5f;
        [SerializeField] private Tilemap dirtTilemap;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private int dirtOctaves = 3;
        [SerializeField] private float dirtFrequency = 1f;

        private ImplicitFractal _dirtNoise;

        [Header("Gravel")]
        [Range(0f, 1f)] [SerializeField] private float gravelThreshold = 0.5f;
        [SerializeField] private Tilemap gravelTilemap;
        [SerializeField] private TileBase gravelTile;
        [SerializeField] private int gravelOctaves = 3;
        [SerializeField] private float gravelFrequency = 1f;

        private ImplicitFractal _gravelNoise;

        [Header("RichSoil")]
        [Range(0f, 1f)] [SerializeField] private float richSoilThreshold = 0.5f;
        [SerializeField] private Tilemap richSoilTilemap;
        [SerializeField] private TileBase richSoilTile;
        [SerializeField] private int richSoilOctaves = 3;
        [SerializeField] private float richSoilFrequency = 1f;

        private ImplicitFractal _richSoilNoise;
        
        [Header("Air")]
        public List<List<Vector3Int>> AirRegions = new List<List<Vector3Int>>();

        [Header("Water")]
        [SerializeField] private Tilemap waterTilemap;
        [SerializeField] private TileBase waterTile;
        [Range(0, 10)] [SerializeField] private int minWaterLevel = 0;
        [Range(0, 10)] [SerializeField] private int maxWaterLevel = 4;

        [Header("Debug")]
        [SerializeField] private bool showChunks = true;
        [SerializeField] private bool showAir = true;

        // TODO: Need a way to store and simulate water, need a way to store closed cavern regions

        void Start()
        {
            // Initialize the rock noise
            _rockNoise = new ImplicitFractal(FractalType.MULTI,
                BasisType.SIMPLEX,
                InterpolationType.QUINTIC,
                rockOctaves,
                rockFrequency,
                seed + 10);

            // Initialize the dirt noise
            _dirtNoise = new ImplicitFractal(FractalType.MULTI,
                BasisType.SIMPLEX,
                InterpolationType.QUINTIC,
                dirtOctaves,
                dirtFrequency,
                seed);

            // Initialize the gravel noise
            _gravelNoise = new ImplicitFractal(FractalType.MULTI,
                BasisType.SIMPLEX,
                InterpolationType.QUINTIC,
                gravelOctaves,
                gravelFrequency,
                seed + 100);

            // Initialize the rich soil noise
            _richSoilNoise = new ImplicitFractal(FractalType.MULTI,
                BasisType.SIMPLEX,
                InterpolationType.QUINTIC,
                richSoilOctaves,
                richSoilFrequency,
                seed - 100);

            // Set the random seed
            Random.InitState(seed);
        }

        void Update()
        {
            GenerateChunks();
        }

        private void OnDrawGizmos()
        {
            // Draw each chunk border
            if (showChunks)
            {
                foreach (Chunk chunk in Chunks)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(chunk.Bounds.center, chunk.Bounds.size);
                }
            }

            // Draw each air 
            if (showAir)
            {
                foreach (List<Vector3Int> airRegion in AirRegions)
                {
                    foreach (Vector3Int airTile in airRegion)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube((Vector3)(airTile) + (Vector3.one / 2f), Vector3.one);
                    }
                }
            }
        }

        #region Chunks

        // Generates chunks from the player transform position
        private void GenerateChunks()
        {
            // Get the current chunk index of the player
            Vector2Int playerChunkIndex = WorldToChunkIndex(playerTransform.position);

            for (int x = playerChunkIndex.x - chunkGenerationRadius;
                 x < playerChunkIndex.x + chunkGenerationRadius + 1;
                 x++)
            {
                for (int y = playerChunkIndex.y - chunkGenerationRadius;
                     y < playerChunkIndex.y + chunkGenerationRadius + 1;
                     y++)
                {
                    // Check if the chunk is within the radius of generation
                    Vector2Int chunkIndex = new Vector2Int(x, y);
                    Vector3 chunkCenter = new Vector3(chunkIndex.x * chunkSize, chunkIndex.y * chunkSize, 0) +
                                          ((Vector3.one * chunkSize) / 2f);
                    if (Vector3.Distance(playerTransform.position, (Vector3)((Vector2)chunkIndex * chunkSize)) <=
                        chunkGenerationRadius * chunkSize)
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

        // Generates a chunk
        private void GenerateChunk(Vector2Int index)
        {
            // Create the chunk
            Chunk chunk = new Chunk(index, chunkSize);

            // Generate the dirt, gravel, and nutrients
            bool[,] dirtMap = new bool[chunkSize, chunkSize];
            bool[,] gravelMap = new bool[chunkSize, chunkSize];
            bool[,] richSoilMap = new bool[chunkSize, chunkSize];
            Vector3Int[,] positionArray = new Vector3Int[chunkSize, chunkSize];

            bool hasCave = false;

            // Iterate over each tile and place it in the scene
            foreach (Vector3Int tile in chunk.Bounds.allPositionsWithin)
            {
                Vector2Int mapIndex = (Vector2Int)(tile - chunk.Bounds.min);
                positionArray[mapIndex.x, mapIndex.y] = tile;
                // Generate the dirt if the y is less than or equal to zero
                if (tile.y <= 0)
                {
                    // Sample the dirt noise to see if dirt should be placed here
                    if (CanGenerateTile(new Vector2Int(tile.x, tile.y), dirtThreshold, _dirtNoise))
                    {
                        // dirtTilemap.SetTile(tile, dirtTile);
                        dirtMap[mapIndex.x, mapIndex.y] = true;
                    }
                    else
                    {
                        hasCave = true;
                    }

                    // Sample the gravel noise to see if gravel should be placed here
                    if (CanGenerateTile(new Vector2Int(tile.x, tile.y), gravelThreshold, _gravelNoise))
                    {
                        // gravelTilemap.SetTile(tile, gravelTile);
                        gravelMap[mapIndex.x, mapIndex.y] = true;
                    }

                    // Sample the rich soil noise to see if the rich soil should be placed here, only place nutrients where there is dirt and isnt gravel
                    if (dirtMap[mapIndex.x, mapIndex.y] && !gravelMap[mapIndex.x, mapIndex.y])
                    {
                        if (CanGenerateTile(new Vector2Int(tile.x, tile.y), richSoilThreshold, _richSoilNoise))
                        {
                            // richSoilTilemap.SetTile(tile, richSoilTile);
                            richSoilMap[mapIndex.x, mapIndex.y] = true;
                        }
                    }
                }
            }

            // Flatten the position array
            Vector3Int[] positions = Flatten2DVectorArray(positionArray);

            // Generate the dirt tiles as a block of tiles
            TileBase[] dirtTiles = GetTilesAsBlock(dirtMap, dirtTile);
            dirtTilemap.SetTiles(positions, dirtTiles);

            // Generate the gravel tiles as a block of tiles
            TileBase[] gravelTiles = GetTilesAsBlock(gravelMap, gravelTile);
            gravelTilemap.SetTiles(positions, gravelTiles);

            // Generate the rich soil tiles as a block of tiles
            TileBase[] richSoilTiles = GetTilesAsBlock(richSoilMap, richSoilTile);
            richSoilTilemap.SetTiles(positions, richSoilTiles);

            // Check if a rock can be generated in the chunk
            if (chunk.Index.y <= -2 && CanGenerateRock(chunk.Index))
            {
                // Get a random position within the chunk
                Vector3Int rockPosition = new Vector3Int(Random.Range(1, chunkSize - 2),
                    Random.Range(1, chunkSize - 2), 0) + chunk.Bounds.min;

                // Generate rock
                Quaternion rotation = Quaternion.identity;
                rotation.eulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
                Instantiate(rockAssets.GetRandomRock(), rockPosition + Vector3.one / 2f, rotation, rockParent);
            }

            // Store the dirt map to the chunk
            chunk.SetDirtMap(dirtMap);

            // Add the chunk to the list
            Chunks.Add(chunk);

            // If the chunk has a cave, attempt to fill it with water
            if (hasCave)
            {
                // First get all the regions starting in the current chunk
                List<List<Vector3Int>> chunkAirRegions = GetAirRegions(chunk);

                // Check each region to see if it exists in the current region collection
                foreach (List<Vector3Int> chunkAirRegion in chunkAirRegions)
                {
                    // Iterate over each tile in the air region
                    foreach (Vector3Int airTile in chunkAirRegion)
                    {
                        // Check if the region tile is already found in any of the current water regions
                        int regionIndex = IsTileInAirRegions(airTile);

                        // TODO: Region Joining not working?

                        // Merge the region into this new region if it exists already
                        if (regionIndex != -1)
                        {
                            AirRegions[regionIndex] = chunkAirRegion;
                        }
                        else
                        {
                            AirRegions.Add(chunkAirRegion);
                        }
                    }

                    // For the current region, add a level of water to the region
                    // First get the lowest point in the region
                    Vector3Int lowestRegionTile = GetLowestRegionTile(chunkAirRegion);

                    // Determine a water level for the air region
                    int waterLevel = Random.Range(minWaterLevel, maxWaterLevel);

                    // Fill the region with water up to the water level
                    List<Vector3Int> water = new List<Vector3Int>();
                    List<TileBase> waterTiles = new List<TileBase>();
                    foreach (Vector3Int tile in chunkAirRegion)
                    {
                        if (tile.y >= lowestRegionTile.y && tile.y <= lowestRegionTile.y + waterLevel)
                        {
                            water.Add(tile);
                            waterTiles.Add(waterTile);
                        }
                    }

                    waterTilemap.SetTiles(water.ToArray(), waterTiles.ToArray());
                }
            }
        }

        #endregion

        #region Air

        // Returns all the empty air regions that start in the current chunk
        private List<List<Vector3Int>> GetAirRegions(Chunk chunk)
        {
            List<List<Vector3Int>> airRegions = new List<List<Vector3Int>>();
            List<Vector3Int> flaggedTiles = new List<Vector3Int>();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Vector3Int currentTile = chunk.Bounds.min + new Vector3Int(x, y, 0);
                    if (!chunk.Dirt[x, y] && !flaggedTiles.Contains(currentTile))
                    {
                        List<Vector3Int> newAirRegion = GetAirRegionTiles(currentTile);
                        if (newAirRegion.Count > 0)
                        {
                            airRegions.Add(newAirRegion);
                        }

                        foreach (Vector3Int tile in newAirRegion)
                        {
                            flaggedTiles.Add(tile);
                        }
                    }
                }
            }

            return airRegions;
        }

        // Gets all the tiles in a single region given a start position
        private List<Vector3Int> GetAirRegionTiles(Vector3Int start)
        {
            List<Vector3Int> tiles = new List<Vector3Int>();
            List<Vector3Int> flaggedTiles = new List<Vector3Int>();

            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            flaggedTiles.Add(start);

            while (queue.Count > 0)
            {
                Vector3Int tile = queue.Dequeue();
                tiles.Add(tile);

                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        Vector3Int currentTile = new Vector3Int(x, y, start.z);
                        if (IsInWorldRange(currentTile))
                        {
                            if ((y == tile.y || x == tile.x))
                            {
                                if (!flaggedTiles.Contains(currentTile))
                                {
                                    queue.Enqueue(currentTile);
                                    flaggedTiles.Add(currentTile);
                                }
                            }
                        }
                        else
                        {
                            // // If an out of bounds tile is reached, just dump the flood fill
                            // return new List<Vector3Int> ();
                        }
                    }
                }
            }

            return tiles;
        }
        
        // Returns a int that isnt -1 if the tile position is in any of the air regions in the world
        private int IsTileInAirRegions(Vector3Int tile)
        {
            // Check each region to see if the tile exists in that region
            int regionIndex = -1;
            foreach (List<Vector3Int> chunkAirRegion in AirRegions)
            {
                regionIndex++;
                if (chunkAirRegion.FindIndex(t => t == tile) != -1)
                {
                    return regionIndex;
                }
            }

            return regionIndex;
        }

        #endregion

        #region Utility

        // Checks if the position is in any of the generated chunks
        private bool IsInWorldRange(Vector3Int position)
        {
            foreach (Chunk chunk in Chunks)
            {
                if (chunk.Bounds.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        // Returns the lowest point in a list of Vector3Int
        private Vector3Int GetLowestRegionTile(List<Vector3Int> region)
        {
            Vector3Int lowestPoint = region[0];
            foreach (Vector3Int tile in region)
            {
                if (tile.y < lowestPoint.y)
                {
                    lowestPoint = tile;
                }
            }

            return lowestPoint;
        }

        // Returns a flattened array from a 2D array of Vector3Ints
        private Vector3Int[] Flatten2DVectorArray(Vector3Int[,] positionArray)
        {
            List<Vector3Int> positions = new List<Vector3Int>();
            foreach (Vector3Int position in positionArray)
            {
                positions.Add(position);
            }

            return positions.ToArray();
        }

        // Returns a flat array of tilebases based on a 2D bool array
        private TileBase[] GetTilesAsBlock(bool[,] map, TileBase tile)
        {
            List<TileBase> tiles = new List<TileBase>();
            foreach (bool flag in map)
            {
                tiles.Add(flag ? tile : null);
            }

            return tiles.ToArray();
        }

        // Returns true if a rock should be generated in the chunk
        private bool CanGenerateRock(Vector2Int position)
        {
            float value = Mathf.Clamp(RemapFloat((float)_rockNoise.Get(position.x, position.y), -1f, 1f, 0f, 1f), 0f,
                1f);
            // Only spawn a rock if dirt is at the position of the rock
            if (CanGenerateTile(new Vector2Int(position.x, position.y), dirtThreshold, _dirtNoise))
            {
                return value > 1f - rockSpawnThreshold.GetThreshold(position.y);
            }

            return false;
        }

        // Returns true if a tile can be generated at the position
        private bool CanGenerateTile(Vector2Int position, float threshold, ImplicitFractal noise)
        {
            float value = Mathf.Clamp(RemapFloat((float)noise.Get(position.x, position.y), -1f, 1f, 0f, 1f), 0f, 1f);
            return value > 1f - threshold;
        }

        // Remaps a float to a new range
        public float RemapFloat(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        // Returns what type of terrain is at the current position
        public TerrainType GetTerrainType(Vector3 position)
        {
            if (position.y > 0)
            {
                // Anything above zero is always air
                return TerrainType.Air;
            }

            // Check the gravel tilemap first
            Vector3Int tile = Vector3Int.FloorToInt(position);
            if (gravelTilemap.GetTile(tile) != null)
            {
                return TerrainType.Gravel;
            }

            // Check the rich soil tilemap
            if (richSoilTilemap.GetTile(tile) != null)
            {
                return TerrainType.RichSoil;
            }

            // Check the water tilemap
            if (waterTilemap.GetTile(tile) != null)
            {
                // Check the dirt tilemap
                if (dirtTilemap.GetTile(tile) != null)
                {
                    return TerrainType.Dirt;
                }
                else
                {
                    return TerrainType.Water;
                }
            }

            return TerrainType.Air;
        }
        
        // Converts a world position to a chunk index
        public Vector2Int WorldToChunkIndex(Vector3 position)
        {
            return new Vector2Int((int)Mathf.Floor(position.x / (float)chunkSize),
                (int)Mathf.Floor(position.y / (float)chunkSize));
        }

        #endregion

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
                return Mathf.Clamp(RemapFloat((float)depth, (float)MinDepth, (float)MaxDepth, MinChance, MaxChance),
                    MinChance, MaxChance);
            }

            // Remaps a float
            private float RemapFloat(float value, float from1, float to1, float from2, float to2)
            {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
}
