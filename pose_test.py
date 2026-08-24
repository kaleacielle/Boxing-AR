import signal
import socket
import sys
import time

import cv2
import mediapipe as mp

UDP_HOST = "127.0.0.1"
UDP_PORT = 5052

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


def main():
    signal.signal(signal.SIGINT, stop_handler)
    signal.signal(signal.SIGTERM, stop_handler)

    camera = cv2.VideoCapture(0, cv2.CAP_AVFOUNDATION)
    if not camera.isOpened():
        print(
            "Unable to open the camera. Allow camera access for the Python "
            "interpreter or the application that launched it in System Settings "
            "> Privacy & Security > Camera.",
            file=sys.stderr,
            flush=True,
        )
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
            while running:
                success, frame = camera.read()
                if not success:
                    time.sleep(0.01)
                    continue

                results = pose.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
                if results.pose_landmarks:
                    sender.sendto(encode_landmarks(results.pose_landmarks.landmark), (UDP_HOST, UDP_PORT))
                else:
                    sender.sendto(b"", (UDP_HOST, UDP_PORT))
    finally:
        camera.release()
        sender.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
