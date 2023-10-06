using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HeighMapGenerator : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private Texture2D source;
    [SerializeField] private int resolution = 4096;
    [SerializeField] private FilterMode filterMode;
    [SerializeField] private TextureWrapMode wrapMode;
    
    [Header("Circle")]
    [SerializeField] private bool generateCircle = true;
    [SerializeField] private float radius = 50f;
    [SerializeField] private float deadZone = 25f;
    [SerializeField] private Vector2 offset = new Vector2(0, 0);

    private void OnValidate() {
        Generate();
    }

    private void Generate() {
        source.Equals(new Texture2D(resolution, resolution));

        source.filterMode = filterMode;
        source.wrapMode = wrapMode;

        float pixelScale = 1f / resolution;
        for (int y = 0; y < source.height; y++) {
            for (int x = 0; x < source.width; x++) {
                if (generateCircle) {
                    float dist = Vector2.Distance(new(x, y), offset);

                    if (dist < radius+deadZone) {
                        float color = 1-(dist > deadZone ? (dist-deadZone)/radius : 0);

                        source.SetPixel(x, y, new(color, color, color));
                    } else {
                        source.SetPixel(x, y, new(0, 0, 0));
                    }
                } else {
                    source.SetPixel(x, y, new(0, 0, 0));
                }
            }
        }
        source.Apply();
    }
}
