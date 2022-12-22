#Created by Joydeep Sarkar
#Created on 14-Dec-2018
#Copyright MyWizard VA

import os
import json
import sys
from flask import Flask, jsonify, request, render_template
from flask_cors import CORS
import platform
if platform.system() == 'Linux':
    from keras import backend as K
elif platform.system() == 'Windows':
    from tensorflow.keras import backend as K
from pathlib import Path

from coreference_resolution_helper import CoreferenceResolution
import re

from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory + "/coreference.log")

app = Flask(__name__)
CORS(app)

# API for Testing the Availability
@app.route('/', methods=['GET','POST'])
def hello_world():
    return 'Hello NLP'

# API for getting coreferences from text
@app.route('/getcoreference', methods=['POST'])
def get_coreference():
    K.clear_session()
    return_value = {}
    return_value["message"] = ""
    return_value["is_success"] = False
    return_value["response_data"] = []
    req = request.get_json()
    sentence = req["text"]
    sentence=str(sentence).encode('ascii',"ignore").decode('ascii')
    string_check= re.compile('[@_!#$%^&* ()<>[\]?/\|\'\"\`}{~:;+-=.,]') 
    flag = 0
    for ch in sentence:
        if(string_check.search(ch) == None):
            flag = 1

    try:
        if(len(sentence) > 3 and flag == 1):
            coref = CoreferenceResolution()
            #return_value["response"] = coref.reference(sentence)
            return_value = coref.reference(sentence)
            #return_value["is_success"] = True
        else:
            return_value["message"] = "Please provide a valid sentence."
            return_value["is_success"] = False
    except Exception as ex:
        print(ex)
    return jsonify(return_value)

if __name__ == '__main__':
    app.run()