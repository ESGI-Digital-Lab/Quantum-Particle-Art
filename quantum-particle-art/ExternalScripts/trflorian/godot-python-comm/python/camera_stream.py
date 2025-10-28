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
server_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
client_socket.bind((SERVER_IP,LISTEN_PORT))
client_socket.setblocking(False)
cap = cv2.VideoCapture(camera_id)
first = True
print("Pythong log",flush=True)
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
        listen = client_socket.recv(chunks)
        #print("Received request from",listen is not None,flush=True)
        #raise "Log"
        send = listen is not None
    except BlockingIOError:
        #print("Nthing on prt",flush=True)
        pass
    if send:
        #print("Sending, is First ?",first,flush=True)
        first = False
        toSend = frame if sendOnlyFrame else image
        _, encoded_image = cv2.imencode(".jpg", toSend)
        #encoded_image = toSend.tobytes()
        nb_chunks = len(encoded_image) // chunks
        print("Sending image of size", len(encoded_image), "in", nb_chunks+1, "chunks")
        octetsX = len(encoded_image).to_bytes(4, byteorder='big', signed=False)
        server_socket.sendto(octetsX, (SERVER_IP, SERVER_PORT))
        for i in range(nb_chunks+1):
            ideb = i*chunks
            iend = min((i+1)*chunks, len(encoded_image))
            section = encoded_image[ideb:iend]
            server_socket.sendto(section, (SERVER_IP, SERVER_PORT))
            time.sleep(1/fps/nb_chunks)
        #time.sleep(1/fps)
            #print(sent := len(section) , "bytes sent", "from", ideb, "to", iend)
    
    if display:
        cv2.imshow("Image", image)
        if cv2.getWindowProperty('Image', 0) < 0:
            break

    if cv2.waitKey(1) & 0xFF == ord("q"):
        break


cv2.destroyAllWindows()

cap.release()
