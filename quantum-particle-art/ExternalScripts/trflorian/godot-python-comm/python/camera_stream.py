import socket
import cv2
import sys
import time
import argparse
import numpy as np
from constants import *

parser = argparse.ArgumentParser(description="Camera stream UDP sender")
parser.add_argument("-d", "--display", action="store_true", help="Show external opencv debug display")
parser.add_argument("-a", "--ack", action="store_true", help="Should wait for ack before sending next frame")
parser.add_argument("-f", "--fps", type=int, default=15, help="Frames per second flushed to server")
parser.add_argument("-i", "--id", type=int, default=0,
                    help="Camera ID, 0 for default camera first hardware camera of the computer")
parser.add_argument("-r", "--res", type=int, nargs=2,
                    help="Camera resized and output resolution width height, independent from the camera resolution itself",
                    default=[1920, 1080])
parser.add_argument("-c", "--chunks", type=int, default=4,
                    help="Numbers of chunks sent one after another before waiting for next frame")
parser.add_argument("-b", "--reserved_bytes", type=int, default=1,
                    help="Numbers of bytes in each chunk reserved for metadata")
parser.add_argument("-s", "--chunk_size", type=int, default=65000,
                    help="Max real size of each UDP chunk including reserved bytes")

args = parser.parse_args()

text = False
sendOnlyFrame = True
display = args.display
ack = args.ack
consecutive_chunks = args.chunks
camera_id = args.id
size = (args.res[0], args.res[1])
fps = args.fps
# 1 byte of the chunk reserved for the chunk index
reserved_bytes_per_chunk = args.reserved_bytes
chunk_size = args.chunk_size
usefulSize = chunk_size - reserved_bytes_per_chunk

# max UDP packet size is 65507 bytes
if chunk_size > 65507:
    print("Warning: chunk size too big for UDP, won't be running, setting to max 65507 bytes")
    exit(1)
try:
    send_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    buffer_size = size[0] * size[1] * 4 * fps + 16 #*fps is just for safety margin of handling a full second of frames in the buffer even if the client is supposingly polling them much faster
    send_socket.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, buffer_size)
    send_socket.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, buffer_size)

    ack_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    ack_socket.bind((SERVER_IP, LISTEN_PORT))
    ack_socket.setblocking(False)
    # empty the socket from any potential residual messages
    try:
        while True:
            ack_socket.recv(4096)
    except BlockingIOError:
        pass
except WindowsError as e:
    print(
        "Couldn't emit throught socket, assuming another instance of this program is already communicating through it")
    exit(1)
cap = cv2.VideoCapture(camera_id)
first = True
print("Python log", flush=True)
i = 0
encoded_image = None
toSend = None
while True:

    ret, frame = cap.read()

    if not ret:
        print(
            "Webcam probably already in use, possibly by another instance of this script, but the other will serve webcam data to the server",
            file=sys.stderr)
        print(f"Error: {ret} ", file=sys.stderr)
        exit(1)

    frame = cv2.resize(frame, size)
    frame = cv2.flip(frame, 1)

    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    gray = cv2.cvtColor(gray, cv2.COLOR_GRAY2BGR)

    blur = cv2.GaussianBlur(gray, (5, 5), 0)

    canny = cv2.Canny(blur, 20, 50)
    canny = cv2.cvtColor(canny, cv2.COLOR_GRAY2BGR)
    if text:
        cv2.putText(
            frame,
            f"OpenCV version: {cv2.__version__}",
            (5, 15),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.5,
            (255, 255, 255),
            1,
        )

    row1 = cv2.hconcat([frame, gray])
    row2 = cv2.hconcat([toSend if toSend is not None else blur, canny])
    image = cv2.vconcat([row1, row2])

    image = cv2.resize(image, (400, 300))
    send = first
    try:
        # print("Checking for request...",flush=True)
        listen = ack_socket.recv(1)
        # print("Received request from",listen is not None, len(listen), listen[0],flush=True)
        # raise "Log"
        send = listen is not None
    except BlockingIOError:
        # print("Nthing on prt",flush=True)
        pass
    # print("Sending, is First ?",first,flush=True)
    if encoded_image is None:
        toSend = frame if sendOnlyFrame else image
        _, encoded_image = cv2.imencode(".jpg", toSend)
        nb_total_chunks = (len(encoded_image) + (usefulSize - 1)) // (usefulSize)
        # print("Sending image of size", len(encoded_image), "in", nb_chunks+1, "chunks", flush=True)
        # We'll be sending for each chunk, it's chunk index so we have 1 more byte per chunk on top of all the pixels data
        octetsX = (len(encoded_image)).to_bytes(4, byteorder='big', signed=False)
        send_socket.sendto(octetsX, (SERVER_IP, SERVER_PORT))
        time.sleep(1 / 1000)  # Delay to ensure the bytes info are reiceved first
        i = 0
    if not ack or send:
        first = False
        for _ in range(consecutive_chunks):
            ideb = i * usefulSize
            iend = min((i + 1) * usefulSize, len(encoded_image))
            # print("Sending chunnk", i, "from", ideb, "to", iend, flush=True)
            section = encoded_image[ideb:iend]
            # Section index at the beggining of the section so we can rebuild correctly the image even if the order of chunks is messed up
            reserved = [0] * reserved_bytes_per_chunk
            reserved[0] = np.uint8(i)
            # Add other metadata in reserved bytes if needed and if number of reserved bytes are enough
            section = np.concatenate((reserved, section))
            if len(section) != chunk_size and iend != len(encoded_image):
                print("Last chunk size isn't correct", len(section), flush=True, file=sys.stderr)
                break
            send_socket.sendto(section, (SERVER_IP, SERVER_PORT))
            i += 1
            time.sleep(1 / fps / nb_total_chunks)  # guarantees order if we delay beetwen any chunks
            if i >= nb_total_chunks:
                encoded_image = None
                break
        # print(sent := len(section) , "bytes sent", "from", ideb, "to", iend)

    if display:
        cv2.imshow("Video feed", image)
        if cv2.getWindowProperty('Video feed', 0) < 0:
            break

    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cv2.destroyAllWindows()

cap.release()
