using System.Numerics;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;


namespace SpawnTools;

public partial class SpawnTools
{
    private static string VectorToString(Vector3 vec)
    {
        return $"{vec.X}|{vec.Y}|{vec.Z}";
    }

    private static string VectorToString(QAngle vec)
    {
        return $"{vec.X}|{vec.Y}|{vec.Z}";
    }

    private static Vector3 StringToVector(string str)
    {
        var explode = str.Split("|");
        return new Vector3(x: float.Parse(explode[0]), y: float.Parse(explode[1]), z: float.Parse(explode[2]));
    }

    private static Vector NormalVectorToValve(Vector3 v)
    {
        return new Vector(v.X, v.Y, v.Z);
    }
}