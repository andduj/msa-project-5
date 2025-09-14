import requests
import time

URL = "http://localhost:8081/api/jobs/import-products"

def main():
    for i in range(3):
        r = requests.post(URL, timeout=10)
        print(i, r.status_code, r.text)
        time.sleep(1)

if __name__ == "__main__":
    main()


