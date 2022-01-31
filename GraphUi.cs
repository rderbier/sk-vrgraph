using StereoKit;
using System.Collections.Generic;
using RDR.Dgraph;
namespace RDR.GraphUI
{
    public class UIElementInfo
	{
		public Pose pose { get; set; }
		public bool isSelected;
	}
	class GraphDisplayer
	{

		private const float armDistance = 0.6f; // arm distance is in meter
		private const float horizontal_fov = 3f * SKMath.Pi / 5f; // horizontal binaucular field of vision angle in radian.< 120 deg 
		private static List<TypeElement> schema = null;
		private static List<TypeElement> filtered = null;
		private static List<IDrawable> UIcomponentList;
		private static Vec3 initialPosition;
		
		static public void initSchema(Dictionary<string, TypeElement> NodeTypeMap, Pose pose)
		{
			List<TypeElement> graphSchema = new List<TypeElement>(NodeTypeMap.Values);
			initialPosition = pose.position;
			
			filtered = graphSchema.FindAll(e => e.relationCount >= 2);
			UIcomponentList = GraphTypeUtils.CreateGraphTypeUIElementList(filtered, pose.position, armDistance);

			
			schema = graphSchema;

		}
	
		
		static public string selectTypeInSchema()
		{
			string selected = null;
			// display all schema objects around a position
			if (schema != null)
			{
				foreach (var e in UIcomponentList)
                {
					var isSelected = e.Draw();
					if (isSelected)
                    {
						selected = (e.GetValue() as TypeElement).name;
                    }
                }
			}
			return selected;
		
		}
	}
}