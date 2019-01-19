using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using System.IO;
using UnityEngine;

public class Facecrop: MonoBehaviour {


    const string FaceAPIKey = "b4326411e08a434a9f65c74831c68ab5";
	const string DetectURL = "https://japaneast.api.cognitive.microsoft.com/face/v1.0/detect";
	const int WIDTH = 1408;
    const int HEIGHT = 792;

    string jsonText = "";
 

    // Use this for initialization
    void Start () {
        StartCoroutine(GetFaceImage(@"c:\Users\kamoh\Documents\facecrop\picture.jpg", Test));

    }

    // Update is called once per frame
    void Update () {
		
	}

    

    IEnumerator GetFaceImage(string filePath, UnityAction<string> callback)
    {


        jsonText = "";



        // 画像をbyte配列に変換する
        byte[] imageData = LoadBytes(filePath);

        // この中でFaceApiに画像を投げる
        var postToFaceAPI = StartCoroutine(PostToFaceAPI(imageData));
        yield return postToFaceAPI;

        Debug.Log(jsonText);


        // jsontextからjsonObjectに変換
        JSONObject json = new JSONObject(jsonText);




        // クリッピング処理
        Texture2D texture = new Texture2D(WIDTH, HEIGHT, TextureFormat.ARGB4444, false);

        texture.LoadImage(imageData);


        int left = (int)json[0].GetField("faceRectangle").GetField("left").n;
        int top = (int)json[0].GetField("faceRectangle").GetField("top").n;
        int width = (int)json[0].GetField("faceRectangle").GetField("width").n;
        int height = (int)json[0].GetField("faceRectangle").GetField("height").n;

        //左下基準に変更する

        int botom = HEIGHT - top - height;


        //拡大する

        float diff = (int)(width * 0.8);

        int x  = (int)(left - diff/2);
        int y = (int)(botom - diff/2);
        width =(int)(width + diff);
        height = (int)(height + diff);


        Texture2D clipTex;
        Color[] pixel;

        pixel = texture.GetPixels(x, y, width, height);


        clipTex = new Texture2D(width, height);
        clipTex.SetPixels(pixel);
        clipTex.Apply();

        string newFilePath = @"c:\Users\kamoh\Documents\facecrop\picture.png";
        byte[] pngData = clipTex.EncodeToPNG();
        File.WriteAllBytes(newFilePath, pngData);


        // 最後にコールバックで新規生成した画像のパスを送る
        callback(newFilePath);
    }


    IEnumerator PostToFaceAPI(byte[] imageData)
    {
        var headers = new Dictionary<string, string>() {
                    { "Ocp-Apim-Subscription-Key", FaceAPIKey },
                { "Content-Type", "application/octet-stream" }
          };

        WWW www = new WWW(DetectURL, imageData, headers);
        yield return www;

        jsonText = www.text;
    }


    byte[] LoadBytes(string filePath)
    {
        FileStream fs = new FileStream(filePath, FileMode.Open);
        BinaryReader bin = new BinaryReader(fs);

        byte[] result = bin.ReadBytes((int)bin.BaseStream.Length);

        bin.Close();
        fs.Close();

        return result;
    }

    private void Test(string path)
    {
        Debug.Log(path);
    }
}
