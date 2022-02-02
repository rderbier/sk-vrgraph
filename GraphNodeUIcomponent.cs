using StereoKit;
using System;
using System.Collections.Generic;
using RDR.GraphUI;

namespace RDR
{
	
	public class GraphNodeUIcomponent : IDrawable
    {
		private Node _node;
		private bool isSelected = false;
		private Pose _pose;
		private float _width;
		private Pose _lerpTo;
		private float lerpCurrentTime;
		private float lerpDuration;
		private bool isLerping = false;
		static public List<IDrawable> buildGraphNodeUIcomponentList(List<Node> listOfNodes)
        {
			List<IDrawable> UIcomponentList = new List<IDrawable>();
			foreach (Node n in listOfNodes)
            {
				UIcomponentList.Add(new GraphNodeUIcomponent(n, new Pose (Vec3.Zero,Quat.Identity), 25 *U.cm));
            }
			return UIcomponentList;

		}
		public GraphNodeUIcomponent(Node node, Pose pose, float width = 0f )
        {
			_node = node;
			_pose = pose; 
			_width = width;
			
        }
		public bool DrawAtPose(Pose newPose)
		{
			if (!isLerping)
			{
				_pose = newPose;
			}
			return Draw();
		}
		public Object GetValue()
        {
			return _node;
        }
		public void LerpTo(Pose toPose, float duration)
        {
			_lerpTo = toPose;
			lerpDuration = duration;
		     lerpCurrentTime = 0f;
			isLerping = true;
			Log.Warn("start lerping ");

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
		public bool Draw()
        {
			bool isSelectedNow = false;
			Pose windowPose;
			if (isLerping)
            {
				lerpCurrentTime += (float)Time.Elapsed;
				float percent = lerpDuration == 0 ? 0 : Math.Min(1, lerpCurrentTime / lerpDuration);
				windowPose = Pose.Lerp(_pose, _lerpTo, percent);
				if (percent == 1f)
                {
					isLerping = false;
					_pose = _lerpTo;
					Log.Warn("end lerping");
                }

			} else
            {
				windowPose = new Pose(_pose.position, _pose.orientation);
			}
			
			Vec2 labelSize = new Vec2(_width - 2*U.cm, 0);
			UI.WindowBegin(_node.name, ref windowPose, new Vec2(_width,0), UIWin.Normal);
			foreach (NodeScalarAttribute a in _node.attributes)
			{
				UI.Label($"{a.name} : {a.value}",labelSize);
			}
			//foreach (NodeRelation r in _node.relations)
			//{
			//	UI.Label($"{r.predicate} -> {r.node.name}");
			//}
			foreach(String predicate in _node.edges.Keys)
            {
				List<Node> relations = _node.edges[predicate];
				if (relations.Count == 1)
                {
					UI.Label($"{predicate} -> {relations[0].name}");
				} else
                {
					UI.Label($"{predicate} -> {relations.Count}");
				}
            }
			UI.WindowEnd();
			
			return isSelectedNow;

		}

    }
}