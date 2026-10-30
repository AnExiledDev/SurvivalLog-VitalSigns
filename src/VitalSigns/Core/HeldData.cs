namespace VitalSigns.Core;

/// <summary>
/// Remembers which reading the page holds, so an unchanged reading is not sent again. The page only holds a reading its
/// script answered for; any other answer (a missing script, no frame, no answer at all) means it may hold nothing, and
/// the next call sends the reading in full.
/// </summary>
public sealed class HeldData
{
    private string? _held;
    private string? _inFlight;
    private bool _isAwaiting;

    /// <summary>Whether the page answered for exactly this reading, the last one it took.</summary>
    public bool Holds(string data) => _held == data && !_isAwaiting;

    /// <summary>The reading to put in this call, or null when the page already holds it.</summary>
    public string? ToSend(string data)
    {
        Begin(data);

        return data == _held ? null : data;
    }

    /// <summary>An install brings a fresh script with nothing in it, so the reading always goes.</summary>
    public string ToInstall(string data)
    {
        Begin(data);

        return data;
    }

    /// <summary>The answer to the call now out, or a <see cref="PageAnswer"/> the plugin made up when the call never left.</summary>
    public void Answered(string answer)
    {
        _held = IsFromScript(answer) ? _inFlight : null;
        _inFlight = null;
        _isAwaiting = false;
    }

    private void Begin(string data)
    {
        // The last call never got an answer: whether the page took its reading is unknown.
        if (_isAwaiting) _held = null;

        _inFlight = data;
        _isAwaiting = true;
    }

    private static bool IsFromScript(string answer) => answer is PageAnswer.Ok or PageAnswer.NoBar or PageAnswer.NoPlayer;
}
