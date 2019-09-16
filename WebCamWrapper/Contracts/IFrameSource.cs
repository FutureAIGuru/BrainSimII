//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
