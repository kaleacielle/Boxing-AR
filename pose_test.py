import signal
import socket
import sys
import time

import cv2
import mediapipe as mp

UDP_HOST = "127.0.0.1"
UDP_PORT = 5052

FRAME_WIDTH = 640
FRAME_HEIGHT = 480
TARGET_FPS = 15
PROCESS_EVERY_N_FRAMES = 2

POSE_LANDMARKS = (
    mp.solutions.pose.PoseLandmark.NOSE,
    mp.solutions.pose.PoseLandmark.LEFT_SHOULDER,
    mp.solutions.pose.PoseLandmark.RIGHT_SHOULDER,
    mp.solutions.pose.PoseLandmark.LEFT_ELBOW,
    mp.solutions.pose.PoseLandmark.RIGHT_ELBOW,
    mp.solutions.pose.PoseLandmark.LEFT_WRIST,
    mp.solutions.pose.PoseLandmark.RIGHT_WRIST,
)

running = True


def stop_handler(signum, frame):
    global running
    running = False


def encode_landmarks(landmarks):
    values = []
    for landmark_index in POSE_LANDMARKS:
        landmark = landmarks[landmark_index.value]
        values.extend((f"{landmark.x:.6f}", f"{landmark.y:.6f}"))
    return ",".join(values).encode("utf-8")


def try_open_camera(index, backend):
    camera = cv2.VideoCapture(index, backend)
    if not camera.isOpened():
        camera.release()
        return None

    camera.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
    camera.set(cv2.CAP_PROP_FPS, TARGET_FPS)

    # Do a quick read to confirm the camera is actually delivering frames.
    success, _ = camera.read()
    if not success:
        camera.release()
        return None

    print(f"Camera opened using index {index} and backend {backend}.", flush=True)
    return camera


def open_camera():
    backends = []
    if sys.platform == "darwin":
        backends.extend([
            ("AVFoundation", cv2.CAP_AVFOUNDATION),
            ("Default", cv2.CAP_ANY),
        ])
    elif sys.platform.startswith("win"):
        backends.extend([
            ("DirectShow", cv2.CAP_DSHOW),
            ("Media Foundation", cv2.CAP_MSMF),
            ("Default", cv2.CAP_ANY),
        ])
    else:
        backends.extend([
            ("V4L2", cv2.CAP_V4L2),
            ("Default", cv2.CAP_ANY),
        ])

    for backend_name, backend in backends:
        for index in range(0, 10):
            camera = try_open_camera(index, backend)
            if camera is not None:
                return camera

        # Some cameras require the default backend on index 0 without a backend.
        fallback = cv2.VideoCapture(0)
        if fallback.isOpened():
            fallback.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
            fallback.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
            fallback.set(cv2.CAP_PROP_FPS, TARGET_FPS)
            success, _ = fallback.read()
            if success:
                print(f"Camera opened using default backend fallback with backend {backend_name}.", flush=True)
                return fallback
            fallback.release()

    print(
        "Unable to open any camera. Please check that a webcam is connected and that "
        "camera access is allowed for the app or Python runtime in your system settings.",
        file=sys.stderr,
        flush=True,
    )
    return None


def main():
    signal.signal(signal.SIGINT, stop_handler)
    signal.signal(signal.SIGTERM, stop_handler)

    print(f"MediaPipe UDP tracker starting on {UDP_HOST}:{UDP_PORT}", flush=True)

    camera = open_camera()
    if camera is None:
        return 1

    sender = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        with mp.solutions.pose.Pose(
            static_image_mode=False,
            model_complexity=1,
            smooth_landmarks=True,
            enable_segmentation=False,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5,
        ) as pose:
            frame_count = 0
            while running:
                success, frame = camera.read()
                if not success:
                    time.sleep(0.01)
                    continue

                frame_count += 1
                if frame_count % PROCESS_EVERY_N_FRAMES != 0:
                    continue

                small_frame = cv2.resize(frame, (FRAME_WIDTH, FRAME_HEIGHT))
                rgb_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)
                results = pose.process(rgb_frame)

                if results.pose_landmarks:
                    payload = encode_landmarks(results.pose_landmarks.landmark)
                    sender.sendto(payload, (UDP_HOST, UDP_PORT))
                else:
                    sender.sendto(b"", (UDP_HOST, UDP_PORT))

                time.sleep(0.033)
    finally:
        camera.release()
        sender.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
