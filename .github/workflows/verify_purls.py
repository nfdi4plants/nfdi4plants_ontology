import requests

input_file = 'missing_purls.txt'
output_file = 'missing_purls_verified.txt'
base_url = 'http://purl.org/nfdi4plants/ontology/dpbo/'

def check_url_status(url):
    try:
        response = requests.head(url, allow_redirects=True)
        return response.status_code == 200
    except requests.RequestException as e:
        print(f"Request failed: {e}")
        return False

with open(input_file, 'r') as infile, open(output_file, 'w') as outfile:
    for line in infile:
        id_unmodified = line.strip()
        id = id_unmodified.replace("_", "")
        url = f"{base_url}{id}"
        if not check_url_status(url):
            outfile.write(f"{id_unmodified}\n")
