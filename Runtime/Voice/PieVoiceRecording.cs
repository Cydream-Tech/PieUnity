using System;
using System.Threading;
using UnityEngine;

namespace Pie
{
    public sealed class PieVoiceRecording
    {
        private enum RecordingState
        {
            Created,
            Recording,
            Encoding,
            Uploading,
            Completed,
            Failed,
            Cancelled,
        }

        private readonly PieVoiceRequest _request;
        private readonly Action<PieVoiceResult> _callback;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private RecordingState _state = RecordingState.Created;
        private AudioClip _clip;
        private string _deviceName;
        private float _startedAt;
        private int _terminal;

        public bool IsRecording
        {
            get { return _state == RecordingState.Recording; }
        }

        public bool IsCompleted
        {
            get
            {
                return _state == RecordingState.Completed
                    || _state == RecordingState.Failed
                    || _state == RecordingState.Cancelled;
            }
        }

        internal PieVoiceRecording(PieVoiceRequest request, Action<PieVoiceResult> callback)
        {
            _request = request ?? new PieVoiceRequest();
            _callback = callback;
        }

        internal void Begin()
        {
            PieVoiceRuntimePump.RunOnMainThread(BeginOnMainThread);
        }

        public void End()
        {
            PieVoiceRuntimePump.RunOnMainThread(EndOnMainThread);
        }

        public void Cancel()
        {
            PieVoiceRuntimePump.RunOnMainThread(CancelOnMainThread);
        }

        internal void Tick()
        {
            if (_state != RecordingState.Recording)
                return;

            var maxSeconds = Mathf.Max(1, _request.MaxSeconds);
            if (Time.realtimeSinceStartup - _startedAt >= maxSeconds)
                EndOnMainThread();
        }

        private void BeginOnMainThread()
        {
            if (_state != RecordingState.Created)
                return;

            if (!PieVoice.ValidateOptions(_request, Complete))
            {
                _state = RecordingState.Failed;
                return;
            }

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Complete(PieVoiceResult.Failure(
                    PieVoiceErrorCode.MicrophonePermissionDenied,
                    "Microphone permission has not been granted. Call PieVoice.RequestMicrophonePermission before recording."));
                return;
            }

            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.MicrophoneUnavailable, "No microphone devices are available."));
                return;
            }

            _deviceName = string.IsNullOrWhiteSpace(_request.DeviceName) ? devices[0] : _request.DeviceName;
            if (!DeviceExists(_deviceName, devices))
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.MicrophoneUnavailable, $"Microphone device not found: {_deviceName}"));
                return;
            }

            if (Microphone.IsRecording(_deviceName))
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.RecordingAlreadyActive, $"Microphone is already recording: {_deviceName}"));
                return;
            }

            var sampleRate = Mathf.Max(8000, _request.SampleRate);
            var maxSeconds = Mathf.Max(1, _request.MaxSeconds);
            try
            {
                _clip = Microphone.Start(_deviceName, false, maxSeconds, sampleRate);
            }
            catch (Exception ex)
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.MicrophoneUnavailable, ex.Message));
                return;
            }

            if (_clip == null)
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.MicrophoneUnavailable, "Microphone failed to start recording."));
                return;
            }

            _startedAt = Time.realtimeSinceStartup;
            _state = RecordingState.Recording;
            PieVoiceRuntimePump.Track(this);
        }

        private void EndOnMainThread()
        {
            if (_state != RecordingState.Recording)
                return;

            PieVoiceRuntimePump.Untrack(this);
            _state = RecordingState.Encoding;

            var clip = _clip;
            var elapsedSeconds = Mathf.Max(0.0f, Time.realtimeSinceStartup - _startedAt);
            var frameCount = 0;
            try
            {
                if (Microphone.IsRecording(_deviceName))
                    frameCount = Microphone.GetPosition(_deviceName);
            }
            catch
            {
                frameCount = 0;
            }

            try
            {
                Microphone.End(_deviceName);
            }
            catch
            {
                // Some platforms report an already-ended microphone when MaxSeconds is reached.
            }

            if (clip == null)
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.InternalError, "Recording clip was not available."));
                return;
            }

            if (frameCount <= 0)
                frameCount = Mathf.Clamp(Mathf.CeilToInt(elapsedSeconds * clip.frequency), 0, clip.samples);
            frameCount = Mathf.Clamp(frameCount, 0, clip.samples);
            var durationSeconds = clip.frequency > 0 ? frameCount / (float)clip.frequency : elapsedSeconds;

            if (durationSeconds < Mathf.Max(0.0f, _request.MinRecordingSeconds))
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.RecordingTooShort, "Recording is too short.", durationSeconds));
                return;
            }

            var samples = PieVoiceRecorder.ExtractSamples(clip, frameCount);
            if (samples.Length == 0)
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.RecordingTooShort, "Recording did not contain audio samples.", durationSeconds));
                return;
            }

            if (_request.EnableSilenceGate && PieVoiceRecorder.IsSilent(samples, _request.SilenceRmsThreshold))
            {
                Complete(PieVoiceResult.Failure(PieVoiceErrorCode.SilentAudio, "Recording is silent.", durationSeconds));
                return;
            }

            var wavBytes = PieWavEncoder.Encode(samples, clip.channels, clip.frequency);
            _state = RecordingState.Uploading;
            PieVoice.UploadBytes(
                _request,
                wavBytes,
                "audio/wav",
                "recording.wav",
                durationSeconds,
                _cancellation.Token,
                Complete);
        }

        private void CancelOnMainThread()
        {
            if (IsCompleted)
                return;

            PieVoiceRuntimePump.Untrack(this);
            _cancellation.Cancel();
            if (_state == RecordingState.Recording && !string.IsNullOrEmpty(_deviceName))
            {
                try
                {
                    Microphone.End(_deviceName);
                }
                catch
                {
                    // Ignore platform-specific microphone teardown errors during cancel.
                }
            }

            Complete(PieVoiceResult.Failure(PieVoiceErrorCode.Cancelled, "Voice recording was cancelled."));
        }

        private void Complete(PieVoiceResult result)
        {
            if (Interlocked.Exchange(ref _terminal, 1) != 0)
                return;

            PieVoiceRuntimePump.Untrack(this);
            if (result != null && result.Ok)
                _state = RecordingState.Completed;
            else if (result != null && result.ErrorCode == PieVoiceErrorCode.Cancelled)
                _state = RecordingState.Cancelled;
            else
                _state = RecordingState.Failed;

            PieVoice.Dispatch(_callback, result);
        }

        private static bool DeviceExists(string deviceName, string[] devices)
        {
            if (devices == null || devices.Length == 0)
                return false;
            for (var i = 0; i < devices.Length; i++)
            {
                if (string.Equals(devices[i], deviceName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
