# -*- coding: utf-8 -*-
"""
Created on Tue Jun 16 11:45:57 2020

@author: m.hari.abhishek
"""

from flask import Flask, request, Response
from flask import jsonify
from Auth import validateToken,validateTokenUsingUrl
import pymongo
from SSAIutils.utils import get_auth_type
from SSAIutils import utils

#from main import invokeIngestData
auth_type = get_auth_type()
import subprocess 
import configparser, os

# config = configparser.RawConfigParser()
# conf_path = "/pythonconfig.ini"
# configpath = str(os.getcwd()) + str(conf_path)
# config.read(configpath)
# prefix=config['config']['applicationSuffix']
import platform
import file_encryptor
config = configparser.RawConfigParser()
configpath = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(configpath)
try:
    config.read(configpath)
except UnicodeDecodeError:   
    #print("****inside exception unidecode pythonconfig****")
    config = file_encryptor.get_configparser_obj(configpath)
    #print("Config:: ",config['config'])
prefix=config['config']['applicationSuffix']


if platform.system() == 'Linux':
    python_path =   str(os.getcwd()) + "/inf_env/bin/python"
    if auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS' or 'app' in str(os.getcwd()):
        python_path =   "python"       
else:
    python_path =   str(os.getcwd()) + "\inf_env\Scripts\python"
    if auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS' or 'app' in str(os.getcwd()):
        python_path =   "python"
    from requests_negotiate_sspi import HttpNegotiateAuth
    
app = Flask(__name__)


@app.route(prefix+"/home",methods=["GET"])
def index():
    return "Inference Engine Python API Home"



@app.route(prefix+"/ingestData",methods=["POST"])
def IngestData():
    try:
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            if validateToken(token) == 'Fail':
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            pass
        elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
            try:
                token = request.headers['Authorization']
            except Exception as e:
                error_auth=e.args
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)
        if request.method == "GET":
            return "Get method not allowed"
        #print("request.json:: ", request.json)
        #print("type:: ", type(request.json))
        correlationId = request.json["CorrelationId"]
        requestId = request.json["RequestId"]
        pageInfo = request.json["PageInfo"]
        userId = request.json["UserId"]
        
        logger = utils.logger('Get', correlationId)
        utils.logger(logger, correlationId, 'INFO', 'Ingest data invoked',str(requestId))

        
        args= correlationId+" "+requestId+" "+pageInfo+" "+userId
        #python_file =   str(os.getcwd())+"/main/invokeIngestData.py"
        if platform.system()=='Linux': 
            python_file =  '\"'+ str(os.getcwd())+"/main/invokeIngestData.py\""
        else:
            python_file =  ""+ str(os.getcwd())+"/main/invokeIngestData.py"
        cmd = python_path+" "+python_file+" "+args
        #python_file =  '\"'+ str(os.getcwd())+"/main/invokeIngestData.py\""
        #cmd = python_path+" "+python_file+" "+args
        utils.logger(logger, correlationId, 'INFO', "Invoking Subprocess: "+ str(cmd) ,str(requestId))

        #print("Invoking Subprocess: "+ cmd)
        if platform.system() == 'Linux':
            subprocess.Popen(cmd,close_fds=True,shell=True)     
        elif platform.system() == 'Windows':
            try:
                cmd1=subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                out,err=cmd1.communicate()
                utils.logger(logger, correlationId, 'INFO',str(out)+"******** PageInfo:::"+str(pageInfo),str(requestId))
            except Exception as e:
                utils.logger(logger, correlationId, 'ERROR','Trace',str(requestId))
                return jsonify({'status': 'false', 'message':str(e.args)}), 500

        utils.logger(logger, correlationId, 'INFO', 'Ingest data ended',str(requestId))

    except Exception as e:
        return jsonify({'status': 'false', 'message':str(e.args)}), 500
    return jsonify({'status': 'True', 'message':"Success"}), 200


@app.route(prefix+"/generateNarratives", methods=['POST'])
def generate_inflow_narratives():
    try: 
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            if validateToken(token) == 'Fail':
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            pass
        elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
            try:
                token = request.headers['Authorization']
            except Exception as e:
                error_auth=e.args
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)    
        if request.method == "GET":
            return "Get method not allowed"
        #print("request.json:: ", request.json)
        #print("type:: ", type(request.json))
        correlationId = request.json["CorrelationId"]
        requestId = request.json["RequestId"]
        inferenceConfigId = request.json["InferenceConfigId"]
        pageInfo = request.json["PageInfo"]
        userId = request.json["UserId"]
        
        logger = utils.logger('Get', correlationId)
        utils.logger(logger, correlationId, 'INFO', 'generateNarratives endpoint  with pageInfo:: '+str(pageInfo),str(requestId))

        args= correlationId+" "+requestId+" "+inferenceConfigId+" "+pageInfo+" "+userId
        #python_file =   str(os.getcwd())+"/main/invokeIngestData.py"
        #python_file =  '\"'+ str(os.getcwd())+"/main/app.py\""
        if platform.system()=='Linux': 
            python_file =  '\"'+ str(os.getcwd())+"/main/app.py\""
        else:
            python_file =  ""+ str(os.getcwd())+"/main/app.py"
        cmd = python_path+" "+python_file+" "+args
        #print("Invoking Subprocess: "+ cmd)
        utils.logger(logger, correlationId, 'INFO', "Invoking Subprocess: "+str(cmd),str(requestId))

        if platform.system() == 'Linux':
            subprocess.Popen(cmd,close_fds=True,shell=True)     
        elif platform.system() == 'Windows':
            try:
                cmd1=subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                out,err=cmd1.communicate()
                utils.logger(logger, correlationId, 'INFO',str(out)+"******** PageInfo:::"+str(pageInfo),str(requestId))
            except Exception as e:
                utils.logger(logger, correlationId, 'ERROR','Trace',str(requestId))
                return jsonify({'status': 'false', 'message':str(e.args)}), 500
            
        utils.logger(logger, correlationId, 'INFO', 'End of generateNarratives endpoint  with pageInfo:: '+str(pageInfo),str(requestId))

        
    except Exception as e:
        return jsonify({'status': 'false', 'message':str(e.args)}), 500
    return jsonify({'status': 'True', 'message':"Success"}), 200


if __name__ == "__main__":
    if auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
        app.run(host='0.0.0.0',port=5000,threaded=False)
    else:
        app.run()
