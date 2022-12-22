# -*- coding: utf-8 -*-
"""
Created on Tue Jun 16 11:45:57 2020

@author: m.hari.abhishek
"""

from flask import Flask, request, Response, session
from flask import jsonify
from Auth import validateToken,validateTokenUsingUrl
import pymongo
import file_encryptor
import subprocess 
import configparser, os

config = configparser.RawConfigParser()
conf_path = "/IngrAIn_Python/main/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)

try:
    config.read(configpath)
except UnicodeDecodeError:    
    
    config = file_encryptor.get_configparser_obj(configpath)
#config.read(configpath)
prefix=config['config']['applicationSuffix']
time_limit = config['config']['timeLimit']
import platform

if platform.system() == 'Linux':
    utils.logger(logger,'auth', 'INFO', "Platform is Linux")
else:
    from requests_negotiate_sspi import HttpNegotiateAuth

from IngrAIn_Python.main import publishModelService
from IngrAIn_Python.main import forecastModelService
from IngrAIn_Python.SSAIutils import utils
from IngrAIn_Python.SSAIutils.utils import get_auth_type
import psutil
import time
from datetime import datetime
from threading import Thread
#from apscheduler.schedulers.background import BackgroundScheduler
auth_type = get_auth_type()

import spacy
if platform.system() == 'Linux':
    thai_model_path='fasttext_model/cc.th.100.bin'
elif platform.system() == 'Windows':
    thai_model_path='fasttext_model\\cc.th.100.bin'
import fasttext,fasttext.util
import gc

app = Flask(__name__)

#global spacy_vectors
""" global spacy_vectors_en
global spacy_vectors_es
global spacy_vectors_pt
global spacy_vectors_de
global spacy_vectors_zh
global spacy_vectors_ja
global spacy_vectors_fr """


global spacy_vectors 
spacy_vectors= {}

#spacy_vectors["bengali"] = {"1":"1",'time' : time.time()}
def load_model(language):
    logger = utils.logger('Get', 'auth')
    if language in spacy_vectors.keys():
        spacy_vectors[language]['time'] = time.time()
        
    else: 
        if language =='english':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('en_core_web_lg'),
                                            'time' : time.time()})
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language English not found")
        elif language =='spanish':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('es_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_es = spacy.load('es_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language Spanish not found")
        elif language =='portuguese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('pt_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_pt = spacy.load('pt_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language Portugal not found")
        elif language =='german':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('de_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_de = spacy.load('de_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language German not found")
        elif language =='chinese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('zh_core_web_lg'),
                                            'time' : time.time()})
                #spacy_vectors_zh = spacy.load('zh_core_web_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language Chinese not found")
        elif language =='japanese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('ja_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_ja = spacy.load('ja_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language japanese not found")
        elif language =='french':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('fr_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_fr = spacy.load('fr_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language French not found")
        elif language =='thai':
            try: 
                spacy_vectors[language] = dict({'model':fasttext.load_model(thai_model_path),
                                            'time' : time.time()})
                #spacy_vectors_thai = fasttext.load_model(thai_model_path)
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Language Thai not found")
	
def remove_unused_model(queue):
    app.config['sv']  = 10
    
    spacy_vectors = queue.get()

    
    #spacy_vectors["bengali"] = {"1":"2",'time' : time.time()}
    
    items_to_be_removed = []
    #time.sleep(20)
    item_default = ['english']
    for item in spacy_vectors.keys():
        
        if (time.time() - spacy_vectors[item]['time']) > int(time_limit) and item not in item_default:
            items_to_be_removed.append(item)
    for item in items_to_be_removed:
        
        spacy_vectors[item]['model'] = ''
        spacy_vectors[item]['time'] = ''
        
        del spacy_vectors[item]['model']
        del spacy_vectors[item]['time']
        del spacy_vectors[item]
        
    gc.collect()
    del gc.garbage[:]
	


#scheduler = BackgroundScheduler(job_defaults={'max_instances': 2},daemon=True)
#scheduler.add_job(func=job_function, trigger="interval", seconds=int(time_limit))
#scheduler.start()

@app.route(prefix+"/home",methods=["GET"])
def index():
    return "Prediction API Home"

@app.route(prefix+"/predictservice",methods=["POST"])
def service():
    #global spacy_vectors
    logger = utils.logger('Get', 'auth')

    try:
        
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            if validateToken(token) == 'Fail':
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            utils.logger(logger,'auth', 'INFO', "Auth Type is WindowsAuthProvider")
        elif auth_type=='Forms' or auth_type=='Form':
            utils.logger(logger,'auth', 'INFO', "enter into form authentication"+str(request.headers))
            try:
                token = request.headers['Authorization']
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Exception in form token"+str(e.args))
                utils.logger(logger, 'auth', 'ERROR','Trace')
    
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)
        if request.method == "GET":
            return "Get method not allowed"
        
        
        correlationId = request.json["CorrelationId"]
        uniqueId = request.json["UniqueId"]
        pageInfo = request.json["PageInfo"]
        userId = request.json["UserId"]
        
        cpu=str(psutil.cpu_percent())
        memory=str(psutil.virtual_memory()[2])
        
        try:
            logger = utils.logger('Get',correlationId)
            utils.updQdb(correlationId,'P','0',pageInfo,userId,UniId = uniqueId)
            if pageInfo =='ForecastModel':
                
                utils.logger(logger, correlationId, 'INFO', ('Started Forecast at time '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
                utils.updQdb(correlationId,'P','In Progress',pageInfo,userId,UniId = uniqueId)
            
                thread = Thread(target=forecastModelService.main_wrapper, args=(correlationId,uniqueId,pageInfo,userId))
                thread.daemon = True
                thread.start()
            elif pageInfo =='PublishModel': 
                dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
                data_json1 = dbcollection.find({"CorrelationId" :correlationId})    
                dbconn.close()
                try:
                    language = data_json1[0].get('Language').lower()
                    #if language=='french':
                    #    language  = 'english'
                except Exception:
                    language = 'english'
                load_model(language)
                
                
                
                
                
                utils.logger(logger, correlationId, 'INFO', ('started publishmodel at time'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
            
                thread = Thread(target=publishModelService.main_wrapper, args=(correlationId,uniqueId,pageInfo,userId,spacy_vectors))
                thread.daemon = True
                thread.start()
#            
            return 'python call to predictive api reached'
        except Exception as e:
            
            
            return 'python processing failed.'
        
        
    except Exception as e:
        
        logger = utils.logger('Get',correlationId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(uniqueId))
        utils.updateErrorInTable(e.args[0], correlationId, uniqueId)
        utils.updQdb(correlationId,'E','ERROR',pageInfo,userId,UniId = uniqueId)
        
    else:
        utils.logger(logger,correlationId,'INFO','Task scheduled')
        



if __name__ == "__main__":
    if auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
        app.run(host='0.0.0.0',port=5000,threaded=False)
    else:
     app.run()
