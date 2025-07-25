using System;

namespace PetViewerLinux
{
    public class PetActivity
    {
        public string Name { get; }
        public string StartMenuText { get; }
        public string StopMenuText { get; }
        public string AnimationPath { get; }
        public AnimationState StartState { get; }
        public AnimationState LoopState { get; }
        public AnimationState EndState { get; }

        public PetActivity(
            string name, 
            string startMenuText, 
            string stopMenuText, 
            string animationPath,
            AnimationState startState,
            AnimationState loopState,
            AnimationState endState)
        {
            Name = name;
            StartMenuText = startMenuText;
            StopMenuText = stopMenuText;
            AnimationPath = animationPath;
            StartState = startState;
            LoopState = loopState;
            EndState = endState;
        }
    }
}
