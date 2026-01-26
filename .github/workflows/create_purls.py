#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Fri Apr 19 16:28:45 2024

@author: hannah
"""

from selenium import webdriver
from selenium.webdriver.support.wait import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
import chromedriver_autoinstaller
from pyvirtualdisplay import Display
import time
import os
import requests

display = Display(visible=0, size=(800,800))
display.start()

chromedriver_autoinstaller.install()
chrome_options = webdriver.ChromeOptions()
options = [
    "--window-size=1200,1200",
    "--ignore-certificate-errors",
    "--headless=new",
    "--no-sandbox",
    "--disable-dev-shm-usage"
]

for option in options:
    chrome_options.add_argument(option)

mail_key = os.getenv("MY_MAIL")
pw_key = os.getenv("MY_PW")
ts4tib = "https://terminology.tib.eu/ts/ontologies/dpbo/terms?iri=https%3A%2F%2Fpurl.org%2Fnfdi4plants%2Fontology%2Fdpbo%2F"
term_ids = []

with open("missing_purls_verified.txt") as f:
    for line in f:
        term_id = line.strip()
        term_ids.append(term_id)

driver = webdriver.Chrome(options = chrome_options)
driver.get("https://archive.org/login?referer=http://purl.archive.org/domain/nfdi4plants_ontology_dpbo")

wait = WebDriverWait(driver, 20)
host1 = wait.until(lambda d: d.execute_script("""
    return document.querySelector('app-root');
"""))
root1 = driver.execute_script("return arguments[0].shadowRoot", host1)

host2 = wait.until(lambda d: d.execute_script("""
    let root1 = arguments[0];
    let el = root1.querySelector('router-slot');
    return el ? el : false;
""", root1))
root2 = driver.execute_script("return arguments[0].shadowRoot", host2)

slot = wait.until(lambda d: d.execute_script("""
    let root2 = arguments[0];
    let el = root2.querySelector('slot');
    return el ? el : false;
""", root2))
slotted_elements = wait.until(lambda d: d.execute_script("""
    let slot = arguments[0];
    let el = slot.assignedElements();
    return el ? el : false;
""", slot))

host3 = slotted_elements[0]
root3 = driver.execute_script("return arguments[0].shadowRoot", host3)


mail = driver.execute_script("return arguments[0].querySelector('#email-input')", root3)
mail.send_keys(mail_key)
pw = driver.execute_script("return arguments[0].querySelector('#password-input')", root3)
pw.send_keys(pw_key)
login = driver.execute_script("return arguments[0].querySelector('ia-button')", root3)
login.click()

def check_url_status(url):
    try:
        response = requests.head(url, allow_redirects=True)
        return response.status_code == 200
    except requests.RequestException as e:
        print(f"Request failed: {e}")
        return False

def validate_purl(purl, base_url, redirect_url, retry):
    url = "http://purl.org" + base_url + "/" + purl
    if not check_url_status(url):
        print("not valid:", purl)
        create_purl(purl, redirect_url, retry+1)
    else:
        print("valid", purl)

def create_purl(purl_to_create, redirect_url, retry=0):
    if retry >= 3:
        print("max number of retries reached for ", purl_to_create)
        return
    
    # get the input fields for adding a new purl
    purl = driver.find_element(By.CSS_SELECTOR, value=".form-control[name='name']")
    target = driver.find_element(By.ID, value="target")

    # get the purl domain and clear the input already in there
    current_purl = purl.get_attribute("value")
    purl.clear()
    target.clear()

    purl_to_create = purl_to_create.replace("_", "")
    new_purl = current_purl + "/" + purl_to_create
    target_url = redirect_url + purl_to_create
    purl.send_keys(new_purl)
    target.send_keys(target_url)
    # searches for button elements anywhere in the html
    # button defines which element to search for
    submit = driver.find_element(By.XPATH, value="//button[@type='submit' and contains(text(), 'Add')]")
    submit.click()
    time.sleep(180)

    # check if the purl was created successfully
    validate_purl(purl_to_create, current_purl, redirect_url, retry)
    
i = 0
for elem in term_ids:
    print(i, elem)
    create_purl(elem, ts4tib)
    i += 1

driver.close()
