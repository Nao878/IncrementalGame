using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility: writes default KanjiRecipe.csv under Resources (UTF-8).
/// </summary>
public static class KanjiRecipeCsvUtility
{
    const string ResourcePath = "Assets/Resources/KanjiRecipe.csv";

    [MenuItem("Tools/Kanji/Generate Default KanjiRecipe.csv")]
    static void GenerateDefaultCsv()
    {
        string dir = Path.GetDirectoryName(ResourcePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("# left,right,result (UTF-8)");
        sb.AppendLine("日,月,明");
        sb.AppendLine("木,木,林");
        sb.AppendLine("火,火,炎");

        File.WriteAllText(ResourcePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("KanjiRecipe", "Generated: " + ResourcePath, "OK");
    }
}
