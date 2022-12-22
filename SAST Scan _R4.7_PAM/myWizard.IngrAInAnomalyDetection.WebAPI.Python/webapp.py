# -*- coding: utf-8 -*-
"""
Created on Fri Sep  2 09:24:32 2022

@author: k.sudhakara.reddy
"""


from flask import Flask,request,jsonify,Response
import sys
import logging
from SSAIutils import utils
from flask import jsonify
from pandas import Timestamp
from numpy import nan
import datetime
from urllib.parse import unquote
import requests
from main import invokeingestdata
from datapreprocessing import datacuration
from datapreprocessing import datatransformation
from models import IsolationForest
from models import ARIMA
import configparser, os
from Auth import validateToken

import platform
from main import file_encryptor
config = configparser.RawConfigParser()
configpath = "/main/pythonconfig.ini"
configpath = str(os.getcwd()) + str(configpath)
try:
    config.read(configpath)
except UnicodeDecodeError:   
    
    config = file_encryptor.get_configparser_obj(configpath)
prefix=config['config']['applicationSuffix']



app = Flask(__name__)

@app.route(prefix+"/home", methods=['GET','POST'])
def test():
   
   return "Anamoly Detection Python API Home"    

@app.route(prefix+'/IngestData', methods=['POST'])
def invokeanomalydata():
    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method is not allowed"
        data=request.get_json()
        
        correlationId=data.get('correlationId')
        requestId=data.get('requestId')
        pageInfo=data.get('pageInfo')
        userId=data.get('userId')
        #utils.insQdb(correlationId, 'P', '5', pageInfo, userId,requestId)
        
        try: 
            invokeingestdata.Read_Data(correlationId,requestId,pageInfo,userId)
            #utils.updQdb(correlationId, 'C', '100', pageInfo, userId,requestId)
            utils.insQdb(correlationId, 'P', '5', 'DataCleanUp', userId,requestId)
            datacuration.main(correlationId, 'DataCleanUp', userId,requestId)
            #utils.insQdb(correlationId, 'C', '100', 'DataCleanUp', userId,requestId)
            utils.updQdb(correlationId, 'C', '100', 'DataCleanUp', userId,requestId=requestId)
        except Exception as e:
            utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId,requestId)
            return 'Invoke Ingestdata Failed'
                    
        return 'Success!'


@app.route(prefix+'/Datatransformation', methods=['POST'])
def Datapreprocessing():
    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method is not allowed"
        data=request.get_json()
        
        data=request.get_json()
        correlationId=data.get('correlationId')
        requestId=data.get('requestId')
        pageInfo=data.get('pageInfo')
        userId=data.get('userId')        
        try: 
            utils.insQdb(correlationId, 'P', '5', pageInfo, userId,requestId)
            datatransformation.main(correlationId,pageInfo,userId,requestId)
            utils.updQdb(correlationId,'C','100',pageInfo,userId,requestId=requestId)
        except Exception as e:
            utils.updQdb(correlationId, 'E',e.args[0], pageInfo, userId,requestId=requestId)
            return 'Datatransformation failed'
        return 'Success!'


@app.route(prefix+'/RecommendedAi', methods=['POST'])
def trainmodel():
    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
        
        data=request.get_json()
        
        correlationId=data.get('correlationId')
        requestId=data.get('requestId')
        pageInfo=data.get('pageInfo')
        userId=data.get('userId')
        #utils.insQdb(correlationId, 'P', '5', pageInfo, userId,requestId)
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        #problemtype = "Regression"
        
        try:
            problemtype = data_json[0]['ProblemType']
        except Exception as e:
            raise Exception(str(e.args))       
        if problemtype == "Regression":
            modelName = "IsolationForest"        
            try: 
                IsolationForest.main(correlationId,modelName,pageInfo,userId,requestId,problemtype)
                #utils.updQdb(correlationId,'C','100',pageInfo,userId,requestId=requestId)
            except Exception as e:
                utils.updQdb(correlationId, 'E',e.args[0], pageInfo, userId,requestId=requestId)
        else:
            if problemtype == 'TimeSeries':
                modelName = "SARIMA"
                try: 
                    ARIMA.main(correlationId,modelName,pageInfo,userId,requestId,problemtype,seasonality=True,version=None)
                except Exception as e:
                    utils.updQdb(correlationId, 'E',e.args[0], pageInfo, userId,requestId=requestId)
                    return 'Datatransformation failed'
        return 'Success!'
        

if __name__ == "__main__":
    app.run()


