﻿using System;
using System.Collections.Generic;
using System.Text;

namespace imageStacker.Core.Test.Unit.ByteImage
{
   public interface IImageProvider<T> where T : IProcessableImage
    {
        T PrepareEmptyImage();
        T PrepareNoisyImage();
        T PreparePrefilledImage(int value);
    }
}
