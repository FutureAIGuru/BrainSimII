using System;

namespace Touchless.Vision.Contracts
{
    public interface IFrameSource : ITouchlessAddIn
    {
        event Action<IFrameSource, Frame, double> NewFrame;

        bool StartFrameCapture();
        void StopFrameCapture();
    }
}
