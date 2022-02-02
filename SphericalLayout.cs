using StereoKit;
using System;
using System.Collections.Generic;
using RDR.GraphUI;

namespace RDR
{
    public class SphericalLayout : ILayout
    {
        
        Pose pose;
        private float armDistance = 0.6f; // arm distance is in meter
        private const float horizontal_fov = 2f * SKMath.Pi / 3f; // horizontal binaucular field of vision angle in radian.< 120 deg 
        private List<IDrawable> elementList;
        private int index;
        private float panelSizeW, panelSizeH;
        private IDrawable selected;

        public SphericalLayout(Pose pose, float diameter, float panelSizeW = 25 * U.cm, float panelSizeH = 8 * U.cm)
        {
            this.pose = pose;
            this.armDistance = diameter;
            this.panelSizeW = panelSizeW;
            this.panelSizeH = panelSizeH;
        }
        public void SetElementList(List<IDrawable> elementList, int focusIndex, int page, int pageSize)
        {
            this.elementList = elementList;
            if (focusIndex >= this.elementList.Count) { focusIndex = this.elementList.Count - 1; }
            index = focusIndex;

        }
        public LayoutStatus DrawAtPose(Pose pose)
        {
            this.pose = pose;
            return Draw();

        }
        private Pose GetPoseForIndex(int index)
        {


            int n = elementList.Count;
            int elementPerLine = (int)Math.Truncate((horizontal_fov * armDistance) / panelSizeW);
            int numberOfLines = n / elementPerLine;
            int line = index / elementPerLine;
            int indexInLine = index % elementPerLine;
            float deltaAngle = panelSizeH / armDistance;
            float verticalAngle = (line - (numberOfLines / 2)) * deltaAngle;
            float panelAngle = panelSizeW / armDistance;
            float horizontalAngle = ((SKMath.Pi - horizontal_fov) / 2.0f) + indexInLine * panelAngle;

            var dh = armDistance * SKMath.Cos(verticalAngle); //
            var h = armDistance * SKMath.Sin(verticalAngle);
            Vec3 at = pose.position + new Vec3(dh * SKMath.Cos(horizontalAngle), h, -(dh * SKMath.Sin(horizontalAngle)));
            Pose p = new Pose(at, Quat.LookAt(at, pose.position, Vec3.Up));

            return p;
        }




        public LayoutStatus Draw()
        {
            LayoutStatus status = new LayoutStatus();
           
            status.index = index;
            status.isSelected = false;
            if (elementList != null)
            {

                 
                for (int i = 0; i < elementList.Count; i++)
                {
                    var e = elementList[i];
                    bool isElementSelected = e.DrawAtPose(GetPoseForIndex(i));
                    if (isElementSelected)
                    {
                        status.isSelected = true;
                        status.selected = e;
                        // update current selected element in this layout
                        if (selected != null)
                        {
                            selected.SetSelected(false);
                        }
                        selected = e;
                    }
                   
                    
                }

            }
            return status;
        }



    }
}

