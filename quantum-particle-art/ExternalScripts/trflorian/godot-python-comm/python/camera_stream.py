import socket
import cv2
import sys
import time
from constants import SERVER_IP, SERVER_PORT

text = False
sendOnlyFrame = True
display = False
camera_id = 0
size = (1920, 1080)
fps = 15
chunks = 65000  # max UDP packet size is 65507 bytes
client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
cap = cv2.VideoCapture(camera_id)
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

    toSend = frame if sendOnlyFrame else image
    _, encoded_image = cv2.imencode(".jpg", toSend)
    #encoded_image = toSend.tobytes()
    nb_chunks = len(encoded_image) // chunks
    print("Sending image of size", len(encoded_image), "in", nb_chunks+1, "chunks")
    octetsX = len(encoded_image).to_bytes(4, byteorder='big', signed=False)
    client_socket.sendto(octetsX, (SERVER_IP, SERVER_PORT))
    for i in range(nb_chunks+1):
        ideb = i*chunks
        iend = min((i+1)*chunks, len(encoded_image))
        section = encoded_image[ideb:iend]
        client_socket.sendto(section, (SERVER_IP, SERVER_PORT))
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
