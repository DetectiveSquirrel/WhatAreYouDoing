using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhatAreYouDoing
{
    public class WhatAreYouDoingSettings : ISettings
    {
        //Mandatory setting to allow enabling/disabling your plugin
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public ToggleNode MultiThreading { get; set; } = new ToggleNode(false);
        public RangeNode<int> MaxCircleDrawDistance { get; set; } = new RangeNode<int>(120, 0, 200);
        public WAYDConfig MovingTraps { get; set; } = new WAYDConfig()
        {
            Enable = true,
            TrapType = TrapType.GroundMover,
            Colors = new WAYDConfig.WAYDColors
            {
                MapColor = new Color(35, 194, 47, 193),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(35, 194, 47, 193),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYDConfig.WAYDWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderCircle = true,
                RenderCircleThickness = 3,
                LineThickness = 6
            },
            Map = new WAYDConfig.WAYDMap
            {
                Enable = true,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
        public WAYDConfig DartTraps { get; set; } = new WAYDConfig()
        {
            Enable = true,
            TrapType = TrapType.Darts,
            Colors = new WAYDConfig.WAYDColors
            {
                MapColor = new Color(35, 194, 47, 193),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(35, 194, 47, 193),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYDConfig.WAYDWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderCircle = true,
                RenderCircleThickness = 3,
                LineThickness = 6
            },
            Map = new WAYDConfig.WAYDMap
            {
                Enable = true,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
    }
}