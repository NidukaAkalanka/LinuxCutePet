using System;

namespace PetViewerLinux
{
    public class EdgeCalibration
    {
        public int TopEdgeY { get; set; } = -1;
        public int BottomEdgeY { get; set; } = -1;
        public int LeftEdgeX { get; set; } = -1;
        public int RightEdgeX { get; set; } = -1;
        
        // Derived corner positions
        public int TopLeftX => LeftEdgeX;
        public int TopLeftY => TopEdgeY;
        public int TopRightX => RightEdgeX;
        public int TopRightY => TopEdgeY;
        public int BottomLeftX => LeftEdgeX;
        public int BottomLeftY => BottomEdgeY;
        public int BottomRightX => RightEdgeX;
        public int BottomRightY => BottomEdgeY;
        
        public bool IsCalibrated => TopEdgeY != -1 && BottomEdgeY != -1 && LeftEdgeX != -1 && RightEdgeX != -1;
        
        public void Reset()
        {
            TopEdgeY = -1;
            BottomEdgeY = -1;
            LeftEdgeX = -1;
            RightEdgeX = -1;
        }
    }
}