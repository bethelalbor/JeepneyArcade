using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Owns the shared runtime paint RenderTexture and its persistent PNG save.
/// </summary>
public class JeepneyPaintManager : MonoBehaviour
{
    private const string SaveFileName = "jeepney_paint.png";

    public Texture2D defaultPaintTexture;
    public RenderTexture paintTexture;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Start()
    {
        LoadPaint();
    }

    [ContextMenu("Save Paint")]
    public void SavePaint()
    {
        if (paintTexture == null)
        {
            Debug.LogError("Jeepney paint could not be saved because JeepneyPaint_RT is missing.", this);
            return;
        }

        RenderTexture previousActive = RenderTexture.active;
        Texture2D readback = null;
        try
        {
            readback = new Texture2D(paintTexture.width, paintTexture.height, TextureFormat.RGBA32, false, false);
            RenderTexture.active = paintTexture;
            readback.ReadPixels(new Rect(0, 0, paintTexture.width, paintTexture.height), 0, 0);
            readback.Apply(false, false);
            File.WriteAllBytes(SavePath, readback.EncodeToPNG());
            Debug.Log($"Jeepney paint saved to '{SavePath}'.", this);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Jeepney paint save failed at '{SavePath}': {exception.Message}", this);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (readback != null)
                Destroy(readback);
        }
    }

    [ContextMenu("Load Paint")]
    public void LoadPaint()
    {
        if (paintTexture == null)
        {
            Debug.LogError("Jeepney paint could not be loaded because JeepneyPaint_RT is missing.", this);
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log($"No saved jeepney paint was found at '{SavePath}'. Loading the default paint.", this);
            ResetPaintLayer();
            return;
        }

        Texture2D loadedPaint = null;
        try
        {
            loadedPaint = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!loadedPaint.LoadImage(File.ReadAllBytes(SavePath), false))
            {
                Debug.LogError($"Jeepney paint load failed because '{SavePath}' is not a valid PNG.", this);
                ResetPaintLayer();
                return;
            }

            Graphics.Blit(loadedPaint, paintTexture);
            Debug.Log($"Jeepney paint loaded from '{SavePath}'.", this);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Jeepney paint load failed at '{SavePath}': {exception.Message}", this);
            ResetPaintLayer();
        }
        finally
        {
            if (loadedPaint != null)
                Destroy(loadedPaint);
        }
    }

    [ContextMenu("Reset Paint Layer")]
    public void ResetPaintLayer()
    {
        if (paintTexture == null || defaultPaintTexture == null)
        {
            Debug.LogError("Jeepney runtime paint layer could not be reset because its source or target is missing.", this);
            return;
        }

        Graphics.Blit(defaultPaintTexture, paintTexture);
        Debug.Log("Jeepney runtime paint layer restored from the default texture.", this);
    }
}
