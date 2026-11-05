using System;
using System.Threading;

namespace VitalSigns.Core;

/// <summary>
/// When the stat change hook goes in: not while every multiplier is 1, since it would run on every stat change of
/// every character for nothing; the first time one is set off 1; and never a second time. Turning the multipliers back
/// to 1 leaves the hook in, and it then returns at once.
/// </summary>
public sealed class DifficultyHookGate
{
    private readonly Action _install;
    private int _isIn;

    public DifficultyHookGate(Action install)
    {
        _install = install;
    }

    public bool IsIn => Volatile.Read(ref _isIn) != 0;

    public void Want(Difficulty wanted)
    {
        if (wanted.IsOff || Interlocked.Exchange(ref _isIn, 1) != 0) return;

        _install();
    }
}
