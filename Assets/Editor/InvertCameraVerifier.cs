using System;
using Nekki.Vector.Core.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class InvertCameraVerifier
{
    [MenuItem("Tools/Verify Invert Camera")]
    private static void Verify()
    {
        if (!EditorApplication.isPlaying || GraphicsSettings.currentRenderPipeline == null)
        {
            throw new InvalidOperationException("Run this verifier in Play mode with URP active.");
        }

        var cameraObject = new GameObject("Invert camera verifier camera");
        var spriteObject = new GameObject("Invert camera verifier sprite");
        var renderTexture = new RenderTexture(64, 64, 16);
        var sourceTexture = new Texture2D(2, 2);
        var pixels = new Texture2D(64, 64);
        Sprite sprite = null;
        var previousActive = RenderTexture.active;

        try
        {
            sourceTexture.SetPixels(new[] { Color.red, Color.red, Color.red, Color.red });
            sourceTexture.Apply();
            sprite = Sprite.Create(sourceTexture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1);

            spriteObject.layer = 30;
            spriteObject.transform.position = new Vector3(0, 2, 0);
            spriteObject.AddComponent<SpriteRenderer>().sprite = sprite;

            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = renderTexture;
            cameraObject.AddComponent<InvertCamera>();

            VerifyRender(camera, renderTexture, pixels, "orthographic");
            camera.orthographic = false;
            camera.fieldOfView = 60;
            VerifyRender(camera, renderTexture, pixels, "perspective");

            Debug.Log("InvertCamera URP render verification passed.");
        }
        finally
        {
            RenderTexture.active = previousActive;
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(spriteObject);
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(sprite);
            UnityEngine.Object.Destroy(sourceTexture);
            UnityEngine.Object.Destroy(pixels);
        }
    }

    private static void VerifyRender(Camera camera, RenderTexture renderTexture, Texture2D pixels, string projection)
    {
        bool originalCulling = GL.invertCulling;
        camera.Render();
        if (GL.invertCulling != originalCulling)
        {
            throw new InvalidOperationException($"{projection} render left inverted culling enabled.");
        }

        RenderTexture.active = renderTexture;
        pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        pixels.Apply();

        Color lower = pixels.GetPixel(32, 19);
        Color upper = pixels.GetPixel(32, 45);
        if (lower.r < 0.7f || lower.g > 0.2f || upper.r > 0.2f)
        {
            throw new InvalidOperationException(
                $"{projection}: expected red below the center and black above it; lower={lower}, upper={upper}.");
        }
    }
}
