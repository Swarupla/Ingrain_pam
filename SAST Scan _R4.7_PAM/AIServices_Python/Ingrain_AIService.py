# -*- coding: utf-8 -*-
"""
Created on Fri Jul 31 21:25:15 2020

@author: s.siddappa.dinnimani
"""
import time
start=time.time()
from flask import Flask, request, Response, session
from flask import jsonify
from threading import Thread
import sys
import time
from multiprocessing import Process
import subprocess
import configparser, os
import file_encryptor
config = configparser.RawConfigParser()
from datetime import datetime
configpath = "./pythonconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:    
    print("****inside exception unidecode pythonconfig****")
    config = file_encryptor.get_configparser_obj(configpath)
basepath=config['config']['path']
import platform
if platform.system() == 'Linux':
    if basepath == "/app/": # for pam server
        sys.path.insert(0, "/app/sentiment_detection/src")
        sys.path.insert(1, "/app/text_summarization/src")
        sys.path.insert(2, "/app/ai_services_pheonix")
    else:
        dir_pattern='/'
        sys.path.insert(0, basepath+dir_pattern+"sentiment_detection/src")
        sys.path.insert(1, basepath+dir_pattern+"text_summarization/src")
        sys.path.insert(2, basepath+dir_pattern+"ai_services_pheonix")
        
elif platform.system() == 'Windows':
    dir_pattern='\\\\'
    sys.path.insert(0, basepath+dir_pattern+"sentiment_detection\\src")
    sys.path.insert(1, basepath+dir_pattern+"text_summarization\\src")
    sys.path.insert(2, basepath+dir_pattern+"ai_services_pheonix")
    

sys.path.insert(0, "/app/sentiment_detection/src")
sys.path.insert(1, "/app/text_summarization/src")
sys.path.insert(2, "/app/ai_services_pheonix")

from Auth import validateToken,validateTokenUsingUrl
from sentiment_api import detect_sentiment_from_text
from webAPI import SimilarityAnalytics
from webAPI import train_Developer
from webAPI import PredictDeveloper
from tasks import PredictIt
from webAPI import SimilarityPrediction
from webAPI import get_summary 
import configparser, os
mainPath = os.getcwd() 
import sys
import copy
import json
sys.path.insert(0, mainPath)
import utils
import spacy_endecrypt as sedt
import pandas as pd
import spacy
auth_type = utils.get_auth_type()

import psutil
from datetime import datetime
configpath = "/app/pythonconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:    
    print("****inside exception unidecode pythonconfig****")
    config = file_encryptor.get_configparser_obj(configpath)



app = Flask(__name__)

#global model_flag
model_flag = False
model_flag_other = False
global model_Object

prefix=config['config']['applicationSuffix']
app.secret_key = 'xyz'
shared_model_va = config['ModelPath']['model_path_va']
shared_model_other = config['ModelPath']['model_path']
cpu_val=config['CPUMemory']['CPU']
memory_val=config['CPUMemory']['Memory']
#encrypt_key=config['PICKLE_ENCRYPT']['key']
global ran_var
ran_var = 'This is a var'
#####new addition
provider, key_val, vector = file_encryptor.get_from_vault(str(os.getcwd()))

if provider == 'AWS' or provider == 'Azure':
    key = key_val
    iv =  vector
    encrypt_key = key_val
elif provider == "NoProvider":
    import base64
    key = base64.b64decode(config['SECURITY']['Key'])
    iv =  base64.b64decode(config['SECURITY']['Iv'])
    encrypt_key = config['SECURITY']['Key']


###################
from nltk.corpus import stopwords
stop = stopwords.words('english')

stop.append('also')
stop.append('well')
stop.append('as')
stop.remove('not')
stop.remove('now')
stop.remove('when')
stop.remove('who')
stop.remove('whom')
stop.remove('why')
stop.remove('what')
stop.remove('which')
end=time.time()

def remove_stopwords_new(query):
    out_query=""
    for word in query.split():
        if word not in (stop):
            out_query = out_query + word +" "
    out_query=out_query.rstrip()
    return out_query


@app.route(prefix+"/hello", methods=['GET','POST'])
def index():
    print("hello my new message.")
    return "I am from Hello method" 

def second_doit(text):      
  import re
  matches=re.findall(r'\"(.+?)\"',text)
  # matches is now ['String 1', 'String 2', 'String3']
  return matches

@app.route(prefix+"/ainlp/parsetext", methods=['POST'])
def detect_intent():
    
    start_time = time.time()
    global model_flag
    global model_client
    global model_Object_intent
    global model_Object_entity
    global model_flag_other
    global model_Object_other
    global model_client_other
    global model_correlation_other
    
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    response={}
    data = request.get_json()
    query_creation_date=datetime.now().strftime('%Y-%m-%d %H:%M:%S')    
    try:
        if "query" not in data or len(data["query"])<2:
            
            raise ValueError('query is not provided')
        if ('client_id' not in data or len(data['client_id'])<2):
            
            raise ValueError('client_id is not provided')                          
    except Exception as ex:
        response["message"] = str(ex)
        response["is_success"] = False        
        return jsonify(response)  
    client_id=data['client_id']
    query=data['query']
    query_db_val=query
    query=query.encode('ascii',"ignore").decode('ascii')
    original_data=query
    query=str(query).lower()
    dc_id=data['dc_id']
    model_name=data["model_name"]
    correlation_id=data["correlation_id"]
    logger = utils.logger('Get', correlation_id)
    try:
        
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlation_id, 'INFO', ('import in Ingrainaiservice for intentandentity func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory))
        
        if (client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id=="VTM_Training"):
            utils.logger(logger, correlation_id, 'INFO', ('Inside Va models with client as='  + str(client_id)))
            
            if model_flag == True:
                if model_client != client_id:
                    model_flag = False
            model_flag_other=False
            utils.logger(logger, correlation_id, 'INFO', ('with model flag as='  + str(model_flag)))
            
        else:
           
            if model_flag_other == True:
                utils.logger(logger, correlation_id, 'INFO', ('Inside other VA models with model_flag as='  + str(model_flag_other)))
                
                if(model_correlation_other != correlation_id):
                    
                    model_flag_other = False
            
        
        
        if model_flag == False:
            
            utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag as='  + str(model_flag)))
            if (client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id=="VTM_Training"):
                va_model_path_entity = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id+dir_pattern+dc_id+dir_pattern+correlation_id+dir_pattern+"output_entity"+dir_pattern+"model-best"
                va_model_path_intent = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id+dir_pattern+dc_id+dir_pattern+correlation_id+dir_pattern+"output_intent"+dir_pattern+"model-best"
                utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag with loading model_path for entity {}'.format(va_model_path_entity)))
                utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag with loading model_path for intent {}'.format(va_model_path_intent)))
                model_Object_entity=sedt.spacy_decrypt(va_model_path_entity,encrypt_key)
                #model_Object_entity = spacy.load(va_model_path_entity)
                model_Object_intent=sedt.spacy_decrypt(va_model_path_intent,encrypt_key)
                utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag after loading model with spacy decrypt for entity&intent'))
                #model_Object_intent = spacy.load(va_model_path_intent)
                
                sedt.spacy_encrypt(va_model_path_entity,encrypt_key)
                sedt.spacy_encrypt(va_model_path_intent,encrypt_key)
                utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag after loading model with spacy encrypt for entity&intent')) 
                #va_model_path = shared_model_va +"/generated/"+(client_id)+("/")+(dc_id)+("/")+(correlation_id)+("/output/")+(model_name)
                #model_Object = load_model_from_path(va_model_path)
                model_flag = True
                model_client = client_id
                #print(model_Object, model_client)

        print("model_flag_other", model_flag_other)
        if model_flag_other == False:
            
            if not((client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id=="VTM_Training")): 
                va_model_path_entity = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id+dir_pattern+dc_id+dir_pattern+correlation_id+dir_pattern+"output_entity"+dir_pattern+"model-best"
                va_model_path_intent = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id+dir_pattern+dc_id+dir_pattern+correlation_id+dir_pattern+"output_intent"+dir_pattern+"model-best"
                model_Object_entity=sedt.spacy_decrypt(va_model_path_entity,encrypt_key)
                model_Object_intent=sedt.spacy_decrypt(va_model_path_intent,encrypt_key)
                sedt.spacy_encrypt(va_model_path_entity,encrypt_key)
                sedt.spacy_encrypt(va_model_path_intent,encrypt_key)
                model_flag_other = True
                model_client_other = client_id
                model_correlation_other = correlation_id
                utils.logger(logger, correlation_id, 'INFO', ('Inside if of  model_flag_other after loading model with spacy encrypt for entity&intent')) 
                
        if (client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id == 'VTM_Training'):
            
            
            val=remove_stopwords_new(query)
            utils.logger(logger, correlation_id, 'INFO', ('if loop of query parsing post sw removal {}'.format(query))) 
            mydict={}
            mydict['text'] = val        
            doc_intent = model_Object_intent(val)
            doc_entity = model_Object_entity(val)
            val1=doc_intent.cats
            
            val=sorted(val1.items(), key=lambda x:x[1],reverse=True)[0]
            mydict['intent']={"confidence":val[1],"name":val[0]}
            utils.logger(logger, correlation_id, 'INFO', ('inside Intent{}'.format(mydict['intent'])))
            newdict = pd.DataFrame()
            for i in range(0,len(val1)):
                newdict= newdict.append([[sorted(val1.items(), key=lambda x:x[1],reverse=True)[i][1],sorted(val1.items(), key=lambda x:x[1],reverse=True)[i][0]]])
            newdict.rename(columns = {0:'confidence', 1:'name'}, inplace = True)
            mydict1 = newdict.to_dict("records")
            mydict['intent_ranking']= mydict1
            mydict['entities']=[]
            if len(doc_entity.ents)==0:
                mydict['entities'].append({"end": '',"entity": '',"extractor": "DIETClassifier","start": '',"value": ''})
            else:
                
                for ent in doc_entity.ents:
                    value=str(ent.text)
                    if str(value).startswith('"') or str(value).startswith("'"):
                        value = str(value)[1:]
                    if str(value).endswith('"') or str(value).endswith("'"):
                        value = str(value)[:-1]
                    mydict['entities'].append({"end": ent.end_char,"entity": ent.label_,"extractor": "DIETClassifier","start": ent.start_char,"value": value})
            mydictdata=mydict['entities']
            mysplit_data=original_data.split(' ')
            for index,data in enumerate(mydictdata):
                    temp=data['value']
                    val=utils.intentstrMatch(temp,mysplit_data)
                    
                    if val!='':
                        mydict['entities'][index]['value']=val
            response["message"] = ""
            response["result"] = mydict
            response["is_success"] = True
            utils.aiServicesPrediction(correlation_id,'Intent&Entity',str(query_db_val),str(mydict),query_creation_date,"AIServicesIntentEntityPrediction")
            utils.logger(logger, correlation_id, 'INFO', ('DB insertion is complete'))
            utils.logger(logger, correlation_id, 'INFO', ('response_for query {} is {}'.format(mydict['text'],mydict)))
    except Exception as e:
        utils.logger(logger, correlation_id, 'ERROR','Trace')
        response["message"] = ""
        response["result"] = str(e)
        response["is_success"] = False
    end_time = time.time()
    utils.logger(logger, correlation_id, 'INFO', ('total time for entire Intent&entity took  is {}'.format(end_time - start_time)))
    return jsonify(response)

@app.route(prefix+"/ainlp/train", methods=['POST'])
def train_intent():
    print("train my new message.")
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    return train_nlp()

@app.route(prefix+"/sentiment/detecttone", methods=['POST'])
def detect_sentiment():
    print("detect sentiment.")
    cpu,memory=utils.Memorycpu()
    logger = utils.logger('Get', 'sentiment')
    utils.logger(logger,'sentiment', 'INFO', ('import in Ingrainaiservice for sentiment_detect_tone func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory))

    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            utils.logger(logger, 'sentiment', 'ERROR','Trace')
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    utils.logger(logger,'sentiment', 'INFO', ('sentiment token validation is complete'))
    query_creation_date=datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    logger = utils.logger('Get', 'sentiment')
    utils.logger(logger,'sentiment', 'INFO', ('entering detect_sentiment_from_text_func'))
    try:
        val,data=detect_sentiment_from_text()
        query_db_val=data
        mydict=val
        utils.logger(logger,'sentiment', 'INFO', ('prediction is complete'))
        utils.aiServicesPrediction('sentiment','sentiment',str(query_db_val),str(mydict),query_creation_date,"AIServicesSentimentPrediction")
        utils.logger(logger,'sentiment', 'INFO', ('Prediction and db update is complete'))
    except Exception as e:
        utils.logger(logger, 'sentiment', 'ERROR',str(e))
        utils.logger(logger, 'sentiment', 'ERROR','Trace')
        return_value={}
        return_value["is_success"] = False
        return_value["message"] = str(e)
        return_value["response_data"] = []
        val=return_value
    return jsonify(val)

@app.route(prefix+"/get_summary", methods=['POST'])
def detect_summary():
    print("get summary")
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    return get_summary()

@app.route(prefix+"/coreference/getcoreference", methods=['POST'])
def detect_coreference():
    print("get coreference resolution")
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    return get_coreference()


@app.route(prefix+"/SimilarityAnalytics", methods=['GET'])
def IngestData_Pheonix():
    cpu,memory=utils.Memorycpu()
    cpu=100-float(cpu)
    memory=100-float(memory)
    if float(cpu)<float(cpu_val) or float(memory)<float(memory_val):
        mes='Available Memory or cpu is low'
        content=str(mes)+'\tcpu\t'+str(cpu)+'\tmemory\t'+str(memory)
        return jsonify(content),500
    else:
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            if validateToken(token) == 'Fail':
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            pass
        elif auth_type=='Forms' or auth_type=='Form':
            token = request.headers['Authorization']
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)
        return SimilarityAnalytics()



@app.route(prefix+"/train_Developer", methods=['GET'])
def train_best_Developer():
  
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    return train_Developer()



@app.route(prefix+"/PredictDeveloper", methods=['POST'])
def predict_best_Developer():
  
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    return PredictDeveloper()

@app.route(prefix+"/PredictIt", methods=['POST','GET'])
def predict_AiService():
    print('inside predictit method')
    if auth_type == 'AzureAD':
        token = request.headers['Authorization']
        if validateToken(token) == 'Fail':
            return Response(status=401)
    elif auth_type == 'WindowsAuthProvider':
        pass
    elif auth_type=='Forms' or auth_type=='Form':
        token = request.headers['Authorization']
        if validateTokenUsingUrl(token)=='Fail':
            return Response(status=401)
    if request.method=='POST':
        return PredictIt()
    else:
        return Response(status=405)


	
def continue_processing():
    time.sleep(120)
    print("Hi, i am from contine_processing")


if __name__ == "__main__":
    if auth_type=='Forms' or auth_type=='Form':
        app.run(host='0.0.0.0',port=5000,threaded=False)
    else:
        app.run(host='0.0.0.0',port='6061')
