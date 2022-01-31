using StereoKit;
using System;
using System.Collections.Generic;
using RDR.GraphUI;

namespace RDR
{
    public class VshapeLayout : ILayout
    {
        private int size;
        private int _page,_pageSize;
        private Pose _pose;
        private int index;
        private float _angle;
        private float _elementWidth;
        private bool trackGesture = false;
        private Vec3 slidePosition;
        private Vec3 rightDir;
        private Vec3 leftDir;
        private Vec3 xaxis;
        List<IDrawable> elementList;

        public VshapeLayout(Pose pose, int _size, float angleInRadian, float elementWidth = 0f)
        {
            _pose = pose;
            _angle = angleInRadian;
            _elementWidth = elementWidth;
            rightDir = new Vec3(-SKMath.Sin(_angle), 0, SKMath.Cos(_angle));
            leftDir = new Vec3(SKMath.Sin(_angle), 0, SKMath.Cos(_angle));
            xaxis = Vec3.UnitX * _pose.orientation;
        }
        public void SetElementList(List<IDrawable> _elementList, int focusIndex, int page, int pageSize)
        {
            elementList = _elementList;
            if (focusIndex >= elementList.Count) { focusIndex = elementList.Count - 1; }
            index = focusIndex;
            _page = page;
            _pageSize = pageSize;


        }
        public LayoutStatus DrawAtPose(Pose pose)
        {
            _pose = pose;
            return Draw();

        }
        private Pose GetPoseForIndex(int i, float deltaX)
        {
            Pose pose;
            Quat orientation;
            Quat panelOrientation;
            Vec3 elementPosition;
            if (i > index)
            {
                Vec3 origin = new Vec3(-(_elementWidth / 2f), 0f, 0f) - (_elementWidth / 2f) * rightDir;
                panelOrientation = Quat.FromAngles(0f, 45f, 0f);

                if ((deltaX > 0f) && (i == index + 1)) // first next card
                {
                   
                    orientation = _pose.orientation * panelOrientation;
                    Vec3 relativePosition = origin + ((i - index) * _elementWidth) * rightDir;
                    elementPosition = _pose.position + _pose.orientation * relativePosition;
                    Pose normalPose = new Pose(elementPosition, orientation);
                    pose = Pose.Lerp(normalPose, _pose, deltaX / _elementWidth );
                    
                    
                }
                else
                {
                    orientation = _pose.orientation * panelOrientation;
                    Vec3 relativePosition = origin + ((i - index) * _elementWidth - deltaX) * rightDir;
                    elementPosition = _pose.position + _pose.orientation * relativePosition;
                    pose = new Pose(elementPosition, orientation);
                }
                
            }
            else if (i <index)
            {
                Vec3 origin = new Vec3((_elementWidth / 2f), 0f, 0f) - (_elementWidth / 2f) * leftDir; 
                panelOrientation = Quat.FromAngles(0f, -45f, 0f);

                if ((deltaX < 0f) && (i == index - 1)) // first next card
                {
                    
                    orientation = _pose.orientation * panelOrientation;
                    Vec3 relativePosition = origin + ((index-i) * _elementWidth) * leftDir;
                    elementPosition = _pose.position + _pose.orientation * relativePosition;
                    Pose normalPose = new Pose(elementPosition, orientation);
                    pose = Pose.Lerp(normalPose, _pose, (-deltaX) / _elementWidth );

                }
                else
                {
                    orientation = _pose.orientation * panelOrientation;
                    Vec3 relativePosition = origin + ((index - i) * _elementWidth + deltaX) * leftDir;
                    elementPosition = _pose.position + _pose.orientation * relativePosition;
                    pose = new Pose(elementPosition, orientation);

                }
                


            } else // central element
            {
                pose = new Pose(_pose.position + xaxis * deltaX, _pose.orientation);
            }
            return pose;
        }



        public LayoutStatus Draw()
        {
            LayoutStatus status = new LayoutStatus();
            status.page = _page;
            status.pageSize = _pageSize;
            status.index = index;


            if (elementList != null)
            {
                Pose centralElementPose = _pose;


                Vec3 scale = new Vec3(_elementWidth, 1f, 0.01f);
                Matrix m = _pose.ToMatrix(scale);
                Bounds b = new Bounds(_pose.position, (Matrix.S(scale) * Matrix.R(_pose.orientation)) * Vec3.One);
                //Lines.AddAxis(new Pose(b.center, Quat.Identity), 0.2f, 0.005f);
                //Mesh.Cube.Draw(Material.UIBox, Matrix.S(scale)*Matrix.R(_pose.orientation)* Matrix.T(_pose.position));
                // handle touch rotation
                Hand hand = Input.Hand(Handed.Right);

                //Lines.Add(_pose.position, _pose.position + xaxis, Color.White, 0.01f);
                float deltaX = 0f;

                if (hand.IsTracked)
                {
                    Vec3 fingertip = hand[FingerId.Index, JointId.Tip].position;
                    if (b.Contains(fingertip))
                    {

                        //Mesh.Cube.Draw(Material.UIBox, m);
                        if (trackGesture == false)
                        {
                            slidePosition = fingertip;
                            trackGesture = true;

                            Log.Warn("Touched");
                        }
                        else
                        {
                            float move = Vec3.Dot(xaxis, fingertip - slidePosition);
                            if (Math.Abs(move) > 2 * U.cm)
                            { // move enough on this axis
                                deltaX = move;
                                Log.Warn("deltaX  " + deltaX);
                            }
                            if (Math.Abs(move) > (_elementWidth / 2f)) // move enough on this axis
                            {
                                Log.Warn("Change index ");
                                if (move > 0f) 
                                {
                                    moveUp();
                                }
                                else 
                                {
                                    moveDown();

                                }
                            
                                
                                slidePosition = fingertip;
                                trackGesture = false;

                            }
                        }

                    }
                    else
                    {
                        trackGesture = false;
                    }
                }
                else
                {
                    Log.Warn("hand not tracked");
                }

                centralElementPose = new Pose(_pose.position + xaxis * deltaX, _pose.orientation);

       

                for (int i = 0; i < elementList.Count; i++)
                {

                    elementList[i].DrawAtPose(GetPoseForIndex(i, deltaX));

                }

            }
            return status;
        }
        private void moveUp()
        {
            if (index < elementList.Count - 1)
            {
                index += 1;
                for (int i = 0; i < elementList.Count; i++)
                {
                    elementList[i].LerpTo(GetPoseForIndex(i, 0), .5f);
                }
            }
        }
        private void moveDown()
        {
            if (index > 0)
            {
                index -= 1;
                for (int i = 0; i < elementList.Count; i++)
                {
                    elementList[i].LerpTo(GetPoseForIndex(i, 0), .5f);
                }
            }
        }

    }
}
