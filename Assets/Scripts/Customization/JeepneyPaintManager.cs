using UnityEngine;

public class JeepneyPaintManager : MonoBehaviour
{
    public Texture2D defaultPaintTexture;
    public RenderTexture paintTexture;

    void Start()
    {
        Graphics.Blit(
            defaultPaintTexture,
            paintTexture
        );
    }
}