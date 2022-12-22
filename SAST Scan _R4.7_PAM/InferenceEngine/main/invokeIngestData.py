import platform

if platform.system() == 'Linux':
    conf_path = '/pythonconfig.ini'
    conf_path_pheonix = '/pheonixentityconfig.ini'
    work_dir = ''
elif platform.system() == 'Windows':
    conf_path = '\pythonconfig.ini'
    conf_path_pheonix = '\pheonixentityconfig.ini'
    work_dir = ''
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

from SSAIutils import utils
#from SSAIutils import encryption
import sys
import pandas as pd
import numpy as np
import json
from datetime import datetime
import os
import requests
from urllib.parse import urlencode
from collections import ChainMap
from SSAIutils import EncryptData
from SSAIutils import AggregateAgileData		
from SSAIutils.inference_engine_utils import *		
import base64

import multiprocessing 
import concurrent.futures
import math
#import ast
import warnings
warnings.filterwarnings("ignore")

correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]

#correlationId = "00040560-0000-0000-0000-000000000000"
#requestId = "00040560-0000-0000-0000-000000000000"
#pageInfo = 
#userId = sys.argv[4]

min_df =utils.min_data()
min_data_VDS = 104
EncryptionFlag = utils.getEncryptionFlag(correlationId)

#def main(correlationId,requestId,pageInfo,userId):
try:
    Incremental = False    
    # make Db connection get input sinf=gle payload details from "Ingrain Requests table"
    invokeIngestData, _,allparams = utils.getRequestParams(correlationId, requestId)
    #eval(invokeIngestData)
    #print("evaluated2")
    #print(correlationId, requestId, pageInfo, userId)
    utils.updQdb(correlationId, 'P', '5', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
    logger = utils.logger('Get', correlationId)
    #print("invokeIngestData:: ", invokeIngestData)
    #print("invokeIngestData_type:: ", type(invokeIngestData))
    #invokeIngestData = ast.literal_eval(invokeIngestData)
    invokeIngestData = eval(invokeIngestData)
    parent = invokeIngestData['Parent']
    mapping = invokeIngestData['mapping']
    mapping_flag = invokeIngestData['mapping_flag']
    ClientUID = invokeIngestData['ClientUId'] 
    DeliveryConstructUId = invokeIngestData['DeliveryConstructUId']
    insta_id = ''
    auto_retrain = False
    Incremental = False
    global lastDateDict
    lastDateDict = {}
    previousLastDate = None
    auth_type = utils.get_auth_type()
    #print ("abc",invokeIngestData['Flag'])
    if invokeIngestData['Flag'] == "AutoRetrain":
        auto_retrain = True
    if invokeIngestData['Flag'] == "Incremental":
        Incremental = True
    #print ("AutoRetrain",auto_retrain)		
    MappingColumns = {}
    if 'DataSetUId' in allparams:
        DataSetUId = allparams['DataSetUId']
    else:
        DataSetUId = False
    #print ("AutoRetrain",auto_retrain)		
    MappingColumns = {}
    if DataSetUId:
        message = utils.update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId)
        utils.update_usecasedefeinition(DataSetUId,correlationId)
        if message == "Ingestion completed":
            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
        else:
            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
            utils.updQdb(correlationId, 'E', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            
    else:
        if mapping_flag == 'False' or (mapping_flag =='True' and (Incremental or auto_retrain)):
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # Metrics data implementation
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            MultiSourceDfs = {}
            if invokeIngestData['metric'] != 'null':
                metricDfs = {}
                x = eval(invokeIngestData['metric'])
    #            interval = x['interval']
                # Autoretrain
                metricArgs = {
                    "ClientUID"            : invokeIngestData["ClientUId"],
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
                        "FromDate": lastDate,
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
                    meticData, metricDfs = utils.MetricFunction(metricArgs,metricDfs,metricsUrl,metricAccessToken)
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
                            finalMetricData["UniqueRowID"] = finalMetricData['ReleaseUId'] +  finalMetricData['ProcessedOn'] + finalMetricData['IterationUId'] ####### changesssssss
                            finalMetricData.rename(columns={'ProcessedOn': 'DateColumn'}, inplace = True)
                            finalMetricData['DateColumn'] = pd.to_datetime(finalMetricData['DateColumn'],errors = 'coerce')
                            

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
                #print (x)
                if status_code != 200:
                    entityDfs['Entity'] = "Phoenix API is returned " + str(status_code) + " code, Error Occured while calling Phoenix API"  
                else:	   
                    if x["method"] == "AGILE":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:																																				  
                                start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                previousLastDate = start
                                end = datetime.today().strftime('%m/%d/%Y')                           
                            elif Incremental:																		
                                start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')		   
                                end = datetime.today().strftime('%m/%d/%Y') 																												
                            else:																						 
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUId"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                }
                            if entity in ['Defect','Task','Risk','Issue']:
                                delType = "Agile"
                            if delType == "Agile":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
        #                        CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,config,deltype = None)
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,Incremental)                           
                            elif delType == "AD/Agile":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,Incremental)                           
                            elif delType == "ALL":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,Incremental)
                                else:
                                    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                #print("please provide correct method")
                                utils.logger(logger, correlationId, 'INFO', ' please provide correct method',str(requestId))

                               
                    elif x["method"] == "DEVOPS":
                        for entity, delType in x["Entities"].items():
                            if delType in ["Devops", "AD/Devops", "ALL"]:
                                if auto_retrain:
                                    #print ("inside")						
                                    start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                elif Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y') 
                                    start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUId"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                devopsAPI = EntityConfig['CDMConfig']['CDMAPI']
                                devopsAPI = devopsAPI.format(urlencode(k))
                                #print (devopsAPI)
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,Incremental)
                                else:
                                    utils.IterationAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                    elif x["method"] == "AD":
                        for entity, delType in x["Entities"].items():
                            #print(delType)
                            if delType in ["AD","AD/Agile","AD/Devops", "ALL","AD/PPM"]:
                                if auto_retrain:
                                    #print ("inside")						
                                    start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                if Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y') 
                                    start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUId"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                AdApi = EntityConfig['CDMConfig']['CDMAPI']
                                AdApi = AdApi.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,Incremental)
                                else:
                                    utils.IterationAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)                                
                    elif x["method"] == "OTHERS":
                        for entity, delType in x["Entities"].items():
                            #print(delType)
                            if delType in ["AD","AD/Agile","AD/Devops", "ALL","Others","Others/PPM"]:
                                if auto_retrain:
                                    #print ("inside")						
                                    start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                    previousLastDate = start
                                    end = datetime.today().strftime('%m/%d/%Y')
                                if Incremental:
                                    end = datetime.today().strftime('%m/%d/%Y') 
                                    start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')
                                else:
                                    end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                    start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUId"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                     }
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                OthersApi = EntityConfig['CDMConfig']['CDMAPI']
                                OthersApi = OthersApi.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                                else:
                                    utils.IterationAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                    elif x["method"] == "PPM":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:																																				  
                                start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                previousLastDate = start
                                end = datetime.today().strftime('%m/%d/%Y')                           
                            elif Incremental:																		
                                start = datetime.strptime(str(x["startDate"]), '%Y-%m-%d %H:%M:%S').strftime('%m/%d/%Y')		   
                                end = datetime.today().strftime('%m/%d/%Y') 																												
                            else:																						 
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                
                            k = {"ClientUId": invokeIngestData["ClientUId"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                }
                        
                            if delType == "Agile/PPM":
                                delType = "Agile"
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)                           
                            elif delType == "AD/PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)                            
                            elif delType == "PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                                else:
                                    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                #print("please provide correct method")
                                utils.logger(logger, correlationId, 'INFO', 'please provide correct method',str(requestId))
                                
                    else:
                        entityDfs = {}
                
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
                    x = x[0]
                    k = {"ClientUID": invokeIngestData["ClientUId"],
                         "DCID": invokeIngestData["DeliveryConstructUId"],
                         "ProblemType": x['ProblemType'],
                         "InstaID": x['InstaId'],
                         "TargetColumn": x['TargetColumn'],
                         "Dimension": x['Dimension']
                         }
                    insta_id = x['InstaId']
                    instaDataUrl = utils.getInstaURL(correlationId)
                    #if auto_retrain:
                    #    lastDataPoint = utils.getlastDataPoint(correlationId)
                
                    #below if block ignored for LDAP Merge
                    if instaDataUrl.startswith("mywizardphoenixam", 8):
                       # if config["GenericSettings"]["authProvider"] == "AzureAD":
                       #     accessToken,status_code = utils.getVDSAzureToken()
                       # else:
                       #     accessToken,status_code = utils.formVDSAuth()

                        instaTokenUrl = config['PAM']['PamTokenUrl']
                        auth_type = utils.get_auth_type()
                        if auth_type == 'AzureAD':
                            Creds = requests.post(instaTokenUrl, headers={'Content-Type': 'application/json'},
                                                  data=json.dumps({"username": config['PAM']['username'],
                                                                   "password": config['PAM']['pwd']}))

                            if Creds.status_code != 200:
                                utils.updQdb(correlationId, 'E', "Token API is returned " + str(
                                    Creds.status_code) + " code, Error Occured while calling API ", pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                            else:
                        
                                accessToken = Creds.json()['token']
                        if auth_type == 'WINDOWS_AUTH' or  Creds.status_code == 200:
                            if auto_retrain:
                                p = {"LastFitDate": lastDataPoint, "ProcessFlow": "IncrementalLoad"}
                            else:
                                p = {"LastFitDate": None, "ProcessFlow": "FullDump"}
                            k.update(p)
                            if auth_type == 'WINDOWS_AUTH':
                                result = requests.post(instaDataUrl, data=json.dumps(k),
                                                       headers={'Content-Type': 'application/json'},
                                                       auth=HttpNegotiateAuth())
                            else:
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
                                    InstaData[x['Dimension']] = pd.to_datetime(InstaData[x['Dimension']], format="%Y-%m-%d %H:%M",
                                                                               exact=True)
                                    lastDateDict['vds_InsatML'] = InstaData[x['Dimension']].max().strftime('%Y-%m-%d %H:%M:%S')
                    else:
                    
                        if x.get("Source",None)=="VDS(AIOPS)":
                            if auth_type == "AzureAD" == "AzureAD":
                                accessToken, status_code = utils.getVDSAIOPSAzureToken()
                            elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                accessToken, status_code = utils.formVDSAuth()
                            elif auth_type == 'WindowsAuthProvider':
                                accessToken = ''
                                status_code =200
                        else:
                            if auth_type == "AzureAD" == "AzureAD":
                                accessToken, status_code = utils.getVDSAzureToken()
                            elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                accessToken, status_code = utils.formVDSAuth()
                            elif auth_type == 'WindowsAuthProvider':
                                accessToken = ''
                                status_code =200
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
                            #print(k)
                            if auth_type == 'WindowsAuthProvider':
                                result = requests.post(instaDataUrl, data=json.dumps(k),headers={'Content-Type': 'application/json'},auth=HttpNegotiateAuth())
                            elif auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                result = requests.post(instaDataUrl, data=json.dumps(k),
                                                   headers={'Content-Type': 'application/json',
                                                            'authorization': 'bearer {}'.format(accessToken)})
                        
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
                    MultiSourceDfs['VDSInstaML'] = vdsInstaMlDfs

                # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                # InstaML Regression
                # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                elif x[0]["UseCaseId"] != 'null':
                    for x_i in x:
                        vdsInstaMlDfs[x_i["CorrelationId"]] = {}
                        lastDateDict[x_i["CorrelationId"]] = {}
                    k = {
                        "UseCaseID": x[0]["UseCaseId"],
                        "ClientUID": invokeIngestData["ClientUId"],
                        "DCID": invokeIngestData["DeliveryConstructUId"],
                        # "InstaID"              : x['InstaId']
                        # "Flag"                 : x['Flag']
                    }
                    instaDataUrl = utils.getInstaURL(correlationId)
                    if auth_type == "AzureAD":
                        accessToken, status_code = utils.getVDSAzureToken()
                    elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                        accessToken, status_code = utils.formVDSAuth()
                    elif auth_type == 'WindowsAuthProvider':
                        accessToken = ''
                        status_code =200
                    if auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
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
                        #print (k,instaDataUrl,accessToken)
                        if auth_type == 'WindowsAuthProvider':
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json'},auth=HttpNegotiateAuth())
                        elif auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                            result = requests.post(instaDataUrl, data=json.dumps(k),
                                               headers={'Content-Type': 'application/json',
                                                        'authorization': 'bearer {}'.format(accessToken)})
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
                    MultiSourceDfs['VDSInstaML'] = vdsInstaMlDfs
                    #print ("hereadafa",lastDateDict)
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
                for filepath in x:
                    # print(filepath)
                    if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
                        data_t = utils.read_csv(filepath)
                        #data_t.columns = data_t.columns.str.replace('.', '')
                        data_t = data_t.rename(columns=lambda x: x.strip())
                        for col in list(data_t.columns):
                            if str(col).endswith(".1"):
                                raise Exception("Duplicate column names detected. Please validate the data and try again.")
                        if data_t.shape[0] == 0:
                            fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        elif data_t.shape[0] <= min_df:
                            fileDfs[
                                filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                        else:
                            fileDfs[filepath] = data_t
                    elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):
                        data_t = utils.read_excel(filepath)
                        #data_t.columns = data_t.columns.str.replace('.', '')
                        data_t = data_t.rename(columns=lambda x: x.strip())
                        for col in list(data_t.columns):
                            if str(col).endswith(".1"):
                                raise Exception("Duplicate column names detected. Please validate the data and try again.")
                        if data_t.shape[0] == 0:
                            fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        elif data_t.shape[0] <= min_df:
                            fileDfs[
                                filepath] = "Number of records in the file is less than or equal to "+str(min_df)+". Please upload data file with more number of records."
                        else:
                            fileDfs[filepath] = data_t
                if 'AgileUsecase' in invokeIngestData['fileupload']:
                    Agileusecase = invokeIngestData['fileupload']['AgileUsecase']
                    oldcorrelationId = Agileusecase['oldcorrelationid']
                    columns_list = utils.getColumnnames(oldcorrelationId)       
                    new_column_list = list(data_t.columns)
                    if columns_list != new_column_list:
                        fileDfs[filepath] = "Column mismatch...please upload the file with same columns"																
               
                MultiSourceDfs['File'] = fileDfs
            else:
                MultiSourceDfs['File'] = {}
        
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            # Custome Connector
            # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if invokeIngestData['Customdetails']!='null':
                customDfs={'Customdetails':invokeIngestData['Customdetails']}
                AppId=customDfs.get('Customdetails').get('AppId')
                UsecaseId = customDfs.get('Customdetails').get('UsecaseID','None')#UsecaseID:"fa52b2ab-6d7f-4a97-b834-78af04791ddf"
                Ambulance = False
                if UsecaseId in ["fa52b2ab-6d7f-4a97-b834-78af04791ddf","169881ad-dc85-4bf8-bc67-7b1212836a97","be0c67a1-4320-461e-9aff-06a545824a32",
                                           "6761146a-0eef-4b39-8dd8-33c786e4fb86","8d3772f2-f19b-4403-840d-2fb230ac630f","668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2"]:
                    Ambulance = True
        
                cDetails = invokeIngestData["Customdetails"]
                auth_type = utils.get_auth_type()
                
                if AppId =="ba58a983-99a8-4030-9d17-29b337b4dd36":#changing min data for RSP360
                    min_df=3
            
                if "CustomFlags" in cDetails :
                    usecase  = cDetails["CustomFlags"]["FlagName"]
                    if usecase in ["IA_Defect","IA_Schedule"]:
                        url =  cDetails["AppUrl"]
                        if auth_type == 'AzureAD':
                            customurl_token,status_code=utils.CustomAuth(AppId)
                        elif auth_type == 'WindowsAuthProvider':
                            customurl_token = ''
                            status_code =200
                    
                        if status_code == 200:
                            data,code = utils.getIaUsaCaseData(ClientUID,DeliveryConstructUId,url,cDetails,customurl_token)
                            if type(data) != bool:
                                if not data.empty:          
                                   MultiSourceDfs['Custom'] = {"Custom":data}
                                else:
                                   MultiSourceDfs['Custom'] = {"Custom":"Data after processing is Empty"}
                            else:
                                MultiSourceDfs['Custom'] = {"Custom":"APP Api returned Error "+str(code)+"status code"}
                    
                    else:
                        fromdate =  cDetails["InputParameters"]["FromDate"]
                        todate =  cDetails["InputParameters"]["ToDate"]
                        data = AggregateAgileData.get_aggregated_data(ClientUID,DeliveryConstructUId,fromdate,todate,config,parent,Incremental,auto_retrain,flag = "agileusecase")
                        if not data.empty:   
                            MultiSourceDfs['Custom'] = {"Custom":data}
                        else:
                            MultiSourceDfs['Custom'] = {"Custom":"Data after processing is Empty"}
                   
                elif AppId in ["595fa642-5d24-4082-bb4d-99b8df742013","a3798931-4028-4f72-8bcd-8bb368cc71a9","9fe508f7-64bc-4f58-899b-78f349707efa"]:   #SPA Velocity prediction
                   #print ("i am herer1")
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
                      TotalRecordCount = 0
                      TotalPageCount =  1
                      PageNumber = 1
                      BatchSize = invokeIngestData['Customdetails']['InputParameters'].get("BatchSize")
                      data = pd.DataFrame()
                      while PageNumber <=TotalPageCount:
                          #print (PageNumber , TotalPageCount)
                          jsondata = {
                                    "ClientUID" : invokeIngestData["ClientUId"], 
                                    "DeliveryConstructUId" : [invokeIngestData["DeliveryConstructUId"]], 
                                    "StartDate" : startdate, 
                                    "EndDate" : enddate, 
                                    "TotalRecordCount" : TotalRecordCount,
                                    "PageNumber":PageNumber,
                                    "BatchSize":int(BatchSize)
                                  }
                          if Ambulance:                       
                                  jsondata["IterationUId"]=eval(invokeIngestData['Customdetails']['InputParameters'].get("IterationUId"))
                          elif AppId=="595fa642-5d24-4082-bb4d-99b8df742013":
                                  if UsecaseId in ["64a6c5be-0ecb-474e-b970-06c960d6f7b7","5cab6ea1-8af4-4f74-8359-e053629d2b98","68bf25f3-6df8-4e14-98fa-34918d2eeff1"]:    #SPA
                                      jsondata["IsTeamLevelData"] = "0"
                                  elif UsecaseId in ["877f712c-7cdc-435a-acc8-8fea3d26cc18","6b0d8eb3-0918-4818-9655-6ca81a4ebf30"]:  #'regression'
                                      jsondata["IsTeamLevelData"] = "1"
                                  elif UsecaseId == 'f0320924-2ee3-4398-ad7c-8bc172abd78d':    #timeseries
                                      jsondata["IsTeamLevelData"] = "1"
                                      Teamlevel = invokeIngestData['Customdetails']['InputParameters'].get("TeamAreaUId",None)
                                      if Teamlevel != "null" and Teamlevel != None:
                                          jsondata["TeamAreaUId"]=[Teamlevel]
                          if auth_type == 'AzureAD':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                             'Authorization': 'Bearer {}'.format(customurl_token)})
                          elif auth_type == 'WindowsAuthProvider':
                            result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json'},
                                                 auth=HttpNegotiateAuth())
                          #print (invokeIngestData.get('Customdetails').get('AppUrl'),jsondata,{'Content-Type': 'application/json',
                                                           #  'Authorization': 'Bearer {}'.format(customurl_token)})
                          #print ("otuout",result,invokeIngestData.get('Customdetails').get('AppUrl'),json.dumps(jsondata),'Bearer {}'.format(customurl_token))
                          if result.status_code!=200:
                              MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting "+invokeIngestData.get('Customdetails').get('AppUrl')+" is "+str(result.status_code)}
                              break
                          else:
                              data_json=result.json()
                              #print (data_json)
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
                      #print (invokeIngestData)          
                      #if allparams['ProblemType'] == "TimeSeries":
                      #    min_df = 9
                      if data.shape[0]==0:
                           if 'Custom' in MultiSourceDfs:
                               #print ("asasf",type(MultiSourceDfs['Custom']))
                               if not isinstance(MultiSourceDfs['Custom'],dict):
                                    MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                           else:
                               MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                      elif data.shape[0]<=min_df and not auto_retrain:
                           MultiSourceDfs['Custom'] = {"Custom":"Number of records less than or equal to  "+str(min_df)+".  Please provide minimum 20 valid records"}    
                      else:
                           #print ("herehrehre",data.columns)
                           #data.to_csv("TeamAllocationData.csv")
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
                               elif data.shape[0]<=min_df and not auto_retrain:		
                                   MultiSourceDfs['Custom'] = {"Custom":"Number of records less than or equal to  "+str(min_df)+".  Please provide minimum 20 valid records"} 
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
                                           #print ("abcdcd",data,aggDays)
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
                      #print (MultiSourceDfs['Custom'])       
                      #print (MultiSourceDfs['Custom'])
                else:
                   if auth_type == 'AzureAD' or auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                            customurl_token,status_code=utils.CustomAuth(AppId)
                   elif auth_type == 'WindowsAuthProvider':
                            customurl_token = ''
                            status_code =200
                   if auth_type == 'WINDOWS_AUTH' or status_code==200:
                       if "FetchType" in invokeIngestData.get('Customdetails') and invokeIngestData.get('Customdetails').get("FetchType")=="Multiple":
                           source_url = invokeIngestData.get('Customdetails').get('AppUrl')
                           body_params = invokeIngestData.get('Customdetails').get('InputParameters')
                           response = utils.exec_post_request(source_url, body_params, customurl_token, '1')

                           if response.status_code==200:
                               response_data = response.json()
                               try:
                                   actual_data = pd.DataFrame.from_dict(response_data['Data'])
                                   total_pages = int(response_data['TotalPageCount'])
                                   if total_pages > 1:
                                       ncores = multiprocessing.cpu_count()
                                       with concurrent.futures.ThreadPoolExecutor(max_workers=ncores*5) as executor:
                                           future_to_url = {executor.submit(
                                               utils.exec_post_request, 
                                               source_url,  
                                               body_params, 
                                               customurl_token,
                                               i) for i in range(1, total_pages+1)}
                                       for future in concurrent.futures.as_completed(future_to_url):
                                           try:
                                               res = future.result()
                                               if(res.status_code == 200):
                                                   response_data = res.json()
                                                   new_data = pd.DataFrame.from_dict(response_data['Data'])
                        
                                                   actual_data = pd.concat([new_data,actual_data],ignore_index=True)
                                                   actual_data.drop_duplicates(inplace=True)
                                                        
                                           except Exception as exc:
                                                #print('exception: %s' % (exc))
                                                utils.logger(logger, correlationId, 'INFO', 'exception: %s' % (exc),str(requestId))
                                                           
                                   MultiSourceDfs['Custom'] = {"Custom":actual_data} 
                               except Exception as e:
                                    MultiSourceDfs['Custom'] = {"Custom":e}  
                           else:
                                MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(response.status_code)}

                       else:
                           user_date_column_name = customDfs.get('Customdetails').get('DateColumn')		
                           if auth_type == 'AzureAD':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                                 'Authorization': 'Bearer {}'.format(customurl_token)})
                           elif auth_type == 'WindowsAuthProvider':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json'},
                                                     auth=HttpNegotiateAuth())
                           elif auth_type.upper() == 'FORM' or auth_type.upper() == 'FORMS':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                                 'Authorization':(customurl_token)})
										  
                           if result.status_code==200:
                               #print (str(result.content))			   
                               data_json=result.json()
                               #print (data_json)
                               data=pd.DataFrame(data_json)
                               if data.shape[0]==0:
                                   MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                               elif data.shape[0]<=min_df and not auto_retrain:
                                   MultiSourceDfs['Custom'] = {"Custom":"No of rows in the data is less than "+str(min_df)}
                                
                               else:
                                    if (user_date_column_name == None) or (user_date_column_name==""):
                                        MultiSourceDfs['Custom'] = {"Custom": "User has entered empty DateColumn name"}
                                    if user_date_column_name not in data.columns:
                                        MultiSourceDfs['Custom'] = {"Custom": "User specified DateColumn '{}' is not found in the data".format(user_date_column_name)}
                                    else:                               
                                        data[user_date_column_name] = pd.to_datetime(data[user_date_column_name])
                                        lastDateDict["Custom"] = {"user_date_column_name":data[user_date_column_name].max().strftime('%Y-%m-%d %H:%M:%S')}
                                    MultiSourceDfs['Custom'] = {"Custom":data}
                             
                           else:
                                MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
					 
                   else:
                         MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}

            else:
                 MultiSourceDfs['Custom']={}
            if 'CustomMultipleFetch' in invokeIngestData and  invokeIngestData['CustomMultipleFetch']!='null':
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
                            #print("multithreading pool workitems...")
                            utils.logger(logger, correlationId, 'INFO', "multithreading pool workitems...",str(requestId))
                            results = []
                            #token = generate_token(authentication,token_url, credentials) #check for exception for token generation
                        
                            if FetchType.strip().lower() == 'single': # PageNumber by default = 1
                                results = utils.call_api(Url, 1, BatchSize, correlationId, customurl_token,  UserEmailId, AppServiceUId, HttpMethod, key)
                                data = pd.DataFrame(results)
                                if DataFlag == "Incremental":
                                    frames = [incremental_data, data]
                                    data = pd.concat(frames)
                                #print(data)
                                MultiSourceDfs['CustomMultipleFetch'] = {"CustomMultipleFetch":data}
                            elif FetchType.strip().lower() == 'multiple':
                                pages = math.ceil(int(TotalNoOfRecords)/int(BatchSize))
                                #print('total pages:' +str(pages))
                    
                                with concurrent.futures.ThreadPoolExecutor(max_workers=ncores*(5)) as executor:
                                    future_to_url = {executor.submit(utils.call_api, Url, i, BatchSize, correlationId, customurl_token, UserEmailId, AppServiceUId, HttpMethod, key)  for i in range(1,int(pages)+1)}
                                    for future in concurrent.futures.as_completed(future_to_url):
                                        try:
                                            results.extend(future.result())
                                        except Exception as exc:
                                            #print(' generated an exception: %s' % (exc))
                                            utils.logger(logger, correlationId, 'INFO', 'generated an exception: %s' % (exc),str(requestId))
                                        else:
                                            #print(' length of results stories is %d ' % ( len(results)))
                                            utils.logger(logger, correlationId, 'INFO', ' length of results stories is %d ' % ( len(results)),str(requestId))
                                            
                                #print('end of getting records in...',abs(s - datetime.datetime.now()))
                                if len(results) == int(TotalNoOfRecords):
                                    data = pd.DataFrame(results)
                                    if DataFlag == "Incremental":
                                        frames = [incremental_data, data]
                                        data = pd.concat(frames)
                                    #print(str(data))
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
                MultiSourceDfs, MappingColumns  = utils.TransformEntities(MultiSourceDfs,ClientUID, DeliveryConstructUId,EntityMappingColumns,parent,Incremental,auto_retrain,flag=None,correlationId = correlationId)
            if mapping_flag == 'False':                    
                utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                #print ("hereadafa:::::::::::::::::::::::::::::::::::::::::::h:::::::",MultiSourceDfs)		
                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs, g,parent, mapping,
                                                     mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                     datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,previousLastDate = previousLastDate)
                utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
                if message == 'single_file':
                    utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
                    utils.logger(logger, correlationId, 'INFO', 'GenerateInflow Invoked',str(requestId))
                    
                    df = utils.data_from_chunks(corid=correlationId,collection="IE_IngestData")
                    df1=df.dropna(how="all")
                    threshold=int(len(df1)*0.02)
                    if df1.shape[0]<=min_df:
                        raise Exception("Number of records less than or equal to  "+str(min_df)+".  Please provide minimum "+str(min_df+1)+" valid records")
                    
                    data_copy=df1.copy()
                    data_copy,unique_values_dict,null_value_columns=get_uniqueValues(data_copy)
                    
                    #datw_columns can be used for dropdown list  i.e user to select the date column
                    date_columns=identify_date_columns(data_copy)
                        
                    databins,updatedUniquedict,bincolumns,numericalcolumns=binningContinousColumn(data_copy,unique_values_dict,threshold)
                    
                    sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
                    chunks, size = utils.file_split(databins,Incremental=False,appname='')
                    utils.save_data_chunks(chunks, "IE_PreprocessedData", correlationId, pageInfo, userId,list(databins.columns),False,requestId,
                                                  sourceDetails, colunivals=None, lastDateDict={},previousLastDate = '')
    
    
                    dimensionlist,target_columns,removedcolumns,suggestedDimslist,filtervaluesdict=get_important_columns(databins,updatedUniquedict,date_columns,null_value_columns,bincolumns)
                    
                    filtervaluesjsonstr=assignFalseForFilterValues(filtervaluesdict)
                    if EncryptionFlag:
                        filtervaluesjsonstr = EncryptData.EncryptIt(filtervaluesjsonstr)
                                      
                    if len(date_columns)==0 and(len(dimensionlist)+len(suggestedDimslist) < 2 ):
                        raise Exception("Date column and categorical/continuous columns are missing from the data. Please try with other datasets")
                                    
                    if len(date_columns) ==0:
                        raise Exception("There are no date columns present. Please try with other dataset")
                                
                    if len(dimensionlist)+len(suggestedDimslist) < 2:
                        raise Exception("There should be atleast two columns other than date column which can be categorical type or numerical type or combination of both (categorical/numerical) types")
                    
                    if len(dimensionlist)<=1:
                        raise Exception("Data does not have enough features to generate Inferences")
                    
                    utils.update_config(correlationId,target_columns,date_columns,dimensionlist,suggestedDimslist,filtervaluesjsonstr,bincolumns,numericalcolumns,userId)
                    utils.logger(logger, correlationId, 'INFO', 'GenerateInflow completed',str(requestId))
                    utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
    
                else:
                    utils.store_pickle_file(MultiSourceDfs, correlationId)
                    utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
                    utils.updQdb(correlationId, 'M', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
        if mapping_flag == 'True':
            utils.updQdb(correlationId, 'P', '55', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
            MultiSourceDfs_old = utils.load_pickle_file(correlationId)
            g = {}
            if not auto_retrain and not Incremental:

                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs_old, g, parent, mapping,
                                                 mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                 datapre=None, lastDateDict=lastDateDict,MappingColumns=MappingColumns,previousLastDate = previousLastDate)
            else:
                column_names = []
                for entity in MultiSourceDfs_old['Entity']:
                    column_names = MultiSourceDfs_old['Entity'][entity].columns.tolist()
                    MultiSourceDfs['Entity'][entity] = MultiSourceDfs['Entity'][entity][column_names]
                    column_names = []        
                message = utils.save_data_multi_file(correlationId, pageInfo,requestId, userId, MultiSourceDfs, g, parent, mapping,
                                                 mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,Incremental,
                                                 datapre=None, lastDateDict=lastDateDict,MappingColumns=MappingColumns,previousLastDate = previousLastDate)
            
            utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)),str(requestId))
            # GenerateInflow - Started
            utils.logger(logger, correlationId, 'INFO', 'GenerateInflow Invoked',str(requestId))
            
            df = utils.data_from_chunks(corid=correlationId,collection="IE_IngestData")
            df1=df.dropna(how="all")
            threshold=int(len(df1)*0.02)
            if df1.shape[0]<=min_df:
                raise Exception("Number of records less than or equal to  "+str(min_df)+".  Please provide minimum "+str(min_df+1)+" valid records")
            
            data_copy=df1.copy()
            data_copy,unique_values_dict,null_value_columns=get_uniqueValues(data_copy)
            
            utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            
            #datw_columns can be used for dropdown list  i.e user to select the date column
            date_columns=identify_date_columns(data_copy)
                
            databins,updatedUniquedict,bincolumns,numericalcolumns=binningContinousColumn(data_copy,unique_values_dict,threshold)
            
            utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
            sourceDetails = {"CID": None, "DUID": None, "Entity": "Manual", "Source": "FILE"}
            chunks, size = utils.file_split(databins,Incremental=False,appname='')
            utils.save_data_chunks(chunks, "IE_PreprocessedData", correlationId, pageInfo, userId,list(databins.columns),False,requestId,
                                                  sourceDetails, colunivals=None, lastDateDict={},previousLastDate = '')
    
            dimensionlist,target_columns,removedcolumns,suggestedDimslist,filtervaluesdict=get_important_columns(databins,updatedUniquedict,date_columns,null_value_columns,bincolumns)
            
            filtervaluesjsonstr=assignFalseForFilterValues(filtervaluesdict)
            if EncryptionFlag:
                filtervaluesjsonstr = EncryptData.EncryptIt(filtervaluesjsonstr)
                  
            
            utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)

            
            if len(date_columns)==0 and(len(dimensionlist)+len(suggestedDimslist) < 2 ):
                raise Exception("Date column and categorical/continuous columns are missing from the data. Please try with other datasets")
                            
            if len(date_columns) ==0:
                raise Exception("There are no date columns present. Please try with other dataset")
                        
            if len(dimensionlist)+len(suggestedDimslist) < 2:
                raise Exception("There should be atleast two columns other than date column which can be categorical type or numerical type or combination of both (categorical/numerical) types")
            
            if len(dimensionlist)<=1:
                raise Exception("Data does not have enough features to generate Inferences")
            
            utils.update_config(correlationId,target_columns,date_columns,dimensionlist,suggestedDimslist,filtervaluesjsonstr,bincolumns,numericalcolumns,userId)
            utils.logger(logger, correlationId, 'INFO', 'GenerateInflow completed',str(requestId))
            
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
except Exception as e:
    utils.updQdb(correlationId, 'E', str(e.args[0]), pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=Incremental,requestId=requestId)
    utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    if str(e.args[0]).__contains__('Data is not available for your selection'):
        utils.logger(logger, correlationId, 'INFO',str("Data is not available for your selection"),str(requestId))
    else:
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    #utils.save_Py_Logs(logger, correlationId)
    sys.exit()
