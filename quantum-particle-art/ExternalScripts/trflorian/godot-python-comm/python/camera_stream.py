import socket
import cv2
import sys
import time
import argparse
from constants import *

parser = argparse.ArgumentParser(description="Camera stream UDP sender")
parser.add_argument("-d","--display", action="store_true", help="Show external opencv debug display")
parser.add_argument("-f","--fps", type=int, default=15, help="Frames per second flushed to server")
parser.add_argument("-i","--id", type=int, default=0, help="Camera ID, 0 for default camera first hardware camera of the computer")
parser.add_argument("-r","--res", type=int, nargs = 2, help="Camera resized and output resolution width height, independent from the camera resolution itself", default=[1920,1080])
args = parser.parse_args()

text = False
sendOnlyFrame = True
display = args.display
camera_id = args.id
size = (args.res[0], args.res[1])
fps = args.fps
chunks = 65000  # max UDP packet size is 65507 bytes
try:
    send_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    buffer_size = size[0]*size[1]*4+16
    send_socket.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF,buffer_size)
    send_socket.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF,buffer_size)
    
    ack_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    ack_socket.bind((SERVER_IP, LISTEN_PORT))
    ack_socket.setblocking(False)
    #empty the socket from any potential residual messages
    try:
        while True:
            ack_socket.recv(4096)
    except BlockingIOError:
        pass
except WindowsError as e:
    print("Couldn't emit throught socket, assuming another instance of this program is already communicating through it")
    exit(1)
cap = cv2.VideoCapture(camera_id)
first = True
print("Python log",flush=True)
i=0
encoded_image = None
while True:
    
    ret, frame = cap.read()

    if not ret:
        print("Webcam probably already in use, possibly by another instance of this script, but the other will serve webcam data to the server",file=sys.stderr)
        print(f"Error: {ret} ",file=sys.stderr)
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
    row2 = cv2.hconcat([blur, canny])
    image = cv2.vconcat([row1, row2])

    image = cv2.resize(image, (400, 300))
    send = first
    try:
        #print("Checking for request...",flush=True)
        listen = ack_socket.recv(1) 
        print("Received request from",listen is not None, len(listen), listen[0],flush=True)
        #raise "Log"
        send = listen is not None
    except BlockingIOError:
        #print("Nthing on prt",flush=True)
        pass
    #print("Sending, is First ?",first,flush=True)
    if encoded_image is None:
        toSend = frame if sendOnlyFrame else image
        _, encoded_image = cv2.imencode(".jpg", toSend)
        nb_chunks = len(encoded_image) // chunks
        print("Sending image of size", len(encoded_image), "in", nb_chunks+1, "chunks", flush=True)
        octetsX = len(encoded_image).to_bytes(4, byteorder='big', signed=False)
        send_socket.sendto(octetsX, (SERVER_IP, SERVER_PORT))
        i=0
    if send:
        first = False
    #for i in range(nb_chunks+1):
        ideb = i*chunks
        iend = min((i+1)*chunks, len(encoded_image))
        print("Sending chunnk", i, "from", ideb, "to", iend, flush=True)
        section = encoded_image[ideb:iend]
        send_socket.sendto(section, (SERVER_IP, SERVER_PORT))
        time.sleep(1/fps/nb_chunks)
        i+=1
        if i > nb_chunks+1:
            encoded_image = None
    #time.sleep(1/fps)
        #print(sent := len(section) , "bytes sent", "from", ideb, "to", iend)
    
    if display:
        cv2.imshow("Video feed", image)
        if cv2.getWindowProperty('Video feed', 0) < 0:
            break

    if cv2.waitKey(1) & 0xFF == ord("q"):
        break


cv2.destroyAllWindows()

cap.release()
