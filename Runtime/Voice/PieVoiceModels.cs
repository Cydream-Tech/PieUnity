using System;
using UnityEngine;

namespace Pie
{
    public enum PieVoiceMode
    {
        Clean,
        Structure,
        Compose,
    }

    public enum PieVoiceContextHint
    {
        Chat,
        Email,
        Support,
        Note,
        Task,
        Code,
        Social,
    }

    public enum PieVoiceTone
    {
        Neutral,
        Casual,
        Formal,
        Polite,
        Concise,
    }

    public enum PieVoiceErrorCode
    {
        None,
        MissingVirtualKey,
        MissingAudioSource,
        MicrophoneUnavailable,
        MicrophonePermissionDenied,
        RecordingAlreadyActive,
        RecordingTooShort,
        SilentAudio,
        Cancelled,
        UploadFailed,
        InvalidResponse,
        InternalError,
    }

    [Serializable]
    public class PieVoiceOptions
    {
        public string ApiBaseUrl = "https://token.magicshell.ai";
        public string VirtualKey = "";
        public PieVoiceMode Mode = PieVoiceMode.Structure;
        public PieVoiceContextHint ContextHint = PieVoiceContextHint.Task;
        public PieVoiceTone Tone = PieVoiceTone.Concise;
        public string Language = "auto";
        public string TargetLanguage = "";
        public string PreserveTerms = "";
        public string AsrModel = "";
        public string LlmModel = "";
        public int TimeoutSeconds = 120;
    }

    [Serializable]
    public sealed class PieVoiceRequest : PieVoiceOptions
    {
        public string DeviceName = "";
        public int SampleRate = 16000;
        public int MaxSeconds = 20;
        public float MinRecordingSeconds = 0.4f;
        public bool EnableSilenceGate = true;
        public float SilenceRmsThreshold = 0.01f;
    }

    [Serializable]
    public sealed class PieVoiceFileRequest : PieVoiceOptions
    {
        public string FilePath = "";
        public string MimeType = "audio/wav";
        public string SourceName = "";
    }

    [Serializable]
    public sealed class PieVoiceClipRequest : PieVoiceOptions
    {
        public AudioClip Clip;
        public string SourceName = "recording.wav";
        public float MinDurationSeconds = 0.0f;
        public bool EnableSilenceGate = false;
        public float SilenceRmsThreshold = 0.01f;
    }

    [Serializable]
    public sealed class PieVoiceResult
    {
        public bool Ok;
        public PieVoiceErrorCode ErrorCode = PieVoiceErrorCode.None;
        public string ErrorMessage = "";
        public string RawText = "";
        public string PolishedText = "";
        public string Warning = "";
        public string AsrModel = "";
        public string LlmModel = "";
        public string Mode = "";
        public string ContextHint = "";
        public string Tone = "";
        public string TargetLanguage = "";
        public float DurationSeconds;

        public static PieVoiceResult Success(
            string rawText,
            string polishedText,
            string warning,
            string asrModel,
            string llmModel,
            string mode,
            string contextHint,
            string tone,
            string targetLanguage,
            float durationSeconds)
        {
            return new PieVoiceResult
            {
                Ok = true,
                ErrorCode = PieVoiceErrorCode.None,
                RawText = rawText ?? "",
                PolishedText = string.IsNullOrEmpty(polishedText) ? (rawText ?? "") : polishedText,
                Warning = warning ?? "",
                AsrModel = asrModel ?? "",
                LlmModel = llmModel ?? "",
                Mode = mode ?? "",
                ContextHint = contextHint ?? "",
                Tone = tone ?? "",
                TargetLanguage = targetLanguage ?? "",
                DurationSeconds = durationSeconds,
            };
        }

        public static PieVoiceResult Failure(PieVoiceErrorCode errorCode, string errorMessage, float durationSeconds = 0.0f)
        {
            return new PieVoiceResult
            {
                Ok = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage ?? "",
                DurationSeconds = durationSeconds,
            };
        }
    }
}
