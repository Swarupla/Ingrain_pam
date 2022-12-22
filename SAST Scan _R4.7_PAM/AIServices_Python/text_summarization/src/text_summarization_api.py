 # Created by Joydeep Sarkar
# Created on 09-Feb-2018
# Copyright MyWizard VA


from flask import Flask, request,jsonify
from flask_cors import CORS
import os
import json
from pathlib import Path
from summarization_helper import SummarizationHelper
from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory + "/summarization.log")
app = Flask(__name__)
CORS(app)
import configparser


def getConfigValue1():
    if 'app' in currentDirectory:
        configpath='/app/pythonconfig.ini'
    else:
        configpath = '/var/www/myWizard.IngrAInAIServices.WebAPI.Python/pythonconfig.ini'
    config = configparser.RawConfigParser()
    try:
        config.read(configpath)     
        value = config['config']['baseURL'] 
        print('base url' + value)
        return value        
    except UnicodeDecodeError:        
        return ''


@app.route('/', methods=['GET','POST'])
def server_check():
    return ("Hello Prediction")

#@app.route('/getsummary', methods=['POST'])
def get_summary():
    print('Summary 1')
    return_value = {}
    return_value["is_success"] = False
    return_value["message"] = ""
    return_value["response"] = []
    try:
        req = request.get_json()
        if("query" in req and "range" in req):
            summarization = SummarizationHelper()
            query=req["query"].encode('ascii',"ignore").decode('ascii')
            return_value = summarization.get_summary(query, req["range"])
        else:
            return_value["message"] = "Please provide document and range to get summary."
    except Exception as ex:
        logger.error(str(ex))
        return_value["message"] = str(ex)
    return jsonify(return_value)


if __name__ == "__main__":
    app.run(port=5001)
