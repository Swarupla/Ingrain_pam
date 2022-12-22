# -*- coding: utf-8 -*-
"""
Created on Tue Jan 12 17:59:10 2021

@author: bhavitha.kusam
"""
import time
start = time.time()
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import platform
import concurrent.futures
import multiprocessing
import requests
import json
import base64

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'

import configparser, os
mainPath = os.getcwd() + work_dir
import sys 

sys.path.insert(0, mainPath)
import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
        
from SSAIutils import utils
from SSAIutils import EncryptData
from datetime import datetime,timedelta
from pandas import Timestamp


#from SSAIutils import encryption
import sys
import pandas as pd
import os

Incremental = False

RequestId = sys.argv[1]
DataSetUId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
end = time.time()
def checkflag():
    dbconn, dbcollection = utils.open_dbconn('SSAI_IngrainRequests')
    args = dbcollection.find_one({"DataSetUId": DataSetUId})
    try:
        paramArgs = eval(args["ParamArgs"])
    except:
        paramArgs = {}
    if Incremental in paramArgs and paramArgs["Incremental"] =='True':
        return True
    else:
        return False 
    
def getlasttraineddate():
    dbconn_datasetinfo,dbcollection_datasetinfo = utils.open_dbconn('DataSetInfo')
    args = dbcollection_datasetinfo.find_one({"DataSetUId": DataSetUId})
    lasttraineddate = (datetime.strptime(args['LastModifiedDate'], '%Y-%m-%d %H:%M:%S')).strftime('%m/%d/%Y')
    return lasttraineddate
    
def generate_azure_token(url, credentials):    
    headers = {
        "Content-Type": "application/x-www-form-urlencoded"
    }
    payload = {        
        "grant_type": credentials['grant_type'],
        "resource": credentials['resource'],
        "client_id": credentials['client_id'],
        "client_secret": credentials['client_secret']
    }
    try:
        r = requests.post(url, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None
    return token, r.status_code

def exec_post_request(url, headers, body, token, page_number):
    body.update({'PageNumber':page_number})
    #print(headers)
    if headers == '{}':
        headers.update({'Authorization':'Bearer {}'.format(token)})
        r = requests.post(url, headers=headers, data=body)

    else:
        #token ='eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiJhcGk6Ly83ZjkzOTUyMy0xN2UwLTQ1M2ItOTAzNy1mNzlmNTAzNmNhOTYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8zODEyYWU1ZS1jZDRhLTQ1ZTQtOGVmYy1lNjE3YjhlN2M1ZGIvIiwiaWF0IjoxNjEzNjUxNzExLCJuYmYiOjE2MTM2NTE3MTEsImV4cCI6MTYxMzY1NTYxMSwiYWNyIjoiMSIsImFpbyI6IkUyWmdZTWprbjNLMjB6enZxRzd4SmNzRkN1RVREaDVjTHpFdHRWM0M2VEViNjc3L2g4TUIiLCJhbXIiOlsicHdkIl0sImFwcGlkIjoiMmIwYWUxNTMtZGNiYS00MDFjLWJkOTYtMjc5ZDY5NWU0NmVhIiwiYXBwaWRhY3IiOiIwIiwiaXBhZGRyIjoiMTAzLjEwLjEzMy44NyIsIm5hbWUiOiJTeXN0ZW1EYXRhIEFkbWluIiwib2lkIjoiZDk0MDQ1YWItYjU5OS00YjUzLTkwNjQtZGU1MGQ3Y2JmODRlIiwicmgiOiIwLkFBQUFYcTRTT0VyTjVFV09fT1lYdU9mRjIxUGhDaXU2M0J4QXZaWW5uV2xlUnVvc0FIcy4iLCJzY3AiOiJEZXZVVF9BSV9BUEkiLCJzdWIiOiJIMVpsdm11ZVc3UUozckpERmgwcVJwbE9jOG9pWENaejZlN2xPaDhlUjAwIiwidGlkIjoiMzgxMmFlNWUtY2Q0YS00NWU0LThlZmMtZTYxN2I4ZTdjNWRiIiwidW5pcXVlX25hbWUiOiJteXdpemFyZHN5c3RlbWRhdGFhZG1pbkBtd3Bob2VuaXgub25taWNyb3NvZnQuY29tIiwidXBuIjoibXl3aXphcmRzeXN0ZW1kYXRhYWRtaW5AbXdwaG9lbml4Lm9ubWljcm9zb2Z0LmNvbSIsInV0aSI6ImNRcUk0X0ZCVVVpcmdjM3QyMDJ1QUEiLCJ2ZXIiOiIxLjAifQ.DouTuNDNBq9VlwAexpyAa0CtnyYpxF6T0k5ghnDppsvwVMj6h6P9RGoJN38IqX8AgMLqkeCZZnoVmEr7THBMi6FfkpfCzCgl28d_yZIMN-u5zLD2MnCVuACDJWlvXlojHfuDOSfKngZnTKA58-4gPDORgCcmI7NONFw7wT5RhJ6zT3zcpKXdSDBv1guFr_18L4VedT495975iZcUYN8itY2EK4oTxoa6f7wNio0xpFsffy3ILDMkIAOezUer2xVIS5ACj71-8DDG0QPPXcMZGQPw8NzBJe6Jar44MwH_XlvLzzTAqp6n0m8W4hz-ulmA3eOgs165mYa6dtK3mXieFw'
        r = requests.post(url, headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(token)}, data=json.dumps(body))
        #print(r)
    return r
	
try:
    utils.updQdbbyDataSetUId(DataSetUId, 'P', '20', pageInfo, userId,RequestId)
    logger = utils.logger('Get',DataSetUId)
    utils.logger(logger, DataSetUId, 'INFO', ('import took '  + str(end-start)+' secs'+" with CPU: "+cpu+" Memory: "+memory),str(RequestId))
    utils.logger(logger, DataSetUId, 'INFO', ('Large data pull Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(RequestId))
    dbconn,dbcollection = utils.open_dbconn('DataSetInfo')
    dbcollection.update_one({"DataSetUId":DataSetUId},{'$set':{         
                                    'Status' : 'P','Message':"in progress"                                                      
                                   }}) 
    data_json_datasetinfo = list(dbcollection.find({"DataSetUId": DataSetUId}))

    ClientUID = data_json_datasetinfo[0].get('ClientUId')
    DeliveryConstructUId = data_json_datasetinfo[0].get('DeliveryConstructUId')
    dbconn.close()
    SourceDetails,SourceName = utils.getFilePath(DataSetUId)
    #print("sourcedeatils",SourceDetails)
    MultiSourceDfs = {}
    fileDfs = {}
    min_df =utils.min_data()
    if SourceName == 'File':
        try:
            FileDetail = SourceDetails['FileDetail']
            for i in range(len(FileDetail)):
                filepath = FileDetail[i]['FilePath']
                #print("testing1",filepath)
                #if platform.system() == 'Linux':                
                    #filepath = filepath.replace('\\', '//')
                    #filepath = eval(filepath)
                    #print("testing1",filepath)
                if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
                    try:
                        data_t = utils.read_csv(filepath)
                    except:
                        with open(filepath, newline='', encoding='cp1252') as csvfile:
                            data_t = pd.read_csv(csvfile)
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                    elif data_t.shape[0] <= min_df:
                        fileDfs[filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                    else:
                        fileDfs[filepath] = data_t
                elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):
                    data_t = utils.read_excel(filepath)
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                    elif data_t.shape[0] <= min_df:
                        fileDfs[filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                    else:
                        fileDfs[filepath] = data_t
                MultiSourceDfs['Large data'] = fileDfs

        except Exception as e:
             FileDetail = SourceDetails['FileDetail']
             filepath = FileDetail[0]['FilePath']
             filepath = filepath.replace('\\', '//')
             fileDfs[filepath] = e
             MultiSourceDfs['Large data'] = fileDfs

    else:
       MultiSourceDfs['Large data'] = {}
    if SourceName == 'ExternalAPI':
        Incremental = checkflag()
        externalapi_details = SourceDetails['ExternalAPIDetail']
        source_url = externalapi_details['Url']
        headers = externalapi_details['Headers']
        body_params = externalapi_details['Body']
        if Incremental:
            lasttraineddate = getlasttraineddate()
            body_params.update({'StartDate':lasttraineddate})
            body_params.update({'EndDate':str(datetime.now().strftime('%m/%d/%Y')) })
        auth_type = externalapi_details['AuthType']
        if auth_type == 'Token':
            bearer_token = externalapi_details['Token']
        elif auth_type == 'AzureAD':
            azure_url = externalapi_details['AzureUrl']
            azure_cred = externalapi_details['AzureCredentials']
            x = base64.b64decode(azure_url) #En.............................
            azure_url_decrypt = (EncryptData.DescryptIt(x))
            x = base64.b64decode(azure_cred) #En.............................
            azure_cred_decrypt = eval(EncryptData.DescryptIt(x))	
            bearer_token,_ = generate_azure_token(azure_url_decrypt, azure_cred_decrypt)
        else:
            bearer_token= None
        
        #fetching data from url
        response = exec_post_request(source_url, headers, body_params, bearer_token, '0')
        if response.status_code == 200:
            response_data = response.json()
            try:
                if response_data['ActualData'] == []:
                    if Incremental:
                        MultiSourceDfs['API'] = {"Custom":"Incremental data is not present"}
                    else:
                        MultiSourceDfs['API'] = {"Custom":"Data is not present"}
                else:					
                    actual_data = pd.DataFrame.from_dict(response_data['ActualData'])
                    total_pages = int(response_data['TotalPageCount'])
                    if total_pages > 1:
                        ncores = multiprocessing.cpu_count()
                        with concurrent.futures.ThreadPoolExecutor(max_workers=ncores*5) as executor:
                            future_to_url = {executor.submit(
								exec_post_request, 
								source_url, headers, 
								body_params, 
								bearer_token,
								i) for i in range(1, total_pages+1)}
                        for future in concurrent.futures.as_completed(future_to_url):
                            try:
                                res = future.result()
                                if(res.status_code == 200):
                                    response_data = res.json()
                                    new_data = pd.DataFrame.from_dict(response_data['ActualData'])
		
                                    actual_data = pd.concat([new_data,actual_data],ignore_index=True)
                                    actual_data.drop_duplicates(inplace=True)
										
                            except Exception as exc:
                                print('exception: %s' % (exc))
										   
                    MultiSourceDfs['API'] = {"Custom":actual_data} 
            except Exception as e:
                MultiSourceDfs['API'] = {"Custom":e}
        else:
            MultiSourceDfs['API'] = {"Custom":"Getting "+ str(response.status_code)+" error from API"}		
    else:
        MultiSourceDfs['API'] = {}
    MultiSourceDfs['metricDf'] = {}   
    MultiSourceDfs['Entity'] = {}
    MultiSourceDfs['VDSInstaML'] = {}
    MultiSourceDfs['File'] = {}
    MultiSourceDfs['Custom'] = {}
    MultiSourceDfs['CustomMultipleFetch'] = {}

    message = utils.save_data_multi_file(correlationId=None, pageInfo=pageInfo,requestId=RequestId, userId=userId, multi_source_dfs=MultiSourceDfs, g={},parent={'Name':'null','Type':'File'}, mapping={},
                                             mapping_flag=False, ClientUID=ClientUID, DeliveryConstructUId=DeliveryConstructUId, insta_id='', auto_retrain=False,Incremental=Incremental,
                                             datapre=None, lastDateDict={},MappingColumns= {},previousLastDate = {},DataSetUId=DataSetUId)
    utils.updQdbbyDataSetUId(DataSetUId, 'C', '100', pageInfo, userId,RequestId)
    utils.logger(logger, DataSetUId, 'INFO', ('Large data pull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(RequestId))

except Exception as e:
    logger = utils.logger('Get',DataSetUId)
    utils.logger(logger,DataSetUId,'ERROR','Trace',str(RequestId))
    utils.updQdbbyDataSetUId(DataSetUId, 'E', e.args[0], pageInfo, userId,RequestId)
    dbconn,dbcollection = utils.open_dbconn('DataSetInfo')
    dbcollection.update_one({"DataSetUId":DataSetUId},{'$set':{         
                                    'Status' : 'E','Message':e.args[0]                                                      
                                   }}) 
    dbconn.close()
    
