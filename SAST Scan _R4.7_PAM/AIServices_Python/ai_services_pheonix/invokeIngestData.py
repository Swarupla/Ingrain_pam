# -*- coding: utf-8 -*-
"""
Created on Mon Jul 27 16:18:14 2020

@author: s.siddappa.dinnimani
"""
import time
start=time.time()                 
import configparser, os
import sys
from pathlib import Path
import platform
import re,glob
mainDirectory = str(Path(__file__).parent.parent.absolute())
if platform.system() == 'Linux':
    configpath = mainDirectory+"/pythonconfig.ini"
    EntityConfigpath = mainDirectory+"/pheonixentityconfig.ini"
elif platform.system() == 'Windows':
    configpath = mainDirectory+"\\pythonconfig.ini"
    EntityConfigpath = mainDirectory+"\\pheonixentityconfig.ini"

from datetime import datetime, timedelta
#Adding reference to Root path (File encrypter)
rootPath = str(Path(__file__).parent.parent.absolute())
sys.path.insert(0, rootPath )


import file_encryptor
config = configparser.RawConfigParser()
#configpath = mainDirectory+"/pythonconfig.ini"
#EntityConfigpath = mainDirectory+"/pheonixentityconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
EntityConfig = configparser.RawConfigParser()
EntityConfig.read(EntityConfigpath)										 

import sys
import pandas as pd
import numpy as np
import json
from datetime import datetime
import os
import requests
from urllib.parse import urlencode
from collections import ChainMap
import os,sys,inspect
currentdir = os.path.dirname(os.path.abspath(inspect.getfile(inspect.currentframe())))
parentdir = os.path.dirname(currentdir)
sys.path.insert(0,parentdir) 
import utils
from datetime import datetime,timedelta
from dateutil import relativedelta
import warnings
import base64
import EncryptData
end=time.time()
warnings.filterwarnings("ignore")
global min_df
min_df=20
auth_type = utils.get_auth_type()

def getcust(inid):
    from uuid import UUID
    newuuid=UUID(str(inid)).bytes
    return Binary(bytes(bytearray(newuuid)), UUID_SUBTYPE)

def callNotificationAPIforTO(correlation_id,uniId,mynewdata):
    
    AppServiceURL = config['BulkPredictionNotificationAPI']['AppServiceURL']
    
    customurl_token,status_code = utils.getEntityToken() #Envi specific
    
    logger = utils.logger('Get', correlation_id)
    
    utils.logger(logger,correlation_id,'INFO',"My data is " +str(mynewdata),str(uniId))
    utils.logger(logger,correlation_id,'INFO',"Url is " +str(AppServiceURL),str(uniId))
    
    utils.logger(logger,correlation_id,'INFO','Status Code is '+ str(status_code),str(uniId))
    utils.logger(logger,correlation_id,'INFO','Custom URL Token is '+ str(customurl_token),str(uniId))
    
    if status_code == 200:
        
        result=requests.post(AppServiceURL,data=mynewdata,headers={'Authorization': 'Bearer {}'.format(customurl_token)})
                        
        utils.logger(logger,correlation_id,'INFO','Result is '+ str(result),str(uniId))
    
        utils.logger(logger,correlation_id,'INFO','Status Code for notification API is '+ str(result.status_code),str(uniId))
        utils.logger(logger,correlation_id,'INFO','Content for notification API is '+ str(result.content),str(uniId))
        
    else :
        
        utils.logger(logger,correlation_id,'INFO','Status Code for notification API is not 200',str(uniId))

def Read_Data(correlationId,pageInfo,userId,uniId,invokeIngestData,flag = False):
    start_time=time.time()

    parent = invokeIngestData['Parent']
    logger = utils.logger('Get', correlationId)
    #utils.logger(logger, correlationId, 'INFO', ('import in invoke ingest data for read_data func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
    errFlag = False
    
    if pageInfo=='PredictData':
        min_df=int(config['Min_Data']['min_data_predict'])
    else:
        min_df=int(config['Min_Data']['min_data'])
    
    mapping_flag = invokeIngestData['mapping_flag']
    
    auto_retrain = False
    global lastDateDict				   
    lastDateDict = {}
    if invokeIngestData['Flag'] == "AutoRetrain":
        auto_retrain = True
    MappingColumns = {}
    if mapping_flag == "False":    
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        # Entities  Implementation
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        MultiSourceDfs = {}
        utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data'),str(uniId))
        utils.updQdb(correlationId, 'P', '15', pageInfo, userId,uniId)
        
        if invokeIngestData['pad'] != "null" and invokeIngestData['pad'] != '':
            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data for entities'),str(uniId))
            global entityDfs 
            entityDfs = {}
            lastDateDict['Entity'] = {}
            x = eval(invokeIngestData['pad'])
            if auth_type == 'AzureAD':
                entityAccessToken,status_code = utils.getEntityToken() #Envi specific
            elif auth_type == 'WindowsAuthProvider' or auth_type == 'Forms' or auth_type == 'Form':
                entityAccessToken = ''
                status_code = 200
            if status_code != 200:
                utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                entityDfs['Entity'] = "Phoenix API is returned " + str(status_code) + " code, Error Occured while calling Phoenix API"  
            else:
                if x["method"] == "AGILE":
                    utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {}'.format(x["method"])),str(uniId))
                    utils.logger(logger, correlationId, 'INFO', '#97invokeingestdata',str(uniId))
                    for entity, delType in x["Entities"].items():
                        if auto_retrain:
                            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for autoretrain'.format(x["method"])),str(uniId))
                            start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
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
                            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for deltype {}'.format(x["method"],delType)),str(uniId))
                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
    #                       
                            utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                            
                        elif delType == "AD/Agile":
                            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for deltype {}'.format(x["method"],delType)),str(uniId))
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)

                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
							
                            utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
						
                        elif delType == "ALL":
                            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for deltype {}'.format(x["method"],delType)),str(uniId))
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
                            if entity != 'Iteration':
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                            else:
                                utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                        else:
                            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                               
                elif x["method"] == "DEVOPS":
                    utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {}'.format(x["method"])),str(uniId))
                    for entity, delType in x["Entities"].items():
                        if delType in ["Devops", "AD/Devops", "ALL"]:
                            if auto_retrain:
                                #print ("inside")
                                utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for autoretrain'.format(x["method"])),str(uniId))			
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
                            else:
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                 }
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            devopsAPI = EntityConfig['CDMConfig']['CDMAPI']
                            devopsAPI = devopsAPI.format(urlencode(k))
                            #print (devopsAPI)
                            if entity != 'Iteration':
                                utils.CallCdmAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                            else:
                                utils.IterationAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                elif x["method"] == "AD":
                    utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {}'.format(x["method"])),str(uniId))
                    for entity, delType in x["Entities"].items():
                        #print(delType)
                        if delType in ["AD","AD/Agile","AD/Devops", "ALL","AD/PPM"]:
                            if auto_retrain:
                                utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for autoretrain'.format(x["method"])),str(uniId))			
                                #print ("inside")						
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
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
                                utils.CallCdmAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                            else:
                                utils.IterationAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                                
                elif x["method"] == "OTHERS":
                    utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {}'.format(x["method"])),str(uniId))
                    for entity, delType in x["Entities"].items():
                        #print(delType)
                        if delType in ["AD","AD/Agile","AD/Devops", "ALL","Others","Others/PPM"]:
                            if auto_retrain:
                                utils.logger(logger, correlationId, 'INFO',('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for autoretrain'.format(x["method"])),str(uniId))			
                                #print ("inside")						
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
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
                                utils.CallCdmAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                            else:
                                utils.IterationAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                elif x["method"] == "PPM":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:
                                utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data with method {} for autoretrain'.format(x["method"])),str(uniId))
                                start = utils.getlastDataPointCDM(correlationId, 'Entity', entity)
                                end = datetime.today().strftime('%m/%d/%Y')                           
                            else:
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                    }
                        
                            if delType == "Agile/PPM":
                                delType = "Agile"
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))                           
                            elif delType == "AD/PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                                       
                            elif delType == "PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    
                                    utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId,str(uniId))
                                    
                                else:
                                    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
                            else:
                                utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                
                else:
                    entityDfs = {}

            if isinstance(entityDfs[entity],str):
                utils.updateModelStatus(correlationId,uniId,"","Warning",str(entityDfs[entity]))
                utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                errFlag = True
                
            MultiSourceDfs['Entity'] = entityDfs
        else:
            MultiSourceDfs['Entity'] = {}
        utils.updQdb(correlationId, 'P', '40', pageInfo, userId,uniId)
        
       
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        # file Upload
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if invokeIngestData['fileupload']['fileList'] != 'null':  ###CHANGES
            utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data for file_upload'),str(uniId))
            fileDfs = {}
            x = invokeIngestData['fileupload']['fileList']
            if platform.system()=='Linux':
                x = x.replace('\\', '//')
                x = eval(x)
            else:
                x = invokeIngestData['fileupload']['fileList']
                y = config['filePath']['inputFilePath']
                x = eval(x)
                temp_path=''
                temp_path = os.path.basename(x[0])
                pattern=re.search(r'.+\/(.+)\/.+',str(x[0]))
                aicore =config['filePath']['inputFilextendedPath']
                y = y + "\\" + aicore
                y = y + "\\" + invokeIngestData['CorrelationId'] + "\\" + pattern.group(1)
                y = glob.glob(y+"\\*")
           
            
            for filepath in x:
                # print(filepath)
                if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
                    if platform.system()=='Windows':
                        filepath = y[0]
#                    data_t = utils.read_csv(filepath)
                    data_t = utils.read_csv(filepath)
                    utils.logger(logger, correlationId, 'INFO', "Number of rows" +str(data_t.shape[0]),str(uniId))
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True
                        
                        

                    elif data_t.shape[0] <= min_df:
                        fileDfs[filepath] = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True
                        
                        

                    elif data_t.shape[0] > utils.max_data():
                        fileDfs[filepath] = "Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource"                                                                                                                                                                                     
                    else:
                        fileDfs[filepath] = data_t
                elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):
                    if platform.system()=='Windows':
                        filepath = y[0]
                    data_t = utils.read_excel(filepath)
                    utils.logger(logger, correlationId, 'INFO', "Number of rows" +str(data_t.shape[0]),str(uniId))

#                    data_t = utils.read_excel(filepath)
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True
                        
                        

                    elif data_t.shape[0] <= min_df:
                        fileDfs[filepath] = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True
                        
                        

                    elif data_t.shape[0] > utils.max_data():
                        val=utils.max_data()
                        fileDfs[filepath] = "Number of records greater than "+str(val)+". Kindly upload data through mydatasource"                                                                         
                    else:
                        fileDfs[filepath] = data_t
                        
               
            MultiSourceDfs['File'] = fileDfs
            utils.logger(logger, correlationId, 'INFO', str(type(MultiSourceDfs['File'])),str(uniId))
            utils.logger(logger, correlationId, 'INFO', "In Invoke Ingest Data",str(uniId))
        else:
            MultiSourceDfs['File'] = {}
        
        try:
            
            if invokeIngestData['Customdetails']!="null" and (str(invokeIngestData.get('Customdetails').get('UsecaseID')) == str(config['TOUseCaseId']['DefectUseCaseId']) or str(invokeIngestData.get('Customdetails').get('UsecaseID')) == str(config['TOUseCaseId']['TestCaseUseCaseId'])):
               utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data for custom_format'),str(uniId))
               
               # invokeIngestData = eval(invokeIngestData)
               
               myval=invokeIngestData
               
               
               
               
               try:
                   myval['Customdetails']['InputParameters'] = eval(myval['Customdetails']['InputParameters'])
               except Exception as e:
                   utils.logger(logger, correlationId, 'INFO', "During eval getting an error"+str(e.args[0]),str(uniId))
               
               errFlag = utils.checkInputParameters(correlationId,uniId)
               
               if (errFlag==True):
                   utils.updateModelStatus(correlationId,uniId,"","Error","Invalid dates provided as input. Please provide EndDate greater than StartDate")
                   utils.updQdb(correlationId, 'E', "Invalid dates provided as input. Please provide EndDate greater than StartDate", pageInfo, userId,uniId)
                   
               usecase=str(myval['Customdetails']['UsecaseID'])
                                                                                 
               
               myval['Customdetails']['InputParameters']['DeliveryConstructUId']=str(myval['DeliveryConstructUId'])
               myval['Customdetails']['InputParameters']['ClientUID']=str(myval['ClientUID'])
               myval['Customdetails']['InputParameters']['UsecaseId']=str(myval['Customdetails']['UsecaseID'])
               myval['Customdetails']['InputParameters']['TotalRecordCount']=str(int(myval['Customdetails']['InputParameters']['TotalRecordCount']))
               myval['Customdetails']['InputParameters']['PageNumber']=str(int(myval['Customdetails']['InputParameters']['PageNumber']))
               myval['Customdetails']['InputParameters']['BatchSize']=str(int(myval['Customdetails']['InputParameters']['BatchSize']))
               myval['Customdetails']['InputParameters']['ReleaseUid'] = myval['Customdetails']['InputParameters']['ReleaseID']
               myval['Customdetails']['InputParameters']['FromDate'] = myval['Customdetails']['InputParameters']['StartDate']
               myval['Customdetails']['InputParameters']['ToDate'] = myval['Customdetails']['InputParameters']['EndDate']
               
               
               customDfs={'Customdetails':invokeIngestData['Customdetails']}
               AppId=customDfs.get('Customdetails').get('AppId')
               customurl_token,status_code=utils.CustomAuth(AppId)
               utils.logger(logger, correlationId, 'INFO',"Token is "+str(customurl_token),str(uniId))
               utils.logger(logger, correlationId, 'INFO', "Status Code before hitting hadoop is " +str(status_code),str(uniId))
               myval['Customdetails']['InputParameters']['ToDate']=(datetime.now() + timedelta(1)).strftime('%Y-%m-%d')
               mynewdata=json.dumps(myval['Customdetails']['InputParameters'])
               utils.logger(logger, correlationId, 'INFO', ('entering mapping_flag=false loop in invoke ingest data for read_data for custom_format which passes to hadoop custom {}'.format(mynewdata)),str(uniId))
               utils.logger(logger, correlationId, 'INFO',"Body is " +str(mynewdata) ,str(uniId))
    
               if status_code==200:
                   #result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                         #'Authorization': 'Bearer {}'.format(customurl_token)})
                   
                   k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                    }
                   li = str(invokeIngestData.get('Customdetails').get('AppUrl')) + '/?{}'
                   link = li.format(urlencode(k))
                   
                   
                   utils.logger(logger, correlationId, 'INFO',"Link is " +str(link) ,str(uniId))
    
                                      
                   result=requests.post(link,data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']}) #config['HadoopApi']['AppServiceUId']
                   utils.logger(logger, correlationId, 'INFO',"Status Code after hitting hadoop is " +str(result.status_code) ,str(uniId))
                   #utils.logger(logger, correlationId, 'INFO',str(result.content) ,str(uniId))
                   # utils.logger(logger, correlationId, 'INFO',str(link) ,str(uniId))
                   # utils.logger(logger, correlationId, 'INFO',str(mynewdata) ,str(uniId))
      
                   
                   
                   AppServiceURL = config['BulkPredictionNotificationAPI']['AppServiceURL']     
                  
    
                   if result.status_code==200:
                       #new#### added
                       if result.text=="No data available":
                           if auto_retrain:
                               MultiSourceDfs['Custom'] ="No incremental data available"                                                     
                               
                           else:
                               MultiSourceDfs['Custom'] ="Data is not available for your selection"                                                                
                               
                       else:
                           #print(result.json())
                           if result.json()['TotalRecordCount'] != 0:
                               utils.logger(logger, correlationId, 'INFO',"entered if block of total record_count",str(uniId))
                               #print('***new entity')
                               #newaddition for max_data_pull
                               maxdata = utils.maxdatapull(usecase,'AISavedUsecases')
                               nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
    
                               utils.logger(logger, correlationId, 'INFO',"Max data pull is " +str(maxdata) ,str(uniId))
                               clientData = result.json()['Entity']
                               months_counter=1
                               if maxdata < result.json()['TotalRecordCount'] and nonprod:
                                    actualfromdate=myval['Customdetails']['InputParameters']['FromDate']
                                    if (datetime.strptime(str(myval['Customdetails']['InputParameters']['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                                   
                                        mynewdata['Customdetails']['InputParameters']['FromDate'] = (datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                                        utils.logger(logger, correlationId, 'INFO',"entering if block of max_data<result_json with {} {} ".format(maxdata,result.json()['TotalRecordCount']) ,str(uniId))
                                        result=requests.post(link,data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']}) #config['HadoopApi']['AppServiceUId']
                                        while maxdata > result.json()['TotalRecordCount']:
                                            months_counter+=1
                                            if (datetime.strptime(str(myval['Customdetails']['InputParameters']['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                                                mynewdata['Customdetails']['InputParameters']['FromDate'] = (datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                                                result=requests.post(link,data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']}) #config['HadoopApi']['AppServiceUId']
                                        #entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=3*months_counter)).strftime('%m/%d/%Y')
                               
                               if clientData != []:
                                   clientData = pd.DataFrame(clientData)
                                   
                                   
                               numberOfPages = result.json()['TotalPageCount']
                               TotalRecordCount = result.json()['TotalRecordCount'] 
                               i = 1
                               ########while loop start here
                               while i < numberOfPages:
                                    myval['Customdetails']['InputParameters']['PageNumber']=int(i+1)
                                    myval['Customdetails']['InputParameters']['TotalRecordCount']=TotalRecordCount
                                    mynewdata=json.dumps(myval['Customdetails']['InputParameters'])
                                    
                                    k = {"ClientUId": invokeIngestData["ClientUID"],
                                     "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                    }
                                    
                                    li = str(invokeIngestData.get('Customdetails').get('AppUrl')) + '/?{}'
                                    link = li.format(urlencode(k))   
                                    
                                    entityresults=requests.post(link,data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                    if entityresults.status_code == 200:
                                        entityData=entityresults.json()['Entity']
                                        if entityData != []:
                                            df = pd.DataFrame(entityData)
                                            clientData=pd.concat([clientData,df]).reset_index(drop=True)
                                    i+=1         
                               ###########while loop ends here ########
                               #clientData.to_excel(r'/var/www/myWizard.IngrAInAIServices.WebAPI.Python/ai_services_pheonix/test.xlsx',index=False)
                               for col in clientData.columns:
                                   clientData[col].fillna(clientData[col].mode()[0], inplace=True)
                               utils.logger(logger, correlationId, 'INFO', "Columns of the data frame is " + str(clientData.columns),str(uniId))
                               utils.logger(logger, correlationId, 'INFO', "Dimensions of the data frame is "+ str(len(clientData)),str(uniId))
                               if clientData.shape[0]==0:
                                   MultiSourceDfs['Custom'] = "DataFrame is empty. No Data Available for Selection"
                                                       
                                   message = "DataFrame is empty. No Data Available for Selection"
                                   
                                   utils.logger(logger,correlationId,'INFO',str(message),str(uniId))
    
                                    
                                   mynewdata = {
                                        "CorrelationId": correlationId,
                                        "UniId":  uniId,
                                        "pageInfo": "Training",
                                        "Status" : "E",
                                        "Message":message
                                        }
                                   
                                   utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                                   utils.updQdb(correlationId, 'E', "Number of records less than or equal to 19. Please upload file with more records", pageInfo, userId,uniId)
                                   errFlag = True
                                    
                                   callNotificationAPIforTO(correlationId,uniId,mynewdata)
                                   
                                   
                                   
                               elif clientData.shape[0]> utils.max_data():
                                   MultiSourceDfs['Custom'] = "Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource"  
                        
                                   message = "Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource" 
                                    
                                   utils.logger(logger,correlationId,'INFO',str(message),str(uniId))
                                   
                                   mynewdata = {
                                        "CorrelationId": correlationId,
                                        "UniId":  uniId,
                                        "pageInfo": "Training",
                                        "Status" : "E",
                                        "Message":message
                                        }
                                    
                                   callNotificationAPIforTO(correlationId,uniId,mynewdata)
                                   
                                            
                               elif clientData.shape[0]<=min_df and not auto_retrain:
                                   MultiSourceDfs['Custom'] = "No of rows in the data is less than "+str(min_df)
                                                       
                                   message = "Number of rows in the data is less than "+str(min_df)
                                    
                                   utils.logger(logger,correlationId,'INFO',str(message),str(uniId))
                                   
                                   mynewdata = {
                                        "CorrelationId": correlationId,
                                        "UniId":  uniId,
                                        "pageInfo": "Training",
                                        "Status" : "E",
                                        "Message":message
                                        }

                                         
                                    
                                   utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                                   utils.updQdb(correlationId, 'E', "Number of records less than or equal to 19. Please upload file with more records", pageInfo, userId,uniId)
                                   errFlag = True
                                   
                                   callNotificationAPIforTO(correlationId,uniId,mynewdata)
                                   
                                            
                               else:
                                   MultiSourceDfs['Custom'] = clientData                       
                                        
                               utils.logger(logger, correlationId, 'INFO', str(type(MultiSourceDfs['Custom'])),str(uniId))
                           else:#incase record_count is 0
                                MultiSourceDfs['Custom'] = "Total record count is from hadoop table is {} hence cant proceed with training ".format(result.json()['TotalRecordCount'])
                                                       
                                message = "Total record count is from hadoop table is {} hence cant proceed with training".format(result.json()['TotalRecordCount'])
                                    
                                utils.logger(logger,correlationId,'INFO',str(message),str(uniId))
                                
                                mynewdata = {
                                        "CorrelationId": correlationId,
                                        "UniId":  uniId,
                                        "pageInfo": "Training",
                                        "Status" : "E",
                                        "Message":message
                                        }
                                    
                                         

                                utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                                utils.updQdb(correlationId, 'E', "Number of records less than or equal to 19. Please upload file with more records", pageInfo, userId,uniId)
                                errFlag = True
                                
                                callNotificationAPIforTO(correlationId,uniId,mynewdata)
                                
                                         
                   else:
                        MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
                        utils.updateModelStatus(correlationId,uniId,"","The Status Code after hitting post token generation is {}".format(str(result.status_code)),"The Status Code after hitting post token generation is {}".format(str(result.status_code)))
                        
                        customurl_token,status_code = utils.getEntityToken() #Envi specific 
                        
                        message = "The Status Code after hitting post token generation is "+str(result.status_code)
                        
                        utils.logger(logger,correlationId,'INFO',str(message),str(uniId))
                        
                        mynewdata = {
                            "CorrelationId": correlationId,
                            "UniId":  uniId,
                            "pageInfo": "Training",
                            "Status" : "E",
                            "Message":message
                            }
                        
                        utils.updateModelStatus(correlationId,uniId,"","Error",str(message))
                        utils.updQdb(correlationId, 'E', str(message), pageInfo, userId,uniId)
                        errFlag = True
                          
                        callNotificationAPIforTO(correlationId,uniId,mynewdata)
                        
                                 
               else:
                     MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
                     utils.updateModelStatus(correlationId,uniId,"","The Token is not Authorized","The Token is not Authorized")

            elif invokeIngestData['Customdetails']!="null" and str(invokeIngestData.get('Customdetails').get('UsecaseID')) == str(config['ScrumBanUseCaseId']['UseCaseId']):
               mylist=[]
               utils.logger(logger, correlationId, 'INFO', ('In Ambulance Line,entering mapping_flag=false loop in invoke ingest data for read_data for custom_format'),str(uniId))
               myval=invokeIngestData
               errFlag = utils.checkInputParameters(correlationId,uniId)
               
               if (errFlag==True):
                   utils.updateModelStatus(correlationId,uniId,"","Error","Invalid dates provided as input. Please provide EndDate greater than StartDate")
                   utils.updQdb(correlationId, 'E', "Invalid dates provided as input. Please provide EndDate greater than StartDate", pageInfo, userId,uniId)                                                          
               clientdet=myval['ClientUID']
               dcdet=myval['DeliveryConstructUId']
               usecase=myval['Customdetails']['UsecaseID']
               try:
                    myval['Customdetails']['InputParameters'] = eval(myval['Customdetails']['InputParameters'])
               except Exception as e:
                   utils.logger(logger, correlationId, 'INFO',str(e.args),str(uniId))
               total_count=int(myval['Customdetails']['InputParameters']['TotalRecordCount'])
               pagenumber=int(myval['Customdetails']['InputParameters']['PageNumber'])
               batch_size=int(myval['Customdetails']['InputParameters']['BatchSize'])
               mylist.append(dcdet)
               myval['Customdetails']['InputParameters']['DeliveryConstructUId']=mylist
               myval['Customdetails']['InputParameters']['ClientUID']=clientdet
               myval['Customdetails']['InputParameters']['UsecaseId']=usecase
               myval['Customdetails']['InputParameters']['TotalRecordCount']=total_count
               myval['Customdetails']['InputParameters']['PageNumber']=pagenumber
               myval['Customdetails']['InputParameters']['BatchSize']=batch_size
               customDfs={'Customdetails':invokeIngestData['Customdetails']}
               AppId=customDfs.get('Customdetails').get('AppId')
               customurl_token,status_code=utils.CustomAuth(AppId)
               myval['Customdetails']['InputParameters']['EndDate']=(datetime.now() + timedelta(1)).strftime('%Y-%m-%d')
               mynewdata=json.dumps(myval['Customdetails']['InputParameters'])
               utils.logger(logger, correlationId, 'INFO', ('In Ambulance Line,entering mapping_flag=false loop in invoke ingest data for read_data for custom_format which passes to hadoop custom {}'.format(mynewdata)),str(uniId))
               
               if status_code==200:
                   #result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                         #'Authorization': 'Bearer {}'.format(customurl_token)})
    
                   result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                   if result.status_code==200:
                       #new#### added
                       if result.text=="No data available":
                           if auto_retrain:
                               MultiSourceDfs['Custom'] = "No incremental data available"
                           else:
                               MultiSourceDfs['Custom'] = "Data is not available for your selection"
                               utils.updateModelStatus(correlationId,uniId,"","Error","Data is not available for your selection")
                               utils.updQdb(correlationId, 'E', 'Data is not available for your selection', pageInfo, userId,uniId)
                               errFlag = True
                       else:
                           if result.json()['TotalRecordCount'] != 0:
                                
                               clientData = result.json()['Client']
                               
                               #new addition
                               maxdata = utils.maxdatapull(usecase,'AISavedUsecases')
                               nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
    
                               months_counter=1
                               
                               if maxdata < result.json()['TotalRecordCount'] and nonprod:
                                   actualfromdate = mynewdata['Customdetails']['InputParameters']['StartDate']
                                   if (datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                                        mynewdata['Customdetails']['InputParameters']['StartDate']=(datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                                        result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                        while maxdata > result.json()['TotalRecordCount']:
                                            months_counter+=1
                                            if (datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y') > actualfromdate:
                                                mynewdata['Customdetails']['InputParameters']['StartDate']=(datetime.strptime(str(mynewdata['Customdetails']['InputParameters']['EndDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*months_counter)).strftime('%m/%d/%Y')
                                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    
    
                               #new addition ends here
                               if clientData != []:
                                   clientData=clientData[0]['Items']
                                   clientData = pd.DataFrame(clientData)
                                   
                                   
                               numberOfPages = result.json()['TotalPageCount']
                               TotalRecordCount = result.json()['TotalRecordCount'] 
                               i = 1
                               ########while loop start here
                               while i < numberOfPages:
                                    myval['Customdetails']['InputParameters']['PageNumber']=int(i+1)
                                    myval['Customdetails']['InputParameters']['TotalRecordCount']=TotalRecordCount
                                    mynewdata=json.dumps(myval['Customdetails']['InputParameters'])
                                    entityresults=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                                    if entityresults.status_code == 200:
                                        entityData=entityresults.json()['Client']
                                        if entityData != []:
                                            entityData=entityData[0]['Items']
                                            df = pd.DataFrame(entityData)
                                            clientData=pd.concat([clientData,df]).reset_index(drop=True)
                                    i+=1         
                               ###########while loop ends here ########
                               #clientData.to_excel(r'/var/www/myWizard.IngrAInAIServices.WebAPI.Python/ai_services_pheonix/test.xlsx',index=False)
                               for col in clientData.columns:
                                   clientData[col].fillna(" ", inplace=True)
                               utils.logger(logger, correlationId, 'INFO', str(clientData.columns),str(uniId))
                               if clientData.shape[0]==0:
                                   MultiSourceDfs['Custom'] = "DataFrame is empty. No Data Available for Selection"
                                   utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                   utils.updateModelStatus(correlationId,uniId,"","Error","DataFrame is empty. No Data Available for Selection")
                                   utils.updQdb(correlationId, 'E', 'DataFrame is empty. No Data Available for Selection', pageInfo, userId,uniId)
                                   errFlag = True
                               elif clientData.shape[0]<=min_df and not auto_retrain:
                                   MultiSourceDfs['Custom'] = "No of rows in the data is less than "+str(min_df)
                                   utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                   utils.updateModelStatus(correlationId,uniId,"","Error","No of rows in the data is less than "+str(min_df))
                                   utils.updQdb(correlationId, 'E', "No of rows in the data is less than "+str(min_df), pageInfo, userId,uniId)
                                   errFlag = True
                               elif clientData.shape[0]> utils.max_data():
                                   MultiSourceDfs['Custom'] = "Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource"
                                   utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                   utils.updateModelStatus(correlationId,uniId,"","Error","Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource")
                                   utils.updQdb(correlationId, 'E', "Number of records greater than "+str(utils.max_data())+". Kindly upload data through mydatasource", pageInfo, userId,uniId)
                                   errFlag = True
                               else:
                                   MultiSourceDfs['Custom'] = clientData
                                   utils.logger(logger, correlationId, 'INFO', 'In Ambulance Line, head of client data is' + str(clientData.head()),str(uniId))            
                                   utils.logger(logger, correlationId, 'INFO', str(type(MultiSourceDfs['Custom'])),str(uniId))
                           else:
                               MultiSourceDfs['Custom'] = "Data is not available for your selection,as getting total record count is zero"
                               utils.updateModelStatus(correlationId,uniId,"","Error","Data is not available for your selection,as getting total record count is zero")
                               utils.updQdb(correlationId, 'E', 'Data is not available for your selection,as getting total record count is zero', pageInfo, userId,uniId)
                               errFlag = True

                   else:
                        MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
                        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                                                                     
               else:
                     MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
                     utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                     
            elif invokeIngestData['Customdetails']!="null":
                ########################################################################################FDS_custom comes here
                if 'AICustom' in invokeIngestData['Customdetails']:
                    utils.logger(logger, correlationId, 'INFO', ('In fds_custom code for invoke_ingestdata#custom details'),str(uniId))
                    if  invokeIngestData['Customdetails']["AICustom"] == "True":
                        AppId=invokeIngestData.get('Customdetails').get('AppId')
                        customurl_token,status_code=utils.CustomAuth(AppId)
                        if auth_type == 'WINDOWS_AUTH' or status_code==200:
                            if auth_type == 'AzureAD' or auth_type == 'Azure':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                                    'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                            elif auth_type == 'WindowsAuthProvider':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                        auth=HttpNegotiateAuth())
                            elif auth_type=="Forms" or auth_type=='Form':
                                result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId'],'Authorization': (customurl_token)})
                            if result.status_code==200:
                                utils.logger(logger, correlationId, 'INFO', ('In status_code=200'),str(uniId))
                                data_json=result.json()
                                data=pd.DataFrame(data_json)
                                if 'DeliveryConstruct' in data.columns:
                                    data.drop('DeliveryConstruct',axis=1,inplace=True)
                                if data.shape[0]==0:
                                    utils.logger(logger, correlationId, 'Info', 'data_shape=0',str(uniId))
                                    utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                    MultiSourceDfs['Custom'] = {"Custom":"Data is not available for your selection"}
                                    utils.updateModelStatus(correlationId,uniId,"","Warning",str(MultiSourceDfs['Custom']['Custom']))
                                    utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                                    errFlag = True
                                elif data.shape[0]<=min_df and not auto_retrain :
                                    utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                    MultiSourceDfs['Custom'] = {"Custom":"No of rows in the data is less than "+str(min_df)}
                                    utils.updateModelStatus(correlationId,uniId,"","Warning",str(MultiSourceDfs['Custom']['Custom']))
                                    errFlag = True
                                    
                                else:
                                    utils.logger(logger, correlationId, 'INFO', 'fds_custom_Code_data_generated',str(uniId))
                                    if auth_type=="Forms" or auth_type=='Form':#pam server
                                        MultiSourceDfs['Custom'] = data
                                    else:
                                        data["UniqueRowID"] = data.ClientUId  + data.EndToEndUId + data.IncidentNumber
                                        MultiSourceDfs['Custom'] = data
                            else:
                                utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                                MultiSourceDfs['Custom'] = {"Custom":"Application API returned "+str(result.status_code)}
                
                                
                        else:
                            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                            MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(status_code)}



                ###############################################################################################FDS_custom ends here
                else:
                    utils.logger(logger, correlationId, 'INFO', ('In Generic Pull,entering mapping_flag=false loop in invoke ingest data for read_data for custom_format'),str(uniId))
                    myval=invokeIngestData
                    
                    try:
                        myval['Customdetails']['InputParameters'] = eval(myval['Customdetails']['InputParameters'])
                    except Exception as e:
                        utils.logger(logger, correlationId, 'INFO', "During eval getting an error"+str(e.args[0]),str(uniId))
                    
                    mynewdata=json.dumps(myval['Customdetails']['InputParameters'])
                    customDfs={'Customdetails':invokeIngestData['Customdetails']}
                    AppId=customDfs.get('Customdetails').get('AppId')
                    customurl_token,status_code=utils.CustomAuth(AppId)
                    utils.logger(logger, correlationId, 'INFO', 'Status code is '+str(status_code),str(uniId))
                    utils.logger(logger, correlationId, 'INFO', 'Custom URL token is '+str(customurl_token),str(uniId))
                    utils.logger(logger, correlationId, 'INFO', str(mynewdata),str(uniId))
                    
                    if status_code==200:
                        result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=mynewdata,headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token)})
                        if result.status_code==200:
                            #new#### added
                            if len(result.json()) ==0:
                                if auto_retrain:
                                    MultiSourceDfs['Custom'] = "No incremental data available"
                                else:
                                    MultiSourceDfs['Custom'] = "Data is not available for your selection"
                            else:                                
                                    clientData = result.json()
                                    
                                    if clientData != []:
                                        clientData = pd.DataFrame(clientData)
                                        
                                    for col in clientData.columns:
                                        clientData[col].fillna(" ", inplace=True)
                                    utils.logger(logger, correlationId, 'INFO', str(clientData.columns),str(uniId))
                                    if clientData.shape[0]==0:
                                        MultiSourceDfs['Custom'] = "DataFrame is empty. No Data Available for Selection"
                                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                                        errFlag = True  
                                                            
                                    elif clientData.shape[0]<=min_df and not auto_retrain:
                                        MultiSourceDfs['Custom'] = "No of rows in the data is less than "+str(min_df)
                                        utils.updateModelStatus(correlationId,uniId,"","Error","No. of records less than or equal to 19. Please validate the data.")
                                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                                        errFlag = True
                                                            
                                    else:
                                        MultiSourceDfs['Custom'] = clientData
                                        utils.logger(logger, correlationId, 'INFO', str(type(MultiSourceDfs['Custom'])),str(uniId))
                        else:
                            MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
                            utils.updateModelStatus(correlationId,uniId,"","Error","The Status Code after hitting post token generation is "+str(result.status_code))
                            utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                            errFlag = True
                                                                                        
                    else:
                        MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
                        utils.updateModelStatus(correlationId,uniId,"","Error","The Token is not Authorized")
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True

            elif 'CustomSource' in invokeIngestData.keys() and invokeIngestData['CustomSource'] != 'null':
                global customdataDfs
                customdataDfs = {}
                lastDateDict = {}
                customdata = invokeIngestData['CustomSource']
                t = base64.b64decode(customdata)
                dtext = json.loads(EncryptData.DescryptIt(t))
                if dtext["Type"] == "API":
                    entityArgs = dtext["BodyParam"]
                    TargetNode = dtext["TargetNode"].split('.')
                    hadoopApi = dtext["ApiUrl"]
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
                        MultiSourceDfs['Custom'] = customdataDfs
                    else:
                        MultiSourceDfs['Custom'] = {"custom":customdataDfs['custom']}
                        utils.updateModelStatus(correlationId,uniId,"","Warning",customdataDfs['custom'])
                        utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
                        errFlag = True

                elif dtext["Type"] == "CustomDbQuery":
                    try:
                        query = dtext['Query']
                        connection = utils.open_phoenixdbconn()
                        data = connection.command(eval(query))
                        data_df = pd.DataFrame(data['cursor']['firstBatch'])
                        for col in data_df.columns:
                            if col=='CreatedOn':
                                print(col,data_df[col].dtype,data_df[col].iloc[0].keys())
                            try:
                                if data_df[col].dtype == 'object' and 'DateTime' in data_df[col].iloc[0].keys():
                                    data_df[col] = [pd.to_datetime(d['DateTime']) for d in data_df[col]]
                            except Exception as e:
                                utils.logger(logger, correlationId, 'INFO', "error during DateTime conversion"+str(e.args[0]),str(uniId))
                        MultiSourceDfs['Custom'] = data_df
                    except:
                        from uuid import UUID
                        query = dtext['Query']
                        query = eval(query)
                        inid = query["pipeline"][0]["$match"]["AppServiceUId"]
                        connection = utils.open_phoenixdbconn()
                        from bson.binary import Binary, UUID_SUBTYPE
                        query["pipeline"][0]["$match"]["AppServiceUId"] = getcust(inid)
                        data = connection.command(query)
                        data_df = pd.DataFrame(data['cursor']['firstBatch'])
                        for col in data_df.columns:
                            if col=='CreatedOn':
                                print(col,data_df[col].dtype,data_df[col].iloc[0].keys())
                            try:
                                if data_df[col].dtype == 'object' and 'DateTime' in data_df[col].iloc[0].keys():
                                    data_df[col] = [pd.to_datetime(d['DateTime']) for d in data_df[col]]
                            except Exception as e:
                                utils.logger(logger, correlationId, 'INFO', "getting error while converting DateTime Columns"+str(e.args[0]),str(uniId))
                        MultiSourceDfs['Custom'] = data_df

                                                           
            else:
                 MultiSourceDfs['Custom']={}
                 
        except Exception as e:
            utils.logger(logger,correlationId,'INFO','Content is '+ str(invokeIngestData),str(uniId))
            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
            raise Exception("Ingesting_Data failed with DataSetUID")
        
        #entity data identification
        EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])

        j = []
        if MultiSourceDfs['File'] != {}:
            for k,v in MultiSourceDfs['File'].items():
                if  type(v) != str:
                    b = list(EntityMappingColumns.intersection(v.columns))
                    if len(b) == 2:
                        MultiSourceDfs['Entity'][k] = v 
                        j.append(k)
        for i in j:
            del MultiSourceDfs['File'][i] 
            
        if len(MultiSourceDfs['File']) > 1 and MultiSourceDfs['Entity'] == {}:
            for k,v in MultiSourceDfs['File'].items():
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
             utils.updQdb(correlationId, 'E',"Uploaded "+n+" files contains non CDM entity data, Mapping is not possible.Kindly Upload entity related data", pageInfo, userId,uniId)
        if  MultiSourceDfs['File'] == {} and MultiSourceDfs['Entity'] != {}:
            MultiSourceDfs, MappingColumns  = utils.TransformEntities(MultiSourceDfs,invokeIngestData["ClientUID"],invokeIngestData["DeliveryConstructUId"],EntityMappingColumns,parent,auto_retrain,flag = False)
    end_time=time.time()
    utils.logger(logger, correlationId, 'INFO', ('Total time in invokeingest read_data function which in  %s seconds ---'%(end_time - start_time)),str(uniId))
    
    return MultiSourceDfs,MappingColumns,lastDateDict,errFlag
