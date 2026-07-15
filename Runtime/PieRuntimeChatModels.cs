using System;

namespace Pie
{
    /// <summary>
    /// A runtime chat request. content is sent to the model while displayContent
    /// is the player-facing text stored in the session timeline.
    /// </summary>
    [Serializable]
    public sealed class PieChatRequest
    {
        public string content = "";
        public string displayContent = "";
        public string clientTurnId = "";
    }

    [Serializable]
    public sealed class PieTimelineItem
    {
        public int schemaVersion = 1;
        public string sessionId = "";
        public string clientTurnId = "";
        public string itemId = "";
        public string kind = "";
        public string status = "";
        public long startedAt;
        public long updatedAt;
        public long completedAt;
        public string text = "";
        public string summary = "";
        public string toolCallId = "";
        public string toolName = "";
        public string argsJson = "";
        public string resultText = "";
        public string detailsJson = "";
        public bool isError;
        public bool isTruncated;
    }

    [Serializable]
    public sealed class PieSessionContentBlock
    {
        public string type = "";
        public string text = "";
    }

    [Serializable]
    public sealed class PieSessionMessage
    {
        public string role = "";
        public long timestamp;
        public string displayContent = "";
        public string clientTurnId = "";
        public string toolCallId = "";
        public string toolName = "";
        public bool isError;
        public PieSessionContentBlock[] content = Array.Empty<PieSessionContentBlock>();
        public string errorMessage = "";
        public string stopReason = "";
    }

    [Serializable]
    public sealed class PieSessionSnapshot
    {
        public int schemaVersion = 1;
        public string id = "";
        public string title = "";
        public string createdAt = "";
        public string updatedAt = "";
        public int messageCount;
        public PieSessionMessage[] messages = Array.Empty<PieSessionMessage>();
        public PieTimelineItem[] timelineItems = Array.Empty<PieTimelineItem>();
    }

    [Serializable]
    public sealed class PieAgentStatus
    {
        public string phase = "idle";
        public string turnState = "completed";
        public string activeToolName = "";
        public string lastStopReason = "";
        public bool hasPendingToolContinuation;
    }

    [Serializable]
    public sealed class PieRuntimeError
    {
        public string code = "";
        public string category = "";
        public string message = "";
        public bool retryable;
        public string sessionId = "";
        public string clientTurnId = "";
        public bool hadToolActivity;
    }

    [Serializable]
    public sealed class PieTurnResult
    {
        public string sessionId = "";
        public string clientTurnId = "";
        public string outcome = "";
        public string stopReason = "";
    }
}
