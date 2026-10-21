using System;
using UnityEngine;
using UI.Utilites;

namespace UI.Common
{
    [Serializable] public class ProgressBarUIParameters
    {
        public Color BackgroundColor=ColorUtils.FromHex("3B697E"),ProgressColor=ColorUtils.FromHex("5EA4BB");
        public int ProgressWidth=6,LevelWidth=6;
        public bool ShowNumbers,ShowAnimation;
        public bool ShowProgress=true;
        public int AnimationCardCount=1;
        public int CurrentLevel,MaxLevel,BoostedLevel,CurrentProgress,MaxProgress;
        public int[] LevelRequirements=new int[0];
        public string BoostTimerId;
        public float BoostDuration;
    }
}
