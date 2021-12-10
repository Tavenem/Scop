﻿namespace Scop;

public interface IWeighted
{
    public double EffectiveWeight => Weight ?? 1;
    double? Weight { get; set; }
}
