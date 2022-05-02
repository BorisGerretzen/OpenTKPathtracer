using OpenTK.Mathematics;

namespace PathTracer.Helpers;

public static class Color {
    public static Vector3 FromHex(int color) {
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;

        return new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
    }

    public static Vector3 FromHexString(string hex) {
        hex = hex.Replace("#", "");
        return FromHex(int.Parse(hex));
    }
}