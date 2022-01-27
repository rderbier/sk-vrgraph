using StereoKit;
using System.Collections.Generic;

namespace RDR
{
	interface IDrawable<T>
	{
		bool DrawAtPose(Pose pose);
		bool Draw();
		T GetValue();
	}
	public class GraphTypeUIcomponent : IDrawable<TypeElement>
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
		public TypeElement GetValue()
        {
			return info;
        }
		public bool Draw()
        {
			UI.WindowBegin(info.name, ref pose, Vec2.Zero, UIWin.Empty);
			//start at center and auto expand in all directions
			UI.Toggle(info.name, ref isSelected);

			// display predicates of type uid for this type

			//UI.PopSurface();
			UI.WindowEnd();

			Vec3 dir = pose.orientation * (- Vec3.Forward);

			Vec2 size = new Vec2(30 * U.cm, 5 * U.cm);

			Vec3 nx = Vec3.Cross(dir, Vec3.Up);
			dir = Vec3.Cross(Vec3.Up,nx);
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
			return isSelected;

		}

    }
	public class UIElementInfo
	{
		public Pose pose { get; set; }
		public bool isSelected;
	}
	class GraphUi
	{

		private const float armDistance = 0.6f; // arm distance is in meter
		private const float horizontal_fov = 3f * SKMath.Pi / 5f; // horizontal binaucular field of vision angle in radian.< 120 deg 
		private static List<TypeElement> schema = null;
		private static List<TypeElement> filtered = null;
		private static IDrawable<TypeElement>[] UIcomponentArray;
		private static Vec3 initialPosition;
		static public void initSchema(Dictionary<string, TypeElement> NodeTypeMap, Vec3 pos)
		{
			List<TypeElement> graphSchema = new List<TypeElement>(NodeTypeMap.Values);
			initialPosition = pos;
			
			filtered = graphSchema.FindAll(e => e.relationCount >= 2);
			UIcomponentArray = new IDrawable<TypeElement>[filtered.Count];

			
			
			var ntypes = filtered.Count;
			
			var angle = SKMath.Pi - horizontal_fov / 2.0f;
			float panelSizeW = 25 * U.cm;
			float panelSizeH = 8 * U.cm;
			float panelAngle = panelSizeW / armDistance;

			// stereokit coordinate system is right handed ie forward is -z
			float deltaAngle = panelSizeH / armDistance;
			float prezAngle = -3 * deltaAngle;

			for (var i = 0; i < ntypes; i++)
			{
				var dh = armDistance * SKMath.Cos(prezAngle); //
				var h = armDistance * SKMath.Sin(prezAngle);
				Vec3 at = pos + new Vec3(dh * SKMath.Cos(angle), h, -(dh * SKMath.Sin(angle)));
				
				UIcomponentArray[i] = new GraphTypeUIcomponent(filtered[i], new Pose(at, Quat.LookAt(at, pos, Vec3.Up)) );
				

				angle -= panelAngle;
				if (angle < SKMath.Pi / 6f)
				{
					angle = SKMath.Pi - horizontal_fov / 2.0f; // restart from left
					h += panelSizeH;
					prezAngle += deltaAngle; // next row angle.

				}
			}
			schema = graphSchema;

		}
		static public void displayNodeList(List<Node> nodeList, Pose pose)
        {
			if (nodeList != null)
			{
				Vec3 position = pose.position;
				Quat orientation = pose.orientation;

				foreach (Node node in nodeList)
				{
					displayNode(node, new Pose(position, orientation));
					position = position - 15 * U.cm * Vec3.Forward * orientation;
				}
			}
        }
		static private void displayNode(Node node, Pose pose)
        {
			Pose windowPose = new Pose(pose.position,pose.orientation);
			UI.WindowBegin(node.uid, ref windowPose, Vec2.Zero, UIWin.Body);
			foreach (NodeScalarAttribute a in node.attributes) {
				UI.Label($"{a.name} : {a.value}");
		    }
			foreach (NodeRelation r in node.relations)
			{
			    UI.Label($"{r.predicate} : {r.node.name}");	
			}
			UI.WindowEnd();
		}
		static public string selectTypeInSchema()
		{
			string selected = null;
			// display all schema objects around a position
			if (schema != null)
			{
				foreach (var e in UIcomponentArray)
                {
					var isSelected = e.Draw();
					if (isSelected)
                    {
						selected = e.GetValue().name;
                    }
                }
			}
			return selected;
		
		}
	}
}