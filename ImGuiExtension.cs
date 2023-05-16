using ImGuiNET;
using SharpDX;
using ImGuiVector4 = System.Numerics.Vector4;

namespace WhatAreYouDoing
{
    public class ImGuiExtension
    {
        // Int Drags
        public static int IntDrag(string labelString, int value, int minValue, int maxValue, float dragSpeed)
        {
            var refValue = value;
            ImGui.DragInt(labelString, ref refValue, dragSpeed, minValue, maxValue);
            return refValue;
        }

        // Color Pickers
        public static Color ColorPicker(string labelName, Color inputColor)
        {
            var color = inputColor.ToVector4();
            var colorToVect4 = new ImGuiVector4(color.X, color.Y, color.Z, color.W);
            return ImGui.ColorEdit4(labelName, ref colorToVect4, ImGuiColorEditFlags.AlphaBar)
                ? new Color(colorToVect4.X, colorToVect4.Y, colorToVect4.Z, colorToVect4.W)
                : inputColor;
        }

        // Checkboxes
        public static bool Checkbox(string labelString, bool boolValue)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            return boolValue;
        }
    }
}
