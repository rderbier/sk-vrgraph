using System;
using System.Collections.Generic;
using System.Text;
using StereoKit;

// Interfaces for the UI 
namespace RDR.GraphUI
{
	public class LayoutStatus
    {
		public IDrawable focusOn;
		public String action;
		public int page;
		public int pageSize;
		public int index;
    }
	public interface IDrawable
	{
		bool DrawAtPose(Pose pose);
		bool Draw();
		void LerpTo(Pose toPose, float duration);
		Object GetValue();
	}
	public interface ILayout
	{
		void SetElementList(List<IDrawable> elementList, int focusIndex, int page, int pageSize);
		LayoutStatus  DrawAtPose( Pose pose);
		LayoutStatus Draw();
		
	}

}
