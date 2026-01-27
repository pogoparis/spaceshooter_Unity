using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class EnemyRoutePackCreator
{
    [MenuItem("Tools/Enemy Routes/Create Base Pack")]
    public static void CreateBasePack()
    {
        const string folder = "Assets/Routes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Routes");

        Create("Route_StraightDown", MakeStraight());
        Create("Route_DiagonalLeft", MakeDiag(-3f));
        Create("Route_DiagonalRight", MakeDiag(3f));
        Create("Route_ZigZag3", MakeZigZag3());
        Create("Route_SCurveLeft", MakeSCurve(-2.5f));
        Create("Route_SCurveRight", MakeSCurve(2.5f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Base route pack created in Assets/Routes");
    }

    static void Create(string assetName, List<EnemyRouteSO.RouteNode> nodes)
    {
        string path = $"Assets/Routes/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<EnemyRouteSO>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<EnemyRouteSO>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.offsetsRelativeToSpawn = true;
        asset.loop = false;
        asset.endMode = EnemyRouteSO.EndMode.ContinueDefaultMovement;
        asset.nodes = nodes;

        EditorUtility.SetDirty(asset);
    }

    static EnemyRouteSO.RouteNode Node(float x, float y, float speed, float pause = 0f)
    {
        return new EnemyRouteSO.RouteNode
        {
            offset = new Vector2(x, y),
            speed = speed,
            pause = pause
        };
    }

    static List<EnemyRouteSO.RouteNode> MakeStraight()
        => new() { Node(0, 0, 2.6f), Node(0, -12, 2.6f) };

    static List<EnemyRouteSO.RouteNode> MakeDiag(float endX)
        => new() { Node(0, 0, 2.8f), Node(endX, -12, 2.8f) };

    static List<EnemyRouteSO.RouteNode> MakeZigZag3()
        => new()
        {
            Node(0, 0, 3.2f),
            Node(-2.2f, -3f, 3.2f),
            Node( 2.2f, -6f, 3.2f),
            Node(-2.2f, -9f, 3.2f),
            Node(0, -12f, 3.2f),
        };

    static List<EnemyRouteSO.RouteNode> MakeSCurve(float side)
        => new()
        {
            Node(0, 0, 2.7f),
            Node( side, -3f, 2.7f),
            Node(-side, -6f, 2.7f),
            Node( side, -9f, 2.7f),
            Node(0, -12f, 2.7f),
        };
}
