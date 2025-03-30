namespace OOs;

/// <summary>
/// Defines some typical states of the stateful object whoose lifetime matches ussual workflow:
/// Stopped -> Starting -> Started -> Stopping -> Stopped -> Disposed
/// </summary>
public enum State
{
    /// <summary>
    /// Currently stopped
    /// </summary>
    Stopped,
    /// <summary>
    /// In startup transition
    /// </summary>
    Starting,
    /// <summary>
    /// Started and is up and running
    /// </summary>
    Started,
    /// <summary>
    /// In stopping transition
    /// </summary>
    Stopping,
    /// <summary>
    /// Disposed, cannot be used anymore
    /// </summary>
    Disposed
}
