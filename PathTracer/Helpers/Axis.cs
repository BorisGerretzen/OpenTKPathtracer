using System.ComponentModel;

namespace PathTracer.Helpers;

public enum Axis {
    X,
    Y,
    Z,
    None
}

public static class AxisHelpers {
    public static int AxisToInt(Axis axis) {
        if (axis == Axis.X) return 0;
        if (axis == Axis.Y) return 1;
        if (axis == Axis.Z) return 2;
        if (axis == Axis.None) return -1;
        throw new InvalidEnumArgumentException($"Invalid enum value '{axis}'");
    }
}