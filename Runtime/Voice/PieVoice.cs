using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Pie
{
    public static class PieVoice
    {
        public static PieVoiceRecording BeginRecording(PieVoiceRequest request, Action<PieVoiceResult> callback)
        {
            PieVoiceRuntimePump.Ensure();
            var recording = new PieVoiceRecording(request ?? new PieVoiceRequest(), callback);
            recording.Begin();
            return recording;
        }

        public static void RequestMicrophonePermission(Action<bool> callback)
        {
            PieVoiceRuntimePump.RequestMicrophonePermission(callback);
        }

        public static void TranscribeAudioClip(PieVoiceClipRequest request, Action<PieVoiceResult> callback)
        {
            PieVoiceRuntimePump.Ensure();
            PieVoiceRuntimePump.RunOnMainThread(() => TranscribeAudioClipOnMainThread(request, callback));
        }

        public static void TranscribeFile(PieVoiceFileRequest request, Action<PieVoiceResult> callback)
        {
            PieVoiceRuntimePump.Ensure();
            var normalized = request ?? new PieVoiceFileRequest();
            if (!ValidateOptions(normalized, callback))
                return;

            if (string.IsNullOrWhiteSpace(normalized.FilePath) || !File.Exists(normalized.FilePath))
            {
                Dispatch(callback, PieVoiceResult.Failure(
                    PieVoiceErrorCode.MissingAudioSource,
                    "Audio file was not found."));
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var audioBytes = File.ReadAllBytes(normalized.FilePath);
                    var responseJson = await PieVoiceHttpClient.TranscribeAsync(
                        normalized,
                        audioBytes,
                        string.IsNullOrWhiteSpace(normalized.MimeType) ? "audio/wav" : normalized.MimeType,
                        string.IsNullOrWhiteSpace(normalized.SourceName) ? Path.GetFileName(normalized.FilePath) : normalized.SourceName,
                        CancellationToken.None);
                    Dispatch(callback, BuildResultFromResponse(responseJson, 0.0f));
                }
                catch (Exception ex)
                {
                    Dispatch(callback, PieVoiceResult.Failure(PieVoiceErrorCode.UploadFailed, ex.Message));
                }
            });
        }

        internal static bool ValidateOptions(PieVoiceOptions options, Action<PieVoiceResult> callback)
        {
            if (options == null)
            {
                Dispatch(callback, PieVoiceResult.Failure(PieVoiceErrorCode.InternalError, "PieVoice options are required."));
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.VirtualKey))
            {
                Dispatch(callback, PieVoiceResult.Failure(PieVoiceErrorCode.MissingVirtualKey, "Voice virtual key is required."));
                return false;
            }

            if (options.TimeoutSeconds <= 0)
                options.TimeoutSeconds = 120;

            return true;
        }

        internal static void Dispatch(Action<PieVoiceResult> callback, PieVoiceResult result)
        {
            if (callback == null)
                return;

            PieVoiceRuntimePump.PostToMainThread(() => callback(result ?? PieVoiceResult.Failure(
                PieVoiceErrorCode.InternalError,
                "Voice transcription returned no result.")));
        }

        internal static PieVoiceResult BuildResultFromResponse(string responseJson, float durationSeconds)
        {
            try
            {
                var payload = JsonUtility.FromJson<PieVoiceTranscribePayload>(responseJson ?? "{}");
                if (payload == null)
                    return PieVoiceResult.Failure(PieVoiceErrorCode.InvalidResponse, "Voice transcription returned an empty response.", durationSeconds);

                return PieVoiceResult.Success(
                    payload.rawText,
                    payload.polishedText,
                    payload.warning,
                    payload.asrModel,
                    payload.llmModel,
                    payload.mode,
                    payload.contextHint,
                    payload.tone,
                    payload.targetLanguage,
                    durationSeconds);
            }
            catch (Exception ex)
            {
                return PieVoiceResult.Failure(PieVoiceErrorCode.InvalidResponse, $"Invalid voice transcription response: {ex.Message}", durationSeconds);
            }
        }

        private static void TranscribeAudioClipOnMainThread(PieVoiceClipRequest request, Action<PieVoiceResult> callback)
        {
            var normalized = request ?? new PieVoiceClipRequest();
            if (!ValidateOptions(normalized, callback))
                return;

            var clip = normalized.Clip;
            if (clip == null)
            {
                Dispatch(callback, PieVoiceResult.Failure(
                    PieVoiceErrorCode.MissingAudioSource,
                    "AudioClip is required."));
                return;
            }

            var durationSeconds = clip.length;
            if (normalized.MinDurationSeconds > 0.0f && durationSeconds < normalized.MinDurationSeconds)
            {
                Dispatch(callback, PieVoiceResult.Failure(
                    PieVoiceErrorCode.RecordingTooShort,
                    "AudioClip is shorter than the configured minimum duration.",
                    durationSeconds));
                return;
            }

            var samples = PieVoiceRecorder.ExtractSamples(clip, clip.samples);
            if (normalized.EnableSilenceGate && PieVoiceRecorder.IsSilent(samples, normalized.SilenceRmsThreshold))
            {
                Dispatch(callback, PieVoiceResult.Failure(
                    PieVoiceErrorCode.SilentAudio,
                    "AudioClip is silent.",
                    durationSeconds));
                return;
            }

            var wavBytes = PieWavEncoder.Encode(samples, clip.channels, clip.frequency);
            UploadBytes(normalized, wavBytes, "audio/wav", normalized.SourceName, durationSeconds, CancellationToken.None, callback);
        }

        internal static void UploadBytes(
            PieVoiceOptions options,
            byte[] audioBytes,
            string mimeType,
            string sourceName,
            float durationSeconds,
            CancellationToken cancellationToken,
            Action<PieVoiceResult> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var responseJson = await PieVoiceHttpClient.TranscribeAsync(
                        options,
                        audioBytes,
                        mimeType,
                        sourceName,
                        cancellationToken);
                    Dispatch(callback, BuildResultFromResponse(responseJson, durationSeconds));
                }
                catch (OperationCanceledException)
                {
                    var cancelledByCaller = cancellationToken.IsCancellationRequested;
                    Dispatch(callback, PieVoiceResult.Failure(
                        cancelledByCaller ? PieVoiceErrorCode.Cancelled : PieVoiceErrorCode.UploadFailed,
                        cancelledByCaller ? "Voice transcription was cancelled." : "Voice transcription timed out.",
                        durationSeconds));
                }
                catch (Exception ex)
                {
                    Dispatch(callback, PieVoiceResult.Failure(PieVoiceErrorCode.UploadFailed, ex.Message, durationSeconds));
                }
            });
        }

        [Serializable]
        private sealed class PieVoiceTranscribePayload
        {
            public string rawText;
            public string polishedText;
            public string asrModel;
            public string llmModel;
            public string mode;
            public string contextHint;
            public string tone;
            public string targetLanguage;
            public string warning;
        }
    }
}
