using FrooxEngine;

namespace NeosAnimationToolset
{
    public interface ITrackable
    {
        void OnStart(RecordingTool rt);
        void OnUpdate(float T);
        void OnStop();
        void OnReplace(Animator anim);
        void Clean();
    }
}
