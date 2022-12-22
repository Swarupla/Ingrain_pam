# -*- coding: utf-8 -*-
"""
Created on Thu Aug 13 10:41:14 2020

@author: s.siddappa.dinnimani
"""

import time
import os
import sys
import numpy as np
import pandas as pd
#from SSAIutils import encryption
import pickle
import json
import EncryptData
import io
import uuid
from pymongo import MongoClient
import math
import logging
import re
from urllib.parse import urlencode
from datetime import datetime
#from SSAIutils import encryption
from pymongo.errors import ServerSelectionTimeoutError
import configparser

from cryptography.fernet import Fernet
import requests
from collections import ChainMap
from datetime import datetime,timedelta                             
import platform
import psutil
from pythonjsonlogger import jsonlogger
if platform.system() == 'Linux':
    configpath = '/app/pythonconfig.ini'
    EntityConfigpath = "/app/pheonixentityconfig.ini"																																				
elif platform.system() == 'Windows':
    configpath = 'D:\\myWizard\\myWizard.IngrAInAIServices.WebAPI.Python\\pythonconfig.ini'
    EntityConfigpath = "D:\\myWizard\\myWizard.IngrAInAIServices.WebAPI.Python\\pheonixentityconfig.ini"                                                                                                                                                

if platform.system() == 'Windows':                                                                                                                                             
    from requests_negotiate_sspi import HttpNegotiateAuth

import base64
import io
import re
import file_encryptor
from Crypto.Cipher import AES
from datetime import datetime,timedelta
from dateutil import relativedelta
#global num_type, text_cols, DATE_FORMATS, ordinal_vals
# numerical types
#newly added
num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
num_type_int = ['int64', 'int', 'int32']
num_type_float = ['float', 'float32', 'float64']
#newly added
#configpath = '/var/www/myWizard.IngrAInAIServices.WebAPI.Python/pythonconfig.ini'
#EntityConfigpath = "/var/www/myWizard.IngrAInAIServices.WebAPI.Python/pheonixentityconfig.ini"                                                                                                                                             
config = configparser.RawConfigParser()
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
EntityConfig = configparser.RawConfigParser()
EntityConfig.read(EntityConfigpath)   

auth_type = config['auth']['authProvider']
saveModelPath = config['filePath']['saveModelPath']
logpath = config['filePath']['pyLogsPath']
if not os.path.exists(logpath):
    os.mkdir(logpath)
if auth_type == 'WindowsAuthProvider':
    saveModelPath = bytes(saveModelPath, "utf-8").decode("unicode_escape")
if not os.path.exists(saveModelPath):
    os.mkdir(saveModelPath)

pickleKey = config['PICKLE_ENCRYPT']['key']

key = base64.b64decode(config['SECURITY']['Key'])
iv = base64.b64decode(config['SECURITY']['Iv'])
mode = eval(config['SECURITY']['mode'])

class CustomJsonFormatter(jsonlogger.JsonFormatter):
    def add_fields(self, log_record, record, message_dict):
        super(CustomJsonFormatter, self).add_fields(log_record, record, message_dict)
        if not log_record.get('asctime'):
            # this doesn't use record.created, so it is slightly off
            now = datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%fZ')
            log_record['DateTime'] = now
        else:
            log_record['DateTime']  = log_record.get('asctime')
        if log_record.get('levelname'):
            log_record['LogType'] = log_record['levelname'].upper()
        else:
            log_record['LogType'] = record.levelname
        if log_record.get('uniqueId'):
            log_record['UniqueId'] = log_record.get('uniqueId')
        else:
            log_record['UniqueId'] = record.uniqueId    
        if log_record.get('message'):
            log_record['Message'] = log_record['message']
        else:
            log_record['Message'] = record.message
        if log_record.get('correlationId'):
            log_record['CorrelationUId'] = log_record['correlationId']
        else:
            log_record['CorrelationUId'] = record.correlationId
        if log_record['LogType'] == "ERROR":
            if "exc_info" in log_record:
                log_record['Exception'] = log_record["exc_info"]
                del log_record['exc_info']
            else:
                log_record['Exception'] = log_record["message"]
                del log_record['Message']
        del log_record['message']
        del log_record['uniqueId']
        del log_record['correlationId']
        
            
def json_translate(obj):
    if isinstance(obj, CustomJsonFormatter):
        return {"uniqueId": obj.uniqueId,"correlationId":obj.correlationId}

def Memorycpu():
    cpu=psutil.cpu_percent()
    memory=psutil.virtual_memory()[2]
    return(str(cpu),str(memory))

#new function added
def processId():
    pid = os.getpid()
    return(str(pid))
# new function added
def logger(logger, cor, level=None, msg=None,uniqueId=None):
    if logger == 'Get':
        d = datetime.now()
        logger = logging.getLogger(cor)
        if len(logger.handlers) > 0:
            for handler in logger.handlers:
                logger.removeHandler(handler)
            #        handler = logging.StreamHandler()

        handler = logging.FileHandler(
            logpath + str('Python_AIservices') + '_' + str(d.day) + '_' + str(d.month) + '_' + str(d.year) + '.log')
        #formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
        formatter = CustomJsonFormatter(json_default=json_translate)

        #formatter = jsonlogger.JsonFormatter('%(asctime)s - %(levelname)s - %(message)s - %(process)d - %(uniqueId)s',rename_fields={"asctime": "TimeStamp", "levelname": "Level","message":"MessageTemplate"},json_default=json_translate)
        handler.setFormatter(formatter)
        logger.addHandler(handler)
        
        return logger
    elif logger != 'Get':
        LogLevel = config['Logging']['LogLevel']
        if level == 'INFO' and (LogLevel in ['INFO','ALL']):
            logger.setLevel(logging.INFO)
            logger.info(msg,extra={"uniqueId":uniqueId,"correlationId":cor,"ProcessId":processId()})
        elif level == 'DEBUG' and LogLevel == 'ALL':
            logger.setLevel(logging.DEBUG)
            logger.debug(msg)
        elif level == 'WARNING' and LogLevel == 'ALL':
            logger.setLevel(logging.WARNING)
            logger.warning(msg)
        elif level == 'ERROR' and (LogLevel in ['ALL','ERROR']):
            logger.setLevel(logging.ERROR)
            if msg == 'Trace':
                logger.error('Trace', exc_info=True,extra={"uniqueId":uniqueId,"correlationId":cor,"ProcessId":processId()})
            else:
                logger.error(msg,extra={"uniqueId":uniqueId,"correlationId":cor,"ProcessId":processId()})
        elif level == 'CRITICAL' and LogLevel == 'ALL':
            logger.setLevel(logging.CRITICAL)
            if msg == 'Trace':
                logger.critical(exc_info=True)
            else:
                logger.critical(msg)
def min_data():
    min1=config['Min_Data']['min_data']
    return int(min1)
def max_data():
    max_data2train=config['MaxData']['max_data_train']
    return int(max_data2train)                    
def open_dbconn(collection):
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection.get_default_database()
    db_IngestedData = db[collection]

    return connection, db_IngestedData
def get_auth_type():
    auth=config['auth']['authProvider']
    return auth
    
def encryptPickle(files,file_paths):
    file_paths = eval(file_paths)
    for index in range(0,len(files)):
        pickeledData = pickle.dumps(files[index])
        f = Fernet((bytes(pickleKey,"utf-8")))
        encrypted_data = f.encrypt(pickeledData)
        file_paths[index] = open(file_paths[index], 'wb')
        pickle.dump(encrypted_data, file_paths[index])
        
    return

def encryptPickle_for_BestDeveloper(file,file_path):
    pickeledData = pickle.dumps(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    encrypted_data = f.encrypt(pickeledData)
    file_path = open(file_path, 'wb')
    pickle.dump(encrypted_data, file_path)
    return

def decryptPickle(filepath):
    file = open(filepath, 'rb')
    loadedPickle = pickle.load(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    model = pickle.loads(f.decrypt(loadedPickle))
    return model

def GetStopWordsfromDB(correlationId,unid):
     dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
     model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
     dbconn.close()
     stop_words = model_json[0].get("StopWords")
     return stop_words

def GetOutputTypefromDB(correlationId,unid):
     dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
     model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
     dbconn.close()
     output_type = model_json[0].get("Threshold_TopnRecords")
     return output_type

def GetUniqueColfromDB(correlationId,unid):
     dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
     model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
     dbconn.close()
     unique_id = model_json[0].get("ScoreUniqueName")
     return unique_id,model_json[0]
 
def getUseCaseId(correlationId,unid):    
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
    dbconn.close()
    usecase_id = model_json[0].get('UsecaseId')
    return usecase_id

def checkInputParameters(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
    dbconn.close()
    data = eval(model_json[0].get('SourceDetails'))
    try:
        data['Customdetails']['InputParameters'] = eval(data['Customdetails']['InputParameters'])
    except Exception as e:
        error_encounterd = str(e.args[0])
    start = pd.to_datetime(data['Customdetails']['InputParameters']['StartDate'])
    end = pd.to_datetime(data['Customdetails']['InputParameters']['EndDate'])
    if (end > start):
        return False
    else :
        return True
    
def getUserId(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
    dbconn.close()
    try:
        userid = model_json[0].get('CreatedByUser')
        return userid
    except:
        return False

def getUserIdTextSummary(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServicesTextSummaryPrediction')
    model_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
    dbconn.close()
    try:
        userid = model_json[0].get('CreatedBy')
        return userid
    except:
        return False

def callNotificationAPIforTO(correlation_id,uniId,mynewdata):
    
    AppServiceURL = config['BulkPredictionNotificationAPI']['AppServiceURL']
    
    customurl_token,status_code = getEntityToken() #Envi specific
    
    logger(correlation_id,'INFO',"My data is " +str(mynewdata),str(uniId))
    logger(correlation_id,'INFO',"Url is " +str(AppServiceURL),str(uniId))
    
    logger(correlation_id,'INFO','Status Code is '+ str(status_code),str(uniId))
    logger(correlation_id,'INFO','Custom URL Token is '+ str(customurl_token),str(uniId))
    
    if status_code == 200:
        
        result=requests.post(AppServiceURL,data=mynewdata,headers={'Authorization': 'Bearer {}'.format(customurl_token)})
                        
        logger(correlation_id,'INFO','Result is '+ str(result),str(uniId))
    
        logger(correlation_id,'INFO','Status Code for notification API is '+ str(result.status_code),str(uniId))
        logger(correlation_id,'INFO','Content for notification API is '+ str(result.content),str(uniId))
        
    else :
        
        logger(correlation_id,'INFO','Status Code for notification API is not 200',str(uniId))
        
def read_csv(filepath):  #En.................
    with open(filepath, 'rb') as fo:
        text = fo.read()
    x = EncryptData.decryptFile(text)       #De333.......................
    try:
        s = str(x,'utf-8')
    except:
        s = str(x,'latin-1')    
    data_t = pd.read_csv(io.StringIO(s))   
    return data_t

def read_excel(filepath):  #En.................
    with open(filepath, 'rb') as fo:
        text = fo.read()
    toread = io.BytesIO()
    jk = EncryptData.decryptFile(text)
    toread.write(jk)                         #De444.......................
    toread.seek(0)  
    data_t = pd.read_excel(toread)
    return data_t

def CheckTextCols(data_copy):
    # replace any space from feature names
    #data_copy.columns = data_copy.columns.str.replace(' ','')
    
    # get headers of dataframe
    names = data_copy.columns.values.tolist()
    UnknownTypes = []
    text_cols = []
    #chekings for categorical,countinues and rext data      
    for col in names:
        if data_copy[col].dtype.name =='bool':
            data_copy[col] =  data_copy[col].astype('category')
        try:
            data_copy[col].dropna(inplace=True)
            #calculating unique percenatge for values in column
            try:
                uniquePercent = len(data_copy[col].unique())*1.0/len(data_copy[col])*100
            except:
                uniquePercent = 0 
            #classifying categorical and text column based on unique percentage of values in column 2
            if data_copy[col].dtype == 'object':
                word_list = []
                for word in data_copy[col].values:
                    word = str(word)
                    word_list.append(len(word.split()))
                Avg_word_count = round(sum(word_list)*1.0/len(data_copy[col]))
                

                if uniquePercent > 2 and Avg_word_count > 1 : 
                    data_copy[col] =  data_copy[col].astype(str)
                    text_cols.append(col)
                else:
                    UnknownTypes.append(col)
        except Exception as e:
            print('Exception from checkTExtColsutils function',str(e))
            pass

                

    return text_cols

def UpdateDataRecords(correlationid,data,flag = None):
      print("InsideUPDATE Recordsss...........................")
      dbconn, dbcollection = open_dbconn('AICoreModels')
      args = list(dbcollection.find({"CorrelationId": correlationid}))
      print("InsideUPDATE Recordsss1...........................")
      app = args[0]["ApplicationId"]
      cid = args[0]["ClientId"]  
      dcuid= args[0]["DeliveryConstructId"]
      user = args[0]["ModifiedBy"]
      dbconn.close()
      print(app,cid,dcuid,user)
      print("InsideUPDATE Recordsss2...........................")
      if app == None:
          app = "Ingrain"
      dbconn1, dbcollection1 = open_dbconn('AIServiceRecordsDetails')
      print("InsideUPDATE Recordsss3...........................")
      dbcollection1.insert({"CorrelationId": correlationid,
                "ClientId": cid,
                "DeliveryConstructId": dcuid,
                "OriginalRecords" :  str(list(data.shape)),
                "Application" : app,
                "CreatedByUser": user,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))})
      print("InsideUPDATE Recordsss5...........................")
      dbconn1.close()
    
def update_usecasedefeinition(DataSetUId,correlationId):
    dbconn, dbcollection = open_dbconn("DataSetInfo")
    data_json = list(dbcollection.find({"DataSetUId": DataSetUId}))
    dbconn.close()
    if data_json != {}:
        dbconn_ps_ingestdata, dbcollection_ps_ingestdata = open_dbconn("AIServiceRecordsDetails")       
        dbcollection_ps_ingestdata.insert({
                    "CorrelationId": correlationId,
                    "ClientId": data_json[0].get('ClientUId'),
                    "DeliveryConstructId": data_json[0].get('DeliveryConstructUId'),
                    "OriginalRecords":data_json[0].get('ValidRecordsDetails').get('Records'),
                    "Application" : "Ingrain",
                    "CreatedByUser": data_json[0].get('CreatedBy'),
                    "CreatedOn": data_json[0].get('ModifiedOn')
                    
                    
                })
    return

def files_split(data,Incremental = False,appname=None):#1st function
    data = data.reset_index(drop=True)
    print("data size in flag split::"+str(data.shape))
    #data.dropna(inplace=True)
    print("data size in flag split after dropna::"+str(data.shape))
    t_filesize = (sys.getsizeof(data) / 1024) / 1024
    nulls = data.isnull().sum(axis=1)
    print("Utils Line 341 "+str(type(nulls)))
    null_index = nulls.idxmin()
    row1size = (sys.getsizeof(data.iloc[[null_index]]) / 1024) / 1024
    capacity = 0.1
    nrows = data.shape[0]
    #print("falg:::::::::::::",t_filesize > capacity)
    if t_filesize > capacity and not Incremental:
        if row1size != 0: rows = math.ceil(capacity / row1size)
        if rows != 0:
            chunks = np.split(data, range(1 * rows, (nrows // rows + 1) * rows, rows))
        #print("chunks::::::::::::::",len(chunks))
    elif t_filesize <= capacity and not Incremental:
        chunks = []
        chunks.append(data)
    
    return chunks, t_filesize


def file_split(data): #return rows from file_split
    chunks=[]
    t_filesize=0
    try:
        data = data.reset_index(drop=True)
        t_filesize = (sys.getsizeof(data) / 1024) / 1024
        nulls = data.isnull().sum(axis=1)
        null_index = nulls.idxmin()
        row1size = (sys.getsizeof(data.iloc[[null_index]]) / 1024) / 1024
        capacity = 2
        nrows = data.shape[0]
        if t_filesize > capacity:
            if row1size != 0: rows = math.ceil(capacity / row1size)
            if rows != 0:
                chunks = np.split(data, range(1 * rows, (nrows // rows + 1) * rows, rows))
        elif t_filesize <= capacity:
            chunks = []
            chunks.append(data)
    except Exception as e:
        print('Exception from file_split function',e)
        pass
    return chunks, t_filesize

#dbcount=0
def updQdb(correlationId, status, progress, pageInfo, userId,uniid,message=None):
    dbconn, dbcollection = open_dbconn("AIServiceRequestStatus")
    if not progress.isdigit() and status != 'E':
        if str(progress)=='RCVD_request_after_token_validation':
            message = progress
            rmessage = "In - Progress"
            progress = '0'
        else:
            message = progress
            rmessage = "Task Complete"
            progress = '0'
    elif status == "C":
        message = "Task Complete"
        rmessage = "Task Complete"
    elif status == "E":
        if message!=None:
            message=str(message)
            rmessage = str(message)
        else:
            message = "Error"
            rmessage = "Error"
    else:
        message = "In - Progress"
        rmessage = "In - Progress"
    try:
        dbcollection.update_many({"CorrelationId": correlationId,"PageInfo": pageInfo,"UniId":uniid},
                                 {'$set': {"CorrelationId": correlationId,
                                           "Status": status,
                                           "Progress": progress,
                                           "RequestStatus": rmessage,
                                           "Message": message,
                                           "PageInfo": pageInfo,
                                           "CreatedByUser": userId,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "ModifiedByUser": userId}})
      
    except ServerSelectionTimeoutError:
        error_encounterd = str(e.args[0])
    return

def updPredStatus(correlationId, status, progress, pageInfo, userId,uniid):
    dbconn, dbcollection = open_dbconn("AIServiceRequestStatus")
    
    if status=='P' and progress=='10%':
        message = "Predicition initiated"
        
    elif status=='P' and progress=='50%':
        message = "Predicition in Progress" 
    
    elif status=='C' and progress=='100%':
        message = " Predicition Completed"
    elif status=='E':
        message = "Error occured during Prediction"
        
    try:
        dbcollection.update_many({"CorrelationId": correlationId,"PageInfo": pageInfo,"UniId":uniid},
                                 {'$set': {"CorrelationId": correlationId,
                                           "Status": status,
                                           "Progress": progress,
                                           "Message": message,
                                           "PageInfo": pageInfo,
                                           "CreatedByUser": userId,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "ModifiedByUser": userId}},upsert=True)
      
    except ServerSelectionTimeoutError:
        error_encounterd = str(e.args[0])
        pass
    
def getlastDataPointCDM(correlationId, *argv):
    dbconn, dbcollection = open_dbconn('AIServiceIngestData')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    lastDate = data["lastDateDict"]
    for arg in argv:
        lastDate = lastDate[arg]
    if datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S').strftime('%Y-%m-%d')==datetime.today().strftime('%Y-%m-%d'):
        return (datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')
    else:
        #return (datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')
        return (datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S')+timedelta(days=1)).strftime('%m/%d/%Y')

def update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId):
    dbconn, dbcollection = open_dbconn("DataSet_IngestData")
    data_json = list(dbcollection.find({"DataSetUId": DataSetUId}))
    dbconn.close()
    if data_json != {}:
        dbconn_ps_ingestdata, dbcollection_ps_ingestdata = open_dbconn("AIServiceIngestData")       
        dbcollection_ps_ingestdata.insert({
                    "CorrelationId": correlationId,
                    "InputData": {},
                    "SourceDetails": data_json[0].get('SourceDetails'),
                    "ColumnUniqueValues": data_json[0].get('ColumnUniqueValues'),
                    "lastDateDict": data_json[0].get('lastDateDict'),
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "DataSetUId":DataSetUId
                })
        dbconn_AIServiceIngestData, dbcollection_AIServiceIngestData = open_dbconn("AIServiceRequestStatus")       
        dbcollection_AIServiceIngestData.update_many({"CorrelationId": correlationId,"PageInfo": pageInfo},
                                 {'$set': {"CorrelationId": correlationId,"ColumnNames":data_json[0].get('ColumnsList'),"UniqueColumns":data_json[0].get('ColumnsList')}})
        dbconn_AIServiceIngestData.close()
        return "Ingestion completed"
        
    else:
        return "datasetUID not exists in the Large data collection"
def getRequestParams(correlationId,pageInfo,unid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    if pageInfo != "":
        args = list(dbcollection.find({"CorrelationId": correlationId, "PageInfo": pageInfo,"UniId":unid}))
        print(args)
        return  args[0]["SourceDetails"],args[0]["SelectedColumnNames"],args[0]
    else:
        args = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
        cols = []
        for item in args:
            print(item,".................................................")
            if item["PageInfo"] in ["TrainModel","Ingest_Train","Retrain", "PredictData","TrainAndPredict"]:
                cols = item["SelectedColumnNames"]
        return "",cols

def customRequestParamsForTraining(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    args = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
    cols = []
    print('******new to requestparams',args)
    val=args[0]['SourceDetails']
    val=eval(val)
    
    for item in args:
        print(item,".................................................")
        if item["PageInfo"] in ["TrainModel","Ingest_Train","Retrain", "PredictData","TrainAndPredict"]:
            cols = item["SelectedColumnNames"]
            if len(cols) == 0:
                cols = item["ColumnNames"]
                
    print('******val',val)
    val=val['Customdetails']
    return val,cols

def customRequestParamsForTO(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    args = list(dbcollection.find({"CorrelationId": correlationId,"UniId":unid}))
   
    print('******new to requestparams',args)
    val=args[0]['SourceDetails']
    val=eval(val)
   
    cols =  args[0].get('ColumnNames')
   
    print('******val',val)
    val=val['Customdetails']
    return val,cols

def updateAIDataRecords(correlationid,pageInfo,actualdata,predicteddata,create_date,collectionName=None,flag = None):
    if collectionName==None:
        dbconn1, dbcollection = open_dbconn('AIServicesPrediction')
        dbcollection.insert_many([{"CorrelationId": correlationid,
                                            "ActualData": actualdata,
                                            "PredictedData": predicteddata,
                                            "Status":'',
                                            "Progress":'',
                                            "PageInfo": pageInfo,
                                            "CreatedOn":create_date,
                                            "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                            "ModifiedByUser": ''}])
    else:
        dbconn1, dbcollection = open_dbconn(collectionName)
        dbcollection.insert_many([{"CorrelationId": correlationid,
                                            "ActualData": actualdata,
                                            "PredictedData": predicteddata,
                                            "Status":'',
                                            "Progress":'',
                                            "PageInfo": pageInfo,
                                            "CreatedOn":create_date,
                                            "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                            "ModifiedByUser": ''}])    

    dbconn1.close()

def aiServicesPrediction(correlationid,pageInfo,actualdata,predicteddata,create_date,collectionName=None,flag = None):
        actualdata=EncryptData.EncryptIt(actualdata)
        predicteddata=EncryptData.EncryptIt(predicteddata)
        updateAIDataRecords(correlationid,pageInfo,actualdata,predicteddata,create_date,collectionName)

def updateModelStatus(correlationid,UniId,modelname,modelstatus,status_msg):
    
    dbconn, dbcollection = open_dbconn('AICoreModels')
    
    if(modelstatus == "Error"):
        model_json = list(dbcollection.find({"CorrelationId": correlationid}))
        print(model_json,'from AI core models')
        model_path = model_json[0]["PythonModelName"]
        print(model_path)
        if(model_path == "" or model_path == None):
            dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":modelname,
                                                                       "ModelStatus":modelstatus,
                                                                       "ModifiedOn":  datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                                                       "StatusMessage":status_msg}})
        else:
            dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":model_path,
                                                                       "ModelStatus":"ReTrain Failed",
                                                                        "ModifiedOn":datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                                                       "StatusMessage":status_msg}})
    else:
        dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":modelname,
                                                                       "ModelStatus":modelstatus,
                                                                       "ModifiedOn":datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                                                       "StatusMessage":status_msg}})

def getEncryptionFlag(corrid):  #En.................
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    print("******************* corrid " ,corrid)
    #flag = list(dbcollection.find({"CorrelationId": corrid}))[0]["DBEncryptionRequired"]
    flag = False
    return flag

def DateTimeStampParser(data):
    rege = [r'[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}[\t\s]\d{2}:\d{2}:\d{2}|[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}']
    rege1 = r'(\w*) '
    for col in data.columns:
        l = list(data[col][data[col].notnull()])
        if len(l) > 0:
            temp = l[0]
            if isinstance(temp, type(datetime)):
                data[col] = pd.to_datetime(data[col], errors='coerce')
                data.dropna(subset=[col], inplace=True)
            elif re.compile(rege1).match(str(temp)) is None:
                for pattern in rege:
                    if re.compile(pattern).match(str(temp)) is not None:
                        try:
                            data[col] = pd.to_datetime(data[col])
                        except Exception as e:
                            error_encounterd = str(e.args[0])

    return data


def getUniId(correlationId, requestId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
    return args["UniId"]


def getFrequency(correlationId, requestId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
    if 'Frequency' in args:
        return args["Frequency"]
    else:
        return None
    



def save_data_chunk_bulk(chunks, collection, corid,uniId,userId,appName,uniqueIdentifier, pageInfo, Incremental=False,requestId=None,sourceDetails=None, colunivals=None,
                     timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None):
    #print("inside save_data_chunks")
    
    try :
        dbconn, dbcollection = open_dbconn(collection)
    
        dbcollection.remove({"CorrelationId": corid})
    except Exception as e:
        error_encounterd = str(e.args[0])
    
    EnDeRequired = getEncryptionFlag(corid)
    #print("Herere", lastDateDict)
    
    for chi in range(len(chunks)):
        #print(chi)
        Id = str(uuid.uuid4())
        if not timeseries:
            # data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            try:
                chunks[chi] = chunks[chi].set_index('index')
                data_json = chunks[chi].apply(lambda x: dict(x.dropna()), axis=1).to_json(orient='index', date_format='iso', date_unit='s')
            except Exception as e:
                list_indexes = []
                for i, row in chunks[chi].iterrows():
                    try:
                        pd.DataFrame(row).to_json(orient='index', date_format='iso', date_unit='s')
                    except Exception as e:
                        list_indexes.append(i)
                chunks[chi].drop(index=list_indexes, inplace=True)
                chunks[chi].reset_index(drop=True, inplace=True)
                data_json = chunks[chi].to_json(orient='index', date_format='iso', date_unit='s')
            # SOC Identifying date,datetimestamps
            data = DateTimeStampParser(chunks[chi])
            DataType = {}
            #print(data_json)
            for i, j in data.iteritems():
                if str(data[i].dtypes) == 'object':
                    data[i]
                    DataType.update({i: 'Category'})
                elif str(data[i].dtypes) == 'float64':
                    DataType.update({i: 'Float'})
                elif str(data[i].dtypes) == 'int64':
                    DataType.update({i: 'Integer'})
                elif str(data[i].dtypes) in ['datetime64[ns]', 'datetime64[ns, UTC]']:
                    DataType.update({i: 'Date'})
                elif str(data[i].dtypes) == 'bool':
                    DataType.update({i: 'bool'})
            # EOC Identifying date,datetimestamps

        elif timeseries == "one":
            data_json = {each: chunks[chi][each].reset_index().to_json(orient='index', date_format='iso') for each in
                         chunks[chi]}
            if EnDeRequired:
                data_json = str(data_json)
            DataType = {}
        else:
            data_json = {timeseries: chunks[chi].reset_index().to_json(orient='index', date_format='iso')}
            
            DataType = {}
        
        EnDeRequired = config['BulkPredictionNotificationAPI']['EncryptionFlag']
        
        if str(EnDeRequired) == "True":                                  #EN555.................................
            data_json = EncryptData.EncryptIt(data_json)

        #print("data_json")
        if not Incremental:
            print(len(data_json),chi,"............................................................................................................................................................................................................................................")
            if datapre == None:
                dbcollection.insert({
                    "CorrelationId": corid,
                    "InputData": data_json,
                    "UniqueId":uniId,
                    "UniqueColumnIdentifier":uniqueIdentifier,
                    #"ColumnsList": columns,
                    "DataType": DataType,
                    "App": appName,
                    "Status":"C",
                    "Progress":100,
                    "Page_number":int(chi+1),
                    "Total Number of pages":int(len(chunks)),
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "previousLastDate":previousLastDate,
                    "lastDateDict": lastDateDict,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                })
            else:
                if chi == 0:
                    dbcollection.remove({"CorrelationId": corid})
                '''dbcollection.insert({"CorrelationId"     : corid},
                                         {"$set":{ 
                                     "CorrelationId":corid,
                                     "InputData":data_json,
                                     "PageInfo":pageInfo,
                                     "ColumnsList":columns,
                                     "CreatedByUser":userId,
                                     "CreatedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                                     }})'''
    
                dbcollection.insert({
                    "CorrelationId": corid,
                    "InputData": data_json,
                    #"ColumnsList": columns,
                    "DataType": DataType,
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                })
        else:
            id1 = getUniId(corid, requestId)
            Frequency = getFrequency(corid, requestId)	
            if len(data_json) != 2:			
                if datapre == None:
                    dbcollection.insert({
                        "CorrelationId": corid,
                        "UniqueId": id1,
                        "ActualData": data_json,
                        "CreatedByUser": userId,
                        "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":userId,
                        "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "_id":Id,
                        "Status":"P",
                        "Progress":"10",
                        "Chunk_number":str(chi),
                        "ErrorMessage":"",
                        "Frequency":Frequency										 
                })
                else:
                    if chi == 0:
                        dbcollection.remove({"CorrelationId": corid})
                    dbcollection.insert({
                        "CorrelationId": corid,
                        "UniqueId": id1,
                        "ActualData": data_json,
                        "CreatedByUser": userId,
                        "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":userId,
                        "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "_id":Id,
                        "Status":"P",
                        "Progress":"10",                
                        "Chunk_number":str(chi),
                        "ErrorMessage":"",
                        "Frequency":Frequency										 
                })
            
       

    
    dbconn.close()
    
def save_data_chunk(chunks, collection, corid,uniId,userId,appName,shape,uniqueIdentifier,statusMessage, pageInfo, Incremental=False,requestId=None,sourceDetails=None, colunivals=None,
                     timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None):
    #print("inside save_data_chunks")
    dbconn, dbcollection = open_dbconn(collection)
    EnDeRequired = getEncryptionFlag(corid)
    #print("Herere", lastDateDict)
    
    for chi in range(len(chunks)):
        #print(chi)
        Id = str(uuid.uuid4())
        if not timeseries:
            # data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            try:
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            except Exception as e:
                list_indexes = []
                for i, row in chunks[chi].iterrows():
                    try:
                        pd.DataFrame(row).to_json(orient='records', date_format='iso', date_unit='s')
                    except Exception as e:
                        list_indexes.append(i)
                chunks[chi].drop(index=list_indexes, inplace=True)
                chunks[chi].reset_index(drop=True, inplace=True)
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            # SOC Identifying date,datetimestamps
            data = DateTimeStampParser(chunks[chi])
            DataType = {}
            #print(data_json)
            for i, j in data.iteritems():
                if str(data[i].dtypes) == 'object':
                    data[i]
                    DataType.update({i: 'Category'})
                elif str(data[i].dtypes) == 'float64':
                    DataType.update({i: 'Float'})
                elif str(data[i].dtypes) == 'int64':
                    DataType.update({i: 'Integer'})
                elif str(data[i].dtypes) in ['datetime64[ns]', 'datetime64[ns, UTC]']:
                    DataType.update({i: 'Date'})
                elif str(data[i].dtypes) == 'bool':
                    DataType.update({i: 'bool'})
            # EOC Identifying date,datetimestamps

        elif timeseries == "one":
            data_json = {each: chunks[chi][each].reset_index().to_json(orient='records', date_format='iso') for each in
                         chunks[chi]}
            if EnDeRequired:
                data_json = str(data_json)
            DataType = {}
        else:
            data_json = {timeseries: chunks[chi].reset_index().to_json(orient='records', date_format='iso')}
            
            DataType = {}
        
        EnDeRequired = config['BulkPredictionNotificationAPI']['EncryptionFlag']
        
        if str(EnDeRequired) == "True":                                  #EN555.................................
            data_json = EncryptData.EncryptIt(data_json)

        #print("data_json")
        if not Incremental:
            print(len(data_json),chi,"............................................................................................................................................................................................................................................")
            if datapre == None:
                dbcollection.insert({
                    "CorrelationId": corid,
                    "InputData": data_json,
                    "UniqueId":uniId,
                    "UniqueColumnIdentifier":uniqueIdentifier,
                    #"ColumnsList": columns,
                    "DataType": DataType,
                    "App": appName,
                    "Status":"C",
                    "Progress":100,
                    "StatusMessage":statusMessage,
                    "Page_number":int(chi+1),
                    "Total Number of pages":int(len(chunks)),
                    "TotalRecordCount":int(shape),
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "previousLastDate":previousLastDate,
                    "lastDateDict": lastDateDict,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                })
            else:
                if chi == 0:
                    dbcollection.remove({"CorrelationId": corid})
                '''dbcollection.insert({"CorrelationId"     : corid},
                                         {"$set":{ 
                                     "CorrelationId":corid,
                                     "InputData":data_json,
                                     "PageInfo":pageInfo,
                                     "ColumnsList":columns,
                                     "CreatedByUser":userId,
                                     "CreatedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                                     }})'''
    
                dbcollection.insert({
                    "CorrelationId": corid,
                    "InputData": data_json,
                    #"ColumnsList": columns,
                    "DataType": DataType,
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                })
        else:
            id1 = getUniId(corid, requestId)
            Frequency = getFrequency(corid, requestId)	
            if len(data_json) != 2:			
                if datapre == None:
                    dbcollection.insert({
                        "CorrelationId": corid,
                        "UniqueId": id1,
                        "ActualData": data_json,
                        "CreatedByUser": userId,
                        "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":userId,
                        "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "_id":Id,
                        "Status":"P",
                        "Progress":"10",
                        "Chunk_number":str(chi),
                        "ErrorMessage":"",
                        "Frequency":Frequency										 
                })
                else:
                    if chi == 0:
                        dbcollection.remove({"CorrelationId": corid})
                    dbcollection.insert({
                        "CorrelationId": corid,
                        "UniqueId": id1,
                        "ActualData": data_json,
                        "CreatedByUser": userId,
                        "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":userId,
                        "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "_id":Id,
                        "Status":"P",
                        "Progress":"10",                
                        "Chunk_number":str(chi),
                        "ErrorMessage":"",
                        "Frequency":Frequency										 
                })
            
       

    
    dbconn.close()
    
def data_from_chunk(corid, collection, lime=None, recent=None):
    rege = [r'[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}[\t\s]\d{2}:\d{2}:\d{2}|[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}']
    rege1 = r'(\w*) '
    EnDeRequired = getEncryptionFlag(corid)
    dbconn, dbcollection = open_dbconn(collection)
    
    if not lime:
        data_json = dbcollection.find({"CorrelationId": corid})
    else:
        if lime == True:
            data_json = dbcollection.find({"CorrelationId": corid}).limit(1)
        else:
            data_json = dbcollection.find({"CorrelationId": corid}).limit(1)

    if recent != None:
        count = data_json.collection.count_documents({"CorrelationId": corid})
        temp = {}
        for countj in range(count):
            temp.update({datetime.strptime(data_json[countj].get('CreatedOn'), '%Y-%m-%d %H:%M:%S'): data_json[
                countj].get('UploadId')})
            recentdoc = temp.get(max(temp.keys()))
        data_json = dbcollection.find({"CorrelationId": corid, "UploadId": recentdoc})
        count = data_json.collection.count_documents({"CorrelationId": corid, "UploadId": recentdoc})
    else:
        count = data_json.collection.count_documents({"CorrelationId": corid})

    data_t1 = pd.DataFrame()
    if not lime:
        for counti in range(count):
            t = data_json[counti].get('InputData')
            #print(len(t),counti,"=====================================================================================================================================================================================================")
            if EnDeRequired :
                t = EncryptData.DescryptIt(base64.b64decode(t)) 
							 
            data_t = pd.DataFrame(json.loads(t))
            data_t1 = data_t1.append(data_t, ignore_index=True)
    else:
        count = 1
        for counti in range(count):
            t = data_json[counti].get('InputData')
            if EnDeRequired :
                t = EncryptData.DescryptIt(base64.b64decode(t))
                #print(t)				#En.....................
            data_t = pd.DataFrame(json.loads(t))
            data_t1 = data_t1.append(data_t, ignore_index=True)

    #    for counti in range(count)    :
    #        data_t = data_json[counti].get('Inputdata')
    #        for row in data_t:
    #            data_t1 = data_t1.append(json_normalize(json.loads(row)),ignore_index=True, format='ISO')

    dbconn.close()
    output = data_t1.to_dict(orient='records')
    return output

def save_data_chunks(chunks, collection, corid, pageInfo, userId, UniId,columns,uniquecolumns=None, sourceDetails=None, colunivals=None,
                     timeseries=None, datapre=None, lastDateDict=None,ingestion_message=None,df_shape=None):
    #newly added ----dbconn1, dbcollection1 = open_dbconn(collection)
    #dont touch existing file_split() and modify to a new function to return row value
    #write custom function to get the row count
    dbconn_requeststatus, dbcollection_requeststatus = open_dbconn("AIServiceRequestStatus")
    dbcollection_requeststatus.update_many({"CorrelationId": corid,"PageInfo": pageInfo,"UniId":UniId},
                                 {'$set': {"CorrelationId": corid,
                                           "ColumnNames"  : columns,
                                           "UniqueColumns" : uniquecolumns,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "Ingestion_Message":ingestion_message,
                                           "Dataframe_Shape":df_shape,
                                           "ModifiedByUser": userId}})
    dbconn_requeststatus.close()
    
    dbconn, dbcollection = open_dbconn(collection)
    for chi in range(len(chunks)):
        print(chi)
       
        if not timeseries:
            # data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            try:
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            except Exception as e:
                list_indexes = []
                for i, row in chunks[chi].iterrows():
                    try:
                        pd.DataFrame(row).to_json(orient='records', date_format='iso', date_unit='s')
                    except Exception as e:
                        list_indexes.append(i)
                chunks[chi].drop(index=list_indexes, inplace=True)
                chunks[chi].reset_index(drop=True, inplace=True)
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            # SOC Identifying date,datetimestamp


        elif timeseries == "one":
            data_json = {each: chunks[chi][each].reset_index().to_json(orient='records', date_format='iso') for each in
                         chunks[chi]}
          
        else:
            data_json = {timeseries: chunks[chi].reset_index().to_json(orient='records', date_format='iso')}
        logger(corid,'INFO','******from data json')
        logger(corid,'INFO',data_json)
        if pageInfo=='PredictData':
            logger(corid,'INFO','******from data json10')
            logger(corid,'INFO',data_json)
        data_json = EncryptData.EncryptIt(data_json)
        if pageInfo != 'PredictData':# wehave to add count.
            if datapre == None:
                dbcollection.insert({
                "CorrelationId": corid,
                "InputData": data_json,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "lastDateDict": lastDateDict,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
            })
            else:
                dbcollection.insert({
                "CorrelationId": corid,
                "InputData": data_json,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
            })
        else:
            if datapre == None:
                dbcollection.insert({
                "CorrelationId": corid,
                "ActualData": data_json,
                "UniId":UniId,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "lastDateDict": lastDateDict,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                "Status":"P",
                "Progress":"10",
                "Chunk_number":str(chi),
                "ErrorMessage":"",
                "PredictedData":""
            })
            else:
                dbcollection.insert({
                "CorrelationId": corid,
                "ActualData": data_json,
                "UniId":UniId,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                "Status":"P",
                "Progress":"10",
                "Chunk_number":str(chi),
                "ErrorMessage":"",
                "PredictedData":""
            })          

    dbconn.close()
    
#newly added
def getUniqueValues(df):
    UniqueValuesCol = {}
    typeDict = {}
    for col in df:
        typeDict[col] = df.dtypes[col].name
        if df[col].dtype.name == 'datetime64[ns]':
            UniqueValuesCol[col] = list(df[col].dropna().unique().astype('str'))
        if df[col].dtype.name in num_type or df[col].dtype.name == 'category':
            UniqueValuesCol[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())]
        if df[col].dtype.name == 'object':
            if len(list(df[col].dropna().unique())) > 2:
                UniqueValuesCol[col] = list(df[col].dropna().unique())[:3]
            else:
                UniqueValuesCol[col] = list(df[col].dropna().unique())
    return UniqueValuesCol, typeDict
#newly added
         
def save_data_multi_file(correlationId, pageInfo, userId, UniId,multi_source_dfs, parent, mapping, mapping_flag, ClientUID,
                         DeliveryConstructUId, insta_id, auto_retrain, datapre=None, lastDateDict=None,
                         MappingColumns=None,usecase = None):
    all_columns = {}
    cols = []
    parent_file_index = 0
    counter = 0
    logger(correlationId, 'INFO',str(multi_source_dfs) ,str(UniId))

    
    
    if mapping_flag != 'True':
        if len(multi_source_dfs['Custom']) != 0:

            if isinstance(multi_source_dfs['Custom'], pd.DataFrame):
                data_frame = multi_source_dfs['Custom']                
                columns = list(data_frame.columns)
                unique_columns = list(data_frame.columns)
                chunks, size = file_split(data_frame)
                df_shape = list(data_frame.shape)
                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                 "Source": "Custom"}
                #added by me
                if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':#need to give that
                    save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, userId,UniId, columns, unique_columns,
                    sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #new method added
                elif auto_retrain:
                    size = append_data_chunks(data_frame, "AIServiceIngestData", correlationId, pageInfo, userId,
                                                 UniId,columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)                                
                else: #here we have to add
                    try:
                        UniqueValuesCol,typeDict=getUniqueValues(data_frame) #newly added
                    except Exception as e:
                        UniqueValuesCol = None
                        error_encounterd = str(e.args[0])

                    #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                    #sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                   
                    UpdateDataRecords(correlationId,data_frame)
                    save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                    sourceDetails, colunivals=UniqueValuesCol, lastDateDict=lastDateDict,ingestion_message=ingestion_message) #newlyadded
                return "single_file",data_frame
            else:
                if pageInfo == 'Retrain' or auto_retrain:
                    updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                    
                else:
                    updateModelStatus(correlationId,UniId,"","Warning",multi_source_dfs['Custom']['Custom'])
                updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId,multi_source_dfs['Custom'])
                
                raise Exception(multi_source_dfs['Custom'])

        else:
            for key in multi_source_dfs.keys():

                if multi_source_dfs[key] != {}:
                    if key == 'Entity':
                        
                        type_key = ''
                        for key1 in multi_source_dfs[key].keys():

                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                details = {}
                                data_frame = multi_source_dfs[key][key1]
                                # cols_details1 = data_frame.dtypes.apply(lambda x: x.name).to_dict()
                                cols_details = {col: data_frame[col].dtypes.name for col in data_frame.columns if
                                                col in MappingColumns[key1]}
                                # cols_details = MappingColumns[str(key1).lower()]
                                details["Columns"] = cols_details
                                details["FileExtensionOrig"] = 'Entity'
                                cols.append(list(data_frame.columns))
                                if parent['Name'] == key1 and parent['Type'] == 'Entity':
                                    parent_file_index = counter
                                counter += 1
                                all_columns[correlationId + "_" + key1] = details
                            
                    elif key == 'File':
                        
                        for key1 in multi_source_dfs[key].keys():
                            type_key = ''
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                details = {}
                                data_frame = multi_source_dfs[key][key1]
                                file_name = os.path.basename(key1).split('.')[0]
                                extension = os.path.basename(key1).split('.')[1]
                                cols_details = data_frame.dtypes.apply(lambda x: x.name).to_dict()
                                details["Columns"] = cols_details
                                details["FileExtensionOrig"] = extension
                                cols.append(list(data_frame.columns))
                                if parent['Name'] == file_name and parent['Type'] == 'File':
                                    parent_file_index = counter
                                counter += 1
                                all_columns[file_name] = details
                           
                          
        if counter == 0:
            for key in multi_source_dfs.keys():
                    if key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], str):
                                if pageInfo == 'Retrain' or auto_retrain:
                                    updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                    
                                else:                       
                                    updateModelStatus(correlationId,UniId,"","Warning",multi_source_dfs[key][key1])
                                    
                                updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                #if auto_retrain == False:
                                   # updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                               # else:
                                #    updQdb(correlationId, 'C', "100", pageInfo, userId,UniId)
                                #    updateModelStatus(correlationId,UniId,"","Completed",multi_source_dfs[key][key1])
                                
                                raise Exception("Data Not available for selection")
                                
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            print('from utils multiselect',multi_source_dfs[key][key1])
                            if isinstance(multi_source_dfs[key][key1], str):
                                if pageInfo == 'Retrain' or auto_retrain:
                                    updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                    
                                else:
                                    
                                    updateModelStatus(correlationId,UniId,"","Warning",multi_source_dfs[key][key1])
                                updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                
                                raise Exception("Data Not available for selection")
        elif counter == 1:
            
            for key in multi_source_dfs.keys():
                if multi_source_dfs[key] != {}:
                    if key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'Entity'
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": type_key,
                                                 "Source": "CDM"}
                                if usecase == "NextWord":
                                    
                                    columns = CheckTextCols(data_frame)
                                    if len(columns) == 0:
                                        if pageInfo == 'Retrain' or auto_retrain:
                                            
                                            updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                        else:
                                            
                                            updateModelStatus(correlationId,UniId,"","Warning","There is no text columns for your data,please upload data with text columns")
                                        updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                        raise Exception("There is no text columns for your data,please upload data with text columns")
                                        
                                elif usecase == "Similarity":
                                    
                                    columns = CheckTextCols(data_frame)
                                    
                                    unique_columns = list(data_frame.columns)
                                    
                                    if len(columns) == 0:
                                        if pageInfo == 'Retrain' or auto_retrain:
                                            
                                            updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                        else:
                                            
                                            updateModelStatus(correlationId,UniId,"","Warning","There is no text columns for your data,please upload data with text columns")
                                        updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                        raise Exception("There is no text columns for your data,please upload data with text columns")
                                    
                                else:
                                    
                                    columns = list(data_frame.columns)
                                chunks, size = file_split(data_frame)
                                df_shape = list(data_frame.shape)
                                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                

                                if len(chunks)==0:
                                    if pageInfo == 'Retrain' or auto_retrain:
                                        updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")    
                                    else:                       
                                        updateModelStatus(correlationId,UniId,"","Warning","There is no text columns for your data,please upload data with text columns")
                                    updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                    raise Exception("There is no text columns for your data,please upload data with text columns")
                                else:
                                    print(columns,"....................................")
                                    if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':#need to give that
                                        save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                            sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #new method added
                                    elif auto_retrain:
                                        size = append_data_chunks(data_frame, "AIServiceIngestData", correlationId, pageInfo, userId,
                                                    UniId,columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)                                                 
                                    else:
                                        UniqueValuesCol,typeDict=getUniqueValues(data_frame) #newly added
                                        #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                                        #sourceDetails, colunivals=None, lastDateDict=lastDateDict)
#                                        UpdateDataRecords(correlationId,data_frame)
                                        save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                            sourceDetails, colunivals=UniqueValuesCol, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #newlyadded
                                        #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId,
                                    #             columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                    #newly added for custom
                    elif key=='Custom':
                        if auto_retrain:
                            _,sel_cols,_ = getRequestParams(correlationId,pageInfo,UniId)
                            unique_columns =columns.copy()
                            if len(set(sel_cols).intersection(columns)) != len(sel_cols):
                                    if pageInfo == 'Retrain' or auto_retrain:
                                        updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                        
                                    else:
                                        
                                        updateModelStatus(correlationId,UniId,"","Warning","uploaded File missing trained columns, please check the data and Upload again")
                                    updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                    raise("Unmatched Columns")
                        chunks, size = file_split(data_frame)
                        df_shape = list(data_frame.shape)
                        ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                        sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                            "Source": type_key}
                        if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':#need to give that
                            save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #new method added
                        else:
                            #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                            #                sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                            UniqueValuesCol,typeDict=getUniqueValues(data_frame) #newly added
                            #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                                #sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                            UpdateDataRecords(correlationId,data_frame)
                            save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                sourceDetails, colunivals=UniqueValuesCol, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #newlyadded
                    #new added for custom
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'File'
                                if usecase == "NextWord":
                                    columns = CheckTextCols(data_frame)
                                    if len(columns) == 0:
                                        if pageInfo == 'Retrain' or auto_retrain:
                                            updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                            
                                        else:                       
                                            updateModelStatus(correlationId,UniId,"","Warning","There is no text columns for your data,please upload data with text columns")
                                            
                                        updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                        raise Exception("There is no text columns for your data,please upload data with text columns")
                                else:
                                    columns = CheckTextCols(data_frame)
                                    unique_columns = list(data_frame.columns)
                    
                                if auto_retrain:
                                    _,sel_cols,_ = getRequestParams(correlationId,pageInfo,UniId)
                                    if len(set(sel_cols).intersection(columns)) != len(sel_cols):
                                        if pageInfo == 'Retrain' or auto_retrain:
                                            updateModelStatus(correlationId,UniId,"","Completed","Retrain Failed")
                                        else:                       

                                            updateModelStatus(correlationId,UniId,"","Warning","uploaded File missing trained columns, please check the data and Upload again")
                                        updQdb(correlationId, 'E', "ERROR", pageInfo, userId,UniId)
                                        raise("Unmatched Columns")
                                chunks, size = file_split(data_frame)
                                df_shape = list(data_frame.shape)
                                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                                 "Source": type_key}
                                if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':#need to give that
                                    save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                        sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #new method added
                                elif auto_retrain:
                                    size = append_data_chunks(data_frame, "AIServiceIngestData", correlationId, pageInfo, userId,
                                                 UniId,columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                                else:
                                    #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                                    #             sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                                    UniqueValuesCol,typeDict=getUniqueValues(data_frame) #newly added

                                    #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                                    #sourceDetails, colunivals=None, lastDateDict=lastDateDict)# 
                                    UpdateDataRecords(correlationId,data_frame)
                                    save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                                        sourceDetails, colunivals=UniqueValuesCol, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) #newlyadded
            return "single_file",data_frame
        counter = len(cols)
        flag_1 = True
        for i in range(0, counter):
            if i == counter - 1:
                break
            else:
                # print(i)
                if cols[i] == cols[i + 1]:
                    # print("True")
                    continue
                else:
                    flag_1 = False
                    break
        print("FLAG_1:: ", flag_1)
        if not flag_1:
            parent_cols = cols[parent_file_index]
            set_p = set(parent_cols)
            set_overlap = set_p
            for i in range(0, counter):

                if i != parent_file_index:
                    set_i = set(cols[i])
                    set_overlap = set_overlap & set_i
                    # print("Overlap::",set_overlap)
            if len(set_overlap) > 1:
                flag_2 = True
            else:
                flag_2 = False

            print("FLAG_2:: ", flag_2)

        if not flag_1 and not flag_2:
            flag_4 = True
            parent_cols = cols[parent_file_index]
            set_p = set(parent_cols)
            for i in range(0, counter):

                if i != parent_file_index:

                    set_i = set(cols[i])
                    overlap = set_p & set_i
                    # universe = set_p | set_i
                    # print("overlap::", overlap)
                    result = float(len(overlap)) / len(set_p) * 100
                    if result == 0:
                        continue
                    else:
                        flag_4 = False
                        break
            print("FLAG_4:: ", flag_4)

        if not flag_1 and not flag_2 and not flag_4:
            flag_3 = True
            print("FLAG_3:: ", flag_3)
        else:
            flag_3 = False
            print("FLAG_3:: ", flag_3)

        final_df = None
        flag_final = True
        if flag_1 or flag_2:
            for key in multi_source_dfs.keys():
                if multi_source_dfs[key] != {}:
                    if key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                if flag_final:
                                    final_df = data_frame.copy()
                                    flag_final = False
                                else:
                                    final_df = pd.concat([final_df, data_frame], axis=0, ignore_index=True)
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                if flag_final:
                                    final_df = data_frame.copy()
                                    flag_final = False
                            else:
                                final_df = pd.concat([final_df, data_frame], axis=0, ignore_index=True)

            # data_t = final_df[parent_cols]
            columns = list(final_df.columns)
            unique_columns = list(final_df.columns)
            # chunks,filesize= file_split(final_df)
            sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}

            chunks, filesize = file_split(final_df)
            if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':
                save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, 
                userId,UniId, columns,unique_columns,sourceDetails, colunivals=None, lastDateDict=lastDateDict) #new method added
            elif auto_retrain:
                size = append_data_chunks(data_frame, "AIServiceIngestData", correlationId, pageInfo, userId,
                                                 UniId,columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)                            
            else:
                UniqueValuesCol,typeDict=getUniqueValues(data_frame) #newly added
                #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,
                #sourceDetails, colunivals=None, lastDateDict=lastDateDict)
#                UpdateDataRecords(correlationId,final_df)
                save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId, columns,unique_columns,
                    sourceDetails, colunivals=UniqueValuesCol, lastDateDict=lastDateDict) #newlyadded
                #save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId, UniId,columns, sourceDetails,
                #         colunivals=None, lastDateDict=lastDateDict)

            return "single_file",None
        # Insert Into DB- Mapping Related Details
        if flag_1:

            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            # print("Part4")
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag1",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            else:
                dbcollection.remove({"CorrelationId": correlationId})
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag1",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            # print("Part6")
            dbconn.close()
            # print("Part7")
            return "single_file",None
        elif flag_2:
            # print("Part8")
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            # print("Part4")
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag2",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            else:
                dbcollection.remove({"CorrelationId": correlationId})
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag2",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            dbconn.close()
            # print("Part9")
            return "single_file",None
        elif flag_3:
            # print("Part10")
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            # print("Part4")
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag3",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            else:
                dbcollection.remove({"CorrelationId": correlationId})
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag3",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            dbconn.close()
            # print("Part11")
            return "multi_file",None
        elif flag_4:
            # print("Part12")
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            # print("Part4")
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag4",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            else:
                dbcollection.remove({"CorrelationId": correlationId})
                Id = str(uuid.uuid4())
                # print("Part5")
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag4",

                    # "FilePath": filepath,
                    # "ParentFile": parentfile,
                    "pageInfo": pageInfo,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            dbconn.close()
            return "multi_file",None
        # return "multi_file"
    else:
        '''mapping={
        "mapping0": {
            "source_file": "clients",
            "source_column": "client_id",
            "mapping_file": "loans",
            "mapping_column": "client_id"
        },
        "mapping1": {
            "source_file": "loans",
            "source_column": "loan_id",
            "mapping_file": 'payments',
            "mapping_column": "loan_id"
            }
       }'''

        mapping = eval(mapping)
        orig_mappings = mapping.copy()
        first_file = True
        flag_exit = False
        i = 0
        list_mapped_files = []
        final_df = {}
        while (len(orig_mappings) != 0 and not flag_exit):
            if first_file:
                # print("mapping",mapping["mapping%s" %i]["file1"],mapping["mapping%s" %i]["mapping_file"])
                df1 = None
                df2 = None
                if mapping["mapping%s" % i]["source_file"] == mapping["mapping%s" % i]["mapping_file"]:
                    raise Exception("Mapping should not be done on the same file")
                for key in multi_source_dfs.keys():
                    if multi_source_dfs[key] != {}:
                        if key == 'Entity':
                            for key1 in multi_source_dfs[key].keys():
                                if key1 == mapping["mapping%s" % i]["source_file"]:
                                    data_frame = multi_source_dfs[key][key1]
                                    df1 = data_frame.copy()
                        if key == 'File':
                            for key1 in multi_source_dfs[key].keys():
                                file_name = os.path.basename(key1).split('.')[0]
                                val = file_name.replace(correlationId + '_', '')
                                if val == mapping["mapping%s" % i]["source_file"]:
                                    data_frame = multi_source_dfs[key][key1]
                                    df1 = data_frame.copy()
                for key in multi_source_dfs.keys():
                    if multi_source_dfs[key] != {}:
                        if key == 'Entity':
                            for key1 in multi_source_dfs[key].keys():
                                if key1 == mapping["mapping%s" % i]["mapping_file"]:
                                    data_frame = multi_source_dfs[key][key1]
                                    df2 = data_frame.copy()
                        if key == 'File':
                            for key1 in multi_source_dfs[key].keys():
                                file_name = os.path.basename(key1).split('.')[0]
                                val = file_name.replace(correlationId + '_', '')
                                if val == mapping["mapping%s" % i]["mapping_file"]:
                                    data_frame = multi_source_dfs[key][key1]
                                    df2 = data_frame.copy()
                try:
                    final_df = pd.merge(df1, df2, left_on=mapping["mapping%s" % i]["source_column"],
                                        right_on=mapping["mapping%s" % i]["mapping_column"], how='left')
                except Exception as e:
                    raise Exception("Mapping should be done on same type of columns")
                list_mapped_files.append(mapping["mapping%s" % i]["source_file"])
                list_mapped_files.append(mapping["mapping%s" % i]["mapping_file"])
                first_file = False
                del orig_mappings["mapping%s" % i]

            else:
                for m_id, m_data in mapping.items():
                    # print(m_id, m_data)
                    if (mapping[m_id]["source_file"] in set(list_mapped_files) or mapping[m_id]["mapping_file"] in set(
                            list_mapped_files)) and m_id in orig_mappings.keys():
                        # print("mapping",mapping[m_id]["file1"],mapping[m_id]["mapping_file"])
                        if mapping[m_id]["source_file"] == mapping[m_id]["mapping_file"]:
                            raise Exception("Mapping should not be done on the same file")
                        df1 = None
                        df2 = None
                        if mapping[m_id]["source_file"] in set(list_mapped_files):
                            df1 = final_df
                            num_rows = final_df.shape[0]
                            join = 'left'
                        else:
                            for key in multi_source_dfs.keys():
                                if multi_source_dfs[key] != {}:
                                    if key == 'Entity':
                                        for key1 in multi_source_dfs[key].keys():
                                            if key1 == mapping[m_id]["source_file"]:
                                                data_frame = multi_source_dfs[key][key1]
                                                df1 = data_frame.copy()
                                    if key == 'File':
                                        for key1 in multi_source_dfs[key].keys():
                                            file_name = os.path.basename(key1).split('.')[0]
                                            val = file_name.replace(correlationId + '_', '')
                                            if val == mapping[m_id]["source_file"]:
                                                data_frame = multi_source_dfs[key][key1]
                                                df1 = data_frame.copy()
                        if mapping[m_id]["mapping_file"] in set(list_mapped_files):
                            df2 = final_df
                            join = 'right'
                        else:
                            for key in multi_source_dfs.keys():
                                if multi_source_dfs[key] != {}:
                                    if key == 'Entity':
                                        for key1 in multi_source_dfs[key].keys():
                                            if key1 == mapping[m_id]["mapping_file"]:
                                                data_frame = multi_source_dfs[key][key1]
                                                df2 = data_frame.copy()
                                    if key == 'File':
                                        for key1 in multi_source_dfs[key].keys():
                                            file_name = os.path.basename(key1).split('.')[0]
                                            val = file_name.replace(correlationId + '_', '')
                                            if val == mapping[m_id]["mapping_file"]:
                                                data_frame = multi_source_dfs[key][key1]
                                                df2 = data_frame.copy()
                        try:
                            if join == 'left':
                                final_df = pd.merge(df1, df2, left_on=mapping[m_id]["source_column"],
                                                    right_on=mapping[m_id]["mapping_column"], how='left')
                            elif join == 'right':
                                final_df = pd.merge(df2, df1, left_on=mapping[m_id]["mapping_column"],
                                                    right_on=mapping[m_id]["source_column"], how='left')
                        except Exception as e:
                            raise Exception("Mapping should be done on same type of columns")
                        # if final_df.shape[0] > num_rows:
                        # raise Exception("The parent-child relationship is not correct. Please validate.")
                        list_mapped_files.append(mapping[m_id]["source_file"])
                        list_mapped_files.append(mapping[m_id]["mapping_file"])
                        del orig_mappings[m_id]
                flag_exit = True
        if final_df.shape[0] == 0:
            raise Exception("Mapping should be done on columns having common datapoints")
        list_drop_cols = [col for col in final_df.columns if final_df[col].nunique() == 0]
        if len(list_drop_cols) == len(final_df.columns):
            raise Exception("Mapping should be done on columns having common datapoints")
        else:
            final_df.drop(list_drop_cols, axis=1, inplace=True)
        columns = final_df.columns
        data_t = final_df[columns]
        columns = list(data_t.columns)
        chunks, filesize = file_split(data_t)
        sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}

        columns = list(data_t.columns)
        unique_columns = list(data_t.columns)
        chunks, filesize = file_split(data_t)
        if pageInfo=='PredictData' or pageInfo=='TrainAndPredict':
            save_data_chunks(chunks, "AIServicesPrediction", correlationId, pageInfo, 
            userId,UniId, columns,unique_columns,sourceDetails, colunivals=None, lastDateDict=lastDateDict) #new method added
        elif auto_retrain:
            size = append_data_chunks(data_frame, "AIServiceIngestData", correlationId, pageInfo, userId,
                                                 UniId,columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)                        
        else:
#            UpdateDataRecords(correlationId,data_t)
            save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId, UniId,columns, unique_columns,sourceDetails,
                         colunivals=None, datapre=datapre, lastDateDict=lastDateDict)
        return "multi_file",data_t

def append_data_chunks(InstaData, collection, correlationId, pageInfo, userId,UniId,columns, sourceDetails, colunivals=None,
                       lastDateDict=None):
    df = data_from_chunks(corid=correlationId, collection="AIServiceIngestData")
    df = df.append(InstaData, ignore_index=True)
    unique_columns = columns.copy()
    
    df.sort_index(inplace=True)
    df.drop_duplicates(keep="last", inplace=True)
    # print (freq)
    df_shape = list(df.shape)
    ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
    chunks, size = file_split(df)
    dbconn, dbcollection = open_dbconn("AIServiceIngestData")
    dbcollection.remove({"CorrelationId": correlationId})
    save_data_chunks(chunks, "AIServiceIngestData", correlationId, pageInfo, userId,UniId,columns,unique_columns,sourceDetails=sourceDetails,
                     colunivals=colunivals,lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
    return size                                                                                                                                                                  


                
def insQdb(correlationId, status, progress, pageInfo, userId,uniid):
    dbconn, dbcollection = open_dbconn("AIServiceRequestStatus")
    if not progress.isdigit():
        message = progress
        rmessage = "Task Complete"
        progress = '0'
    elif status == "C":
        message = "Task Complete"
        rmessage = "Task Complete"
    else:
        message = "In - Progress"
        rmessage = "In - Progress"
    try:
        dbcollection.insert({"CorrelationId": correlationId,
               "Status": status,
               "Progress": progress,
               "RequestStatus": rmessage,
               "Message": message,
               "pageInfo": pageInfo,
               "CreatedByUser": userId,
               "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
               "ModifiedByUser": userId})
      
    except ServerSelectionTimeoutError:
        error_encounterd = str(e.args[0])
    return    
             
def data_from_chunks(corid, collection, lime=None, recent=None):
    dbconn, dbcollection = open_dbconn(collection)
    if not lime:
        data_json = dbcollection.find({"CorrelationId": corid})
    else:
        if lime == True:
            data_json = dbcollection.find({"CorrelationId": corid}).limit(1)
        else:
            data_json = dbcollection.find({"CorrelationId": corid}).limit(1)

    if recent != None:
        count = data_json.collection.count_documents({"CorrelationId": corid})
        temp = {}
        for countj in range(count):
            temp.update({datetime.strptime(data_json[countj].get('CreatedOn'), '%Y-%m-%d %H:%M:%S'): data_json[
                countj].get('UploadId')})
            recentdoc = temp.get(max(temp.keys()))
        data_json = dbcollection.find({"CorrelationId": corid, "UploadId": recentdoc})
        count = data_json.collection.count_documents({"CorrelationId": corid, "UploadId": recentdoc})
    else:
        print('new to utils*******',corid)
        count = data_json.collection.count_documents({"CorrelationId": corid})
        print('count',count)
    data_t1 = pd.DataFrame()
    if not lime:
        for counti in range(count):
            t = data_json[counti].get('InputData')
            try:
                data_t = pd.DataFrame(json.loads(t))
            except:
                t = EncryptData.DescryptIt(base64.b64decode(t)) 
                data_t = pd.DataFrame(json.loads(t))
            data_t1 = data_t1.append(data_t, ignore_index=True)
    else:
        count = 1
        for counti in range(count):
            data_t = pd.DataFrame(json.loads(data_json[counti].get('InputData')))
            data_t1 = data_t1.append(data_t, ignore_index=True)

    dbconn.close()
    return data_t1
def check_encrypt_for_offlineutility(DataSetUId):
    dbconn_datasetinfo,dbcollection_datasetinfo = open_dbconn('DataSetInfo')
    data_json_datasetinfo = list(dbcollection_datasetinfo.find({"DataSetUId": DataSetUId}))
    EnDeRequired = data_json_datasetinfo[0].get('DBEncryptionRequired')
    print(EnDeRequired)
    dbconn_datasetinfo.close()
    return (EnDeRequired)

def checkofflineutility(correlationId):
    dbconn, dbcollection = open_dbconn("AIServiceIngestData")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    if 'DataSetUId' in data_json[0]:
        if data_json[0].get('DataSetUId') != 'null':
            return data_json[0].get('DataSetUId')
        else:
            return False
    else:
        return False

def data_from_chunks_offline_utility(corid, collection, lime=None, recent=None,DataSetUId=None):
    EnDeRequired = check_encrypt_for_offlineutility(DataSetUId)
    dbconn, dbcollection = open_dbconn(collection)
    if not lime:
        data_json = dbcollection.find({"DataSetUId": DataSetUId})
    else:
        if lime == True:
            data_json = dbcollection.find({"DataSetUId": DataSetUId}).limit(1)
        else:
            data_json = dbcollection.find({"DataSetUId": DataSetUId}).limit(1)
    if recent != None:
        count = data_json.collection.count_documents({"DataSetUId": DataSetUId})
        temp = {}
        for countj in range(count):
            temp.update({datetime.strptime(data_json[countj].get('CreatedOn'), '%Y-%m-%d %H:%M:%S'): data_json[
                countj].get('UploadId')})
            recentdoc = temp.get(max(temp.keys()))
        data_json = dbcollection.find({"DataSetUId": DataSetUId, "UploadId": recentdoc})
        count = data_json.collection.count_documents({"DataSetUId": DataSetUId, "UploadId": recentdoc})
    else:
        print('new to utils*******',corid)
        count = data_json.collection.count_documents({"DataSetUId": DataSetUId})
        print('count',count)
    data_t1 = pd.DataFrame()
    if not lime:
        for counti in range(count):
            t = data_json[counti].get('InputData')
            try:
                data_t = pd.DataFrame(json.loads(t))
            except:
                t = EncryptData.DescryptIt(base64.b64decode(t)) 
        
                data_t = pd.DataFrame(json.loads(t))
            data_t1 = data_t1.append(data_t, ignore_index=True)
    else:
        count = 1
        for counti in range(count):
            t = data_json[counti].get('InputData')
            if EnDeRequired :
                t = EncryptData.DescryptIt(base64.b64decode(t))
                #print(t)               #En.....................
            data_t = pd.DataFrame(json.loads(t))
            data_t1 = data_t1.append(data_t, ignore_index=True)
    dbconn.close()
    return data_t1


#below function will return actual data for prediction
# data points change       
def GetModelName(correlationid,UniId):
    dbconn, dbcollection = open_dbconn('AICoreModels')
    model_json = list(dbcollection.find({"CorrelationId": correlationid}))
    return model_json[0]["ModelName"]

#below function will return actual data for prediction
# data points change
# db_count=0   
def GetModelSimilarityName(correlationid):
    dbconn, dbcollection = open_dbconn('AICoreModels')
    model_json = list(dbcollection.find({"CorrelationId": correlationid}))
    dbconn.close()
    return model_json[0]["ModelName"], model_json[0]["ModelPath"]


#need to add language collection in ai core models
def GetLanguageForModel(correlationid):
    dbconn, dbcollection = open_dbconn('AIServiceRequestStatus')
    model_json = list(dbcollection.find({"CorrelationId": correlationid}))
    dbconn.close()
    try:
        if model_json[0]["Language"] == None:
            return 'english'
        else:
            return model_json[0]["Language"]
    except:
        return 'english'
    #return 'english'
    """
    dbconn, dbcollection = open_dbconn('AICoreModels')
    model_json = list(dbcollection.find({"CorrelationId": correlationid}))
    dbconn.close()
    return model_json[0]["Language"]
    """
def GetNoOfResults():
    result=config['SIMILARITY']['no_of_results']
    return result   
#get model data point count()
def GetModelDataPointCount(correlationid):
    dbconn, dbcollection = open_dbconn('AIServiceIngestData')
    model_json = dbcollection.find_one({"CorrelationId": correlationid})
    dbconn.close()
    if 'datacount' in model_json:
        return model_json['datacount']
    else: # old data doesnt have data count column
        return 0 #old data has no data count, whether recalculate? or not?
    #return model_json[0]["ModelName"], model_json[0]["ModelPath"]

#below function will return actual data for prediction
# data points change      
def GetModelActualData(correlationid,UniId):
    dbconn, dbcollection = open_dbconn('AIServicesPrediction') #need to add collection name and column name
    model_json = list(dbcollection.find({"CorrelationId": correlationid,"UniId":UniId}))
    print('from getmodelactualdatafunction',model_json)
    input_data = {}
    for i in range(len(model_json)):
        print(model_json,i,'from model json for loop get modelactualdata')
        data = EncryptData.DescryptIt(base64.b64decode(model_json[i].get("ActualData")))
        k = model_json[i]["Chunk_number"]
        input_data[k] = str(data)
    return input_data    

def GetLanguageForActualData(correlationid,UniId):
    #AI services prediction
    return 'english'
    
def UpdateAIserviceCollection(correlationid,uniqueId,chunk_number,predictions,Error_val):
    dbconn, dbcollection = open_dbconn('AIServicesPrediction')
    predictions=json.dumps(predictions)
    predictions = EncryptData.EncryptIt(predictions)
    if Error_val=='':
        dbcollection.update_one(
        {"CorrelationId":correlationid,
        "UniId":uniqueId,"Chunk_number":chunk_number},
        {'$set':{'PredictedData' : predictions,
        'Status' : "C",
        'Progress':"100",
            "ErrorMessage":"TaskCompleted!"
                }
        }
        )
    else:
        dbcollection.update_one(
        {"CorrelationId":correlationid,
        "UniId":uniqueId,"Chunk_number":chunk_number},
        {'$set':{'PredictedData' : "",
        'Status' : "E",'Progress':"", "ErrorMessage":Error_val
    }})
        print('Error block',Error_val)
    return 

def save_model(correlationId, pickle_list,UniId,models):
    file_paths = []
    for index in range(0,len(pickle_list)):
        file_paths.append(saveModelPath + '_'+ str(correlationId) + '_' + str(UniId) + '_' + str(pickle_list[index]) + '.pkl')
    
    file_paths = str(file_paths)
    dbconn, dbcollection = open_dbconn('AICoreModels')
    dbcollection.update_many({"CorrelationId": correlationId},{'$set':{"ModelPath":file_paths}})
    encryptPickle(models,file_paths)
#    pickle.dump(model, open(file_path, 'wb'))

def save_model_for_BestDeveloper(correlationId, name,UniId,model):
    file_path = saveModelPath + '_'+ str(correlationId) + '_' + str(UniId) + '_' + str(name) + '.pkl'
    dbconn, dbcollection = open_dbconn('AICoreModels')
    dbcollection.update_many({"CorrelationId": correlationId},{'$set':{"ModelPath":file_path}})
    encryptPickle_for_BestDeveloper(model,file_path)

def getModelPaths(correlationId,UniId):
    
    dbconn, dbcollection = open_dbconn('AICoreModels')
    model_paths = list(dbcollection.find({"CorrelationId": correlationId}))[0]["ModelPath"]
    return model_paths         



def load_model(correlationId, name,UniId):
    file_path =  saveModelPath + '_'+ str(correlationId) + '_' + str(UniId) + '_' + str(name) + '.pkl'
    try:
        vectorizer = decryptPickle(file_path)
    except:      
        vectorizer = pickle.load(open(file_path, "rb"))
    return vectorizer

def load_model_from_Path(file_path):
    #file_path =  saveModelPath + '_'+ correlationId + '_' + UniId + '_' + name + '.pkl'
    try:
        vectorizer = decryptPickle(file_path)
    except:      
        vectorizer = pickle.load(open(file_path, "rb"))
    return vectorizer

#Decrypting

def DescryptIt(message):
   # from Crypto.Cipher import AES
  #  key = base64.b64decode("lrnNHLUV5AQRYR/eRvCfJwfwgztwHBcsi7+wwgmZRRE=")
   # iv = base64.b64decode("xfrF9vus1x9HBMgWHpsIGQ==")
  #  mode = AES.MODE_CBC
    obj2 = AES.new(key,mode,iv)
    dtext = obj2.decrypt(message)
    dtext = dtext[:-dtext[-1]].decode('latin-1')
    return dtext

def getMetricAzureToken():

    TokenURL = config['METRIC']['AdTokenUrl']

    headers = {"Content-Type": "application/x-www-form-urlencoded"}

    payload = {
        "grant_type": config['METRIC']['grant_type'],
        "resource": config['METRIC']['resource'],
        "client_id": config['METRIC']['client_id'],
        "client_secret": config['METRIC']['client_secret']
    }
    try:
        r = requests.post(TokenURL, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None

    return token, r.status_code


def CustomAuth(AppId):
    EnDeRequired = True
    
    if (AppId=='36b5d37e-f4b7-4395-beb7-85c043202091'):
        token, status_code = getMetricAzureToken()
        return token, status_code                                 
        
    dbconn, dbcollection = open_dbconn('AppIntegration')
    data = dbcollection.find_one({"ApplicationID": AppId})
    # key=['username','Scope']
    creds = data.get('Credentials')
    # for x in key:
    #    del creds[x]
    v=data.get('TokenGenerationURL')
    if EnDeRequired:
        x = base64.b64decode(creds) #En.............................
        creds = eval(DescryptIt(x))

        x = base64.b64decode(v) #En.............................
        v = DescryptIt(x)
    #print (data)
    if data.get('Authentication') == 'AzureAD' or data.get('Authentication') == 'Azure':

        r = requests.post(v, headers={'Content-Type': "application/x-www-form-urlencoded"},
                          data=creds)
        if r.status_code == 200:
            token = r.json()['access_token']
            return token, r.status_code
        else:
            return False, 401
    elif data.get('Authentication') == 'WindowsAuthProvider':
        r = requests.post(v, headers={'Content-Type': "application/x-www-form-urlencoded"},auth=HttpNegotiateAuth())
        if r.status_code == 200:
            token = ""
            return token, r.status_code
        else:
            return False, 401
    elif data.get('Authentication') == 'Form' or data.get('Authentication') == 'Forms':
        url =  config['PAD']['tokenAPIUrl']
        username = config['PAD']['username']
        password = config['PAD']['password']
        data_body = {"username":username,
		"password":password
		}
        print ("hererere",data_body,url)
        r = requests.post(url, data = json.dumps(data_body),headers={'Content-Type': "application/json"})
        print ("========================toekn status",r.status_code)
        if r.status_code == 200:
            token = r.json()['token']
            return token, r.status_code
        else:
            return False, r.status_code
    

def GetTestData(clientuid,dcid,Entity,WorkItemExternalId):
    k ={
        "clientUId": clientuid,
        "includeCompleteHierarchy": "true",
        "deliveryConstructUId": dcid,
        }
    PhoenixApi=config['Entity']['PhoenixApi']
    PhoenixApi=PhoenixApi.format(urlencode(k))
    #PhoenixApi="https://mywizardapi-devtest-lx.aiam-dh.com/core/v1/workItems/Query?clientUId="+clientuid+"&includeCompleteHierarchy=true&deliveryConstructUId="+dcid
    workitemType = eval(config['Entity']['AgileWorkItemTypes'])
    g=([key for key,v in workitemType.items() if v == Entity])
 
    WorkItemTypeUId=(', '.join(g))
    args={
        "ClientUId": clientuid,
        "WorkItemTypeUId":WorkItemTypeUId,
        "DeliveryConstructUId" : dcid,
        "WorkItemExternalId" : WorkItemExternalId
        }
    
    tokenArgs ={ 
                  "grant_type": config['Entity']['grant_type'],
                 "client_id": config['Entity']['client_id'],
                  "resource": config['Entity']['resource'],
                  "client_secret":config['Entity']['client_secret']
                }
        
    if auth_type == 'AzureAD':
        AdTokenUrl = config['Entity']['AdTokenUrl']
        EntityRequest = requests.post(AdTokenUrl,data=tokenArgs,headers = {'Content-Type':'application/x-www-form-urlencoded'})
                   
        EntityAccessToken = EntityRequest.json()['access_token']
    
        EntityResults = requests.post(PhoenixApi,data=json.dumps(args),
                                                   headers={'Content-Type':'application/json',
                                                        'Authorization': 'bearer {}'.format(EntityAccessToken),
                                                        'AppServiceUId':'00040560-0000-0000-0000-000000000000'},
                                                  params={"clientUId": clientuid, 
                                                           "deliveryConstructUId":dcid,
                                                           "includeCompleteHierarchy": "true"}
                                                       )
    elif auth_type == 'WindowsAuthProvider':
            EntityResults = requests.post(PhoenixApi,data=json.dumps(args),
                                                   headers={'Content-Type':'application/json',
                                                        'AppServiceUId':'00040560-0000-0000-0000-000000000000'},
                                                  params={"clientUId": clientuid, 
                                                           "deliveryConstructUId":dcid,
                                                           "includeCompleteHierarchy": "true"},
                                                     auth=HttpNegotiateAuth()
                                                       )
    
    
    z2=EntityResults.json()["WorkItems"][0]
    z3 = EntityResults.json()["WorkItems"][0]["WorkItemAttributes"]
    x = {}
    dfs=pd.DataFrame()
    for item in z3:
        #print(item)
        x[item['DisplayName']] = item['Value']
        #print(x[item['DisplayName']])
        cols = ['CreatedOn','ModifiedOn','StackRank','WorkItemTypeUId','WorkItemExternalId','CreatedAtSourceByUser']  
        for i in cols:
            if i in ['CreatedOn','ModifiedOn']:
                x.update({i.lower():z2[i]})
            else:
                x.update({i.lower():z2[i]})
                
        df = pd.DataFrame([x])
        dfs=dfs.append(df)
        dfs.reset_index(drop = True,inplace = True)
    return dfs

def get_chunks_dataframe(chunks):    
    
    for chi in range(len(chunks)):
        print(chi)
       
        if not timeseries:
            # data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            try:
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            except Exception as e:
                list_indexes = []
                for i, row in chunks[chi].iterrows():
                    try:
                        pd.DataFrame(row).to_json(orient='records', date_format='iso', date_unit='s')
                    except Exception as e:
                        list_indexes.append(i)
                chunks[chi].drop(index=list_indexes, inplace=True)
                chunks[chi].reset_index(drop=True, inplace=True)
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
        
    return data_json

def identifyDtype(data_copy):
    empty_cols = [col for col in data_copy.columns if data_copy[col].dropna().empty]
    data_copy.drop(columns = empty_cols ,inplace = True)  
    date_regex = "[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}[\t\s]\d{2}:\d{2}:\d{2}|[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}"
    date_cols = []
    incorrect_date_format = []
    names=list(data_copy.columns)
    for col in names:
    
        data_copy[col].dropna(inplace=True)
        arr = list(range(len(data_copy[col])))  # resetting column index
        data_copy[col].index = arr
        if data_copy[col].dtype == 'datetime64[ns]':
            date_cols.append(col)
    
        if data_copy[col].dtype.name == 'datetime64[ns, UTC]':
            date_cols.append(col)
            
            data_copy[col] = data_copy[col].astype('datetime64[ns]')
    
        elif data_copy[col].dtype.name == 'object':
            date = list(data_copy[col].unique())
            if bool(re.match(date_regex, str(date[0]))):
                try:
                    
                    data_copy[col] = pd.to_datetime(data_copy[col])
                    date_cols.append(col)
                    incorrect_date_format.append(col)
                except Exception as e:
                    error_encounterd = str(e.args[0])
        else:
            pass

    # dates type columns are removed
    names = list(set(names).difference(set(date_cols)))
    id_cols = []
    names=list(data_copy.columns)
    text_cols=[]
    cat_cols=[]
    maxCat=1000
    UnknownTypes = []
    for col in names:
        if data_copy[col].dtype.name == 'bool':
            data_copy[col] = data_copy[col].astype('category')
            cat_cols.append(col)
        data_copy[col].dropna(inplace=True)
        # calculating unique percenatge for values in column
        uniquePercent = len(data_copy[col].unique()) * 1.0 / len(data_copy[col]) * 100
        # classifying categorical and text column based on unique percentage of values in column 2
        if data_copy[col].dtype == 'object':
            word_list = []
            for word in data_copy[col].values:
                word = str(word)
                word_list.append(len(word.split()))
            Avg_word_count = round(sum(word_list) * 1.0 / len(data_copy[col]))

            if uniquePercent <= 50 and Avg_word_count <= 3 and len(data_copy[col].unique()) < maxCat:
                #data_copy[col] = data_copy[col].astype('category')
                cat_cols.append(col)
            elif uniquePercent > 50 and Avg_word_count > 3:
                data_copy[col] = data_copy[col].astype(str)
                text_cols.append(col)
            elif col.lower() in text_cols:
                data_copy[col] = data_copy[col].astype('object')
                text_cols.append(col)
            else:
                UnknownTypes.append(col)
            
    return cat_cols

def save_filters_data(correlation_id,pageInfo,userId,filters):
    dbconn, dbcollection=open_dbconn("AICore_Preprocessing")
    #data_json = dbcollection.find({"CorrelationId": correlation_id})
    filters = EncryptData.EncryptIt(json.dumps(filters))
    dbcollection.insert_one({"CorrelationId": correlation_id,
                                           "PageInfo": pageInfo,
                                           "CreatedByUser": userId,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "Filters": filters,
                                           "ModifiedByUser": userId})
def IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken):
    final_df = pd.DataFrame()
    api_error_flag = True
    IterationTypes = eval(EntityConfig['CDMConfig']['IterationTypes'])
    auth_type = get_auth_type()
    for j in range(len(IterationTypes)):
        entityArgs.update({"IterationTypeUId": IterationTypes[j]})
        entityArgs.update({"PageNumber": 1})
        entityArgs.update({"TotalRecordCount": 0})
        if auth_type == 'AzureAD':
            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
        elif auth_type == 'WindowsAuthProvider':
            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                 auth=HttpNegotiateAuth())
    #print("sdsdgsgsg",auto_retrain)
        print(resp.status_code)
        if resp.status_code == 200:
            api_error_flag = False
            if resp.text != "No data available":
                if resp.json()['TotalRecordCount'] != 0:
                    workItem = resp.json()['Entity']
                    entityDataframe = pd.DataFrame(workItem)
                    numberOfPages = resp.json()['TotalPageCount']
                    TotalRecordCount = resp.json()['TotalRecordCount']
                    i = 1    
                    while i < numberOfPages:
                        entityArgs.update({"PageNumber": i + 1})
                        entityArgs.update({"TotalRecordCount": TotalRecordCount})
                        if auth_type == 'AzureAD':
                            entityresults = requests.post(agileAPI, data=json.dumps(entityArgs), headers={'Content-Type'  : 'application/json','authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                         
                        elif auth_type == 'WindowsAuthProvider':
                            entityresults = requests.post(agileAPI, data=json.dumps(entityArgs), headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                                         
                        if entityresults.status_code == 200:
                            entityData = entityresults.json()['Entity']
                            if entityData != []:
                                df = pd.DataFrame(entityData)
                            entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                        i = i + 1
                else:
                    entityDataframe  = pd.DataFrame()
            else:
                entityDataframe  = pd.DataFrame()
            final_df = pd.concat([final_df,entityDataframe]).reset_index(drop=True)           
    if final_df.empty:
        if not api_error_flag:
            if auto_retrain:
                entityDfs[entity] = "No incremental data available"
            else:
                entityDfs[entity] = "Data is not available for your selection"
        else:
           entityDfs[entity] =  "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
    elif final_df.shape[0] <= min_data() and not auto_retrain:                                  
        entityDfs[entity] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
    elif final_df.shape[0] > max_data():
        entityDfs[entity] = "Number of records greater than "+str(max_data())+". Kindly upload data through mydatasource"
    else:
        final_df["modifiedon"] = pd.to_datetime(final_df["modifiedon"], format="%Y-%m-%d %H:%M", exact=True)
        lastDateDict['Entity'][entity] = pd.to_datetime(final_df["modifiedon"].max()).strftime('%Y-%m-%d %H:%M:%S')
        final_df.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
        entityDfs[entity] = final_df
        assocColumn = entity.lower()+"associations"
        col = entityDfs[entity][assocColumn]
        entityDfs[entity]['itemexternalid'] = np.nan
        for i in range(len(entityDfs[entity][assocColumn])):
            if col[i]!='' and col[i] != None:
                val = []
                try:
                    for k in range(len(eval(col[i]))):
                        if 'ItemExternalId' in eval(eval(col[i])[k]).keys():
                            val.append(eval(eval(col[i])[k])['ItemExternalId']) 
                except:
                    for k in range(len((col[i]))):
                        if 'ItemExternalId' in ((col[i])[k]).keys():
                            val.append(((col[i])[k])['ItemExternalId'])                                 
                entityDfs[entity]['itemexternalid'][i] = val
        entityDfs[entity] = entityDfs[entity].explode('itemexternalid').fillna('')
        entityDfs[entity].drop(columns=assocColumn,inplace=True)
                                               
    return

def getEntityArgs(invokeIngestData,entity,start,end,method,delType):
    if method == 'AGILE' and delType == "Agile":
        #print(eval(EntityConfig['WorkItem'][entity]))
        entityArgs = {
                "ClientUID"            : invokeIngestData["ClientUID"],
                "DeliveryConstructUId" : invokeIngestData["DeliveryConstructUId"],
                "EntityUId"            : eval(EntityConfig['WorkItem']['EntityUID']),
                "ColumnList"           : eval(EntityConfig['WorkItem'][entity]),
                "WorkItemTypeUId"      : [val for key, val in eval(EntityConfig['CDMConfig']['AgileWorkItemTypes']).items() if entity == key][0],
                "RowStatusUId"         : eval(EntityConfig['CDMConfig']['rowstatusuid'])['Active'],
                #"Displayname"          : eval(EntityConfig['WorkItem'][entity]),
                "PageNumber"           : "1",
                "TotalRecordCount"     : "0",
                "BatchSize"            : "5000",
                "FromDate"             : start,
                "ToDate"               : end}
    else:
         entityArgs = {
               "ClientUID"            : invokeIngestData["ClientUID"],
               "DeliveryConstructUId" : invokeIngestData["DeliveryConstructUId"],
               "EntityUId"            : eval(EntityConfig[entity.casefold()]['EntityUID']),
               "ColumnList"           : eval(EntityConfig[entity.casefold()]['RequiredColumns']),
               "RowStatusUId"         : eval(EntityConfig['CDMConfig']['rowstatusuid'])['Active'],
               "FieldName"            : eval(EntityConfig[entity.casefold()]['fieldname']),
               "PageNumber"           : "1",
               "TotalRecordCount"     : "0",
               "BatchSize"            : "5000",
               "FromDate"             : start,
               "ToDate"               : end}
    return entityArgs   

def getEntityToken():
    TokenURL = config['Entity']['AdTokenUrl']
    headers = {
        "Content-Type": "application/x-www-form-urlencoded"
    }
    payload = {
        "grant_type": config['Entity']['grant_type'],
        "resource": config['Entity']['resource'],
        "client_id": config['Entity']['client_id'],
        "client_secret": config['Entity']['client_secret']
    }
    try:
        r = requests.post(TokenURL, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None
    return token, r.status_code

####new function addition
#AISavedUsecases
#savedUsecases for apps----->
#AIcoremodels---> correlationID
def maxdatapull(searchTerm,collectionName=None):
    if collectionName==None:
        collectionName='AICoreModels'
        searchKey='CorrelationId'
    elif collectionName!=None:
        collectionName='AISavedUsecases'
        searchKey='UsecaseId'
    dbconn, dbcollection = open_dbconn(collectionName)
    if searchTerm:
        args = dbcollection.find_one({searchKey: searchTerm})
        try:
            if "MaxDataPull" in args:
                data_points = args["MaxDataPull"]
                
                if int(data_points) < 4:
                    return int(config['MaxPull']['records'])           
            else:
                data_points = int(config['MaxPull']['records'])
        except:
            data_points = int(config['MaxPull']['records'])
    return int(data_points)



####new function addition complete

def TransformEntitiesapi(customdataDfs,EntityMappingColumns):                          
    min_df = min_data() 
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])
    uids = [p.replace("externalid","uid") for p in EntityMappingColumns ]
    #productinstance = set([p.replace("externalid","productinstances_productinstanceuid") for p in EntityMappingColumns ])
    MultiSourceDfs={}
    #for k,v in MultiSourceDfs['Entity'].items():
    k="ent"
    v=customdataDfs.copy()
    otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])             
    #if  type(v) != str:
    ids = list(set(uids).intersection(v.columns))
    if 'workitemuid' in ids:
         ids = 'workitemuid'
    else:
        if len(ids) > 1:
            ids = list(set([k.lower()+"uid"]).intersection(ids))[0]
        else:
            ids = ids[0]
    file_name = ids[:-3]
    b = list(EntityMappingColumns.intersection(v.columns))
    a = file_name + otherReqCols[0]
    otherReqCols.remove(otherReqCols[0])
    otherReqCols.insert(0,a)
    m = list(set(otherReqCols).intersection(b))
    prdctid = "productinstanceuid"                       
    if "workitemexternalid" in m:
        file_name = k.lower()
        if 'createdbyproductinstanceuid' in v.columns:
            v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
        if 'stateuid' in v.columns:
            v.rename(columns = {'stateuid':'State'}, inplace=True)
        
        for col in m:
            v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
        v["UniqueRowID"] = v.workitemexternalid + v.itemexternalid
        v = v.drop_duplicates(keep="last")
        MultiSourceDfs = v.copy()

    return MultiSourceDfs


def Customsourcedata(hadoopApi,TargetNode,entityArgs,entityAccessToken,customdataDfs,lastDateDict,Incremental=False):
    auth_type = get_auth_type()
    resp = requests.post(hadoopApi, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    if resp.status_code == 200:
        if resp.text == "No data available":
            customdataDfs['custom'] = "Data is not available for your selection"
        else:
            if resp.json()['TotalRecordCount'] != 0:
                entityDataframe = pd.DataFrame({})
                numberOfPages = resp.json()['TotalPageCount']
                TotalRecordCount = resp.json()['TotalRecordCount']
                BatchSize = resp.json()['BatchSize']
                i = 0
                while i < numberOfPages:
                    entityArgs.update({"PageNumber": i + 1})
                    entityArgs.update({"TotalRecordCount": TotalRecordCount})
                    if auth_type == 'AzureAD':
                        entityresults = requests.post(hadoopApi, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        entityresults = requests.post(hadoopApi, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                    if entityresults.status_code == 200:
                        entityData = entityresults.json()
                        for j in range(len(TargetNode)):
                            try:
                                entityData = entityData[TargetNode[j]]
                            except:
                                entityData = entityData[0][TargetNode[j]]                            
                        if entityData != []:
                            df = pd.DataFrame(entityData)
                        entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                    i = i + 1
                if entityDataframe.empty:
                    customdataDfs['custom'] = "Data is not available for your selection"
                elif entityDataframe.shape[0] <= min_data():
                    customdataDfs['custom'] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
                else:
                    for col in entityDataframe.columns:
                        try:
                            if entityDataframe[col].dtype == 'object' and 'DateTime' in entityDataframe[col].iloc[0].keys():
                                entityDataframe[col] = [pd.to_datetime(d['DateTime']) for d in entityDataframe[col]]
                        except Exception as e:
                            error_encounterd = str(e.args[0])
                        try:
                            queryDF[[dtext["DateColumn"]]] = queryDF[dtext["DateColumn"]].apply(lambda x: x['DateTime'])
                            lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                        except Exception as e:
                            error_encounterd = str(e.args[0])
                    customdataDfs = entityDataframe
                    try:
                        if 'workitemassociations' in customdataDfs.columns:
                            assocColumn = 'workitemassociations'
                        else:
                            if entityArgs['EntityUId'] != "00040020-0200-0000-0000-000000000000":
                                entityn = "entitynew"
                                assocColumn = entityn.lower()+"associations"
                        if entityArgs['EntityUId'] != "00040020-0200-0000-0000-000000000000":
                            col = customdataDfs[assocColumn]
                            customdataDfs['itemexternalid'] = pd.Series(data = "",index =  customdataDfs.index)
                        for i in range(len(customdataDfs[assocColumn])):
                            if col[i]!='' and col[i] != None:
                                val = []
                                try:
                                    for k in range(len(eval(col[i]))):
                                        if 'ItemExternalId' in eval(eval(col[i])[k]).keys():
                                            val.append(eval(eval(col[i])[k])['ItemExternalId'])
                                except:
                                    for k in range(len((col[i]))):
                                        if 'ItemExternalId' in ((col[i])[k]).keys():
                                            val.append(((col[i])[k])['ItemExternalId'])
                                customdataDfs['itemexternalid'][i] = val
                        customdataDfs = customdataDfs.explode('itemexternalid').fillna('')
                        customdataDfs.drop(columns=assocColumn,inplace=True)
                        if isinstance(customdataDfs, pd.DataFrame):
                            EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
                            customdataDfs = TransformEntitiesapi(customdataDfs,EntityMappingColumns)
                    except Exception as e:
                        error_encounterd = str(e.args[0])
            else:
                customdataDfs['custom'] = "Data is not available for your selection"
    else:
        e = "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
        customdataDfs['custom']=e
    #print("CallCdmAPI is done")
    return customdataDfs

def open_phoenixdbconn():
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection[config['DBconnection']['PhoenixDB']]
    return db

def CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationid,uniId):
    start_time=time.time()
    
    cpu,memory=Memorycpu()
    
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':
        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                  auth=HttpNegotiateAuth())
    elif auth_type == 'Forms':
        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                      auth=HttpNegotiateAuth())
    #print("sdsdgsgsg",auto_retrain)
    if resp.status_code == 200:
        if resp.text == "No data available":
            if auto_retrain:
                entityDfs[entity] = "No incremental data available"
            else:
                entityDfs[entity] = "Data is not available for your selection"
        else:
            if resp.json()['TotalRecordCount'] != 0:
                ######max_data_pull addition
                maxdata = maxdatapull(correlationid)
                nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']

                months_counter=1
                if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                    actualfromdate = entityArgs['FromDate']
                    if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                        entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                        if auth_type == 'AzureAD':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                    'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                        elif auth_type == 'WindowsAuthProvider':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth()) 
                        while maxdata > resp.json()['TotalRecordCount']:
                            months_counter+=1
                            if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                                entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                                if auth_type == 'AzureAD':
                                    resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                    'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                elif auth_type == 'WindowsAuthProvider':
                                    resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth()) 

                entityDataframe = pd.DataFrame({})
                numberOfPages = resp.json()['TotalPageCount']
                TotalRecordCount = resp.json()['TotalRecordCount']
                BatchSize = resp.json()['BatchSize']
                ######mac_data_pull addition_complete
                
                
                i = 0
                while i < numberOfPages:
                    entityArgs.update({"PageNumber": i + 1})
                    entityArgs.update({"TotalRecordCount": TotalRecordCount})
                    if auth_type == 'AzureAD':
                        entityresults = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        entityresults = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                      auth=HttpNegotiateAuth())
                    elif auth_type == 'Forms':
                        entityresults = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                      auth=HttpNegotiateAuth())
                            
                    if entityresults.status_code == 200:
                        entityData = entityresults.json()['Entity']
                        if entityData != []:
                            df = pd.DataFrame(entityData)
                        entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                    i+=1 
                if entityDataframe.empty:
                    if auto_retrain:
                        entityDfs[entity] = "No incremental data available"
                    else:
                        entityDfs[entity] = "Data is not available for your selection"
                elif entityDataframe.shape[0] <= min_data() and not auto_retrain:                                   
                    entityDfs[entity] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
                elif entityDataframe.shape[0] >max_data():
                    entityDfs[entity] = "Number of records greater than "+str(max_data())+". Kindly upload data through mydatasource"
                else:
                    entityDataframe["modifiedon"] = pd.to_datetime(entityDataframe["modifiedon"], format="%Y-%m-%d %H:%M", exact=True)
                    lastDateDict['Entity'][entity] = pd.to_datetime(entityDataframe["modifiedon"].max()).strftime('%Y-%m-%d %H:%M:%S')
                    entityDataframe.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
                    try:
                        for col in entityDataframe.columns:
                            entityDataframe[col].fillna(entityDataframe[col].mode()[0], inplace=True)
                    except Exception as e:
                        logger(correlationid, 'INFO',str(e.args[0]),str(uniId))
                    entityDfs[entity] = entityDataframe
                    #######itemexternalid change############
                    if 'workitemassociations' in entityDfs[entity].columns:
                        assocColumn = 'workitemassociations'
                    elif (entity != "CodeBranch" and entity != "Task"):
                        assocColumn = entity.lower()+"associations"
                    else:
                        if entity == "Task":
                            assocColumn = "deliverytaskassociations"
                    col = entityDfs[entity][assocColumn]
                    entityDfs[entity]['itemexternalid'] = np.nan
                    for i in range(len(entityDfs[entity][assocColumn])):
                        if col[i]!='' and col[i] != None:
                            val = []
                            try:
                                for k in range(len(eval(col[i]))):
                                    if 'ItemExternalId' in eval(eval(col[i])[k]).keys():
                                        val.append(eval(eval(col[i])[k])['ItemExternalId']) 
                            except:
                                for k in range(len((col[i]))):
                                    if 'ItemExternalId' in ((col[i])[k]).keys():
                                        val.append(((col[i])[k])['ItemExternalId'])                                 
                            entityDfs[entity]['itemexternalid'][i] = val
                    entityDfs[entity] = entityDfs[entity].explode('itemexternalid').fillna('')
                    entityDfs[entity].drop(columns=assocColumn,inplace=True)
                        
                    ##########itemexternalid change completed##########
            else:
                entityDfs[entity] = "Data is not available for your selection"
                
    else:
        e = "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
        

        entityDfs[entity] = e
    
    return
    
def DoAd_AgileTransfrom(df,EntityConfig,m,ids,RequiredColumns):
    
    if RequiredColumns == 'WorkItem':
        cols_Req = eval(EntityConfig[RequiredColumns]["RequiredColumns"]).split(',') + m
    elif RequiredColumns == 'deliverytask':
        cols_Req = eval(EntityConfig["task"]["RequiredColumns"]).split(',') + m
    else:
        cols_Req = eval(EntityConfig[RequiredColumns.casefold()]["RequiredColumns"]).split(',') + m
    if ids != 'codebranchuid' and ids != 'environmentuid' and ids != 'assignmentuid' and ids != 'workitemuid':
        itemexternalid =  m[1]
    dfs = []
    if RequiredColumns == 'WorkItem':
        print("code has to be written for agileusecase")
        '''
        val = "value"
        dispName = "displayname"
        glist = set(df[dispName].dropna().unique())
        #if flag == 'agileusecase':
        df4=pd.DataFrame()
        df5=pd.DataFrame()
        df7=pd.DataFrame()
        df2=df[['value','externalvalue','displayname']]
        df2=df2[df2['displayname'].isin(['Iteration'])]
        df2.reset_index(drop=True, inplace=True)
        df2=df2.drop(['displayname'], axis = 1)
        df2.rename(columns={'value':'Iteration','externalvalue':'Iterationname'}, inplace=True)
        df2=df2.replace('',np.nan)
        df2 = df2[df2['Iteration'].notna()]
        df4=pd.Series(df2.Iterationname.values,index=df2.Iteration).to_dict()
        df2.reset_index(drop=True, inplace=True)
        df3=df[['value','externalvalue','displayname']]
        df3=df3[df3['displayname'].isin(['Release'])]
        df3.rename(columns={'value':'Release','externalvalue':'Releasename'}, inplace=True)
        df3=df3.drop(['displayname'], axis = 1)
        df3=df3.replace('',np.nan)
        df3 = df3[df3['Release'].notna()]
        df3.reset_index(drop=True, inplace=True)
        df5=pd.Series(df3.Releasename.values,index=df3.Release).to_dict()
        df6=df[['value','idvalue','displayname']]
        df6=df6[df6['displayname'].isin(['State'])]
        df6.reset_index(drop=True, inplace=True)
        df6=df6.drop(['displayname'], axis = 1)
        df6.rename(columns={'value':'StateUID','idvalue':'State'}, inplace=True)
        df6=df6.replace('',np.nan)
        df7=pd.Series(df6.StateUID.values,index=df6.State).to_dict()
        '''
    else:
        print("here")
        if ids != 'builduid' and ids !="assignmentuid" and ids != "resourceuid":
            val = "fieldvalue"
            dispName = "fieldname"
            glist = set(df[dispName].dropna().unique())

    if "modifiedon" in df.columns:
        df.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
    cols_Req.remove("modifiedon")
    if 'DateColumn' not in cols_Req:
        cols_Req = cols_Req + ['DateColumn']        
    if "modifiedon" in  cols_Req:
        cols_Req.remove("modifiedon") 
    if ids != 'builduid' and ids !="assignmentuid" and ids != "resourceuid" and ids != 'workitemuid':
        
        glist = glist.union(set(cols_Req))
    temp_list = list()
    if RequiredColumns != 'WorkItem':
        for x in df[ids].unique():
            df1 = df[df[ids].isin([x])].sort_values(by=['DateColumn'],ascending=False)
            df1 = df1[df1.DateColumn.isin([list(df1.DateColumn)[0]])]
            df1 = df1[cols_Req]
            df1 = df1.loc[:,~df1.columns.duplicated()]
            temp_list.append(df1)
            if ids == 'codebranchuid' or ids == 'builduid' or ids == 'environmentuid'  or ids == 'assignmentuid' or ids == 'resourceuid'or ids == 'assignmentuid':
                dfs.append(df1)
            else:
                df1['key'] = df1[itemexternalid].astype(str)+ df1[dispName]
                df1.drop_duplicates(inplace = True)
                temp_ = []
                for s in df1.itemexternalid.unique():
                    temp = df1[df1.itemexternalid.isin([s])]
                    temp = temp.T
                    cols = temp.loc[dispName].dropna().to_dict()
                    temp.rename(columns = cols,inplace = True)
                    temp.drop(index = ['key'], inplace = True)
                    inx = set(temp.index).difference({val,dispName})
                    for col in inx:
                        temp[col] =  pd.Series(temp.loc[col].unique()[0],index= temp.index)
                    inx =  list(inx)
                    t = list(cols.values()) + inx
                    inx.append(dispName)
                    temp.drop(index =inx,inplace = True)
                    temp = temp[t]
                    temp = temp.loc[:,~temp.columns.duplicated()]
                    glist = glist.difference({val,dispName})
                    j = pd.DataFrame(data = None, index = temp.index, columns = glist)
                    j.loc[val] = temp.loc[val]
                    temp_.append(j)
                df_uid= pd.concat(temp_)
                dfs.append(df_uid)  
         
        entity_df = pd.concat(dfs)
    else:
        entity_df = df
    '''
    if RequiredColumns == 'WorkItem':
        try:
            d = {oldk: oldv for oldk, oldv in df4.items()}
            entity_df['Iterationname'] = entity_df['Iteration'].map(d)
        except:
            pass
        try:
            e= {oldk: oldv for oldk, oldv in df5.items()}
            entity_df['Releasename'] = entity_df['Release'].map(e)
        except:
            pass
        try:
            f= {oldv: oldk for oldk, oldv in df7.items()}
            entity_df['StateUID'] = entity_df['State'].map(f)
        except:
            pass
    '''
    entity_df.reset_index(drop = True,inplace = True)
    return entity_df

def TransformEntities(MultiSourceDfs,cid,dcuid,EntityMappingColumns,parent,auto_retrain,flag = False):                          
    min_df = min_data() 
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])
    uids = [p.replace("externalid","uid") for p in EntityMappingColumns ]
    #productinstance = set([p.replace("externalid","productinstances_productinstanceuid") for p in EntityMappingColumns ])
    MappingColumns = {}
    for k,v in MultiSourceDfs['Entity'].items():
        otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])             
        if  type(v) != str:
            ids = list(set(uids).intersection(v.columns))
            if 'workitemuid' in ids:
                 ids = 'workitemuid'
            else:
                if len(ids) > 1:
                    ids = list(set([k.lower()+"uid"]).intersection(ids))[0]
                else:
                    ids = ids[0]
            file_name = ids[:-3]
            b = list(EntityMappingColumns.intersection(v.columns))
            a = file_name + otherReqCols[0]
            otherReqCols.remove(otherReqCols[0])
            otherReqCols.insert(0,a)
            m = list(set(otherReqCols).intersection(b))
            prdctid = "productinstanceuid"                       
            if "workitemexternalid" in m:
                file_name = k.lower()
                if 'createdbyproductinstanceuid' in v.columns:
                    v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
                if 'stateuid' in v.columns:
                    v.rename(columns = {'stateuid':'State'}, inplace=True)
                
                for col in m:
                    v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
                v["UniqueRowID"] = v.workitemexternalid + v.itemexternalid
                try:
                    df = v.copy()
                    v = transformCNV(v,cid,dcuid,k,WorkItem = True)
                except:
                    v = df.copy()
            else:
                if k != 'Environment' and k != "Observation":
                    if 'createdbyproductinstanceuid' in v.columns:
                        v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
                    v["UniqueRowID"] = v[m[0]] + v[m[1]]
                    for col in m:                  
                        v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
                v.columns = v.columns.str.replace('[.]', '')
                try:
                    df = v.copy()
                    v = transformCNV(v,cid,dcuid,k,WorkItem = False)
                except:
                    v = df.copy()

            if v.empty:
                if auto_retrain:
                    v="No Incremental Data Available"
                else:
                    v = "Data is not availble"
                MultiSourceDfs['Entity'][k] = v 
            elif v.shape[0] <= min_df and not auto_retrain:
                v = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                MultiSourceDfs['Entity'][k] = v
            elif v.shape[0] >max_data():
                v = "Number of records greater than "+str(max_data())+". Kindly upload data through mydatasource"
                MultiSourceDfs['Entity'][k] = v
            else:
                if flag == None:
                    if type(v) != str:
                        Rename_cols = dict(ChainMap(*list(map(lambda x: {x: x + "_" + file_name}, list(v.columns.astype(str))))))
                        if parent['Name'] == 'null' or  parent['Name'][:-7] == k:
                            if 'DateColumn' in Rename_cols.keys():
                                Rename_cols['DateColumn'] = 'DateColumn'
                        v.rename(columns=Rename_cols, inplace=True)
                        empty_cols = [col for col in v.columns if v[col].dropna().empty]
                        v.drop(columns = empty_cols ,inplace = True)
                        v = v.replace('',np.nan)
                        v = v.replace("",np.nan)
                        if k.endswith(".csv") or k.endswith(".xlsx"):
                            k = os.path.basename(k).split('.')[0][36:]
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m))) 
                        else:
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m)))
                else:
                      empty_cols = [col for col in v.columns if v[col].dropna().empty]
                      v.drop(columns = empty_cols ,inplace = True)
                      v = v.replace('',np.nan)
                      v = v.replace("",np.nan)
                      for col in v.columns:
                            try:
                                v[col].fillna(v[col].mode()[0], inplace=True)
                            except:
                                v.drop(columns = [col], inplace = True)
                      MultiSourceDfs['Entity'][k] = v.copy()
                      MappingColumns[k] = []

    return MultiSourceDfs, MappingColumns


#need to check further
def getLatestData(correlationId):
    EnDeRequired = True
    dbconn, dbcollection = open_dbconn('AIServiceIngestData')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    dataframe = pd.DataFrame()
    try:
        for i in range(len(data_json)):
            actual_data = data_json[i].get("InputData")
            if EnDeRequired :
                t = base64.b64decode(actual_data) #DE55......................................
                data = eval(EncryptData.DescryptIt(t))
            else:
                data = actual_data.strip().rstrip()
                if isinstance(data,str):
                    try:
                        data = eval(data)
                    except SyntaxError:
                        data = eval(''.join([i if ord(i) < 128 else ' ' for i in data]))
                    except:
                        data = json.loads(data)
            data = pd.DataFrame(data)
            dataframe = dataframe.append(data, ignore_index = True)
            dataframe = dataframe.sort_values(by='DateColumn')
            dataframe = dataframe.tail(5)
    except:
        dataframe = 'Error'


    return dataframe



    

def getRequestSummaryDetails(correlationId,unid):
    dbconn, dbcollection = open_dbconn('AIServicesTextSummaryPrediction')
    args = list(dbcollection.find({"CorrelationId": correlationId, "UniId":unid}))
    dbconn.close()
    args=EncryptData.DescryptIt(base64.b64decode(args[0]["ActualData"]))
    return eval(args)

def UpdateAIServicesPrediction(correlationid,uniqueId,predictions,status,progress,msg):
    dbconn, dbcollection = open_dbconn('AIServicesTextSummaryPrediction')
    predictions=EncryptData.EncryptIt(predictions)
    dbcollection.update_one({"CorrelationId":correlationid,"UniId":uniqueId},
    {'$set':{'PredictedData' : predictions,
                    'Status' : status,
                   'Progress': str(progress),
                   "Chunk_number":0,
               "ErrorMessage": msg,
                "ModifiedOn":datetime.now().strftime('%Y-%m-%d %H:%M:%S')
            }
    })
    dbconn.close()
    return

def getClientNativeValues(df,cid,dcuid,entity,token,WorkItem = False):

    metadataAPI = config['ClientNative']['metadataAPI']
    if WorkItem:
        args  = {"ClientUId":cid,
                     "DeliveryConstructUId":dcuid,    
                     "EntityUId":  eval(EntityConfig['WorkItem']['EntityUID']),
                     "WorkItemTypeUId": eval(EntityConfig['CDMConfig']['AgileWorkItemTypes'])[entity]
            }
    else:
          args  = {"ClientUId":cid,
                     "DeliveryConstructUId":dcuid,    
                     "EntityUId":  eval(EntityConfig[entity.lower()]['EntityUID'])
                   
            }
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':
        metaAPIResults = requests.post(metadataAPI,data=json.dumps(args),
                                                       headers={'Content-Type':'application/json',
                                                            'Authorization': 'bearer {}'.format(token),
                                                            'AppServiceUId':config['ClientNative']['AppServiceUId']},
                                                      params={"clientUId": cid, 
                                                               "deliveryConstructUId":dcuid}
                                                           )
    elif auth_type == 'WindowsAuthProvider':
        metaAPIResults = requests.post(metadataAPI,data=json.dumps(args),
                                                       headers={'Content-Type':'application/json',
                                                            'Authorization': 'bearer {}'.format(token),
                                                            'AppServiceUId':config['ClientNative']['AppServiceUId']},
                                                      params={"clientUId": cid, 
                                                               "deliveryConstructUId":dcuid},
                                                           auth=HttpNegotiateAuth())  
    
    if metaAPIResults.status_code == 200:
        metaAPIResponse = metaAPIResults.json()['EntityPropertyValues']
        key_value = {}
        cdm_value = {}
        for item in metaAPIResponse:
            if item['DisplayName'] != None:
                values = item['Values']
                values_dict = {}
                cdm_dict = {}
                for i in range(len(values)):
                    t = values[i]['ProductPropertyValueUId'].replace("-","").upper()
                    values_dict[t] = values[i]['ProductPropertyValue']
                    cdm_dict[t] = values[i]['EntityPropertyValue']
  
                key_value[item['DisplayName']] = values_dict
                cdm_value[item['DisplayName']] = cdm_dict
                
        
 
        return key_value,cdm_value
    else:
        return False,metaAPIResults.status_code

def transformCNV(df,cid,dcuid,entity,WorkItem = False):
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':
        token,status_code = getVDSAzureToken()
    elif auth_type == 'WindowsAuthProvider':
        token = ''
        status_code = 200
        
    if WorkItem:
        key_value,cdm_value = getClientNativeValues(df,cid,dcuid,entity,token,WorkItem = True)
        NvList = ['Currency','Probability','State','Priority','Severity']
       
        for col in NvList:
            if col in key_value.keys():
                  k = key_value[col]
                  c = cdm_value[col] 
                  col = col.lower()+"uididvalue"
                  df[col+"Cnv"] = df[col]
                  df[col] = df[col].astype(str).replace(k)
#                df['StateUID'] = df['StateUID'].dropna().map(lambda a:  str(uuid.UUID(a)))
#                df['StateUID'] = df['StateUID'].astype(str).replace(cdm_value['State'])
    else:
         key_value,cdm_value = getClientNativeValues(df,cid,dcuid,entity,token,WorkItem = False)
         #print(cdm_value)
         print("key_value.keys()",key_value.keys())
         NvList = ['PhaseDetected', 'PhaseInjected', 'Priority', 'Reference', 'Severity', 'State','Type']
         if key_value != False:
             for col in NvList: 
                 if col in key_value.keys(): 
                     k = key_value[col]
                     c = cdm_value[col] 
                     col = col.lower()+"uid"
                     if len(k) !=0 and col in  df.columns:
                         print(col)
#                         df[col] = df[col].replace('',np.nan)
#                         df[col] = df[col].fillna(df.priorityuid.mode()[0]).map(lambda a:  str(uuid.UUID(a)))
                         df[col+"CnvId"] = df[col]
                         df[col+"CnvName"] = df[col]
                         df[col+"CnvName"] =  df[col+"CnvName"].astype(str).replace(k)
                         df[col] = df[col].astype(str).replace(c)
    return df

def intentstrMatch(inputStr,split_data):
    return_value=''
    try:
        for data in split_data:
            data=re.sub(r'\"','',data)
            print(data.lower(),inputStr)
            data=data.lstrip(' ')
            data=data.rstrip(' ')
            if str(data).lower()==str(inputStr):
                print('okay')
                return_value=data
                break
    except Exception as e:
            print('Exception from strmatch utils',str(e))
    return return_value
#if __name__ == '__main__':
    #input_file='/var/www/myWizard.IngrAInAIServices.WebAPI.Python/intent_entity/generated/client7/dc1/corr8/excel_data/APT_100.xlsx'
    #input_file_csv='/var/www/myWizard.IngrAInAIServices.WebAPI.Python/nextword_prediction/training/client7/dc1/corr3/VA_Chat_Utterances_eng.txt' 
    #decrypted =read_txt(input_file)
    #decrypted =read_textfile(input_file_csv)

    #print("results*************",decrypted)

