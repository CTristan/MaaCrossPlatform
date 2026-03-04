namespace MaaGui.Constants;

/// <summary>
/// MaaCore callback message types
/// </summary>
public enum AsstMsg
{
    /* Global Info */
    InternalError = 0,
    InitFailed,
    ConnectionInfo,
    AllTasksCompleted,
    AsyncCallInfo,
    Destroyed,

    /* TaskChain Info */
    TaskChainError = 10000,
    TaskChainStart,
    TaskChainCompleted,
    TaskChainStopped,
    TaskChainExtraInfo,

    /* SubTask Info */
    SubTaskError = 20000,
    SubTaskStart,
    SubTaskCompleted,
    SubTaskExtraInfo,
    SubTaskStopped,
}
