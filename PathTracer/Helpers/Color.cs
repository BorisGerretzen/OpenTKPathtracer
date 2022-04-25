using OpenTK.Mathematics;

namespace PathTracer.Helpers; 

public static class Color {
    public static Vector3 FromHex(int color) {
        int r = (color >> 16) & 0xFF;
        int g = (color >> 8) & 0xFF;
        int b = color & 0xFF;

        return new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
    }

    public static Vector3 FromHexString(string hex) {
        hex = hex.Replace("#", "");
        return FromHex(int.Parse(hex));
    }
}