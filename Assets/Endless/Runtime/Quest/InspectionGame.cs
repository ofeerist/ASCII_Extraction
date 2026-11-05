using System;
namespace Pawnshop.Endless
{
    // Spatial progress is independent of rendering/input so pauses, jumps and repeats can be tested.
    public sealed class InspectionGame
    {
        public InspectionTool Tool { get; }
        public readonly float[] coverage = new float[3];
        public float Scratch { get; private set; }
        public float Reaction { get; private set; }
        bool scratching;
        public InspectionGame(InspectionTool tool) { Tool=tool; }
        public bool Complete => Tool==InspectionTool.GoldReagent ? Reaction>=1 : coverage[0]>=1&&coverage[1]>=1&&coverage[2]>=1;
        public float Progress => Tool==InspectionTool.GoldReagent ? (Scratch+Reaction)/2 : (coverage[0]+coverage[1]+coverage[2])/3;
        public void Scan(int region,float dt,bool held)
        {
            if(!held || region<0 || region>2 || Tool==InspectionTool.GoldReagent || float.IsNaN(dt))return;
            coverage[region]=Math.Min(1,coverage[region]+Math.Clamp(dt,0,.05f)/.65f);
        }
        public void ApplyReagent(float position,bool onStrip,float dt,bool held)
        {
            if(Tool!=InspectionTool.GoldReagent || Complete)return;
            if(!held || !onStrip){scratching=false;return;}
            if(Scratch<1)
            {
                if(!scratching){if(position>.12f)return;scratching=true;}
                if(position<0 || position>1 || position>Scratch+.15f)return;
                Scratch=Math.Max(Scratch,position);if(Scratch>=.95f)Scratch=1;
            }
            else if(position>.35f && position<.65f && !float.IsNaN(dt))Reaction=Math.Min(1,Reaction+Math.Clamp(dt,0,.05f));
        }
    }
}
