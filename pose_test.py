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


def open_camera():
    camera_candidates = []
    if sys.platform == "darwin":
        camera_candidates.append((0, cv2.CAP_AVFOUNDATION))
        camera_candidates.append((0, cv2.CAP_ANY))
    else:
        camera_candidates.append((0, cv2.CAP_DSHOW))
        camera_candidates.append((0, cv2.CAP_ANY))

    for index, backend in camera_candidates:
        camera = cv2.VideoCapture(index, backend)
        if camera.isOpened():
            camera.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
            camera.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
            camera.set(cv2.CAP_PROP_FPS, TARGET_FPS)
            print(f"Camera opened using backend {backend}.", flush=True)
            return camera
        camera.release()

    fallback = cv2.VideoCapture(0)
    if fallback.isOpened():
        fallback.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
        fallback.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
        fallback.set(cv2.CAP_PROP_FPS, TARGET_FPS)
        print("Camera opened using default backend.", flush=True)
        return fallback

    print(
        "Unable to open the camera. Allow camera access for the Python "
        "interpreter or the application that launched it in System Settings "
        "> Privacy & Security > Camera.",
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
