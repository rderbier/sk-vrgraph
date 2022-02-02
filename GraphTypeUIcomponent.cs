using StereoKit;
using System;
using System.Collections.Generic;
using RDR.Dgraph;
using RDR.GraphUI;

namespace RDR
{
	public static class GraphTypeUtils
	{
		private const float horizontal_fov = 3f * SKMath.Pi / 5f; // horizontal binaucular field of vision angle in radian.< 120 deg 

		public static List<IDrawable> CreateGraphTypeUIElementList(List<TypeElement> list, Vec3 pos, float armDistance)
		{
			List<IDrawable> UIcomponentList = new List<IDrawable>();

			var ntypes = list.Count;

			var angle = SKMath.Pi - horizontal_fov / 2.0f;
			float panelSizeW = 25 * U.cm;
			float panelSizeH = 8 * U.cm;
			float panelAngle = panelSizeW / armDistance;

			// stereokit coordinate system is right handed ie forward is -z
			float deltaAngle = panelSizeH / armDistance;
			float prezAngle = -3 * deltaAngle;

			foreach (var typeElement in list)
			{
				var dh = armDistance * SKMath.Cos(prezAngle); //
				var h = armDistance * SKMath.Sin(prezAngle);
				Vec3 at = pos + new Vec3(dh * SKMath.Cos(angle), h, -(dh * SKMath.Sin(angle)));

				UIcomponentList.Add(new GraphTypeUIcomponent(typeElement, new Pose(at, Quat.LookAt(at, pos, Vec3.Up))));


				angle -= panelAngle;
				if (angle < SKMath.Pi / 6f)
				{
					angle = SKMath.Pi - horizontal_fov / 2.0f; // restart from left
					h += panelSizeH;
					prezAngle += deltaAngle; // next row angle.

				}
			}
			return UIcomponentList;
		}
		public static List<IDrawable> CreateGraphTypeUIElementList(List<TypeElement> list)
		{
			List<IDrawable> UIcomponentList = new List<IDrawable>();


			Pose p = new Pose();
			foreach (var typeElement in list)
			{
				
				
				UIcomponentList.Add(new GraphTypeUIcomponent(typeElement, p));

			}
			return UIcomponentList;
		}
	}
	public class GraphTypeUIcomponent : IDrawable
    {
		private TypeElement info;
		private bool isSelected = false;
		private Pose pose;
		public GraphTypeUIcomponent(TypeElement typeElement, Pose _pose)
        {
			info = typeElement;
			pose = _pose;

			
        }
		public bool DrawAtPose(Pose newPose)
		{
			pose = newPose;
			return Draw();
		}
		public bool IsSelected()
        {
			return isSelected;
        }
		public bool SetSelected(bool v)
		{
			isSelected = v;
			return this.isSelected;
		}
		public Object GetValue()
        {
			return info;
        }
		private void DisplayLabels()
        {
			Vec3 dir = pose.orientation * (-Vec3.Forward);

			Vec2 size = new Vec2(30 * U.cm, 5 * U.cm);

			Vec3 nx = Vec3.Cross(dir, Vec3.Up);
			dir = Vec3.Cross(Vec3.Up, nx);
			Vec3 labelPosition = pose.position + (-15 * U.cm * nx) + dir * 5.0f * U.cm;

			foreach (var f in info.fields)
			{
				if (f.isRelation)
				{

					UI.PushSurface(new Pose(labelPosition, pose.orientation), Vec3.Zero, size);

					UI.Text(f.name, TextAlign.TopCenter);

					UI.PopSurface();
					labelPosition = labelPosition + dir * 10.0f * U.cm;

				}
			}

		}
		public void LerpTo(Pose toPose, float duration)
		{
			
		}
		public bool Draw()
        {
			bool isSelectedNow = false;
			UI.WindowBegin(info.name, ref pose, Vec2.Zero, UIWin.Empty);
			//start at center and auto expand in all directions
			if (UI.Toggle(info.name, ref isSelected))
            {
				isSelectedNow = isSelected;
            }

			// display predicates of type uid for this type

			//UI.PopSurface();
			UI.WindowEnd();

			
			return isSelectedNow;

		}

    }
}