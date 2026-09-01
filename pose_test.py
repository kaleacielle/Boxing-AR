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


def try_open_camera(index, backend, backend_name):
    camera = cv2.VideoCapture(index, backend)
    if camera is None or not camera.isOpened():
        return None

    camera.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
    camera.set(cv2.CAP_PROP_FPS, TARGET_FPS)

    if not camera.grab():
        camera.release()
        return None

    success, frame = camera.retrieve()
    if not success or frame is None:
        camera.release()
        return None

    print(f"Camera opened using index {index} with backend {backend_name}.", flush=True)
    return camera


def get_camera_backends():
    def add_if_supported(name, value):
        if value is not None and hasattr(cv2, value):
            backends.append((name, getattr(cv2, value)))

    backends = []
    if sys.platform == "darwin":
        add_if_supported("AVFoundation", "CAP_AVFOUNDATION")
        add_if_supported("QuickTime", "CAP_QT")
        if hasattr(cv2, "CAP_ANY"):
            backends.append(("Default", cv2.CAP_ANY))
    elif sys.platform.startswith("win"):
        add_if_supported("DirectShow", "CAP_DSHOW")
        add_if_supported("Media Foundation", "CAP_MSMF")
        if hasattr(cv2, "CAP_ANY"):
            backends.append(("Default", cv2.CAP_ANY))
    else:
        add_if_supported("V4L2", "CAP_V4L2")
        add_if_supported("GStreamer", "CAP_GSTREAMER")
        if hasattr(cv2, "CAP_ANY"):
            backends.append(("Default", cv2.CAP_ANY))

    if not backends:
        backends.append(("Default", cv2.CAP_ANY if hasattr(cv2, "CAP_ANY") else 0))

    return backends


def open_camera():
    for backend_name, backend in get_camera_backends():
        for index in range(0, 12):
            camera = try_open_camera(index, backend, backend_name)
            if camera is not None:
                return camera

        # Some devices only work if opened without a backend hint.
        default_camera = cv2.VideoCapture(0)
        if default_camera is not None and default_camera.isOpened():
            default_camera.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
            default_camera.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
            default_camera.set(cv2.CAP_PROP_FPS, TARGET_FPS)
            if default_camera.grab():
                success, frame = default_camera.retrieve()
                if success and frame is not None:
                    print(f"Camera opened using default fallback with backend {backend_name}.", flush=True)
                    return default_camera
            default_camera.release()

    for index in range(0, 12):
        camera = cv2.VideoCapture(index)
        if camera is not None and camera.isOpened():
            camera.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
            camera.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
            camera.set(cv2.CAP_PROP_FPS, TARGET_FPS)
            if camera.grab():
                success, frame = camera.retrieve()
                if success and frame is not None:
                    print(f"Camera opened using plain index {index} fallback.", flush=True)
                    return camera
            camera.release()

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
