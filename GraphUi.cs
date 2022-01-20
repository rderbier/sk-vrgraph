using StereoKit;
using System.Collections.Generic;

namespace RDR
{
	public class UIElementInfo
	{
		public Pose pose { get; set; }
		public bool isSelected;
	}
	class GraphUi
	{

		private const float armDistance = 0.6f; // arm distance is in meter
		private const float horizontal_fov = 3f * SKMath.Pi / 5f; // horizontal binaucular field of vision angle in radian.< 120 deg 
		private static UIElementInfo[] typePoseList;
		private static List<TypeElement> schema = null;
		private static List<TypeElement> filtered = null;
		private static Vec3 initialPosition;
		static public void initSchema(List<TypeElement> graphSchema, Vec3 pos)
		{
			initialPosition = pos;
			
			filtered = graphSchema.FindAll(e => e.relationCount >= 2);
			var ntypes = filtered.Count;
			typePoseList = new UIElementInfo[ntypes];
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
				typePoseList[i] = new UIElementInfo();
				typePoseList[i].pose = new Pose(at, Quat.LookAt(at, pos, Vec3.Up));
				typePoseList[i].isSelected = false;

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
		static public string selectTypeInSchema()
		{
			string selected = null;
			// display all schema objects around a position
			if (schema != null)
			{




				for (var i = 0; i < filtered.Count; i++)
				{

					//UI.PushSurface(typePoseList[i], Vec3.Zero, Vec2.Zero);
					var pose = typePoseList[i].pose;
					UI.WindowBegin(filtered[i].name, ref pose, Vec2.Zero, UIWin.Empty);
					//start at center and auto expand in all directions
					if (UI.Toggle(filtered[i].name, ref typePoseList[i].isSelected))
					{
						if (typePoseList[i].isSelected)
						{
							selected = filtered[i].name;
							

							
						}
					}
					// display predicates of type uid for this type
					
					//UI.PopSurface();
					UI.WindowEnd();
					
					Vec3 dir = (typePoseList[i].pose.position - (initialPosition - 2 * U.cm * Vec3.Up)).Normalized;
				
					Vec2 size = new Vec2(30*U.cm, 5*U.cm);
					
					Vec3 nx = Vec3.Cross(dir,Vec3.Up);
					Vec3 labelPosition = typePoseList[i].pose.position + (-15*U.cm*nx) + dir * 5.0f * U.cm;

					foreach (var f in filtered[i].fields)
					{
						if (f.isRelation)
						{
							
							UI.PushSurface(new Pose(labelPosition, typePoseList[i].pose.orientation), Vec3.Zero, size);
							
							UI.Text(f.name,TextAlign.TopCenter);
							
							UI.PopSurface();
							labelPosition = labelPosition + dir * 10.0f * U.cm;

						}
					}
					
					typePoseList[i].pose = pose; // set the pose back. 


				}
			}
			return selected;
		
		}
	}
}