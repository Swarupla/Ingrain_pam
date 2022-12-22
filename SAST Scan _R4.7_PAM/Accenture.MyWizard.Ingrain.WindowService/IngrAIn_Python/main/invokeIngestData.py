import time
start = time.time()
import platform
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    conf_path_pheonix = '/IngrAIn_Python/main/pheonixentityconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    conf_path_pheonix = '\IngrAIn_Python\main\pheonixentityconfig.ini'
    work_dir = '\IngrAIn_Python'
    from requests_negotiate_sspi import HttpNegotiateAuth

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
EntityConfig = configparser.RawConfigParser()
EntityConfigpath = str(os.getcwd()) + str(conf_path_pheonix)
EntityConfig.read(EntityConfigpath)							 
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from dateutil import relativedelta
from SSAIutils import utils
#from SSAIutils import encryption
import sys
import pandas as pd
import numpy as np
import json
from datetime import datetime,timedelta
import os
import requests
from urllib.parse import urlencode
from collections import ChainMap
from SSAIutils import EncryptData
from SSAIutils import AggregateAgileData				
import base64
import multiprocessing 
import concurrent.futures
import math


correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
if len(sys.argv) == 5:
    genericflow = False
elif len(sys.argv) > 5 and sys.argv[5] == 'True':
    genericflow = True
else:
    genericflow = False

end = time.time()
min_df = utils.min_data()
min_data_VDS = utils.mindatapointsforVDS()

def evaluateModel(correlationid_list,request_list):
    try:
        sys.argv[1] = correlationid_list
        sys.argv[2] = request_list
        sys.argv[3] = "DataCleanUp"
        from main import invokeDataCleanup
        return "Sucess"
    except:
        return "error"

def getcust(inid):
    from uuid import UUID
    newuuid=UUID(str(inid)).bytes
    return Binary(bytes(bytearray(newuuid)), UUID_SUBTYPE)

try:
    Incremental = False    
    # make Db connection get input sinf=gle payload details from "Ingrain Requests table"
    if not genericflow:
        invokeIngestData, _,allparams = utils.getRequestParams(correlationId, requestId)
    else:
        invokeIngestData, _,allparams = utils.getRequestParams_generic_flow(correlationId)
	
    
    #utils.updQdb(correlationId, 'P', '5', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
    logger = utils.logger('Get', correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took :' + str(end-start) + ' secs'+" with CPU: "+cpu+" Memory: "+memory),str(requestId))
    
    invokeIngestData = eval(invokeIngestData)
 
    parent = invokeIngestData['Parent']   
    mapping = invokeIngestData['mapping']
    mapping_flag = invokeIngestData['mapping_flag']
    ClientUID = invokeIngestData['ClientUID']
    DeliveryConstructUId = invokeIngestData['DeliveryConstructUId']
    insta_id = ''
    auto_retrain = False
    #Incremental = False
    global lastDateDict
    lastDateDict = {}
    previousLastDate = None
    auth_type = utils.get_auth_type()
    
    if invokeIngestData['Flag'] == "AutoRetrain":
        auto_retrain = True
    if invokeIngestData['Flag'] == "Incremental":
        Incremental = True
    	
    MappingColumns = {}
    if 'DataSetUId' in allparams:
        DataSetUId = allparams['DataSetUId']
    else:
        DataSetUId = False
    	
    MappingColumns = {}
    if DataSetUId:
        message = utils.update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId)
        utils.update_usecasedefeinition(DataSetUId,correlationId)
        if message == "Ingestion completed":
            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data: Source Type :' + str(message)+  str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
            if not genericflow:
                utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            else:
                utils.updQdb(correlationId, 'P', '10', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
                data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                dbconn.close()
                currentUniqueId = data_json[0]["TargetUniqueIdentifier"]
                ProblemType = data_json[0]["ProblemType"]
                dbconn, dbcollection = utils.open_dbconn("PS_UsecaseDefinition")
                data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                dbconn.close()
                if ProblemType != "TimeSeries":
                    targetuniquessness = data_json[0]["UniquenessDetails"][currentUniqueId]
                else: 
                    targetuniquessness = {}
                    targetuniquessness["Percent"] = 100
                if targetuniquessness["Percent"] > 90:
                    #call datacleanup
                    import uuid
                    dbconn, dbcollection = utils.open_dbconn("SSAI_IngrainRequests")
                    data_json = list(dbcollection.find({"CorrelationId": correlationId, "RequestId": requestId}))
                    utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                    requestId = str(uuid.uuid4())
                    dbcollection.insert({
                                    "AppID": data_json[0].get('AppID'),
                                    "CorrelationId": correlationId,
                                    "IsRetrainedWSErrorModel": data_json[0].get('IsRetrainedWSErrorModel'),
                                    "PythonProcessID": data_json[0].get('PythonProcessID'),
                                    "IsFMVisualize": data_json[0].get('IsFMVisualize'),
                                    "FMCorrelationId": data_json[0].get('FMCorrelationId'),
                                    "DataSetUId": data_json[0].get('DataSetUId'),
                                    "ApplicationName": data_json[0].get('ApplicationName'),
                                    "ClientId": data_json[0].get('ClientId'),
                                    "DeliveryconstructId": data_json[0].get('DeliveryconstructId'),
                                    "DataPoints": data_json[0].get('DataPoints'),
                                    "ProcessId": data_json[0].get('ProcessId'),
                                    "UseCaseID": data_json[0].get('UseCaseID'),
                                    "RequestStatus": "In - Progress",
                                    "RetryCount": data_json[0].get('RetryCount'),
                                    "Frequency": data_json[0].get('Frequency'),
                                    "InstaID": data_json[0].get('InstaID'),
                                    "UniId": data_json[0].get('UniId'),
                                    "TemplateUseCaseID": data_json[0].get('TemplateUseCaseID'),
                                    "Category": data_json[0].get('Category'),
                                    "CreatedByUser": data_json[0].get('CreatedByUser'),
                                    "ModifiedByUser": data_json[0].get('ModifiedByUser'),
                                    "PyTriggerTime": data_json[0].get('PyTriggerTime'),
                                    "LastProcessedOn": data_json[0].get('LastProcessedOn'),
                                    "EstimatedRunTime": data_json[0].get('EstimatedRunTime'),
                                    "ClientID": data_json[0].get('ClientID'),
                                    "DCID": data_json[0].get('DCID'),
                                    "TriggerType": data_json[0].get('TriggerType'),
                                    "AppURL": data_json[0].get('AppURL'),
                                    "SendNotification": data_json[0].get('SendNotification'),
                                    "IsNotificationSent": data_json[0].get('IsNotificationSent'),
                                    "NotificationMessage": data_json[0].get('NotificationMessage'),
                                    "TeamAreaUId": data_json[0].get('TeamAreaUId'),
                                    "RetrainRequired": data_json[0].get('RetrainRequired'),
                                    "_id": str(uuid.uuid4()),
                                    "RequestId": requestId,
                                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                    "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                    "ModelName": "null",
                                    "ProblemType": "null",
                                    "Status": "P",
                                    "Message": "In - Progress",
                                    "Progress": "5",
                                    "pageInfo": "DataCleanUp",
                                    "ParamArgs": "{}",
                                    "Function": "DataCleanUp"
                                    })
                    dbconn.close()
                    utils.updateautotrain_record(correlationId,"DataCleanUp","25")
                    sys.argv[2] = requestId
                    sys.argv[3] = "DataCleanUp"
                    from main import invokeDataCleanup
                else:
                    utils.updQdb(correlationId, 'E', 'Unique Identifier is having less than 90% uniqueness', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)

        else:
            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data: Source Type :' + str(message)+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
            utils.updQdb(correlationId, 'E', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            
    else:
        if mapping_flag == 'False' or (mapping_flag =='True' and (Incremental or auto_retrain)):
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # Metrics data implementation
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            MultiSourceDfs = {}
            if invokeIngestData['metric'] != 'null':
                utils.logger(logger, correlationId, 'INFO', ('Metric pull started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

                metricDfs = {}
                x = eval(invokeIngestData['metric'])
    #            interval = x['interval']
                # Autoretrain
                metricArgs = {
                    "ClientUID"            : invokeIngestData["ClientUID"],
                    "DeliveryConstructUId" : [invokeIngestData["DeliveryConstructUId"]],
                    "MetricCode"           : x["metrics"],
                    "PageNumber"           : "1",
                    "TotalRecordCount"     : "0",
                    "BatchSize"            : "5000"
    #           "IterationTypeUIds"       : [x["Granularity"]]
                }

                if auto_retrain:
                    lastDate = utils.getlastDataPointCDM(correlationId, "metricDf")
                    previousLastDate = lastDate
                    metricArgs.update({
                        "FromDate": x["endDate"],
                        "ToDate": datetime.today().strftime('%Y-%m-%d')
                    })
                elif Incremental:
                    metricArgs.update({
                        "FromDate": datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%Y-%m-%d'),
                        "ToDate": datetime.today().strftime('%Y-%m-%d')
                    })
                else:
                    metricArgs.update({
                        "FromDate": x["startDate"],
                        "ToDate": x["endDate"]
                    })
                metricsUrl = config['METRIC']['metricApiUrl']
                #userEmailId = config['METRIC']['username']
                if auth_type == 'AzureAD':
                    metricAccessToken,status_code = utils.getMetricAzureToken()
                elif auth_type == 'WindowsAuthProvider':
                    metricAccessToken = ''
                    status_code =200
            
                if status_code != 200:
                    metricDfs['Metric'] = "Phoenix API is returned " + str(status_code) + " code, Error Occured while calling Phoenix API"  
                else:
                    meticData, metricDfs = utils.MetricFunction(metricArgs,metricDfs,metricsUrl,metricAccessToken,correlationId)
                    if metricDfs == {}:
                        if meticData.empty:
                            if auto_retrain:
                                metricDfs["Metric"] = "No incremental data available"
                            else:
                                metricDfs["Metric"] = "Data is not available for your selection"  # Error case 2
                        elif meticData.shape[0] <= min_df:
                            metricDfs[
                                    "Metric"] ="Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."  # Error case 3 # Error case 3
                        else:
                            metricDfs["OriginalDf"] = meticData
                            meticData['ProcessedOn'] = pd.to_datetime(meticData['ProcessedOn'], format="%Y-%m-%d %H:%M",
                                                                          exact=True)
                            meticData['Date'] = meticData['ProcessedOn'].apply(lambda x: pd.to_datetime(str(x)[:13]))
                            lastDateDict["metricDf"] = meticData['Date'].max().strftime('%Y-%m-%d %H:%M:%S')
                            columns = ['WorkItemType', 'WorkItemTypeUId', 'MetricUId',
                                           'ProcessAreaUId', 'TeamAreaUId', 'TimePeriodUId']
                            meticData['indexKey'] = meticData.Date.astype(str) + \
                                                        meticData.ReleaseUId.astype(str) + \
                                                        meticData.IterationUId.astype(str)

                            meticData.drop(columns=columns, inplace=True,errors = "ignore")
                            meticData.index = meticData['indexKey']
                            columns = ['ProcessedOn', 'ReleaseUId', 'IterationUId']
                            for t in meticData.MetricCode.unique():
                                columns.append(t + "_" + "Value")
                                columns.append(t + "_" + "RAG")
                            finalMetricData = pd.DataFrame(data=None, columns=columns, index=meticData.index)

                            pd.set_option('mode.chained_assignment', None)
                            meticData['indexKey'] = meticData.index
                            for t in meticData.to_dict('records'):
                                col1 = str(t['MetricCode']) + "_" + "Value"
                                col2 = str(t['MetricCode']) + "_" + "RAG"
                                col3 = 'ReleaseUId'
                                col4 = 'ProcessedOn'
                                col5 = 'IterationUId'
                                finalMetricData[col1][t['indexKey']] = t['Value']
                                finalMetricData[col2][t['indexKey']] = t['RAG']
                                finalMetricData[col3][t['indexKey']] = t['ReleaseUId']
                                finalMetricData[col4][t['indexKey']] = t['ProcessedOn']
                                finalMetricData[col5][t['indexKey']] = t['IterationUId']
                            finalMetricData = finalMetricData.replace('',np.nan)
                            metricDfs["Metric"] = finalMetricData
                            finalMetricData.fillna("",inplace=True)
                            finalMetricData["UniqueRowID"] = finalMetricData['ReleaseUId'] +  finalMetricData['ProcessedOn'].astype(str) + finalMetricData['IterationUId'] ####### changesssssss
                            finalMetricData.rename(columns={'ProcessedOn': 'DateColumn'}, inplace = True)
                            finalMetricData['DateColumn'] = pd.to_datetime(finalMetricData['DateColumn'],errors = 'coerce')
                utils.logger(logger, correlationId, 'INFO', ('Metric pull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                            

                MultiSourceDfs["metricDf"] = metricDfs
            else:
                MultiSourceDfs['metricDf'] = {}
            utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
        
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # Entities  Implementation
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if invokeIngestData['pad'] != 'null':
                global entityDfs 
                entityDfs = {}
                lastDateDict['Entity'] = {}
                x = eval(invokeIngestData['pad'])
                auth_type = utils.get_auth_type()
                if auth_type == 'AzureAD':
                    entityAccessToken,status_code = utils.getMetricAzureToken()
                elif auth_type == 'WindowsAuthProvider':
                    entityAccessToken = ''
                    status_code =200
                utils.logger(logger, correlationId, 'INFO', ('Entity pull Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                
                if status_code != 200:
                    entityDfs['Entity'] = "Phoenix API is returned " + str(status_code) + " code, Error Occured while calling Phoenix API"  
                else:	   
                    if x["method"] == "AGILE":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:																																				  
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                previousLastDate = start
                                end = datetime.today().strftime('%m/%d/%Y')                           
                            elif Incremental:
                                if pageInfo == "CascadeFile":
                                    start = datetime.strptime(str(x["startDate"]), '%Y/%m/%d').strftime('%m/%d/%Y')
                                else:
                                    start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')		   
                                end = datetime.today().strftime('%m/%d/%Y') 																												
                            else:																						 
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                }
                            if entity in ['Defect','Task','Risk','Issue']:
                                delType = "Agile"
                            if delType == "Agile":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
        #                        CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,config,deltype = None)
                                utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)                           
                            elif delType == "AD/Agile":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)                            
                            elif delType == "ALL":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)
                                else:
                                    utils.IterationAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                            else:
                                raise Exception("please provide correct method. Method should be Agile")
                               
                    elif x["method"] == "DEVOPS":
                        for entity, delType in x["Entities"].items():
                            if delType in ["Devops", "AD/Devops", "ALL"]:
                                if auto_retrain:
                                    						
                                    start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                elif Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y') 
                                    if pageInfo == "CascadeFile":
                                        start = datetime.strptime(str(x["startDate"]), '%Y/%m/%d').strftime('%m/%d/%Y')
                                    else:
                                        start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                devopsAPI = EntityConfig['CDMConfig']['CDMAPI']
                                devopsAPI = devopsAPI.format(urlencode(k))
                                
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(devopsAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)
                                else:
                                    utils.IterationAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                    elif x["method"] == "AD":
                        for entity, delType in x["Entities"].items():
                            
                            if delType in ["AD","AD/Agile","AD/Devops", "ALL","AD/PPM"]:
                                if auto_retrain:
                                    						
                                    start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                if Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y') 
                                    if pageInfo == "CascadeFile":
                                        start = datetime.strptime(str(x["startDate"]), '%Y/%m/%d').strftime('%m/%d/%Y')
                                    else:
                                        start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                AdApi = EntityConfig['CDMConfig']['CDMAPI']
                                AdApi = AdApi.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(AdApi,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)
                                else:
                                    utils.IterationAPI(AdApi,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)                                
                    elif x["method"] == "OTHERS":
                        for entity, delType in x["Entities"].items():
                            
                            if delType in ["AD","AD/Agile","AD/Devops", "ALL","Others","Others/PPM"]:
                                if auto_retrain:
                                    						
                                    start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                if Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y')
                                    if pageInfo == "CascadeFile":
                                        start = datetime.strptime(str(x["startDate"]), '%Y/%m/%d').strftime('%m/%d/%Y')
                                    else:
                                        start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                OthersApi = EntityConfig['CDMConfig']['CDMAPI']
                                OthersApi = OthersApi.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(OthersApi,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                                else:
                                    utils.IterationAPI(OthersApi,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                    
                    elif x["method"] == "PPM":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:																																				  
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                previousLastDate = start
                                end = datetime.today().strftime('%m/%d/%Y')                           
                            elif Incremental:
                                if pageInfo == "CascadeFile":
                                    start = datetime.strptime(str(x["startDate"]), '%Y/%m/%d').strftime('%m/%d/%Y')
                                else:
                                    start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')		   
                                end = datetime.today().strftime('%m/%d/%Y') 																												
                            else:																						 
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                }
                        
                            if delType == "Agile/PPM":
                                delType = "Agile"
                                method = "AGILE"
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,method,delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)                           
                            elif delType == "AD/PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)                            
                            elif delType == "PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(agileAPI,correlationId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,Incremental)
                                else:
                                    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                            else:
                                raise Exception("please provide correct method. Expected method is PPM")
                                
                    else:
                        entityDfs = {}
                
                utils.logger(logger, correlationId, 'INFO', ('Entity pull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                MultiSourceDfs['Entity'] = entityDfs
            else:
                MultiSourceDfs['Entity'] = {}
            utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
        
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # InstaML
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if invokeIngestData['InstaMl'] != 'null':
                vdsInstaMlDfs = {}
                x = invokeIngestData['InstaMl']
                if auto_retrain:
                    lastDataPoint = utils.getlastDataPoint(correlationId)
                if x[0]["UseCaseId"] == 'null':
                    utils.logger(logger, correlationId, 'INFO', ('Instaml data pull Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                    x = x[0]
                    k = {"ClientUID": invokeIngestData["ClientUID"],
                         "DCID": invokeIngestData["DeliveryConstructUId"],
                         "ProblemType": x['ProblemType'],
                         "InstaID": x['InstaId'],
                         "TargetColumn": x['TargetColumn'],
                         "Dimension": x['Dimension']
                         }
                    insta_id = x['InstaId']
                    instaDataUrl = utils.getInstaURL(correlationId)
                    
                
                    #below if block ignored for LDAP Merge
                    if instaDataUrl.startswith("mywizardphoenixam", 8) or 'pam' in config['BaseUrl']['myWizardAPIUrl']:
                       

                        instaTokenUrl = config['PAD']['tokenAPIUrl']
                        auth_type = utils.get_auth_type()
                        if auth_type.upper() == 'FORMS' or auth_type.upper() == 'FORM':
                            Creds = requests.post(instaTokenUrl, headers={'Content-Type': 'application/json'},
                                                  data=json.dumps({"username": config['PAD']['username'],
                                                                   "password": config['PAD']['password']}))

                        if Creds.status_code != 200:
                                utils.updQdb(correlationId, 'E', "Token API is returned " + str(
                                    Creds.status_code) + " code, Error Occured while calling API ", pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                        else:
                        
                            accessToken = Creds.json()['token']                       
                            if auto_retrain:
                                p = {"LastFitDate": lastDataPoint, "ProcessFlow": "IncrementalLoad"}
                            else:
                                p = {"LastFitDate": "2019-01-01", "ProcessFlow": "FullDump"}
                            k.update(p)
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json',
                                                        'authorization': '{}'.format(accessToken)})

                            if result.status_code != 200 or result.json() == None or result.text == 'null':
                                e = "VDS Raw data API is returned " + str(
                                    result.status_code) + " code, Error Occured while calling Phoenix API or API returning Null"
                                vdsInstaMlDfs['vds_InsatML'] = e

                            else:
                                instaDataJson = result.json()["ActualData"]

                                if not instaDataJson:
                                    InstaData = pd.DataFrame()
                                    if auto_retrain:
                                        vdsInstaMlDfs['vds_InsatML'] = "No Incremental Data Available"
                                    else:
                                        vdsInstaMlDfs['vds_InsatML'] = "Data is not available for your selection"
                                else:
                                    InstaData = pd.DataFrame(instaDataJson)

                                if InstaData.empty:
                                    if auto_retrain:
                                        vdsInstaMlDfs['vds_InsatML'] = "No Incremental Data Available"
                                    else:
                                        vdsInstaMlDfs['vds_InsatML'] = "Data is not available for your selection"
                                elif InstaData.shape[0] <= min_df and not auto_retrain:
                                    vdsInstaMlDfs[
                                        'vds_InsatML'] ="Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                                else:
                                    vdsInstaMlDfs['vds_InsatML'] = InstaData
                                    InstaData[x['Dimension']] = pd.to_datetime(InstaData[x['Dimension']], dayfirst=True)
                                                                    
                                    lastDateDict['vds_InsatML'] = InstaData[x['Dimension']].max().strftime('%Y-%m-%d %H:%M:%S')
                    else:
                    
                        if x.get("Source",None)=="VDS":
                            if config["GenericSettings"]["authProvider"] == "AzureAD":
                                accessToken, status_code = utils.getVDSAIOPSAzureToken()
                            else:
                                accessToken, status_code = utils.formVDSAuth()
                        else:
                            if config["GenericSettings"]["authProvider"] == "AzureAD":
                                accessToken, status_code = utils.getVDSAzureToken()
                            else:
                                accessToken, status_code = utils.formVDSAuth()
                        #if auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                        if status_code != 200:
                            utils.updQdb(correlationId, 'E', "Token API is returned " + str(
                                status_code) + " code, Error Occured while calling API ", pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)

                        else:
                            #accessToken = Creds.json()['access_token']
                            if auto_retrain:
                                p = {"LastFitDate": lastDataPoint, "ProcessFlow": "IncrementalLoad"}
                            else:
                                p = {"LastFitDate": "2019-01-01", "ProcessFlow": "FullDump"}
                            k.update(p)
                            
                            if auth_type == 'WindowsAuthProvider':
                                result = requests.post(instaDataUrl, data=json.dumps(k),headers={'Content-Type': 'application/json'},auth=HttpNegotiateAuth())
                            elif auth_type == 'AzureAD' :
                                result = requests.post(instaDataUrl, data=json.dumps(k),
                                                   headers={'Content-Type': 'application/json',
                                                            'authorization': 'bearer {}'.format(accessToken)})
                            elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                result = requests.post(instaDataUrl, data=json.dumps(k),
                                                   headers={'Content-Type': 'application/json',
                                                            'authorization': '{}'.format(accessToken)})
                        
                            if result.status_code != 200 or result.json() == None or result.text == 'null':
                                e = "VDS Raw data API is returned " + str(
                                    result.status_code) + " code, Error Occured while calling Phoenix API or API returning Null"
                                vdsInstaMlDfs['vds_InsatML'] = e

                            else:
                                instaDataJson = result.json()["ActualData"]
                                if not instaDataJson:
                                    InstaData = pd.DataFrame()
                                    if auto_retrain:
                                        vdsInstaMlDfs['vds_InsatML'] = "No Incremental Data Available"
                                    else:
                                        vdsInstaMlDfs['vds_InsatML'] = "Data is not available for your selection"
                                else:
                                    InstaData = pd.DataFrame(instaDataJson)
                                frequency,_,_ = utils.getTimeSeriesParams(correlationId)
                                if list(frequency.keys())[0] == 'Hourly':
                                    min_datapoints = utils.mindatapointsforHourly()
                                else:
                                    min_datapoints = min_data_VDS
                                if InstaData.empty:
                                    if auto_retrain:
                                        vdsInstaMlDfs['vds_InsatML'] = "No Incremental Data Available"
                                    else:
                                        vdsInstaMlDfs['vds_InsatML'] = "Data is not available for your selection"
                                elif InstaData.shape[0] <= min_df and not auto_retrain:
                                    vdsInstaMlDfs[
                                        'vds_InsatML'] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                                else:
                                    if InstaData.shape[0] > min_datapoints and not auto_retrain:
                                        vdsInstaMlDfs['vds_InsatML'] = InstaData.tail(min_datapoints)
                                    else:                                                                                                                         
                                        vdsInstaMlDfs['vds_InsatML'] = InstaData
                                    InstaData[x['Dimension']] = pd.to_datetime(InstaData[x['Dimension']],dayfirst=True)
                                    lastDateDict['vds_InsatML'] = InstaData[x['Dimension']].max().strftime('%Y-%m-%d %H:%M:%S')
                                if not isinstance (vdsInstaMlDfs['vds_InsatML'],str):
                                    if  (vdsInstaMlDfs['vds_InsatML'].isnull().all()).any():
                                        vdsInstaMlDfs['vds_InsatML'] = "Data provided cant be all null for the last "+ str(min_datapoints)+ " datapoints"
                                    #vdsInstaMlDfs['vds_InsatML'][vdsInstaMlDfs['vds_InsatML'].columns[(-vdsInstaMlDfs['vds_InsatML'].astype('bool').all() | vdsInstaMlDfs['vds_InsatML'].isnull().all())]]=[-99999.007]*vdsInstaMlDfs['vds_InsatML'].shape[0]
                    utils.logger(logger, correlationId, 'INFO', ('Instaml data pull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

                    MultiSourceDfs['VDSInstaML'] = vdsInstaMlDfs

                # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                # InstaML Regression
                # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                elif x[0]["UseCaseId"] != 'null':
                    utils.logger(logger, correlationId, 'INFO', ('InstaRegression data pull Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                    for x_i in x:
                        vdsInstaMlDfs[x_i["CorrelationId"]] = {}
                        lastDateDict[x_i["CorrelationId"]] = {}
                    k = {
                        "UseCaseID": x[0]["UseCaseId"],
                        "ClientUID": invokeIngestData["ClientUID"],
                        "DCID": invokeIngestData["DeliveryConstructUId"],
                        # "InstaID"              : x['InstaId']
                        # "Flag"                 : x['Flag']
                    }
                    instaDataUrl = utils.getInstaURL(correlationId)
                    if x[0].get("Source",None)=="VDS(AIOPS)":
                            if auth_type == "AzureAD" == "AzureAD":
                                accessToken, status_code = utils.getVDSAIOPSAzureToken()
                            elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                accessToken, status_code = utils.formVDSAuth()
                            elif auth_type == 'WindowsAuthProvider':
                                accessToken = ''
                                status_code =200
                    elif auth_type == "AzureAD":
                        accessToken, status_code = utils.getVDSAzureToken()
                    else:
                        accessToken, status_code = utils.formVDSAuth()
                    elif auth_type == 'WindowsAuthProvider':
                        accessToken = ''
                        status_code =200
                    if x[0].get("Source",None)=="VDS(AIOPS)" or auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                        if status_code != 200:
                            utils.updQdb(correlationId, 'E', "Token API is returned " + str(
                                status_code) + " code, Error Occured while calling API ", pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                            for x_i in x:
                                vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "Token API is returned " + str(
                                    status_code) + " code, Error Occured while calling API "
                    if status_code == 200:
                        #accessToken = Creds.json()['access_token']
                        if auto_retrain:
                            p = {"LastFitDate": lastDataPoint, "ProcessFlow": "IncrementalLoad"}
                        else:
                            p = {"LastFitDate": "2019-01-01", "ProcessFlow": "FullDump"}
                        k.update(p)
                        
                        if auth_type == 'WindowsAuthProvider':
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json'},auth=HttpNegotiateAuth())
                        elif auth_type == 'AzureAD':
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json',
                                                        'authorization': 'bearer {}'.format(accessToken)})
                        elif  auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json',
                                                        'authorization': '{}'.format(accessToken)})
                        if result.status_code != 200 or result.json() == None or result.text == 'null':
                            e = "VDS Raw data API is returned " + str(
                                result.status_code) + " code, Error Occured while calling Phoenix API or API returning Null"
                            for x_i in x:
                                vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = e
                        else:
                            instaDataJson = result.json()
                            for x_i in x:
                                ProblemType = x_i['ProblemType']
                                Dimension = x_i['Dimension']
                                insta_id = x_i['InstaId']
                                if not instaDataJson:
                                    InstaData = pd.DataFrame()
                                    vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "Data is not available for your selection"
                    
                                else:
                                    InstaData = pd.DataFrame(columns=[Dimension])
                                    if ProblemType == "Regression":
                                        for eachInstaML in instaDataJson["IntsaMLTrainingDataResponse"]:
                                            InstaData = pd.merge(InstaData, pd.DataFrame(eachInstaML["ActualData"]),
                                                                 how="outer", on=Dimension)
                                    else:
                                        for eachInstaML in instaDataJson["IntsaMLTrainingDataResponse"]:
                                            if eachInstaML["InstaID"] == x_i['InstaId']:
                                                InstaData = pd.DataFrame(eachInstaML["ActualData"])
    
                                if InstaData.empty:
                                    if auto_retrain:
                                        vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "No Incremental Data Available"
                                    else:
                                        vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "Data is not available for your selection"
                                else:
                                    if InstaData.shape[0] > min_data_VDS and InstaData.shape[0] > min_df and not auto_retrain:
                                        vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = InstaData.tail(min_data_VDS)
                                    elif InstaData.shape[0] <= min_df and ProblemType != "Regression" and not auto_retrain:
                                        vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                                    else:
                                        vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = InstaData
                                
                                    if not isinstance (vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'],str):
                                        if  ((vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression']=="").all() | vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'].isnull().all()).any() and not auto_retrain:
                                            vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'] = "Data provided cant be all null for the last 104 datapoints"                                                             
                                        else:
                                            vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'][x_i['Dimension']] = pd.to_datetime(vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'][x_i['Dimension']], dayfirst=True)
                                            lastDateDict[x_i["CorrelationId"]]['vds_InsatML'] = vdsInstaMlDfs[x_i["CorrelationId"]]['InstaML_Regression'][x_i['Dimension']].max().strftime('%Y-%m-%d %H:%M:%S')
                    utils.logger(logger, correlationId, 'INFO', ('InstaRegression data pull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                    MultiSourceDfs['VDSInstaML'] = vdsInstaMlDfs
                    
            else:
                MultiSourceDfs['VDSInstaML'] = {}
               # MultiSourceDfs['InstaML_Regression'] = {}
                # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # file Upload
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if invokeIngestData['fileupload']['fileList'] != 'null':  ###CHANGES
                fileDfs = {}
                x = invokeIngestData['fileupload']['fileList']
                if platform.system() == 'Linux':
                    x = x.replace('\\', '//')
                    x = eval(x)
                elif platform.system() == 'Windows':
                    x = x.strip('[').strip(']').replace('"','').split(',')
                utils.logger(logger, correlationId, 'INFO', ('File upload Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                for filepath in x:
                    
                    if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
                        data_t = utils.read_csv(filepath)
                        data_t = data_t.rename(columns=lambda x: x.strip())
                        for col in list(data_t.columns):
                            if str(col).endswith(".1"):
                                raise Exception("Duplicate column names detected. Please validate the data and try again.")
                                
                        if data_t.shape[0] < 1 and pageInfo == "CascadeFile":
                            fileDfs[filepath] = "Number of records in the file is less than 1. Please upload data file with more number of records for Cascade Visualization."
                        elif data_t.shape[0] < 1 and utils.isFMModel(correlationId):
                            if Incremental:
                                fileDfs[filepath] = "Minimum number of records required is 1. Please validate the data and try again."
                            else:
                                fileDfs[filepath] = "Minimum number of unique Releases required is 20. Please validate the data and try again."
                        elif data_t.shape[0] == 0:
                            fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        elif data_t.shape[0] <= min_df and pageInfo!= "CascadeFile" and not utils.isFMModel(correlationId):
                            fileDfs[
                                filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                        else:
                            if utils.isFMModel(correlationId):
                                data_t = utils.FMDerivedColumns(data_t, Incremental)
                            data_t[data_t.select_dtypes(include='bool').columns] = data_t[data_t.select_dtypes(include='bool').columns].astype('str')
                            fileDfs[filepath] = data_t							
                    elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):
                        data_t = utils.read_excel(filepath)
                        data_t = data_t.rename(columns=lambda x: x.strip())
                        for col in list(data_t.columns):
                            if str(col).endswith(".1"):
                                raise Exception("Duplicate column names detected. Please validate the data and try again.")
                        if data_t.shape[0] < 1 and pageInfo == "CascadeFile":
                            fileDfs[filepath] = "Number of records in the file is less than 1. Please upload data file with more number of records for Cascade Visualization."
                        elif data_t.shape[0] < 1 and utils.isFMModel(correlationId):
                            if Incremental:
                                fileDfs[filepath] = "Minimum number of records required is 1. Please validate the data and try again."
                            else:
                                fileDfs[filepath] = "Minimum number of unique Releases required is 20. Please validate the data and try again."
                        elif data_t.shape[0] == 0:
                            fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        elif data_t.shape[0] <= min_df and pageInfo!= "CascadeFile" and not utils.isFMModel(correlationId):
                            fileDfs[
                                filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                        else:
                            if utils.isFMModel(correlationId):
                                data_t = utils.FMDerivedColumns(data_t, Incremental)
                            data_t[data_t.select_dtypes(include='bool').columns] = data_t[data_t.select_dtypes(include='bool').columns].astype('str')
                            fileDfs[filepath] = data_t
                if 'AgileUsecase' in invokeIngestData['fileupload']:
                    Agileusecase = invokeIngestData['fileupload']['AgileUsecase']
                    oldcorrelationId = Agileusecase['oldcorrelationid']
                    columns_list = utils.getColumnnames(oldcorrelationId)       
                    new_column_list = list(data_t.columns)
                    if columns_list != new_column_list:
                        fileDfs[filepath] = "Column mismatch...please upload the file with same columns"																
                utils.logger(logger, correlationId, 'INFO', ('File upload Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
               
                MultiSourceDfs['File'] = fileDfs
            else:
                MultiSourceDfs['File'] = {}
        
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # Custome Connector
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if invokeIngestData['Customdetails']!='null':
                utils.logger(logger, correlationId, 'INFO', ('Custom datapull Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
                customDfs={'Customdetails':invokeIngestData['Customdetails']}
                AppId=customDfs.get('Customdetails').get('AppId')
                UsecaseId = customDfs.get('Customdetails').get('UsecaseID','None')#UsecaseID:"fa52b2ab-6d7f-4a97-b834-78af04791ddf"
                Ambulance = False
                #min_df = utils.customdatapoints(UsecaseId, AppId)
                if UsecaseId in ["fa52b2ab-6d7f-4a97-b834-78af04791ddf","169881ad-dc85-4bf8-bc67-7b1212836a97","be0c67a1-4320-461e-9aff-06a545824a32",
                                           "6761146a-0eef-4b39-8dd8-33c786e4fb86","8d3772f2-f19b-4403-840d-2fb230ac630f","668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2"]:
                    Ambulance = True
        
                cDetails = invokeIngestData["Customdetails"]
                auth_type = utils.get_auth_type()
            
                if "CustomFlags" in cDetails :
                    usecase  = cDetails["CustomFlags"]["FlagName"]
                    if usecase in ["IA_Defect","IA_Schedule"]:
                        min_df = utils.customdatapoints(UsecaseId, AppId)
                        url =  cDetails["AppUrl"]
                        if auth_type == 'AzureAD':
                            customurl_token,status_code=utils.CustomAuth(AppId)
                        elif auth_type == 'WindowsAuthProvider':
                            customurl_token = ''
                            status_code =200
                    
                        if status_code == 200:
                            if len(cDetails["InputParameters"]["ReleaseUID"]) == 0 and Incremental:
                                MultiSourceDfs['Custom'] = {"Custom":"ReleaseUID is Empty"}
                            else:
                                data,code = utils.getIaUsaCaseData(ClientUID,DeliveryConstructUId,url,cDetails,customurl_token,correlationId)
                                if type(data) != bool:
                                    if not data.empty:          
                                       MultiSourceDfs['Custom'] = {"Custom":data}
                                    else:
                                       MultiSourceDfs['Custom'] = {"Custom":"Data after processing is Empty"}
                                else:
                                    MultiSourceDfs['Custom'] = {"Custom":"APP Api returned Error "+str(code)+"status code"}
                        else:
                            MultiSourceDfs['Custom'] = {"Custom":"Token Api returned Error "+str(code)+"status code"}                    
                    else:
                        if auto_retrain:
                            fromdate = cDetails["InputParameters"]["FromDate"]
                            previousLastDate = fromdate
                            todate = datetime.today().strftime('%m/%d/%Y')
                        elif Incremental:
                            todate = datetime.today().strftime('%m/%d/%Y') 
                            fromdate = datetime.strptime(str(cDetails["InputParameters"]["FromDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                        else:
                            fromdate =  cDetails["InputParameters"]["FromDate"]
                            todate =  cDetails["InputParameters"]["ToDate"]
                        data,lastDateDict = AggregateAgileData.get_aggregated_data(ClientUID,correlationId,DeliveryConstructUId,fromdate,todate,config,parent,Incremental,auto_retrain,flag = "agileusecase")
                        if not data.empty:   
                            MultiSourceDfs['Custom'] = {"Custom":data}
                        else:
                            MultiSourceDfs['Custom'] = {"Custom":"Data after processing is Empty"}
                   
                elif AppId in ["595fa642-5d24-4082-bb4d-99b8df742013","a3798931-4028-4f72-8bcd-8bb368cc71a9","9fe508f7-64bc-4f58-899b-78f349707efa"]:   #SPA Velocity prediction
                   
                   #min_df = utils.customdatapoints(UsecaseId, AppId)
                   if auth_type == 'AzureAD':
                            customurl_token,status_code=utils.CustomAuth(AppId)
                   elif auth_type == 'WindowsAuthProvider':
                            customurl_token = ''
                            status_code =200
                   
                   if status_code != 200:
                      MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
                  
                   else:
                      startdate = invokeIngestData['Customdetails']['InputParameters'].get("StartDate")
                      enddate = invokeIngestData['Customdetails']['InputParameters'].get("EndDate")
                      enddate = (datetime.strptime(enddate, '%Y-%m-%d')+timedelta(days=1)).strftime('%Y-%m-%d')

                      TotalRecordCount = 0
                      TotalPageCount =  1
                      PageNumber = 1
                      BatchSize = invokeIngestData['Customdetails']['InputParameters'].get("BatchSize")
                      data = pd.DataFrame()

                      while PageNumber <=TotalPageCount:
                          
                          jsondata = {
                                    "ClientUID" : invokeIngestData["ClientUID"], 
                                    "DeliveryConstructUId" : [invokeIngestData["DeliveryConstructUId"]], 
                                    "StartDate" : startdate, 
                                    "EndDate" : enddate, 
                                    "TotalRecordCount" : TotalRecordCount,
                                    "PageNumber":PageNumber,
                                    "BatchSize":int(BatchSize)
                                  }
                                  
                          if Ambulance:                       
                                  jsondata["IterationUId"]=eval(invokeIngestData['Customdetails']['InputParameters'].get("IterationUId"))
                          elif AppId=="595fa642-5d24-4082-bb4d-99b8df742013" or AppId=="9fe508f7-64bc-4f58-899b-78f349707efa":
                                  if UsecaseId in ["64a6c5be-0ecb-474e-b970-06c960d6f7b7","5cab6ea1-8af4-4f74-8359-e053629d2b98","68bf25f3-6df8-4e14-98fa-34918d2eeff1"]:    #SPA
                                      jsondata["IsTeamLevelData"] = "0"
                                  elif UsecaseId in ["877f712c-7cdc-435a-acc8-8fea3d26cc18","6b0d8eb3-0918-4818-9655-6ca81a4ebf30"]:  #'regression'
                                      jsondata["IsTeamLevelData"] = "1"
                                  elif UsecaseId == 'f0320924-2ee3-4398-ad7c-8bc172abd78d':    #timeseries
                                      jsondata["IsTeamLevelData"] = "1"
                                      Teamlevel = invokeIngestData['Customdetails']['InputParameters'].get("TeamAreaUId",None)
                                      if Teamlevel != "null" and Teamlevel != None:
                                          jsondata["TeamAreaUId"]=[Teamlevel]
                          if PageNumber == 1:
                              if auth_type == 'AzureAD':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                                 'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                              elif auth_type == 'WindowsAuthProvider':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                     auth=HttpNegotiateAuth()) 
                              x = 1
                              maxdata = utils.maxdatapull(correlationId)
                              nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
                              try:
                                total_rec = result.json()['TotalRecordCount']
                              except Exception as e:
                                  if result.status_code!=200:
                                    MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting "+invokeIngestData.get('Customdetails').get('AppUrl')+" for getting record count is "+str(result.status_code)}
                                    break
                              if maxdata < result.json()['TotalRecordCount'] and nonprod:
                                  actualfromdate = jsondata["StartDate"]
                                  if (datetime.strptime(str(jsondata['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d') > actualfromdate:
                                      jsondata["StartDate"] = (datetime.strptime(str(jsondata['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d')
                                      if auth_type == 'AzureAD':
                                        result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                                         'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                      elif auth_type == 'WindowsAuthProvider':
                                        result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                             auth=HttpNegotiateAuth()) 
                                      while maxdata > result.json()['TotalRecordCount']:
                                          x = x + 1
                                          if (datetime.strptime(str(jsondata['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d') > actualfromdate:
                                              jsondata["StartDate"] = (datetime.strptime(str(jsondata['EndDate']), '%Y-%m-%d')-relativedelta.relativedelta(months=1*x)).strftime('%Y-%m-%d')
                                              if auth_type == 'AzureAD':
                                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                                                 'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                              elif auth_type == 'WindowsAuthProvider':
                                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                                 auth=HttpNegotiateAuth()) 
                                      startdate = jsondata["StartDate"]
                          if auth_type == 'AzureAD':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                             'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                          elif auth_type == 'WindowsAuthProvider':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                 auth=HttpNegotiateAuth())
                          
                          if result.status_code!=200:
                              MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting "+invokeIngestData.get('Customdetails').get('AppUrl')+" is "+str(result.status_code)}
                              break
                          else:
                              data_json=result.json()
                              
                              if len(data_json["Client"]) <= 0:
                                      #MultiSourceDfs['Custom'] = {"Custom":"No Data Available for Selection"}
                                      data=pd.DataFrame()
                                      break
                              else:
                                  if PageNumber == 1:                              
                                      data=pd.DataFrame(data_json['Client'][0]["Items"])
                                      if UsecaseId in ["877f712c-7cdc-435a-acc8-8fea3d26cc18","6b0d8eb3-0918-4818-9655-6ca81a4ebf30"] and "TeamAreaUId" in data.columns and "IterationUId" in data.columns:
                                          data["TeamIterationID"] = data["TeamAreaUId"]+"_"+data["IterationUId"]
                                      TotalRecordCount = data_json["TotalRecordCount"]
                                      TotalPageCount = data_json["TotalPageCount"]
                                  else:
                                      temp_data = pd.DataFrame(data_json['Client'][0]["Items"])
                                      if UsecaseId in ["877f712c-7cdc-435a-acc8-8fea3d26cc18","6b0d8eb3-0918-4818-9655-6ca81a4ebf30"] and "TeamAreaUId" in temp_data.columns and "IterationUId" in temp_data.columns:
                                          temp_data["TeamIterationID"] = temp_data["TeamAreaUId"]+"_"+temp_data["IterationUId"]
                                      data = data.append(temp_data,ignore_index=True)                              
                                  PageNumber = PageNumber + 1
                                
                      #if allparams['ProblemType'] == "TimeSeries":
                      #    min_df = 9
                      min_df = utils.customdatapoints(UsecaseId, AppId)
                      if data.shape[0]==0:
                           if 'Custom' in MultiSourceDfs:
                               
                               if not isinstance(MultiSourceDfs['Custom'],dict):
                                    MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                           else:
                               MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                      elif data.shape[0]<min_df and not auto_retrain:
                           MultiSourceDfs['Custom'] = {"Custom":"Number of records less than or equal to  "+str(min_df)+".  Please provide minimum"+str(min_df)+"valid records"}    
                      else:
                           no_data = False
                           if Ambulance:
                               if UsecaseId == "fa52b2ab-6d7f-4a97-b834-78af04791ddf":                              
                                   data= data[data["ChangeRequestHigh"] != "0"]
                               elif UsecaseId == "6761146a-0eef-4b39-8dd8-33c786e4fb86":                              
                                   data= data[data["ServiceRequestCritical"] != "0"]
                               elif UsecaseId == "be0c67a1-4320-461e-9aff-06a545824a32":                              
                                   data= data[data["ServiceRequestHigh"] != "0"]
                               elif UsecaseId == "8d3772f2-f19b-4403-840d-2fb230ac630f":                              
                                   data= data[data["ProblemHigh"] != "0"]
                               elif UsecaseId == "668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2":                              
                                   data= data[data["ProblemCritical"] != "0"]
                               elif UsecaseId == "169881ad-dc85-4bf8-bc67-7b1212836a97":                              
                                   data= data[data["ChangeRequestCritical"] != "0"]
                               if data.shape[0]==0:
                                   MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                                   no_data = True
                               elif data.shape[0]<min_df and not auto_retrain:		
                                   MultiSourceDfs['Custom'] = {"Custom":"Number of records less than or equal to  "+str(min_df)+".  Please provide minimum"+str(min_df)+"valid records"} 
                                   no_data = True

                           if not no_data:								   
                               MultiSourceDfs['Custom'] = {"Custom":data}
                     
                               if AppId == "a3798931-4028-4f72-8bcd-8bb368cc71a9" :
                                   if Ambulance :
                                       data["StartOn"] = pd.to_datetime(data["StartOn"])
                                       data["EndOn"] = pd.to_datetime(data["EndOn"])
                                       lastDateDict["Custom"] = {"StartOn":data["StartOn"].max().strftime('%Y-%m-%d %H:%M:%S')}
                                       if allparams['ProblemType'] == "TimeSeries":
                                           data = data.sort_values(by="StartOn")
                                           aggDays= (data['EndOn'].iloc[-1] - data['StartOn'].iloc[-1]).days
                                           
                                           utils.updateAggregateValue(correlationId,aggDays)
                                   else: 
                                       data["ModifiedAtSourceOn"] = pd.to_datetime(data["ModifiedAtSourceOn"])
                                       lastDateDict["Custom"] = {"ModifiedAtSourceOn":data["ModifiedAtSourceOn"].max().strftime('%Y-%m-%d %H:%M:%S')}
                               else:
                                   data["StartDate"] = pd.to_datetime(data["StartDate"])
                                   data["EndDate"] = pd.to_datetime(data["EndDate"])
                                   lastDateDict["Custom"] = {"StartDate":data["StartDate"].max().strftime('%Y-%m-%d %H:%M:%S')}
                                   if allparams['ProblemType'] == "TimeSeries":
                                       data = data.sort_values(by="StartDate")
                                       aggDays= (data['EndDate'].iloc[-1] - data['StartDate'].iloc[-1]).days
                                       utils.updateAggregateValue(correlationId,aggDays)                    
                      
                else:
                   if auth_type == 'AzureAD' or auth_type.upper() == 'FORMS' or auth_type.upper() == 'FORM':
                            customurl_token,status_code=utils.CustomAuth(AppId)
                   elif auth_type == 'WindowsAuthProvider':
                            customurl_token = ''
                            status_code =200
                   if AppId == '9102fb74-5deb-46ff-9798-9fe6b20945f3' or AppId == 'a2a52651-0598-417e-aad3-adf843c25d1c':
                            user_date_column_name = 'DateColumn'
                   else:
                            user_date_column_name = customDfs.get('Customdetails').get('DateColumn')		
                   if auth_type == 'WINDOWS_AUTH' or status_code==200:
                       if auth_type == 'AzureAD':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                             'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                       elif auth_type.upper() == 'FORMS' or auth_type.upper() == 'FORM':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                             'Authorization': (customurl_token)})

                       elif auth_type == 'WindowsAuthProvider':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                 auth=HttpNegotiateAuth())
										  
                       if result.status_code==200:
                           		   
                           data_json=result.json()
                           data=pd.DataFrame(data_json)
                           if (AppId == '9102fb74-5deb-46ff-9798-9fe6b20945f3' or AppId == 'a2a52651-0598-417e-aad3-adf843c25d1c') and Incremental and data.shape[0]>0:
                               data = data[(data['Status']!="Resolved") & (data['Status']!="Closed")& (data['Status']!="Cancelled")& (data['Status']!=None)&(data['Status']!=np.nan)]
                               data.set_index(np.arange(len(data.index))) 

                           if data.shape[0]==0:
                               if Incremental:
                                   MultiSourceDfs['Custom'] = {"Custom":"No data available for prediction"}		
                               else:								   
                                   MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                           elif data.shape[0]<min_df and not auto_retrain and not Incremental:
                               MultiSourceDfs['Custom'] = {"Custom":"No of rows in the data is less than "+str(min_df)}
                            
                           else:
                               if (user_date_column_name == None) or (user_date_column_name==""):
                                   MultiSourceDfs['Custom'] = {"Custom": "User has entered empty DateColumn name"}
                               if user_date_column_name not in data.columns:
                                   MultiSourceDfs['Custom'] = {"Custom": "User specified DateColumn '{}' is not found in the data".format(user_date_column_name)}
                               else:                               
                                   data["UniqueRowID"] = data.ClientUId + data.EndToEndUId + data.IncidentNumber
                                   data[user_date_column_name] = pd.to_datetime(data[user_date_column_name])
                                   lastDateDict["Custom"] = {"user_date_column_name":data[user_date_column_name].max().strftime('%Y-%m-%d %H:%M:%S')}
                                   MultiSourceDfs['Custom'] = {"Custom":data}
                         
                       else:
                            MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
					 
                   else:
                         MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
                utils.logger(logger, correlationId, 'INFO', ('Custom datapull Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
            elif 'CustomSource' in invokeIngestData.keys() and invokeIngestData['CustomSource'] != 'null':
                global customdataDfs
                customdataDfs = {}
                lastDateDict = {}
                customdata = invokeIngestData['CustomSource']
                t = base64.b64decode(customdata)
                dtext = json.loads(EncryptData.DescryptIt(t))
                if dtext["Type"] == "API":
                    entityArgs = dtext["BodyParam"]
                    hadoopApi = dtext["ApiUrl"]
                    TargetNode = dtext["TargetNode"].split('.')
                    if dtext["KeyValues"] != {}:
                        k={"ClientUId": dtext["KeyValues"]["ClientUId"],"DeliveryConstructUId": dtext["KeyValues"]["DeliveryConstructUId"]}
                    else:
                        k = {"ClientUId": invokeIngestData["ClientUID"],"DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]}
                    hadoopApi = hadoopApi.format(urlencode(k))
                    Authtoken = dtext["Authentication"]
                    if Authtoken["UseIngrainAzureCredentials"] == True:
                        entityAccessToken = Authtoken["Token"]
                    else:
                        if Authtoken["Type"] == 'AzureAD':
                            TokenURL = Authtoken['AzureUrl']
                            headers = {"Content-Type": "application/x-www-form-urlencoded"}
                            payload = Authtoken['AzureCredentials']
                            try:
                                r = requests.post(TokenURL, data=payload, headers=headers)
                                tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
                                token = tokenjson["access_token"]
                            except:
                                token = None
                            entityAccessToken = token
                        elif Authtoken["Type"] == 'Token':
                            entityAccessToken = Authtoken["Token"]

                    customdataDfs = utils.Customsourcedata(hadoopApi,TargetNode,entityArgs,entityAccessToken,customdataDfs,lastDateDict,Incremental=False)
                    if isinstance(customdataDfs, pd.DataFrame):
                        MultiSourceDfs['Custom'] = {"Custom":customdataDfs}
                    else:
                        MultiSourceDfs['Custom'] = {"Custom":customdataDfs['custom']}
                elif dtext["Type"] == "CustomDbQuery":
                    try:
                        query = dtext['Query']
                        connection = utils.open_phoenixdbconn()
                        data = connection.command(eval(query))
                        queryDF = pd.DataFrame(data['cursor']['firstBatch'])
                        columns_list=[]
                        for col in queryDF.columns: 
                            m=(queryDF[col].map(lambda x : type(x).__name__)=='UUID').any()
                            if m:
                                columns_list.append(col)
                                
                            if len(columns_list)>0:
                                for x in columns_list:        
                                    queryDF[x]=queryDF[x].apply(str)
                        for col in queryDF.columns: 
                            try:
                                if queryDF[col].dtype == 'object' and 'DateTime' in queryDF[col].iloc[0].keys():
                                    queryDF[col] = [pd.to_datetime(d['DateTime']) for d in queryDF[col]]
                            except:
                                utils.logger(logger,correlationId, 'INFO', ("DB Query column is not eligible for Datetime conversion"),str(None))
                            try:
                                queryDF[[dtext["DateColumn"]]] = queryDF[dtext["DateColumn"]].apply(lambda x: x['DateTime'])
                                lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                            except:
                                utils.logger(logger,correlationId, 'INFO', ("DB Query column is not eligible for Datetime conversion"),str(None))
                        MultiSourceDfs['Custom'] = {"Custom":queryDF}
                    except:
                        from uuid import UUID
                        from pymongo.errors import InvalidBSON
                        query = dtext['Query']	
                        import re
                        if 'ISODate' in query:
                            m = re.search('ISODate(.*})',query)
                            m = m.group(0).split('}')[0]
                            date_withoutkey = m.split('ISODate')[1]
                            new_date = datetime.fromisoformat(eval(date_withoutkey))
                            new_date = repr(datetime.strptime(str(new_date), '%Y-%m-%d %H:%M:%S').strftime('%Y-%m-%d %H:%M:%S').replace(' 0',' ').replace('-0','-'))
                            query = query.replace(m,new_date,1)
                            query = eval(query)
                            for key,value in query["pipeline"][0]["$match"].items():
                                for key1,value1 in value.items():
                                    if eval(new_date)==value1:
                                        query["pipeline"][0]["$match"][key][key1] = datetime.strptime(str(query["pipeline"][0]["$match"][key][key1]), '%Y-%m-%d %H:%M:%S')

                        else:						
                            query = eval(query)
                        
                        regexUuid = "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[89aAbB][a-f0-9]{3}-[a-f0-9]{12}"
                        from bson.binary import Binary, UUID_SUBTYPE
                        for key,value in query["pipeline"][0]["$match"].items():
                            
                            if bool(re.match(regexUuid, str(value))):
                                query["pipeline"][0]["$match"][key] = getcust(value)
                        connection = utils.open_phoenixdbconn()
                        data = connection.command(query)		
                        queryDF = pd.DataFrame(data['cursor']['firstBatch'])
                        for col in queryDF.columns:
                            try:
                                if queryDF[col].dtype == 'object' and 'DateTime' in queryDF[col].iloc[0].keys():
                                    queryDF[col] = [pd.to_datetime(d['DateTime']) for d in queryDF[col]]
                            except:
                                utils.logger(logger,correlationId, 'INFO', ("Custom DB Query"),str(None))
                            try:
                                queryDF[[dtext["DateColumn"]]] = queryDF[dtext["DateColumn"]].apply(lambda x: x['DateTime'])
                                lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                            except:
                                utils.logger(logger,correlationId, 'INFO', ("Custom DB Query"),str(None))
                        lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                        MultiSourceDfs['Custom'] = {"Custom":queryDF}

            else:
                 MultiSourceDfs['Custom']={}
            if 'CustomMultipleFetch' in invokeIngestData and  invokeIngestData['CustomMultipleFetch']!='null':
                utils.logger(logger, correlationId, 'INFO', ('CustomMultifetch Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
                auth_type = utils.get_auth_type()
                CustomMultipleFetch = invokeIngestData['CustomMultipleFetch']
            
                DataSource = CustomMultipleFetch['DataSource']
                TotalNoOfRecords = CustomMultipleFetch['TotalNoOfRecords']
                BatchSize = CustomMultipleFetch['BatchSize']
                FetchType = CustomMultipleFetch['FetchType']
                HttpMethod = CustomMultipleFetch['HttpMethod']
                AppId = CustomMultipleFetch['ApplicationId']
                AppServiceUId = CustomMultipleFetch['AppServiceUId']
                key = "TrainData"
                Url = CustomMultipleFetch['Url']
                if 'DataFlag' in CustomMultipleFetch.keys():
                    DataFlag = CustomMultipleFetch['DataFlag']
                    if DataFlag not in ["FullDump","Incremental"]:
                        DataFlag = "FullDump"
                else:
                    DataFlag = "FullDump"
                if DataFlag == "Incremental":
                    incremental_data = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData")    
                UserEmailId = userId
                if BatchSize =='':
                   BatchSize = '10000000'
                if auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                    customurl_token,status_code=utils.CustomAuth(AppId)
                elif auth_type == 'WindowsAuthProvider':
                    customurl_token = 'WindowsAuthProvider'
                    status_code = 200
                if status_code==200:
                    ncores = multiprocessing.cpu_count()
                
                    if DataSource.strip().lower() == 'custom': 
                        if int(TotalNoOfRecords) > 0 and int(BatchSize) > 0: 
                            
                            results = []
                            #token = generate_token(authentication,token_url, credentials) #check for exception for token generation
                        
                            if FetchType.strip().lower() == 'single': # PageNumber by default = 1
                                results = utils.call_api(Url, 1, BatchSize, correlationId, customurl_token,  UserEmailId, AppServiceUId, HttpMethod, key)
                                data = pd.DataFrame(results)
                                if DataFlag == "Incremental":
                                    frames = [incremental_data, data]
                                    data = pd.concat(frames)
                                
                                MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":data}
                            elif FetchType.strip().lower() == 'multiple':
                                pages = math.ceil(int(TotalNoOfRecords)/int(BatchSize))
                                
                    
                                with concurrent.futures.ThreadPoolExecutor(max_workers=ncores*(5)) as executor:
                                    future_to_url = {executor.submit(utils.call_api, Url, i, BatchSize, correlationId, customurl_token, UserEmailId, AppServiceUId, HttpMethod, key)  for i in range(1,int(pages)+1)}
                                    for future in concurrent.futures.as_completed(future_to_url):
                                        try:
                                            results.extend(future.result())
                                        except Exception as exc:
                                            raise Exception (' generated an exception for Custom Data Multiple Fetch!)
                                        else:
                                            print(' length of results stories is %d ' % ( len(results)))
                                
                                if len(results) == int(TotalNoOfRecords):
                                    data = pd.DataFrame(results)
                                    if DataFlag == "Incremental":
                                        frames = [incremental_data, data]
                                        data = pd.concat(frames)
                                    
                                    MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":data}
                                    #return results 
                                else: 
                                    MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":"All records not fetched either due to connectivity issue with URL or total record mentioned is not correct"}
                            else: 
                                MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":"FetchType should be either single or multiple. Received FetchType: " + FetchType}
                        else:
                            MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":"TotalNoOfRecords & BatchSize should be greater than 0. Received TotalNoOfRecords=" + str(TotalNoOfRecords) +" BatchSize" + str(BatchSize)}
                    elif DataSource.strip().lower() == 'inrequest':
                        if "Data" in CustomMultipleFetch.keys():
                            data = CustomMultipleFetch.get('Data')
                            MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":pd.DataFrame(data)}
                        else: 
                            MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":'Data not found for DataSource=inrequest'}
                    else: 
                        MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":"DataSource should be either Custom or inrequest. Received DataSource: " + DataSource}
                else:
                    MultiSourceDfs['CustomMultipleFetch']={"CustomMultipleFetch":"Token API is returned " + str(
                                status_code) + " code, Error Occured while calling Phoenix API or API returning Null"}   
                utils.logger(logger, correlationId, 'INFO', ('CustomMultifetch Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
           
            else:
                 MultiSourceDfs['CustomMultipleFetch']={}    
            #entity data identification
            EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])

            j = []
            g = {}
            if MultiSourceDfs['File'] != {}:
                for k,v in MultiSourceDfs['File'].items():
                    if  type(v) != str:
                        b = list(EntityMappingColumns.intersection(v.columns))
                        if len(b) == 2:
                            entity_name = os.path.basename(k).split('.')[0][36:]
                            ext = os.path.basename(k).split('.')[1]
                            if entity_name[0] == '_':
                                entity_name = entity_name.split('_')[1]
                            g[entity_name] = ext
                            MultiSourceDfs['Entity'][entity_name] = v
                            j.append(k)
            for i in j:
                del MultiSourceDfs['File'][i] 
            
            if len(MultiSourceDfs['File']) > 1 and MultiSourceDfs['Entity'] == {}:
                for k,v in MultiSourceDfs['File'].items():
                    if   type(v) != str:
                        file_name = os.path.basename(k).split('.')[0][36:]
                        Rename_cols = dict(ChainMap(*list(map(lambda x: {x: x + file_name}, list(v.columns)))))
                        v.rename(columns=Rename_cols, inplace=True)
                        MultiSourceDfs['File'][k] = v.copy()
                
            if MultiSourceDfs['File'] != {} and MultiSourceDfs['Custom'] != {}:
                entity_custom = str(invokeIngestData.get('Customdetails').get('InputParameters')['ServiceType'])
                for k,v in MultiSourceDfs['Custom'].items():
                    if  type(v) != str:
                         MultiSourceDfs['File'][entity_custom+"."+k] = v 
                MultiSourceDfs['Custom'] = {}          
        
            if MultiSourceDfs['File'] != {} and MultiSourceDfs['Entity'] != {}:
                 z = [os.path.basename(file).split('.')[0] for file in  list(MultiSourceDfs['File'].keys())]
                 n = ",".join(z)  
                 utils.updQdb(correlationId, 'E',"Uploaded "+n+" files contains non CDM entity data, Mapping is not psossible.Kindly Upload entity related data", pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            if  MultiSourceDfs['File'] == {} and MultiSourceDfs['Entity'] != {}: 
                utils.logger(logger, correlationId, 'INFO', ('Transform entities Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
                MultiSourceDfs, MappingColumns  = utils.TransformEntities(MultiSourceDfs,ClientUID, DeliveryConstructUId,EntityMappingColumns,parent,Incremental,auto_retrain,flag=None,correlationId = correlationId)
                utils.logger(logger, correlationId, 'INFO', ('Transform entities Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            

            if mapping_flag == 'False':                    
                utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId)) 
                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs, g,parent, mapping,
                                                     mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                     datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,previousLastDate = previousLastDate)
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
                utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                if message == 'single_file':
                    utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
                    if not genericflow:
                        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                    else:
                        utils.updQdb(correlationId, 'P', '10', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
                        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                        dbconn.close()
                        currentUniqueId = data_json[0]["TargetUniqueIdentifier"]
                        ProblemType = data_json[0]["ProblemType"]
                        dbconn, dbcollection = utils.open_dbconn("PS_UsecaseDefinition")
                        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                        dbconn.close()
                        if ProblemType != "TimeSeries":
                            targetuniquessness = data_json[0]["UniquenessDetails"][currentUniqueId]
                        else: 
                            targetuniquessness = {}
                            targetuniquessness["Percent"] = 100
                        if targetuniquessness["Percent"] > 90:
                            #call datacleanup
                            import uuid
                            dbconn, dbcollection = utils.open_dbconn("SSAI_IngrainRequests")
                            data_json = list(dbcollection.find({"CorrelationId": correlationId, "RequestId": requestId}))
                            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                            if invokeIngestData['InstaMl'] != 'null' and invokeIngestData['InstaMl'][0]["UseCaseId"] != 'null':
                                correlationid_list = []
                                correlationid_list = [item['CorrelationId'] for item in invokeIngestData['InstaMl']]
                                request_list = []
                                for i in correlationid_list:
                                    requestId = str(uuid.uuid4())
                                    dbcollection.insert({
                                    "AppID": data_json[0].get('AppID'),
                                    "CorrelationId": i,
                                    "IsRetrainedWSErrorModel": data_json[0].get('IsRetrainedWSErrorModel'),
                                    "PythonProcessID": data_json[0].get('PythonProcessID'),
                                    "IsFMVisualize": data_json[0].get('IsFMVisualize'),
                                    "FMCorrelationId": data_json[0].get('FMCorrelationId'),
                                    "DataSetUId": data_json[0].get('DataSetUId'),
                                    "ApplicationName": data_json[0].get('ApplicationName'),
                                    "ClientId": data_json[0].get('ClientId'),
                                    "DeliveryconstructId": data_json[0].get('DeliveryconstructId'),
                                    "DataPoints": data_json[0].get('DataPoints'),
                                    "ProcessId": data_json[0].get('ProcessId'),
                                    "UseCaseID": data_json[0].get('UseCaseID'),
                                    "RequestStatus": "In - Progress",
                                    "RetryCount": data_json[0].get('RetryCount'),
                                    "Frequency": data_json[0].get('Frequency'),
                                    "InstaID": data_json[0].get('InstaID'),
                                    "UniId": data_json[0].get('UniId'),
                                    "TemplateUseCaseID": data_json[0].get('TemplateUseCaseID'),
                                    "Category": data_json[0].get('Category'),
                                    "CreatedByUser": data_json[0].get('CreatedByUser'),
                                    "ModifiedByUser": data_json[0].get('ModifiedByUser'),
                                    "PyTriggerTime": data_json[0].get('PyTriggerTime'),
                                    "LastProcessedOn": data_json[0].get('LastProcessedOn'),
                                    "EstimatedRunTime": data_json[0].get('EstimatedRunTime'),
                                    "ClientID": data_json[0].get('ClientID'),
                                    "DCID": data_json[0].get('DCID'),
                                    "TriggerType": data_json[0].get('TriggerType'),
                                    "AppURL": data_json[0].get('AppURL'),
                                    "SendNotification": data_json[0].get('SendNotification'),
                                    "IsNotificationSent": data_json[0].get('IsNotificationSent'),
                                    "NotificationMessage": data_json[0].get('NotificationMessage'),
                                    "TeamAreaUId": data_json[0].get('TeamAreaUId'),
                                    "RetrainRequired": data_json[0].get('RetrainRequired'),
                                    "_id": str(uuid.uuid4()),
                                    "RequestId": requestId,
                                    "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                    "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                    "ModelName": "null",
                                    "ProblemType": "null",
                                    "Status": "P",
                                    "Message": "In - Progress",
                                    "Progress": "5",
                                    "pageInfo": "DataCleanUp",
                                    "ParamArgs": "{}",
                                    "Function": "DataCleanUp"
                                    })
                                    request_list.append(requestId)
                                dbconn.close()
                                from multiprocessing import cpu_count
                                from joblib import Parallel, delayed
                                ncpu = cpu_count() 
                                if platform.system() == 'Linux':
                                    jobs = Parallel(n_jobs=ncpu, backend='multiprocessing')
                                elif platform.system() == 'Windows':
                                    jobs = Parallel(n_jobs=ncpu, backend="threading")
                                tasks = (delayed(evaluateModel)(correlationid_list[cfg],request_list[cfg]) for cfg in range(len(correlationid_list)))
                                results = jobs(tasks)
                            else:
                                requestId = str(uuid.uuid4())
                                dbcollection.insert({
                                "AppID": data_json[0].get('AppID'),
                                "CorrelationId": data_json[0].get('CorrelationId'),
                                "IsRetrainedWSErrorModel": data_json[0].get('IsRetrainedWSErrorModel'),
                                "PythonProcessID": data_json[0].get('PythonProcessID'),
                                "IsFMVisualize": data_json[0].get('IsFMVisualize'),
                                "FMCorrelationId": data_json[0].get('FMCorrelationId'),
                                "DataSetUId": data_json[0].get('DataSetUId'),
                                "ApplicationName": data_json[0].get('ApplicationName'),
                                "ClientId": data_json[0].get('ClientId'),
                                "DeliveryconstructId": data_json[0].get('DeliveryconstructId'),
                                "DataPoints": data_json[0].get('DataPoints'),
                                "ProcessId": data_json[0].get('ProcessId'),
                                "UseCaseID": data_json[0].get('UseCaseID'),
                                "RequestStatus": "In - Progress",
                                "RetryCount": data_json[0].get('RetryCount'),
                                "Frequency": data_json[0].get('Frequency'),
                                "InstaID": data_json[0].get('InstaID'),
                                "UniId": data_json[0].get('UniId'),
                                "TemplateUseCaseID": data_json[0].get('TemplateUseCaseID'),
                                "Category": data_json[0].get('Category'),
                                "CreatedByUser": data_json[0].get('CreatedByUser'),
                                "ModifiedByUser": data_json[0].get('ModifiedByUser'),
                                "PyTriggerTime": data_json[0].get('PyTriggerTime'),
                                "LastProcessedOn": data_json[0].get('LastProcessedOn'),
                                "EstimatedRunTime": data_json[0].get('EstimatedRunTime'),
                                "ClientID": data_json[0].get('ClientID'),
                                "DCID": data_json[0].get('DCID'),
                                "TriggerType": data_json[0].get('TriggerType'),
                                "AppURL": data_json[0].get('AppURL'),
                                "SendNotification": data_json[0].get('SendNotification'),
                                "IsNotificationSent": data_json[0].get('IsNotificationSent'),
                                "NotificationMessage": data_json[0].get('NotificationMessage'),
                                "TeamAreaUId": data_json[0].get('TeamAreaUId'),
                                "RetrainRequired": data_json[0].get('RetrainRequired'),
                                "_id": str(uuid.uuid4()),
                                "RequestId": requestId,
                                "CreatedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                "ModifiedOn": str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                                "ModelName": "null",
                                "ProblemType": "null",
                                "Status": "P",
                                "Message": "In - Progress",
                                "Progress": "5",
                                "pageInfo": "DataCleanUp",
                                "ParamArgs": "{}",
                                "Function": "DataCleanUp"
                                })
                                dbconn.close()
                                if invokeIngestData['InstaMl'] != 'null':
                                    utils.updateautotrain_record(correlationId,"DataCleanUp","25")
                                sys.argv[2] = requestId
                                sys.argv[3] = "DataCleanUp"
                                from main import invokeDataCleanup
                                #invokeDataCleanup.dataCleanup_for_single_file(correlationId,requestId,"DataCleanUp",userId,'AutoTrain')

                        else:
                            utils.updQdb(correlationId, 'E', 'Unique Identifier is having less than 90% uniqueness', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)

    
                else:
                    utils.store_pickle_file(MultiSourceDfs, correlationId)
                    utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
                    utils.updQdb(correlationId, 'M', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
        if mapping_flag == 'True':
            utils.updQdb(correlationId, 'P', '55', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            MultiSourceDfs_old = utils.load_pickle_file(correlationId)
            g = {}
            if not auto_retrain and not Incremental:
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs_old, g, parent, mapping,
                                                 mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                 datapre=None, lastDateDict=lastDateDict,MappingColumns=MappingColumns,previousLastDate = previousLastDate)
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
            else:
                column_names = []
                for entity in MultiSourceDfs_old['Entity']:
                    column_names = MultiSourceDfs_old['Entity'][entity].columns.tolist()
                    MultiSourceDfs['Entity'][entity] = MultiSourceDfs['Entity'][entity][column_names]
                    column_names = []  
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            
                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs, g, parent, mapping,
                                                 mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                 datapre=None, lastDateDict=lastDateDict,MappingColumns=MappingColumns,previousLastDate = previousLastDate)
                utils.logger(logger, correlationId, 'INFO', ('Save Multifile Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))            

            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
except Exception as e:
    
    utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
    if genericflow:
        utils.updateautotrain_record_forerror(correlationId,"DataCleanUp")
    if str(e.args[0]).__contains__('Data is not available for your selection'):
        utils.logger(logger, correlationId, 'INFO',str("Data is not available for your selection"),str(requestId))
    else:
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    #utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    utils.save_Py_Logs(logger, correlationId)
    sys.exit()

