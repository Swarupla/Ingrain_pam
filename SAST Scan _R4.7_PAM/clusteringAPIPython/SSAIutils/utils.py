import platform
if platform.system() == 'Linux':
    conf_path = '/main/pythonconfig.ini'
    EntityConfig_path = '/main/pheonixentityconfig.ini'
    work_dir = '/'
    #modelPath = '/saveModels/'
    # pylogs_dir = '/IngrAIn_Python/PyLogs/'
elif platform.system() == 'Windows':
    conf_path = '\main\pythonconfig.ini'
    EntityConfig_path = '\main\pheonixentityconfig.ini'
    work_dir = '\\'
    from requests_negotiate_sspi import HttpNegotiateAuth
    #modelPath = '\saveModels\\'
    # pylogs_dir = '\IngrAIn_Python\PyLogs\\'
import configparser, os

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)

from main import file_encryptor
from dateutil import relativedelta

try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
        #config.read(configpath) 

import io
mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
logpath = config['filePath']['pyLogsPath']
if os.path.isdir(logpath):
    variable="Dir path exists"
else:
    os.makedirs(logpath)
#logpath = os.getcwd()+str(pylogs_dir)
#if platform.system() == 'Linux':
#    saveModelPath = str(os.getcwd()) + modelPath
#elif platform.system() == 'Windows':
saveModelPath = config['filePath']['saveModelPath']

if os.path.isdir(saveModelPath):
    variable="Path is available"
else:
    os.makedirs(saveModelPath)
auth_type = config['auth_scheme']['authProvider']

def min_data():
    min1=config['Min_Data']['min_data']
    
    return int(min1)
    

import collections
import numbers
import uuid
from pymongo import MongoClient
import sys
import numpy as np
import pandas as pd
from pandas.io.json import json_normalize
from collections import ChainMap								
import math
import json
import logging
from datetime import datetime
import os
import re
from sklearn.model_selection import train_test_split
#from SSAIutils import encryption
import pickle
from pymongo.errors import ServerSelectionTimeoutError
from pymongo.errors import NetworkTimeout
import configparser
import pymongo
import requests,json
from main import EncryptData
from datetime import datetime,timedelta
from pythonjsonlogger import jsonlogger
from cryptography.fernet import Fernet
import base64
import io
EntityConfig = configparser.RawConfigParser()
EntityConfig.read(str(os.getcwd()) + str(EntityConfig_path))
pickleKey = config['SECURITY']['pklFileKey']

'''
collection="PS_IngestedData"
'''
def get_max_categories():
    cat=config["DataCuration"]["maxCategories"]
    return cat
def get_auth_type():
    auth=config['auth_scheme']['authProvider']
    return auth
def lambda_exec(data,freq_most):
    return data.apply(lambda word: " ".join(word for word in word.split() if word not in freq_most))
def lambda_execute(series):
    return series.apply(lambda word1: " ".join(word1.lower() for word1 in str(word1).split()))
def lambda_execute1(series,stop):
    return series.apply(lambda word2: " ".join(word2 for word2 in str(word2).split() if str(word2) not in stop))
def getEntityToken():
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
    file_path = open(file_path, 'wb')
    pickle.dump(encrypted_data, file_path)
    return 

def decryptPickle(filepath):
    file = open(filepath, 'rb')
    loadedPickle = pickle.load(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    model = pickle.loads(f.decrypt(loadedPickle))
    return model

def getEncryptionFlag(corrid):  #En.................
    dbconn, dbcollection = open_dbconn("Clustering_IngestData")
    flag = list(dbcollection.find({"CorrelationId": corrid}))[0]["DBEncryptionRequired"]
    
    return flag

def open_dbconn1(collection):

    connection = MongoClient(config['DBconnection']['connectionString'])
    db = 'ingrAIn'
    db_IngestedData = connection[db][collection]
    return connection,db_IngestedData

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


def get_DataCleanUpView(correlationId):
    dtype = {}
    scale = {}
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    q = data_json[0]['Feature Name']
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
    rege = [  # r'\d{1,4}[/-]\d{1,2}[/-]\d{1,4}|\t|:?\d{2}:?\d{2}:?\d{2}',
        r'\d{1,4}[/-]\d{1,2}[/-]\d{1,4}[\t, ,]:?\d{2}:?\d{2}:?\d{2}',
        r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$',
        r'^(\b(0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](0?[1-9]|1[0-2])[^\w\d\r\n:](\d{4}|\d{2})\b)|(\b(0?[1-9]|1[0-2])[^\w\d\r\n:](0?[1-9]|[12]\d|30|31)[^\w\d\r\n:](\d{4}|\d{2})\b)']
    #    rege=(r'^(-?(?:[1-9][0-9]*)?[0-9]{4})-(1[0-2]|0[1-9])-(3[01]|0[1-9]|[12][0-9])T(2[0-3]|[01][0-9]):([0-5][0-9]):([0-5][0-9])(Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])?$')
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


def file_split(data):
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
    return chunks, t_filesize


'''
collection="SSAI_IngestedData" 
corid = str(uuid.uuid4())
chunks = file_split(data)
save_data_chunks(corid=None,chunks,collection):
'''
def check_columns(corid, collection):
    dbconn, dbcollection = open_dbconn(collection)
    data_json = dbcollection.find({"CorrelationId": corid})
    dbconn.close()
    #target_variable = data_json[0].get('Actual_Target')
    return data_json

def subset_columns_clustering(corid,collection):
    dbconn, dbcollection = open_dbconn(collection)
    data_json = dbcollection.find({"CorrelationId": corid})
    columns_list_ogdata=data_json[0].get('ColumnsList')
    dbconn.close()

    dbconn, dbcollection = open_dbconn('Clustering_IngestData')
    data_json = dbcollection.find({"CorrelationId": corid})
    columns_list_userselected=data_json[0].get('Columnsselectedbyuser')
    dbconn.close()
    if set(columns_list_userselected).issubset(columns_list_ogdata):
        return True
    else:
        return False







def save_data_chunks(chunks, collection, corid, pageInfo, userId, columns, sourceDetails=None, colunivals=None,
                     timeseries=None, datapre=None, lastDateDict=None,modelname=None,mapdata=None,ingestion_message=None,df_shape=None):
    dbconn, dbcollection = open_dbconn(collection)
    EnDeRequired = getEncryptionFlag(corid)

    for chi in range(len(chunks)):
        Id = str(uuid.uuid4())
        if not timeseries:
            #data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            try:
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            except Exception as e:
                list_indexes= []
                for i,row in chunks[chi].iterrows():
                    try:
                        pd.DataFrame(row).to_json(orient='records', date_format='iso', date_unit='s')
                    except Exception as e:
                        list_indexes.append(i)
                chunks[chi].drop(index =list_indexes ,inplace=True)
                chunks[chi].reset_index(drop=True,inplace=True)
                data_json = chunks[chi].to_json(orient='records', date_format='iso', date_unit='s')
            # SOC Identifying date,datetimestamps
            data = DateTimeStampParser(chunks[chi])
            DataType = {}
            for i, j in data.iteritems():
                if str(data[i].dtypes) == 'object':
                    data[i]
                    DataType.update({i: 'Category'})
                elif str(data[i].dtypes) == 'float64':
                    DataType.update({i: 'Float'})
                elif str(data[i].dtypes) == 'int64':
                    DataType.update({i: 'Integer'})
                elif str(data[i].dtypes) == 'datetime64[ns]':
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
        
        if datapre == None and mapdata==True:
            dbcollection.remove({"CorrelationId": corid,"ModelName":modelname})
            dbcollection.insert({
                "CorrelationId": corid,
                "InputData": data_json,
                "ColumnsList": columns,
                "DataType": DataType,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "lastDateDict": lastDateDict,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                "ModelName":modelname,
                "Ingestion_Message":ingestion_message,
                "Dataframe_Shape":df_shape
            })
        elif datapre == None and mapdata==None:
            dbcollection.insert({
                "CorrelationId": corid,
                "InputData": data_json,
                "ColumnsList": columns,
                "DataType": DataType,
                "SourceDetails": sourceDetails,
                "ColumnUniqueValues": colunivals,
                "lastDateDict": lastDateDict,
                "PageInfo": pageInfo,
                "CreatedByUser": userId,
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                "ModelName":modelname,
                "Ingestion_Message":ingestion_message,
                "Dataframe_Shape":df_shape
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
                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                "ModelName":modelname,
                "Ingestion_Message":ingestion_message,
                "Dataframe_Shape":df_shape
            })

    dbconn.close()


'''
Saving user inputed column data to mongoDB
'''


def save_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                  DateFormat, userId,target_variable=None,collection=None):
    dbconn, dbcollection = open_dbconn(collection)
    # chunks,filesize= file_split(data)
    # for chi in range(len(chunks)):
    Id = str(uuid.uuid4())
    dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               'inputcols': UserInputColumns,
                               "ColumnUniqueValues": ColUniqueValues,
                               "types": typeDict,
                               "target_variable": target_variable,
                               "DateFormats": DateFormat,
                               "removedcols": {},
                               "CreatedByUser": userId}])


def Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns, DateFormat, userId,collection):
    dbconn, dbcollection = open_dbconn(collection)
    dbcollection.update_many({"CorrelationId": correlationId}, {'$set':
                                                                    {"CorrelationId": correlationId,
                                                                     "ColumnUniqueValues": ColUniqueValues,
                                                                     "types": typeDict,
                                                                     "DateFormats": DateFormat,
                                                                     "inputcols": UserInputColumns,
                                                                     "removedcols": {},
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


def get_DataCleanUP_FilteredData(correlationId):
    dbconn, dbcollection = open_dbconn('DataCleanUP_FilteredData')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    types = data_json[0]['types']
    target = data_json[0]['target_variable']
    removedcols = data_json[0]['removedcols']
    UserInputColumns = data_json[0]['inputcols']
    DateFormats = data_json[0]['DateFormats']
    return types, target, removedcols, UserInputColumns, DateFormats


def save_DE_DataCleanup(feature_name, Outlier_Dict, corrDict, correlationId, TargetProblemType, OrginalDtypes, userId,collection):
    Id = str(uuid.uuid4())
    dbconn, dbcollection = open_dbconn(collection)
    dbcollection.insert_many([{"_id": Id,
                               "CorrelationId": correlationId,
                               "Feature Name": feature_name,
                               "OutlierDict": Outlier_Dict,
                               "CorrelationToRemove": corrDict,
                               "OginalDtypes": OrginalDtypes,
                               "Target_ProblemType": TargetProblemType,
                               "CreatedByUser": userId}])


def Update_DE_DataCleanup(correlationId, unchangedColumns, flag, feature_name=None, Outlier_Dict=None, corrDict=None,
                          TargetProblemType=None, orgtyp=None, userId=None):
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    if flag == None:
        dbcollection.update_many({"CorrelationId": correlationId},
                                 {'$set': {"CorrelationId": correlationId,
                                           "Feature Name": feature_name,
                                           "OutlierDict": Outlier_Dict,
                                           "CorrelationToRemove": corrDict,
                                           "Target_ProblemType": TargetProblemType,
                                           "OginalDtypes": orgtyp,
                                           "UnchangedDtypeColumns": unchangedColumns,
                                           "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                           "CreatedByUser": userId}})

    else:
        dbcollection.update_many({"CorrelationId": correlationId},
                                 {'$set': {"CorrelationId": correlationId,
                                           "UnchangedDtypeColumns": unchangedColumns}})


def get_DataCleanUp(correlationId):
    dtype = {}
    scale = {}
    dbconn, dbcollection = open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    q = data_json[0]['Feature Name']
    columns_modifiedDict = data_json[0]['DtypeModifiedColumns']
    scale_modified = data_json[0]['ScaleModifiedColumns']
    orgnlTypes = data_json[0]['OginalDtypes']
    targetType = data_json[0]['Target_ProblemType']
    df = pd.DataFrame.from_dict(q, orient='index')
    for col in df.index:
        dtype[col] = list(df.Datatype[col].keys())
        scale[col] = list(df.Scale[col].keys())
    return df, columns_modifiedDict, dtype, scale, scale_modified, orgnlTypes, targetType


def save_data(correlationId, pageInfo, userId, collection, filepath=None, data=None,modelname=None,datapre=None,mapdata=None):
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
    save_data_chunks(chunks, collection, correlationId, pageInfo, userId, columns, sourceDetails, colunivals=None,
                     datapre=datapre,modelname=modelname,mapdata=mapdata)
    return filesize


'''CHANGES START HERE'''


def save_data_multi_file(correlationId, pageInfo, userId,UniId, multi_source_dfs, parent, mapping, mapping_flag, ClientUID,
                         DeliveryConstructUId, insta_id, auto_retrain, datapre=None, lastDateDict=None,MappingColumns= None):
    all_columns = {}
    cols = []
    parent_file_index = 0
    counter = 0
    if mapping_flag != 'True':
        if multi_source_dfs['VDSInstaML'] != {}:
            if "vds_InsatML" in multi_source_dfs['VDSInstaML'].keys():
                if isinstance(multi_source_dfs['VDSInstaML']['vds_InsatML'], pd.DataFrame):
                    data_frame = multi_source_dfs['VDSInstaML']['vds_InsatML']
                    columns = list(data_frame.columns)
                    df_shape = list(data_frame.shape)
                    ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                    # chunks,size =file_split(data_frame)
                    sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "InstaID": insta_id,
                                     "Source": "InstaMl_VDS"}
                    if auto_retrain:
                        size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo, userId,
                                                  columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                    else:
                        chunks, size = file_split(data_frame)
                        save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,
                                         sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
                    return "single_file"
                else:
                    raise Exception(multi_source_dfs['VDSInstaML']['vds_InsatML'])
            else:
                if isinstance(multi_source_dfs['VDSInstaML']['InstaML_Regression'], pd.DataFrame):
                    data_frame = multi_source_dfs['VDSInstaML']['InstaML_Regression']
                    columns = list(data_frame.columns)
                    # chunks,size =file_split(data_frame)
                    sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "InstaID": insta_id,
                                     "Source": "InstaML_Regression"}
                    df_shape = list(data_frame.shape)
                    ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                    if auto_retrain:
                        size = append_data_chunks(data_frame, "PS_IngestedData", correlationId, pageInfo, userId,
                                                  columns, sourceDetails, colunivals=None, lastDateDict=lastDateDict)
                    else:
                        chunks, size = file_split(data_frame)
                        save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns,
                                         sourceDetails, colunivals=None, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
                    return "single_file"
                else:
                    raise Exception(multi_source_dfs['VDSInstaML']['InstaML_Regression'])
        
        elif multi_source_dfs['Custom'] != {}:
             if isinstance(multi_source_dfs['Custom']['Custom'], pd.DataFrame):
                    data_frame = multi_source_dfs['Custom']['Custom']
                    columns = list(data_frame.columns)
                    df_shape = list(data_frame.shape)
                    ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                    chunks, size = file_split(data_frame)
                    sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                                 "Source": "Custom"}
                    save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns,
                                                 sourceDetails, colunivals=None,lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape) 
                    return "single_file"
             else:
                 raise Exception(multi_source_dfs['Custom']['Custom'])

                
        else:
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
                                cols_details = data_frame.dtypes.apply(lambda x: x.name).to_dict()
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
        elif counter == 1:

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
                                size = append_data_chunks(data_frame, "Clustering_BusinessProblem", correlationId, pageInfo,
                                                          userId, columns, sourceDetails, colunivals=None,
                                                          lastDateDict=lastDateDict)
                            else:
                                df_shape = list(data_frame.shape)
                                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                                chunks, size = file_split(data_frame)
                                save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns,
                                                 sourceDetails, colunivals=None,lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
                    elif key == 'Entity':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'Entity'
                                columns = list(data_frame.columns)
                                df_shape = list(data_frame.shape)
                                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                                # chunks,size =file_split(data_frame)
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": type_key,
                                                 "Source": "CDM"}
                                if auto_retrain:
                                    size = append_data_chunks(data_frame, "Clustering_BusinessProblem", correlationId, pageInfo,
                                                              userId, columns, sourceDetails, colunivals=None,
                                                              lastDateDict=lastDateDict)
                                else:
                                    chunks, size = file_split(data_frame)
                                    save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId,
                                                     columns, sourceDetails, colunivals=None,lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
                    elif key == 'File':
                        for key1 in multi_source_dfs[key].keys():
                            if isinstance(multi_source_dfs[key][key1], pd.DataFrame):
                                data_frame = multi_source_dfs[key][key1]
                                type_key = 'File'
                                columns = list(data_frame.columns)
                                df_shape = list(data_frame.shape)
                                ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
                
                                chunks, size = file_split(data_frame)
                                sourceDetails = {"CID": ClientUID, "DUID": DeliveryConstructUId, "Entity": "Manual",
                                                 "Source": type_key}
                                save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns,
                                                 sourceDetails, colunivals=None,lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
            return "single_file"
        counter = len(cols)
        flag_1 = True
        for i in range(0, counter):
            if i == counter - 1:
                break
            else:
                if cols[i] == cols[i + 1]:
                    continue
                else:
                    flag_1 = False
                    break
        if not flag_1:
            parent_cols = cols[parent_file_index]
            parent_cols_len = parent_cols.__len__()
            set_p = set(parent_cols)
            set_overlap = set_p
            for i in range(0, counter):

                if i != parent_file_index:
                    set_i = set(cols[i])
                    set_overlap = set_overlap & set_i
            if len(set_overlap) > 1:
                flag_2 = True
            else:
                flag_2 = False


        if not flag_1 and not flag_2:
            flag_4 = True
            parent_cols = cols[parent_file_index]
            set_p = set(parent_cols)
            for i in range(0, counter):

                if i != parent_file_index:

                    set_i = set(cols[i])
                    overlap = set_p & set_i
                    # universe = set_p | set_i
                    result = float(len(overlap)) / len(set_p) * 100
                    if result == 0:
                        continue
                    else:
                        flag_4 = False
                        break

        if not flag_1 and not flag_2 and not flag_4:
            flag_3 = True
        else:
            flag_3 = False

        final_df = None
        flag_final = True
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
                size = append_data_chunks(final_df, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns,
                                          sourceDetails, colunivals=None, lastDateDict=lastDateDict)
            else:
                chunks, filesize = file_split(final_df)
                save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns, sourceDetails,
                                 colunivals=None,lastDateDict=lastDateDict)
            return "single_file"
        # Insert Into DB- Mapping Related Details
        if flag_1:

            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
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
            dbconn.close()
            return "single_file"
        elif flag_2:
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
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
                dbcollection.insert_one({
                    "_id": Id,
                    "CorrelationId": correlationId,
                    "File": all_columns,
                    "Flag": "flag2",

                    # "FilePath": filepath,
                    "CreatedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                    "CreatedByUser": userId
                })
            dbconn.close()
            return "single_file"
        elif flag_3:
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")                    # "ParentFile": parentfile,
                    #"pageInfo": pageInfo,

            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
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
            return "multi_file"
        elif flag_4:
            dbconn, dbcollection = open_dbconn("PS_MultiFileColumn")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            if len(data_json) == 0:
                Id = str(uuid.uuid4())
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
        i = 0
        list_mapped_files = []
        final_df = {}
        while (len(orig_mappings) != 0):
            if first_file:
                df1 = None
                df2 = None
                if mapping["mapping%s" % i]["source_file"] == mapping["mapping%s" % i]["mapping_file"]:
                    raise Exception("Mapping should not be done on the same file")
                if mapping["mapping%s" % i]["source_file"] =='' or mapping["mapping%s" % i]["mapping_file"] =='' or mapping["mapping%s" % i]["source_column"] =='' or mapping["mapping%s" % i]["mapping_column"] =='':
                    raise Exception("Mapping fields not selected or Mapping values are null")
                
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
                                        right_on=mapping["mapping%s" % i]["mapping_column"], how='left', suffixes=('_x', ''))
                except Exception as e:
                    raise Exception("Mapping should be done on same type of columns")
                list_drop_cols = [col for col in final_df.columns if final_df[col].nunique() == 0]
                if len(list_drop_cols) == len(final_df.columns):
                    raise Exception("Mapping should be done on columns having common datapoints")
                else:
                    final_df.drop(list_drop_cols,axis=1, inplace=True)
                #if final_df.shape[0] > df1.shape[0]:
                    #raise Exception("The parent-child relationship is not correct. Please validate.")
                list_mapped_files.append(mapping["mapping%s" % i]["source_file"])
                list_mapped_files.append(mapping["mapping%s" % i]["mapping_file"])
                first_file = False
                del orig_mappings["mapping%s" % i]

            else:
                for m_id, m_data in mapping.items():
                    if mapping[m_id]["source_file"] =='' or mapping[m_id]["mapping_file"] =='' or mapping[m_id]["source_column"] =='' or mapping[m_id]["mapping_column"] =='':
                            raise Exception("Mapping fields not selected or Mapping values are null")
                    if (mapping[m_id]["source_file"] in set(list_mapped_files) or mapping[m_id]["mapping_file"] in set(
                            list_mapped_files)) and m_id in orig_mappings.keys():
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
                            if join =='left':
                                final_df = pd.merge(df1, df2, left_on=mapping[m_id]["source_column"],
                                                    right_on=mapping[m_id]["mapping_column"], how='left')
                            elif join =='right':
                                final_df = pd.merge(df2, df1, left_on=mapping[m_id]["mapping_column"],
                                                    right_on=mapping[m_id]["source_column"], how='left')
                        except Exception as e:
                            raise Exception("Mapping should be done on same type of columns")
                        #if final_df.shape[0] > num_rows:
                            #raise Exception("The parent-child relationship is not correct. Please validate.")
                        list_mapped_files.append(mapping[m_id]["source_file"])
                        list_mapped_files.append(mapping[m_id]["mapping_file"])
                        del orig_mappings[m_id]

        if final_df.shape[0] == 0:
            raise Exception("Mapping should be done on columns having common datapoints")
        list_drop_cols = [col for col in final_df.columns if final_df[col].nunique() == 0]
        if len(list_drop_cols) == len(final_df.columns):
            raise Exception("Mapping should be done on columns having common datapoints")
        else:
            final_df.drop(list_drop_cols,axis=1, inplace=True)
        columns = final_df.columns
        data_t = final_df[columns]
        columns = list(data_t.columns)
        chunks, filesize = file_split(data_t)
        sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
        if auto_retrain:
            size = append_data_chunks(final_df, "PS_IngestedData", correlationId, pageInfo, userId, columns,
                                      sourceDetails, colunivals=None, lastDateDict=lastDateDict)
        else:
            chunks, filesize = file_split(data_t)
            save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId, columns, sourceDetails,
                             colunivals=None, datapre=datapre,lastDateDict=lastDateDict)
        return "multi_file"


def store_pickle_file(multi_source_dfs, correlationId):
    tempFile = saveModelPath + "RawOrginalData" + "_" + correlationId + ".pkl"
    output = open(tempFile, 'wb')
    pickle.dump(multi_source_dfs, output)
    output.close()


def load_pickle_file(correlationId):
    tempFile = saveModelPath + "RawOrginalData" + "_" + correlationId + ".pkl"
    pkl_file = open(tempFile, 'rb')
    mydict2 = pickle.load(pkl_file)
    pkl_file.close()
    return mydict2


'''CHANGES END HERE'''


def save_data_timeseries(correlationId, pageInfo, userId, collection, columns, data=None, datapre=None):
    sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
    t_filesize = (sys.getsizeof(data) / 1024) / 1024
    capacity = 4
    if t_filesize < capacity:
        save_data_chunks([data], collection, correlationId, pageInfo, userId, columns, sourceDetails, colunivals=None,
                         timeseries="one", datapre=None)
        tsize = t_filesize
    else:
        tsize = 0
        for key in data:
            chunks, filesize = file_split(data[key])
            tsize += filesize
            save_data_chunks(chunks, collection, correlationId, pageInfo, userId, columns, sourceDetails,
                             colunivals=None, timeseries=key, datapre=None)
    return tsize


'''
collection="PS_IngestedData"    
corid = '9ec89034-4e3f-4193-9cec-b561fb041464'
corid="56e94516-be22-4939-8b56-6fc035de0ec0"
data_t = data_fro_chunks(corid,collection)
'''


def data_from_chunks(corid, collection, lime=None, recent=None,modelName=None):
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
            temp.update({datetime.strptime(data_json[countj].get('CreatedOn'), '%Y-%m-%d %H:%M:%S'): data_json[countj].get('UploadId')})
            recentdoc = temp.get(max(temp.keys()))
        data_json = dbcollection.find({"CorrelationId": corid, "UploadId": recentdoc})
        count = data_json.collection.count_documents({"CorrelationId": corid, "UploadId": recentdoc})
    else:
        if modelName==None:
            count = data_json.collection.count_documents({"CorrelationId": corid})
        else:
            #count = data_json.collection.count_documents({"CorrelationId": corid,"ModelName":modelName})
            count=list(data_json.collection.find({"CorrelationId": corid,"ModelName":modelName}))
    data_t1 = pd.DataFrame()
    if not lime:
        if not modelName:
            for counti in range(count):
                t = data_json[counti].get('InputData')
                if EnDeRequired :
                    t = EncryptData.DescryptIt(base64.b64decode(t)) 
                data_t = pd.DataFrame(json.loads(t))
                data_t1 = data_t1.append(data_t, ignore_index=True)
        elif modelName:
            for i,_ in enumerate(count):
                t = count[i].get('InputData')
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
            temp = l[1]
            if re.compile(rege1).match(str(temp)) is None:
                for pattern in rege:
                    if re.compile(pattern).match(str(temp)) is not None:
                        # if platform.system() == 'Linux':
                        data_t1[col] = pd.to_datetime(data_t1[col], dayfirst=True, errors='coerce')
                        # elif platform.system() == 'Windows':
                        # data_t1[col]= pd.to_datetime(data_t1[col],errors='coerce')
                        #data_t1.dropna(subset=[col], inplace=True)

    return data_t1




def data_timeseries(corid, collection):
    dbconn, dbcollection = open_dbconn(collection)
    data_json = dbcollection.find({"CorrelationId": corid})
    count = data_json.collection.count_documents({"CorrelationId": corid})
    data_t = {}
    if count == 1:
        data_t = {key: pd.read_json(data_json[0]['InputData'][key]) for key in data_json[0]['InputData']}
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


def insQdb(correlationId, status, progress, pageInfo, userId, modelName=None, problemType=None, UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None):
    Id = str(uuid.uuid4())
    dbconn, dbcollection = open_dbconn("Clustering_IngestData")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    try:
        retrain=data_json[0].get('retrain')
    except:
        retrain=None
    dbconn, dbcollection = open_dbconn('Clustering_StatusTable')
    if not progress.isdigit():
        message = progress
        progress = '0'
    elif status == "C":
        message = "Completed"
    else:
        message = "InProgress"

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
                                   "ModifiedByUser": userId,
                                   "DCUID": "null" if DCUID==None else DCUID,
                                   "ClientID":"null" if ClientID==None else ClientID,
                                   "ServiceID":"null" if ServiceID==None else ServiceID,
                                   "ModelName":"null" if userdefinedmodelname==None else userdefinedmodelname,
                                   "Clustering_type":"null",
                                   "retrain":retrain
                                   }])
    else:
        dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": correlationId,
                                   "ModelType": modelName,
                                   "ProblemType": problemType,
                                   "Message": message,
                                   "UniId": UniId,
                                   "Status": status,
                                   "Progress": progress,
                                   "pageInfo": pageInfo,
                                   "CreatedByUser": userId,
                                   "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                   "ModifiedByUser": userId,
                                   "DCUID": "null" if DCUID==None else DCUID,
                                   "ClientID":"null" if ClientID==None else ClientID,
                                   "ServiceID":"null" if ServiceID==None else ServiceID,
                                   "ModelName":"null" if userdefinedmodelname==None else userdefinedmodelname,
                                   "Clustering_type":"null",
                                   "retrain":retrain}])
    dbconn.close()


def updQdb(correlationId, status, progress, pageInfo, userId, modelName=None, problemType=None, UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,
           retrycount=3):
    dbconn, dbcollection = open_dbconn("Clustering_IngestData")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    
    try:
        for x,y in data_json[0].get('ProblemType').items():
            for a,b in y.items():   
                if b=="True":
                    clustering_type=a
                    break
    except:
        clustering_type=None

    try:
        retrain=data_json[0].get('retrain')
    except:
        retrain=None

    dbconn, dbcollection = open_dbconn('Clustering_StatusTable')
    if not progress.isdigit():
        message = progress
        rmessage = "Error"
        Status = 'E'
        progress = '0'
    elif status == "C":
        message = "Completed"
        rmessage = "Completed"
    else:
        message = "InProgress"
        rmessage = "InProgress"
    try:
        if not modelName:
            if UniId != None:
                data = dbcollection.find({"CorrelationId": correlationId,
                                          "UniId": UniId,
                                          "pageInfo": pageInfo
                                          })
            else:
                data = dbcollection.find({"CorrelationId": correlationId,
                                          "pageInfo": pageInfo})

            dbcollection.update_many({'UniId': data[0].get('UniId'),'pageInfo':pageInfo}, {'$set': {"CorrelationId": correlationId,
                                                                                        "Status": status,
                                                                                        "Progress": progress,
                                                                                        "RequestStatus": rmessage,
                                                                                        "Message": message,
                                                                                        "UniId": UniId,
                                                                                        "pageInfo": pageInfo,
                                                                                        "CreatedByUser": userId,
                                                                                        "ModifiedOn": datetime.now().strftime(
                                                                                            '%Y-%m-%d %H:%M:%S'),
                                                                                        "ModifiedByUser": userId,
                                                                                         "DCUID": "null" if DCUID==None else DCUID,
                                                                                         "ClientID":"null" if ClientID==None else ClientID,
                                                                                         "ServiceID":"null" if ServiceID==None else ServiceID,
                                                                                         "ModelName":"null" if userdefinedmodelname==None else userdefinedmodelname,
                                                                                         "Clustering_type":"null" if clustering_type==None else clustering_type,
                                                                                          "retrain":retrain}})
        else:
             if UniId and modelName==None:
                data = dbcollection.find({"CorrelationId": correlationId,
                                          "pageInfo": pageInfo,
                                          "UniId": UniId})
             elif modelName and UniId==None:
                data = dbcollection.find({"CorrelationId": correlationId,
                                          "pageInfo": pageInfo,
                                          "ModelType": modelName,
                                          "ProblemType": problemType})

             else:
                data = dbcollection.find({"CorrelationId": correlationId,
                                          "pageInfo": pageInfo,
                                          "ModelType": modelName,
                                          "ProblemType": problemType,
                                          "UniId":UniId})
             dbcollection.update({'UniId': data[0].get('UniId'),'ModelType': modelName}, {'$set': {"CorrelationId": correlationId,
                                                                                   "Status": status,
                                                                                   "Progress": progress,
                                                                                   "RequestStatus": rmessage,
                                                                                   "Message": message,
                                                                                   "UniId": UniId,
                                                                                   "pageInfo": pageInfo,
                                                                                   "CreatedByUser": userId,
                                                                                   "ModifiedOn": datetime.now().strftime(
                                                                                       '%Y-%m-%d %H:%M:%S'),
                                                                                   "ModifiedByUser": userId,
                                                                                    "DCUID": "null" if DCUID==None else DCUID,
                                                                                    "ClientID":"null" if ClientID==None else ClientID,
                                                                                    "ServiceID":"null" if ServiceID==None else ServiceID,
                                                                                    "ModelName": "null" if userdefinedmodelname==None else userdefinedmodelname,
                                                                                    "Clustering_type":"null" if clustering_type==None else clustering_type,
                                                                                     "retrain":retrain}})
    except ServerSelectionTimeoutError:

        if retrycount > 0:
            retrycount -= 1
            updQdb(correlationId, status, progress, pageInfo, userId, modelName=modelName, problemType=problemType,
                   UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,retrycount=retrycount)
        else:
            raise ServerSelectionTimeoutError
    ##        with open("abc.txt",'a')as f:
    ##            f.write(str(x) + " documents updated. " +str(modelName)+str(progress)+"\n")
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
    if isinstance(obj, CustomJsonFormatter):
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
            logpath + str('Python_clustering_log') + '_' + str(d.day) + '_' + str(d.month) + '_' + str(d.year) + '.log')
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
        # try:

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
                    temp_a = {"Select Option": "True", "Text": "False", "category": "False", "Id": "False"}
                else:
                    for indx, val in enumerate(dtyp_l):
                        temp_a.update({str(val): ('True' if indx == 0 else 'False')})
                temp_b['Datatype'] = temp_a

        temp_m.update(temp_b)
        temp_m.update({'Unique': str(col_v['percent_unique'])})
        temp_c = {}
        temp_d = {}
        #for x, y in enumerate(col_v['CorelatedWith']):
        #    temp_c.update({str(x): y})
        temp_d['Correlation'] = {}
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
        stratify = None
    try:
        x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=test_size,
                                                            random_state=50, stratify=stratify)
    except ArithmeticError:
        x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=test_size,
                                                            random_state=50, stratify=None)

    return x_train, x_test, y_train, y_test


def getFeatureSelectionVariable(correlationId):
    dbconn, dbcollection = open_dbconn("ME_FeatureSelection")
    data_json = list(dbcollection.find({"CorrelationId": correlationId},
                                       {'_id': 0, "FeatureImportance": 1, "Train_Test_Split": 1, "KFoldValidation": 1,
                                        "StratifiedSampling": 1}))
    selectedFeatures = [each for each in data_json[0]["FeatureImportance"] if
                        data_json[0]["FeatureImportance"][each]['Selection'] == "True"]
    trainTestSplitRatio = (100 - int(data_json[0]["Train_Test_Split"]["TrainingData"])) / 100
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


def getSelectedColumn(featureColumns, dataColumns):
    # selectedColumn = featureColumns
    selectedColumn = [each for each in dataColumns if
                      (each in featureColumns) or (each.startswith(tuple(map(lambda x: x + "_L", featureColumns)))) or (
                          each.startswith(tuple(map(lambda x: x + "_OHE", featureColumns))))]
    return selectedColumn


def insert_EvalMetrics_FI_C(correlationId, modelName, problemType, accuracy, log_loss, TP, TN, FP, FN, sensitivity,
                            specificity, precision, recall, f1score, c_error, ar_score, aucEncoded, featureImportance,
                            timeSpend, pageInfo, userId, modelparams=None, HTId=None, counter=3):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            dbcollection.insert_one({
                "_id": Id,
                "CorrelationId": correlationId,
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
                "AUCImage": aucEncoded,
                "featureImportance": featureImportance,
                "ModelParams": modelparams,
                "ProblemTypeFlag": False,
                "RunTime": timeSpend,
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
                                    "AUCImage": aucEncoded,
                                    "ProblemTypeFlag": False,
                                    "featureImportance": featureImportance,
                                    "RunTime": timeSpend,
                                    "pageInfo": pageInfo,
                                    "CreatedByUser": userId
                                }})
            dbconn.close()

    except (ServerSelectionTimeoutError, NetworkTimeout):

        if counter > 0:
            counter = counter - 1
            insert_EvalMetrics_FI_C(correlationId, modelName, problemType, accuracy, log_loss, TP, TN, FP, FN,
                                    sensitivity, specificity, precision, recall, f1score, c_error, ar_score, aucEncoded,
                                    featureImportance, timeSpend, pageInfo, userId, modelparams=None, HTId=None,
                                    counter=counter)
        else:
            raise ServerSelectionTimeoutError


def insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, report, TP, TN, FP, FN,
                               ConfusionEncoded, accuracy_score, featureImportance, timeSpend, pageInfo, userId,
                               modelparams=None, HTId=None, counter=3):
    try:
        if HTId == None:
            dbconn, dbcollection = open_dbconn("SSAI_RecommendedTrainedModels")
            Id = str(uuid.uuid4())
            dbcollection.insert_one({
                "_id": Id,
                "CorrelationId": correlationId,
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
                                    "ProblemTypeFlag": True,
                                    "pageInfo": pageInfo,
                                    "CreatedByUser": userId
                                }})

            dbconn.close()
    except (ServerSelectionTimeoutError, NetworkTimeout):
        if counter > 0:
            counter = counter - 1
            insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, report, TP, TN, FP, FN,
                                       ConfusionEncoded, accuracy_score, featureImportance, timeSpend, pageInfo, userId,
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
        if counter > 0:
            counter = counter - 1
            insert_MultiClassMetrics_C(correlationId, modelName, pType, matthews_coefficient, accuracy_score, timeSpend,
                                       pageInfo, userId, modelparams=None, HTId=None, counter=counter)
        else:
            raise ServerSelectionTimeoutError


def insert_EvalMetrics_FI_R(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                            featureImportance, timeSpend, pageInfo, userId, modelparams=None, HTId=None, counter=3):
    try:
        if HTId == None:
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
                "featureImportance": featureImportance,
                "RunTime": timeSpend,
                "ModelParams": modelparams,
                "pageInfo": pageInfo,
                "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "CreatedByUser": userId
            })
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
                                    "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                    "CreatedByUser": userId
                                }})
            dbconn.close()
    except (ServerSelectionTimeoutError, NetworkTimeout):

        if counter > 0:
            counter = counter - 1
            insert_EvalMetrics_FI_R(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                                    featureImportance, timeSpend, pageInfo, userId, modelparams=None, HTId=None,
                                    counter=counter)
        else:
            raise ServerSelectionTimeoutError


def insert_EvalMetrics_FI_T(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal, timeSpend,
                            actual, forecasted, RangeTime, selectedFreq, lastDataRecorded, pageInfo, userId,
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
            "pageInfo": pageInfo,
            "LastDataRecorded": lastDataRecorded,
            "LastTrainDateTime": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
            "CreatedByUser": userId
        })
        dbconn.close()
    except ServerSelectionTimeoutError:
        if retrycount > 0:
            retrycount -= 1
            insert_EvalMetrics_FI_T(correlationId, modelName, problemType, r2ScoreVal, rmsVal, maeVal, mseVal,
                                    timeSpend, actual, forecasted, RangeTime, selectedFreq, pageInfo, userId,
                                    retrycount=retrycount)
        else:
            raise ServerSelectionTimeoutError



def save_file(file, file_name, problemType, correlationId, pageInfo, userId, train_cols=None, FileType=None, HTId=None,clustering_modeluniqueId_viz=None,
              retrycount=3):
    if clustering_modeluniqueId_viz==None:
        file_path = saveModelPath + str(correlationId) + '_' + str(file_name) + '.pickle'
    elif clustering_modeluniqueId_viz!=None:
        file_path = saveModelPath + str(correlationId) + '_' +str(clustering_modeluniqueId_viz)+'_'+ str(file_name) + '.pickle'
    Ofile_path = open(file_path, 'wb')
    if problemType != "TimeSeries":
        encryptPickle(file,file_path)
        #pickle.dump(file, Ofile_path)
        cfg = None
    else:
        #pickle.dump(file[0], Ofile_path)
        encryptPickle(file[0], file_path)
        cfg = file[1]
    Ofile_path.close()

    dbconn, dbcollection = open_dbconn('Clustering_SSAI_savedModels')
    try:
        Id = str(uuid.uuid4())
        if Id==clustering_modeluniqueId_viz:
            Id=Id[::-1]
        dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": correlationId,
                                   "HTId": HTId,
                                   "FileName": file_name,
                                   "FilePath": file_path,
                                   "FileType": FileType,
                                   "Configuration": cfg,
                                   "TrainCols": train_cols,
                                   "inputSample": "undefined",
                                   "pageInfo": pageInfo,
                                   "ProblemType": problemType,
                                   "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                                   "CreatedByUser": userId,
                                   "Clustering_ModeluniqueId_Viz":clustering_modeluniqueId_viz}])
    except ServerSelectionTimeoutError:
        if retrycount > 0:
            retrycount -= 1
            save_file(file, file_name, problemType, correlationId, pageInfo, userId, train_cols=train_cols,
                      FileType=FileType, retrycount=retrycount)
        else:
            raise ServerSelectionTimeoutError

    dbconn.close()


def insInputSample(correlationId, data):
    dataToDict = str(data.to_dict('records'))
    dbconn, dbcollection = open_dbconn('SSAI_DeployedModels')
    data = dbcollection.find({"CorrelationId": correlationId})

    dbcollection.update({'_id': data[0].get('_id')}, {'$set': {
        "InputSample": dataToDict,
    }})
    dbconn.close()


def save_file_t(file, file_name, correlationId, pageInfo, userId, problemType=None, FileType=None):
    file_path = saveModelPath + str(correlationId) + '_' + str(file_name) + '.pickle'
    Ofile_path = open(file_path, 'wb')
    pickle.dump(file, Ofile_path)
    Ofile_path.close()

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

def model_viz_rf(correlationId,problemtype,pageInfo):
    dbconn, dbcollection = open_dbconn("Clustering_SSAI_savedModels")
    data_json = list(dbcollection.find({"CorrelationId": correlationId,"ProblemType":problemtype,"pageInfo":pageInfo}))
    if len(data_json)!=0:
        if "Clustering_ModeluniqueId_Viz" in list(data_json[0].keys()):
            clustering_modeluniqueId_viz=data_json[0]['Clustering_ModeluniqueId_Viz']
            return clustering_modeluniqueId_viz,True
        
    else:
        return 0,False

def get_pickle_file(correlationId=None, FileName=None, FileType=None, FilePath=None,clustering_modeluniqueId_viz=None):
    if correlationId != None and FileType != None and FileName == None and clustering_modeluniqueId_viz==None:
        dbconn, dbcollection = open_dbconn('Clustering_SSAI_savedModels')
        data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileType": FileType})
        dbconn.close()
        file_name = data[0].get('FileName')
        ProblemType = data[0].get('ProblemType')
        file_path = data[0].get('FilePath')        
        traincols = None
        loaded_model = decryptPickle(file_path)
        #file = open(file_path, 'rb')
        #loaded_model = pickle.load(file)
    elif correlationId != None and FileType != None and FileName != None and clustering_modeluniqueId_viz==None:
        dbconn, dbcollection = open_dbconn('Clustering_SSAI_savedModels')
        data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileName": FileName,
                                  "FileType": FileType})
        dbconn.close()

        file_name = data[0].get('FileName')
        ProblemType = data[0].get('ProblemType')
        traincols = data[0].get('TrainCols')
        file_path = data[0].get('FilePath')
        loaded_model = decryptPickle(file_path)
        #file = open(file_path, 'rb')
        #loaded_model = pickle.load(file)
    elif correlationId != None and FileType != None and FileName != None and clustering_modeluniqueId_viz!=None:
        dbconn, dbcollection = open_dbconn('Clustering_SSAI_savedModels')
        data = dbcollection.find({"CorrelationId": correlationId,
                                  "FileName": FileName,
                                  "FileType": FileType,
                                  "Clustering_ModeluniqueId_Viz":clustering_modeluniqueId_viz})
        dbconn.close()

        file_name = data[0].get('FileName')
        ProblemType = data[0].get('ProblemType')
        traincols = data[0].get('TrainCols')
        file_path = data[0].get('FilePath')
        loaded_model = decryptPickle(file_path)

    elif (correlationId == None and FileType == None) and FilePath != None:
        file = open(FilePath, 'rb')
        file_name = None
        ProblemType = None
        traincols = None
        loaded_model = decryptPickle(file_path)
        #loaded_model = pickle.load(file)

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
        if not data[0]["TimeSeries"]:
            return None, None
    except KeyError:
        return None, None
    f = {}
    fdict = {int(k): v for k, v in data[0]["TimeSeries"]["Frequency"].items()}
    for each in fdict:
        if fdict[each]["Steps"]:
            f[fdict[each]["Name"]] = fdict[each]["Steps"]
    agg = data[0]["TimeSeries"]["Aggregation"]
    return f, agg


def getDateCol(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data = dbcollection.find({"CorrelationId": correlationId})
    try:
        return data[0]['TimeSeries']['TimeSeriesColumn']  # Inconsistent tab error
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
    dbcollection.update_one({'CorrelationId': correlationId, 'UniqueId': uniqueId}, {'$set': {
        'Status': 'E', 'ErrorMessage': error
    }})
    dbconn.close()


def getRequestParams(correlationId, uniId):
    dbconn, dbcollection = open_dbconn('Clustering_IngestData')
    args = dbcollection.find_one({"CorrelationId": correlationId, "UniId": uniId})
    return args["ParamArgs"],args


def getUniId(correlationId, requestId):
    dbconn, dbcollection = open_dbconn('SSAI_IngrainRequests')
    args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
    return args["UniId"]


def getlastDataPoint(correlationId):
    dbconn, dbcollection = open_dbconn('SSAI_RecommendedTrainedModels')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    return data["LastDataRecorded"]


def append_data_chunks(InstaData, collection, correlationId, pageInfo, userId, columns, sourceDetails, colunivals=None,
                       lastDateDict=None):
    #freq, _ = getTimeSeriesParams(correlationId)
    df = data_from_chunks(corid=correlationId, collection="Clustering_BusinessProblem")
    df = df.append(InstaData, ignore_index=True)
    
    df.sort_index(inplace=True)
    df.drop_duplicates(keep="last", inplace=True)
    df_shape=list(df.shape)
    ingestion_message = "Successfully ingested "+str(df_shape[0])+" records for "+str(df_shape[1])+" attributes"
    chunks, size = file_split(df)
    dbconn, dbcollection = open_dbconn("Clustering_BusinessProblem")
    dbcollection.remove({"CorrelationId": correlationId})
    save_data_chunks(chunks, "Clustering_BusinessProblem", correlationId, pageInfo, userId, columns, sourceDetails=sourceDetails,
                     colunivals=colunivals, lastDateDict=lastDateDict,ingestion_message=ingestion_message,df_shape=df_shape)
    return size


def getlastDataPointCDM(correlationId, *argv):
    dbconn, dbcollection = open_dbconn('Clustering_BusinessProblem')
    data = dbcollection.find_one({"CorrelationId": correlationId})
    lastDate = data["lastDateDict"]
    for arg in argv:
        lastDate = lastDate[arg]
    return (datetime.strptime(lastDate, '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')#lastDate.strftime('%m/%d/%Y')


def save_vectorizer(correlationId, name, vectorizer):
    pickle.dump(vectorizer, open(saveModelPath + '_' + correlationId + '_' + name + '.pkl', 'wb'))


def load_vectorizer(correlationId, name):
    vectorizer = pickle.load(open(saveModelPath + '_' + correlationId + '_' + name + '.pkl', "rb"))
    return vectorizer


'''CLUSTERING CHANGES START'''


def store_cluster_dictionary(cluster, correlationId):
    tempFile = saveModelPath + "cluster" + "_" + correlationId + ".pkl"
    output = open(tempFile, 'wb')
    pickle.dump(cluster, output)
    output.close()


def load_cluster_dictionary(correlationId):
    tempFile = saveModelPath + "cluster" + "_" + correlationId + ".pkl"
    pkl_file = open(tempFile, 'rb')
    mydict2 = pickle.load(pkl_file)
    pkl_file.close()
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
        token=None
    return token,r.status_code

def formVDSAuth():
    try:
        r = requests.post(config['PAD']['tokenAPIUrl'], headers={'Content-Type': 'application/json'},
                          data=json.dumps({"username": config['PAD']['username'],
                                           "password": encryption.decrypt(config['PAD']['password'])}))
    except:
        token = None
    return token, r.status_code


def CustomAuth1(AppId):
    dbconn, dbcollection = open_dbconn('AppIntegration')
    data = dbcollection.find_one({"ApplicationID": AppId})
    #key=['username','Scope']
    creds=data.get('Credentials')
    #for x in key:
    #    del creds[x]
    if data.get('Authentication')=='Azure':
        r= requests.post(data.get('TokenGenerationURL'),headers={'Content-Type':"application/x-www-form-urlencoded"},data=creds)
        if r.status_code==200:
            token=r.json()['access_token']
        
    
    return token,r.status_code

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
    if data.get('Authentication') == 'AzureAD' or data.get('Authentication') == 'Azure':

        r = requests.post(v, headers={'Content-Type': "application/x-www-form-urlencoded"},
                          data=creds)
        if r.status_code == 200:
            token = r.json()['access_token']
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
        r = requests.post(url, data = json.dumps(data_body),headers={'Content-Type': "application/json"})
        if r.status_code == 200:
            token = r.json()['token']
            return token, r.status_code
        else:
            return False, r.status_code
    


def writetocsv(correlationId,tab,section,time):
    with open("executiontime.csv",'a') as f:
        f.write(','.join([correlationId,tab,section,str(time)]))
        f.write('\n')
        
import psutil

def n_jobs_randomsearch(correlationId,modelName,pageInfo):
    if pageInfo=='RecommendedAI':
        try:
            ERT=int(config['GenericSettings']['ERT'])
            cpu_threshold=int(config['GenericSettings']['CPU_THRESHOLD'])
        except ValueError:
            ERT=300
            cpu_threshold=50
            
        dbconn, dbcollection = open_dbconn('ME_RecommendedModels')
        data = dbcollection.find_one({"CorrelationId": correlationId})
        njobs=int(config['GenericSettings']['Njobs'])
        cpu_count=psutil.cpu_count()
        cpu_count=min(cpu_count,njobs)
        cpu_perc=[]
        for i in range(5):
            cpu_perc.insert(i,psutil.cpu_percent(interval=0.3))
            cpu_perc_avg=sum(cpu_perc)/len(cpu_perc)
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg<cpu_threshold and cpu_count<=2) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg>cpu_threshold and cpu_count<=2) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg>cpu_threshold and cpu_count<=2) :
            njobs=1
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg<cpu_threshold and cpu_count<=2):
            njobs=2
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg<cpu_threshold and 2<cpu_count<=4) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg>cpu_threshold and 2<cpu_count<=4) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg>cpu_threshold and 2<cpu_count<=4):
            njobs=1
        if data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg<cpu_threshold and 2<cpu_count<=4:
            njobs=2
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg<cpu_threshold and 4<cpu_count<=8) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg>cpu_threshold and 4<cpu_count<=8) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg>cpu_threshold and 4<cpu_count<=8):
            njobs=1
        if data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg<cpu_threshold and 4<cpu_count<=8:
            njobs=3
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg<cpu_threshold and cpu_count>8) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')<ERT and cpu_perc_avg>cpu_threshold and cpu_count>8) or (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg>cpu_threshold and cpu_count>8):
            njobs=int(0.15*cpu_count)
        if (data.get('SelectedModels').get(modelName).get('EstimatedRunTime')>ERT and cpu_perc_avg<cpu_threshold and cpu_count>8):
            njobs=int(0.4*cpu_count)
    
    return njobs

def n_jobs(pageInfo,correlationId=None,modelName=None):
    if config['GenericSettings']['Override']=='True':
        try:
            njobs=int(config['GenericSettings']['Njobs'])
            cpu_count=psutil.cpu_count()
            njobs=min(cpu_count,njobs)
        except ValueError:
            njobs=1
        return njobs
    
    if pageInfo=='RecommendedAI':
        njobs=n_jobs_randomsearch(correlationId,modelName,pageInfo)
    else:
         try:
            cpu_threshold=int(config['GenericSettings']['CPU_THRESHOLD'])
         except ValueError:
            cpu_threshold=50
         njobs=int(config['GenericSettings']['Njobs'])
         cpu_count=psutil.cpu_count()
         cpu_count=min(cpu_count,njobs)
         cpu_perc=[]
         for i in range(5):
            cpu_perc.insert(i,psutil.cpu_percent(interval=0.3))
            cpu_perc_avg=sum(cpu_perc)/len(cpu_perc)
        
         if  cpu_perc_avg>cpu_threshold and cpu_count<=2 :
            njobs=1
         if cpu_perc_avg<cpu_threshold and cpu_count<=2:
             njobs=2
         if cpu_perc_avg>cpu_threshold and 2<cpu_count<=4:
            njobs=1
         if cpu_perc_avg<cpu_threshold and 2<cpu_count<=4:
             njobs=2
         if cpu_perc_avg>cpu_threshold and 4<cpu_count<=8:
            njobs=2
         if cpu_perc_avg<cpu_threshold and 4<cpu_count<=8:
             njobs=3
         if cpu_perc_avg>cpu_threshold and cpu_count>8:
            njobs=2
         if cpu_perc_avg<cpu_threshold and cpu_count>8:
             njobs=int(0.4*cpu_count)
             
    return njobs
            
def getUniqueIdentifier(correlationId):
    dbconn, dbcollection = open_dbconn("PS_BusinessProblem")
    data_json = dbcollection.find({"CorrelationId": correlationId})
    dbconn.close()
    return data_json[0].get('TargetUniqueIdentifier')        
   
            
    #return njobs
def getEntityArgs(invokeIngestData,entity,start,end,method,delType):
    if method == 'AGILE' and delType == "Agile":
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


def CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId=None):
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':

        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                  auth=HttpNegotiateAuth())
    if resp.status_code == 200:
        if resp.text == "No data available":
            if auto_retrain:
                entityDfs[entity] = "No incremental data available"
            else:
                entityDfs[entity] = "Data is not available for your selection"
        else:
            if resp.json()['TotalRecordCount'] != 0:
                try:
                    maxdata = maxdatapull(correlationId)
                except:
                    maxdata = 30000
                x = 1
                nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']

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
                elif entityDataframe.shape[0] <= min_data() and not auto_retrain:                    				
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
    return



def IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId):
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
        if resp.status_code == 200:
            api_error_flag = False
            if resp.text != "No data available":
                if resp.json()['TotalRecordCount'] != 0:
                    try:
                        maxdata = maxdatapull(correlationId)
                    except:
                        maxdata = 30000
                    x = 1
                    nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']

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
        '''
        val = "value"
        dispName = "displayname"
        glist = set(df[dispName].dropna().unique())
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

def TransformEntities(MultiSourceDfs,cid,dcuid,EntityMappingColumns,parent,auto_retrain):
    min_df = min_data() 
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])
    uids = [p.replace("externalid","uid") for p in EntityMappingColumns ]
    #productinstance = set([p.replace("externalid","productinstances_productinstanceuid") for p in EntityMappingColumns ])
    MappingColumns = {}
    for k,v in MultiSourceDfs['Entity'].items():
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
#            if k != "CodeBranch" :
#                prdctid = list(set(productinstance).intersection(v.columns))[0]
#            else:
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
            else:
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

    return MultiSourceDfs, MappingColumns

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
      
    metaAPIResults = requests.post(metadataAPI,data=json.dumps(args),
                                                       headers={'Content-Type':'application/json',
                                                            'Authorization': 'bearer {}'.format(token),
                                                            'AppServiceUId':config['ClientNative']['AppServiceUId']},
                                                      params={"clientUId": cid, 
                                                               "deliveryConstructUId":dcuid}
                                                           )
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
                    values_dict[values[i]['ProductPropertyValueUId']] = values[i]['EntityPropertyIdValue']
                    cdm_dict[values[i]['ProductPropertyValueUId']] = values[i]['EntityPropertyValue']
  
                key_value[item['DisplayName']] = values_dict
                cdm_value[item['DisplayName']] = cdm_dict
                
        
 
        return key_value,cdm_value
    else:
        return False,metaAPIResults.status_code





def transformCNV(df,cid,dcuid,entity,WorkItem = False):
    import uuid
    token,status_code = getEntityToken()
    if WorkItem:
        key_value,cdm_value = getClientNativeValues(df,cid,dcuid,entity,token,WorkItem = True)
        if key_value != False:
            df['StateUID'] = df['StateUID'].dropna().map(lambda a:  str(uuid.UUID(a)))
            df['StateUID'] = df['StateUID'].astype(str).replace(cdm_value['State'])
    else:
         key_value,cdm_value = getClientNativeValues(df,cid,dcuid,entity,token,WorkItem = False)
         NvList = ['PhaseDetected', 'PhaseInjected', 'Priority', 'Reference', 'Severity', 'State']
         if key_value != False:
             for col in NvList: 
                 if col in key_value.keys(): 
                     k = key_value[col]
                     c = cdm_value[col] 
                     col = col.lower()+"uid"
                     if len(k) !=0 and col in  df.columns:
                         df[col] = df[col].dropna().map(lambda a:  str(uuid.UUID(a)))
                         df[col+"CNV"] = df[col]
                         df[col] = df[col].astype(str).replace(c)
    return df																	

def update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId):
    dbconn, dbcollection = open_dbconn("DataSet_IngestData")
    data_json = list(dbcollection.find({"DataSetUId": DataSetUId}))
    dbconn.close()
    if len(data_json)!= 0 :
        dbconn_ps_ingestdata, dbcollection_ps_ingestdata = open_dbconn("Clustering_BusinessProblem")       
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
    dbconn, dbcollection = open_dbconn("Clustering_BusinessProblem")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    
    if len(data_json)!= 0:
        if 'DataSetUId' in data_json[0]:
            if data_json[0].get('DataSetUId') != 'null':
                return data_json[0].get('DataSetUId')
            else:
                return False
        else:
            return False
    else:
        return False

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
    else:
        MultiSourceDfs = customdataDfs

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
                        #else:
                         #   if entityArgs['EntityUId'] != "00040020-0200-0000-0000-000000000000":
                          #      entityn = "entitynew"
                           #     assocColumn = entityn.lower()+"associations"
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
                        else:
                            if entityArgs['EntityUId'] != "00040020-0200-0000-0000-000000000000":
                                for col in customdataDfs.columns:
                                    if "associations" in col or "extensions" in col :
                                        del customdataDfs[col]
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
    return customdataDfs

def open_phoenixdbconn():
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection[config['DBconnection']['PhoenixDB']]
    return db

def UsecaseDefinitionDetails_forlarge_file(df,datasetUID,userId):
    num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
    RecordCount = str(df.shape[0])
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
    dbconn, dbcollection = open_dbconn('DataSetInfo')
    dbcollection.update_many({"DataSetUId": datasetUID},
                              {"$set" :{"RecordCount":RecordCount,
                                        "UniquenessDetails" : unPercent,
                                        "UniqueValues" : unValues,
                                        "ValidRecordsDetails" : t1}},upsert=True)
    
    

    return df
def data_from_chunks_offline_utility(corid, collection, lime=None, recent=None,DataSetUId=None):
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
            temp = l[1]
            if re.compile(rege1).match(str(temp)) is None:
                for pattern in rege:
                    if re.compile(pattern).match(str(temp)) is not None:
                        # if platform.system() == 'Linux':
                        data_t1[col] = pd.to_datetime(data_t1[col], dayfirst=True, errors='coerce')
                        # elif platform.system() == 'Windows':
                        # data_t1[col]= pd.to_datetime(data_t1[col],errors='coerce')
                        # data_t1.dropna(subset=[col], inplace=True)

    return data_t1
def check_encrypt_for_offlineutility(DataSetUId):
    dbconn_datasetinfo,dbcollection_datasetinfo = open_dbconn('DataSetInfo')
    data_json_datasetinfo = list(dbcollection_datasetinfo.find({"DataSetUId": DataSetUId}))
    EnDeRequired = data_json_datasetinfo[0].get('DBEncryptionRequired')
    dbconn_datasetinfo.close()
    return (EnDeRequired)



def maxdatapull(correlationId):
    dbconn, dbcollection = open_dbconn('Clustering_IngestData')
    if correlationId:
        args = dbcollection.find_one({"CorrelationId": correlationId})
        if "MaxDataPull" in args:
            data_points = args["MaxDataPull"]
        else:
            data_points = 30000
    return int(data_points)