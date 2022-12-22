# -*- coding: utf-8 -*-


import platform
from pandas import Timestamp
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    conf_path_pheonix = '/IngrAIn_Python/main/pheonixentityconfig.ini'
    work_dir = '/IngrAIn_Python'
    modelPath = '/IngrAIn_Python/saveModels/'
    # pylogs_dir = '/IngrAIn_Python/tranPyLogs/'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    conf_path_pheonix = '\IngrAIn_Python\main\pheonixentityconfig.ini'
    work_dir = '\IngrAIn_Python'
    modelPath = '\IngrAIn_Python\saveModels\\'
    from requests_negotiate_sspi import HttpNegotiateAuth
import configparser, os

mainPath = os.getcwd() + work_dir
import sys
from pythonjsonlogger import jsonlogger

sys.path.insert(0, mainPath)
sys.path.insert(0, mainPath + '/main')
import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
EntityConfig = configparser.RawConfigParser()
EntityConfigpath = str(os.getcwd()) + str(conf_path_pheonix)
EntityConfig.read(EntityConfigpath)
logpath = config['filePath']['pyLogsPath']
if not os.path.exists(logpath):
    os.mkdir(logpath)
saveModelPath = config['filePath']['saveModelPath']

pickleKey = config['SECURITY']['pklFileKey']


import collections
import numbers
import uuid
from pymongo import MongoClient
import sys

import numpy as np
import pandas as pd
from pandas.io.json import json_normalize
							
import math
import json
import logging
from datetime import datetime,timedelta
from dateutil import relativedelta
import os
import re
import numpy
from numpy import random
#sys.modules['numpy.random.bit_generator']=numpy.random._bit_generator
from sklearn.model_selection import train_test_split
#from SSAIutils import encryption
import pickle
from pymongo.errors import ServerSelectionTimeoutError
from pymongo.errors import NetworkTimeout
import configparser
import pymongo
import requests, json
from collections import ChainMap
from urllib.parse import urlencode


from SSAIutils import EncryptData
from cryptography.fernet import Fernet
import base64
import io
import sklearn
#sys.modules['sklearn.preprocessing._label'] = sklearn.preprocessing.label
from sklearn import linear_model
#sys.modules['sklearn.linear_model._logistic'] = sklearn.linear_model.logistic
#sys.modules['sklearn.linear_model._ridge']=sklearn.linear_model.ridge
from sklearn import metrics
#sys.modules['sklearn.metrics._scorer'] = sklearn.metrics.scorer
#sys.modules['sklearn.metrics._classification'] = sklearn.metrics.classification
#sys.modules['sklearn.metrics._regression']=sklearn.metrics.regression
#sys.modules['sklearn.metrics._ranking']=sklearn.metrics.ranking


'''
collection="PS_IngestedData"
'''

def min_data():
    min1=config['Min_Data']['min_data']
    
    return int(min1)
def lambda_exec(data,freq_most):
    return data.apply(lambda x: " ".join(x for x in x.split() if x not in freq_most))
def get_auth_type():
    auth=config['GenericSettings']['authProvider']
    return auth
def mindatapointsforVDS():
    mindatapointsforVDS1 = config['GenericSettings']['mindatapointsforVDS']
    return int(mindatapointsforVDS1)

def gettraincols(corrid):
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    try:
        Sample = json.loads(list(dbcollection.find({"CorrelationId": corrid}))[0]["InputSample"])[0]
    except:
        x = base64.b64decode(list(dbcollection.find({"CorrelationId": corrid}))[0]["InputSample"]) #En.............................
        Sample = json.loads(EncryptData.DescryptIt(x))[0]   
    columns = list(Sample.keys())
    return columns

def customdatapoints(UsecaseId, AppId):
    dbconn, dbcollection = open_dbconn('PublicTemplateMapping')
    if AppId:
        args = dbcollection.find_one({"ApplicationID": AppId, "UsecaseID": UsecaseId})
        if args == None:
            args = dbcollection.find_one({ "UsecaseID": UsecaseId,"IsMultipleApp" : "yes"})
        datapoints = args["DataPoints"]
    return int(datapoints)

def maxdatapull(correlationId):
    dbconn, dbcollection = open_dbconn('SSAI_DeployedModels')
    if correlationId:
        args = dbcollection.find_one({"CorrelationId": correlationId})
        if "MaxDataPull" in args:
            data_points = args["MaxDataPull"]
        else:
            data_points = int(config['maxpull']['records'])
        if int(data_points) == 0:
            data_points = 30000
    return int(data_points)


def mindatapointsforHourly():
    mindatapointsforHourly = config['GenericSettings']['hourly']
    return int(mindatapointsforHourly)

def lambdaDataQaulity(FilterData_obj):
    for col in FilterData_obj.columns:
        FilterData_obj[col] =  FilterData_obj[col].astype(str).str.strip()
    return FilterData_obj


def getRequestParams(correlationId, requestId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    if requestId:
        args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
    else:
        args= dbcollection.find_one({"CorrelationId": correlationId,"pageInfo": "IngestData"},sort=[( 'CreatedOn', pymongo.DESCENDING )])
    #print (args)
    return args["ParamArgs"], args["ModelName"],args
	
def getRequestParams_generic_flow(correlationId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    args= dbcollection.find_one({"CorrelationId": correlationId,"pageInfo": "IngestData"},sort=[( 'CreatedOn', pymongo.DESCENDING )])
    #print (args)
    return args["ParamArgs"], args["ModelName"],args
	
def getAutoRetrainflag(correlationId,pageInfo):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    args = dbcollection.find_one({"CorrelationId": correlationId, "pageInfo": pageInfo})
    try:
        paramArgs = eval(args["ParamArgs"])
    except:
        paramArgs = {}
    if paramArgs == "AutoRetrain":
        return True
    else:
        return False
    
def open_dbconn(collection):
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection.get_default_database()
    db_IngestedData = db[collection]

    return connection, db_IngestedData

def encryptPickle(file,file_path):
    pickeledData = pickle.dumps(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    encrypted_data = f.encrypt(pickeledData)
    if platform.system() == 'Windows':
        file_path = open(bytes(file_path, "utf-8").decode("unicode_escape"), 'wb')
    else:
        file_path = open(file_path, 'wb')
    pickle.dump(encrypted_data, file_path)
    return 

def decryptPickle(filepath):
    if platform.system() == 'Windows':
        file = open(bytes(filepath, "utf-8").decode("unicode_escape"), 'rb')
    else:
        file = open(filepath, 'rb')
    loadedPickle = pickle.load(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    model = pickle.loads(f.decrypt(loadedPickle))
    return model


def getEncryptionFlag(corrid):  #En.................
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    #print("******************* corrid " ,corrid)
    flag = list(dbcollection.find({"CorrelationId": corrid}))[0]["DBEncryptionRequired"]
    #print("getenc********")
    
    return flag
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

def getInstaURL(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data = list(dbcollection.find({"CorrelationId": correlationId}))
    url = data[0]['InstaURL']
    return url

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

def get_DataCleanUpView(correlationId):
    EnDeRequired = getEncryptionFlag(correlationId)
    dtype = {}
    scale = {}
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    q = data_json[0]['Feature Name']
    if EnDeRequired:
        x = base64.b64decode(q) #En.............................
        q = json.loads(EncryptData.DescryptIt(x))
    df = pd.DataFrame.from_dict(q, orient='index')
    for col in df.index:
        dtype[col] = list(df.Datatype[col].keys())
        scale[col] = list(df.Scale[col].keys())
    return df, dtype, scale

def get_DataCleanUpView_addfeature(correlationId):
    EnDeRequired = getEncryptionFlag(correlationId)
    dtype = {}
    scale = {}
    #print("x")
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    q = data_json[0]['Feature Name_New Feature']
    if EnDeRequired:
        x = base64.b64decode(q) #En.............................
        q = json.loads(EncryptData.DescryptIt(x))
    df = pd.DataFrame.from_dict(q, orient='index')
    for col in df.index:
        dtype[col] = list(df.Datatype[col].keys())
        scale[col] = list(df.Scale[col].keys())
    return df, dtype, scale

def get_OGDataFrame(data, encoders):
    data_t = data
    for key, value in encoders.items():
        if key == 'OHE':
            for key1, value1 in value.items():
                encoder = key1
                columns = value1.get('EncCols')
                og_cols = value1.get('OGCols')
            dec_val = encoder.inverse_transform(data_t.get(columns))
            dec_data = pd.DataFrame(dec_val, columns=og_cols)
            data_t = data_t.drop(columns, axis=1)
            data_t = pd.concat([data_t.reset_index(drop=True), dec_data.reset_index(drop=True)], axis=1)
        elif key == 'LE':
            for key1, value1 in value.items():
                encoder = key1
                columns = value1
            for indx in range(len(columns)):
                dec_val1 = encoder.inverse_transform(data_t.get(columns[indx]))
                dec_data1 = pd.DataFrame(dec_val1, columns=[columns[indx][:len(columns[indx]) - 2]])
                data_t = data_t.drop(columns[indx], axis=1)
                data_t = pd.concat([data_t.reset_index(drop=True), dec_data1.reset_index(drop=True)], axis=1)
    return data_t


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


'''
chunks = file_split(data)
'''
def updateProcessId(correlationId,UniId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    data = dbcollection.find({"CorrelationId": correlationId,
                                              "UniId": UniId,										  
                                              })
    dbcollection.update({"UniId": UniId}, {'$set': {"PythonProcessID": processId() }})                                      
    dbconn.close()

def file_split(data,Incremental = False,appname=None):
    data = data.reset_index(drop=True)
    t_filesize = (sys.getsizeof(data) / 1024) / 1024
    nulls = data.isnull().sum(axis=1)
    null_index = nulls.idxmin()
    row1size = (sys.getsizeof(data.iloc[[null_index]]) / 1024) / 1024
    capacity = 2
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
    elif Incremental:        
		
            dbconn,dbcollection = open_dbconn('AppIntegration')
            data_json = list(dbcollection.find({"ApplicationName":appname}))
            rows = int(data_json[0].get("chunkSize"))
            #print("rows::::::::::::",rows)
            chunks = np.split(data, range(1 * rows, (nrows // rows + 1) * rows, rows))
            #print("chunks:::::",len(chunks))
    return chunks, t_filesize


'''
collection="SSAI_IngestedData" 
corid = str(uuid.uuid4())
chunks = file_split(data)
save_data_chunks(corid=None,chunks,collection):
'''
def getAddFeatures(UserInputColumns,parentcorrelationId,correlationId):
    dbconn, dbcollection = open_dbconn("DE_DataCleanup")
    data_json = list(dbcollection.find({"CorrelationId": parentcorrelationId}))
    dbconn.close()
    Features_Created = []
    if len(data_json)>0:
        if 'NewAddFeatures' in data_json[0]:
            NewAddFeatures = data_json[0]['NewAddFeatures']
            for features in NewAddFeatures:
                for j in NewAddFeatures[features]:
                    if NewAddFeatures[features][j]['ColDrp'] in UserInputColumns:
                        flag = 1
                    else:
                        flag = 0
                        break
                if flag == 1:
                    Features_Created.append(features)
            dbconn, dbcollection = open_dbconn("DE_DataCleanup")
            #data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbcollection.update_one({"CorrelationId":correlationId},{'$set':{         
                                    'NewAddFeatures' : { feature: data_json[0]['NewAddFeatures'][feature] for feature in Features_Created }
                                   }}) 
            dbconn.close()
                       

    return 

def save_data_chunks(chunks, collection, corid, pageInfo, userId, columns,Incremental=False,requestId=None,sourceDetails=None, colunivals=None,
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
        if EnDeRequired:                                  #EN555.................................
            data_json = EncryptData.EncryptIt(data_json)

        #print("data_json")
        if not Incremental:
            #print(len(data_json),chi,"............................................................................................................................................................................................................................................")
            if datapre == None:
                dbcollection.insert({
                    "CorrelationId": corid,
                    "InputData": data_json,
                    "ColumnsList": columns,
                    "DataType": DataType,
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
                    "ColumnsList": columns,
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


'''
Saving user inputed column data to mongoDB
'''


def save_DataCleanUP_FilteredData(target_variable, correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                  DateFormat, userId):
    dbconn, dbcollection = open_dbconn('DataCleanUP_FilteredData')
    # chunks,filesize= file_split(data)
    # for chi in range(len(chunks)):
    Id = str(uuid.uuid4())
    try:
        dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               'inputcols': UserInputColumns,
                               "ColumnUniqueValues": ColUniqueValues,
                               "types": typeDict,
                               "target_variable": target_variable,
                               "DateFormats": DateFormat,
                               "removedcols": {},
                               "CreatedByUser": userId}])
    except:
        for key,values in ColUniqueValues.items():
            ColUniqueValues[key] = list(ColUniqueValues[key])[:10]        
        dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               'inputcols': UserInputColumns,
                               "ColumnUniqueValues": ColUniqueValues,
                               "types": typeDict,
                               "target_variable": target_variable,
                               "DateFormats": DateFormat,
                               "removedcols": {},
                               "CreatedByUser": userId}])


def Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns, DateFormat, userId,removedcols ={}):
    dbconn, dbcollection = open_dbconn('DataCleanUP_FilteredData')
    dbcollection.update_many({"CorrelationId": correlationId}, {'$set':
                                                                    {"CorrelationId": correlationId,
                                                                     "ColumnUniqueValues": ColUniqueValues,
                                                                     "types": typeDict,
                                                                     "DateFormats": DateFormat,
                                                                     "inputcols": UserInputColumns,
                                                                     "removedcols": removedcols,
                                                                     "CreatedByUser": userId}})


def get_DataCleanUP_FilteredData_visualization(correlationId):
    frames = []
    dbconn, dbcollection = open_dbconn('DataCleanUP_FilteredData')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    for df in data_json:
        datadf = pd.DataFrame(df['UserData'])
        frames.append(datadf)
    FilterData = pd.concat(frames)
    return FilterData

						 
def uniqueIdentifier(corrid):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data_json = list(dbcollection.find({"CorrelationId": corrid}))
    unique_id = data_json[0]['TargetUniqueIdentifier']
    
    return unique_id

def get_DataCleanUP_FilteredData(correlationId):
    dbconn, dbcollection = open_dbconn('DataCleanUP_FilteredData')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    types = data_json[0]['types']
    target = data_json[0]['target_variable']
    removedcols = data_json[0]['removedcols']
    UserInputColumns = data_json[0]['inputcols']
    DateFormats = data_json[0]['DateFormats']
    return types, target, removedcols, UserInputColumns, DateFormats


def save_DE_DataCleanup(feature_name, Outlier_Dict, corrDict, correlationId, TargetProblemType, OrginalDtypes, userId,msg,Feature_name_new = {}):
    Id = str(uuid.uuid4())
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               "Feature Name": feature_name,
                               "OutlierDict": Outlier_Dict,
                               "CorrelationToRemove": corrDict,
                               "OginalDtypes": OrginalDtypes,
                               "Target_ProblemType": TargetProblemType,
                               "ProcessedRecords" : msg,
                               "CreatedByUser": userId,
                               "Feature Name_New Feature":Feature_name_new}])


def Update_DE_DataCleanup(correlationId, unchangedColumns, flag, feature_name=None, Outlier_Dict=None, corrDict=None,
                          TargetProblemType=None, orgtyp=None, userId=None,feature_name_new={}):
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    if flag != 4:
        dbcollection.update_many({"CorrelationId": correlationId},
                                 {'$set': {"CorrelationId": correlationId,
                                           "Feature Name": feature_name,
                                           "OutlierDict": Outlier_Dict,
                                           "CorrelationToRemove": corrDict,
                                           "Target_ProblemType": TargetProblemType,
                                           "OginalDtypes": orgtyp,
                                           "UnchangedDtypeColumns": unchangedColumns,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "CreatedByUser": userId,
                                           "Feature Name_New Feature":feature_name_new}})

    else:
        dbcollection.update_many({"CorrelationId": correlationId},
                                 {'$set': {"CorrelationId": correlationId,
                                           "UnchangedDtypeColumns": unchangedColumns}})

def Update_DE_DataCleanup2(correlationId,  flag, feature_name=None, Outlier_Dict=None, corrDict=None,
                          TargetProblemType=None,  userId=None,feature_name_new = {}):
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    if flag == None:
        dbcollection.update_many({"CorrelationId": correlationId},
                                 {'$set': {"CorrelationId": correlationId,
                                           "Feature Name": feature_name,
                                           "OutlierDict": Outlier_Dict,
                                           "CorrelationToRemove": corrDict,
                                           "Target_ProblemType": TargetProblemType,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "CreatedByUser": userId,
                                           "Feature Name_New Feature":feature_name_new}})


def get_DataCleanUp(correlationId):
    EnDeRequired = getEncryptionFlag(correlationId)
    dtype = {}
    scale = {}
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    q = data_json[0]['Feature Name']
    try:
        w = data_json[0]['Feature Name_New Feature']
        if not EnDeRequired:
            for i in list(q.keys()):
                if i in list(w.keys()):
                    q.pop(i)
        
    except:
        w = []
    if EnDeRequired:
        x = base64.b64decode(q) #De222
        q = json.loads(EncryptData.DescryptIt(x))
        if w != [] and w != {}:
            x = base64.b64decode(w) #De222
            w = json.loads(EncryptData.DescryptIt(x))
            
            for i in list(q.keys()):
                if i in list(w.keys()):
                    q.pop(i)
    try:
        columns_modifiedDict = data_json[0]['DtypeModifiedColumns']
    except:
        columns_modifiedDict ={}
    try:
        scale_modified = data_json[0]['ScaleModifiedColumns']
    except:
        scale_modified = {}
    orgnlTypes = data_json[0]['OginalDtypes']
    targetType = data_json[0]['Target_ProblemType']
    df = pd.DataFrame.from_dict(q, orient='index')
    df2 = pd.DataFrame.from_dict(w, orient='index')
    try:
        df2 = df2.loc[ : ,df2.columns != 'AddFeature']
    except:
        df2 = df2

    df3 = pd.concat([df,df2],axis=0)
    for col in df3.index:
        dtype[col] = list(df3.Datatype[col].keys())
        #scale[col] = list(df3.Scale[col].keys())
        if df3.Scale[col] != {}:
            for k, v in df3.Scale[col].items():
                if v == 'True':
                    O_N = ["Ordinal", "Nominal"]
                    O_N.remove(k)
                    O_N.insert(0, k)
                    scale[col] = O_N
        else:
            scale[col] = []
    return df3, columns_modifiedDict, dtype, scale, scale_modified, orgnlTypes, targetType


def save_data(correlationId, pageInfo, userId, collection, filepath=None, data=None, datapre=None):
    if filepath:
        if filepath.endswith('.csv'):
            data_t = pd.read_csv(filepath, error_bad_lines=False, encoding='latin-1')
            if data_t.shape[0] == 0:
                return 'No data in the csv. Please upload with data'
            if data_t.shape[0] <= 20:
                return 'Number of records less than or equal to 20. Please upload file with more records'
        elif filepath.endswith('.xlsx'):
            data_t = pd.read_excel(filepath, encoding='latin-1')
            if data_t.shape[0] == 0:
                return 'No data in the excel. Please upload with data'
            if data_t.shape[0] <= 20:
                return 'Number of records less than or equal to 20. Please upload file with more records'
        else:
            return 'please upload correct file'
        data_t.dropna(how='all', axis=1, inplace=True)
    elif data is not None:
        data_t = data
        data_t.dropna(how='all', axis=1, inplace=True)

    columns = list(data_t.columns)
    chunks, filesize = file_split(data_t)
    sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
    save_data_chunks(chunks, collection, correlationId, pageInfo, userId, columns,Incremental=False,requestId=None,sourceDetails=sourceDetails, colunivals=None,
                     datapre=datapre)
    return filesize


'''CHANGES START HERE'''

def UsecaseDefinitionDetails(df,correlationId,userId):
    
    EnDeRequired = getEncryptionFlag(correlationId)
    num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
    
    empty_cols = [col for col in df.columns if df[col].dropna().empty]
    df.drop(columns = empty_cols,inplace = True)
    p = ",".join(empty_cols)
    recs = df.shape
    #t = "IngrAIn have found "+str(recs)+" valid records and Removed following empty attributes from data.  "+str([p])
    t = "Successfully ingested "+str(recs[0])+" records for "+str(recs[1])+" attributes"
    if len(empty_cols) > 0:
        t = t + " and removed following attributes due to empty records. "+str([p])
    t1 = {"EmptyColumns":empty_cols,"Msg":t,"Records":list(recs)}
    unPercent = {}
    unValues = {}
    for col in df.columns:
        try:
            x = round(len(df[col].unique()) * 100.0 / len(df[col]),2)
        except:
            df[col] = df[col].astype(str)
            x = round(len(df[col].unique()) * 100.0 / len(df[col]),2)
       
        l = round((100 - x )* len(df)/100)
        if x >= 80 and  x < 100:
            msg = "You have selected unique identifier as  "+ col+" which has "+str(x)+"% uniqueness,IngrAIn will be converting to 100% unique by removing "+str(round(100-x,2))+"% ("+str(l)+") duplicate records. please click on ok to procced."
        else:
            msg = {}
        unPercent[col] = {"Message" :msg,"Percent":x}
        if df[col].dtypes.name in num_type:
            unValues[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())][:50]
        else:
            unValues[col] = list(df[col].dropna().unique().astype('str'))[:3]
     
    if EnDeRequired:
        unValues = EncryptData.EncryptIt(json.dumps(unValues))
    
    dbconn, dbcollection = open_dbconn('PS_UsecaseDefinition')
    dbcollection.update_many({"CorrelationId": correlationId},
                              {"$set" :{"CorrelationId": correlationId,
                                        "UniquenessDetails" : unPercent,
                                        "UniqueValues" : unValues,
                                        "ValidRecordsDetails" : t1,
                                         "UniqueIdentifier":None,
                                        "CreatedByUser": userId,
                                        "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S')
                                        }},upsert=True)
    
    

    return df



def updQdb(correlationId, status, progress, pageInfo, userId, modelName=None, problemType=None, UniId=None,
           retrycount=3,Incremental = False,requestId=None):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    # print (dbconn,dbcollection)
    if not progress.isdigit():
        message = progress
        rmessage = "Task Complete"
        Status = 'E'
        progress = '0'
    elif status == "C":
        message = "Task Complete"
        rmessage = "Task Complete"
    else:
        message = "In - Progress"
        rmessage = "In - Progress"  
    try:
        if not Incremental:
            if not modelName:
                if UniId != None:
                    if requestId == None:
                        data = dbcollection.find({"CorrelationId": correlationId,
                                              "UniId": UniId,
                                              "pageInfo": pageInfo
											  
                                              })
                    else:
                        data = dbcollection.find({"CorrelationId": correlationId,
                                              "UniId": UniId,
                                              "pageInfo": pageInfo,
											  "RequestId":requestId
											  
                                              })
					
                else:
                    if requestId == None:
                        data = dbcollection.find({"CorrelationId": correlationId,
                                              "pageInfo": pageInfo})
											  
                    else:
                        data = dbcollection.find({"CorrelationId": correlationId,
                                              "pageInfo": pageInfo,
											  "RequestId":requestId})
											  

                    #print("-----------------------",correlationId, pageInfo,data[0].get('RequestId'))
                dbcollection.update_many({'RequestId': data[0].get('RequestId')}, {'$set': {"CorrelationId": correlationId,
                                                                                            "Status": status,
                                                                                            "Progress": progress,
                                                                                            "RequestStatus": rmessage,
                                                                                            "Message": message,
                                                                                            "UniId": UniId,
                                                                                            "pageInfo": pageInfo,
                                                                                            "CreatedByUser": userId,
                                                                                            "ModifiedOn": datetime.now().strftime(
                                                                                                '%Y-%m-%d %H:%M:%S'),
                                                                                            "ModifiedByUser": userId}})
            else:
                if UniId:
                    data = dbcollection.find({"CorrelationId": correlationId,
                                              "pageInfo": pageInfo,
                                              "UniId": UniId})

                else:
                    data = dbcollection.find({"CorrelationId": correlationId,
                                              "pageInfo": pageInfo,
                                              "ModelName": modelName,
                                              "ProblemType": problemType})
                # print ("dataata",list(data))
                # print (dbcollection,modelName)
                dbcollection.update({'RequestId': data[0].get('RequestId')}, {'$set': {"CorrelationId": correlationId,
                                                                                       "Status": status,
                                                                                       "Progress": progress,
                                                                                       "RequestStatus": rmessage,
                                                                                       "Message": message,
                                                                                       "UniId": UniId,
                                                                                       "pageInfo": pageInfo,
                                                                                       "CreatedByUser": userId,
                                                                                       "ModifiedOn": datetime.now().strftime(
                                                                                           '%Y-%m-%d %H:%M:%S'),
                                                                                       "ModifiedByUser": userId}})
        else:
            #print("inside else")
            UniId = getUniId(correlationId,requestId)
            if not modelName:
                #print("inside if")
                if UniId != None:
                    data = dbcollection.find({"CorrelationId": correlationId,
                                                          "UniId": UniId,
                                                          "pageInfo": pageInfo,
                                                          "RequestId":requestId})
                    
                else:
                    data = dbcollection.find({"CorrelationId": correlationId,
                                                          "pageInfo": pageInfo,
                                                          "RequestId":requestId})
    
         
                dbcollection.update_many({'RequestId': data[0].get('RequestId')}, {'$set': {"CorrelationId": correlationId,
                                                                                                        "Status": status,
                                                                                                        "Progress": progress,
                                                                                                        "RequestStatus": rmessage,
                                                                                                        "Message": message,
                                                                                                        "UniId": UniId,
                                                                                                        "pageInfo": pageInfo,
                                                                                                        "CreatedByUser": userId,
                                                                                                        "ModifiedOn": datetime.now().strftime(
                                                                                                            '%Y-%m-%d %H:%M:%S'),
                                                                                                        "ModifiedByUser": userId}})
            else:
                if UniId:
                    
                    data = dbcollection.find({"CorrelationId": correlationId,
                                                          "pageInfo": pageInfo,
                                                          "UniId": UniId,
                                                          "RequestId":requestId
                                                          })
    
                else:
                    data = dbcollection.find({"CorrelationId": correlationId,
                                                          "pageInfo": pageInfo,
                                                          "ModelName": modelName,
                                                          "ProblemType": problemType,
                                                          "RequestId":requestId
                                                         })
                
                print ("progress",progress," ",data[0].get('RequestId'))
                dbcollection.update({'RequestId': data[0].get('RequestId')}, {'$set': {"CorrelationId": correlationId,
                                                                                                   "Status": status,
                                                                                                   "Progress": progress,
                                                                                                   "RequestStatus": rmessage,
                                                                                                   "Message": message,
                                                                                                   "UniId": UniId,
                                                                                                   "pageInfo": pageInfo,
                                                                                                   "CreatedByUser": userId,
                                                                                                   "ModifiedOn": datetime.now().strftime(
                                                                                                       '%Y-%m-%d %H:%M:%S'),
                                                                                                   "ModifiedByUser": userId}})

    except ServerSelectionTimeoutError:

                    if retrycount > 0:
                        retrycount -= 1
                        updQdb(correlationId, status, progress, pageInfo, userId, modelName=modelName, problemType=problemType,
                               UniId=UniId, retrycount=retrycount)
                    else:
                        raise ServerSelectionTimeoutError
     ##        with open("abc.txt",'a')as f:
     ##            f.write(str(x) + " documents updated. " +str(modelName)+str(progress)+"\n")
    except Exception as e:
        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^",str(e))
    dbconn.close()


def updateUniqueIdentifier(correlationId,columns,source,col = None):
    r = re.compile("UniqueRowID.*") ##### changesss
    if col == None:
        uniId = list(filter(r.match, columns))[0] ##### changesss
    else:
        uniId = col
    dbconn, dbcollection = open_dbconn('PS_UsecaseDefinition')
    dbcollection.update_many({"CorrelationId": correlationId},
                              {"$set" :{"CorrelationId": correlationId,
                                        "UniqueIdentifier" : uniId,
                                        "Source" : source,
                                        "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S')
                                        }},upsert=True)


def save_data_multi_file(correlationId, pageInfo,requestId, userId, multi_source_dfs, g ,parent, mapping, mapping_flag, ClientUID,
                         DeliveryConstructUId, insta_id, auto_retrain,Incremental, datapre=None, lastDateDict=None,
                         MappingColumns=None, previousLastDate = None,DataSetUId=None):
    all_columns = {}
    cols = []
    parent_file_index = 0
    counter = 0
    if Incremental == True:
        dbconn,dbcollection = open_dbconn('SSAI_DeployedModels')
        data_json = list(dbcollection.find({"CorrelationId":correlationId}))
        dbconn.close()
        appname = data_json[0].get("LinkedApps")[0]
    
    if mapping_flag != 'True':
	
        if 'API' not in multi_source_dfs.keys():
            multi_source_dfs['API'] = {}
        if multi_source_dfs['VDSInstaML'] != {}:
            if "vds_InsatML" in multi_source_dfs['VDSInstaML'].keys():
                if isinstance(multi_source_dfs['VDSInstaML']['vds_InsatML'], pd.DataFrame):
                    data_frame = multi_source_dfs['VDSInstaML']['vds_InsatML']
                    columns = list(data_frame.columns)
                    # chunks,size =file_split(data_frame)
                    sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "InstaID": insta_id,
                                     "Source": "InstaMl_VDS"}
                    if auto_retrain:
                        # print (InstaData)
                        size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo, userId,
                                                  columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                    else:
                        
                        data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                        columns = list(data_frame.columns)
                        chunks, size = file_split(data_frame)
                        save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                         sourceDetails, colunivals=None, lastDateDict=lastDateDict ,previousLastDate = previousLastDate)
                    return "single_file"
                else:
                    raise Exception(multi_source_dfs['VDSInstaML']['vds_InsatML'])

            else:
                counter = len(multi_source_dfs['VDSInstaML'].keys())
                error_dict = {}
                for key_i in multi_source_dfs['VDSInstaML'].keys():
                    if isinstance(multi_source_dfs['VDSInstaML'][key_i]['InstaML_Regression'], pd.DataFrame):
                        counter = counter-1
                        error_dict[key_i] = "Proper data formed."
                    else:
                        error_dict[key_i] = multi_source_dfs['VDSInstaML'][key_i]['InstaML_Regression']
                if counter == 0 :
                    for key_i in multi_source_dfs['VDSInstaML'].keys():
                        dfs = multi_source_dfs['VDSInstaML'][key_i]
                        data_frame = dfs['InstaML_Regression']
                        columns = list(data_frame.columns)
                        # chunks,size =file_split(data_frame)
                        sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "InstaID": insta_id,
                                         "Source": "InstaML_Regression"}
                        if auto_retrain:
                            # print (InstaData)
                            size = append_data_chunks(data_frame, "PS_IngestedData", key_i, pageInfo, userId,
                                                      columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                        else:
                            data_frame = UsecaseDefinitionDetails(data_frame,key_i,userId)
                            columns = list(data_frame.columns)
                            chunks, size = file_split(data_frame)
                            save_data_chunks(chunks, "PS_IngestedData", key_i, pageInfo, userId, columns,Incremental,requestId,
                                             sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                    return "single_file"
                else:
                    raise Exception(str(error_dict))

        elif multi_source_dfs['Custom'] != {}:
            if isinstance(multi_source_dfs['Custom']['Custom'], pd.DataFrame):
                data_frame = multi_source_dfs['Custom']['Custom']
                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                 "Source": "Custom"}
                data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                #chunks, size = file_split(data_frame)
                columns = list(data_frame.columns)
                if Incremental and pageInfo == "CascadeFile":
                    data_frame = cascadePredictionValidations(correlationId, data_frame)
                    columns = list(data_frame.columns)
                    chunks, size = file_split(data_frame,Incremental=Incremental,appname=appname)
                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                                
                elif Incremental:  
                    cols_required = gettraincols(correlationId)
                    unique_id = uniqueIdentifier(correlationId) 
                    try:
                        if unique_id not in cols_required:
                            cols_required.append(unique_id)
                    except Exception as e:
                        error_encounterd = str(e.args[0])
                    #print("cols_required",cols_required)
                    data_frame = data_frame[[i for i in cols_required if i in data_frame.columns]]															   
                    chunks, size = file_split(data_frame,Incremental=Incremental,appname=appname)
																															
                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                elif auto_retrain:
                    size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo,
                                                              userId, columns, sourceDetails, colunivals=None,
                                                              lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                    
                else:
                    try:
                        updateUniqueIdentifier(correlationId,columns,"custom")
                    except IndexError as e:
                        error_encounterd = str(e.args[0])
                    chunks, size = file_split(data_frame)
                    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                return "single_file"
            else:
                updQdb(correlationId, 'E',multi_source_dfs['Custom']['Custom'], pageInfo, userId)
                raise Exception(multi_source_dfs['Custom']['Custom'])
        elif multi_source_dfs['API'] != {} and 'API' in multi_source_dfs.keys():
            if isinstance(multi_source_dfs['API']['Custom'], pd.DataFrame):
                data_frame = multi_source_dfs['API']['Custom']
                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                 "Source": "Custom"}
                data_frame = UsecaseDefinitionDetails_forlarge_file(data_frame,DataSetUId,userId)
                #chunks, size = file_split(data_frame)
                columns = list(data_frame.columns)
                chunks, size = file_split(data_frame)
                save_data_chunks_large_file(chunks, "DataSet_IngestData", pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,DataSetUId=DataSetUId)											 
                return "single_file"
            else:
                raise Exception(multi_source_dfs['API']['Custom'])	   
        elif 'CustomMultipleFetch' in multi_source_dfs and multi_source_dfs['CustomMultipleFetch'] != {}:
            if isinstance(multi_source_dfs['CustomMultipleFetch']['CustomMultipleFetch'], pd.DataFrame):
                data_frame = multi_source_dfs['CustomMultipleFetch']['CustomMultipleFetch']
                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                 "Source": "CustomMultipleFetch"}
                data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                #chunks, size = file_split(data_frame)
                columns = list(data_frame.columns)
                if Incremental:
                    cols_required = gettraincols(correlationId)
                    unique_id = uniqueIdentifier(correlationId) 
                    try:
                        if unique_id not in cols_required:                    
                            cols_required.append(unique_id)
                    except Exception as e:
                        error_encounterd = str(e.args[0])
                    data_frame = data_frame[[i for i in cols_required if i in data_frame.columns]]
                    chunks, size = file_split(data_frame,Incremental = Incremental,appname=appname)
                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                else:
                    chunks, size = file_split(data_frame)
                    dbconn, dbcollection = open_dbconn('PS_IngestedData')
                    delete = dbcollection.delete_many({"CorrelationId": correlationId})
                    dbconn.close()
                    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)

                return "single_file"
            else:
                raise Exception(multi_source_dfs['CustomMultipleFetch']['CustomMultipleFetch'])
        else:
            # print (multi_source_dfs.keys(),multi_source_dfs)
            #print("elsepart*************")
            for key in multi_source_dfs.keys():

                if multi_source_dfs[key] != {}:
                    if key == 'metricDf':
                        if isinstance(multi_source_dfs[key]['Metric'], pd.DataFrame):
                            details = {}
                            data_frame = multi_source_dfs[key]['Metric']
                            details["Columns"] = data_frame.dtypes.apply(lambda x: x.name).to_dict()
                            details["FileExtensionOrig"] = 'Metric'
                            type_key = 'Metric'
                            cols.append(list(data_frame.columns))
                            if parent['Name'] == 'Metric' and parent['Type'] == 'Metric':
                                parent_file_index = counter
                            counter += 1
                            all_columns[correlationId + "_" + "Metric"] = details
                    elif key == 'Entity':
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
                                if g != {}:
                                    if key1 in g.keys():
                                        details["FileExtensionOrig"] = g[key1]
                                    elif key1 not in g.keys():
                                        details["FileExtensionOrig"] = 'Entity'
                                else:										
                                    details["FileExtensionOrig"] = 'Entity'
                                cols.append(list(data_frame.columns))
                                if parent['Name'] == key1 and parent['Type'] == 'Entity':
                                    parent_file_index = counter
                                counter += 1
                                all_columns[correlationId + "_" + key1] = details
                                #print("allcolumns***")
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

                    elif key == 'Large data':
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
            #raise Exception("Data Not available for selection")
            for key in multi_source_dfs.keys():
                if multi_source_dfs[key] != {}:
                    if key == 'metricDf':
                        if isinstance(multi_source_dfs[key]['Metric'], str):
                            raise Exception(multi_source_dfs[key]['Metric'])
                            
                    elif key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], str):
                               raise Exception(multi_source_dfs[key][key1])
                                
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], str):
                                raise Exception(multi_source_dfs[key][key1])

                    elif key == 'Large data':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], str):
                                raise Exception(multi_source_dfs[key][key1])                        
        elif counter == 1:
            #print("conter1****")
            for key in multi_source_dfs.keys():
                if multi_source_dfs[key] != {}:
                    if key == 'metricDf':
                        if isinstance(multi_source_dfs[key]['Metric'], pd.DataFrame):
                            data_frame = multi_source_dfs[key]['Metric']
                            type_key = 'Metric'
                            columns = list(data_frame.columns)
                            # chunks,size =file_split(data_frame)
                            sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": 'MetricCode',
                                             "Source": "VDS"}
                            if auto_retrain:
                                size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo,
                                                          userId, columns, sourceDetails, colunivals=None,
                                                          lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                            elif Incremental:
                                cols_required = gettraincols(correlationId)
                                unique_id = uniqueIdentifier(correlationId) 
                                try:
                                    if unique_id not in cols_required:
                                        cols_required.append(unique_id)
                                except Exception as e:
                                    error_encounterd = str(e.args[0])
                                data_frame = data_frame[[i for i in cols_required if i in data_frame.columns]]
                                
                                chunks, size = file_split(data_frame,Incremental = Incremental,appname=appname)
                                save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                            else:
                                data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                                columns = list(data_frame.columns)
                                updateUniqueIdentifier(correlationId,columns,"Metric")
                                chunks, size = file_split(data_frame)
                                save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                    elif key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'Entity'
                                columns = list(data_frame.columns)
                                # chunks,size =file_split(data_frame)
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": type_key,
                                                 "Source": "CDM"}
                                if auto_retrain:
                                    size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo,
                                                              userId, columns, sourceDetails, colunivals=None,
                                                              lastDateDict=lastDateDict,previousLastDate = previousLastDate)
                                elif Incremental and pageInfo == "CascadeFile":
                                    data_frame = cascadePredictionValidations(correlationId, data_frame)
                                    columns = list(data_frame.columns)
                                    chunks, size = file_split(data_frame,Incremental=Incremental,appname=appname)
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                                elif Incremental:
                                    cols_required = gettraincols(correlationId)
                                    unique_id = uniqueIdentifier(correlationId) 
                                    try:
                                        if unique_id not in cols_required:
                                            cols_required.append(unique_id)
                                    except Exception as e:
                                        error_encounterd = str(e.args[0])
                                    data_frame = data_frame[[i for i in cols_required if i in data_frame.columns]]
                                    chunks, size = file_split(data_frame,Incremental=Incremental,appname=appname)
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                                else:
                                    #print("dataelse*****")
                                    data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                                    columns = list(data_frame.columns)
                                    #print("columns")
                                    updateUniqueIdentifier(correlationId,columns,"Entity")
                                    chunks, size = file_split(data_frame)
                                    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId,
                                                     columns,Incremental,requestId, sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'File'
                                columns = list(data_frame.columns)
                                if not isFMModel(correlationId):
                                    chunks, size = file_split(data_frame)
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                                 "Source": type_key}
                                if Incremental and pageInfo == "CascadeFile":
                                    data_frame = cascadePredictionValidations(correlationId, data_frame)
                                elif Incremental and isFMModel(correlationId):
                                    data_frame = FMPredictionValidations(correlationId, data_frame)						   
                                data_frame = UsecaseDefinitionDetails(data_frame,correlationId,userId)
                                columns = list(data_frame.columns)
                                
                                if Incremental and pageInfo == "CascadeFile":
                                    #data_frame = cascadePredictionValidations(correlationId, data_frame)
                                    columns = list(data_frame.columns)
                                    chunks, size = file_split(data_frame)
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict)    
                                elif Incremental and isFMModel(correlationId):
                                    columns = list(data_frame.columns)
                                    chunks, size = file_split(data_frame)
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict)    
                                elif Incremental:
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                                else:
                                    if isFMModel(correlationId):
                                        data_frame = FMTrainingValidations(correlationId, data_frame)
                                        chunks, size = file_split(data_frame)
                                    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                                    #print("file saved")

                    elif key == 'Large data':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'File'
                                columns = list(data_frame.columns)
                                chunks, size = file_split(data_frame)
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                                 "Source": type_key}
                                data_frame = UsecaseDefinitionDetails_forlarge_file(data_frame,DataSetUId,userId)
                                columns = list(data_frame.columns)
                                
                                if Incremental:
                                    save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                     sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                                else:
                                    save_data_chunks_large_file(chunks, "DataSet_IngestData", pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,DataSetUId=DataSetUId)											 
            return "single_file"
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
        #print("FLAG_1:: ", flag_1)
        if not flag_1:
            parent_cols = cols[parent_file_index]
            parent_cols_len = parent_cols.__len__()
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

            #print("FLAG_2:: ", flag_2)

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
            #print("FLAG_4:: ", flag_4)

        if not flag_1 and not flag_2 and not flag_4:
            flag_3 = True
            #print("FLAG_3:: ", flag_3)
        else:
            flag_3 = False
            #print("FLAG_3:: ", flag_3)

        final_df = None
        flag_final = True
        #print ("hereeee",multi_source_dfs)        
        if flag_1 or flag_2:
            for key in multi_source_dfs.keys():
                if multi_source_dfs[key] != {}:
                    if key == 'metricDf':
                        data_frame = multi_source_dfs[key]['Metric']
                        if flag_final:
                            final_df = data_frame.copy()
                            flag_final = False

                    elif key == 'Entity':
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
            # chunks,filesize= file_split(final_df)
            sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
            if auto_retrain:
                size = append_data_chunks(final_df, "PS_IngestedData", correlationId, pageInfo, userId, columns,
                                          sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
            if Incremental and pageInfo == "CascadeFile":
                final_df = cascadePredictionValidations(correlationId, final_df)
                chunks, filesize = file_split(final_df)
                columns = list(final_df.columns)
                save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId, sourceDetails,
                                 colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
            elif Incremental:
                cols_required = gettraincols(correlationId)
                unique_id = uniqueIdentifier(correlationId) 
                try:
                    if unique_id not in cols_required:
                        cols_required.append(unique_id)
                except Exception as e:
                        error_encounterd = str(e.args[0])
                final_df = final_df[[i for i in cols_required if i in final_df.columns]]
                chunks, size = file_split(final_df,Incremental=Incremental,appname=appname)
                save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
            else:
                final_df = UsecaseDefinitionDetails(final_df,correlationId,userId)
                chunks, filesize = file_split(final_df)
                save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId, sourceDetails,
                                 colunivals=None, lastDateDict=lastDateDict, previousLastDate = previousLastDate)
            return "single_file"
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
            return "single_file"
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
            return "single_file"
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
            return "multi_file"
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
            return "multi_file"
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

                        if 'Metric' == mapping["mapping%s" % i]["source_file"]:
                            data_frame = multi_source_dfs[key]['Metric']
                            df1 = data_frame.copy()
                        elif key == 'Entity':
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

                        if 'Metric' == mapping["mapping%s" % i]["mapping_file"]:
                            data_frame = multi_source_dfs[key]['Metric']
                            df2 = data_frame.copy()
                        elif key == 'Entity':
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
                                        right_on=mapping["mapping%s" % i]["mapping_column"], how = mapping["mapping%s" % i]["mapping_join"])
                except MemoryError as e:
                    #print(e)
                    raise Exception("Memory Error")
                except Exception as e:
                    raise Exception("Mapping should be done on same type of columns")
                # if final_df.shape[0] > df1.shape[0]:
                # raise Exception("The parent-child relationship is not correct. Please validate.")
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

                                    if 'Metric' == mapping[m_id]["source_file"]:
                                        data_frame = multi_source_dfs[key]['Metric']
                                        df1 = data_frame.copy()
                                    elif key == 'Entity':
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
                            num_rows = final_df.shape[0]
                            join = 'right'
                        else:
                            for key in multi_source_dfs.keys():
                                if multi_source_dfs[key] != {}:

                                    if 'Metric' == mapping[m_id]["mapping_file"]:
                                        data_frame = multi_source_dfs[key]['Metric']
                                        df2 = data_frame.copy()
                                    elif key == 'Entity':
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
                                                    right_on=mapping[m_id]["mapping_column"], how = mapping["mapping%s" % i]["mapping_join"])
                            elif join == 'right':
                                final_df = pd.merge(df2, df1, left_on=mapping[m_id]["mapping_column"],
                                                    right_on=mapping[m_id]["source_column"], how = mapping["mapping%s" % i]["mapping_join"])
                        except MemoryError as e:
                            #print(e)
                            raise Exception("Memory Error")
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
        list_drop_cols = []
        for col in final_df.columns:
            try:
                if final_df[col].nunique() == 0:
                    list_drop_cols.append(col)
            except Exception as e:
                error_encounterd = str(e.args[0])
        if len(list_drop_cols) == len(final_df.columns):
            raise Exception("Mapping should be done on columns having common datapoints")
        else:
            final_df.drop(list_drop_cols, axis=1, inplace=True)
        columns = final_df.columns
        data_t = final_df[columns]
        columns = list(data_t.columns)
        sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
        if auto_retrain:
            size = append_data_chunks(final_df, "PS_IngestedData", correlationId, pageInfo, userId, columns,
                                      sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
        if Incremental and pageInfo == "CascadeFile":
            final_df = cascadePredictionValidations(correlationId, final_df)
            columns = list(final_df.columns)
            chunks, filesize = file_split(final_df)
            save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                             sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
        elif Incremental:
            cols_required = gettraincols(correlationId)
            unique_id = uniqueIdentifier(correlationId) 
            try:
                if unique_id not in cols_required:
                    cols_required.append(unique_id)
            except Exception as e:
                        error_encounterd = str(e.args[0])
            if unique_id+'_'+mapping['mapping0']['source_file'].lower() in columns:
                final_df[unique_id] = final_df[unique_id+'_'+mapping['mapping0']['source_file'].lower()]
            final_df = final_df[[i for i in cols_required if i in final_df.columns]]                              											 
            chunks, size = file_split(final_df,Incremental=Incremental,appname=appname)
            save_data_chunks(chunks, "SSAI_PublishModel", correlationId, pageInfo, userId, columns,Incremental,requestId,
                                                 sourceDetails, colunivals=None, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
        else:
            columns = list(data_t.columns)
            r = re.compile("UniqueRowID.*") ##### changesss
            k = list(filter(r.match, columns))
            data_t["UniqueRowID"] = pd.Series(data = "",index = data_t.index)
            for col in k:
                data_t["UniqueRowID"] = data_t["UniqueRowID"].astype(str) + data_t[col].astype(str)
            data_t = UsecaseDefinitionDetails(data_t,correlationId,userId)
            columns = list(data_t.columns)
            updateUniqueIdentifier(correlationId,columns,"Entity","UniqueRowID")            
            chunks, filesize = file_split(data_t)
            save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental,requestId, sourceDetails,
                             colunivals=None, datapre=datapre, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
        return "multi_file"


def store_pickle_file(multi_source_dfs, correlationId):
    tempFile = saveModelPath + "RawOrginalData" + "_" + correlationId + ".pkl"
#    output = open(tempFile, 'wb')
#    pickle.dump(multi_source_dfs, output)
    encryptPickle(multi_source_dfs,tempFile)
#    output.close()


def load_pickle_file(correlationId):
    tempFile = saveModelPath + "RawOrginalData" + "_" + correlationId + ".pkl"
    mydict2 = decryptPickle(tempFile)
#    pkl_file = open(tempFile, 'rb')
#    mydict2 = pickle.load(pkl_file)
#    pkl_file.close()
    
    return mydict2


'''CHANGES END HERE'''


def save_data_timeseries(correlationId, pageInfo, userId, collection, columns, data=None, datapre=None):
    sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
    t_filesize = (sys.getsizeof(data) / 1024) / 1024
    capacity = 4
    if t_filesize < capacity:
        try:
            save_data_chunks([data], collection, correlationId, pageInfo, userId, columns,Incremental=False,requestId=None, sourceDetails=sourceDetails, colunivals=None,
                         timeseries="one", datapre=None)
            tsize = t_filesize
        except:
            tsize = 0
            key = list(data.keys())[0]
            length = int(len(data[key]))
            new_data = data.copy()
            startlength = 0
            while startlength <= length:
                if length >=  startlength:
                    new_data[key] = new_data[key][startlength:startlength+150000]
                else:
                    new_data[key] = new_data[key][startlength:]                   
                save_data_chunks([new_data], collection, correlationId, pageInfo, userId, columns,Incremental=False,requestId=None, sourceDetails=sourceDetails, colunivals=None,
                             timeseries="one", datapre=None)
                startlength = startlength + 150000
                new_data = data.copy()  		
    else:
        tsize = 0
        for key in data:
            chunks, filesize = file_split(data[key])
            tsize += filesize
            save_data_chunks(chunks, collection, correlationId, pageInfo, userId, columns,Incremental=False,requestId=None,sourceDetails=sourceDetails,
                             colunivals=None, timeseries=key, datapre=None)
    return tsize


'''
collection="PS_IngestedData"    
corid = '9ec89034-4e3f-4193-9cec-b561fb041464'
corid="56e94516-be22-4939-8b56-6fc035de0ec0"
data_t = data_fro_chunks(corid,collection)
'''


def data_from_chunks(corid, collection, lime=None, recent=None):
    #print("herrrrrrrrrrrrrrrrrrrrr")
    # rege = [r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$',
    #        r'^(\b(0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](0?[1-9]|1[0-2])[^\w\d\r\n:](\d{4}|\d{2})\b)|(\b(0?[1-9]|1[0-2])[^\w\d\r\n:](0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](\d{4}|\d{2})\b)']
    #    rege=(r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$')
    #    rege = [r'\d{1,4}[/-]\d{1,2}[/-]\d{1,4}|\t|:?\d{2}:?\d{2}:?\d{2}']
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
    #
    for col in data_t1.columns:
        l = list(data_t1[col][data_t1[col].notnull()])
        if len(l) > 1:
            # print ("abc",l)
            temp = l[1]
            if re.compile(rege1).match(str(temp)) is None:
                for pattern in rege:
                    if re.compile(pattern).match(str(temp)) is not None:
                        # print("data1:")
                        # print(data_t1)
                        # if platform.system() == 'Linux':
                        data_t1[col] = pd.to_datetime(data_t1[col], dayfirst=True, errors='coerce')
                        # elif platform.system() == 'Windows':
                        # data_t1[col]= pd.to_datetime(data_t1[col],errors='coerce')
                        # print("data2:")
                        # print(data_t1)
                        # data_t1.dropna(subset=[col], inplace=True)
    if collection =="PS_IngestedData":
        dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": corid}))
        UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
        dbconn.close()
        if UniqueIdentifir != None:
                dbconn, dbcollection = open_dbconn("PS_UsecaseDefinition")
                data_json = list(dbcollection.find({"CorrelationId": corid}))
                UniquePercent= data_json[0]['UniquenessDetails'][UniqueIdentifir]['Percent']
                dbconn.close()
                if UniquePercent >= 90 and UniquePercent < 100:
                    data_t1 = data_t1.drop_duplicates(subset=[UniqueIdentifir], keep="last")

    return data_t1


def data_timeseries(corid, collection):
    EnDeRequired = getEncryptionFlag(corid)
    dbconn, dbcollection = open_dbconn(collection)
    data_json = dbcollection.find({"CorrelationId": corid})
    #print(str(data_json[0])[:300])
    count = data_json.collection.count_documents({"CorrelationId": corid})
    data_t = {}
   
    if count == 1:
        if EnDeRequired:
            p =   base64.b64decode(data_json[0]['InputData'])
            t = eval(EncryptData.DescryptIt(p))
            data_t = {key: pd.read_json(t[key]) for key in t}
        else:
            data_t = {key: pd.read_json(data_json[0]['InputData'][key]) for key in data_json[0]['InputData']}
    else:
        if EnDeRequired:
            for counti in range(count):
                p = base64.b64decode(data_json[counti]['InputData'])
                t = eval(EncryptData.DescryptIt(p))
                for key in t.keys():
                    if key in data_t:
                        df = pd.read_json(t[key])
                        data_t[key] = data_t[key].append(df, ignore_index=True)
                    else:
                        df = pd.read_json(t[key])
                        data_t[key] = df
        else:
            for counti in range(count):
                for key in data_json[counti]['InputData'].keys():
                    if key in data_t:
                        df = pd.read_json(data_json[counti]['InputData'][key])
                        data_t[key] = data_t[key].append(df, ignore_index=True)
                    else:
                        df = pd.read_json(data_json[counti]['InputData'][key])
                        data_t[key] = df
            
            # data_t = {key:pd.read_json(data_json[counti]['InputData'][key],convert_dates=['index']).rename(columns={'index':"DateCol"}) for key in data_json[counti]['InputData']}
    dbconn.close()
    return data_t


def insQdb(correlationId, status, progress, pageInfo, userId, modelName=None, problemType=None, UniId=None):
    Id = str(uuid.uuid4())
    dbconn, dbcollection = open_dbconn('SSAI_UseCase')
    if not progress.isdigit():
        message = progress
        progress = '0'
    elif status == "C":
        message = "Task Complete"
    else:
        message = "In - Progress"

    if not modelName:
        dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": correlationId,
                                   "Status": status,
                                   "Progress": progress,
                                   "Message": message,
                                   "UniId": UniId,
                                   "pageInfo": pageInfo,
                                   "CreatedByUser": userId,
                                   "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                   "ModifiedByUser": userId}])
    else:
        dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": correlationId,
                                   "Model Name": modelName,
                                   "ProblemType": problemType,
                                   "Message": message,
                                   "UniId": UniId,
                                   "Status": status,
                                   "Progress": progress,
                                   "pageInfo": pageInfo,
                                   "CreatedByUser": userId,
                                   "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                   "ModifiedByUser": userId}])

    dbconn.close()

def check_CorId(collection, correlationId, pageInfo):
    dbconn, dbcollection = open_dbconn(collection)
    data_json = dbcollection.find({"CorrelationId": correlationId,
                                   "pageInfo": pageInfo,
                                   "RequestStatus": "In - Progress"})
    count = data_json.collection.count_documents({"CorrelationId": correlationId,
                                                  "pageInfo": pageInfo,
                                                  "RequestStatus": "In - Progress"})
    dbconn.close()
    return count


class CustomJsonFormatter(jsonlogger.JsonFormatter):
    def add_fields(self, log_record, record, message_dict):
        super(CustomJsonFormatter, self).add_fields(log_record, record, message_dict)
        if not log_record.get('asctime'):
            # this doesn't use record.created, so it is slightly off
            now = datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%fZ')
            log_record['DateTime'] = now
        else:
            log_record['DateTime']	= log_record.get('asctime')
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
            log_record['Exception'] = log_record["exc_info"]
            del log_record['exc_info']

        del log_record['message']
        del log_record['uniqueId']
        del log_record['correlationId']
		
			
def json_translate(obj):
    if isinstance(obj, MyClass):
        return {"uniqueId": obj.uniqueId,"correlationId":obj.correlationId}

def processId():
    pid = os.getpid()
    return(str(pid))

def logger(logger, cor, level=None, msg=None,uniqueId=None):
    if logger == 'Get':
        d = datetime.now()
        logger = logging.getLogger(cor)
        if len(logger.handlers) > 0:
            for handler in logger.handlers:
                logger.removeHandler(handler)
            #        handler = logging.StreamHandler()

        handler = logging.FileHandler(
            logpath + 'SelfServiceAIPython' + '_' + str(d.day) + '_' + str(d.month) + '_' + str(d.year) + '.log')
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


def save_Py_Logs(logger, correlationId, file=None):
    # return
    if file == None:
        # print("handler::",str(logger.handlers[0]))
        # try:
        # print(logger)

        #print(str(logger.handlers[0]))
        FileName = str(logger.handlers[0]).split(' ')[1].split('/')[
            len(str(logger.handlers[0]).split(' ')[1].split('/')) - 1]
        # except Exception as e:
        # FileName = str(logger.handlers[0]).split(' ')[1].split('\\')[2].split('\x0')[1]
        file = open(str(logger.handlers[0]).split(' ')[1])
        Log = file.read()
        file.close()
        for i in logger.handlers:
            logger.removeHandler(i)
            i.flush()
            i.close()
        logging.shutdown()
    else:
        FileName = str(file)
        file = open(str(file))
        Log = file.read()
        file.close()
    return
    dbconn, dbcollection = open_dbconn('Py_Logs')
    data = dbcollection.find({"CorrelationId": correlationId})
    count = data.collection.count_documents({"CorrelationId": correlationId})
    if count == 0:
        Id = str(uuid.uuid4())
        dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": correlationId,
                                   "FileName": FileName,
                                   "Log": Log}])
    elif count >= 1:
        dbcollection.update_many({'_id': data[0].get('_id')},
                                 {'$set': {"CorrelationId": correlationId,
                                           "FileName": FileName,
                                           "Log": Log}})
    dbconn.close()


def data_cleanup_json(Data_quality_df, DtypeDict, ScaleDict):
    feature_name = {}
    for col_n, col_v in Data_quality_df.iterrows():
        temp_m = {}
        for dtyp_k, dtyp_l in DtypeDict.items():
            if dtyp_k == col_v['column_name']:
                temp_a = {}
                temp_b = {}

                if len(dtyp_l) == 0:
                    temp_a = {"Select Option": "True", "Text": "False", "category": "False", "Id": "False","float64":"False","int64":"False","datetime64[ns]":"False"}
                else:
                    for indx, val in enumerate(dtyp_l):
                        temp_a.update({str(val): ('True' if indx == 0 else 'False')})
                temp_b['Datatype'] = temp_a

        temp_m.update(temp_b)
        temp_m.update({'Unique': str(col_v['percent_unique'])})
        temp_c = {}
        temp_d = {}
        for x, y in enumerate(col_v['CorelatedWith']):
            temp_c.update({str(x): y})
        temp_d['Correlation'] = temp_c
        temp_m.update(temp_d)
        for temp_e, temp_f in ScaleDict.items():
            if temp_e == col_v['column_name']:
                temp_g = {}
                temp_h = {}
                for indx1, val1 in enumerate(temp_f):
                    temp_g.update({str(val1): ('True' if indx1 == 0 else 'False')})
                temp_h['Scale'] = temp_g
        #                for indx1,val1 in enumerate(temp_f):
        #                    temp_g.update({str(val1):'True' if indx1==0 else 'False'})
        #                temp_h['Scale'] = [temp_g]
        temp_m.update(temp_h)
        temp_m.update({'Missing Values': str(col_v['percent_missing'])})
        temp_m.update({'Outlier': str(col_v['Outlier_Data'])})
        temp_m.update({'Balanced': str(col_v['Balanced'])})
        temp_m.update({'Skewness': str(col_v['IsSkewed'])})
        temp_m.update({'ImBalanced': str(col_v['ImBalanced_col'])})
        temp_m.update({'OrdinalNominal': str(col_v['OrdinalNominal'])})
        temp_m.update({'ImBalanced': str(col_v['ImBalanced_col'])})

        temp_bin = col_v['Binning_Values']
        temp_col = {}
        for indx, dic_val, in enumerate(temp_bin.items()):
            temp = {}
            temp.update({'SubCatName': dic_val[0]})
            temp.update({'Value': dic_val[1]})
            temp_col.update({'SubCat' + str(indx): temp})
        temp_m.update({'BinningValues': temp_col})

        temp_m.update({'Data_Quality_Score': str(col_v['Data_Quality_Score'])})
        temp_m.update({'ProblemType': str(col_v['ProblemType'])})
        temp_m.update({'Q_Info': str(col_v['Q_Info'])})
        feature_name.update({col_v['column_name']: temp_m})
    return feature_name


def train_test_split_utils(x, y, test_size=0.3, random_state=50, stratify=None):
    if stratify:
        stratify = y
    else:
        stratify = None
    try:
        x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=test_size,
                                                            random_state=50, stratify=stratify)
    
    except Exception:
        
        x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=test_size,
                                                            random_state=50, stratify=None)

    return x_train, x_test, y_train, y_test


def getFeatureSelectionVariable(correlationId):
    dbconn, dbcollection = open_dbconn("ME_FeatureSelection")
    data_json = list(dbcollection.find({"CorrelationId": correlationId},
                                       {'_id': 0, "FeatureImportance": 1, "Train_Test_Split": 1, "KFoldValidation": 1,
                                        "StratifiedSampling": 1}))
    # print (data_json)
    selectedFeatures = [each for each in data_json[0]["FeatureImportance"] if
                        data_json[0]["FeatureImportance"][each]['Selection'] == "True"]
    trainTestSplitRatio = (100 - int(data_json[0]["Train_Test_Split"]["TrainingData"])) / 100
    try:
        Kfold = data_json[0]["KFoldValidation"]["SelectedKFold"]
    except:
        Kfold = data_json[0]["KFoldValidation"]["ApplyKFold"]
    stratify = data_json[0]["StratifiedSampling"]
    return ({"selectedFeatures": selectedFeatures, "trainTestSplitRatio": trainTestSplitRatio, "Kfold": Kfold,
             "stratify": stratify})


def getTargetVariable(correlationId):
    dbconn, dbcollection = open_dbconn("ME_FeatureSelection")
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    target_variable = data_json[0].get('Actual_Target')
    return target_variable

#def getUniqueIdentifier(correlationId):
#    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
#    data_json = dbcollection.find({"CorrelationId": correlationId})
#    dbconn.close()
#    return data_json[0].get('TargetUniqueIdentifier')


def getSelectedColumn(featureColumns, dataColumns):
    # selectedColumn = featureColumns
    selectedColumn = [each for each in dataColumns if
                      (each in featureColumns) or (each.startswith(tuple(map(lambda x: x + "_L", featureColumns)))) or (
                          each.startswith(tuple(map(lambda x: x + "_OHE", featureColumns))))]
    return selectedColumn

def insert_EvalMetrics_FI_C(correlationId, modelName, problemType, accuracy, log_loss, TP, TN, FP, FN, sensitivity,
                            specificity, precision, recall, f1score, c_error, ar_score, aucEncoded, featureImportance,
                            timeSpend, pageInfo, userId, visualization, modelparams=None,version=None, HTId=None, counter=3,clustering_flag = True):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            # print (TP,TN,FP,FN)
            if version==0:
                dbcollection.insert_many([{
                          
                "CorrelationId": correlationId,
                "ProblemType": problemType,
                "Accuracy": accuracy * 100,
                "log_loss": log_loss,
                "modelName": modelName,
                "_id":Id,
                "TruePositive": int(TP),
                "TrueNegative": int(TN),
                "FalsePositive": int(FP),
                "FalseNegative": int(FN),
                "Sensitivity": sensitivity,
                "Specificity": specificity,
                "precision": precision,
                "recall": recall,
                "f1score": f1score,
                "c_error": c_error,
                "ar_score": ar_score,
                "AUCImage": aucEncoded,
                "featureImportance": featureImportance,
                "ModelParams": modelparams,
                "Clustering_Flag": clustering_flag,
                "RunTime": timeSpend,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "visualization": visualization,
                "pageInfo": pageInfo,
                "Version":version,
                "CreatedByUser": userId
            }])
            else:
                dbcollection.update_many({"CorrelationId": correlationId,"modelName": modelName,"Version":version},
                {"$set":{
                    "CorrelationId": correlationId,
                    "ProblemType": problemType,
                    "Accuracy": accuracy * 100,
                    "log_loss": log_loss,
                    "TruePositive": int(TP),
                    "TrueNegative": int(TN),
                    "FalsePositive": int(FP),
                    "FalseNegative": int(FN),
                    "Sensitivity": sensitivity,
                    "Specificity": specificity,
                    "precision": precision,
                    "recall": recall,
                     "_id":Id,
                    "f1score": f1score,
                    "c_error": c_error,
                    "ar_score": ar_score,
                    "AUCImage": aucEncoded,
                    "featureImportance": featureImportance,
                    "ModelParams": modelparams,
                    "Clustering_Flag": clustering_flag,
                    "RunTime": timeSpend,
                    "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "visualization": visualization,
                    "pageInfo": pageInfo,
                    "Version":version,
                    "CreatedByUser": userId
                }},upsert=True)
                dbconn.close()
        else:
            dbconn, dbcollection = open_dbconn("ME_HyperTuneVersion")
            data_json = dbcollection.find({"CorrelationId": correlationId, "HTId": HTId})
            dbcollection.update({'_id': data_json[0].get('_id')},
                                {'$set': {
                                    "modelName": modelName,
                                    "ProblemType": problemType,
                                    "Accuracy": accuracy * 100,
                                    "log_loss": log_loss,
                                    "TruePositive": int(TP),
                                    "TrueNegative": int(TN),
                                    "FalsePositive": int(FP),
                                    "FalseNegative": int(FN),
                                    "Sensitivity": sensitivity,
                                    "Specificity": specificity,
                                    "precision": precision,
                                    "recall": recall,
                                    "f1score": f1score,
                                    "c_error": c_error,
                                    "ar_score": ar_score,
                                    "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                    "visualization": visualization,
                                    "AUCImage": aucEncoded,
                                    "Clustering_Flag": clustering_flag,
                                    "featureImportance": featureImportance,
                                    "RunTime": timeSpend,
                                    "Version":version,
                                    "pageInfo": pageInfo,
                                    "CreatedByUser": userId
                                }})
            dbconn.close()

    except (ServerSelectionTimeoutError, NetworkTimeout):

        if counter > 0:
            counter = counter - 1
            insert_EvalMetrics_FI_C(correlationId, modelName, problemType, accuracy, log_loss, TP, TN, FP, FN,
                                    sensitivity, specificity, precision, recall, f1score, c_error, ar_score, aucEncoded,
                                    featureImportance, timeSpend, pageInfo, userId,visualization, modelparams=None, HTId=None,
                                    counter=counter)
        else:
            raise ServerSelectionTimeoutError



def insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, report, TP, TN, FP, FN,
                               ConfusionEncoded, accuracy_score, featureImportance, timeSpend, pageInfo, userId,visualization,
                               modelparams=None,version=None, HTId=None, counter=3, clustering_flag = True):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            # print (TP,TN,FP,FN)
            if version==0:
                dbcollection.insert_many([
                {
                "CorrelationId": correlationId,
                "ProblemType": pType,
                "Matthews_Coefficient": matthews_coefficient,
                "report": report,
                 "_id":Id,
                "modelName": modelName,
                "TruePositive": np.array(TP).tolist(),
                "TrueNegative": np.array(TN).tolist(),
                "FalsePositive": np.array(FP).tolist(),
                "FalseNegative": np.array(FN).tolist(),
                "ConfusionEncoded": ConfusionEncoded,
                "Accuracy": accuracy_score * 100,
                "featureImportance": featureImportance,
                "ModelParams": modelparams,
                "RunTime": timeSpend,
                "Clustering_Flag": clustering_flag,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "visualization": visualization,
                "pageInfo": pageInfo,
                "Version":version,
                "CreatedByUser": userId}])
            else:
                dbcollection.update_many({"CorrelationId": correlationId,"modelName": modelName,"Version":version},
                {"$set":{
                "CorrelationId": correlationId,
                "ProblemType": pType,
                 "_id":Id,
                "Matthews_Coefficient": matthews_coefficient,
                "report": report,
                "TruePositive": np.array(TP).tolist(),
                "TrueNegative": np.array(TN).tolist(),
                "FalsePositive": np.array(FP).tolist(),
                "FalseNegative": np.array(FN).tolist(),
                "ConfusionEncoded": ConfusionEncoded,
                "Accuracy": accuracy_score * 100,
                "featureImportance": featureImportance,
                "ModelParams": modelparams,
                "RunTime": timeSpend,
                "Clustering_Flag": clustering_flag,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "visualization": visualization,
                "pageInfo": pageInfo,
                "Version":version,
                "CreatedByUser": userId

            }},upsert=True)
            dbconn.close()
        else:
            dbconn, dbcollection = open_dbconn("ME_HyperTuneVersion")
            data_json = dbcollection.find({"CorrelationId": correlationId, "HTId": HTId})
            dbcollection.update({'_id': data_json[0].get('_id')},
                                {'$set': {
                                    "modelName": modelName,
                                    "ProblemType": pType,
                                    "Matthews_Coefficient": matthews_coefficient,
                                    "report": report,
                                    "TruePositive": np.array(TP).tolist(),
                                    "TrueNegative": np.array(TN).tolist(),
                                    "FalsePositive": np.array(FP).tolist(),
                                    "FalseNegative": np.array(FN).tolist(),
                                    "ConfusionEncoded": ConfusionEncoded,
                                    "Accuracy": accuracy_score * 100,
                                    "featureImportance": featureImportance,
                                    "ModelParams": modelparams,
                                    "RunTime": timeSpend,
                                    "Clustering_Flag": clustering_flag,
                                    "visualization": visualization,
                                    "pageInfo": pageInfo,
                                    "Version":version,
                                    "CreatedByUser": userId
                                }})

            dbconn.close()
    except (ServerSelectionTimeoutError, NetworkTimeout):
        #print("trying again")
        if counter > 0:
            counter = counter - 1
            insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, report, TP, TN, FP, FN,
                                       ConfusionEncoded, accuracy_score, featureImportance, timeSpend, pageInfo, userId,visualization,
                                       modelparams=None, HTId=None, counter=counter)
        else:
            raise ServerSelectionTimeoutError




def insert_Text_Classification_MultiClassMetrics_C(correlationId, modelName, pType, accuracy_score,
                                                   matthews_coefficient, timeSpend, pageInfo, userId, modelparams=None,
                                                   HTId=None, counter=3):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            # print (TP,TN,FP,FN)
            dbcollection.insert_one({
                "_id": Id,
                "CorrelationId": correlationId,
                "modelName": modelName,
                "ProblemType": pType,
                "Matthews_Coefficient": matthews_coefficient,
                "Accuracy": accuracy_score * 100,
                "ModelParams": modelparams,
                "RunTime": timeSpend,
                "ProblemTypeFlag": True,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "pageInfo": pageInfo,
                "CreatedByUser": userId

            })
            dbconn.close()
        else:
            dbconn, dbcollection = open_dbconn("ME_HyperTuneVersion")
            data_json = dbcollection.find({"CorrelationId": correlationId, "HTId": HTId})
            dbcollection.update({'_id': data_json[0].get('_id')},
                                {'$set': {
                                    "modelName": modelName,
                                    "ProblemType": pType,
                                    "Matthews_Coefficient": matthews_coefficient,
                                    "Accuracy": accuracy_score * 100,
                                    "ModelParams": modelparams,
                                    "ProblemTypeFlag": True,
                                    "RunTime": timeSpend,
                                    "pageInfo": pageInfo,
                                    "CreatedByUser": userId
                                }})

            dbconn.close()
    except (ServerSelectionTimeoutError, NetworkTimeout):
        #print("trying again")
        if counter > 0:
            counter = counter - 1
            insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, accuracy_score, timeSpend,
                                       pageInfo, userId, modelparams=None, HTId=None,version=None, counter=counter)
        else:
            raise ServerSelectionTimeoutError


def insert_EvalMetrics_FI_R(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                            featureImportance, timeSpend, pageInfo, userId,visualization, modelparams=None,version=None, HTId=None, counter=3,clustering_flag = True):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            r2ScoreVal["error_rate"] = float("%.2f" % round(r2ScoreVal["error_rate"] * 100, 2))
            if version==0:
                dbcollection.insert_many([{
                          
                "CorrelationId": correlationId,
                "modelName": modelName,
                "_id":Id,
                "ProblemType": problemType,
                "r2ScoreVal": r2ScoreVal,
                "rmsVal": rmsVal,
                "maeVal": maeVal,
                "mseVal": mseVal,
                "featureImportance": featureImportance,
                "RunTime": timeSpend,
                "ModelParams": modelparams,
                "Clustering_Flag": clustering_flag,
                "visualization": visualization,
                "pageInfo": pageInfo,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "Version":version,
                "CreatedByUser": userId
            }])
            else:
                dbcollection.update_many({"CorrelationId": correlationId,"modelName": modelName,"Version":version}, {"$set":{
                "CorrelationId": correlationId,
                "_id":Id,
                "modelName": modelName,
                "ProblemType": problemType,
                "r2ScoreVal": r2ScoreVal,
                "rmsVal": rmsVal,
                "maeVal": maeVal,
                "mseVal": mseVal,
                "featureImportance": featureImportance,
                "RunTime": timeSpend,
                "ModelParams": modelparams,
                "Clustering_Flag": clustering_flag,
                "visualization": visualization,
                "pageInfo": pageInfo,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "Version":version,
                "CreatedByUser": userId
            }},upsert=True)
            dbconn.close()
        else:
            dbconn, dbcollection = open_dbconn("ME_HyperTuneVersion")
            data_json = dbcollection.find({"CorrelationId": correlationId, "HTId": HTId})
            r2ScoreVal["error_rate"] = float("%.2f" % round(r2ScoreVal["error_rate"] * 100, 2))
            dbcollection.update({'_id': data_json[0].get('_id')},
                                {'$set': {
                                    "modelName": modelName,
                                    "ProblemType": problemType,
                                    "r2ScoreVal": r2ScoreVal,
                                    "rmsVal": rmsVal,
                                    "maeVal": maeVal,
                                    "mseVal": mseVal,
                                    "featureImportance": featureImportance,
                                    "RunTime": timeSpend,
                                    "pageInfo": pageInfo,
                                    "visualization": visualization,
                                    "Clustering_Flag": clustering_flag,
                                    "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                    "Version":version,
                                    "CreatedByUser": userId
                                }})
            dbconn.close()
    except (ServerSelectionTimeoutError, NetworkTimeout):

        if counter > 0:
            counter = counter - 1
            insert_EvalMetrics_FI_R(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                                    featureImportance, timeSpend, pageInfo, userId,visualization, modelparams=None, HTId=None,
                                    counter=counter)
        else:
            raise ServerSelectionTimeoutError


def insert_EvalMetrics_FI_T(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal, timeSpend,
                            actual, forecasted, RangeTime, selectedFreq, lastDataRecorded,xlabelname,ylabelname,legend, pageInfo, userId,version=None,
                            retrycount=3):
    try:
        dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
        Id = str(uuid.uuid4())
        r2ScoreVal["error_rate"] = float("%.2f" % round(r2ScoreVal["error_rate"] * 100, 2))
        dbcollection.insert_one({
            "_id": Id,
            "CorrelationId": correlationId,
            "modelName": modelName,
            "ProblemType": problemType,
            "r2ScoreVal": r2ScoreVal,
            "rmsVal": rmsVal,
            "maeVal": maeVal,
            "mseVal": mseVal,
            "RunTime": timeSpend,
            "Actual": actual,
            "Forecast": forecasted,
            "RangeTime": RangeTime,
            "Frequency": selectedFreq,
            "xlabelname":xlabelname,
            "ylabelname":ylabelname,
            "legend":legend,
            "Target":ylabelname,
            "pageInfo": pageInfo,
            "Version":version,
            "LastDataRecorded": lastDataRecorded,
            "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
            "CreatedByUser": userId
        })
        dbconn.close()
    except ServerSelectionTimeoutError:
        if retrycount > 0:
            retrycount -= 1
            insert_EvalMetrics_FI_T(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                                    timeSpend, actual, forecasted, RangeTime, selectedFreq,lastDataRecorded,xlabelname,ylabelname,legend, pageInfo, userId,version=None,
                                    retrycount=retrycount)
        else:
            raise ServerSelectionTimeoutError
def insInputSample(correlationId, data):
    EnDeRequired = getEncryptionFlag(correlationId)
    dataToDict = str(data.to_json(orient='records', date_format='iso', date_unit='s'))
    dbconn, dbcollection = open_dbconn('SSAI_DeployedModels')
    data = dbcollection.find({"CorrelationId": correlationId})
    if EnDeRequired :
            dataToDict = EncryptData.EncryptIt(dataToDict)
    dbcollection.update({'_id': data[0].get('_id')}, {'$set': {
        "InputSample": dataToDict,
    }})
    dbconn.close()
    
	
	
def save_file(file, file_name, problemType, correlationId, pageInfo, userId, train_cols=None, FileType=None, HTId=None,version=None,
              retrycount=3):
    
    
    if version==None:
        version=None
        file_name=file_name
    elif version==0:
        version=0
        file_name=file_name
    else:
        version=version
        file_name=file_name+'_'+str(version)
        
    
    file_path = saveModelPath + str(correlationId) + '_' + str(file_name) + '.pickle'
#    Ofile_path = open(file_path, 'wb')
    if problemType != "TimeSeries":
         encryptPickle(file,file_path)
#        pickle.dump(file, Ofile_path)
         cfg = None
    else:
#        pickle.dump(file[0], Ofile_path)
         encryptPickle(file[0], file_path)
         cfg = file[1]
#    Ofile_path.close()
#...................................................................................

    dbconn, dbcollection = open_dbconn('SSAI_savedModels')
    try:
        Id = str(uuid.uuid4())
        dbcollection.update_many({"CorrelationId" : correlationId,"FileName": file_name,"Version":version},
                                { "$set":								
        #dbcollection.insert({		"_id":Id,
									   {"CorrelationId": correlationId,
                                       "HTId": HTId,
                                       "FileName": file_name,
                                       "FilePath": file_path,
                                       "FileType": FileType,
                                       "Configuration": cfg,
                                       "TrainCols": train_cols,
                                       "inputSample": "undefined123",
                                       "pageInfo": pageInfo,
                                       "ProblemType": problemType,
                                       "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                       "Version":version,
                                       "CreatedByUser": userId}},upsert=True,
                                       )
    except ServerSelectionTimeoutError:
        if retrycount > 0:
            retrycount -= 1
            save_file(file, file_name, problemType, correlationId, pageInfo, userId, train_cols=train_cols,
                      FileType=FileType, retrycount=retrycount)
        else:
            raise ServerSelectionTimeoutError

    dbconn.close()




def save_file_t(file, file_name, correlationId, pageInfo, userId, problemType=None, FileType=None):
    
    
    
    file_path = saveModelPath + str(correlationId) + '_' + str(file_name) + '.pickle'
#    Ofile_path = open(file_path, 'wb')
#    pickle.dump(file, Ofile_path)
    encryptPickle(file,file_path)
#    Ofile_path.close()
#...................................................................................
    
    
    
    
    
    
    dbconn, dbcollection = open_dbconn('SSAI_savedModels')
    Id = str(uuid.uuid4())
    dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               "FileName": file_name,
                               "FilePath": file_path,
                               "FileType": FileType,
                               "pageInfo": pageInfo,
                               "ProblemType": problemType,
                               "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                               "CreatedByUser": userId}])
    dbconn.close()







def get_pickle_file(correlationId=None, FileName=None, FileType=None, FilePath=None,version=None):
    if correlationId != None and FileType != None and FileName == None:
        dbconn, dbcollection = open_dbconn('SSAI_savedModels')
        data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileType": FileType})
        dbconn.close()
        file_name = data[0].get('FileName')
        ProblemType = data[0].get('ProblemType')
        file_path = data[0].get('FilePath')
        #print(file_path)
        traincols = None
        
#...................................................................................
        
        
        loaded_model = decryptPickle(file_path)
#        file = open(file_path, 'rb')
#        loaded_model = pickle.load(file)
        
        
    if correlationId != None and FileType != None and FileName != None:
        dbconn, dbcollection = open_dbconn('SSAI_savedModels')
        #print(correlationId, FileName, FileType)
        
        if version==None or version==0:
                data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileName": FileName,
                                  "FileType": FileType})
        elif version!=0 or version!=None:
                data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileName": FileName+'_'+str(version),
                                  "FileType": FileType})
                
        dbconn.close()

        file_name = data[0].get('FileName')
        ProblemType = data[0].get('ProblemType')
        traincols = data[0].get('TrainCols')
        file_path = data[0].get('FilePath')
#...................................................................................
        
        
#        file = open(file_path, 'rb')
#        loaded_model = pickle.load(file)
        loaded_model = decryptPickle(file_path)
        
        
        
        
    elif (correlationId == None and FileType == None) and FilePath != None:
        file_name = None
        ProblemType = None
        traincols = None
        
        
#...................................................................................
#        loaded_model = pickle.load(file)
        #        file = open(FilePath, 'rb')

        loaded_model = decryptPickle(FilePath)

        
    return loaded_model, file_name, ProblemType, traincols



num_type_int = ['int64', 'int', 'int32']


def getUniqueValues(df):
    UniqueValuesCol = {}
    typeDict = {}
    for col in df:
        typeDict[col] = df.dtypes[col].name
        if df[col].dtype.name == 'datetime64[ns]':
            UniqueValuesCol[col] = list(df[col].dropna().unique().astype(str,copy=False))
            for x,y in enumerate(UniqueValuesCol[col]):
                if isinstance(y,np.str_):
                    UniqueValuesCol[col][x]=str(y)
        elif df[col].dtype.name in num_type_int:
            UniqueValuesCol[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())]
        else:
            UniqueValuesCol[col] = list(df[col].dropna().unique())
            for x,y in enumerate(UniqueValuesCol[col]):
                if isinstance(y,np.bool_):
                    UniqueValuesCol[col][x]=bool(y)
    return UniqueValuesCol, typeDict



def get_OGData(data, encoders):
    data_t = data
    for key, value in encoders.items():
        if key == 'OHE':
            for key1, value1 in value.items():
                encoder = key1
                columns = value1.get('EncCols')
                og_cols = value1.get('OGCols')
            dec_val = encoder.inverse_transform([data_t.get(columns)])
            data_t = data_t.append(pd.Series(dec_val[0], index=og_cols))
            data_t = data_t.drop(columns)
        elif key == 'LE':

            for key1, value1 in value.items():
                encoder = key1
                columns = value1
            for indx in range(len(columns)):
                # if data_t is series
                dec_val = encoder.inverse_transform([int(data_t.get(columns[indx]))])
                data_t = data_t.append(pd.Series(dec_val, index=[columns[indx][:len(columns[indx]) - 2]]))
                data_t = data_t.drop(columns[indx])
                # if data_t is dictionary
    #                dec_val=lencm.inverse_transform([data_t.get(columns[indx])])
    #                data_t.update({columns[indx][:len(columns[indx])-2]:dec_val[0]})
    #                data_t.pop(columns[0])
    return data_t


def getTimeSeriesParams(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data = dbcollection.find({"CorrelationId": correlationId})
    try:
        if not data[0]["TimeSeries"] or isinstance(data[0]["TimeSeries"],str):
            return None, None, None
    except KeyError:
        return None, None, None
    f = {}
    value = 0    
    fdict = {int(k): v for k, v in data[0]["TimeSeries"]["Frequency"].items()}
    for each in fdict:
        if fdict[each]["Steps"]:
            f[fdict[each]["Name"]] = fdict[each]["Steps"]
            if fdict[each]["Name"]=="CustomDays":
               value = fdict[each]["value"]            
    agg = data[0]["TimeSeries"]["Aggregation"]
    return f, agg, value


def getDateCol(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data = dbcollection.find({"CorrelationId": correlationId})

    try:
        if data[0].get("TemplateUsecaseID",None) in ["2dcf1c54-099b-4711-b9a2-f08ad96d71b7","64a6c5be-0ecb-474e-b970-06c960d6f7b7","877f712c-7cdc-435a-acc8-8fea3d26cc18","6b0d8eb3-0918-4818-9655-6ca81a4ebf30","5cab6ea1-8af4-4f74-8359-e053629d2b98","5b7667e6-b48a-4bc4-8082-587f407df5b5"] or data[0].get("ParentCorrelationId",None) in ["b54f4da6-ebcb-4b03-8189-8f6bad701ff3"]:
            return "_"
        elif not isinstance(data[0]['TimeSeries'],str):	
            return data[0]['TimeSeries']['TimeSeriesColumn']  # Inconsistent tab error
        else:
            return data[0]['TargetUniqueIdentifier']
    except KeyError:
        return 'DateColumn'


def setRetrain(correlationId, val):
    dbconn, dbcollection = open_dbconn('ME_RecommendedModels')
    dbcollection.update_one({"CorrelationId": correlationId}, {"$set": {"retrain": val}})
    dbconn.close()


def fetchProblemType(correlationId):
    dbconn, dbcollection = open_dbconn('ME_RecommendedModels')
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    problemtype = data_json[0].get('ProblemType')

    dbconn, dbcollection = open_dbconn('ME_FeatureSelection')
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    UniqueTarget = data_json[0].get('UniqueTarget')

    return [problemtype, UniqueTarget]


def getuniqueval(correlationId):
    dbconn, dbcollection = open_dbconn('ME_FeatureSelection')
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    target_var = data_json[0].get('UniqueTarget')
    return target_var


def decode_target_vals(En_Data, correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    target_variable = data_json[0].get('TargetColumn')

    encoders = {}

    # Fetch data to be encoded from data processing table
    dbproconn, dbprocollection = open_dbconn("DE_DataProcessing")
    data_json = dbprocollection.find({"CorrelationId": correlationId})
    dbproconn.close()

    Data_to_Encode = data_json[0].get('DataEncoding')
    En_Data_cols = list(En_Data.columns)

    if len(Data_to_Encode) > 0:
        OHEcols = []
        LEcols = []

        for keys, values in Data_to_Encode.items():
            if values.get('encoding') == 'One Hot Encoding' and (
                    values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (
                    keys != 'ChangeRequest' and keys != 'PChangeRequest'):
                OHEcols.append(keys)
            elif values.get('encoding') == 'Label Encoding' and (
                    values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (
                    keys != 'ChangeRequest' and keys != 'PChangeRequest'):
                LEcols.append(keys)
        # OHE
        # Fetch Pickle file and encoded columns
        if len(OHEcols) > 0:
            ohem, _, enc_cols, _ = get_pickle_file(correlationId, FileType='OHE')
            encoders = {'OHE': {ohem: {'EncCols': enc_cols, 'OGCols': OHEcols}}}

        if len(LEcols) > 0:
            lencm, _, Lenc_cols, _ = get_pickle_file(correlationId, FileType='LE')
            # encoders.update({'LE':{lencm:Lenc_cols}})
            diff = [value for value in Lenc_cols if value in En_Data_cols]
            encoders.update({'LE': {lencm: diff}})

        OGData = get_OGDataFrame(En_Data, encoders)
    else:
        OGData = En_Data

    target_unique_vals = list(OGData[target_variable].unique())

    return target_unique_vals

def decode_only_target_vals(En_Data, correlationId,unique=None):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    target_variable = data_json[0].get('TargetColumn')
    target_variable = target_variable+"_L"
    
    # Fetch data to be encoded from data processing table
    
    lencm, _, Lenc_cols, _ = get_pickle_file(correlationId, FileType='LE')
    

    
    if unique:
        target_unique_vals = list(np.unique(list(lencm.inverse_transform(En_Data[target_variable]))))
    else:
        target_unique_vals = list(lencm.inverse_transform(En_Data[target_variable]))

    return target_unique_vals

def formatfloat(x):
    return "%.3g" % float(x)


def pformat(dictionary, function):
    if isinstance(dictionary, dict):
        return type(dictionary)((key, pformat(value, function)) for key, value in dictionary.items())
    if isinstance(dictionary, collections.Container):
        return type(dictionary)(pformat(value, function) for value in dictionary)
    if isinstance(dictionary, numbers.Number):
        return function(dictionary)
    return dictionary


'''    
def testdbconn():
    dbconn,dbcollection = open_dbconn('DE_PreProcessedData')

    data = dbcollection.find({'CorrelationId':'7d123792-7aa2-489a-a0f3-4b94a5bcb322'})
    df = pd.DataFrame(data)
    df.to_csv('DE_PreprocessedData.csv')
    dbconn.close()
testdbconn()
'''


def updateErrorInTable(error, correlationId, uniqueId):
    dbconn, dbcollection = open_dbconn('SSAI_PublishModel')
    dbcollection.update_many({'CorrelationId': correlationId, 'UniqueId': uniqueId}, {'$set': {
        'Status': 'E', 'ErrorMessage': str(error)
    }})
    dbconn.close()


def getlastDataPoint(correlationId):
    dbconn, dbcollection = open_dbconn('PS_IngestedData')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    return (datetime.strptime(data["lastDateDict"]["vds_InsatML"], '%Y-%m-%d %H:%M:%S')+timedelta(hours=1)).strftime('%d-%m-%Y %H:%M:%S')


def append_data_chunks(InstaData, collection, correlationId, pageInfo, userId, columns, sourceDetails, colunivals=None,
                       lastDateDict=None,previousLastDate = None):
    freq, _,_ = getTimeSeriesParams(correlationId)
    df = data_from_chunks(corid=correlationId, collection="PS_IngestedData")
    df = df.append(InstaData, ignore_index=True)
    df = UsecaseDefinitionDetails(df,correlationId,userId)
    columns = list(df.columns)
    # print (df)
    # print (InstaData)
    DateCol = getDateCol(correlationId)
    if DateCol == "_":
        if 'Custom' in lastDateDict:
            DateCol = list(lastDateDict['Custom'].keys())[0]
        else:
            DateCol = list(lastDateDict['Entity'].keys())[0]
    df.set_index(df[DateCol], drop=True, inplace=True)
    try:
        df.index = pd.to_datetime(df.index)
    except ValueError:
        df.index = pd.to_datetime(df.index,utc=True)
    df.sort_index(inplace=True)
    df.drop_duplicates(keep="last", inplace=True)
    # print (freq)
    if not freq:
        df = df.last("2Y")
    elif list(freq.keys())[0] in ["Hourly", "Daily", "Weekly", "Monthly",
                                  "Fortnightly","CustomDays"]:  # keep only 2 years data in ingestData
        df = df.last("2Y")
    # print (df)
    chunks, size = file_split(df)
    dbconn, dbcollection = open_dbconn("PS_IngestedData")
    dbcollection.remove({"CorrelationId": correlationId})
    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,Incremental=False,requestId=None,sourceDetails=sourceDetails,
                     colunivals=colunivals, lastDateDict=lastDateDict,previousLastDate = previousLastDate)
    return size


def getlastDataPointCDM(correlationId, *argv):
    dbconn, dbcollection = open_dbconn('PS_IngestedData')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    lastDate = data["lastDateDict"]
    for arg in argv:
        lastDate = lastDate[arg]
    return (datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')#lastDate.strftime('%m/%d/%Y')

def getlastDataPointagile(correlationId):
    dbconn, dbcollection = open_dbconn('PS_IngestedData')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    try:
        lastDate = data["lastDateDict"]['Entity'].values()
        for arg in lastDate:
            last_Date = arg
    except:
        dbconn, dbcollection = open_dbconn('SSAI_RecommendedTrainedModels')
        data = dbcollection.find_one({"CorrelationId": correlationId})
        last_Date = data["LastTrainDateTime"]
    return (datetime.strptime(last_Date, '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')#lastDate.strftime('%m/%d/%Y')

def save_vectorizer(correlationId, name, vectorizer):
    file_path = saveModelPath + '_' + correlationId + '_' + name + '.pkl'
    encryptPickle(vectorizer,file_path)
#    pickle.dump(vectorizer, open(saveModelPath + '_' + correlationId + '_' + name + '.pkl', 'wb'))


def load_vectorizer(correlationId, name):
    file_path = saveModelPath + '_' + correlationId + '_' + name + '.pkl'
    vectorizer = decryptPickle(file_path)
#    vectorizer = pickle.load(open(saveModelPath + '_' + correlationId + '_' + name + '.pkl', "rb"))
    return vectorizer


'''CLUSTERING CHANGES START'''


def store_cluster_dictionary(cluster, correlationId):
    tempFile = saveModelPath + "cluster" + "_" + correlationId + ".pkl"
#    output = open(tempFile, 'wb')
#    pickle.dump(cluster, output)
    encryptPickle(cluster,tempFile)
#    output.close()


def load_cluster_dictionary(correlationId):
    tempFile = saveModelPath + "cluster" + "_" + correlationId + ".pkl"
#    pkl_file = open(tempFile, 'rb')
#    mydict2 = pickle.load(pkl_file)
    mydict2 = decryptPickle(tempFile)
#    pkl_file.close()
    return mydict2


'''CLUSTERING CHANGES END'''


def getVDSAzureToken():
    TokenURL = config['vdsAzureTokenDetails']['tokenURL']
    headers = {
        "Content-Type": "application/x-www-form-urlencoded"
    }
    payload = {
        "Scope": config['vdsAzureTokenDetails']['scope'],
        "grant_type": config['vdsAzureTokenDetails']['grant_type'],
        "resource": config['vdsAzureTokenDetails']['resource'],
        "client_id": config['vdsAzureTokenDetails']['client_id'],
        "client_secret": config['vdsAzureTokenDetails']['client_secret']
    }
    try:
        r = requests.post(TokenURL, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None
    return token, r.status_code


def getMetricAzureToken():
    TokenURL = config['METRIC']['AdTokenUrl']
    headers = {
        "Content-Type": "application/x-www-form-urlencoded"
    }
    payload = {
        "scope": config['METRIC']['scope'],
        "grant_type": config['METRIC']['grant_type'],
        "resource": config['METRIC']['resource'],
        "client_id": config['METRIC']['client_id'],
        "client_secret": config['METRIC']['client_secret'],
#        "username": config['METRIC']['username'],
#        "password": config['METRIC']['password']
    }
    try:
        r = requests.post(TokenURL, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None
    return token, r.status_code


def formVDSAuth():
    try:
        # r = requests.post(config['PAD']['tokenAPIUrl'], headers={'Content-Type': 'application/json'},
        #                  data=json.dumps({"username": config['PAD']['username'],
        #                                   "password": encryption.decrypt(config['PAD']['password'])}))
        username = config['PAD']['username']
        password = config['PAD']['password']
        data_body = {"username":username,
		"password":password
		}
        r = requests.post(config['PAD']['tokenAPIUrl'], data = json.dumps(data_body),headers={'Content-Type': "application/json"})
        token = r.json()['token']
    except:
        token = None
    return token, r.status_code


def CustomAuth(AppId):
    EnDeRequired = True
    
    dbconn, dbcollection = open_dbconn('AppIntegration')
    data = dbcollection.find_one({"ApplicationID": AppId})
    # key=['username','Scope']
    creds = data.get('Credentials')
    # for x in key:
    #    del creds[x]
    v=data.get('TokenGenerationURL')
    if EnDeRequired:
        x = base64.b64decode(creds) #En.............................
        creds = eval(EncryptData.DescryptIt(x))

        x = base64.b64decode(v) #En.............................
        v = EncryptData.DescryptIt(x)
        #print(creds)
        #print(v)
    if data.get('Authentication') == 'AzureAD'  or data.get('Authentication') == 'Azure':

        r = requests.post(v, headers={'Content-Type': "application/x-www-form-urlencoded"},
                          data=creds)
        if r.status_code == 200:
            token = r.json()['access_token']
            return token, r.status_code
        else:
            return False, r.status_code
    elif data.get('Authentication') == 'Form' or data.get('Authentication') == 'Forms':
        url =  config['PAD']['tokenAPIUrl']
        username = config['PAD']['username']
        password = config['PAD']['password']
        data_body = {"username":username,
		"password":password
		}
        r = requests.post(url, headers={'Content-Type': "application/json"},data = json.dumps(data_body))
        if r.status_code == 200:
            token = r.json()['token']
            return token, r.status_code

        else:
            return False, r.status_code

def request_api(Url,headers,jsonObj, HttpMethod):
    try:
        auth_type = get_auth_type()
        #print (auth_type)
        if HttpMethod.strip().lower() == "post":
            if auth_type == "WindowsAuthProvider":
                #print("inside windows auth")
                init_res = requests.post(Url,headers=headers,json=jsonObj,allow_redirects=False,auth=HttpNegotiateAuth())
            else:
                init_res = requests.post(Url,headers=headers,json=jsonObj,allow_redirects=False)
        elif HttpMethod.strip().lower() == "get":
            init_res = requests.get(Url,headers=headers,allow_redirects=False)
        #print(init_res.status_code)
       # print(init_res.json())
        #log.add_log(logger, 'INFO', "status code ="+str(init_res.status_code)+" for "+ HttpMethod + " call with Url= " + Url)
        if init_res.status_code == 200:
            return True,init_res.json()
        elif init_res.status_code == 500:
            #log.add_log(logger, 'INFO', "URL throwing 500 error, please call the url in postman. url="+Url)
            #log.add_log(logger, 'INFO', "URL throwing 500 error with header="+str(headers))
            #print("URL throwing 500 error, please call the url in postman. body="+str(jsonObj))
            #print(str(Url))
            #print(str(headers))
            #print(str(jsonObj))
            print(str(HttpMethod))
            return False,{}
        elif init_res.status_code == 401:
            #log.add_log(logger, 'INFO', "Unauthorized to use Url="+Url)
            #print(str(Url))
            #print(str(headers))
            #print(str(jsonObj))
            print(str(HttpMethod))            
            return False,{}
        else:
            #print(str(Url))
            #print(str(headers))
            #print(str(jsonObj))
            print(str(HttpMethod))
            return False, {}
    except Exception as ce:
        return False,ce

def call_api(Url, i, BatchSize, correlationId, token, UserEmailId, AppServiceUId, HttpMethod, key):
    try:
        if HttpMethod.strip().lower() == "get":
            base_url = ''.join(Url.split())[:-1]
            Url = base_url + str(i)
        #print("fetching page number ", i)
        jsonObj = {
        "CorrelationId": correlationId,
        "BatchSize": BatchSize,
        "PageNumber": str(i),
        }
        if token != 'WindowsAuthProvider':
            headers = {
            	'authorization': "Bearer " + token,
                'AppServiceUId': AppServiceUId,
        		'UserEmailId': UserEmailId,
                'Connection': 'Close'
                }
        elif token == 'WindowsAuthProvider':
            headers = {
                'AppServiceUId': AppServiceUId,
                'UserEmailId': UserEmailId,
                'Connection': 'Close'
                }
        print (Url,headers,jsonObj)
					   
        request_status,request_output = request_api(Url,headers,jsonObj, HttpMethod) 
        #print("end page number", i)
        if request_status:
            if key in request_output.keys():
                key_data = request_output.get(key)
                return list(key_data)
            else:
                print(key + " not found in the response.")
        else:
            #print('status is false')
            #print("page number ",i)
            print(request_output)
            return []
    except Exception as e:
        print(e)
        #log.add_log(logger, 'INFO', "Error in fetching data. Please try calling the serviceurl in postman. Exception raised in the call_api function")
        #log.add_log(logger, 'INFO', "URL = " + Url)
        #log.add_log(logger, 'ERROR', 'Trace')

def getVDSAIOPSAzureToken(AppName="VDS(AIOPS)"):
    EnDeRequired = True
    dbconn, dbcollection = open_dbconn('AppIntegration')
    data = dbcollection.find_one({"ApplicationName": AppName})
    dbconn.close()	
    # key=['username','Scope']
    creds = data.get('Credentials')
    # for x in key:
    #    del creds[x]
    v=data.get('TokenGenerationURL')
    if EnDeRequired:
        x = base64.b64decode(creds) #En.............................
        creds = eval(EncryptData.DescryptIt(x))

        x = base64.b64decode(v) #En.............................
        v = EncryptData.DescryptIt(x)
    if data.get('Authentication') == 'Azure' or data.get('Authentication') == 'AzureAD':
        r = requests.post(v, headers={'Content-Type': "application/x-www-form-urlencoded"},
                          data=creds)
        if r.status_code == 200:
            token = r.json()['access_token']
    return token, r.status_code

def getUniqueIdentifier(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")						 
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    try:
        uid_list = data_json[0].get("delta_uids")
        UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
    except:
        return None,None
    return UniqueIdentifir,uid_list

def getColumnnames(correlationId):
    try:
        dbproconn,dbprocollection = open_dbconn("PS_IngestedData")
        data_json = list(dbprocollection.find({"CorrelationId" :correlationId})) 
        dbproconn.close()
        columns_list = data_json[0]['ColumnsList']
    except:
        columns_list = []
    return columns_list



def CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental=False):
    auth_type = get_auth_type()
    #print("inside callcdmspi")
    if auth_type == 'AzureAD':

        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
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
                maxdata = maxdatapull(correlationId)
                nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
                x = 1
                if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                    actualfromdate = entityArgs['FromDate']
                    if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                        entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                        if auth_type == 'AzureAD':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                      'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                        elif auth_type == 'WindowsAuthProvider':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())                    
                        while maxdata > resp.json()['TotalRecordCount']:
                            x = x + 1
                            if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                                entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                                if auth_type == 'AzureAD':
                                    resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                          'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                elif auth_type == 'WindowsAuthProvider':
                                    resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())                         
                entityDataframe = pd.DataFrame({})
                numberOfPages = resp.json()['TotalPageCount']
                TotalRecordCount = resp.json()['TotalRecordCount']
                BatchSize = resp.json()['BatchSize']

                #number_of_Pages = math.ceil(maxdata/BatchSize)
                #if number_of_Pages != 0:
                #    if numberOfPages > number_of_Pages:
                #        numberOfPages =number_of_Pages
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
                            
                    if entityresults.status_code == 200:
                        entityData = entityresults.json()['Entity']
                        if entityData != []:
                            df = pd.DataFrame(entityData)
                        entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                    i = i + 1 
                if entityDataframe.empty:
                    if auto_retrain:
                        entityDfs[entity] = "No incremental data available"
                    else:
                        entityDfs[entity] = "Data is not available for your selection"
                elif entityDataframe.shape[0] <= min_data() and not auto_retrain and not Incremental:                    				
                    entityDfs[entity] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
                else:
                    entityDataframe["modifiedon"] = pd.to_datetime(entityDataframe["modifiedon"], format="%Y-%m-%d %H:%M", exact=True)
                    lastDateDict['Entity'][entity] = pd.to_datetime(entityDataframe["modifiedon"].max()).strftime('%Y-%m-%d %H:%M:%S')
                    entityDataframe.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
                    entityDfs[entity] = entityDataframe
                    #######itemexternalid change############
                    if 'workitemassociations' in entityDfs[entity].columns:
                        assocColumn = 'workitemassociations'
                    elif (entity != "CodeBranch" and entity != "Task"):
                        assocColumn = entity.lower()+"associations"
                    else:
                        if entity == "Task":
                            assocColumn = "deliverytaskassociations"
                    #print("assocColumn",assocColumn)
                    #if assocColumn != "codebranchassociations":
                    if entity != "CodeBranch": 
                        col = entityDfs[entity][assocColumn]
                        entityDfs[entity]['itemexternalid'] = pd.Series(data = "",index =  entityDfs[entity].index)
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
    #print("CallCdmAPI is done")
    return



def IterationAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken):
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
            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
    #print("sdsdgsgsg",auto_retrain)
        #print(resp.status_code)
        if resp.status_code == 200:
            api_error_flag = False
            if resp.text != "No data available":
                if resp.json()['TotalRecordCount'] != 0:
                    maxdata = maxdatapull(correlationId)
                    nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
                    x = 1
                    if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                        actualfromdate = entityArgs['FromDate']
                        if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                            entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                            if auth_type == 'AzureAD':
                                resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',    'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                            elif auth_type == 'WindowsAuthProvider':
                                resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                            while maxdata > resp.json()['TotalRecordCount']:
                                x = x + 1
                                if (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                                    entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                                    if auth_type == 'AzureAD':
                                        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',   'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                    elif auth_type == 'WindowsAuthProvider':
                                        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']}, auth=HttpNegotiateAuth())
                            
                    entityDataframe = pd.DataFrame({})
                    numberOfPages = resp.json()['TotalPageCount']
                    TotalRecordCount = resp.json()['TotalRecordCount']

                    i = 0   
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


def DoAd_AgileTransfrom(df,EntityConfig,m,ids,RequiredColumns,flag=None):
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
        #print("code has to be written for agileusecase")
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
        #print("here")
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
         #print("key_value.keys()",key_value.keys())
         NvList = ['PhaseDetected', 'PhaseInjected', 'Priority', 'Reference', 'Severity', 'State','Type']
         if key_value != False:
             for col in NvList: 
                 if col in key_value.keys(): 
                     k = key_value[col]
                     c = cdm_value[col] 
                     col = col.lower()+"uid"
                     if len(k) !=0 and col in  df.columns:
                         #print(col)
#                         df[col] = df[col].replace('',np.nan)
#                         df[col] = df[col].fillna(df.priorityuid.mode()[0]).map(lambda a:  str(uuid.UUID(a)))
                         df[col+"CnvId"] = df[col]
                         df[col+"CnvName"] = df[col]
                         df[col+"CnvName"] =  df[col+"CnvName"].astype(str).replace(k)
                         df[col] = df[col].astype(str).replace(c)
    return df

def TransformEntities(MultiSourceDfs,cid,dcuid,EntityMappingColumns,parent,Incremental,auto_retrain,flag = None,correlationId = None):							
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
            #print("COLUMNS:::::",v.columns)
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
                
                if Incremental:
                    if file_name.lower()+'_prediction' in EntityConfig['WorkItem']:
                        if (len(MultiSourceDfs['Entity'].keys()) > 1 and file_name.lower()==parent['Name'].split('.')[0].lower()) or len(MultiSourceDfs['Entity'].keys()) == 1:
                            required = eval(EntityConfig['WorkItem'][file_name.lower()+'_prediction'])
                            try:
                                v.rename(columns={'stateuid':'State'}, inplace=True)
                                v = v[v['State'].isin(required)]
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                

            else:
               # print("k" , k)
                #print("M:::::::::::::",m)
                

                if k != 'Environment' and k != "CodeBranch" and k != "Observation":
                    if 'createdbyproductinstanceuid' in v.columns:
                        v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
                    #print("m::::",m)
                    v["UniqueRowID"] = v[m[0]] + v[m[1]]
                    for col in m:                  
                        v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
                else:
                    v["UniqueRowID"] = v[m[0]]
                v.columns = v.columns.str.replace('[.]', '')
                try:
                    df = v.copy()
                    v = transformCNV(v,cid,dcuid,k,WorkItem = False)
                except:
                    v = df.copy()
                #print("before incrmental block", v.columns)
                #print("column values",v["stateexternalid"])
                if Incremental:
                    if file_name.lower() in EntityConfig['prediction']:
                        if (len(MultiSourceDfs['Entity'].keys()) > 1 and file_name.lower()==parent['Name'].split('.')[0].lower()) or len(MultiSourceDfs['Entity'].keys()) == 1:
                            required = eval(EntityConfig['prediction'][file_name.lower()])
                            #print(v['stateuid'].isin(required))
                            try:
                                #print("Shape Before filter", v.to_csv("file.csv"))
                                v = v[v['stateuid'].isin(required)]
                                #print("Shape After filter", v.shape)
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                #print("after incrmental block", v.columns)
                #print("column values",v["stateexternalid"])
               # print("Number of null values in stateexternalid",v["stateexternalid"].isnull().sum(axis = 0))
                #print("Number of empty strings in stateexternalid",(v["stateexternalid"].values == '').sum())
            if Incremental:
                dbproconn, dbprocollection = open_dbconn("SSAI_DeployedModels")
                data_json = dbprocollection.find({"CorrelationId": correlationId})
                print("datainc")
                ModelType = data_json[0].get('ModelType')
                dbproconn.close()
                if ModelType == 'TimeSeries':
                    min_df = -1
                else:                       
                    min_df = 0
            if v.empty:
                if auto_retrain:
                    v="No Incremental Data Available"
                else:
                    v = "Data is not available"
                MultiSourceDfs['Entity'][k] = v 
            elif v.shape[0] <= min_df and not auto_retrain:
                v = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                MultiSourceDfs['Entity'][k] = v
            else:
                if flag == None:
                    if type(v) != str:
                        Rename_cols = dict(ChainMap(*list(map(lambda x: {x: x + "_" + file_name}, list(v.columns.astype(str))))))
                        if parent['Name'] == 'null' or  parent['Name'][:-7] == k:
                            if 'DateColumn' in Rename_cols.keys():
                                Rename_cols['DateColumn'] = 'DateColumn'
                        v.rename(columns=Rename_cols, inplace=True)
                        if not Incremental:
                            empty_cols = [col for col in v.columns if v[col].dropna().empty]
                            v.drop(columns = empty_cols ,inplace = True)
                        v = v.replace('',np.nan)
                        v = v.replace("",np.nan)
                        if k.endswith(".csv") or k.endswith(".xlsx"):
                            k = os.path.basename(k).split('.')[0][36:]
                            if k[0] == "_":
                                k = k.split("_")[1]
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m)))
                            #print("data******")
                        else:
                            #print("vcolumns",v.columns)
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m)))
                            #print("dataisthere*******")
                else:
                        if not Incremental:
                            empty_cols = [col for col in v.columns if v[col].dropna().empty]
                            v.drop(columns = empty_cols ,inplace = True)
                        v = v.replace('',np.nan)
                        v = v.replace("",np.nan)
                        MultiSourceDfs['Entity'][k] = v
                        #print("datathere*********")
    return MultiSourceDfs, MappingColumns

def MetricFunction(metricArgs,metricDfs,metricsUrl,metricAccessToken,correlationId):
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':
        metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                  headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(metricAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
        metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                  headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                  auth=HttpNegotiateAuth())
    if metricResults.status_code != 200:
        e = "Phoenix API is returned " + str(metricResults.status_code) + " code, Error Occured while calling Phoenix API"
        metricDfs['Metric'] = e  
        meticData = pd.DataFrame()
    else:
        metricData = metricResults.json()
        if  metricData['Client'] == []:
            metricData = pd.DataFrame()
            #print("no data")
            metricDfs['Metric'] = "Data is not available for your selection"  # Error case 2
            meticData = pd.DataFrame()
        else:
            dfs = []
            for item in metricData['Client']:
                df = pd.DataFrame(item['Items'])
                dfs.append(df)
            meticData = pd.concat(dfs)
            maxdata = maxdatapull(correlationId)
            nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
            x = 1
            if maxdata < metricResults.json()['TotalRecordCount'] and nonprod:
                actualfromdate = metricArgs['FromDate']
                if (datetime.strptime(str(metricArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                    metricArgs['FromDate'] = (datetime.strptime(str(metricArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                    if auth_type == 'AzureAD':
                        metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                                  headers={'Content-Type'  : 'application/json',
                                                  'authorization' : 'bearer {}'.format(metricAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                                  headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                  auth=HttpNegotiateAuth())
                    while maxdata > metricResults.json()['TotalRecordCount']:
                        x = x + 1
                        if (datetime.strptime(str(metricArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y') > actualfromdate:
                            metricArgs['FromDate'] = (datetime.strptime(str(metricArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                            if auth_type == 'AzureAD':
                                metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                                          headers={'Content-Type'  : 'application/json',
                                                          'authorization' : 'bearer {}'.format(metricAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                            elif auth_type == 'WindowsAuthProvider':
                                metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                                          headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                          auth=HttpNegotiateAuth())
                        
            numberOfPages = metricResults.json()['TotalPageCount']
            TotalRecordCount = metricResults.json()['TotalRecordCount']
            i = 1
            while i < numberOfPages:
                metricArgs.update({"PageNumber": i + 1})
                metricArgs.update({"TotalRecordCount": TotalRecordCount})
                if auth_type == 'AzureAD':
                     metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                              headers={'Content-Type'  : 'application/json',
                                              'authorization' : 'bearer {}'.format(metricAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                elif auth_type == 'WindowsAuthProvider':
                     metricResults = requests.post(metricsUrl, data=json.dumps(metricArgs),
                                              headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                              auth=HttpNegotiateAuth())                            
                if metricResults.status_code == 200:
                    metricData = metricResults.json()
                    if metricData['Client'] != []:
                        dfs = []
                        for item in metricData['Client']:
                            df = pd.DataFrame(item['Items'])
                            dfs.append(df)
                        meticData2 = pd.concat(dfs)
                        meticData = pd.concat([meticData,meticData2]).reset_index(drop=True)
                i = i + 1
    return meticData,metricDfs

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

def checkUniqueIdInData(correlationId, column_list):
    dbconn, dbcollection = open_dbconn('PS_BusinessProblem')
    data_json = dbcollection.find({"CorrelationId": correlationId})
    TargetUniqueIdentifier = data_json[0].get('TargetUniqueIdentifier')
    unique_id = ""
    if TargetUniqueIdentifier in column_list:
        unique_id = TargetUniqueIdentifier
    return unique_id


def updateAggregateValue(correlationId,aggDays):
    dbconn, dbcollection = open_dbconn('PS_BusinessProblem')
    data_json = dbcollection.find({"CorrelationId": correlationId})
    cfg = data_json[0]["TimeSeries"]
    print ("!!!!!!!!!!!!!!",data_json[0])
    for each in cfg["Frequency"]:
        if cfg["Frequency"][each]["Name"] == "CustomDays":
            cfg["Frequency"][each]["value"] = aggDays
        
    dbcollection.update({"CorrelationId": correlationId},
                        {'$set':{"TimeSeries": cfg }})  


def removespecialchar(val):
    regex = re.compile('.$') 
    specialcha=['.','$']
    if (regex.search(val)==None):
        return str(val)
    else:
        return ''.join(i for i in val if not i in specialcha)

def getIaUsaCaseData(ClientUID,DeliveryConstructUId,url,cDetails,token,correlationId):
    auth_type = get_auth_type()
    payload = { "ClientUID"            : ClientUID, 
                "DeliveryConstructUId" : [DeliveryConstructUId], 
                "StartDate"            : cDetails["InputParameters"]["FromDate"],
                "EndDate"              : cDetails["InputParameters"]["ToDate"],
                "ReleaseUID"           : cDetails["InputParameters"]["ReleaseUID"],
                "Measure_metrics"      : cDetails["InputParameters"]["Measure_metrics"],
                "TotalRecordCount"     : 0,
                "PageNumber"           : 1,
                "BatchSize"            : 5000
               }
    #print("payload::",payload)
    #print("URL::",url)
    #print("token:::",token)
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':
        resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json',
                                                'authorization' : 'bearer {}'.format(token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
        resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                auth=HttpNegotiateAuth())
    if resp.status_code == 200:
        if resp.json()['TotalRecordCount'] != 0:
            entityDataframe = pd.DataFrame()
            maxdata = maxdatapull(correlationId)
            nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
            x = 1
            if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                actualfromdate = payload['StartDate']
                if (datetime.strptime(str(payload['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d') > actualfromdate:
                    payload['StartDate'] = (datetime.strptime(str(payload['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d')
                    if auth_type == 'AzureAD':
                        resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json',
                                                                'authorization' : 'bearer {}'.format(token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                                auth=HttpNegotiateAuth())
                    while maxdata > resp.json()['TotalRecordCount']:
                        x = x + 1
                        if (datetime.strptime(str(payload['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d') > actualfromdate:
                            payload['StartDate'] = (datetime.strptime(str(payload['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d')
                            if auth_type == 'AzureAD':
                                resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json',
                                                                        'authorization' : 'bearer {}'.format(token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                            elif auth_type == 'WindowsAuthProvider':
                                resp = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
            numberOfPages = resp.json()['TotalPageCount']
            TotalRecordCount = resp.json()['TotalRecordCount']
            entityDataframe = pd.DataFrame()
            i = 0
            while i < numberOfPages:
                payload.update({"PageNumber": i + 1})
                payload.update({"TotalRecordCount": TotalRecordCount})
                if auth_type == 'AzureAD':
                    entityresults = requests.post(url, data=json.dumps(payload),headers={'Content-Type'  : 'application/json','authorization' : 'bearer {}'.format(token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                elif auth_type == 'WindowsAuthProvider':
                    entityresults = requests.post(url, data=json.dumps(payload),
                                                            auth=HttpNegotiateAuth(),headers={'Content-Type'  : 'application/json','authorization' : 'bearer {}'.format(token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                        
                if entityresults.status_code == 200:
                    entityData = entityresults.json()['Items']
                    if entityData != []:
                        df = pd.DataFrame(entityData)
                    entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                i = i + 1 
            if len(cDetails["InputParameters"]["ReleaseUID"]) > 0:
                releases = []
                for rl in entityDataframe.ReleaseUId.unique():
                    temp = entityDataframe[entityDataframe.ReleaseUId.isin([rl])]
                    temp = temp.sort_values(by='ModifiedOn',ascending=True)
                    releases.append(temp.iloc[[-1]])
                entityDataframe= pd.concat(releases)

        else:
            entityDataframe = 	pd.DataFrame()	
        return entityDataframe,"_"
    else:
        return False,resp.status_code

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

            else:
                customdataDfs['custom'] = "Data is not available for your selection"
    else:
        e = "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
        customdataDfs['custom']=e
    #print("CallCdmAPI is done ")
    return customdataDfs

def open_phoenixdbconn():
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection[config['DBconnection']['PhoenixDB']]
    return db

#update user ingested data with appended Cascaded data
def updateIngestDataWithCascade(cascadeMergedDf, correlationId, pageInfo, userId):
    dbconn, dbcollection = open_dbconn("PS_IngestedData")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    sourceDetails = data_json[0]["SourceDetails"]
    dbconn.close()
    
    columns = list(cascadeMergedDf.columns)
    chunks, filesize = file_split(cascadeMergedDf)
    
    save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId,
                           columns, Incremental = False, requestId = None,
                           sourceDetails = sourceDetails, colunivals = None,
                           timeseries = None, datapre = False, lastDateDict = None,
                           previousLastDate = None)

#update PS_BusinessProblem with the transformed data columns
def updateBusinessProblemWithCascade(data_final, correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    inputColumns = []
    availableColumns = list(data_final.columns)
    dbcollection.insert({"CorrelationId": correlationId,
                         "InputColumns": inputColumns,
                         "AvailableColumns": availableColumns,
                         "TargetColumn": "",
                         "TargetUniqueIdentifier": "",
                         "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))})  

def getFilePath(datasetid):
    dbconn, dbcollection = open_dbconn("DataSetInfo")
    data_json = list(dbcollection.find({"DataSetUId": datasetid}))
    dbconn.close()
    return data_json[0]['SourceDetails'],data_json[0]['SourceName']

def update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId):
    dbconn, dbcollection = open_dbconn("DataSet_IngestData")
    data_json = list(dbcollection.find({"DataSetUId": DataSetUId}))
    dbconn.close()
    if data_json != {}:
        dbconn_ps_ingestdata, dbcollection_ps_ingestdata = open_dbconn("PS_IngestedData")       
        dbcollection_ps_ingestdata.insert({
                    "CorrelationId": correlationId,
                    "InputData": {},
                    "ColumnsList": data_json[0].get('ColumnsList'),
                    "DataType": data_json[0].get('DataType'),
                    "SourceDetails": data_json[0].get('SourceDetails'),
                    "ColumnUniqueValues": data_json[0].get('ColumnUniqueValues'),
                    "lastDateDict": data_json[0].get('lastDateDict'),
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "DataSetUId":DataSetUId
                })
        return "Ingestion completed"
    else:
        return "datasetUID not exists in the Large data collection"
    
def checkofflineutility(correlationId):
    dbconn, dbcollection = open_dbconn("PS_IngestedData")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    if 'DataSetUId' in data_json[0]:
        if data_json[0].get('DataSetUId') != 'null':
            return data_json[0].get('DataSetUId')
        else:
            return False
    else:
        return False
    
def getcorrelationId(RequestId,DataSetUId):
    dbconn, dbcollection = open_dbconn("SSAI_IngrainRequests")
    data_json = list(dbcollection.find({"RequestId": RequestId,"DataSetUId":DataSetUId}))
    if 'CorrelationId' in data_json[0]:
        return data_json[0].get("CorrelationId")
    else:
        return None

def check_encrypt_for_offlineutility(DataSetUId):
    dbconn_datasetinfo,dbcollection_datasetinfo = open_dbconn('DataSetInfo')
    data_json_datasetinfo = list(dbcollection_datasetinfo.find({"DataSetUId": DataSetUId}))
    EnDeRequired = data_json_datasetinfo[0].get('DBEncryptionRequired')
    #print(EnDeRequired)
    dbconn_datasetinfo.close()
    return (EnDeRequired)

def data_from_chunks_offline_utility(corid, collection, lime=None, recent=None,DataSetUId=None):
    #print("herrrrrrrrrrrrrrrrrrrrr")
    # rege = [r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$',
    #        r'^(\b(0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](0?[1-9]|1[0-2])[^\w\d\r\n:](\d{4}|\d{2})\b)|(\b(0?[1-9]|1[0-2])[^\w\d\r\n:](0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](\d{4}|\d{2})\b)']
    #    rege=(r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$')
    #    rege = [r'\d{1,4}[/-]\d{1,2}[/-]\d{1,4}|\t|:?\d{2}:?\d{2}:?\d{2}']
    rege = [r'[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}[\t\s]\d{2}:\d{2}:\d{2}|[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}']
    rege1 = r'(\w*) '
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
        count = data_json.collection.count_documents({"DataSetUId": DataSetUId})

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
    #
    for col in data_t1.columns:
        l = list(data_t1[col][data_t1[col].notnull()])
        if len(l) > 1:
            # print ("abc",l)
            temp = l[1]
            if re.compile(rege1).match(str(temp)) is None:
                for pattern in rege:
                    if re.compile(pattern).match(str(temp)) is not None:
                        # print("data1:")
                        # print(data_t1)
                        # if platform.system() == 'Linux':
                        data_t1[col] = pd.to_datetime(data_t1[col], dayfirst=True, errors='coerce')
                        # elif platform.system() == 'Windows':
                        # data_t1[col]= pd.to_datetime(data_t1[col],errors='coerce')
                        # print("data2:")
                        # print(data_t1)
                        # data_t1.dropna(subset=[col], inplace=True)
    if collection =="PS_IngestedData":
        dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": corid}))
        UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
        dbconn.close()
        if UniqueIdentifir != None:
                dbconn, dbcollection = open_dbconn("PS_UsecaseDefinition")
                data_json = list(dbcollection.find({"CorrelationId": corid}))
                UniquePercent= data_json[0]['UniquenessDetails'][UniqueIdentifir]['Percent']
                dbconn.close()
                if UniquePercent >= 90 and UniquePercent < 100:
                    data_t1 = data_t1.drop_duplicates(subset=[UniqueIdentifir], keep="last")

    return data_t1



def save_data_chunks_large_file(chunks, collection, pageInfo, userId, columns,Incremental=False,requestId=None,sourceDetails=None, colunivals=None,
                     timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None):
    #print("inside save_data_chunks")
    dbconn, dbcollection = open_dbconn(collection)
    
    EnDeRequired = check_encrypt_for_offlineutility(DataSetUId)
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
        if EnDeRequired:                                  #EN555................................
            data_json = EncryptData.EncryptIt(data_json)

        #print("data_json")
        if datapre == None:
            dbcollection.insert({
                    "DataSetUId": DataSetUId,
                    "InputData": data_json,
                    "ColumnsList": columns,
                    "DataType": DataType,
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "previousLastDate":previousLastDate,
                    "lastDateDict": lastDateDict,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "ModifiedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "ModifiedBy": userId
                    
                })
        else:
            if chi == 0:
                dbcollection.remove({"DataSetUId": DataSetUId})
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
                    "DataSetUId": DataSetUId,
                    "InputData": data_json,
                    "ColumnsList": columns,
                    "DataType": DataType,
                    "SourceDetails": sourceDetails,
                    "ColumnUniqueValues": colunivals,
                    "PageInfo": pageInfo,
                    "CreatedByUser": userId,
                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "ModifiedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "ModifiedBy": userId
                })

    dbconn_datasetinfo,dbcollection_datasetinfo = open_dbconn('DataSetInfo')
    #print("before update")
    dbcollection_datasetinfo.update_one({"DataSetUId":DataSetUId},{'$set':{         
                                    'Status' : 'C','Message':"Data Ingestion Completed","Progress":"100"                                                     
                                   }}) 
    if Incremental:       
        dbcollection_datasetinfo.update_one({"DataSetUId":DataSetUId},{'$set':{         
                                    'LastModifiedDate' : str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))                                                     
                                   }})  										  
    dbconn_datasetinfo.close()
   
    dbconn.close()
    return 

def updQdbbyDataSetUId(DataSetUId, status, progress, pageInfo, userId,RequestId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    
    # print (dbconn,dbcollection)
    if not progress.isdigit():
        message = progress
        rmessage = "Task Complete"
        Status = 'E'
        progress = '0'
    elif status == "C":
        message = "Task Complete"
        rmessage = "Task Complete"
    else:
        message = "In - Progress"
        rmessage = "In - Progress" 
        
         
    dbcollection.update_many({'DataSetUId': DataSetUId,'RequestId':RequestId}, {'$set': {"Status": status,
                                                                    "Progress": progress,
                                                                     "RequestStatus": rmessage,
                                                                      "Message": message,
                                                                      "pageInfo": pageInfo,
                                                                      "CreatedByUser": userId,
                                                                      "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                                                     "ModifiedByUser": userId}})
    return 						   

def UsecaseDefinitionDetails_forlarge_file(df,datasetUID,userId):
    num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
    RecordCount = str(df.shape[0])
    empty_cols = [col for col in df.columns if df[col].dropna().empty]
    df.drop(columns = empty_cols,inplace = True)
    #print("dataisusecase")
    p = ",".join(empty_cols)
    recs = df.shape
    #t = "IngrAIn have found "+str(recs)+" valid records and Removed following empty attributes from data.  "+str([p])
    t = "Successfully ingested "+str(recs[0])+" records for "+str(recs[1])+" attributes"
    if len(empty_cols) > 0:
        t = t + " and removed following attributes due to empty records. "+str([p])
    t1 = {"EmptyColumns":empty_cols,"Msg":t,"Records":list(recs)}
    unPercent = {}
    unValues = {}
    for col in df.columns:
        try:
            x = round(len(df[col].unique()) * 100.0 / len(df[col]),2)
        except:
            df[col] = df[col].astype(str)
            x = round(len(df[col].unique()) * 100.0 / len(df[col]),2)
       
        l = round((100 - x )* len(df)/100)
        if x >= 80 and  x < 100:
            msg = "You have selected unique identifier as  "+ col+" which has "+str(x)+"% uniqueness,IngrAIn will be converting to 100% unique by removing "+str(round(100-x,2))+"% ("+str(l)+") duplicate records. please click on ok to procced."
        else:
            msg = {}
        unPercent[col] = {"Message" :msg,"Percent":x}
        if df[col].dtypes.name in num_type:
            unValues[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())][:50]
        else:
            unValues[col] = list(df[col].dropna().unique().astype('str'))[:3]
    #print("UniquenessDetails",unPercent)   
    #print("UniqueValues",unValues)	
    dbconn, dbcollection = open_dbconn('DataSetInfo')
    dbcollection.update_many({"DataSetUId": datasetUID},
                              {"$set" :{"RecordCount":RecordCount,
                                        "UniquenessDetails" : unPercent,
                                        "UniqueValues" : unValues,
                                        "ValidRecordsDetails" : t1}},upsert=True)
    
    

    return df

def update_usecasedefeinition(DataSetUId,correlationId):
    dbconn, dbcollection = open_dbconn("DataSetInfo")
    data_json = list(dbcollection.find({"DataSetUId": DataSetUId}))
    dbconn.close()
    if data_json != {}:
        dbconn_ps_ingestdata, dbcollection_ps_ingestdata = open_dbconn("PS_UsecaseDefinition")       
        dbcollection_ps_ingestdata.insert({
                    "CorrelationId": correlationId,
                    "CreatedByUser": data_json[0].get('CreatedBy'),
                    "ModifiedOn": data_json[0].get('ModifiedOn'),
                    "UniqueValues": data_json[0].get('UniqueValues'),
                    "UniquenessDetails": data_json[0].get('UniquenessDetails'),
                    "ValidRecordsDetails": data_json[0].get('ValidRecordsDetails')
                    
                })
        return 

#cascade visualization related utils functions
def getCascadeInpSample(cascaded_corid):
    dbconn,dbcollection = open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
    dbconn.close()
    EnDeRequired = getEncryptionFlag(cascaded_corid)
    
    if EnDeRequired:
        t=EncryptData.DescryptIt(base64.b64decode(data_json[0].get("InputSample")))
        inputSample = json.loads(t)
    else:
        inputSample=json.loads(data_json[0].get("InputSample"))
    requiredColumns = list(inputSample[0].keys())
    
    return requiredColumns


def checkIfLastModelDeployed(cascaded_corid):
    deployedFlag = True
    
    dbconn,dbcollection = open_dbconn('SSAI_CascadedModels')
    data_json = list(dbcollection.find({"CascadedId":cascaded_corid}))
    dbconn.close()
    
    dbModelList = data_json[0].get('ModelList')
    lastModelNumber = len(dbModelList.keys())
    lastModelCorrelationId = dbModelList.get("Model"+str(lastModelNumber)).get('CorrelationId')
    lastDeployedModel = "Model"+str(lastModelNumber)
    
    dbconn,dbcollection = open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":lastModelCorrelationId}))
    dbconn.close()
    
    if data_json[0].get("Status") != "Deployed" :
        deployedFlag = False
        lastDeployedModel = "Model"+str(lastModelNumber-1)
        
    return deployedFlag,lastDeployedModel

def getUniqueIdsCascade(cascaded_corid):
    _,lastDeployedModel = checkIfLastModelDeployed(cascaded_corid)
    
    uniqueIds = {}
    dbconn,dbcollection = open_dbconn('SSAI_CascadedModels')
    data_json = list(dbcollection.find({"CascadedId":cascaded_corid}))
    dbconn.close()
    
    mappings = data_json[0].get('Mappings')
    for model in mappings:
        uniqueIds[model] = mappings[model]["UniqueMapping"]["Source"]
        if ("Model" + str(int(model[-1])+1)) == lastDeployedModel:
            uniqueIds["Model" + str(int(model[-1])+1)] = mappings[model]["UniqueMapping"]["Target"]
                
    return uniqueIds

def cascadePredictionValidations(cascaded_corid, data_frame):
    cols_required = getCascadeInpSample(cascaded_corid)
    uniqueIds =  getUniqueIdsCascade(cascaded_corid)
    cols_required = list(set(cols_required).difference(set(uniqueIds.values())))
    
    data_frame_uids = list((set(data_frame.columns)).intersection(set(uniqueIds.values())))
    if len(data_frame_uids) == 0:
        raise Exception("UniqueId is not present in uploaded data")
    else: 
        if uniqueIds["Model1"] not in data_frame_uids:
            data_frame.rename(columns = {data_frame_uids[0] : uniqueIds["Model1"]}, inplace = True)
        drop_ids =  list(set(data_frame_uids).difference(set([uniqueIds["Model1"]])))
        if len(drop_ids)>0:
            data_frame.drop(columns=drop_ids, inplace=True,errors = "ignore")
    
    #drop rows with duplicate uniqueids
    data_frame.drop_duplicates(subset=uniqueIds["Model1"], keep="last", inplace=True)
    
    cols_required = cols_required + [uniqueIds["Model1"]]
    missing_columns = list(set(cols_required)-set(data_frame.columns))
    
    if len(missing_columns)==0:
        data_frame = data_frame[cols_required]
    else:
        missing_col_string = str(missing_columns)[1:-1]
        raise Exception(missing_col_string + " missing in the data. Please validate.")
         
    #remove rows where uniqueId is not present, other cells might be present
    data_frame = data_frame[data_frame[uniqueIds["Model1"]].notna()]
    #if id column is all whole number, change data type to int64
    num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
    if data_frame[uniqueIds["Model1"]].dtype in num_type:
        if np.array_equal(data_frame[uniqueIds["Model1"]], data_frame[uniqueIds["Model1"]].astype(int)):
            data_frame[uniqueIds["Model1"]] = data_frame[uniqueIds["Model1"]].astype(int)
    
    #remove rows where every column is blank, except uniqueId column
    remove_id_df = data_frame[data_frame[uniqueIds["Model1"]].notna() & (data_frame.isnull().sum(axis=1) == len(data_frame.columns) - 1)]
    remove_ids = remove_id_df[uniqueIds["Model1"]].to_list()
    data_frame = data_frame[~data_frame[uniqueIds["Model1"]].isin(remove_ids)]
    
    missing_columns = []
    for col in data_frame.columns:
        if data_frame[col].isnull().sum() == data_frame.shape[0]:
            missing_columns.append(col)

    if len(missing_columns) > 0:
        missing_col_string = str(missing_columns)[1:-1]
        raise Exception("Values missing in " + missing_col_string + ". Please validate the data.")
        
    if data_frame.shape[0] < 1:
        raise Exception("Data insufficient. Minimum number of records required for prediction is two")
    
    return data_frame

def getBaseModelUniqueId(cascaded_corid):
    uniqueIds = getUniqueIdsCascade(cascaded_corid)
    return uniqueIds["Model1"]

'''
#not required
def getBaseModelUniqueId(correlationId):
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    cascaded_corid = data_json[0]["CascadeIdList"][0]
    
    uniqueIds = getUniqueIdsCascade(cascaded_corid)
    
    return uniqueIds["Model1"]


def getCascadeEncryptionFlag(correlationId):
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    cascaded_corid = data_json[0]["CascadeIdList"][0]
    dbconn.close()
    flag = list(dbcollection.find({"CorrelationId": cascaded_corid}))[0]["DBEncryptionRequired"]
    return flag
'''
#cascade visualization related utils functions end here
def isFMModel(correlationId):
    dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
              
    
    if data_json[0]["IsFMModel"]:
        #print("2nd model")
        return True
    
    cascadeCorrelationID = data_json[0].get('FMCorrelationId',None)
    if cascadeCorrelationID:
        dbconn, dbcollection = open_dbconn("SSAI_DeployedModels")
        data_json = list(dbcollection.find({"CorrelationId": cascadeCorrelationID}))
        dbconn.close()
    
        if data_json[0]["IsFMModel"]:
            #print("1st model")
            return True
    return False

def FMDerivedColumns(data, Incremental):
    
    #check if all cells are empty strings
    temp_df = data.replace(r'^\s*$', np.nan, regex=True)
    if temp_df.isna().all().sum() == data.shape[1]:
        if Incremental:
            raise Exception("Minimum number of records required is 1. Please validate the data and try again.")
        else:
            raise Exception("Minimum number of unique Releases required is 20. Please validate the data and try again.")

    date_cols = ["PlannedEndDateTime","PlannedStartDateTime","RequestedEndDateTime",
                     "RequestedStartDateTime","ChangeStartDate"]
    
    if Incremental:
        req_cols = ["SNChangeNumber", "Release Name", "ChangeStartDate",
       "Category", "AssignmentGroup", "ChangeType", "SupportCompany",
       "PrimaryImpactedApplication", "Risk", "AssetType",
       "RequestedStartDateTime", "RequestedEndDateTime",
       "PlannedStartDateTime", "PlannedEndDateTime"]
    else:
        req_cols = ["ChangeOutcome", "SNChangeNumber", "Release Name", "ChangeStartDate",
       "Category", "AssignmentGroup", "ChangeType", "SupportCompany",
       "PrimaryImpactedApplication", "Risk", "AssetType",
       "RequestedStartDateTime", "RequestedEndDateTime",
       "PlannedStartDateTime", "PlannedEndDateTime"]
        
    missing_columns = list(set(req_cols)-set(data.columns))
    #missing_columns = list(set(date_cols)-set(data.columns))
    
    #all date columns should be available to get the derived columns
    if len(missing_columns)!=0:
        missing_col_string = str(missing_columns)[1:-1]
        raise Exception("Incorrect Data Template :" + missing_col_string + " are missing in the data. Please validate.")
    
    for dcol in date_cols:
        try:
            data[dcol] = data[dcol].apply(pd.to_datetime)
        except:
            raise Exception("Incorrect Data Template :" + str(dcol) + " is not in correct date format. Please validate.")

    #data[date_cols] = data[date_cols].apply(pd.to_datetime)
    data["Planned Hours"] = round((data["PlannedEndDateTime"]-data["PlannedStartDateTime"]).dt.seconds/3600,1)
    data["Requested Hours"] = round((data["RequestedEndDateTime"]-data["RequestedStartDateTime"]).dt.seconds/3600,1)
    data["Week"] = np.where(data["ChangeStartDate"].dt.weekday < 5, "Weekday", "Weekend")
    return data

def getFMPredictedCols(results):
    predictedCols = []
    for item in results:
        predDict = {}
        predDict["ID"] = item["SNChangeNumber"]
        predDict["Target"] = item["predictedValue"]
        predDict["TargetScore"] = item["predictionProbability"]
        predictedCols.append(predDict) 
        
    return predictedCols

def FMTrainingValidations(correlationId, data_frame):
    
    data_frame.replace('',np.nan, inplace=True)
    
    req_cols = ["ChangeOutcome", "SNChangeNumber", "Release Name", "ChangeStartDate",
       "Category", "AssignmentGroup", "ChangeType", "SupportCompany",
       "PrimaryImpactedApplication", "Risk", "AssetType",
       "RequestedStartDateTime", "RequestedEndDateTime",
       "PlannedStartDateTime", "PlannedEndDateTime"]
    
    derived_cols = ["Planned Hours","Requested Hours","Week"]
        
    missing_columns = list(set(req_cols+derived_cols)-set(data_frame.columns))
    
    #all required columns should be available
    if len(missing_columns)==0:
        data_frame = data_frame[req_cols+derived_cols]
    else:
        missing_col_string = str(missing_columns)[1:-1]
        raise Exception("Incorrect Data Template :" + missing_col_string + " are missing in the data. Please validate.")
    
    # number of unique Releases should be >=20
    if data_frame["Release Name"].nunique() < 20:
        raise Exception("Minimum number of unique Releases required is 20. Please validate the data and try again.")
    
    #remove rows where SNChangeNumber is not present, other cells might be present
    data_frame = data_frame[data_frame["SNChangeNumber"].notna()]
    
    #remove rows where Release Name is not present, other cells might be present
    data_frame = data_frame[data_frame["Release Name"].notna()]
    
    #drop rows with duplicate uniqueids
    data_frame.drop_duplicates(subset="SNChangeNumber", keep="last", inplace=True)
        
    #If the values are missing for the entire column for a particular release, drop all rows in that release
    drop_releases = []
    for release in data_frame["Release Name"].unique():
        temp_df = data_frame.loc[data_frame["Release Name"] == release]
        
        for col in temp_df.columns:
            if col!="Release Name"  and (temp_df[col].isnull().sum() == temp_df.shape[0]):
                drop_releases.append(release)
                break
            
    if len(drop_releases)>0:
        data_frame = data_frame[~data_frame["Release Name"].isin(drop_releases)]

    #remove duplicates
    data_frame = data_frame.loc[:,~data_frame.columns.duplicated()]
        
    #missing value imputation with most frequently occuring value
    for col in data_frame.columns:
        if col!="SNChangeNumber" and col!="Release Name":
            data_frame[col].fillna(data_frame[col].mode()[0], inplace=True)
    
    return data_frame

def FMPredictionValidations(correlationId, data_frame):
    
    data_frame.replace('',np.nan, inplace=True)
    
    req_cols = ["SNChangeNumber", "Release Name", "ChangeStartDate",
       "Category", "AssignmentGroup", "ChangeType", "SupportCompany",
       "PrimaryImpactedApplication", "Risk", "AssetType",
       "RequestedStartDateTime", "RequestedEndDateTime",
       "PlannedStartDateTime", "PlannedEndDateTime"]
    
    derived_cols = ["Planned Hours","Requested Hours","Week"]
        
    missing_columns = list(set(req_cols+derived_cols)-set(data_frame.columns))
    
    #all required columns should be available
    if len(missing_columns)==0:
        data_frame = data_frame[req_cols+derived_cols]
    else:
        missing_col_string = str(missing_columns)[1:-1]
        raise Exception("Incorrect Data Template :" + missing_col_string + " are missing in the data. Please validate.")
    
    #remove rows where SNChangeNumber is not present, other cells might be present
    data_frame = data_frame[data_frame["SNChangeNumber"].notna()]
    
    #remove rows where Release Name is not present, other cells might be present
    data_frame = data_frame[data_frame["Release Name"].notna()]
    
    #drop rows with duplicate uniqueids
    data_frame.drop_duplicates(subset="SNChangeNumber", keep="last", inplace=True)
        
    #If the values are missing for the entire column for a particular release, drop all rows in that release
    drop_releases = []
    for release in data_frame["Release Name"].unique():
        temp_df = data_frame.loc[data_frame["Release Name"] == release]
        
        for col in temp_df.columns:
            if col!="Release Name"  and (temp_df[col].isnull().sum() == temp_df.shape[0]):
                drop_releases.append(release)
                break
            
    if len(drop_releases)>0:
        data_frame = data_frame[~data_frame["Release Name"].isin(drop_releases)]

    #remove duplicates
    data_frame = data_frame.loc[:,~data_frame.columns.duplicated()]
        
    #missing value imputation with most frequently occuring value
    for col in data_frame.columns:
        if col!="SNChangeNumber" and col!="Release Name":
            data_frame[col].fillna(data_frame[col].mode()[0], inplace=True)
    
    return data_frame

def getFMPredictionData(correlationId,cascadeCorrelationID,uniqueId):
    dbconn, dbcollection = open_dbconn("SSAI_FMVisualization")
    data_json = list(dbcollection.find({"CorrelationId": correlationId,"UniqueId":uniqueId}))
              
    dbconn.close()
    Incremental = data_json[0]["IsIncremental"]
    
    if Incremental:
        FMEnDeRequired = getEncryptionFlag(correlationId)
        dbconn,dbcollection = open_dbconn('SSAI_PublishModel')
        data_json = list(dbcollection.find({"CorrelationId":correlationId,"UniqueId": uniqueId}))
        dbconn.close()
        data = []
        for i in range(len(data_json)):
            actual_data = str(data_json[i].get("ActualData"))
            if FMEnDeRequired:
                t = base64.b64decode(actual_data)
                actual_data = eval((EncryptData.DescryptIt(t)).encode('ascii',"ignore").decode('ascii'))
            else:
                actual_data = actual_data.strip().rstrip()
                actual_data = actual_data.encode('ascii',"ignore").decode('ascii')
                if isinstance(actual_data,str):
                    try:
                        actual_data = eval(actual_data)
                    except SyntaxError:
                        actual_data = eval(''.join([i if ord(i) < 128 else ' ' for i in actual_data]))
                    except:
                        actual_data = json.loads(actual_data)
            data = data + actual_data
        data = pd.DataFrame(data)
    else:
        data = data_from_chunks(corid = cascadeCorrelationID, collection = "PS_IngestedData")
        
        #get the latest 5 releases for prediction
        
        temp_df = data.copy()
        temp_df = temp_df[["Release Name","PlannedEndDateTime"]]
        temp_df["Release End Date"] = temp_df.groupby(["Release Name"])["PlannedEndDateTime"].transform("max")
        temp_df.drop("PlannedEndDateTime", axis=1, inplace = True)
        temp_df.drop_duplicates(inplace = True)
        temp_df.sort_values(by="Release End Date", ascending=False, inplace=True)
        releases = temp_df["Release Name"][:5].to_list()
        
        data = data[data["Release Name"].isin(releases)]
        
    return data

    
def getFMTransformedData(currentResultDf,cascadeResultDf):
    cascadeResultDf.rename(columns={"TargetScore_Met":"PredictedOutcome"}, inplace = True)
    drop_columns = ["TargetScore_Missed"]
    cascadeResultDf.drop(drop_columns, axis=1, inplace = True, errors="ignore")
    
    df_merge = pd.merge(currentResultDf, cascadeResultDf, left_on = "SNChangeNumber", right_on = "ID", suffixes=(False, False))
    df_merge.drop(["ID","ChangeOutcome"], axis=1, inplace = True, errors="ignore")        
    df_merge.rename(columns={"Target":"ChangeOutcome"}, inplace = True)
    
    date_cols = ["PlannedEndDateTime","PlannedStartDateTime","RequestedEndDateTime",
                     "RequestedStartDateTime"]
    df_merge[date_cols] = df_merge[date_cols].apply(pd.to_datetime)
    derived_cols = {"Planned Hours","Requested Hours","Week"}
    if not derived_cols.issubset(df_merge.columns):
        df_merge["Planned Hours"] = round((df_merge["PlannedEndDateTime"]-df_merge["PlannedStartDateTime"]).dt.seconds/3600,1)
        df_merge["Requested Hours"] = round((df_merge["RequestedEndDateTime"]-df_merge["RequestedStartDateTime"]).dt.seconds/3600,1)
        df_merge["Week"] = np.where(df_merge["ChangeStartDate"].dt.weekday < 5, "Weekday", "Weekend")
    if (df_merge[["Planned Hours","Requested Hours"]]<0).values.any():
        return "Unhealthy Data : Planned Start Date is greater than Planned End Date. Please validate and try again"
    #create a field Sort Order, Scaled Sort Order, and PredOut 
    df_merge["Sort Order"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["PredictedOutcome"].rank(ascending=False, method="first")
    
    df_merge["Scaled Sort Order"] = df_merge["PredictedOutcome"]*df_merge["Sort Order"]
    
    df_merge["Numerator"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["Scaled Sort Order"].transform("sum")
    df_merge["Denominator"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["Sort Order"].transform("sum")
    df_merge["PredOut"] = round(df_merge["Numerator"]/df_merge["Denominator"],2)
    df_merge.drop(columns=["Numerator","Denominator"],inplace=True, errors="ignore")
    
    derived_data = df_merge.copy()
        
    #create ["Average Planned Hours", "Average Requested Hours"] groupby "Release Name"
    derived_data["Average Planned Hours"] = round(derived_data.groupby(["Release Name"])["Planned Hours"].transform("mean"),2)
    derived_data["Average Requested Hours"] = round(derived_data.groupby(["Release Name"])["Requested Hours"].transform("mean"),2)
    
    #drop unnecessary columns
    derived_data.drop(columns=["ChangeStartDate", "Category",
           "SubCategory", "AssignmentGroup","SupportCompany","Risk", "PlannedEndDateTime",
           "PlannedStartDateTime","RequestedEndDateTime",
           "RequestedStartDateTime","AssetID", "AssetStatus",
           "AssetType", "Week","SNChangeNumber"],inplace=True,errors='ignore')
            
    for i,j in derived_data[['PrimaryImpactedApplication','Release Name']].values:
        if str('PredOut_'+i) not in derived_data.columns:
            derived_data['PredOut_'+i] = 0
        derived_data.loc[derived_data['Release Name']==j,'PredOut_'+i] = derived_data[(derived_data['PrimaryImpactedApplication']==i) & (derived_data['Release Name']==j)]['PredOut'].unique().copy()[0]
        
        change_type = derived_data[(derived_data['PrimaryImpactedApplication']==i) & (derived_data['Release Name']==j)]['ChangeType'].value_counts()
        for x in change_type.index:
            derived_data.loc[derived_data['Release Name']==j,x+'_'+i] = change_type[x]
    
    dict1 = derived_data.loc[derived_data["ChangeOutcome"]=="Met","Release Name"].value_counts().reset_index().rename(columns={"Release Name":"Numerator"})
    data_final = pd.merge(derived_data,dict1,left_on=["Release Name"],right_on=["index"],how="left")
    data_final.drop(columns=["index"],inplace=True,errors="ignore")
    data_final["Numerator"].fillna(0,inplace=True)
    data_final["Denominator"] = data_final.groupby(["Release Name"])["Numerator"].transform("count")
    data_final["Release Success Probability"] = data_final["Numerator"]/data_final["Denominator"]
    data_final.drop(columns=["Numerator","Denominator"],inplace=True,errors="ignore")
    
    #calculate "Release End Date" for each release
    data_final["Release End Date"] = df_merge.groupby(["Release Name"])["PlannedEndDateTime"].transform("max")
    
    #drop unnecessary columns
    data_final.drop(columns=["ChangeType", "PrimaryImpactedApplication",
           "Planned Hours", "Requested Hours", "ChangeOutcome", "PredictedOutcome", "Sort Order",
           "Scaled Sort Order","PredOut"], inplace=True, errors="ignore")
    
    #finally add ["Total Tickets"] for each release and drop duplicates
    data_final["Total Tickets"] = derived_data.groupby(["Release Name"])["Release Name"].transform("count")
    data_final.drop_duplicates(inplace = True)
    
    return data_final

def updateautotrain_record(correlationId,pageInfo,progress):
    dbconn, dbcollection = open_dbconn("SSAI_IngrainRequests")
    data_json = list(dbcollection.find({"CorrelationId": correlationId,"pageInfo":"IngestData"}))
    requestId = data_json[0]['AutoTrainRequestId']
    dbcollection.update_one({"CorrelationId": correlationId, "RequestId": requestId,"Function":"AutoTrain"},{'$set':{         
                                    'FunctionPageInfo' : pageInfo,'Progress':progress                                                    
                                   }}) 
    dbconn.close()
    return

def updateautotrain_record_forerror(correlationId,pageInfo):
    regression_flag = check_instaregression(correlationId)
    if not regression_flag:
        dbconn, dbcollection = open_dbconn("SSAI_IngrainRequests")
        data_json = list(dbcollection.find({"CorrelationId": correlationId,"pageInfo":"IngestData"}))
        requestId = data_json[0]['AutoTrainRequestId']
        dbcollection.update_one({"CorrelationId": correlationId, "RequestId": requestId,"Function":"AutoTrain"},{'$set':{         
                                        'FunctionPageInfo' : pageInfo,"Status":"E","RequestStatus":"Error"                                                    
                                       }})
        dbconn.close()
    else:
        dbconn, dbcollection = open_dbconn("SSAI_IngrainRequests")
        dbcollection.update_one({"CorrelationId": correlationId, "pageInfo": pageInfo},{'$set':{         
                                        "Status":"E","RequestStatus":"Error"                                                    
                                       }})
        dbconn.close()
    return

def check_instaregression(correlationId):
    dbconn,dbcollection = open_dbconn('PS_BusinessProblem')
    data = list(dbcollection.find({"CorrelationId" :correlationId}))
    dbconn.close()
    if 'UseCaseID' in data[0] and data[0]['UseCaseID'] != None:
        return True
    else:
        return False

def check_instaml(correlationId):
    dbconn,dbcollection = open_dbconn('PS_BusinessProblem')
    data = list(dbcollection.find({"CorrelationId" :correlationId}))
    dbconn.close()
    if 'InstaId' in data[0] and data[0]['InstaId'] != None:
        return True
    else:
        return False
