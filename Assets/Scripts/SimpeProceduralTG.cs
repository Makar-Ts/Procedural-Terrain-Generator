using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(Terrain))]
public class SimpeProceduralTG : MonoBehaviour
{
    [Header("Heights Noises")]
    [SerializeField] private NoiseSet[] Noises;
    
    [Header("Textures Noises")]
    [SerializeField] private TexturesNS[] TextureNoises;

    [Header("Tree Noise")]
    [SerializeField] private float treesDencity = 0.2f;
    [SerializeField] private Vector2 treesSizeRange = new(1, 2.5f);
    [SerializeField] private NoiseSet TreeNoise, LawnNoise;
    //[SerializeField] private TreeNoiseSet[] treeNoises;
    [Header("Other")]
    [SerializeField] private bool useCorrectionHeightmap = false;
    [SerializeField] private bool useLastTerrainColorToHeightmap = false;
    [SerializeField] private Texture2D correctionHighmap;
    [SerializeField] private float correctionHighmapStrength = 0.1f;
    [SerializeField] private float correctionHighmapLerp = 0.8f;

    private Terrain terrain;
    private float minMaxHeight = 0;
    private float[] minMaxAlphamapsHeights;
    private Vector3Int terrainSize;

    private void Start() {
        terrain = GetComponent<Terrain>();

        SetNoiseSeed();
        print(minMaxHeight);

        terrainSize = Vector3Int.FloorToInt(new (terrain.terrainData.heightmapResolution, 0, terrain.terrainData.heightmapResolution));
        float scale = terrain.terrainData.size.x / 1000;

        print(terrain.terrainData.alphamapLayers);

        if (Noises.Length != 0) {
            if (useCorrectionHeightmap) {
                terrain.terrainData.SetHeights(0, 0, GenerateHeights(Noises, new(terrainSize.x, terrainSize.z), scale, correctionHighmap, correctionHighmapStrength, correctionHighmapLerp));   
            } else {
                terrain.terrainData.SetHeights(0, 0, GenerateHeights(Noises, new(terrainSize.x, terrainSize.z), scale));
            }            
        }

        if (TextureNoises.Length != 0) {
            if (useLastTerrainColorToHeightmap) { 
                terrain.terrainData.SetAlphamaps(0, 0, GenerateAlphamaps(TextureNoises, Vector2Int.FloorToInt(new(terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution)), terrain.terrainData.alphamapLayers, correctionHighmap));
            } else {
                terrain.terrainData.SetAlphamaps(0, 0, GenerateAlphamaps(TextureNoises, Vector2Int.FloorToInt(new(terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution)), terrain.terrainData.alphamapLayers));
            }
        }

        if (terrain.terrainData.treePrototypes.Length != 0) {
            TreeInstance[] trees;
            if (useCorrectionHeightmap) trees = GenerateTrees(TreeNoise, LawnNoise, new(Mathf.RoundToInt(terrain.terrainData.size.x), Mathf.RoundToInt(terrain.terrainData.size.z)), 0.3f, terrain.terrainData.treePrototypes.Length, correctionHighmap);
            else                        trees = GenerateTrees(TreeNoise, LawnNoise, new(Mathf.RoundToInt(terrain.terrainData.size.x), Mathf.RoundToInt(terrain.terrainData.size.z)), 0.3f, terrain.terrainData.treePrototypes.Length);
            
            terrain.terrainData.treeInstances = trees;
        }

        if (terrain.terrainData.detailPrototypes.Length != 0) {
            int detailsTypeCount = terrain.terrainData.detailPrototypes.Length;
            int[,] details;
            
            if (useCorrectionHeightmap) details = GenerateDetails(LawnNoise, new(Mathf.RoundToInt(terrain.terrainData.size.x), Mathf.RoundToInt(terrain.terrainData.size.z)), 4000, correctionHighmap);
            else                        details = GenerateDetails(LawnNoise, new(Mathf.RoundToInt(terrain.terrainData.size.x), Mathf.RoundToInt(terrain.terrainData.size.z)), 4000);

            for (int i = 0; i < detailsTypeCount; i++) {
                terrain.terrainData.SetDetailLayer(0, 0, i, details);
            }
        }
    }



    private void SetNoiseSeed() {
        int globalSeed = UnityEngine.Random.Range(0, 100000000);

        foreach (var item in Noises)
        {
            item.noise = new FastNoiseLite();
            item.noise.SetNoiseType(item.noiseType);
            item.noise.SetFrequency(item.frequency);

            if (item.constantSeed) {
                item.noise.SetSeed(item.seed);
            } else if (item.randomlySeed) {
                item.seed = UnityEngine.Random.Range(0, 100000000);
                item.noise.SetSeed(item.seed);
            } else {
                item.noise.SetSeed(globalSeed);
            }

            if (!item.rangeUsage) minMaxHeight += item.amplitude;
        }

        minMaxAlphamapsHeights = new float[terrain.terrainData.alphamapLayers];
        foreach (var item in TextureNoises)
        {
            item.noise = new FastNoiseLite();
            item.noise.SetNoiseType(item.noiseType);
            item.noise.SetFrequency(item.frequency);

            if (item.constantSeed) {
                item.noise.SetSeed(item.seed);
            } else if (item.randomlySeed) {
                item.seed = UnityEngine.Random.Range(0, 100000000);
                item.noise.SetSeed(item.seed);
            } else {
                item.noise.SetSeed(globalSeed);
            }

            if (!item.rangeUsage) minMaxAlphamapsHeights[item.terrainTextureIndex] += item.amplitude;
        }

        TreeNoise.noise = new FastNoiseLite();
        TreeNoise.noise.SetNoiseType(TreeNoise.noiseType);
        TreeNoise.noise.SetFrequency(TreeNoise.frequency);

        if (TreeNoise.constantSeed) {
            TreeNoise.noise.SetSeed(TreeNoise.seed);
        } else if (TreeNoise.randomlySeed) {
            TreeNoise.seed = UnityEngine.Random.Range(0, 100000000);
            TreeNoise.noise.SetSeed(TreeNoise.seed);
        } else {
            TreeNoise.noise.SetSeed(globalSeed);
        }

        LawnNoise.noise = new FastNoiseLite();
        LawnNoise.noise.SetNoiseType(LawnNoise.noiseType);
        LawnNoise.noise.SetFrequency(LawnNoise.frequency);

        if (LawnNoise.constantSeed) {
            LawnNoise.noise.SetSeed(LawnNoise.seed);
        } else if (LawnNoise.randomlySeed) {
            LawnNoise.seed = UnityEngine.Random.Range(0, 100000000);
            LawnNoise.noise.SetSeed(LawnNoise.seed);
        } else {
            LawnNoise.noise.SetSeed(globalSeed);
        }
    }

    private float[,] GenerateHeights(NoiseSet[] Noises, Vector2Int terrainSize, float scale, Texture2D correctionHighmap = null, float correctionHighmapStrength = 0f, float correctionHighmapLerp = 1f) {
        float[,] heights = new float[terrainSize.x, terrainSize.y];

        for (int x=0; x<terrainSize.x; x++)
        {
            for (int y=0; y<terrainSize.y; y++)
            {
                float height = 0;
                foreach (var item in Noises)
                {
                    if (!item.rangeUsage) height += item.noise.GetNoise(x/item.zoom*scale, y/item.zoom*scale)*item.amplitude;
                    
                    //print(item.noise.GetNoise(x, y) + " " + item.noiseType);
                }
                foreach (var item in Noises)
                {
                    if (item.rangeUsage && item.heightRange.x < height && item.heightRange.y > height) 
                    {   
                        float addHeight = 0;
                        if (height-item.heightRange.x < item.rangeUsageSmooth)
                        {
                            addHeight += Mathf.Lerp(0, item.noise.GetNoise(x/item.zoom*scale, y/item.zoom*scale)*item.amplitude, (height-item.heightRange.x)/(item.rangeUsageSmooth));
                        } else if (item.heightRange.y-height < item.rangeUsageSmooth) 
                        {
                            addHeight += Mathf.Lerp(0, item.noise.GetNoise(x/item.zoom*scale, y/item.zoom*scale)*item.amplitude, (item.heightRange.y-height)/(item.rangeUsageSmooth));
                        } else 
                        {
                            addHeight += item.noise.GetNoise(x/item.zoom*scale, y/item.zoom*scale)*item.amplitude;   
                        }

                        height += addHeight;
                    }
                }

                if (correctionHighmap != null) {
                    float color = correctionHighmap.GetPixel(Mathf.FloorToInt(x/(float)terrainSize.x*correctionHighmap.width), Mathf.FloorToInt(y/(float)terrainSize.y*correctionHighmap.height)).grayscale;

                    if (color != 0) height += color*correctionHighmapStrength;
                }

                heights[x, y] = scaleBetween(height, 0, 1, -minMaxHeight, minMaxHeight);
            }
        }

        return heights;
    }

    private float[,,] GenerateAlphamaps(TexturesNS[] Noises, Vector2Int terrainSize, int alphamapLayers, Texture2D correctionHighmap = null) {
        float[,,] heights = new float[terrainSize.x, terrainSize.y, alphamapLayers];

        for (int x=0; x<terrainSize.x; x++)
        {
            for (int y=0; y<terrainSize.y; y++)
            {
                if (correctionHighmap != null) {
                    float grayscale = correctionHighmap.GetPixel(Mathf.FloorToInt(x/(float)terrainSize.x*correctionHighmap.width), 
                                                   Mathf.FloorToInt(y/(float)terrainSize.y*correctionHighmap.height)).grayscale;

                    if (grayscale >= 0.99f) {
                        heights[x, y, alphamapLayers-1] = grayscale;

                        continue;
                    } else if (grayscale > 0) {
                        heights[x, y, alphamapLayers-1] = grayscale;
                    }
                }
                
                foreach (var item in Noises)
                {
                    if (!item.rangeUsage) heights[x, y, item.terrainTextureIndex] += scaleBetween(item.noise.GetNoise(x/item.zoom, y/item.zoom)*item.amplitude,
                                                                                                  0, 1, -minMaxAlphamapsHeights[item.terrainTextureIndex], minMaxAlphamapsHeights[item.terrainTextureIndex]) * item.terrainTextureStrength;
                }
                
                /*float outHeight = scaleBetween(height, 0, alphamapLayers-1, -minMaxAlphamapHeight, minMaxAlphamapHeight);

                for (int i = 0; i < alphamapLayers; i++) {
                    heights[x, y, i] = (Mathf.Abs(outHeight-i) > 1 ? 0 : Mathf.Abs(outHeight-i));
                }*/
            }
        }

        return heights;
    }

    private TreeInstance[] GenerateTrees(NoiseSet Noise, NoiseSet LawnNoise, Vector2Int terrainSize, float treeDencity, int treesTypesCount, Texture2D correctionHighmap = null) {
        List<TreeInstance> trees = new List<TreeInstance>();

        for (int x=0; x<terrainSize.x; x++)
        {
            for (int y=0; y<terrainSize.y; y++)
            {
                if (correctionHighmap != null) if (correctionHighmap.GetPixel(
                                                            Mathf.FloorToInt(y/(float)terrainSize.y*correctionHighmap.height), 
                                                            Mathf.FloorToInt(x/(float)terrainSize.x*correctionHighmap.width)).grayscale > 0) continue;

                float spawnChance = UnityEngine.Random.Range(0.0f, 1.0f) + scaleBetween(LawnNoise.noise.GetNoise(x/LawnNoise.zoom, y/LawnNoise.zoom), 0, 0.8f, -LawnNoise.amplitude, LawnNoise.amplitude);
                if (spawnChance > treeDencity) continue;

                float noise = scaleBetween(Noise.noise.GetNoise(x/Noise.zoom, y/Noise.zoom), -0.5f*(treesTypesCount-1), 0.5f*(treesTypesCount-1), -Noise.amplitude, Noise.amplitude);
                float chance = UnityEngine.Random.Range(0.0f, 1.0f*(treesTypesCount-1));

                TreeInstance tree = new();
                tree.prototypeIndex = (treesTypesCount != 1 ? Mathf.RoundToInt(chance+noise) : 0);
                tree.position = new((x+UnityEngine.Random.Range(-0.5f, 0.5f))/terrain.terrainData.size.x, terrain.SampleHeight(new(x, 0, y))/terrain.terrainData.size.y, (y+UnityEngine.Random.Range(-0.5f, 0.5f))/terrain.terrainData.size.z);
                float scale = UnityEngine.Random.Range(treesSizeRange.x, treesSizeRange.y);
                tree.widthScale = scale;
                tree.heightScale = scale;
                tree.color = Color.white;
                tree.lightmapColor = Color.white;

                trees.Add(tree);
            }
        }

        return trees.ToArray();
    }

    private int[,] GenerateDetails(NoiseSet Noise, Vector2Int terrainSize, int detailDencity, Texture2D correctionHighmap = null) {
        int[,] details = new int[terrainSize.x, terrainSize.y];

        for (int x=0; x<terrainSize.x; x++)
        {
            for (int y=0; y<terrainSize.y; y++)
            {
                float correction = 1;

                if (correctionHighmap != null) { 
                    correction = correctionHighmap.GetPixel(
                                                            Mathf.FloorToInt(x/(float)terrainSize.x*correctionHighmap.width-0.01f*correctionHighmap.width), 
                                                            Mathf.FloorToInt(y/(float)terrainSize.y*correctionHighmap.height)).grayscale;

                    if (correction > 0) continue;
                }

                details[x, y] = Mathf.RoundToInt(scaleBetween(Noise.noise.GetNoise(x/Noise.zoom, y/Noise.zoom), 0, 1, -Noise.amplitude, Noise.amplitude)*detailDencity);
            }
        }

        return details;
    }

    float scaleBetween(float value, float outMin, float outMax, float inputMin, float inputMax) =>
        outMin + (outMax - outMin) * ((value - inputMin) / (inputMax - inputMin));
    
    float CalculateLayerStrength(float alphaValue, int currentLayer, int alphamapLayers)
    {
        if (currentLayer-1 < alphaValue & alphaValue < currentLayer+1) {
            return Mathf.Abs(alphaValue-currentLayer);
        }
        return 0;
    }
}

[Serializable]
public class NoiseSet
{
    [NonSerialized] public FastNoiseLite noise;
    [Header("Seed")]
    public bool constantSeed      = false;
    public bool randomlySeed      = true;
    public int seed               = 0;
    [Header("Noise Settings")]
    public FastNoiseLite.NoiseType noiseType;
    public float frequency        = 0.2f;
    public float amplitude        = 1;
    public float zoom             = 1;
    [Header("Height Range")]
    [HideInInspector] public bool rangeUsage        = false;
    [HideInInspector] public float rangeUsageSmooth = 1f;
    [HideInInspector] public Vector2 heightRange    = new Vector2(0, 10);
}

[Serializable]
public class TexturesNS : NoiseSet
{
    [Header("Other")]
    public int terrainTextureIndex = 0;
    public float terrainTextureStrength = 1f;
}