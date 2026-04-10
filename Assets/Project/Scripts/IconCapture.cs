using UnityEngine;
using System.IO;
using System.Collections;

public class IconCapture : MonoBehaviour
{
    public Camera cam;
    public RenderTexture rt;

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        Capture();
    }
    public void Capture()
    {
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/icon.png", bytes);

        RenderTexture.active = null;
    }
}